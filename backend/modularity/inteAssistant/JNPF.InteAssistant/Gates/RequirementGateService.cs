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
/// 需求门控服务
///
/// 修复项（相对清言版本）：
///   ✅ ExtractJson 中 JsonDocument 加 using（修复内存泄漏 #3）
///   ✅ ExtractFromImages 中 StringContent 加 using（修复内存泄漏 #4）
///   ✅ HttpClient 认证改为 per-request HttpRequestMessage（修复最佳实践 #8）
///   ✅ ExtractFromImages 包含 model 参数（修复编译错误 #1）
///   ✅ API Key 从配置读取，不硬编码（修复配置风险 #6）
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

    private static readonly Dictionary<string, string[]> DimensionKeywords = new()
    {
        ["系统概述"] = new[] { "系统", "平台", "管理系统", "业务领域", "行业", "目标", "用户群", "建设", "项目" },
        ["业务实体"] = new[] { "管理", "信息", "数据", "记录", "档案", "清单", "台账", "物料", "商品", "订单", "客户", "供应商", "仓库", "员工", "设备", "项目", "凭证" },
        ["角色权限"] = new[] { "角色", "权限", "用户", "管理员", "操作员", "审批人", "岗位", "职责", "负责人", "仓管", "采购员", "销售员", "财务", "人事" },
        ["业务流程"] = new[] { "流程", "步骤", "审批", "下单", "入库", "出库", "首先", "然后", "接着", "最后", "→", "->", "流转", "状态" },
        ["数据报表"] = new[] { "报表", "统计", "分析", "导出", "图表", "数据量", "每天", "每月", "条记录", "万条", "Excel" },
        ["系统集成"] = new[] { "对接", "集成", "接口", "API", "ERP", "财务系统", "电商平台", "第三方", "外部系统", "同步", "数据交换" },
        ["非功能需求"] = new[] { "性能", "安全", "部署", "并发", "响应时间", "等保", "加密", "公有云", "私有化", "高可用", "容灾" },
        ["附件材料"] = new[] { "截图", "附件", "原型", "图片", "文档", "模板" }
    };

    private static readonly string[] EntityKeywords =
    {
        "管理", "信息", "数据", "记录", "档案", "清单", "台账",
        "物料", "工单", "设备", "生产线", "BOM", "工艺", "质检", "配方", "排产", "工序",
        "商品", "SKU", "订单", "客户", "供应商", "仓库", "库存", "采购", "销售", "入库", "出库",
        "员工", "考勤", "薪资", "绩效", "招聘", "培训", "社保", "请假",
        "项目", "任务", "里程碑", "预算", "合同", "分包",
        "凭证", "科目", "应收", "应付", "发票", "报销",
        "巡检", "保养", "维修", "备件", "故障", "点检",
        "审批", "会签", "或签", "退回", "转办", "催办"
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
    // 硬规则校验
    // ═══════════════════════════════════════════════════

    public HardRuleResult ValidateHardRules(string text, int attachmentCount)
    {
        var content = (text ?? "").Trim();

        // 规则1：最低门槛
        if (content.Length < 500 && attachmentCount == 0)
        {
            return Fail("输入内容不足",
                "企业级需求分析需要足够的业务信息（至少500字）。\n\n" +
                "请提供以下信息：\n" +
                "1. 系统名称和所属行业\n" +
                "2. 核心业务对象（如商品、订单、仓库）\n" +
                "3. 使用角色及职责\n" +
                "4. 核心业务流程\n" +
                "5. 截图或文档附件");
        }

        // 规则2：垃圾内容
        if (content.Length > 0 && GarbagePatterns.Any(p =>
            Regex.IsMatch(content, p, RegexOptions.IgnoreCase)))
        {
            return Fail("检测到无效输入", "请输入真实的业务需求描述。");
        }

        // 规则3：长度上限
        if (content.Length > 100000)
        {
            return Fail("需求文本过长", "请精简到10万字以内。");
        }

        // 规则4：附件数量
        if (attachmentCount > 10)
        {
            return Fail("附件数量超限", "最多上传10个附件。");
        }

        // 规则5：文字过短时必须有附件
        if (content.Length < 2000 && attachmentCount == 0)
        {
            return Fail("文字描述较少",
                "您的描述不足2000字且无附件。请补充业务细节，或上传需求文档/系统截图。");
        }

        // 规则6：维度覆盖率
        var dimensions = DetectDimensions(content, attachmentCount);
        if (dimensions.Covered.Count < 2)
        {
            return Fail("需求信息覆盖维度不足",
                $"当前仅覆盖{dimensions.Covered.Count}个维度（至少需要2个）。\n" +
                $"已覆盖：{string.Join("、", dimensions.Covered)}\n" +
                $"未覆盖：{string.Join("、", dimensions.Missing)}");
        }

        // 规则7：业务实体
        var entityCount = CountEntities(content);
        if (entityCount < 1)
        {
            return Fail("未识别到业务实体",
                "未识别到具体的业务对象。请描述系统涉及的核心业务对象。");
        }

        return new HardRuleResult
        {
            Passed = true,
            Reason = $"硬规则通过（{dimensions.Covered.Count}/8维度，{entityCount}个实体）",
            DimensionCount = dimensions.Covered.Count,
            EntityCount = entityCount,
            CoveredDimensions = dimensions.Covered,
            MissingDimensions = dimensions.Missing
        };
    }

    // ═══════════════════════════════════════════════════
    // 维度检测 + 实体计数
    // ═══════════════════════════════════════════════════

    private DimensionDetection DetectDimensions(string text, int attachmentCount)
    {
        var covered = new List<string>();
        var missing = new List<string>();

        foreach (var (dim, keywords) in DimensionKeywords)
        {
            if (dim == "附件材料")
            {
                if (attachmentCount > 0 || keywords.Any(k => text.Contains(k)))
                    covered.Add(dim);
                else
                    missing.Add(dim);
            }
            else if (keywords.Count(k => text.Contains(k)) >= 2)
            {
                covered.Add(dim);
            }
            else
            {
                missing.Add(dim);
            }
        }

        return new DimensionDetection { Covered = covered, Missing = missing };
    }

    private int CountEntities(string text)
    {
        return EntityKeywords.Count(k => text.Contains(k));
    }

    // ═══════════════════════════════════════════════════
    // 追问刹车
    // ═══════════════════════════════════════════════════

    public bool IsForceRefine(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return ForceRefineKeywords.Any(k =>
            text.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsMaxRoundsReached(int roundCount, int maxRounds = 20)
    {
        return roundCount >= maxRounds;
    }

    // ═══════════════════════════════════════════════════
    // LLM 成熟度评估
    // ═══════════════════════════════════════════════════

    public async Task<MaturityResult> EvaluateMaturity(
        List<ChatMessage> history, string provider, CancellationToken ct = default)
    {
        var systemPrompt = """
            你是企业级需求成熟度评估器。根据对话历史判断需求信息的完整度。

            评估维度（总分0-100）：
            1. 业务领域清晰度 (0-30)：是否明确了系统类型/行业？
            2. 核心实体识别 (0-30)：提到了几个业务实体？系统名隐含的也算
            3. 业务流程暗示 (0-20)：是否有流程描述？暗示也算
            4. 约束/规模信息 (0-20)：是否有数量/规则/角色约束？

            mode判定：score < 25 → "explore", score 25-49 → "confirm", score >= 50 → "refine"

            只输出JSON：
            {"score":数字,"mode":"explore|confirm|refine","missing":["缺1"],"strengths":["有1"],"nextQuestion":"下一个该问的问题"}
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
                已识别：{string.Join("、", maturity.Strengths)}
                缺失：{string.Join("、", maturity.Missing)}
                任务：1. 生成合理业务假设清单(□前缀) 2. 一次最多追问3个问题(❓前缀) 3. 提醒补充完可说"开始分析"
                用中文，Markdown格式。
                """,

            "confirm" => $"""
                你是AI架构顾问。用户已提供部分信息，需确认和补充。
                已确认：{string.Join("、", maturity.Strengths)}
                待确认：{string.Join("、", maturity.Missing)}
                任务：1. 整理已确认信息(✅标记) 2. 针对待确认点追问(❓标记) 3. 提醒确认后可说"开始分析"
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
    // 多模态图片提取（真实现：直接调 Vision API）
    //
    // 修复 #4：StringContent 加 using
    // 修复 #8：认证改为 per-request HttpRequestMessage
    // 修复 #1：包含 model 参数（清言版本漏了，编译错误）
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

    /// <summary>
    /// 处理单张图片：构建 Vision API 请求 → 发送 → 解析响应
    /// API 格式假设：OpenAI-compatible multimodal endpoint
    /// </summary>
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
                        new
                        {
                            type = "text",
                            text = "请从图片中提取所有与软件需求相关的业务信息：界面元素、表格字段、业务流程、系统截图中的数据。用结构化中文列出。"
                        },
                        new
                        {
                            type = "image_url",
                            image_url = new { url = $"data:{mimeType};base64,{base64}" }
                        }
                    }
                }
            },
            max_tokens = 2000
        };

        // 修复 #8：用 HttpRequestMessage 设置 per-request 认证
        // 修复 #4：StringContent 在 using 块中创建，块结束自动释放
        using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var jsonPayload = JsonSerializer.Serialize(payload);
        using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        request.Content = content;

        using var response = await client.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
        {
            var responseStr = await response.Content.ReadAsStringAsync(ct);
            // 修复 #3：using 保护 JsonDocument
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

    // 修复 #3：JsonDocument 必须 using，否则底层缓冲区不释放
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
        catch (JsonException)
        {
            return text;
        }
        catch (ArgumentException)
        {
            return text;
        }
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
// DTO
// ═══════════════════════════════════════════════════

public class HardRuleResult
{
    public bool Passed { get; set; }
    public string Reason { get; set; } = "";
    public string Hint { get; set; } = "";
    public int DimensionCount { get; set; }
    public int EntityCount { get; set; }
    public List<string> CoveredDimensions { get; set; } = new();
    public List<string> MissingDimensions { get; set; } = new();
}

public class MaturityResult
{
    public int Score { get; set; }
    public string Mode { get; set; } = "explore";
    public List<string> Missing { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public string NextQuestion { get; set; } = "";
}

internal class DimensionDetection
{
    public List<string> Covered { get; set; } = new();
    public List<string> Missing { get; set; } = new();
}
