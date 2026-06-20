using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JNPF.InteAssistant.Gates;

/// <summary>
/// 需求门控服务（企业级最终版）
///
/// 职责分离：
///   硬规则 → 纯格式检查（长度、垃圾、附件数量）→ 零误杀
///   LLM   → 语义分析（行业、实体、流程、约束）→ 泛行业
///
/// 执行顺序（调用方保证）：
///   1. 附件提取（提取文本，不校验）
///   2. 合并用户文字 + 附件文本
///   3. 硬规则校验（格式检查，用合并后的完整文本）
///   4. LLM 成熟度评估（语义分析）
///   5. 三模式 Prompt 生成
/// </summary>
public class RequirementGateService : ITransient
{
    private readonly ILlmGatewayService _llmGateway;
    private readonly ILogger<RequirementGateService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly string[] ForceRefineKeywords =
    {
        "开始分析", "可以了", "够了", "开始吧", "分析吧", "没有了", "就这些",
        "不用问了", "直接分析", "开始设计",
        "start analysis", "go ahead", "enough", "that's all", "proceed"
    };

    private static readonly string[] GarbagePatterns =
    {
        @"^test$", @"^测试$", @"^123$", @"^abc$", @"^aaa+$", @"^bbb+$",
        @"^hello$", @"^你好$", @"^aaa$", @"^bbb$", @"^ccc$",
        @"^(.)\1{4,}$"
    };

    public RequirementGateService(
        ILlmGatewayService llmGateway,
        ILogger<RequirementGateService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _llmGateway = llmGateway;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    // ═══════════════════════════════════════════════════
    // 硬规则校验 — 纯格式检查，零语义判断
    // ═══════════════════════════════════════════════════

    public HardRuleResult ValidateHardRules(string text, int attachmentCount)
    {
        var content = (text ?? "").Trim();

        // 规则1：文字或附件至少有一个
        if (content.Length < 10 && attachmentCount == 0)
        {
            return Fail("请输入需求描述",
                "请描述您要构建的系统，或上传需求文档/截图。");
        }

        // 规则2：垃圾内容过滤（仅短文本触发）
        if (content.Length > 0 && content.Length < 50 &&
            GarbagePatterns.Any(p => Regex.IsMatch(content, p, RegexOptions.IgnoreCase)))
        {
            return Fail("检测到无效输入", "请输入真实的业务需求描述。");
        }

        // 规则3：长度上限
        if (content.Length > 200000)
        {
            return Fail("输入内容过长", "请精简到20万字以内。");
        }

        // 规则4：附件数量上限
        if (attachmentCount > 10)
        {
            return Fail("附件数量超限", "最多上传10个附件。");
        }

        return new HardRuleResult
        {
            Passed = true,
            Reason = $"硬规则通过（文字{content.Length}字，{attachmentCount}个附件）"
        };
    }

    // ═══════════════════════════════════════════════════
    // 追问刹车
    // ═══════════════════════════════════════════════════

    public bool IsForceRefine(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return ForceRefineKeywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsMaxRoundsReached(int roundCount, int maxRounds = 20)
    {
        return roundCount >= maxRounds;
    }

    // ═══════════════════════════════════════════════════
    // LLM 成熟度评估 — 泛行业语义分析
    // ═══════════════════════════════════════════════════

    public async Task<MaturityResult> EvaluateMaturity(
        List<ChatMessage> history, string provider, CancellationToken ct = default)
    {
        var systemPrompt = """
            你是企业级需求成熟度评估器。根据对话历史判断需求信息的完整度。

            你必须理解任何行业的业务描述，不限于特定行业。

            评估维度（总分0-100）：

            1. 业务领域清晰度 (0-30)
               - 30分：明确系统名称+行业+目标
               - 25分：提到系统类型（"进销存""HR""CRM""OA"等词隐含行业知识）
               - 10分：模糊描述
               - 0分：无法判断

            2. 核心实体识别 (0-30)
               - 不同行业有不同实体，根据用户描述的领域自行识别
               - 例如：进销存→商品/订单/仓库、医院→患者/处方/科室、学校→学生/课程/教师
               - 25分：5+个实体
               - 15分：2-4个
               - 5分：1个
               - 0分：无法识别

            3. 业务流程暗示 (0-20)
               - 暗示也算（"采购要审批"=采购流程+审批节点）
               - 20分：完整流程描述
               - 15分：隐含流程
               - 0分：无

            4. 约束/规模信息 (0-20)
               - 20分：具体约束（并发数、数据量、部署方式）
               - 10分：模糊约束
               - 0分：无

            mode判定：score < 25 → "explore", score 25-49 → "confirm", score >= 50 → "refine"

            只输出JSON：
            {"score":数字,"mode":"explore|confirm|refine","domain":"行业/领域","entities":["实体1","实体2"],"missing":["缺1"],"strengths":["有1"],"nextQuestion":"下一个该问的问题"}
            """;

        var request = new ChatCompletionRequest
        {
            ProviderCode = provider,
            SystemPrompt = systemPrompt,
            Messages = history,
            MaxTokens = 500,
            Temperature = 0.3,
            ResponseFormat = "json",
            MaxRetries = 1,
            TimeoutMs = 30000
        };

        try
        {
            var response = await _llmGateway.ChatAsync(request, ct);
            if (!response.IsSuccess)
            {
                _logger.LogWarning("成熟度评估失败，降级refine: {Error}", response.Error);
                return new MaturityResult { Score = 60, Mode = "refine" };
            }

            var json = ExtractJson(response.Content);
            var parsed = JsonSerializer.Deserialize<MaturityResult>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return parsed ?? new MaturityResult { Score = 60, Mode = "refine" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "成熟度评估异常，降级refine");
            return new MaturityResult { Score = 60, Mode = "refine" };
        }
    }

    // ═══════════════════════════════════════════════════
    // 三模式 System Prompt
    // ═══════════════════════════════════════════════════

    public string GetSystemPrompt(string mode, MaturityResult maturity)
    {
        return mode switch
        {
            "explore" => $"""
                你是AI架构顾问。用户的需求信息较少，需要帮用户梳理。

                已识别的行业/领域：{maturity.Domain ?? "未知"}
                已识别的业务实体：{string.Join("、", maturity.Entities ?? new List<string>())}
                已识别：{string.Join("、", maturity.Strengths ?? new List<string>())}
                缺失：{string.Join("、", maturity.Missing ?? new List<string>())}

                任务：
                1. 根据用户描述的领域，生成该行业的合理业务假设清单
                2. 每个假设给出选项（□ 前缀）
                3. 一次最多追问3个问题（❓ 前缀）
                4. 最后提醒用户：补充完可以说"开始分析"

                用中文，Markdown格式。
                """,

            "confirm" => $"""
                你是AI架构顾问。用户已提供部分信息，需确认和补充。

                已识别的行业/领域：{maturity.Domain ?? "未知"}
                已识别的业务实体：{string.Join("、", maturity.Entities ?? new List<string>())}
                已确认：{string.Join("、", maturity.Strengths ?? new List<string>())}
                待确认：{string.Join("、", maturity.Missing ?? new List<string>())}

                任务：
                1. 整理已确认信息，给出结构化总结（✅ 标记）
                2. 针对待确认点追问（❓ 标记）
                3. 提醒确认后可说"开始分析"

                用中文，Markdown格式。
                """,

            "refine" => """
                你是AI架构顾问。需求信息已充分，做全面需求分析。

                按以下结构输出：
                ## 系统概述
                ## 核心业务实体（含属性和关系）
                ## 业务流程分析（含步骤和异常分支）
                ## 业务规则
                ## 待确认事项
                ## 架构建议

                用中文，Markdown格式，条理清晰，内容详实。
                """,

            _ => "你是AI开发助手，用中文回复。"
        };
    }

    // ═══════════════════════════════════════════════════
    // 多模态图片提取
    // ═══════════════════════════════════════════════════

    public async Task<string> ExtractFromImages(
        List<AttachmentFile> images,
        string apiUrl,
        string apiKey,
        string model,
        CancellationToken ct = default)
    {
        if (images == null || images.Count == 0) return "";

        var client = _httpClientFactory.CreateClient();
        var results = new List<string>();

        foreach (var image in images)
        {
            try
            {
                var result = await ProcessSingleImageAsync(client, image, apiUrl, apiKey, model, ct);
                results.Add(result);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("图片分析被取消: {FileName}", image.FileName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "图片处理异常: {FileName}", image.FileName);
                results.Add($"[图片 {image.FileName} 处理异常：{ex.Message}]");
            }
        }

        return string.Join("\n\n", results);
    }

    private async Task<string> ProcessSingleImageAsync(
        HttpClient client, AttachmentFile image,
        string apiUrl, string apiKey, string model,
        CancellationToken ct)
    {
        var base64 = Convert.ToBase64String(image.Content);
        var mimeType = GetMimeType(image.FileName);

        var payload = new
        {
            model = model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = "请从图片中提取所有与软件需求相关的业务信息：界面元素、表格字段、业务流程、系统截图中的数据。用结构化中文列出。" },
                        new { type = "image_url", image_url = new { url = $"data:{mimeType};base64,{base64}" } }
                    }
                }
            },
            max_tokens = 2000
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var jsonPayload = JsonSerializer.Serialize(payload);
        using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        request.Content = content;

