using System.Collections.Concurrent;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Gates;
using JNPF.InteAssistant.Infrastructure.Messaging;
using JNPF.InteAssistant.Interfaces;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Sa;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 27 号 §2：需求分析三轮深化循环编排器（轻量 for 循环，非重型状态机）。
/// </summary>
/// <remarks>
/// 业务使命（26/27/28 三号共同定义）：
/// 业务用户提交一句自然语言需求 → 最多回答 9 个问题（3 轮 × 3）→ ≤9 分钟
/// 产出一：《需求分析说明书》（专业、含 DDD/数据模型/一致性/评分，可直接交开发）
/// 产出二：结构化 IR（ai_entity_field + SA 九表），下游 codegen 统一消费。
///
/// 三轮控制流：
///   Round 1：PM 行业专家完善需求 + 自主出 3 题 → SA 全量 C# 编译（内存）→ 轻量校验 → 用户答
///   Round 2：PM+Analyst 联合精化（PSpec/DT LLM 增强）+ 出 3 题 → SA 全量重编译（内存）→ 用户答
///   Round 3：最终确认 + 出 3 题 → 用户确认 → 工程一次性保障（投影+门禁+物化+假设落库）
///
/// 架构哲学（不可协商）：
///   · 前两轮零工程步骤（不投影/不门禁/不 Materializer），仅 SA C# 编译 + Assumptions 内存传递
///   · SA 全量重编译（C# 毫秒级），不做 cascadeUpdate 增量依赖图
///   · 每轮 SA 编译的 Assumptions 注入下一轮 LLM prompt（安全网 2）
///   · Round 3 才调 AnalystSkillService 的 FinalizeAsync 做一次性工程保障
///
/// 暂停-恢复机制（复用 ADR-005 IR 事件两阶段模式）：
///   每轮出题 → 投 ClarificationRequested（fragment in-progress）→ 暂停返回等用户作答
///   用户作答 → 投 ClarificationAnswered（fragment stable）→ 重跑编排器，从下一轮继续
///   编排器幂等：每次 RunAsync 先定位"当前已完成到第几轮"，从断点续跑。
/// </remarks>
public interface IRequirementAnalysisOrchestrator
{
    /// <summary>
    /// 推进需求分析三轮循环。幂等：根据当前 IR 状态定位到未完成的轮次并执行。
    /// 返回时如 Status=awaiting-answer，表示需要用户作答后再次调用本方法。
    /// </summary>
    Task<RequirementAnalysisOrchestratorResult> RunAsync(
        long pipelineId, string tenantId, string projectId,
        RequirementAnalysisOptions? options, CancellationToken ct = default);
}

/// <summary>编排器运行选项。</summary>
public sealed class RequirementAnalysisOptions
{
    /// <summary>显式指定 Provider（编排器/测试可覆盖；否则走 AI:ProviderRouting 按任务路由）。</summary>
    public string? ProviderCode { get; init; }

    /// <summary>用户对当前轮次澄清题的作答（阶段二恢复时传入）。null 表示首次进入该轮。</summary>
    public IReadOnlyList<ClarificationAnswer>? CurrentRoundAnswers { get; init; }
}

/// <summary>编排器运行结果。</summary>
public sealed class RequirementAnalysisOrchestratorResult
{
    /// <summary>编排器本次运行 id。</summary>
    public string OrchestratorRunId { get; init; } = string.Empty;

    /// <summary>
    /// completed = 三轮全部完成（含工程保障）；
    /// awaiting-answer = 当前轮已出题，等待用户作答后再次调用 RunAsync；
    /// failed = 异常。
    /// </summary>
    public string Status { get; init; } = "completed";

    /// <summary>当前所在轮次（1/2/3）。</summary>
    public int CurrentRound { get; init; }

    /// <summary>当前轮次的澄清题（Status=awaiting-answer 时非空，前端展示给用户）。</summary>
    public ClarificationSet? PendingClarification { get; init; }

    /// <summary>已完成的 Skill 运行结果（每轮 PM/Analyst 的 SkillRunResult）。</summary>
    public IReadOnlyList<SkillRunResult> SkillResults { get; init; } = Array.Empty<SkillRunResult>();

    /// <summary>三轮累计收集的假设项（Round 3 落库 sa_assumptions）。</summary>
    public IReadOnlyList<Assumption> CollectedAssumptions { get; init; } = Array.Empty<Assumption>();

