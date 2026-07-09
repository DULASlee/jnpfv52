using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Ir;
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
            你是企业级需求成熟度评估器。根据对话历史判断需求信息的完整度，并产出结构化澄清问题。

            你必须理解任何行业的业务描述，不限于特定行业。

            # 第一步：评分（总分0-100）

            1. 业务领域清晰度 (0-30)：30=明确系统名称+行业+目标；25=提到系统类型；10=模糊；0=无法判断
            2. 核心实体识别 (0-30)：25=5+实体；15=2-4；5=1；0=无（按领域自行识别：进销存→商品/订单、医院→患者/处方）
            3. 业务流程暗示 (0-20)：20=完整流程；15=隐含流程（"采购要审批"=采购流程+审批）；0=无
            4. 约束/规模信息 (0-20)：20=具体约束（并发/数据量/部署）；10=模糊；0=无

            mode 判定（严格按分数）：score<25→"explore"，score 25-79→"confirm"，score≥80→"refine"
            注：中文需求 LLM 普遍给分偏高（领域30+实体25+流程15+约束5≈75），故 refine 阈值从 50 提高到 80，让 50-79 分的"中等完整"需求走 confirm 提问以补充关键细节。

            # 第二步：产出结构化澄清问题（仅 mode=explore 或 confirm）

            当 mode 为 explore 或 confirm 时，必须产出 clarifications 数组——这是让用户通过选项（而非打字）细化需求的选择题。
            当 mode 为 refine 时，clarifications 返回空数组 []。

            ## clarifications 产出规范（严格遵守，违反会导致前端渲染失败）

            - clarifications 数组只含 1 个元素（一轮提问）
            - 该元素的 questions 数量必须 3-5 个，聚焦当前最需要澄清的歧义点（基于 missing 列表）
            - 每个 question 必须含：id（q1/q2/...）、text（问题文本）、type、required、options
            - type 取值：single（单选）、multi（多选）、text（纯文本补充）
            - 每个 question.options 数量必须 3-5 个
            - 每个 question.options 的【最后一个】必须固定为：{"id":"o_other","label":"其他","freeText":true}
            - 其余 option 的 id 用 o1/o2/o3，label 为简洁中文选项文本
            - text 类型的问题，options 只放一个"其他"项
            - required=true 表示关键题（影响后续设计的核心歧义），每轮关键题不超过 2 个

            ## 输出格式（只输出 JSON，不要 markdown 代码块、不要注释、不要多余文本）

            {"score":50,"mode":"confirm","domain":"OA/考勤","entities":["请假单","审批记录"],"missing":["请假类型枚举","审批规则"],"strengths":["有角色"],"nextQuestion":"下一个该问的问题","clarifications":[{"title":"需求澄清","intro":"以下问题影响设计","questions":[{"id":"q1","text":"请假类型有哪些？","type":"multi","required":true,"options":[{"id":"o1","label":"事假"},{"id":"o2","label":"病假"},{"id":"o3","label":"年假"},{"id":"o_other","label":"其他","freeText":true}]}]}]}

            注意：上面是一个完整示例。实际输出时根据对话历史填充真实内容。refine 模式下 clarifications 为 []。
            """;

        var request = new ChatCompletionRequest
        {
            ProviderCode = provider,
            SystemPrompt = systemPrompt,
            Messages = history,
            // 必须容纳完整 clarifications 数组（3-5 题 × 3-5 选项 × 中文 label ≈ 1500-2000 tokens）
            // 原 500 会截断 JSON 导致解析失败或 LLM 干脆不产出 clarifications
            MaxTokens = 2500,
            Temperature = 0.2,
            ResponseFormat = "json",
            MaxRetries = 1,
            TimeoutMs = 45000
        };

        try
        {
            var response = await _llmGateway.ChatAsync(request, ct);
            if (!response.IsSuccess)
            {
                // fail-safe：LLM 故障时降级 confirm（继续追问），不降级 refine（直接分析）。
                // refine 会跳过追问直接进入 SA 深度分析，等于放行不完整需求——与主门控 fail-closed 策略一致。
                _logger.LogWarning("成熟度评估 LLM 调用失败，保守降级 confirm（继续追问，不进分析）: {Error}", response.Error);
                return new MaturityResult { Score = 40, Mode = "confirm" };
            }

            var json = ExtractJson(response.Content);
            var parsed = JsonSerializer.Deserialize<MaturityResult>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed == null)
            {
                _logger.LogWarning("成熟度评估 JSON 解析失败，保守降级 confirm（继续追问，不进分析）");
                return new MaturityResult { Score = 40, Mode = "confirm" };
            }

            // 一致性兜底：mode 必须与 score 匹配（LLM 偶尔给出矛盾值）
            parsed.Mode = NormalizeMode(parsed.Score, parsed.Mode);

            return parsed;
        }
        catch (Exception ex)
        {
            // fail-safe：异常时同样降级 confirm，不降级 refine
            _logger.LogError(ex, "成熟度评估异常，保守降级 confirm（继续追问，不进分析）");
            return new MaturityResult { Score = 40, Mode = "confirm" };
        }
    }

    /// <summary>
    /// mode 与 score 一致性校验。LLM 偶尔给出矛盾值（如 score=80 但 mode=explore），
    /// 按 score 强制归一，避免 mode 误判导致提问逻辑异常。
    /// </summary>
    private static string NormalizeMode(int score, string? rawMode)
    {
        var mode = (rawMode ?? "refine").Trim().ToLowerInvariant();
        if (mode != "explore" && mode != "confirm" && mode != "refine")
            mode = "refine";
        var expected = score < 25 ? "explore" : score < 80 ? "confirm" : "refine";
        // 如果 LLM 给的 mode 与 score 阈值矛盾，以 score 为准（score 是客观维度之和）
        if (mode != expected)
        {
            // 仅在严重矛盾时纠正（如 score=85 却 mode=explore）
            // 轻微偏差（score=78 confirm vs score=80 refine 边界）保留 LLM 判断
            var scoreGap = Math.Abs(score - (expected == "explore" ? 12 : expected == "confirm" ? 52 : 90));
            if (scoreGap > 20) return expected;
        }
        return mode;
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
    // 交互式澄清问答生成（ADR-005）
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// 把成熟度评估中的结构化问题草案，升级为符合完整 Schema 的 ClarificationSet。
    ///
    /// 调用时机：mode ∈ {explore, confirm} 且 maturity.HasStructuredClarifications() 时。
    /// 不变量保证（即使 LLM 输出不规范也强制修正）：
    ///   - 每题 Options 数量裁剪到 [3,5]
    ///   - 每题末项恒为 {"id":"o_other","label":"其他","freeText":true}
    ///   - text 题 Options 强制只保留"其他"项
    ///   - 裁剪到 ≤5 题、关键题(required) ≤2 个
    ///   - LLM 未产出 clarifications → fallback 用 Missing 字符串构造 text 题
    /// </summary>
    public ClarificationSet BuildClarificationSet(MaturityResult maturity, int round)
    {
        // fallback：LLM 没产出结构化问题，用 Missing 字符串拼成 text 题
        if (maturity?.Clarifications is null || maturity.Clarifications.Count == 0)
        {
            return BuildFallbackSet(maturity, round);
        }

        var draft = maturity.Clarifications[0];
        var setId = Guid.NewGuid().ToString("N");
        var questions = new List<ClarificationQuestion>();
        var requiredCount = 0;

        foreach (var dq in (draft.Questions ?? new()).Take(5))
        {
            var qId = EnsureQuestionId(dq.Id, questions.Count);
            var type = NormalizeType(dq.Type);
            var required = dq.Required && requiredCount < 2; // 关键题 ≤2
            if (required) requiredCount++;

            var options = BuildOptions(type, dq.Options);
            if (options.Count < 2) continue; // 选项不足，跳过该题

            questions.Add(new ClarificationQuestion
            {
                Id = qId,
                Text = string.IsNullOrWhiteSpace(dq.Text) ? $"问题 {questions.Count + 1}" : dq.Text,
                Type = type,
                Required = required,
                Options = options,
            });

            if (questions.Count >= 5) break;
        }

        // 一题都没构造成功 → fallback
        if (questions.Count == 0)
            return BuildFallbackSet(maturity, round);

        return new ClarificationSet
        {
            SetId = setId,
            Stage = ClarificationStages.Requirement,
            Round = Math.Clamp(round, 1, 7),
            Title = string.IsNullOrWhiteSpace(draft.Title) ? "需求澄清（第 " + Math.Clamp(round, 1, 7) + " 轮）" : draft.Title,
            Intro = string.IsNullOrWhiteSpace(draft.Intro)
                ? "以下问题影响后续设计与开发，请逐题确认。每题最后一项为「其他」，可自由补充。"
                : draft.Intro,
            Questions = questions,
            AllowSkipNonCritical = true,
        };
    }

    /// <summary>判断成熟度评估是否已带可直接升级的结构化问题。</summary>
    public static bool HasStructuredClarifications(MaturityResult maturity)
        => maturity?.Clarifications is { Count: > 0 };

    private static ClarificationSet BuildFallbackSet(MaturityResult maturity, int round)
    {
        // 用 Missing 字符串构造 text 题（保持与既有 ❓ 追问等价的体验）
        var missing = (maturity?.Missing is { Count: > 0 })
            ? maturity.Missing
            : new List<string> { "请补充任何影响系统设计的业务约束或规则" };

        var questions = new List<ClarificationQuestion>();
        for (var i = 0; i < missing.Count && i < 3; i++)
        {
            questions.Add(new ClarificationQuestion
            {
                Id = $"q{i + 1}",
                Text = missing[i],
                Type = "text",
                Required = i == 0, // 第一题作为关键题
                Options = new List<ClarificationOption>
                {
                    new() { Id = "o_other", Label = "其他", FreeText = true },
                },
            });
        }

        return new ClarificationSet
        {
            SetId = Guid.NewGuid().ToString("N"),
            Stage = ClarificationStages.Requirement,
            Round = Math.Clamp(round, 1, 7),
            Title = "需求澄清（第 " + Math.Clamp(round, 1, 7) + " 轮）",
            Intro = "请补充以下信息以便进行准确的需求分析。每项可选择「其他」自由输入。",
            Questions = questions,
            AllowSkipNonCritical = true,
        };
    }

    private static string EnsureQuestionId(string raw, int index)
    {
        var id = string.IsNullOrWhiteSpace(raw) ? "" : Regex.Replace(raw.Trim(), @"[^a-zA-Z0-9_]", "");
        return string.IsNullOrEmpty(id) ? $"q{index + 1}" : id;
    }

    private static string NormalizeType(string raw)
        => (raw ?? "").Trim().ToLowerInvariant() switch
        {
            "multi" or "multiple" or "checkbox" => "multi",
            "text" or "textarea" or "freeform" => "text",
            _ => "single",
        };

    /// <summary>
    /// 构造选项：single/multi 题取 LLM 产出（裁剪到 3-5 + 补"其他"）；
    /// text 题强制只保留"其他"一项。
    /// </summary>
    private static List<ClarificationOption> BuildOptions(string type, List<ProposedOption> draft)
    {
        if (type == "text")
        {
            return new List<ClarificationOption>
            {
                new() { Id = "o_other", Label = "其他", FreeText = true },
            };
        }

        var opts = new List<ClarificationOption>();
        for (var i = 0; i < (draft?.Count ?? 0) && opts.Count < 4; i++)
        {
            var d = draft![i];
            if (string.IsNullOrWhiteSpace(d.Label)) continue;
            // 跳过 LLM 自己加的"其他"项（下面统一补）
            if (d.FreeText) continue;
            var oid = EnsureOptionId(d.Id, opts.Count);
            opts.Add(new ClarificationOption { Id = oid, Label = d.Label!.Trim(), FreeText = false });
        }

        // 至少要凑够 2 个真实选项（加上"其他"=3，满足下限）
        if (opts.Count < 2)
            return new List<ClarificationOption>();

        // 末项恒为"其他"
        opts.Add(new ClarificationOption { Id = "o_other", Label = "其他", FreeText = true });
        return opts;
    }

    private static string EnsureOptionId(string raw, int index)
    {
        var id = string.IsNullOrWhiteSpace(raw) ? "" : Regex.Replace(raw.Trim(), @"[^a-zA-Z0-9_]", "");
        return string.IsNullOrEmpty(id) ? $"o{index + 1}" : id;
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

    /// <summary>
    /// LLM 直接产出的结构化澄清问题（ADR-005）。
    /// 非空时优先于 Missing/NextQuestion：gate 调用方应据此生成 ClarificationSet。
    /// LLM 未返回时为 null（fallback 到 Missing 字符串）。
    /// </summary>
    public List<ProposedClarification>? Clarifications { get; set; }
}

/// <summary>
/// LLM 在成熟度评估中一并产出的澄清问题草案（RequirementGateService 内部用）。
/// GenerateClarificationSetAsync 会把它升级为完整的 ClarificationSet。
/// </summary>
public class ProposedClarification
{
    public string Title { get; set; } = "";
    public string Intro { get; set; } = "";
    public List<ProposedQuestion> Questions { get; set; } = new();
}

public class ProposedQuestion
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public string Type { get; set; } = "single"; // single | multi | text
    public bool Required { get; set; } = false;
    public List<ProposedOption> Options { get; set; } = new();
}

public class ProposedOption
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public bool FreeText { get; set; } = false;
}
