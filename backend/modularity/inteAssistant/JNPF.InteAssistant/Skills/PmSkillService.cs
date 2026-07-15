using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Dto.Skills;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Gates;
using JNPF.InteAssistant.Sa;
using JNPF.InteAssistant.Skills.Cognitive;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 产品经理 Skill（R1 认知模具版）— 真 ToT：TreeSearchAsync 多路并行 + kg.score-candidate 裁决 Top-1。
/// LLM/校验全败 → 抛 Oops.Bah，禁止 fallback 假骨架（施工包 21 R1 / 红线 RL-1）。
/// </summary>
public sealed class PmSkillService : CognitiveSkill, ITransient
{
    private const int TotBranchCount = 3;
    /// <summary>与 LlmCallPolicy["pm-skill"].MaxTokensPerCall 对齐；需覆盖大型 IR-0 JSON。</summary>
    private const int TotMaxTokens = 16384;
    private const int QuestionsPerRound = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ILogger<PmSkillService> _logger;
    private readonly IRequirementEvolutionContext? _evolutionContext;
    private readonly RequirementGateService _gate;
    private readonly IDomainSeedService _seedService;

    public PmSkillService(
        ICognitiveSkillToolkit toolkit,
        ILogger<PmSkillService> logger,
        RequirementGateService gate,
        IDomainSeedService seedService,
        IRequirementEvolutionContext? evolutionContext = null)
        : base(toolkit)
    {
        _logger = logger;
        _gate = gate;
        _seedService = seedService;
        _evolutionContext = evolutionContext;
    }

    public override string SkillId => "pm-skill";
    public override string Version => "2.0.0-cognitive";
    public override SkillLayer Layer => SkillLayer.Decision;
    public override SkillMission Mission => SkillMission.DefineBoundary;

    public override SkillInformationNeeds InformationNeeds { get; } = new()
    {
        IrFragmentTypes = Array.Empty<string>(),
        RequiredStability = IrStabilityStates.Draft,
    };

    public override SkillOutputDeclaration Outputs { get; } = new()
    {
        IrEventTypes = new[] { IrEventTypes.SkeletonCreated },
    };