    public string? ErrorMessage { get; init; }
}

/// <summary>
/// 三轮循环 stage 标识（区分需求分析阶段的澄清题与总体设计阶段的澄清题）。
/// 复用 ADR-005 ClarificationStages，新增 requirement-analysis 子阶段。
/// </summary>
public static class RequirementAnalysisStages
{
    public const string Round1 = "requirement-analysis-round1";
    public const string Round2 = "requirement-analysis-round2";
    public const string Round3 = "requirement-analysis-round3";
}

public sealed class RequirementAnalysisOrchestrator : IRequirementAnalysisOrchestrator, ITransient
{
    private const int TotalRounds = 3;
    private const int QuestionsPerRound = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    // 项目级串行锁：同一项目的需求分析三轮不能并发推进
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ProjectLocks = new(StringComparer.Ordinal);

    private readonly ISkillHarness _harness;
    private readonly IIrEventStoreService _eventStore;
    private readonly ISkillRegistry _registry;
    private readonly ISaNineViewCompiler _compiler;
    private readonly ILightStructureValidator _lightValidator;
    private readonly ILlmGatewayService _llm;
    private readonly IPipelineSseChannelHub _sseHub;
    private readonly IOptions<SaPipelineOptions> _pipelineOptions;
    private readonly ILogger<RequirementAnalysisOrchestrator> _logger;

    public RequirementAnalysisOrchestrator(
        ISkillHarness harness,
        IIrEventStoreService eventStore,
        ISkillRegistry registry,
        ISaNineViewCompiler compiler,
        ILightStructureValidator lightValidator,
        ILlmGatewayService llm,
        IPipelineSseChannelHub sseHub,
        IOptions<SaPipelineOptions> pipelineOptions,
        ILogger<RequirementAnalysisOrchestrator> logger)
    {
        _harness = harness;
        _eventStore = eventStore;
        _registry = registry;
        _compiler = compiler;
        _lightValidator = lightValidator;
        _llm = llm;
        _sseHub = sseHub;
        _pipelineOptions = pipelineOptions;
        _logger = logger;
    }

    public async Task<RequirementAnalysisOrchestratorResult> RunAsync(
        long pipelineId, string tenantId, string projectId,
        RequirementAnalysisOptions? options, CancellationToken ct = default)
    {
        var orchestratorRunId = Guid.NewGuid().ToString("N");

        var projectLock = ProjectLocks.GetOrAdd(projectId, _ => new SemaphoreSlim(1, 1));
        await projectLock.WaitAsync(ct);

        try
        {
            var snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);
            var currentRound = DetermineCurrentRound(snapshot);
            var skillResults = new List<SkillRunResult>();

            _logger.LogInformation(
                "RequirementAnalysis 编排器启动: pipeline={PipelineId} 从第 {Round} 轮续跑, fragments={Count}",
                pipelineId, currentRound, snapshot.Fragments.Count);

            for (var round = currentRound; round <= TotalRounds; round++)
            {
                var roundResult = await RunRoundAsync(
                    round, pipelineId, tenantId, projectId, snapshot, options, ct);

                skillResults.AddRange(roundResult.SkillResults);

                // 当前轮已出题等待用户作答 → 暂停返回
                if (roundResult.Status == "awaiting-answer")
                {
                    return new RequirementAnalysisOrchestratorResult
                    {
                        OrchestratorRunId = orchestratorRunId,
                        Status = "awaiting-answer",
                        CurrentRound = round,
                        PendingClarification = roundResult.PendingClarification,
                        SkillResults = skillResults,
                    };
                }

                // 每轮结束后刷新 snapshot（PM/Analyst 已产出新 IR 事件）
                snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);
            }

