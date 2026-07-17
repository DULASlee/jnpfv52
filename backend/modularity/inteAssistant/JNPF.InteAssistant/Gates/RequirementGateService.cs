using JNPF.DependencyInjection;
using JNPF.FriendlyException;
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
            - type 取值：single（单选）、multi（多选）；**禁止**用 text 作为主澄清题（用户应点选，不是打长文）
            - 每个 question.options 数量必须 3-5 个
            - 每个 question.options 的【最后一个】必须固定为：{"id":"o_other","label":"其他","freeText":true}
            - 其余 option 的 id 用 o1/o2/o3，label 为简洁中文选项文本
            - required=true 表示关键题（影响后续设计的核心歧义），每轮关键题不超过 2 个
            - 本轮至少 1 道 multi；若识别到 ≥2 个实体/术语，至少 1 道 MATRIX_SINGLE
            - 新增字段（P9 矩阵题交互，2026-07-10）：
              · contextHint：为什么问这个问题（string，可选）
              · defaultOption：合理默认值（option id，可选）
              · questionFormat：SINGLE | MULTI | MATRIX_SINGLE | MATRIX_MULTI（默认 MULTI）
              · matrixSubItems：矩阵题行数组，每元素 {"rowId","rowLabel"}（仅 MATRIX_* 格式需要）
              如果问题是对「多个已识别的实体/领域术语」做同一维度的决策（如"每个实体是否需要审批？"），
              应使用 questionFormat="MATRIX_SINGLE" 并输出 matrixSubItems 行；否则用 MULTI。

            ## 输出格式（只输出 JSON，不要 markdown 代码块、不要注释、不要多余文本）

            {"score":50,"mode":"confirm","domain":"OA/考勤","entities":["请假单","审批记录"],"missing":["请假类型枚举","审批规则"],"strengths":["有角色"],"nextQuestion":"下一个该问的问题","clarifications":[{"title":"需求澄清","intro":"以下问题影响设计","questions":[{"id":"q1","text":"请假类型有哪些？","type":"multi","required":true,"questionFormat":"MULTI","options":[{"id":"o1","label":"事假"},{"id":"o2","label":"病假"},{"id":"o3","label":"年假"},{"id":"o_other","label":"其他","freeText":true}]},{"id":"q2","text":"下列实体本期是否必须支持审批？","type":"single","required":true,"questionFormat":"MATRIX_SINGLE","matrixSubItems":[{"rowId":"r1","rowLabel":"请假单"},{"rowId":"r2","rowLabel":"审批记录"}],"options":[{"id":"o1","label":"必须有"},{"id":"o2","label":"可后期"},{"id":"o3","label":"不需要"},{"id":"o_other","label":"其他","freeText":true}]}]}]}

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
            MaxRetries = 3,
            TimeoutMs = 90_000
        };

        Exception? caught = null;
        try
        {
            var response = await _llmGateway.ChatAsync(request, ct);
            if (!response.IsSuccess)
            {
                // 硬错误：LLM 调用失败即抛，禁止返回伪成功兜底对象
                throw Oops.Bah($"需求门控成熟度评估 LLM 失败: {response.Error ?? "(无错误详情)"} provider={provider}");
            }

            var json = ExtractJson(response.Content);
            MaturityResult? parsed = null;
            try
            {
                parsed = JsonSerializer.Deserialize<MaturityResult>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception jex)
            {
                caught = jex;
            }

            if (parsed == null)
            {
                // 硬错误：JSON 解析失败即抛
                throw Oops.Bah($"需求门控成熟度评估 JSON 解析失败: {caught?.Message ?? "(无错误详情)"} provider={provider}");
            }

            // 一致性兜底：mode 必须与 score 匹配（LLM 偶尔给出矛盾值）
            parsed.Mode = NormalizeMode(parsed.Score, parsed.Mode);

            return parsed;
        }
        catch (Exception ex)
        {
            // 硬错误：异常即抛（保留 Oops.Bah 透传，其余异常包装为业务错误）
            if (ex is JNPF.FriendlyException.AppFriendlyException) throw;
            _logger.LogError(ex, "成熟度评估异常 provider={Provider}", provider);
            throw Oops.Bah($"需求门控成熟度评估 LLM 失败: {ex.Message} provider={provider}");
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
    ///   - 主澄清题禁止纯 text；LLM 未产出 → fallback 为 multi + MATRIX_SINGLE
    ///   - 裁剪到 ≤5 题、关键题(required) ≤2 个
    ///   - 透传 questionFormat / matrixSubItems / contextHint / defaultOption
    /// </summary>
    public ClarificationSet BuildClarificationSet(MaturityResult maturity, int round)
    {
        // CR-20260717-01 §3.1: LLM 没产出结构化问题 = 硬错误（不再 BuildFallbackSet 兜底）
        if (maturity?.Clarifications is null || maturity.Clarifications.Count == 0)
        {
            // #region agent log
            AgentDebugLog("F1", "RequirementGateService.BuildClarificationSet", "error: no clarifications from LLM",
                $"{{\"round\":{round},\"score\":{maturity?.Score ?? 0},\"mode\":{JsonEsc(maturity?.Mode)},\"missingCount\":{maturity?.Missing?.Count ?? 0},\"entityCount\":{maturity?.Entities?.Count ?? 0}}}");
            // #endregion
            throw Oops.Bah(
                $"需求门控 BuildClarificationSet: LLM 未产出结构化澄清问题（Clarifications 为空）" +
                $" round={round} score={maturity?.Score ?? 0} mode={maturity?.Mode ?? "(null)"}");
        }

        var draft = maturity.Clarifications[0];
        var setId = Guid.NewGuid().ToString("N");
        var questions = new List<ClarificationQuestion>();
        var requiredCount = 0;

        foreach (var dq in (draft.Questions ?? new()).Take(5))
        {
            var qId = EnsureQuestionId(dq.Id, questions.Count);
            var format = ClarificationQuestion.NormalizeQuestionFormat(dq.QuestionFormat);
            var isMatrix = format.StartsWith("MATRIX_", StringComparison.Ordinal);
            var type = isMatrix
                ? (format == "MATRIX_MULTI" ? "multi" : "single")
                : NormalizeType(dq.Type);

            // 计划约定：主澄清题不做纯文本；text 草案升为 multi
            if (type == "text")
                type = "multi";

            var required = dq.Required && requiredCount < 2;
            if (required) requiredCount++;

            var options = BuildOptions(type, dq.Options);
            if (options.Count < 2) continue;

            List<MatrixSubItem>? matrixRows = null;
            if (isMatrix)
            {
                matrixRows = (dq.MatrixSubItems ?? new())
                    .Where(r => !string.IsNullOrWhiteSpace(r.RowLabel))
                    .Select((r, i) => new MatrixSubItem
                    {
                        RowId = string.IsNullOrWhiteSpace(r.RowId) ? $"r{i + 1}" : r.RowId.Trim(),
                        RowLabel = r.RowLabel.Trim(),
                    })
                    .Take(8)
                    .ToList();
                if (matrixRows.Count < 2)
                {
                    // 矩阵行不足 → 退回普通多选，避免前端空白矩阵
                    format = "MULTI";
                    isMatrix = false;
                    type = "multi";
                    matrixRows = null;
                }
            }

            questions.Add(new ClarificationQuestion
            {
                Id = qId,
                Text = string.IsNullOrWhiteSpace(dq.Text) ? $"问题 {questions.Count + 1}" : dq.Text,
                Type = type,
                Required = required,
                Options = options,
                ContextHint = string.IsNullOrWhiteSpace(dq.ContextHint) ? null : dq.ContextHint.Trim(),
                DefaultOption = string.IsNullOrWhiteSpace(dq.DefaultOption) ? null : dq.DefaultOption.Trim(),
                QuestionFormat = format,
                MatrixSubItems = matrixRows,
            });

            if (questions.Count >= 5) break;
        }

        if (questions.Count == 0)
        {
            // #region agent log
            AgentDebugLog("F1", "RequirementGateService.BuildClarificationSet", "error: draft questions invalid",
                $"{{\"round\":{round},\"draftQuestionCount\":{(draft.Questions?.Count ?? 0)}}}");
            // #endregion
            throw Oops.Bah(
                $"需求门控 BuildClarificationSet: LLM 产出的澄清问题全部无效（questions.Count == 0 after parse）" +
                $" round={round} draftQuestionCount={draft.Questions?.Count ?? 0}");
        }

        // #region agent log
        AgentDebugLog("F2", "RequirementGateService.BuildClarificationSet", "structured set built",
            $"{{\"round\":{round},\"qCount\":{questions.Count},\"formats\":[{string.Join(",", questions.Select(q => JsonEsc(q.QuestionFormat)))}],\"types\":[{string.Join(",", questions.Select(q => JsonEsc(q.Type)))}]}}");
        // #endregion

        return new ClarificationSet
        {
            SetId = setId,
            Stage = ClarificationStages.Requirement,
            Round = Math.Clamp(round, 1, 7),
            Title = string.IsNullOrWhiteSpace(draft.Title) ? "需求澄清（第 " + Math.Clamp(round, 1, 7) + " 轮）" : draft.Title,
            Intro = string.IsNullOrWhiteSpace(draft.Intro)
                ? "以下问题请通过选项确认（支持多选与矩阵）。每题最后一项为「其他」，可自由补充。"
                : draft.Intro,
            Questions = questions,
            AllowSkipNonCritical = true,
        };
    }

    /// <summary>判断成熟度评估是否已带可直接升级的结构化问题。</summary>
    public static bool HasStructuredClarifications(MaturityResult maturity)
        => maturity?.Clarifications is { Count: > 0 };

    private static List<string> DefaultMatrixRowLabels() => new()
    {
        "核心业务单据",
        "审批流程",
        "权限与角色",
        "消息/告警",
        "报表统计",
    };

    private static string TruncateLabel(string text, int max)
        => text.Length <= max ? text : text[..(max - 1)] + "…";

    private static List<ClarificationOption> OptionsWithOther(params (string Id, string Label)[] items)
    {
        var opts = items
            .Where(x => !string.IsNullOrWhiteSpace(x.Label))
            .Select(x => new ClarificationOption { Id = x.Id, Label = x.Label, FreeText = false })
            .ToList();
        opts.Add(new ClarificationOption { Id = "o_other", Label = "其他", FreeText = true });
        return opts;
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
    /// 若选项不足则返回空列表，由调用方跳过该题。
    /// </summary>
    private static List<ClarificationOption> BuildOptions(string type, List<ProposedOption> draft)
    {
        // text 已在上游升为 multi；此处仍兼容，避免空选项
        if (type == "text")
            type = "multi";

        var opts = new List<ClarificationOption>();
        for (var i = 0; i < (draft?.Count ?? 0) && opts.Count < 4; i++)
        {
            var d = draft![i];
            if (string.IsNullOrWhiteSpace(d.Label)) continue;
            if (d.FreeText) continue;
            var oid = EnsureOptionId(d.Id, opts.Count);
            opts.Add(new ClarificationOption { Id = oid, Label = d.Label!.Trim(), FreeText = false });
        }

        if (opts.Count < 2)
            return new List<ClarificationOption>();

        opts.Add(new ClarificationOption { Id = "o_other", Label = "其他", FreeText = true });
        return opts;
    }

    private static string EnsureOptionId(string raw, int index)
    {
        var id = string.IsNullOrWhiteSpace(raw) ? "" : Regex.Replace(raw.Trim(), @"[^a-zA-Z0-9_]", "");
        return string.IsNullOrEmpty(id) ? $"o{index + 1}" : id;
    }

    // #region agent log
    private static void AgentDebugLog(string hypothesisId, string location, string message, string dataJson)
    {
        try
        {
            var line =
                $"{{\"sessionId\":\"ead5d0\",\"runId\":\"clarify-fix\",\"hypothesisId\":{JsonEsc(hypothesisId)},\"location\":{JsonEsc(location)},\"message\":{JsonEsc(message)},\"data\":{dataJson},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            System.IO.File.AppendAllText(@"D:\JNPF-v52\debug-ead5d0.log", line);
        }
        catch { /* never break gate */ }
    }

    private static string JsonEsc(string? s) =>
        System.Text.Json.JsonSerializer.Serialize(s ?? "");
    // #endregion

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
                // degradation-ok: 图片附件处理异常 — 非 LLM 调用，单张图片失败不阻断需求门控（错误信息回传给用户）
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
    public string? ContextHint { get; set; }
    public string? DefaultOption { get; set; }
    /// <summary>SINGLE | MULTI | MATRIX_SINGLE | MATRIX_MULTI</summary>
    public string? QuestionFormat { get; set; }
    public List<MatrixSubItem>? MatrixSubItems { get; set; }
}

public class ProposedOption
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public bool FreeText { get; set; } = false;
}