        using var response = await client.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
        {
            var responseStr = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseStr);
            var extractedText = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            _logger.LogInformation("图片分析成功: {FileName}", image.FileName);
            return $"[图片 {image.FileName} 分析结果]\n{extractedText}";
        }
        else
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("图片分析失败: {Status}, Body={Body}", response.StatusCode, errorBody);
            return $"[图片 {image.FileName} 分析失败，状态码：{response.StatusCode}]";
        }
    }

    // ═══════════════════════════════════════════════════
    // 辅助方法
    // ═══════════════════════════════════════════════════

    private static HardRuleResult Fail(string reason, string hint) =>
        new() { Passed = false, Reason = reason, Hint = hint };

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start) return text;
        var candidate = text[start..(end + 1)];
        try
        {
            using var doc = JsonDocument.Parse(candidate);
            return candidate;
        }
        catch (JsonException) { return text; }
        catch (ArgumentException) { return text; }
    }

    private static string GetMimeType(string fileName) =>
        Path.GetExtension(fileName)?.ToLower() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            _ => "image/jpeg"
        };
}

// ═══════════════════════════════════════════════════
// DTO（精简版）
// ═══════════════════════════════════════════════════

public class HardRuleResult
{
    public bool Passed { get; set; }
    public string Reason { get; set; } = "";
    public string Hint { get; set; } = "";
}

public class MaturityResult
{
    public int Score { get; set; }
    public string Mode { get; set; } = "explore";
    public string? Domain { get; set; }
    public List<string>? Entities { get; set; }
    public List<string> Missing { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public string NextQuestion { get; set; } = "";
}