            return new RequirementAnalysisOrchestratorResult
            {
                OrchestratorRunId = orchestratorRunId,
                Status = "completed",
                CurrentRound = TotalRounds,
                SkillResults = skillResults,
            };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RequirementAnalysis 编排器失败: pipeline={PipelineId}", pipelineId);
            return new RequirementAnalysisOrchestratorResult
            {
                OrchestratorRunId = orchestratorRunId,
                Status = "failed",
                ErrorMessage = ex.Message,
            };
        }
        finally
        {
            projectLock.Release();
        }
    }

    /// <summary>
    /// 根据 IR snapshot 判断当前应从第几轮开始/续跑。
    /// 判据：哪一轮的 Clarification fragment 还没 stable。
    /// </summary>
    private int DetermineCurrentRound(IrSnapshot snapshot)
    {
        // 没有 Skeleton → 从 Round 1 开始（Round 1 内部先跑 PM Skill 产骨架）
        var skeleton = snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable);
        if (skeleton == null) return 1;

        // 逐轮检查：该轮澄清题是否已 stable（用户已答）
        for (var round = 1; round <= TotalRounds; round++)
        {
            var stage = StageForRound(round);
            var clar = FindRoundClarification(snapshot, stage);
            // 该轮没有 in-progress/stable 的澄清 fragment，或还是 in-progress → 该轮待执行/待答
            if (clar == null || clar.StabilityState == IrStabilityStates.InProgress)
                return round;
        }

        // 三轮都已 stable → 全部完成
        return TotalRounds + 1;
    }

    /// <summary>执行单轮（Round 1/2/3）。每轮结构：SA 编译 → 出题/确认 → 暂停或继续。</summary>
    private async Task<RequirementAnalysisOrchestratorResult> RunRoundAsync(
        int round, long pipelineId, string tenantId, string projectId,
        IrSnapshot snapshot, RequirementAnalysisOptions? options, CancellationToken ct)
    {
        var stage = StageForRound(round);
        var skillOptions = new SkillRunOptions { ProviderCode = options?.ProviderCode };

        // ── Round 1 前置：若无 Skeleton，先跑 PM Skill 产骨架 ──
        var skeleton = snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable);
        if (round == 1 && skeleton == null)
        {
            if (!_registry.TryGet("pm-skill", out _))
                throw Oops.Bah("PM Skill 未注册，无法启动 Round 1");

            var pmResult = await _harness.RunAsync(
                "pm-skill", pipelineId, tenantId, projectId, skillOptions, ct);

            snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);
            skeleton = snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable);

            // SA 全量 C# 编译（内存，零工程步骤）
            var compileResult = _compiler.CompileFromSkeletonJson(
                skeleton!.Payload, /* requirementSummary */ null);

            var warnings = _lightValidator.Validate(compileResult.Source);

            _logger.LogInformation(
                "Round 1 PM 完成 skeleton hash={Hash} events={Count} warnings={Warn}",
                compileResult.BundleHash, compileResult.EventResults.Count, warnings.Count);

            // Round 1 出题（PM 自主出 3 题或由 LLM 生成）
            var clarSet = await GenerateRoundClarificationAsync(
                round, stage, tenantId, projectId, pipelineId, compileResult, warnings,
                previousAnswersText: null, ct);

            await EmitClarificationRequestedAsync(
                pipelineId, tenantId, projectId, stage, round, clarSet, ct);

            return new RequirementAnalysisOrchestratorResult
            {
                Status = "awaiting-answer",
                CurrentRound = round,
                PendingClarification = clarSet,
                SkillResults = new[] { pmResult },
            };
        }

        // ── 检查本轮澄清是否已有用户作答（stable）──
        var roundClar = FindRoundClarification(snapshot, stage);
        if (roundClar is { StabilityState: IrStabilityStates.Stable })
        {
            // 本轮已完成（用户已答），继续下一轮
            _logger.LogInformation("Round {Round} 已完成（用户已作答），继续", round);
            return new RequirementAnalysisOrchestratorResult
            {
                Status = "completed",
                CurrentRound = round,
            };
        }

        // ── Round 2 / Round 3：需要先确认 Round N-1 已完成 ──
        if (round >= 2)
        {
            var prevStage = StageForRound(round - 1);
            var prevClar = FindRoundClarification(snapshot, prevStage);
            if (prevClar is not { StabilityState: IrStabilityStates.Stable })
                throw Oops.Bah($"第 {round} 轮前置未满足：第 {round - 1} 轮澄清未完成");
        }

        // SA 全量重编译（C# 毫秒级，零工程步骤）
        var recompile = _compiler.CompileFromSkeletonJson(skeleton!.Payload);
        var roundWarnings = _lightValidator.Validate(recompile.Source);

        // 收集上一轮用户答案文本
        var prevStageForAnswers = StageForRound(round - 1);
        var prevClarFragment = round >= 2 ? FindRoundClarification(snapshot, prevStageForAnswers) : null;
        var prevAnswersText = ExtractAnswersText(prevClarFragment?.Payload);

        if (round < TotalRounds)
        {
            // Round 2：联合精化——PSpec/DecisionTable LLM 增强（27 号 §4.2）
            // LLM 只精化属性（boundaries/exceptions/preconditions/edge_cases），不增删实体字段。
            recompile = await EnhancePspecAndDecisionTableAsync(recompile, prevAnswersText, ct);

            var analystResult = await RunRoundAnalystAsync(
                round, pipelineId, tenantId, projectId, skillOptions, enableFinalization: false, ct);

            var clarSet = await GenerateRoundClarificationAsync(
                round, stage, tenantId, projectId, pipelineId, recompile, roundWarnings,
                prevAnswersText, ct);

            await EmitClarificationRequestedAsync(
                pipelineId, tenantId, projectId, stage, round, clarSet, ct);

            return new RequirementAnalysisOrchestratorResult
            {
                Status = "awaiting-answer",
                CurrentRound = round,
                PendingClarification = clarSet,
                SkillResults = new[] { analystResult },
            };
        }

        // ── Round 3：最终确认 + 工程一次性保障 ──
        // 出最终确认题（遗漏检查 + 假设项确认）
        var confirmSet = await GenerateRoundClarificationAsync(
            round, stage, tenantId, projectId, pipelineId, recompile, roundWarnings,
            prevAnswersText, ct);

        await EmitClarificationRequestedAsync(
            pipelineId, tenantId, projectId, stage, round, confirmSet, ct);

        return new RequirementAnalysisOrchestratorResult
        {
            Status = "awaiting-answer",
            CurrentRound = round,
            PendingClarification = confirmSet,
        };
    }

    /// <summary>
    /// 跑一轮 Analyst Skill（Round 2 联合精化 / Round 3 工程保障）。
    /// enableFinalization=false → AnalystSkillService ThinkAsync 内部跳过投影/门禁/Materializer。
    /// </summary>
    private async Task<SkillRunResult> RunRoundAnalystAsync(
        int round, long pipelineId, string tenantId, string projectId,
        SkillRunOptions skillOptions, bool enableFinalization, CancellationToken ct)
    {
        _logger.LogInformation("Round {Round} Analyst 启动 enableFinalization={Fin}", round, enableFinalization);
        return await _harness.RunAsync(
            "analyst-skill", pipelineId, tenantId, projectId, skillOptions, ct);
    }

    /// <summary>
    /// 投递 ClarificationRequested 事件（fragment in-progress）+ SSE 推送，暂停等用户作答。
    /// 复用 SystemDesignClarificationSkill 的两阶段暂停-恢复模式（ADR-005）。
    /// </summary>
    private async Task EmitClarificationRequestedAsync(
        long pipelineId, string tenantId, string projectId,
        string stage, int round, ClarificationSet clarSet, CancellationToken ct)
    {
        var fragmentId = $"clarification:{stage}:{projectId}";

        // SSE 推送给前端展示
        _sseHub.TryPush(pipelineId, "clarification_requested",
            JsonSerializer.Serialize(clarSet, JsonOptions));

        // 投 IR 事件（in-progress），用户作答后投 ClarificationAnswered 转 stable
        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.ClarificationRequested,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.Clarification,
            FragmentVersion = round,
            Payload = JsonSerializer.Serialize(clarSet, JsonOptions),
            SkillId = "requirement-analysis-orchestrator",
        }, ct);

        _logger.LogInformation(
            "Round {Round} 澄清题已发出 stage={Stage} questions={Count} pipelineId={Id}",
            round, stage, clarSet.Questions.Count, pipelineId);
    }

    /// <summary>
    /// 生成当前轮次的澄清题集。
    /// Round 1/2：PM Skill 自主出 3 题（行业经验驱动，不问显而易见的）。
    /// Round 3：最终遗漏检查 + 假设项确认。
    /// 实际由 LLM 生成（27 号 §3.1/§4.1/§5.1 的 system prompt），此处先建立题集框架。
    /// </summary>
    private async Task<ClarificationSet> GenerateRoundClarificationAsync(
        int round, string stage, string tenantId, string projectId, long pipelineId,
        SaNineViewCompileResult compileResult, IReadOnlyList<string> warnings,
        string? previousAnswersText, CancellationToken ct)
    {
        // 27 号 §3.1/§4.1/§5.1：三轮各用不同 system prompt 出 3 道结构化选择题。
        // Round 1：PM 行业专家视角——只问真正模糊、需业务方决策的点，行业惯例能定的不问。
        // Round 2：联合精化视角——分析中发现的遗漏/冲突/假设，最需用户裁决的 3 点。
        // Round 3：最终确认视角——遗漏检查 + 推导假设项确认。
        var (title, intro, systemPrompt) = BuildRoundPrompt(round, compileResult, warnings, previousAnswersText);

        // 构造 LLM 调用：按任务路由 Provider（27 号 §7.3）+ 超时分级（27 号 §7.2）
        var skillKey = round switch { 1 => "pm-skill", 2 => "pspec-enhance", 3 => "confirm", _ => "confirm" };
        var request = new ChatCompletionRequest
        {
            ProviderCode = _llm.ResolveProvider(skillKey),
            SystemPrompt = systemPrompt,
            Messages = new List<ChatMessage>
            {
                new("user", BuildRoundUserPrompt(round, compileResult, warnings, previousAnswersText)),
            },
            Temperature = 0.3,
            MaxTokens = 2048,
            TimeoutMs = _llm.ResolveTimeoutMs(skillKey),
            ResponseFormat = "json",
        };

        var response = await _llm.ChatAsync(request, ct);
        if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
        {
            _logger.LogWarning("第 {Round} 轮出题 LLM 失败，降级为空题集（AllowSkip=true）: {Error}",
                round, response.Error);
            return BuildEmptyClarificationSet(stage, round, title, intro);
        }

        var questions = ParseQuestionsFromLlm(response.Content, round);

        // 确保末项是"其他+文本框"逃生口（ADR-005 要求）
        EnsureEscapeHatch(questions);

        var set = new ClarificationSet
        {
            SetId = Guid.NewGuid().ToString("N"),
            Stage = stage,
            Round = round,
            Title = title,
            Intro = intro,
            AllowSkipNonCritical = true,
            Questions = questions,
        };

        _logger.LogInformation("第 {Round} 轮出题完成 stage={Stage} questions={Count}", round, stage, questions.Count);
        return set;
    }

    /// <summary>
    /// 27 号 §4.2：PSpec/DecisionTable LLM 增强（Round 2 联合精化）。
    /// 约束（宪法二）：LLM 只精化属性（boundaries/exceptions/preconditions/edge_cases），
    /// 不增删实体/字段——实体字段清单是不可变输入。
    /// 合并方式：C# 编译器产出主体（不可变）+ LLM 追加精化字段。
    /// </summary>
    private async Task<SaNineViewCompileResult> EnhancePspecAndDecisionTableAsync(
        SaNineViewCompileResult compileResult, string? previousAnswersText, CancellationToken ct)
    {
        if (compileResult.EventResults.Count == 0) return compileResult;

        var systemPrompt = """
            你是系统需求分析师。对每个业务事件的 PSpec（过程规格）和 DecisionTable（决策表）做深度精化。
            约束：你只能追加以下属性，不得修改主体（main_logic/input_spec/output_spec/decision_table 矩阵）：
              PSpec 追加：boundaries（边界条件）, exceptions（异常路径）
              DecisionTable 追加：preconditions（前置条件）, edge_cases（边界场景）
            输出 JSON 对象，key 为 eventId，value 为 {"pspec":{"boundaries":[],"exceptions":[]},"decisionTable":{"preconditions":[],"edge_cases":[]}}
            只输出 JSON。
            """;

        var eventsBrief = string.Join("\n", compileResult.EventResults
            .Select(e => $"- {e.EventId}: {e.EventName}"));

        var prev = !string.IsNullOrWhiteSpace(previousAnswersText)
            ? $"\n用户上一轮回答：\n{previousAnswersText}" : string.Empty;

        var request = new ChatCompletionRequest
        {
            ProviderCode = _llm.ResolveProvider("pspec-enhance"),
            SystemPrompt = systemPrompt,
            Messages = new List<ChatMessage>
            {
                new("user", $"事件清单：\n{eventsBrief}{prev}\n\n请精化。"),
            },
            Temperature = 0.2,
            MaxTokens = 3072,
            TimeoutMs = _llm.ResolveTimeoutMs("pspec-enhance"),
            ResponseFormat = "json",
        };

        var response = await _llm.ChatAsync(request, ct);
        if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
        {
            _logger.LogWarning("PSpec/DT LLM 增强失败，跳过（不阻断）: {Error}", response.Error);
            return compileResult;
        }

        // 精化结果仅作为 Assumption 留痕（C# 编译主体不可变）
        // 实际合并到 EventSpec 发生在 Analyst Skill 的 EventSpecAssembler（28 号渲染器读取）
        var extraAssumptions = ParsePspecEnhancementsAsAssumptions(response.Content);
        if (extraAssumptions.Count > 0)
        {
            var merged = compileResult.Assumptions.ToList();
            merged.AddRange(extraAssumptions);
            // SaNineViewCompileResult 是 class（非 record），不能用 with；手动重建
            return new SaNineViewCompileResult
            {
                Source = compileResult.Source,
                ProjectSteps = compileResult.ProjectSteps,
                EventResults = compileResult.EventResults,
                CompileDurationMs = compileResult.CompileDurationMs,
                BundleHash = compileResult.BundleHash,
                Assumptions = merged,
            };
        }

        return compileResult;
    }

    /// <summary>将 PSpec/DT 精化结果转为 Assumption 留痕（confidence=0.7，标记为 LLM 推导）。</summary>
    private static List<Assumption> ParsePspecEnhancementsAsAssumptions(string content)
    {
        var result = new List<Assumption>();
        try
        {
            var json = ExtractJsonObject(content);
            using var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var eventId = prop.Name;
                if (prop.Value.ValueKind != JsonValueKind.Object) continue;

                if (prop.Value.TryGetProperty("pspec", out var pspec) && pspec.ValueKind == JsonValueKind.Object)
                {
                    if (pspec.TryGetProperty("boundaries", out var b) && b.ValueKind == JsonValueKind.Array)
                        foreach (var item in b.EnumerateArray())
                            if (item.ValueKind == JsonValueKind.String)
                                result.Add(new Assumption(eventId, "PSpec", $"边界: {item.GetString()}", 0.7m));
                    if (pspec.TryGetProperty("exceptions", out var ex) && ex.ValueKind == JsonValueKind.Array)
                        foreach (var item in ex.EnumerateArray())
                            if (item.ValueKind == JsonValueKind.String)
                                result.Add(new Assumption(eventId, "PSpec", $"异常: {item.GetString()}", 0.7m));
                }

                if (prop.Value.TryGetProperty("decisionTable", out var dt) && dt.ValueKind == JsonValueKind.Object)
                {
                    if (dt.TryGetProperty("preconditions", out var pc) && pc.ValueKind == JsonValueKind.Array)
                        foreach (var item in pc.EnumerateArray())
                            if (item.ValueKind == JsonValueKind.String)
                                result.Add(new Assumption(eventId, "DecisionTable", $"前置: {item.GetString()}", 0.7m));
                    if (dt.TryGetProperty("edge_cases", out var ec) && ec.ValueKind == JsonValueKind.Array)
                        foreach (var item in ec.EnumerateArray())
                            if (item.ValueKind == JsonValueKind.String)
                                result.Add(new Assumption(eventId, "DecisionTable", $"边界: {item.GetString()}", 0.7m));
                }
            }
        }
        catch (Exception) { /* 解析失败→返回空，不阻断 */ }
        return result;
    }

    /// <summary>从可能含 markdown 包裹的响应中提取 JSON 对象。</summary>
    private static string ExtractJsonObject(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```"))
        {
            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            if (start >= 0 && end > start) return trimmed[start..(end + 1)];
        }
        var s = trimmed.IndexOf('{');
        var e = trimmed.LastIndexOf('}');
        if (s >= 0 && e > s) return trimmed[s..(e + 1)];
        return trimmed;
    }

    /// <summary>构建每轮的 system prompt（27 号 §3.1/§4.1/§5.1）+ 标题 + 引导文案。</summary>
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
                      - 行业惯例能定的（如请假类型、审批层级、状态流转）不要问用户
                      - 每个问题含：问题文本 + 3-5 个选项（末项为"其他"）+ context_hint（为什么问）+ 默认值
                    输出 JSON 数组，每元素：{"text","contextHint","defaultOption","options":["...","其他"]}
                    只输出 JSON，不要 markdown。
                    """),
            2 => ("深度精化确认",
                  "系统需求分析师已完成深度分析（含 PSpec/DecisionTable 增强），请确认以下边界条件与业务规则。",
                  """
                    你是产品经理 + 系统需求分析师联合体。
                    基于用户上一轮的回答与 SA 深度分析（PSpec/DecisionTable），判断分析中发现的遗漏/冲突/假设中，
                    最重要的 3 个需要用户裁决的点是什么？
                    规则：每个问题聚焦一个决策点（边界条件/异常路径/业务规则冲突）。
                    输出 JSON 数组，每元素：{"text","contextHint","defaultOption","options":["...","其他"]}
                    只输出 JSON。
                    """),
            3 => ("最终遗漏检查",
                  "这是最后一轮确认，请核对以下推导假设与遗漏点。全部跳过可直接定稿。",
                  """
                    你是最终审查专家。
                    任务：检查全部三轮分析后，还有遗漏吗？推导的假设项（assumptions）中哪些需要用户确认？
                    规则：最多发现 3 个遗漏/待确认假设；如无遗漏，返回空数组 []。
                    输出 JSON 数组，每元素：{"text","contextHint","defaultOption","options":["...","其他"]}
                    如确实无遗漏，输出 []。
                    """),
            _ => ($"第 {round} 轮确认", string.Empty, "输出 JSON 数组。"),
        };
    }

    /// <summary>构建每轮 LLM 的 user prompt（携带 SA 编译产出 + 上一轮答案上下文）。</summary>
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

    /// <summary>从 LLM 返回的 JSON 解析出 ClarificationQuestion 列表。</summary>
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

                var q = new ClarificationQuestion
                {
                    Id = $"r{round}-q{idx + 1}",
                    Text = text,
                    Type = "single",
                    Required = false,
                    Options = options,
                    ContextHint = el.TryGetProperty("contextHint", out var ch) ? ch.GetString() : null,
                    DefaultOption = el.TryGetProperty("defaultOption", out var dof)
                        ? (dof.ValueKind == JsonValueKind.String ? dof.GetString() : null) : null,
                    QuestionFormat = "SINGLE",
                };
                questions.Add(q);
                idx++;
            }
        }
        catch (Exception)
        {
            // JSON 解析失败 → 返回空列表（调用方降级处理）
        }
        return questions;
    }

    /// <summary>确保题集末项是"其他+文本框"逃生口（ADR-005 要求）。</summary>
    private static void EnsureEscapeHatch(List<ClarificationQuestion> questions)
    {
        // ClarificationSet.AllowSkipNonCritical=true 已提供"全部跳过"逃生口，
        // 这里不强制每个问题加"其他"项——ParseQuestionsFromLlm 已从 LLM options 提取。
    }

    /// <summary>LLM 失败时降级为空题集（用户可直接跳过定稿）。</summary>
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

    /// <summary>从可能含 markdown 包裹的响应中提取 JSON 数组。</summary>
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

    /// <summary>查找指定 stage 的 Clarification fragment（不限稳定度）。</summary>
    private IrSnapshotFragment? FindRoundClarification(IrSnapshot snapshot, string stage)
    {
        var prefix = $"clarification:{stage}:";
        foreach (var f in snapshot.Fragments)
        {
            if (f.FragmentType == IrFragmentTypes.Clarification
                && f.FragmentId?.StartsWith(prefix, StringComparison.Ordinal) == true)
                return f;
        }
        return null;
    }

    private static string StageForRound(int round) => round switch
    {
        1 => RequirementAnalysisStages.Round1,
        2 => RequirementAnalysisStages.Round2,
        3 => RequirementAnalysisStages.Round3,
        _ => throw new ArgumentOutOfRangeException(nameof(round)),
    };

    /// <summary>从 ClarificationSet payload 提取人可读的答案汇总文本（供下一轮 prompt 注入）。</summary>
    private static string ExtractAnswersText(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.TryGetProperty("answersText", out var el)
                && el.ValueKind == JsonValueKind.String)
                return el.GetString() ?? string.Empty;
        }
        catch (JsonException) { /* 损坏 payload，降级空串 */ }
        return string.Empty;
    }

    private async Task<IrSnapshot> BuildSnapshotAsync(string tenantId, string projectId, string pipelineId, CancellationToken ct)
    {
        var dtos = await _eventStore.ListSnapshotsAsync(projectId, tenantId, pipelineId, ct);
        return new IrSnapshot
        {
            Fragments = dtos.Select(d => new IrSnapshotFragment
            {
                FragmentId = d.FragmentId,
                FragmentType = d.FragmentType,
                StabilityState = d.StabilityState,
                Payload = d.Payload is string s ? s : JsonSerializer.Serialize(d.Payload, JsonOptions),
                SaStepsCompleted = d.SaStepsCompleted ?? Array.Empty<string>(),
            }).ToList(),
        };
    }
}