    public override Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        var existing = snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable);
        if (existing != null)
            return Task.FromResult(SkillValidationResult.Fail("IR-0 骨架已 stable，请先修订或新建项目"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    public override Task<SkillValidationResult> ValidateOutputAsync(
        IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct = default)
    {
        if (events.Count != 1 || events[0].EventType != IrEventTypes.SkeletonCreated)
            return Task.FromResult(SkillValidationResult.Fail("PM Skill 必须产出 1 条 SkeletonCreated"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    protected override async IAsyncEnumerable<AppendIrEventRequest> ThinkAsync(
        SkillPerception perception,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var context = perception.Context;
        var skeletonId = $"SK-{context.PipelineId}";
        var fragmentId = $"skeleton:{skeletonId}";

        var payload = await GenerateSkeletonViaTotAsync(context, skeletonId, fragmentId, ct);

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.SkeletonCreated,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.Skeleton,
            FragmentVersion = 1,
            Payload = payload,
        };
    }


    // ── CR-20260713-03：PM 核心流程重构 — 4 步线性（回归"完善需求"初衷）──────────
    //
    // 步骤①：EnhanceRequirement — PM 用专家提示词 + 行业种子，把用户"比较完善"
    //         但尚未特别完善的需求，推进到"开发平台能据此生成专业系统"的完善度。
    //         过程中 LLM 若发现关键不确定点 → 产出追问（对话式，0~N 次），由编排器
    //         暂停流程等用户作答后回灌继续完善（AskUserTurnAsync 协作）。
    //
    // 设计哲学（用户初衷）：像找 AI 聊天让它帮你完善开发方案一样 —— PM 是顾问，
    // 不是审问者；问的是"关键不确定点"，不是机械出 3 道结构化选择题。
    //
    // 产物：EnhancedText（完善后的需求文本，写 IR0_Requirement fragment，stable）
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 步骤①：PM 完善需求。用专家提示词 + 行业种子，把用户原始需求推进到
    /// "开发平台能据此生成专业系统"的完善度。LLM 发现关键不确定点时产出追问。
    /// </summary>
    /// <param name="context">Skill 上下文（三元组 + UserRequirement + SeedMatches）。</param>
    /// <param name="previousTurns">历史追问轮次（首轮为空，后续轮次回灌用户答案）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>完善结果：要么产出 EnhancedText（完成），要么产出 PendingQuestion（需追问）。</returns>
    public async Task<RequirementEnhanceResult> EnhanceRequirementAsync(
        SkillContext context,
        IReadOnlyList<PmClarificationTurn>? previousTurns,
        CancellationToken ct)
        => await EnhanceRequirementAsync(context, previousTurns, onToken: null, ct);

    /// <summary>
    /// CR-20260714-01 改动4：真流式版 EnhanceRequirement。
    /// completed 路径：LLM 先流式输出需求正文(markdown)逐 token 推 SSE → 再输出 ===META=== JSON 元数据。
    /// pending_question 路径：需完整 JSON 才能解析 ClarificationSet，仍用 ChatAsync（不流式）。
    /// </summary>
    public async Task<RequirementEnhanceResult> EnhanceRequirementAsync(
        SkillContext context,
        IReadOnlyList<PmClarificationTurn>? previousTurns,
        Func<string, CancellationToken, Task>? onToken,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // ── 1. 召回行业种子（沿用现有骨架路径，避免重复造轮子）──
        var retrievalText = RequirementTextHelper.ForPmPrompt(context);
        var seeds = await RetrieveEvolutionSeedsAsync(
            context.TenantId, context.ProjectId, context.PipelineId, retrievalText, ct);
        var seedPrompt = RequirementEvolutionContext.RenderPromptBlock(seeds);

        // ── 1b. 按需检索领域知识（整体方案）── DKEE 落地
        var domainSeeds = await _seedService.MatchAsync(ExtractSearchKeyword(context), ct);
        var knowledgePrompt = DomainKnowledgeRenderer.Render(domainSeeds);

        // ── 2. 组装历史追问上下文（若有）──
        var turnsText = BuildClarificationTurnsText(previousTurns);

        // ── 3. LLM 完善需求（CR-20260714-01 改动2：一次出题，铁律2）──
        // systemPrompt 融合：专家身份 + 行业知识 + 输出契约
        // pending_question 时直接产出选择题集（MULTI/MATRIX），不再产自然语言问题后二次出题
        var systemPrompt = """
            你是 JNPF 低代码平台的产品经理 Skill，同时是企业通用管理系统专家。
            你的任务：运用你对各业务领域（人事 / 财务 / 进销存 / 生产 / 审批 / CRM 等）的
            浩瀚行业知识，把用户"比较完善但尚未特别完善"的需求，推进到
            "开发平台能据此生成专业系统"的完善度。

            完善原则：
            - 补全用户遗漏的业务规则、角色权限、状态流转、异常路径、边界条件
            - 基于行业惯例补全用户未提及但同类系统必备的功能点
            - 保持用户原有意图，不得擅自改变业务方向
            - 不得编造用户未提及且行业无共识的具体数据（如具体金额、具体流程节点数）

            输出契约（只输出 JSON，两种之一）：

            情况 A — 需求已完善到可拆解程度：
            {
              "status": "completed",
              "enhancedText": "完善后的完整需求文本（markdown）",
              "completenessNotes": ["本次补全了哪些方面（简述）"]
            }

            情况 B — 存在关键不确定点，必须先问用户（一次出题，直接产出选择题）：
            {
              "status": "pending_question",
              "backgroundText": "向用户解释的背景说明（流式输出，让用户理解为什么要问）",
              "partialEnhancement": "目前已完善的部分（供下一轮继续）",
              "questions": [
                {
                  "text": "问题文本",
                  "questionFormat": "MULTI",
                  "contextHint": "为什么问这个问题",
                  "defaultOption": "opt-1",
                  "options": ["选项1", "选项2", "选项3", "其他"]
                }
              ]
            }

            出题规则（严格遵守）：
            - 普通题用 questionFormat: "MULTI"（多选），每题 options 末项必须为"其他"
            - 如果是对多个事件/模块做同一维度的判断，用 questionFormat: "MATRIX_MULTI"，
              并提供 matrixSubItems: [{"rowId":"evt-1","rowLabel":"事件名"}]
            - 矩阵题（MATRIX_*）不要在 options 里放"其他"（矩阵是行×列结构，无"其他"）
            - 只有影响架构或核心流程的关键不确定点才问，最多 3 题
            - 行业惯例能覆盖的细节直接补全，不要问用户
            """ + "\n" + seedPrompt + knowledgePrompt;

        var userPrompt = $"""
            三元组：tenant={context.TenantId}, project={context.ProjectId}, pipeline={context.PipelineId}

            用户原始需求：
            {retrievalText}

            {(string.IsNullOrEmpty(turnsText) ? "" : $"历史追问：\n{turnsText}\n")}

            请完善需求。若有关键不确定点，直接产出选择题（多选/矩阵）问用户。
            """;

        var request = new ChatCompletionRequest
        {
            ProviderCode = Llm.ResolveProvider(SkillId),
            SystemPrompt = systemPrompt,
            Messages = new List<ChatMessage> { new("user", userPrompt) },
            Temperature = 0.3,
            MaxTokens = 4096,
            TimeoutMs = Llm.ResolveTimeoutMs(SkillId),
            ResponseFormat = "json",
        };

        var response = await ChatWithSchemaRetryAsync(request, "requirement-enhance", ct);

        if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
        {
            _logger.LogWarning(
                "pm-skill EnhanceRequirement LLM 失败 tenant={TenantId} project={ProjectId} pipeline={PipelineId}: {Error}",
                context.TenantId, context.ProjectId, context.PipelineId, response.Error);
            // LLM 失败 → 降级：直接用原始需求作为 EnhancedText（不阻断流程，但记录警告）
            return new RequirementEnhanceResult
            {
                Status = "completed",
                EnhancedText = retrievalText,
                CompletenessNotes = new[] { "LLM 完善失败，降级使用原始需求" },
                SeedIds = seeds.Select(s => s.CaseId).ToList(),
                ClarificationTurns = previousTurns?.Count ?? 0,
            };
        }

        var parsed = ParseEnhanceResponse(response.Content, retrievalText, seeds, previousTurns?.Count ?? 0);

        _logger.LogInformation(
            "pm-skill EnhanceRequirement 完成 tenant={TenantId} project={ProjectId} pipeline={PipelineId} status={Status} turns={Turns} seeds={SeedCount}",
            context.TenantId, context.ProjectId, context.PipelineId, parsed.Status, parsed.ClarificationTurns, seeds.Count);

        return parsed;
    }

    /// <summary>
    /// 解析 EnhanceRequirement LLM 响应。容错：JSON 解析失败时降级为原始需求。
    /// </summary>
    private RequirementEnhanceResult ParseEnhanceResponse(
        string content, string fallbackText,
        IReadOnlyList<RequirementEvolutionSeed> seeds, int turnCount)
    {
        try
        {
            using var doc = JsonDocument.Parse(ExtractJson(content));
            var root = doc.RootElement;
            var status = root.TryGetProperty("status", out var s) && s.ValueKind == JsonValueKind.String
                ? s.GetString() ?? "completed"
                : "completed";

            if (status == "pending_question")
            {
                var background = root.TryGetProperty("backgroundText", out var bg) && bg.ValueKind == JsonValueKind.String
                    ? bg.GetString() ?? string.Empty
                    : root.TryGetProperty("partialEnhancement", out var p) && p.ValueKind == JsonValueKind.String
                        ? p.GetString() ?? fallbackText
                        : fallbackText;
                var partial = root.TryGetProperty("partialEnhancement", out var pe) && pe.ValueKind == JsonValueKind.String
                    ? pe.GetString() ?? fallbackText
                    : fallbackText;

                // CR-20260714-01 改动2（铁律2）：一次出题 — 直接解析 questions 数组产 ClarificationSet
                ClarificationSet? clarSet = null;
                if (root.TryGetProperty("questions", out var qArr) && qArr.ValueKind == JsonValueKind.Array)
                {
                    var questions = ParseQuestionsFromLlm(qArr.GetRawText(), round: turnCount + 1);
                    ApplyMatrixFallback(questions, BuildEmptyCompileResult());
                    EnsureEscapeHatch(questions);

                    clarSet = new ClarificationSet
                    {
                        SetId = Guid.NewGuid().ToString("N"),
                        Stage = "requirement",
                        Round = turnCount + 1,
                        Title = "需求确认",
                        Intro = background,
                        AllowSkipNonCritical = questions.Count == 0,
                        Questions = questions,
                    };

                    _logger.LogInformation(
                        "pm-skill EnhanceRequirement 一次出题 questions={Count} turns={Turns}",
                        questions.Count, turnCount);
                }

                return new RequirementEnhanceResult
                {
                    Status = "pending_question",
                    PendingQuestion = background,
                    PartialEnhancement = partial,
                    PendingClarificationSet = clarSet,
                    SeedIds = seeds.Select(s => s.CaseId).ToList(),
                    ClarificationTurns = turnCount,
                };
            }

            // completed
            var enhanced = root.TryGetProperty("enhancedText", out var e) && e.ValueKind == JsonValueKind.String
                ? e.GetString() ?? fallbackText
                : fallbackText;
            var notes = new List<string>();
            if (root.TryGetProperty("completenessNotes", out var n) && n.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in n.EnumerateArray())
                    if (item.ValueKind == JsonValueKind.String)
                        notes.Add(item.GetString() ?? string.Empty);
            }

            return new RequirementEnhanceResult
            {
                Status = "completed",
                EnhancedText = enhanced,
                CompletenessNotes = notes,
                SeedIds = seeds.Select(s => s.CaseId).ToList(),
                ClarificationTurns = turnCount,
            };
        }
        catch (JsonException)
        {
            // JSON 解析失败 → 降级使用原始需求（不阻断流程）
            return new RequirementEnhanceResult
            {
                Status = "completed",
                EnhancedText = fallbackText,
                CompletenessNotes = new[] { "LLM 响应 JSON 解析失败，降级使用原始需求" },
                SeedIds = seeds.Select(s => s.CaseId).ToList(),
                ClarificationTurns = turnCount,
            };
        }
    }

    /// <summary>
    /// 组装历史追问轮次文本（回灌给 LLM 作为上下文）。
    /// </summary>
    private static string BuildClarificationTurnsText(IReadOnlyList<PmClarificationTurn>? turns)
    {
        if (turns is null || turns.Count == 0) return string.Empty;
        var sb = new StringBuilder();
        foreach (var t in turns)
        {
            sb.AppendLine($"- PM 问：{t.Question}");
            if (!string.IsNullOrWhiteSpace(t.UserAnswer))
                sb.AppendLine($"  用户答：{t.UserAnswer}");
        }
        return sb.ToString();
    }


    // ── 步骤②2b：EnhancePspecDecisionTable ──────────────────────────────────
    //
    // SA 九步拆解中，C# 编译器确定性产出 7 步（DomainModel/AggregateDesign/
    // EventCatalog/CommandQuery/DataModel/UISpec/DeliveryChecklist），但第 5 步
    // IntegrationPoints（PSpec 过程规格）和第 6 步 WorkflowSpec（DecisionTable 决策表）
    // 对 simple/medium 事件返回 Empty，对 complex 事件只是模板机械拼接。
    //
    // 本方法是"PM 参与最后两步"的落点：基于完善后的需求文本 + 完整九步数据，
    // 由 PM LLM 产出 PSpec/DecisionTable 的真语义主体内容。
    //
    // 设计决策（CR-20260713-03）：编译器 SaNineViewCompiler 保持零 LLM 契约不变，
    // 真语义增强在 PM Skill 这一层做，不污染编译器的确定性。
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 步骤②2b：PM LLM 产出 PSpec（过程规格）和 DecisionTable（决策表）的真语义。
    /// 编译器保持零 LLM；本方法在编译后对九步数据的后两步做语义增强。
    /// </summary>
    /// <param name="context">Skill 上下文（三元组 + EnhancedText）。</param>
    /// <param name="compileResult">步骤②2a 的 C# 编译结果（7 步确定性 + 2 步占位）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>语义增强后的 compileResult（IntegrationPoints/WorkflowSpec 有真内容）。</returns>
    public async Task<SaNineViewCompileResult> EnhancePspecDecisionTableAsync(
        SkillContext context,
        SaNineViewCompileResult compileResult,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (compileResult.EventResults.Count == 0) return compileResult;

        var enhancedText = context.UserRequirement ?? string.Empty;

        // ── 按需检索规则知识 ── DKEE 落地
        var ruleSeeds = await _seedService.MatchAsync(
            $"{ExtractSearchKeyword(context)} 规则 审批", ct);
        var rulePrompt = DomainKnowledgeRenderer.RenderRules(ruleSeeds);

        var systemPrompt = """
            你是 JNPF 低代码平台的系统分析师。基于完善后的需求文本，为每个业务事件产出：
            1. PSpec（过程规格）的真语义：input（输入）/ output（输出）/ validation（校验规则）/ algorithm（处理算法）/ boundaries（边界条件）/ exceptions（异常路径）
            2. DecisionTable（决策表）的真语义：conditions（条件）/ actions（动作）/ rules（条件-动作映射矩阵）

            要求：
            - PSpec.algorithm 必须是具体的处理步骤描述，不能是"标准业务处理"这种空话
            - DecisionTable.rules 必须覆盖主要业务分支（至少 执行/拒绝 两条）
            - 基于需求文本里的业务规则推导，不要编造需求未提及的规则

            输出 JSON 对象，key 为 eventId，value 为：
            {
              "pspec": {
                "input": ["..."], "output": ["..."], "validation": ["..."],
                "algorithm": "具体处理步骤", "boundaries": ["..."], "exceptions": ["..."]
              },
              "decisionTable": {
                "conditions": [{"name":"...","operator":"eq","value":"..."}],
                "actions": [{"name":"执行"},{"name":"拒绝"}],
                "rules": [{"conditionMask":[true,false],"actionIndex":0}]
              }
            }
            只输出 JSON。
            """ + "\n" + rulePrompt;
        var eventsBrief = string.Join("\n", compileResult.EventResults
            .Select(e => $"- {e.EventId}: {e.EventName}（{e.Complexity}）"));

        var userPrompt = $"""
            三元组：tenant={context.TenantId}, project={context.ProjectId}, pipeline={context.PipelineId}

            完善后的需求文本：
            {(enhancedText.Length > 8000 ? enhancedText[..8000] + "…【截断】" : enhancedText)}

            业务事件清单：
            {eventsBrief}

            请为每个事件产出 PSpec 和 DecisionTable 的真语义。
            """;

        var request = new ChatCompletionRequest
        {
            ProviderCode = Llm.ResolveProvider(SkillId),
            SystemPrompt = systemPrompt,
            Messages = new List<ChatMessage> { new("user", userPrompt) },
            Temperature = 0.2,
            MaxTokens = 4096,
            TimeoutMs = Llm.ResolveTimeoutMs(SkillId),
            ResponseFormat = "json",
        };

        var response = await Llm.ChatAsync(request, ct);
        if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
        {
            _logger.LogWarning(
                "pm-skill EnhancePspecDecisionTable LLM 失败，保留编译器占位（不阻断）tenant={TenantId} pipeline={PipelineId}: {Error}",
                context.TenantId, context.PipelineId, response.Error);
            return compileResult;
        }

        var enhanced = MergePspecDecisionTableIntoEvents(compileResult, response.Content);

        _logger.LogInformation(
            "pm-skill EnhancePspecDecisionTable 完成 tenant={TenantId} pipeline={PipelineId} events={Count}",
            context.TenantId, context.PipelineId, enhanced.EventResults.Count);

        return enhanced;
    }

    /// <summary>
    /// 把 LLM 产出的 PSpec/DecisionTable 真语义合并进 EventResults。
    /// 容错：单事件解析失败时保留编译器占位，不阻断整体。
    /// </summary>
    private static SaNineViewCompileResult MergePspecDecisionTableIntoEvents(
        SaNineViewCompileResult compileResult, string llmContent)
    {
        Dictionary<string, JsonElement> enhancements;
        try
        {
            using var doc = JsonDocument.Parse(ExtractJson(llmContent));
            enhancements = doc.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.Clone());
        }
        catch (JsonException)
        {
            return compileResult; // JSON 解析失败 → 保留编译器占位
        }

        var mergedEvents = new List<SaEventResult>();
        foreach (var evt in compileResult.EventResults)
        {
            if (!enhancements.TryGetValue(evt.EventId, out var enh))
            {
                mergedEvents.Add(evt);
                continue;
            }

            var newSteps = new Dictionary<string, object>(evt.Steps, StringComparer.Ordinal);
            if (enh.TryGetProperty("pspec", out var pspec))
                newSteps[SaStepNames.IntegrationPoints] = pspec.Deserialize<object>() ?? new { };
            if (enh.TryGetProperty("decisionTable", out var dt))
                newSteps[SaStepNames.WorkflowSpec] = dt.Deserialize<object>() ?? new { };

            mergedEvents.Add(new SaEventResult
            {
                EventId = evt.EventId,
                EventName = evt.EventName,
                Complexity = evt.Complexity,
                Steps = newSteps,
            });
        }

        return new SaNineViewCompileResult
        {
            Source = compileResult.Source,
            ProjectSteps = compileResult.ProjectSteps,
            EventResults = mergedEvents,
            CompileDurationMs = compileResult.CompileDurationMs,
            BundleHash = compileResult.BundleHash,
            Assumptions = compileResult.Assumptions,
        };
    }

    // ── 步骤③：RefineFromAnalysis ──────────────────────────────────────────
    //
    // PM 分析九步数据（含步骤②2b 的真语义 PSpec/DT），反向完善需求文本：
    // 发现"事件粒度异常/字段缺失/规则不全/状态流转缺失/异常路径未覆盖"等遗漏，
    // 补进需求文本。若发现新的关键不确定点 → 产出追问。
    //
    // 这就是用户初衷的"分析 SA 九步数据，进一步完善需求"。
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 步骤③：PM 分析九步数据，反向完善需求文本。发现遗漏则补全；发现关键不确定点则追问。
    /// </summary>
    /// <param name="context">Skill 上下文。</param>
    /// <param name="enhancedText">步骤①产出的完善需求文本。</param>
    /// <param name="compileResult">步骤②的九步数据（含 2b 真语义 PSpec/DT）。</param>
    /// <param name="warnings">轻量校验警告（事件粒度/关键词/SIMPLE 占比）。</param>
    /// <param name="previousTurns">历史追问轮次（步骤③期间的追问）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>二次完善结果：completed（RefinedText）或 pending_question（需追问）。</returns>
    public async Task<RequirementEnhanceResult> RefineFromAnalysisAsync(
        SkillContext context,
        string enhancedText,
        SaNineViewCompileResult compileResult,
        IReadOnlyList<string> warnings,
        IReadOnlyList<PmClarificationTurn>? previousTurns,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var turnsText = BuildClarificationTurnsText(previousTurns);

        // 把九步数据序列化成 LLM 可读的分析上下文
        var analysis = BuildNineViewAnalysisBrief(compileResult, warnings);

        var systemPrompt = """
            你是 JNPF 低代码平台的产品经理 Skill。基于系统分析（SA 九步）的结果数据，
            反向完善需求文本：发现遗漏的业务规则、字段、状态流转、异常路径、边界条件，
            补进需求文本，使其达到"开发平台能据此生成专业系统"的完善度。

            分析重点（基于九步数据）：
            - 事件粒度异常（过粗/过细）→ 建议拆分或合并
            - 字段缺失（实体字段不足以支撑业务事件）→ 补字段
            - 规则不全（PSpec.validation 过于空泛）→ 补具体校验规则
            - 状态流转缺失 → 补状态机
            - 异常路径未覆盖（PSpec.exceptions 为空）→ 补异常处理
            - 关键词缺失（轻量校验警告）→ 补对应业务概念
            - 假设置信度低（Assumptions.confidence < 0.5）→ 重点确认这些假设

            输出契约（只输出 JSON，两种之一）：

            情况 A — 已发现遗漏并补全：
            {
              "status": "completed",
              "refinedText": "二次完善后的完整需求文本（markdown）",
              "gaps": ["本次发现并补全的遗漏（简述）"]
            }

            情况 B — 存在关键不确定点，必须先问用户（一次出题，直接产出选择题）：
            {
              "status": "pending_question",
              "backgroundText": "向用户解释的背景说明",
              "partialEnhancement": "目前已完善的部分",
              "questions": [
                {
                  "text": "问题文本",
                  "questionFormat": "MULTI",
                  "contextHint": "为什么问",
                  "defaultOption": "opt-1",
                  "options": ["选项1", "选项2", "选项3", "其他"]
                }
              ]
            }

            出题规则：普通题用 MULTI（末项必为"其他"）；多维度判断用 MATRIX_MULTI（带 matrixSubItems，无"其他"）。最多 3 题。
            判断标准：只有当某个遗漏无法用行业惯例补全、且会显著影响系统架构时，才问用户。
            """;

        var userPrompt = $"""
            三元组：tenant={context.TenantId}, project={context.ProjectId}, pipeline={context.PipelineId}

            步骤①完善后的需求文本：
            {(enhancedText.Length > 8000 ? enhancedText[..8000] + "…【截断】" : enhancedText)}

            SA 九步分析数据：
            {analysis}

            {(string.IsNullOrEmpty(turnsText) ? "" : $"历史追问：\n{turnsText}\n")}

            请基于九步数据反向完善需求。若有关键不确定点，直接产出选择题问用户。
            """;

        var request = new ChatCompletionRequest
        {
            ProviderCode = Llm.ResolveProvider(SkillId),
            SystemPrompt = systemPrompt,
            Messages = new List<ChatMessage> { new("user", userPrompt) },
            Temperature = 0.3,
            MaxTokens = 4096,
            TimeoutMs = Llm.ResolveTimeoutMs(SkillId),
            ResponseFormat = "json",
        };

        var response = await ChatWithSchemaRetryAsync(request, "requirement-refine", ct);

        if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
        {
            _logger.LogWarning(
                "pm-skill RefineFromAnalysis LLM 失败，保留步骤①文本 tenant={TenantId} pipeline={PipelineId}: {Error}",
                context.TenantId, context.PipelineId, response.Error);
            return new RequirementEnhanceResult
            {
                Status = "completed",
                EnhancedText = enhancedText,
                CompletenessNotes = new[] { "步骤③ LLM 失败，降级使用步骤①文本" },
                ClarificationTurns = previousTurns?.Count ?? 0,
            };
        }

        var parsed = ParseRefineResponse(response.Content, enhancedText, previousTurns?.Count ?? 0);

        _logger.LogInformation(
            "pm-skill RefineFromAnalysis 完成 tenant={TenantId} pipeline={PipelineId} status={Status} gaps={Gaps}",
            context.TenantId, context.PipelineId, parsed.Status,
            parsed.CompletenessNotes.Count);

        return parsed;
    }

    /// <summary>
    /// CR-20260714-01 改动4：真流式完善需求 — completed 路径专用。
    /// LLM 直接输出 markdown 正文（非 JSON），逐 token 通过 onToken 推 SSE → 用户看到打字效果。
    /// 完成后返回 EnhancedText（=完整 markdown），CompletenessNotes 省略（流式模式不解析 JSON 元数据）。
    /// pending_question 场景不适用此方法（需完整 JSON 解析 ClarificationSet）。
    /// </summary>
    public async Task<RequirementEnhanceResult> EnhanceRequirementStreamAsync(
        SkillContext context,
        IReadOnlyList<PmClarificationTurn>? previousTurns,
        Func<string, CancellationToken, Task> onToken,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var retrievalText = RequirementTextHelper.ForPmPrompt(context);
        var seeds = await RetrieveEvolutionSeedsAsync(
            context.TenantId, context.ProjectId, context.PipelineId, retrievalText, ct);
        var seedPrompt = RequirementEvolutionContext.RenderPromptBlock(seeds);
        var turnsText = BuildClarificationTurnsText(previousTurns);

        // 流式专用 systemPrompt：直接输出 markdown 正文（非 JSON），让 token 逐个推送
        var systemPrompt = """
            你是 JNPF 低代码平台的产品经理 Skill，同时是企业通用管理系统专家。
            你的任务：运用行业知识，把用户需求推进到"开发平台能据此生成专业系统"的完善度。

            完善原则：
            - 补全用户遗漏的业务规则、角色权限、状态流转、异常路径、边界条件
            - 基于行业惯例补全用户未提及但同类系统必备的功能点
            - 保持用户原有意图，不得擅自改变业务方向

            直接输出完善后的需求文本（markdown 格式），不要输出 JSON，不要输出任何包裹标记。
            """ + "\n" + seedPrompt;

        var userPrompt = $"""
            三元组：tenant={context.TenantId}, project={context.ProjectId}, pipeline={context.PipelineId}

            用户原始需求：
            {retrievalText}

            {(string.IsNullOrEmpty(turnsText) ? "" : $"历史追问：\n{turnsText}\n")}

            请直接输出完善后的需求文本（markdown）。
            """;

        var request = new ChatCompletionRequest
        {
            ProviderCode = Llm.ResolveProvider(SkillId),
            SystemPrompt = systemPrompt,
            Messages = new List<ChatMessage> { new("user", userPrompt) },
            Temperature = 0.3,
            MaxTokens = 4096,
            TimeoutMs = Llm.ResolveTimeoutMs(SkillId),
            // 不设 ResponseFormat=json — 流式输出纯 markdown
        };

        var fullResponse = new StringBuilder();
        var chunkCount = 0;

        await foreach (var json in Llm.ChatStreamAsync(request, ct))
        {
            if (json.StartsWith("[ERROR]") || json.StartsWith("[error]"))
            {
                _logger.LogWarning("pm-skill EnhanceStream LLM 流式错误: {Error}", json);
                break;
            }

            var token = ExtractToken(json);
            if (string.IsNullOrEmpty(token)) continue;

            chunkCount++;
            fullResponse.Append(token);
            await onToken(token, ct);  // 逐 token 推 SSE
        }

        _logger.LogInformation(
            "pm-skill EnhanceRequirementStream 完成 chunks={Chunks} len={Len} tenant={TenantId} pipeline={PipelineId}",
            chunkCount, fullResponse.Length, context.TenantId, context.PipelineId);

        if (fullResponse.Length == 0)
        {
            // 流式失败 → 降级使用原始需求
            await onToken(retrievalText, ct);
            return new RequirementEnhanceResult
            {
                Status = "completed",
                EnhancedText = retrievalText,
                CompletenessNotes = new[] { "LLM 流式完善失败，降级使用原始需求" },
                SeedIds = seeds.Select(s => s.CaseId).ToList(),
                ClarificationTurns = previousTurns?.Count ?? 0,
            };
        }

        return new RequirementEnhanceResult
        {
            Status = "completed",
            EnhancedText = fullResponse.ToString(),
            CompletenessNotes = Array.Empty<string>(),
            SeedIds = seeds.Select(s => s.CaseId).ToList(),
            ClarificationTurns = previousTurns?.Count ?? 0,
        };
    }

    /// <summary>
    /// CR-20260714-01 改动4：从 SSE JSON 行提取 token（复用 AIDevelopmentPipelineService.ExtractToken 逻辑）。
    /// </summary>
    private static string? ExtractToken(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("text", out var text))
                return text.GetString();

            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var choice = choices[0];
                if (choice.TryGetProperty("delta", out var delta2) &&
                    delta2.TryGetProperty("content", out var content))
                    return content.GetString();
            }

            return null;
        }
        catch { return null; }
    }

    /// <summary>
    /// 解析 RefineFromAnalysis LLM 响应（复用 RequirementEnhanceResult 结构）。
    /// </summary>
    private static RequirementEnhanceResult ParseRefineResponse(
        string content, string fallbackText, int turnCount)
    {
        try
        {
            using var doc = JsonDocument.Parse(ExtractJson(content));
            var root = doc.RootElement;
            var status = root.TryGetProperty("status", out var s) && s.ValueKind == JsonValueKind.String
                ? s.GetString() ?? "completed"
                : "completed";

            if (status == "pending_question")
            {
                var background = root.TryGetProperty("backgroundText", out var bg) && bg.ValueKind == JsonValueKind.String
                    ? bg.GetString() ?? string.Empty : string.Empty;
                var partial = root.TryGetProperty("partialEnhancement", out var p) && p.ValueKind == JsonValueKind.String
                    ? p.GetString() ?? fallbackText : fallbackText;

                // CR-20260714-01 改动2（铁律2）：一次出题 — 直接解析 questions 产 ClarificationSet
                ClarificationSet? clarSet = null;
                if (root.TryGetProperty("questions", out var qArr) && qArr.ValueKind == JsonValueKind.Array)
                {
                    var questions = ParseQuestionsFromLlm(qArr.GetRawText(), round: turnCount + 1);
                    ApplyMatrixFallback(questions, BuildEmptyCompileResult());
                    EnsureEscapeHatch(questions);

                    clarSet = new ClarificationSet
                    {
                        SetId = Guid.NewGuid().ToString("N"),
                        Stage = "requirement",
                        Round = turnCount + 1,
                        Title = "需求深度确认",
                        Intro = background,
                        AllowSkipNonCritical = questions.Count == 0,
                        Questions = questions,
                    };
                }

                return new RequirementEnhanceResult
                {
                    Status = "pending_question",
                    PendingQuestion = background,
                    PartialEnhancement = partial,
                    PendingClarificationSet = clarSet,
                    ClarificationTurns = turnCount,
                };
            }

            var refined = root.TryGetProperty("refinedText", out var e) && e.ValueKind == JsonValueKind.String
                ? e.GetString() ?? fallbackText : fallbackText;
            var gaps = new List<string>();
            if (root.TryGetProperty("gaps", out var n) && n.ValueKind == JsonValueKind.Array)
                foreach (var item in n.EnumerateArray())
                    if (item.ValueKind == JsonValueKind.String)
                        gaps.Add(item.GetString() ?? string.Empty);

            return new RequirementEnhanceResult
            {
                Status = "completed",
                EnhancedText = refined,
                CompletenessNotes = gaps,
                ClarificationTurns = turnCount,
            };
        }
        catch (JsonException)
        {
            return new RequirementEnhanceResult
            {
                Status = "completed",
                EnhancedText = fallbackText,
                CompletenessNotes = new[] { "步骤③响应 JSON 解析失败，降级使用步骤①文本" },
                ClarificationTurns = turnCount,
            };
        }
    }

    /// <summary>
    /// 把九步数据序列化成 LLM 可读的分析摘要（步骤③的输入）。
    /// 包含：事件清单 + 复杂度 + 假设项 + 轻量校验警告。
    /// </summary>
    private static string BuildNineViewAnalysisBrief(
        SaNineViewCompileResult compileResult, IReadOnlyList<string> warnings)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"事件总数：{compileResult.EventResults.Count}");
        sb.AppendLine("事件清单：");
        foreach (var e in compileResult.EventResults)
            sb.AppendLine($"  - {e.EventId}: {e.EventName}（{e.Complexity}）");

        if (compileResult.Assumptions.Count > 0)
        {
            sb.AppendLine($"假设项（{compileResult.Assumptions.Count}，低置信度需重点确认）：");
            foreach (var a in compileResult.Assumptions)
                sb.AppendLine($"  - [{a.Confidence:P0}] {a.EventId}/{a.SourceStep}: {a.Text}");
        }

        if (warnings.Count > 0)
        {
            sb.AppendLine("轻量校验警告：");
            foreach (var w in warnings)
                sb.AppendLine($"  - {w}");
        }

        return sb.ToString();
    }

    public async Task<ClarificationSet> GenerateClarificationAsync(
        int round,
        string stage,
        string tenantId,
        string projectId,
        long pipelineId,
        SaNineViewCompileResult compileResult,
        IReadOnlyList<string> warnings,
        string? previousAnswersText,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // ── CR-01：成熟度评估（在 LLM 出题前，PM 自主判断是否还需要提问）──
        var chatHistory = BuildMaturityChatHistory(
            compileResult, previousAnswersText, tenantId, projectId, pipelineId);
        var maturityProvider = Llm.ResolveProvider("maturity");
        var maturity = await _gate.EvaluateMaturity(chatHistory, maturityProvider, ct);

        _logger.LogInformation(
            "pm-skill 第 {Round} 轮成熟度评估 score={Score} mode={Mode} | pipeline={PipelineId}",
            round, maturity.Score, maturity.Mode, pipelineId);

        // 需求已充分完整 → 跳过提问，返回空题集
        if (maturity.Mode == "refine")
        {
            return BuildRefineEmptySet(stage, round, maturity);
        }

        var (title, intro, systemPrompt) = BuildRoundPrompt(round, compileResult, warnings, previousAnswersText);
        var selectedSlots = SlotInformationGainSelector.SelectTopSlots(compileResult, previousAnswersText, QuestionsPerRound);
        var retrievalText = BuildRoundUserPrompt(round, compileResult, warnings, previousAnswersText)
                            + "\n" + string.Join("\n", selectedSlots.Select(s => $"{s.SlotId}:{s.Title} {s.Description}"));
        var seeds = await RetrieveEvolutionSeedsAsync(tenantId, projectId, pipelineId, retrievalText, ct);
        var seedPrompt = RequirementEvolutionContext.RenderPromptBlock(seeds);
        var slotsPrompt = selectedSlots.Count > 0
            ? "\n优先覆盖的信息增益槽位：\n" + string.Join("\n", selectedSlots.Select(s => $"- {s.SlotId}: {s.Title} — {s.Description}"))
            : string.Empty;
        var request = new ChatCompletionRequest
        {
            ProviderCode = Llm.ResolveProvider(SkillId),
            SystemPrompt = systemPrompt + "\n" + seedPrompt + slotsPrompt,
            Messages = new List<ChatMessage>
            {
                new("user", BuildRoundUserPrompt(round, compileResult, warnings, previousAnswersText)),
            },
            Temperature = 0.3,
            MaxTokens = 2048,
            TimeoutMs = Llm.ResolveTimeoutMs(SkillId),
            ResponseFormat = "json",
        };

        var response = await ChatWithSchemaRetryAsync(request, "clarification", ct);
        if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
        {
            _logger.LogWarning("pm-skill 第 {Round} 轮出题 LLM 失败，降级为空题集: {Error}",
                round, response.Error);
            return BuildEmptyClarificationSet(stage, round, title, intro);
        }

        var questions = ParseQuestionsFromLlm(response.Content, round);
        ApplyMatrixFallback(questions, compileResult);
        EnsureEscapeHatch(questions);
        StampSlotHints(questions, selectedSlots);

        // ── CR-02：PM 自主判断是否允许跳过非关键题 ──
        // explore/confirm 模式有题时：不允许跳过（PM 出的每道题都有信息价值）
        // refine 模式或 LLM 失败降级（0 题）：允许跳过（无实际题目）
        bool allowSkip = questions.Count == 0;

        var set = new ClarificationSet
        {
            SetId = Guid.NewGuid().ToString("N"),
            Stage = stage,
            Round = round,
            Title = title,
            Intro = intro,
            AllowSkipNonCritical = allowSkip,
            Questions = questions,
            TargetSlotIds = selectedSlots.Select(s => s.SlotId).ToList(),
        };

        _logger.LogInformation(
            "pm-skill 第 {Round} 轮出题完成 stage={Stage} mode={Mode} questions={Count} allowSkip={AllowSkip} | pipeline={PipelineId}",
            round, stage, maturity.Mode, questions.Count, allowSkip, pipelineId);
        return set;
    }

    public async Task<PmSpecReviewResult> ReviewSpecAsync(
        SkillContext context,
        string requirementSpecMarkdown,
        CancellationToken ct,
        SaNineViewCompileResult? compileResult = null)
    {
        if (string.IsNullOrWhiteSpace(requirementSpecMarkdown))
        {
            return new PmSpecReviewResult
            {
                Score = 0,
                Verdict = "fail",
                Gaps = new List<string> { "需求分析说明书为空，无法评审" },
                GapDetails = new List<PmSpecReviewGap>
                {
                    new() { Source = "graph", Message = "需求分析说明书为空，无法评审" },
                },
            };
        }

        var graphGaps = compileResult != null
            ? RequirementConflictGraph.FindGaps(compileResult)
            : Array.Empty<RequirementGraphGap>();
        var seeds = await RetrieveEvolutionSeedsAsync(
            context.TenantId, context.ProjectId, context.PipelineId, requirementSpecMarkdown, ct);
        var seedPrompt = RequirementEvolutionContext.RenderPromptBlock(seeds);
        var graphChecklist = graphGaps.Count > 0
            ? "\n确定性冲突图已发现 gaps，请合并到输出 gaps：\n"
              + string.Join("\n", graphGaps.Select(g => $"- source=graph; {g.Code}: {g.Message}"))
            : string.Empty;

        var request = new ChatCompletionRequest
        {
            ProviderCode = Llm.ResolveProvider(SkillId),
            SystemPrompt = """
                你是 JNPF 低代码平台的 PM 需求专家。请审查 02 需求分析说明书是否足以交给开发。
                只输出 JSON：{"score":0-100,"verdict":"pass|fail","gaps":["缺口1"]}。
                评分标准：业务范围清晰、功能流程完整、实体/表可支撑、边界/异常明确、可验收。
                请软性检查是否包含「非目标 / Out of Scope」「失败与补偿」「验收要点」等可交付章节；缺失时写入 gaps，但不要因模板占位本身直接判为失败。
                若存在确定性冲突图 gaps，必须保留；LLM 自己发现的 gaps 也可补充。
                """ + "\n" + seedPrompt + graphChecklist,
            Messages = new List<ChatMessage>
            {
                new("user", $"""
                    三元组：tenant={context.TenantId}, project={context.ProjectId}, pipeline={context.PipelineId}

                    需求分析说明书：
                    {requirementSpecMarkdown}
                    """),
            },
            Temperature = 0.1,
            MaxTokens = 1024,
            TimeoutMs = Llm.ResolveTimeoutMs(SkillId),
            ResponseFormat = "json",
        };

        try
        {
            var response = await ChatWithSchemaRetryAsync(request, "review", ct);
            if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
            {
                _logger.LogWarning("pm-skill ReviewSpec LLM 失败: {Error}", response.Error);
                return new PmSpecReviewResult
                {
                    Score = 0,
                    Verdict = "fail",
                    Gaps = new List<string> { response.Error ?? "PM 终评 LLM 返回空" },
                    GapDetails = new List<PmSpecReviewGap>
                    {
                        new() { Source = "llm", Message = response.Error ?? "PM 终评 LLM 返回空" },
                    },
                };
            }

            var parsed = ParseSpecReviewResult(response.Content);
            var merged = MergeReviewGaps(parsed, graphGaps);
            var cappedScore = RequirementConfidencePolicy.ApplyPmScoreCap(merged.Score, compileResult);
            if (cappedScore != merged.Score)
            {
                var capGap = "存在 confidence<0.5 的核心假设，PM 分数封顶 84";
                merged = new PmSpecReviewResult
                {
                    Score = cappedScore,
                    Verdict = "fail",
                    Gaps = merged.Gaps.Concat(new[] { capGap }).Distinct(StringComparer.Ordinal).ToList(),
                    GapDetails = merged.GapDetails.Concat(new[]
                    {
                        new PmSpecReviewGap { Source = "graph", Message = capGap },
                    }).ToList(),
                };
            }

            _logger.LogInformation(
                "pm-skill ReviewSpec 完成 tenant={TenantId} project={ProjectId} pipeline={PipelineId} score={Score} graphGaps={GraphGapCount} seeds={SeedIds}",
                context.TenantId, context.ProjectId, context.PipelineId, merged.Score, graphGaps.Count,
                string.Join(",", seeds.Select(s => s.CaseId)));
            return merged;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger.LogWarning(ex, "pm-skill ReviewSpec JSON 解析失败");
            return new PmSpecReviewResult
            {
                Score = 0,
                Verdict = "fail",
                Gaps = new List<string> { "PM 终评 JSON 解析失败" },
                GapDetails = new List<PmSpecReviewGap>
                {
                    new() { Source = "llm", Message = "PM 终评 JSON 解析失败" },
                },
            };
        }
    }

    public async Task<PmAmendProposeResult> AmendProposeAsync(
        SkillContext context,
        string userMessage,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            throw Oops.Bah("补充需求不能为空");

        var seeds = await RetrieveEvolutionSeedsAsync(context.TenantId, context.ProjectId, context.PipelineId, userMessage, ct);
        var seedPrompt = RequirementEvolutionContext.RenderPromptBlock(seeds);
        var request = new ChatCompletionRequest
        {
            ProviderCode = Llm.ResolveProvider(SkillId),
            SystemPrompt = """
                你是 PM 需求专家。用户在确认 02 前输入了硬需求，请只做理解回显，不改业务骨架。
                输出 JSON：{"features":[],"flows":[],"entitiesOrTables":[],"summaryMarkdown":"","severity":"patch|enhance","patches":[]}。
                features 必须列功能，flows 必须列流程变化，entitiesOrTables 必须列实体或表影响。
                patches 可选；若能确定性表达变更，输出操作对象：
                {"operation":"AddEntity|AddEvent|PatchRule|AddField|PatchSummary|AddStateTransition","target":"","name":"","displayName":"","type":"","description":"","required":false,"references":"","scopeEventId":"","from":"","to":""}
                禁止在 patches 中表达不确定猜测；不确定就留空 patches。
                """ + "\n" + seedPrompt,
            Messages = new List<ChatMessage>
            {
                new("user", $"""
                    三元组：tenant={context.TenantId}, project={context.ProjectId}, pipeline={context.PipelineId}

                    用户补充需求：
                    {userMessage}
                    """),
            },
            Temperature = 0.2,
            MaxTokens = 1536,
            TimeoutMs = Llm.ResolveTimeoutMs(SkillId),
            ResponseFormat = "json",
        };

        AmendmentUnderstanding understanding;
        var response = await ChatWithSchemaRetryAsync(request, "amend", ct);
        if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
        {
            _logger.LogWarning("pm-skill AmendPropose LLM 失败，使用原文回显: {Error}", response.Error);
            understanding = BuildFallbackUnderstanding(userMessage);
        }
        else
        {
            understanding = ParseAmendmentUnderstanding(response.Content, userMessage);
        }

        _logger.LogInformation(
            "pm-skill AmendPropose 完成 tenant={TenantId} project={ProjectId} pipeline={PipelineId} patches={PatchCount} seeds={SeedIds}",
            context.TenantId, context.ProjectId, context.PipelineId, understanding.Patches.Count,
            string.Join(",", seeds.Select(s => s.CaseId)));

        return new PmAmendProposeResult
        {
            ProposalId = Guid.NewGuid().ToString("N"),
            Understanding = understanding,
        };
    }

    public Task<string> ApplyAmendmentAsync(
        SkillContext context,
        AmendmentUnderstanding understanding,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var delta = new StringBuilder();
        delta.AppendLine("【用户确认的补充需求】");
        if (!string.IsNullOrWhiteSpace(understanding.SummaryMarkdown))
            delta.AppendLine(understanding.SummaryMarkdown.Trim());
        if (understanding.Features.Count > 0)
            delta.AppendLine("功能：" + string.Join("、", understanding.Features));
        if (understanding.Flows.Count > 0)
            delta.AppendLine("流程：" + string.Join("、", understanding.Flows));
        if (understanding.EntitiesOrTables.Count > 0)
            delta.AppendLine("实体/表：" + string.Join("、", understanding.EntitiesOrTables));
        if (understanding.Patches.Count > 0)
        {
            delta.AppendLine("类型化补丁：");
            foreach (var patch in understanding.Patches)
                delta.AppendLine($"- {patch.Operation}: {patch.Target}/{patch.Name}");
        }

        _logger.LogInformation(
            "pm-skill ApplyAmendment 生成 delta tenant={TenantId} project={ProjectId} pipeline={PipelineId} severity={Severity} patches={PatchCount}",
            context.TenantId, context.ProjectId, context.PipelineId, understanding.Severity, understanding.Patches.Count);
        return Task.FromResult(delta.ToString());
    }

    /// <summary>
    /// 三轮澄清作答后：PM 作为完善主体，把用户答案落实为 Typed patches 写回骨架。
    /// 确定性槽位补丁为基线；LLM 失败时仍返回基线，不阻断流程。
    /// </summary>
    public async Task<IReadOnlyList<AmendmentPatch>> RefineSkeletonFromClarificationAsync(
        SkillContext context,
        string skeletonJson,
        string answersText,
        IReadOnlyList<string> filledSlotIds,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        string? existingSummary = null;
        try
        {
            using var doc = JsonDocument.Parse(skeletonJson);
            if (doc.RootElement.TryGetProperty("requirementSummary", out var sum)
                && sum.ValueKind == JsonValueKind.String)
                existingSummary = sum.GetString();
        }
        catch (JsonException) { /* ignore */ }

        var baseline = ClarificationAnswerPatchMapper.BuildPatches(answersText, filledSlotIds, existingSummary);
        if (string.IsNullOrWhiteSpace(answersText) && baseline.Count == 0)
            return Array.Empty<AmendmentPatch>();

        var seeds = await RetrieveEvolutionSeedsAsync(
            context.TenantId, context.ProjectId, context.PipelineId, answersText, ct);
        var seedPrompt = RequirementEvolutionContext.RenderPromptBlock(seeds);
        var skeletonBrief = skeletonJson.Length > 3500 ? skeletonJson[..3500] : skeletonJson;
        var request = new ChatCompletionRequest
        {
            ProviderCode = Llm.ResolveProvider(SkillId),
            SystemPrompt = """
                你是 JNPF 产品经理。用户已回答需求分析澄清题，请把答案落实进需求骨架（完善，不是再出题）。
                只输出 JSON：{"patches":[...],"summaryMarkdown":""}。
                patches 使用：{"operation":"AddEntity|AddEvent|PatchRule|AddField|PatchSummary|AddStateTransition","target":"","name":"","displayName":"","type":"","description":"","required":false,"references":"","scopeEventId":"","from":"","to":""}
                规则：
                - 根据用户答案补字段、规则、状态流转、事件说明；不确定则不要猜造实体。
                - 必须把用户确认点写入 PatchSummary 或 PatchRule 的 description。
                - 禁止输出与答案无关的大幅重写。
                """ + "\n" + seedPrompt,
            Messages = new List<ChatMessage>
            {
                new("user", $"""
                    三元组：tenant={context.TenantId}, project={context.ProjectId}, pipeline={context.PipelineId}
                    已填槽位：{string.Join(",", filledSlotIds)}

                    用户澄清答案：
                    {answersText}

                    当前骨架 JSON（节选）：
                    {skeletonBrief}
                    """),
            },
            Temperature = 0.2,
            MaxTokens = 2048,
            TimeoutMs = Llm.ResolveTimeoutMs(SkillId),
            ResponseFormat = "json",
        };

        IReadOnlyList<AmendmentPatch> llmPatches = Array.Empty<AmendmentPatch>();
        try
        {
            var response = await ChatWithSchemaRetryAsync(request, "amend", ct);
            if (response.IsSuccess && !string.IsNullOrWhiteSpace(response.Content))
            {
                using var doc = JsonDocument.Parse(ExtractJson(response.Content));
                llmPatches = AmendmentPatchApplier.ParsePatches(doc.RootElement);
            }
            else
            {
                _logger.LogWarning("pm-skill RefineSkeleton LLM 失败，仅用确定性补丁: {Error}", response.Error);
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger.LogWarning(ex, "pm-skill RefineSkeleton 解析失败，仅用确定性补丁");
        }

        // LLM 为完善主体：优先保留其补丁，确定性基线补缺口
        var merged = AmendmentPatchApplier.MergePatches(llmPatches, baseline);
        _logger.LogInformation(
            "pm-skill RefineSkeleton 完成 tenant={TenantId} project={ProjectId} pipeline={PipelineId} llmPatches={Llm} baseline={Base} merged={Merged} seeds={SeedIds}",
            context.TenantId, context.ProjectId, context.PipelineId,
            llmPatches.Count, baseline.Count, merged.Count,
            string.Join(",", seeds.Select(s => s.CaseId)));
        return merged;
    }

    public static PmSpecReviewResult ParseSpecReviewResult(string content)
    {
        var json = ExtractJson(content);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var score = root.TryGetProperty("score", out var scoreEl) && scoreEl.TryGetInt32(out var s)
            ? Math.Clamp(s, 0, 100)
            : 0;
        var verdict = root.TryGetProperty("verdict", out var verdictEl) && verdictEl.ValueKind == JsonValueKind.String
            ? verdictEl.GetString() ?? ""
            : "";
        var gaps = new List<string>();
        if (root.TryGetProperty("gaps", out var gapsEl) && gapsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in gapsEl.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                    gaps.Add(item.GetString()!);
        }

        var gapDetails = new List<PmSpecReviewGap>();
        if (root.TryGetProperty("gapDetails", out var detailsEl) && detailsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in detailsEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;
                var message = ReadString(item, "message", "gap");
                if (string.IsNullOrWhiteSpace(message))
                    continue;
                gapDetails.Add(new PmSpecReviewGap
                {
                    Source = ReadString(item, "source") ?? "llm",
                    Message = message,
                });
            }
        }
        if (gapDetails.Count == 0)
            gapDetails = gaps.Select(g => new PmSpecReviewGap { Source = "llm", Message = g }).ToList();
        if (gaps.Count == 0)
            gaps = gapDetails.Select(g => g.Message).ToList();

        return new PmSpecReviewResult
        {
            Score = score,
            Verdict = score >= 85 && string.Equals(verdict, "pass", StringComparison.OrdinalIgnoreCase) ? "pass" : "fail",
            Gaps = gaps,
            GapDetails = gapDetails,
        };
    }

    private static (string Title, string Intro, string SystemPrompt) BuildRoundPrompt(
        int round, SaNineViewCompileResult compileResult,
        IReadOnlyList<string> warnings, string? previousAnswersText)
    {
        return round switch
        {
            1 => ("需求骨架确认",
                  "产品经理已基于行业经验完善了您的需求骨架，请确认以下几个关键决策点。",
                  """
                    你是有 10 年经验的行业产品经理。用户不是专家，你是。
                    任务：基于已生成的需求骨架（businessEvents + entityDrafts），判断最需要用户确认的 3 个模糊点。
                    规则：
                      - 只问真正模糊的、需要业务方决策的点
                      - 行业惯例能定的不要问用户
                      - 每个问题含：问题文本 + 3-5 个选项（末项为"其他"）+ context_hint（为什么问）+ 默认值
                      - 普通题只允许 questionFormat: "MULTI"
                      - 如果问题是「对多个事件做同一维度的决策」，必须使用 "MATRIX_SINGLE" 或 "MATRIX_MULTI"
                    输出 JSON 数组，每元素：
                    {"text","questionFormat","contextHint","defaultOption","matrixSubItems":[{"rowId","rowLabel"}],"options":["...","其他"]}
                    只输出 JSON，不要 markdown。
                    """),
            2 => ("深度精化确认",
                  "系统需求分析师已完成深度分析（含 PSpec/DecisionTable 增强），请确认以下边界条件与业务规则。",
                  """
                    你是产品经理 + 系统需求分析师联合体。
                    基于用户上一轮的回答与 SA 深度分析，判断最重要的 3 个需要用户裁决的点。
                    规则：每个问题聚焦一个决策点（边界条件/异常路径/业务规则冲突）。
                    普通题只允许 questionFormat: "MULTI"；矩阵题使用 "MATRIX_SINGLE"/"MATRIX_MULTI"。
                    输出 JSON 数组，每元素：
                    {"text","questionFormat","contextHint","defaultOption","matrixSubItems":[{"rowId","rowLabel"}],"options":["...","其他"]}
                    只输出 JSON。
                    """),
            3 => ("最终遗漏检查",
                  "这是最后一轮确认，请核对以下推导假设与遗漏点。全部跳过可直接定稿。",
                  """
                    你是最终审查专家。
                    任务：检查全部三轮分析后，还有遗漏吗？推导的假设项中哪些需要用户确认？
                    规则：最多发现 3 个遗漏/待确认假设；如无遗漏，返回空数组 []。
                    普通题只允许 questionFormat: "MULTI"；矩阵题使用 "MATRIX_SINGLE"/"MATRIX_MULTI"。
                    输出 JSON 数组，每元素：
                    {"text","questionFormat","contextHint","defaultOption","matrixSubItems":[{"rowId","rowLabel"}],"options":["...","其他"]}
                    如确实无遗漏，输出 []。
                    """),
            _ => ($"第 {round} 轮确认", string.Empty, "输出 JSON 数组。普通题只允许 questionFormat: \"MULTI\"。"),
        };
    }

    private static string BuildRoundUserPrompt(
        int round, SaNineViewCompileResult compileResult,
        IReadOnlyList<string> warnings, string? previousAnswersText)
    {
        var eventSummary = string.Join("\n",
            compileResult.EventResults.Select(e => $"- {e.EventId}: {e.EventName}（{e.Complexity}）"));

        var assumptionsText = compileResult.Assumptions.Count > 0
            ? "\n编译器推导的假设项：\n" + string.Join("\n",
                compileResult.Assumptions.Select(a => $"- [{a.Confidence:P0}] {a.EventId}/{a.SourceStep}: {a.Text}"))
            : "\n（编译器无假设项）";

        var warningsText = warnings.Count > 0
            ? "\n轻量校验警告：\n" + string.Join("\n", warnings.Select(w => $"- {w}"))
            : string.Empty;

        var prevText = !string.IsNullOrWhiteSpace(previousAnswersText)
            ? $"\n用户上一轮回答：\n{previousAnswersText}"
            : string.Empty;

        return $"""
            需求事件清单（共 {compileResult.EventResults.Count} 个）：
            {eventSummary}
            {assumptionsText}{warningsText}{prevText}

            请给出本轮最需用户确认的最多 {QuestionsPerRound} 个问题。
            """;
    }

    private static List<ClarificationQuestion> ParseQuestionsFromLlm(string content, int round)
    {
        var questions = new List<ClarificationQuestion>();
        try
        {
            var json = ExtractJsonArray(content);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return questions;

            var idx = 0;
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (idx >= QuestionsPerRound) break;
                var text = el.TryGetProperty("text", out var t) ? t.GetString() ?? string.Empty : string.Empty;
                if (string.IsNullOrWhiteSpace(text)) continue;

                var options = new List<ClarificationOption>();
                if (el.TryGetProperty("options", out var optsEl) && optsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var opt in optsEl.EnumerateArray())
                    {
                        var label = opt.ValueKind == JsonValueKind.String
                            ? opt.GetString() ?? string.Empty
                            : opt.TryGetProperty("label", out var l) ? l.GetString() ?? string.Empty : opt.GetRawText();
                        options.Add(new ClarificationOption
                        {
                            Id = $"opt-{options.Count + 1}",
                            Label = label,
                            FreeText = label is "其他" or "其它" or "以上都不是",
                        });
                    }
                }

                var format = "MULTI";
                if (el.TryGetProperty("format", out var fmtEl) && fmtEl.ValueKind == JsonValueKind.String)
                    format = ClarificationQuestion.NormalizeQuestionFormat(fmtEl.GetString());
                else if (el.TryGetProperty("questionFormat", out var qfEl) && qfEl.ValueKind == JsonValueKind.String)
                    format = ClarificationQuestion.NormalizeQuestionFormat(qfEl.GetString());

                List<MatrixSubItem>? matrixItems = null;
                if (el.TryGetProperty("matrixSubItems", out var mxEl) && mxEl.ValueKind == JsonValueKind.Array
                    || el.TryGetProperty("matrix_rows", out mxEl) && mxEl.ValueKind == JsonValueKind.Array)
                {
                    matrixItems = new List<MatrixSubItem>();
                    foreach (var row in mxEl.EnumerateArray())
                    {
                        var rowId = row.TryGetProperty("rowId", out var rid) ? rid.GetString()
                            : row.TryGetProperty("id", out var id2) ? id2.GetString() : null;
                        var rowLabel = row.TryGetProperty("rowLabel", out var rl) ? rl.GetString()
                            : row.TryGetProperty("label", out var lb) ? lb.GetString() : rowId;
                        if (string.IsNullOrWhiteSpace(rowId)) continue;
                        matrixItems.Add(new MatrixSubItem
                        {
                            RowId = rowId!,
                            RowLabel = rowLabel ?? rowId!,
                        });
                    }
                    if (matrixItems.Count > 0 && format is "MULTI")
                        format = "MATRIX_MULTI";
                }

                var qType = format switch
                {
                    "MULTI" or "MATRIX_MULTI" => "multi",
                    "TEXT" => "text",
                    _ => "single",
                };

                questions.Add(new ClarificationQuestion
                {
                    Id = $"r{round}-q{idx + 1}",
                    Text = text,
                    Type = qType,
                    Required = false,
                    Options = options,
                    ContextHint = el.TryGetProperty("contextHint", out var ch) ? ch.GetString()
                        : el.TryGetProperty("context_hint", out var ch2) ? ch2.GetString() : null,
                    DefaultOption = el.TryGetProperty("defaultOption", out var dof)
                        ? (dof.ValueKind == JsonValueKind.String ? dof.GetString() : null)
                        : el.TryGetProperty("default_option", out var dof2)
                            ? (dof2.ValueKind == JsonValueKind.String ? dof2.GetString() : null) : null,
                    QuestionFormat = format,
                    MatrixSubItems = matrixItems,
                });
                idx++;
            }
        }
        catch (JsonException)
        {
            return questions;
        }

        return questions;
    }

    internal static void ApplyMatrixFallback(List<ClarificationQuestion> questions, SaNineViewCompileResult compileResult)
    {
        var eventNames = compileResult.EventResults
            .Select(e => e.EventName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (eventNames.Count < 2) return;

        for (var i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            if (q.MatrixSubItems is { Count: > 0 }) continue;
            if (q.QuestionFormat is not "MULTI") continue;

            var matchedEvents = eventNames
                .Where(en => q.Text.Contains(en, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchedEvents.Count < 2) continue;

            // CR-20260714-01 铁律1：MULTI 升级为 MATRIX 时必须清空 Options，
            // 否则残留的"其他"选项会被错误地加到矩阵题上（矩阵题无"其他"）。
            questions[i] = q with
            {
                MatrixSubItems = matchedEvents.Select((en, j) => new MatrixSubItem
                {
                    RowId = $"evt-{j + 1}",
                    RowLabel = en,
                }).ToList(),
                QuestionFormat = "MATRIX_MULTI",
                Options = new List<ClarificationOption>(),
            };
        }
    }

    internal static void EnsureEscapeHatch(List<ClarificationQuestion> questions)
    {
        // CR-20260714-01 铁律1：多选题每题必有"其他+文本框"，矩阵题无"其他"。
        for (var i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            // 矩阵题（MATRIX_SINGLE / MATRIX_MULTI）是行×列结构，不存在也不应有"其他"选项。
            if (q.QuestionFormat.StartsWith("MATRIX", StringComparison.Ordinal)) continue;
            if (q.Options.Count == 0) continue;
            if (q.Options.Any(o => o.FreeText || o.Label is "其他" or "其它"))
                continue;

            var options = q.Options.Take(4).ToList();
            options.Add(new ClarificationOption
            {
                Id = "o_other",
                Label = "其他",
                FreeText = true,
            });
            questions[i] = q with { Options = options };
        }
    }

    /// <summary>把优先槽位 id 戳进 ContextHint，作答文本可回传 slotId 供 DetectFilledSlots。</summary>
    private static void StampSlotHints(
        List<ClarificationQuestion> questions,
        IReadOnlyList<RequirementSlot> selectedSlots)
    {
        for (var i = 0; i < questions.Count && i < selectedSlots.Count; i++)
        {
            var slot = selectedSlots[i];
            var q = questions[i];
            var stamp = $"[slot:{slot.SlotId}]";
            var hint = string.IsNullOrWhiteSpace(q.ContextHint)
                ? stamp + " " + slot.Description
                : stamp + " " + q.ContextHint;
            questions[i] = q with { ContextHint = hint.Trim() };
        }
    }

    /// <summary>
    /// CR-20260714-01 改动2：构造空编译结果（步骤①尚未九步拆解时，供 ApplyMatrixFallback 兜底）。
    /// </summary>
    private static SaNineViewCompileResult BuildEmptyCompileResult()
    {
        return new SaNineViewCompileResult
        {
            Source = new PreAnalysisModel(),
            ProjectSteps = new Dictionary<string, object>(),
            EventResults = Array.Empty<SaEventResult>(),
            BundleHash = "empty",
            Assumptions = new List<Assumption>(),
        };
    }

    private static ClarificationSet BuildEmptyClarificationSet(string stage, int round, string title, string intro)
        => new()
        {
            SetId = Guid.NewGuid().ToString("N"),
            Stage = stage,
            Round = round,
            Title = title,
            Intro = intro + "（自动生成题目暂不可用，可直接跳过）",
            AllowSkipNonCritical = true,
            Questions = new List<ClarificationQuestion>(),
        };

    /// <summary>
    /// 成熟度达 refine（≥80 分）时返回空题集。
    /// Title/Intro 告知编排器和前端：需求已完整，无需额外提问。
    /// </summary>
    internal static ClarificationSet BuildRefineEmptySet(string stage, int round, MaturityResult maturity)
        => new()
        {
            SetId = Guid.NewGuid().ToString("N"),
            Stage = stage,
            Round = round,
            Title = "需求已足够完整",
            Intro = $"成熟度 {maturity.Score}/100 — 将基于当前信息直接进行深度分析",
            AllowSkipNonCritical = true,   // 无题可答，跳过按钮无意义
            Questions = new List<ClarificationQuestion>(),
        };

    /// <summary>
    /// 为 EvaluateMaturity 构建聊天历史（从 SA 编译结果 + 上一轮答案提取领域摘要）。
    /// 静态方法——不依赖 PmSkillService 实例状态，可独立测试。
    /// </summary>
    internal static List<ChatMessage> BuildMaturityChatHistory(
        SaNineViewCompileResult compileResult,
        string? previousAnswersText,
        string tenantId,
        string projectId,
        long pipelineId)
    {
        var messages = new List<ChatMessage>();
        var parts = new List<string> { $"项目 {projectId}，租户 {tenantId}" };

        if (compileResult.EventResults is { Count: > 0 })
        {
            var entities = compileResult.EventResults.Select(e => e.EventName).Take(15).ToList();
            parts.Add($"实体（{compileResult.EventResults.Count}）：{string.Join("、", entities)}"
                + (compileResult.EventResults.Count > 15 ? " …" : ""));
        }

        if (compileResult.Assumptions is { Count: > 0 })
        {
            var assumptions = compileResult.Assumptions.Take(10).Select(a => a.Text ?? a.ToString());
            parts.Add($"假设项（{compileResult.Assumptions.Count}）：{string.Join("；", assumptions)}");
        }

        messages.Add(new ChatMessage("user", string.Join("\n", parts)));

        if (!string.IsNullOrWhiteSpace(previousAnswersText))
            messages.Add(new ChatMessage("user", $"用户确认/澄清：{previousAnswersText}"));

        return messages;
    }

    private static string ExtractJsonArray(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```"))
        {
            var start = trimmed.IndexOf('[');
            var end = trimmed.LastIndexOf(']');
            if (start >= 0 && end > start) return trimmed[start..(end + 1)];
        }
        var s = trimmed.IndexOf('[');
        var e = trimmed.LastIndexOf(']');
        if (s >= 0 && e > s) return trimmed[s..(e + 1)];
        return trimmed;
    }

    private static AmendmentUnderstanding ParseAmendmentUnderstanding(string content, string fallbackText)
    {
        try
        {
            var json = ExtractJson(content);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new AmendmentUnderstanding
            {
                Features = ReadStringArray(root, "features"),
                Flows = ReadStringArray(root, "flows"),
                EntitiesOrTables = ReadStringArray(root, "entitiesOrTables", "entities_or_tables", "tables"),
                SummaryMarkdown = ReadString(root, "summaryMarkdown", "summary_markdown") ?? fallbackText,
                Severity = NormalizeSeverity(ReadString(root, "severity")),
                Patches = ReadPatches(root),
            };
        }
        catch (JsonException)
        {
            return BuildFallbackUnderstanding(fallbackText);
        }
    }

    private static AmendmentUnderstanding BuildFallbackUnderstanding(string userMessage)
        => new()
        {
            Features = new List<string> { userMessage.Trim() },
            SummaryMarkdown = userMessage.Trim(),
            Severity = "patch",
        };

    private static List<string> ReadStringArray(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Array)
                continue;
            var result = new List<string>();
            foreach (var item in el.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                    result.Add(item.GetString()!);
            return result;
        }

        return new List<string>();
    }

    private static List<AmendmentPatch> ReadPatches(JsonElement root)
    {
        if (!root.TryGetProperty("patches", out var el) || el.ValueKind != JsonValueKind.Array)
            return new List<AmendmentPatch>();

        var patches = new List<AmendmentPatch>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;
            var opText = ReadString(item, "operation", "op", "type");
            if (!Enum.TryParse<AmendmentPatchOperation>(opText, ignoreCase: true, out var operation))
                continue;

            patches.Add(new AmendmentPatch(
                operation,
                ReadString(item, "target", "entity", "eventId") ?? "",
                ReadString(item, "name", "field", "eventName", "ruleId") ?? "",
                ReadString(item, "displayName", "display_name"),
                ReadString(item, "dataType", "fieldType", "type"),
                ReadString(item, "description", "summary"),
                item.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.True,
                ReadString(item, "references", "ref"),
                ReadString(item, "scopeEventId", "scope"),
                ReadString(item, "from"),
                ReadString(item, "to")));
        }

        return patches;
    }

    private static string? ReadString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
            if (root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String)
                return el.GetString();
        return null;
    }

    private static string NormalizeSeverity(string? severity)
        => string.Equals(severity, "enhance", StringComparison.OrdinalIgnoreCase) ? "enhance" : "patch";

    private async Task<IReadOnlyList<RequirementEvolutionSeed>> RetrieveEvolutionSeedsAsync(
        string tenantId,
        string projectId,
        long pipelineId,
        string text,
        CancellationToken ct)
    {
        if (_evolutionContext == null)
            return Array.Empty<RequirementEvolutionSeed>();
        return await _evolutionContext.RetrieveSeedsAsync(tenantId, projectId, pipelineId, text, 3, ct);
    }

    private async Task<ChatCompletionResponse> ChatWithSchemaRetryAsync(
        ChatCompletionRequest request,
        string schemaKind,
        CancellationToken ct)
    {
        var response = await Llm.ChatAsync(request, ct);
        var error = ValidateJsonShape(response.Content, schemaKind);
        if (!response.IsSuccess || error == null)
            return response;

        _logger.LogWarning("pm-skill {SchemaKind} JSON schema 校验失败，重试一次: {Error}", schemaKind, error);
        var retryMessages = request.Messages.ToList();
        retryMessages.Add(new ChatMessage("user", $"上一次 JSON 不符合 schema：{error}。请只输出修正后的 JSON。"));
        var retry = new ChatCompletionRequest
        {
            ProviderCode = request.ProviderCode,
            SystemPrompt = request.SystemPrompt,
            Messages = retryMessages,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            TimeoutMs = request.TimeoutMs,
            ResponseFormat = request.ResponseFormat,
        };
        return await Llm.ChatAsync(retry, ct);
    }

    private static string? ValidateJsonShape(string? content, string schemaKind)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            var json = schemaKind == "clarification" ? ExtractJsonArray(content) : ExtractJson(content);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return schemaKind switch
            {
                "clarification" => ValidateClarificationJson(root),
                "amend" => ValidateAmendJson(root),
                "review" => ValidateReviewJson(root),
                _ => null,
            };
        }
        catch (JsonException ex)
        {
            return $"JSON 解析失败: {ex.Message}";
        }
    }

    private static string? ValidateClarificationJson(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array)
            return "出题结果必须是数组";
        foreach (var q in root.EnumerateArray())
        {
            if (q.ValueKind != JsonValueKind.Object)
                return "每个题目必须是对象";
            if (!q.TryGetProperty("text", out var text) || text.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(text.GetString()))
                return "题目缺少 text";
            if (!q.TryGetProperty("options", out var options) || options.ValueKind != JsonValueKind.Array || options.GetArrayLength() is < 3 or > 5)
                return "题目 options 数量必须为 3-5";
        }

        return null;
    }

    private static string? ValidateAmendJson(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return "Amend 结果必须是对象";
        if (!root.TryGetProperty("summaryMarkdown", out var summary) || summary.ValueKind != JsonValueKind.String)
            return "Amend 缺少 summaryMarkdown";
        if (root.TryGetProperty("patches", out var patches) && patches.ValueKind != JsonValueKind.Array)
            return "patches 必须是数组";
        return null;
    }

    private static string? ValidateReviewJson(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return "Review 结果必须是对象";
        if (!root.TryGetProperty("score", out var score) || !score.TryGetInt32(out _))
            return "Review 缺少整数 score";
        if (!root.TryGetProperty("gaps", out var gaps) || gaps.ValueKind != JsonValueKind.Array)
            return "Review gaps 必须是数组";
        return null;
    }

    private static PmSpecReviewResult MergeReviewGaps(
        PmSpecReviewResult parsed,
        IReadOnlyList<RequirementGraphGap> graphGaps)
    {
        var details = parsed.GapDetails.Count > 0
            ? parsed.GapDetails.ToList()
            : parsed.Gaps.Select(g => new PmSpecReviewGap { Source = "llm", Message = g }).ToList();

        foreach (var gap in graphGaps)
        {
            if (details.Any(d => string.Equals(d.Message, gap.Message, StringComparison.Ordinal)))
                continue;
            details.Add(new PmSpecReviewGap { Source = gap.Source, Message = gap.Message });
        }

        var gapTexts = details.Select(d => d.Message)
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new PmSpecReviewResult
        {
            Score = parsed.Score,
            Verdict = graphGaps.Count > 0 && parsed.Score >= 85 ? "fail" : parsed.Verdict,
            Gaps = gapTexts,
            GapDetails = details,
        };
    }

    private async Task<string> GenerateSkeletonViaTotAsync(
        SkillContext context, string skeletonId, string fragmentId, CancellationToken ct)
    {
        var systemPrompt = """
            你是 JNPF 低代码平台的产品经理 Skill。根据用户需求输出 IR-0 骨架 JSON。
            必须包含：businessEvents（6-15项，每项含 eventId/eventName/complexityHint/dependsOn）、
            roleMatrix（角色×事件→操作）、entityDrafts（3-8项）。

            entityDrafts 每项必须含：
            - entityName: 实体名（PascalCase）
            - displayName: 中文显示名
            - tableName: 表名（留空则编译器自动派生）
            - fields[]: 每字段含 name/type/required/primaryKey（布尔，true=主键）
            - relations[]: 实体间关系声明，每项含 fromField/toEntity/toField/relationType(many-to-one|one-to-many|many-to-many)
              或在字段级用 references: "EntityName.FieldName" 声明外键

            字段示例：
            "fields": [
              {"name":"id","type":"BIGINT","required":true,"primaryKey":true},
              {"name":"employeeId","type":"BIGINT","required":true,"references":"Employee.id"},
              {"name":"leaveDays","type":"float","required":true}
            ]

            关系示例：
            "relations": [
              {"fromField":"employeeId","toEntity":"Employee","toField":"id","relationType":"many-to-one"}
            ]

            roleMatrix 示例：
            "roleMatrix": {
              "roles": ["员工","部门主管","HR"],
              "matrix": {"EV-001": {"员工": ["create","read"], "部门主管": ["approve","reject"]}}
            }

            只输出 JSON，不要 markdown。
            """;

        var seedHints = string.Join(", ", context.SeedMatches.Take(5).Select(s => s.EventNamePattern));
        var userPrompt = $"""
            用户需求：
            {RequirementTextHelper.ForPmPrompt(context)}

            参考种子（可复用模式）：
            {(string.IsNullOrWhiteSpace(seedHints) ? "（无）" : seedHints)}

            请给出一种 businessEvents 切分方案的完整 IR-0 骨架。
            skeletonId 使用 {skeletonId}。
            """;

        var tot = await Llm.TreeSearchAsync(new TreeSearchRequest
        {
            // 27 号 §7.2/§7.3：按任务路由 Provider + 超时分级。
            // context.ProviderCode 显式指定时优先（编排器/测试可覆盖）；否则走 AI:ProviderRouting["pm-skill"]。
            ProviderCode = !string.IsNullOrWhiteSpace(context.ProviderCode)
                ? context.ProviderCode
                : Llm.ResolveProvider(SkillId),
            SystemPrompt = systemPrompt,
            Messages = new List<ChatMessage> { new("user", userPrompt) },
            BranchCount = TotBranchCount,
            BaseTemperature = 0.3,
            TemperatureStep = 0.35,
            ResponseFormat = "json",
            MaxTokens = TotMaxTokens,
            TimeoutMs = Llm.ResolveTimeoutMs(SkillId),
        }, ct);

        if (!tot.IsSuccess || !tot.Succeeded.Any())
            throw Oops.Bah($"PM Skill ToT 全部分支 LLM 失败: {tot.Error}");

        var keyword = ExtractSearchKeyword(context);
        var scored = new List<(string Json, decimal Score, int BranchIndex, double Temperature)>();

        foreach (var candidate in tot.Succeeded)
        {
            string json;
            try
            {
                json = ExtractJson(candidate.Content);
                // #region agent log
                try
                {
                    var content = candidate.Content ?? string.Empty;
                    var trimmed = content.TrimEnd();
                    var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
                    {
                        ["sessionId"] = "ead5d0",
                        ["runId"] = "post-fix",
                        ["hypothesisId"] = "A",
                        ["location"] = "PmSkillService.GenerateSkeletonViaTotAsync:validate",
                        ["message"] = "tot-branch-before-parse",
                        ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        ["data"] = new Dictionary<string, object?>
                        {
                            ["branch"] = candidate.BranchIndex,
                            ["temperature"] = candidate.Temperature,
                            ["tokensIn"] = candidate.TokensIn,
                            ["tokensOut"] = candidate.TokensOut,
                            ["maxTokens"] = TotMaxTokens,
                            ["hitMaxTokens"] = candidate.TokensOut >= TotMaxTokens,
                            ["contentLen"] = content.Length,
                            ["endsWithBrace"] = trimmed.EndsWith('}'),
                            ["jsonExtractLen"] = json.Length,
                            ["head"] = content.Length <= 120 ? content : content[..120],
                            ["tail"] = content.Length <= 160 ? content : content[^160..],
                        },
                    });
                    File.AppendAllText(@"D:\JNPF-v52\debug-ead5d0.log", payload + Environment.NewLine);
                }
                catch { /* debug ingest must not break PM */ }
                // #endregion
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("businessEvents", out var events)
                    || events.ValueKind != JsonValueKind.Array
                    || events.GetArrayLength() == 0)
                {
                    // #region agent log
                    try
                    {
                        var miss = JsonSerializer.Serialize(new Dictionary<string, object?>
                        {
                            ["sessionId"] = "ead5d0",
                            ["runId"] = "post-fix",
                            ["hypothesisId"] = "E",
                            ["location"] = "PmSkillService.GenerateSkeletonViaTotAsync:missingEvents",
                            ["message"] = "tot-branch-missing-businessEvents",
                            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            ["data"] = new Dictionary<string, object?>
                            {
                                ["branch"] = candidate.BranchIndex,
                                ["tokensOut"] = candidate.TokensOut,
                                ["rootProps"] = doc.RootElement.EnumerateObject().Select(p => p.Name).Take(12).ToArray(),
                            },
                        });
                        File.AppendAllText(@"D:\JNPF-v52\debug-ead5d0.log", miss + Environment.NewLine);
                    }
                    catch { }
                    // #endregion
                    _logger.LogWarning(
                        "PM ToT 分支 {Branch}@{Temp} 缺少 businessEvents，跳过",
                        candidate.BranchIndex, candidate.Temperature);
                    continue;
                }
            }
            catch (Exception ex)
            {
                // #region agent log
                try
                {
                    var content = candidate.Content ?? string.Empty;
                    var fail = JsonSerializer.Serialize(new Dictionary<string, object?>
                    {
                        ["sessionId"] = "ead5d0",
                        ["runId"] = "post-fix",
                        ["hypothesisId"] = "A",
                        ["location"] = "PmSkillService.GenerateSkeletonViaTotAsync:parseFail",
                        ["message"] = "tot-branch-json-invalid",
                        ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        ["data"] = new Dictionary<string, object?>
                        {
                            ["branch"] = candidate.BranchIndex,
                            ["tokensOut"] = candidate.TokensOut,
                            ["maxTokens"] = TotMaxTokens,
                            ["hitMaxTokens"] = candidate.TokensOut >= TotMaxTokens,
                            ["contentLen"] = content.Length,
                            ["exType"] = ex.GetType().Name,
                            ["exMsg"] = ex.Message.Length <= 200 ? ex.Message : ex.Message[..200],
                        },
                    });
                    File.AppendAllText(@"D:\JNPF-v52\debug-ead5d0.log", fail + Environment.NewLine);
                }
                catch { }
                // #endregion
                _logger.LogWarning(ex,
                    "PM ToT 分支 {Branch}@{Temp} JSON 无效，跳过",
                    candidate.BranchIndex, candidate.Temperature);
                continue;
            }

            var score = await ScoreCandidateAsync(json, keyword, ct);
            scored.Add((json, score, candidate.BranchIndex, candidate.Temperature));
            _logger.LogInformation(
                "PM ToT 分支 {Branch}@{Temp} score={Score} tokens={In}/{Out}",
                candidate.BranchIndex, candidate.Temperature, score,
                candidate.TokensIn, candidate.TokensOut);
        }

        if (scored.Count == 0)
            throw Oops.Bah("PM Skill ToT 全部分支产出无效（JSON 或 businessEvents 校验失败）");

        var top = SelectTopCandidate(scored);
        _logger.LogInformation(
            "PM ToT Top-1: branch={Branch} temp={Temp} score={Score}",
            top.BranchIndex, top.Temperature, top.Score);

        using var topDoc = JsonDocument.Parse(top.Json);
        return NormalizeSkeletonJson(topDoc.RootElement, skeletonId, fragmentId);
    }

    private async Task<decimal> ScoreCandidateAsync(string candidateJson, string keyword, CancellationToken ct)
    {
        var args = JsonSerializer.Serialize(new { candidateJson, keyword }, JsonOptions);
        var result = await Mcp.CallToolAsync("kg.score-candidate", args, ct);
        if (!result.IsSuccess)
            throw Oops.Bah($"kg.score-candidate 失败: {result.Error}");

        using var doc = JsonDocument.Parse(result.ContentJson);
        if (doc.RootElement.TryGetProperty("score", out var scoreEl)
            && scoreEl.TryGetDecimal(out var score))
        {
            return score;
        }

        return 0m;
    }

    /// <summary>按 kg.score-candidate 评分选 Top-1；同分取首条。</summary>
    public static (string Json, decimal Score, int BranchIndex, double Temperature) SelectTopCandidate(
        IReadOnlyList<(string Json, decimal Score, int BranchIndex, double Temperature)> scored)
    {
        var best = scored[0];
        for (var i = 1; i < scored.Count; i++)
        {
            if (scored[i].Score > best.Score)
                best = scored[i];
        }

        return best;
    }

    public static string ExtractSearchKeyword(SkillContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.UserRequirement))
        {
            var trimmed = context.UserRequirement.Trim();
            return trimmed.Length <= 80 ? trimmed : trimmed[..80];
        }

        return context.SeedMatches.FirstOrDefault()?.EventNamePattern ?? "enterprise";
    }

    private static string NormalizeSkeletonJson(JsonElement root, string skeletonId, string fragmentId)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(root.GetRawText(), JsonOptions)
            ?? new Dictionary<string, JsonElement>();

        dict["@context"] = JsonSerializer.SerializeToElement("https://schema.jnpf.ai/ir/v1");
        dict["@id"] = JsonSerializer.SerializeToElement(fragmentId);
        dict["skeletonId"] = JsonSerializer.SerializeToElement(skeletonId);
        if (!dict.ContainsKey("version"))
            dict["version"] = JsonSerializer.SerializeToElement(1);

        return JsonSerializer.Serialize(dict, JsonOptions);
    }

    public static string ExtractJson(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```"))
        {
            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            if (start >= 0 && end > start)
                return trimmed[start..(end + 1)];
        }

        return trimmed;
    }
}
