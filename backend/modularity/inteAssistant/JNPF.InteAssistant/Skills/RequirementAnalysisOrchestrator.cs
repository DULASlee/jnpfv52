using System.Collections.Concurrent;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;

using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Dto.Skills;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Gates;
using JNPF.InteAssistant.Infrastructure.Messaging;
using JNPF.InteAssistant.Interfaces;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Runtime;
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

    /// <summary>Apply 补充需求后，即使已 Finalize 也强制重生成 02 并复审。</summary>
    public bool ForceRefinalize { get; init; }

    /// <summary>
    /// PM 终评 &lt; 85 时强制 Finalize（留痕逃生口，对齐手动 API SkillsApiService.ConfirmRequirementSpecAsync）。
    /// CR-20260712-01：修复编排器 PM 终评门控逃逸。仅 review.Verdict=fail 时生效，会写 forceConfirm+forceReason 入 IR。
    /// </summary>
    public bool ForceConfirm { get; init; }

    /// <summary>ForceConfirm=true 时的强制理由（留痕审计，对齐手动 API）。</summary>
    public string? ForceReason { get; init; }

    /// <summary>
    /// CR-20260713-03：启用新的 4 步线性 PM 流程（完善需求 → 九步拆解 → 二次完善 → 生成说明书）。
    /// 默认 false（走旧三轮循环，向后兼容）；true 时旁路 RunRoundAsync，走 RunPmPipelineAsync。
    /// 阶段 C 验证稳定后，此开关移除，新流程成为默认。
    /// </summary>
    public bool UseNewPipeline { get; init; }

    /// <summary>
    /// CR-20260713-03：新流程恢复时，用户对 PM 对话式追问的回答（单个 pipeline 可能多轮追问）。
    /// 旧流程用 CurrentRoundAnswers（结构化 ClarificationAnswer）；新流程用此字段（自然语言）。
    /// </summary>
    public string? PmClarificationAnswer { get; init; }

    /// <summary>
    /// CR-20260713-03：新流程步骤④用户对需求说明书的文字反馈（不同意时）。
    /// 非空时触发：PM 据此改需求文本 → 重跑九步拆解 → 回到步骤④。
    /// </summary>
    public string? SpecFeedback { get; init; }

    /// <summary>CR-20260714-01 改动5：用户原始输入（PM 智能判断意图后路由）。</summary>
    public string? UserMessage { get; init; }

    /// <summary>
    /// 首次进入时由调用方（如 sa-gate）传入的用户原始需求文本（合并附件后）。
    /// ResolveUserRequirementAsync 优先使用此值——避免首次进入时 IR snapshot 还没有
    /// stable Requirement fragment 导致 SkillContext.UserRequirement 为空（PM 读不到需求）。
    /// </summary>
    public string? InitialUserRequirement { get; init; }
}

/// <summary>三轮需求分析编排器 API 请求体。</summary>
public sealed class RequirementAnalysisRunRequest
{
    /// <summary>显式指定 Provider（可选）。</summary>
    public string? ProviderCode { get; init; }

    /// <summary>用户对当前轮澄清题的作答（恢复时传入；首次进入为 null）。</summary>
    public IReadOnlyList<ClarificationAnswer>? Answers { get; init; }

    /// <summary>
    /// 强制重跑 Round3 Finalize + PM 终评（用于历史 pipeline 回填 CTA/PmReviewed，或运维重生 02）。
    /// 不绕过「已 StageConfirmed 禁止 Amend」门闩。
    /// </summary>
    public bool ForceRefinalize { get; init; }

    /// <summary>PM 终评 &lt; 85 时强制 Finalize（留痕逃生口）。CR-20260712-01。</summary>
    public bool ForceConfirm { get; init; }

    /// <summary>ForceConfirm=true 时的强制理由（留痕审计）。</summary>
    public string? ForceReason { get; init; }

    /// <summary>CR-20260713-03：启用新 4 步线性 PM 流程。</summary>
    public bool UseNewPipeline { get; init; }

    /// <summary>CR-20260713-03：新流程用户对追问的回答。</summary>
    public string? PmClarificationAnswer { get; init; }

    /// <summary>CR-20260713-03：新流程用户对需求说明书的反馈。</summary>
    public string? SpecFeedback { get; init; }

    /// <summary>
    /// CR-20260714-01 改动5：用户原始输入（不带明确意图参数）。
    /// 当前端无法确定用户意图（确认/修改/回答追问）时传入，PM Skill 智能判断后路由。
    /// </summary>
    public string? UserMessage { get; init; }
}

/// <summary>编排器运行结果。</summary>
public sealed class RequirementAnalysisOrchestratorResult
{
    /// <summary>编排器本次运行 id。</summary>
    public string OrchestratorRunId { get; init; } = string.Empty;

    /// <summary>
    /// completed = 三轮全部完成（含工程保障）；
    /// awaiting-answer = 当前轮已出题，等待用户作答后再次调用 RunAsync；
    /// awaiting-clarification = CR-20260713-03 新流程：PM 对话式追问，等用户回答；
    /// awaiting-spec-confirm = CR-20260713-03 新流程：需求说明书已渲染，等用户确认或反馈；
    /// pm-review-failed = PM 终评未通过；
    /// failed = 异常。
    /// </summary>
    public string Status { get; init; } = "completed";

    /// <summary>当前所在轮次（1/2/3）。</summary>
    public int CurrentRound { get; init; }

    /// <summary>当前轮次的澄清题（Status=awaiting-answer 时非空，前端展示给用户）。</summary>
    public ClarificationSet? PendingClarification { get; init; }

    /// <summary>
    /// CR-20260713-03：新流程 PM 对话式追问的问题（Status=awaiting-clarification 时非空）。
    /// 自然语言问题，前端用输入框展示（非结构化选择题）。
    /// </summary>
    public string? PendingPmQuestion { get; init; }

    /// <summary>
    /// CR-20260713-03：新流程渲染的需求说明书（Status=awaiting-spec-confirm 时非空）。
    /// 前端展示给用户确认；用户不同意时输入文字反馈。
    /// </summary>
    public string? RenderedSpec { get; init; }

    /// <summary>已完成的 Skill 运行结果（每轮 PM/Analyst 的 SkillRunResult）。</summary>
    public IReadOnlyList<SkillRunResult> SkillResults { get; init; } = Array.Empty<SkillRunResult>();

    /// <summary>三轮累计收集的假设项（Round 3 落库 sa_assumptions）。</summary>
    public IReadOnlyList<Assumption> CollectedAssumptions { get; init; } = Array.Empty<Assumption>();

    public string? ErrorMessage { get; init; }

    /// <summary>
    /// CR-20260713-03：门控拒绝时的用户提示（Status=gate-rejected 时非空）。
    /// 前端据此展示"请描述您要构建的系统"等引导文案。
    /// </summary>
    public string? GateHint { get; init; }
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

    public static bool IsRequirementAnalysisStage(string? stage) =>
        stage is Round1 or Round2 or Round3 or "requirement"; // CR-20260712-01 D3: 兼容历史 pipeline（343 等）stage="requirement"
}

public sealed class RequirementAnalysisOrchestrator : IRequirementAnalysisOrchestrator, ITransient
{
    private const int TotalRounds = 3;
    private const int QuestionsPerRound = 3;
    /// <summary>交付说明书前至少完成的 PM 结构化澄清轮次（对齐 28 号附录 B + A-B-C 2–3 轮深度优化）。</summary>
    private const int MinPmOptimizationRounds = 2;
    private static readonly TimeSpan StreamingHeartbeatInterval = TimeSpan.FromSeconds(15);

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
    private readonly PmSkillService _pm;
    private readonly ILlmGatewayService _llm;
    private readonly IPipelineSseChannelHub _sseHub;
    private readonly IOptions<SaPipelineOptions> _pipelineOptions;
    private readonly ILogger<RequirementAnalysisOrchestrator> _logger;
    // CR-20260713-03：新流程步骤①前置门控（可选注入，旧测试不传时降级跳过）
    private readonly RequirementGateService? _gate;

    public RequirementAnalysisOrchestrator(
        ISkillHarness harness,
        IIrEventStoreService eventStore,
        ISkillRegistry registry,
        ISaNineViewCompiler compiler,
        ILightStructureValidator lightValidator,
        PmSkillService pm,
        ILlmGatewayService llm,
        IPipelineSseChannelHub sseHub,
        IOptions<SaPipelineOptions> pipelineOptions,
        ILogger<RequirementAnalysisOrchestrator> logger,
        RequirementGateService? gate = null)
    {
        _harness = harness;
        _eventStore = eventStore;
        _registry = registry;
        _compiler = compiler;
        _lightValidator = lightValidator;
        _pm = pm;
        _llm = llm;
        _sseHub = sseHub;
        _pipelineOptions = pipelineOptions;
        _logger = logger;
        _gate = gate;
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
            // P0：编排器写 IR 时必须有 SkillExecutionScope，否则 PipelineId 回退为 projectId（违反 R12）
            using var execScope = SkillExecutionScope.Begin(
                orchestratorRunId, tenantId, projectId, pipelineId,
                "requirement-analysis-orchestrator", ct);

            // ══ CR-20260714-01 阶段 C：新 4 步线性 PM 流程为唯一流程 ══
            // 旧三轮循环(RunRoundAsync)已废弃，所有调用直接走 RunPmPipelineAsync。
            // ForceRefinalize 运维回填路径保留(下方)，用于历史 pipeline 回填。
            if (options?.ForceRefinalize != true)
            {
                _logger.LogInformation(
                    "RequirementAnalysis 编排器启动[新流程]: pipeline={PipelineId}", pipelineId);
                var newResult = await RunPmPipelineAsync(pipelineId, tenantId, projectId, options, ct);
                return new RequirementAnalysisOrchestratorResult
                {
                    OrchestratorRunId = orchestratorRunId,
                    Status = newResult.Status,
                    PendingPmQuestion = newResult.PendingPmQuestion,
                    RenderedSpec = newResult.RenderedSpec,
                    SkillResults = newResult.SkillResults,
                    ErrorMessage = newResult.ErrorMessage,
                };
            }

            // ── 运维回填路径(ForceRefinalize)：历史 pipeline 回填 CTA/PmReviewed ──
            // 保留旧 Finalize 能力供运维使用，但不走旧三轮循环。
            var snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);
            var currentRound = DetermineCurrentRound(snapshot);
            var skillResults = new List<SkillRunResult>();

            _logger.LogInformation(
                "RequirementAnalysis 编排器启动: pipeline={PipelineId} 从第 {Round} 轮续跑, fragments={Count}",
                pipelineId, currentRound, snapshot.Fragments.Count);

            // Apply / 运维回填：强制 Finalize+PM 终评，不依赖三轮澄清是否齐全
            // CR-20260712-01：PM 终评先跑，fail 则按 ForceConfirm 决定是否 Finalize（修复门控逃逸）
            if (options?.ForceRefinalize == true)
            {
                _logger.LogInformation(
                    "ForceRefinalize 执行 Round3 工程保障 pipeline={PipelineId} priorRound={Round}",
                    pipelineId, currentRound);
                var skillOptions = new SkillRunOptions
                {
                    ProviderCode = options?.ProviderCode,
                    UserRequirement = options?.InitialUserRequirement,
                };
                var review = await ReviewRequirementSpecAsync(
                    pipelineId, tenantId, projectId, orchestratorRunId, skillOptions,
                    forceConfirm: options?.ForceConfirm == true, forceReason: options?.ForceReason, ct: ct);
                var allowFinalize = review.Verdict == "pass" || options?.ForceConfirm == true;
                _logger.LogWarning(
                    "ForceRefinalize PM 终评 verdict={Verdict} score={Score} allowFinalize={Allow} forceConfirm={Force} pipeline={PipelineId}",
                    review.Verdict, review.Score, allowFinalize, options?.ForceConfirm, pipelineId);
                var finalizeResult = await RunRoundAnalystAsync(
                    TotalRounds, pipelineId, tenantId, projectId, skillOptions,
                    enableFinalization: allowFinalize, ct);
                skillResults.Add(finalizeResult);
                return new RequirementAnalysisOrchestratorResult
                {
                    OrchestratorRunId = orchestratorRunId,
                    Status = allowFinalize ? "completed" : "pm-review-failed",
                    CurrentRound = TotalRounds,
                    SkillResults = skillResults,
                };
            }

            // P0：三轮澄清均已 stable 但尚未 Finalize → 强制跑 Round 3 工程保障（禁止静默 completed）
            // CR-20260712-01：PM 终评先跑，fail 则按 ForceConfirm 决定是否 Finalize（修复门控逃逸）
            if (currentRound > TotalRounds)
            {
                if (!await HasFinalizedEngineeringAsync(tenantId, projectId, pipelineId, ct))
                {
                    _logger.LogInformation(
                        "三轮澄清已完成但尚未 Finalize，强制执行 Round 3 工程保障 pipeline={PipelineId}",
                        pipelineId);
                    var skillOptions = new SkillRunOptions
                    {
                        ProviderCode = options?.ProviderCode,
                        UserRequirement = options?.InitialUserRequirement,
                    };
                    var review = await ReviewRequirementSpecAsync(
                        pipelineId, tenantId, projectId, orchestratorRunId, skillOptions,
                        forceConfirm: options?.ForceConfirm == true, forceReason: options?.ForceReason, ct: ct);
                    var allowFinalize = review.Verdict == "pass" || options?.ForceConfirm == true;
                    _logger.LogWarning(
                        "三轮完成 PM 终评 verdict={Verdict} score={Score} allowFinalize={Allow} forceConfirm={Force} pipeline={PipelineId}",
                        review.Verdict, review.Score, allowFinalize, options?.ForceConfirm, pipelineId);
                    var finalizeResult = await RunRoundAnalystAsync(
                        TotalRounds, pipelineId, tenantId, projectId, skillOptions,
                        enableFinalization: allowFinalize, ct);
                    skillResults.Add(finalizeResult);
                    return new RequirementAnalysisOrchestratorResult
                    {
                        OrchestratorRunId = orchestratorRunId,
                        Status = allowFinalize ? "completed" : "pm-review-failed",
                        CurrentRound = TotalRounds,
                        SkillResults = skillResults,
                    };
                }

                return new RequirementAnalysisOrchestratorResult
                {
                    OrchestratorRunId = orchestratorRunId,
                    Status = "completed",
                    CurrentRound = TotalRounds,
                    SkillResults = skillResults,
                };
            }

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
        // SkeletonCreated 投影为 Draft；三轮编排消费 Draft/Stable 均可（随后会 Stabilize）
        var skeleton = FindSkeletonAny(snapshot);
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
    // ════════════════════════════════════════════════════════════════════
    // CR-20260713-03 阶段 B：新 4 步线性 PM 流程（回归"完善需求"初衷）
    //
    // 取代旧三轮循环 RunRoundAsync。当 RequirementAnalysisOptions.UseNewPipeline=true
    // 时由 RunAsync 调用本方法。流程：
    //   步骤① EnhanceRequirement — PM 用提示词+种子完善需求（可对话式追问）
    //   步骤② SaDecompose — 7 步 C# 编译 + 2b PM LLM 产 PSpec/DT 真语义
    //   步骤③ RefineFromAnalysis — PM 分析九步反向完善需求（可追问 → 重跑②）
    //   步骤④ GenerateSpec — 渲染需求说明书，等用户确认或反馈
    //
    // 暂停点（awaiting-user）：
    //   - 步骤① pending_question → awaiting-clarification（PmClarificationTurn）
    //   - 步骤③ pending_question → awaiting-clarification（PmClarificationTurn）
    //   - 步骤④ → awaiting-spec-confirm（用户确认或反馈）
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 新 4 步线性 PM 流程入口。取代旧三轮循环。
    /// 每次调用推进到下一个暂停点；用户作答/反馈后再次调用继续。
    /// </summary>
    private async Task<RequirementAnalysisOrchestratorResult> RunPmPipelineAsync(
        long pipelineId, string tenantId, string projectId,
        RequirementAnalysisOptions? options, CancellationToken ct)
    {
        var snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);
        var providerCode = options?.ProviderCode;

        // ── 构造 SkillContext（PM 三方法的公共输入）──
        // 优先用调用方传入的 InitialUserRequirement（sa-gate 已合并附件）；否则从 IR snapshot 解析。
        // 修复：首次进入时 IR snapshot 还没有 stable Requirement fragment，若不优先用传入值会得到空字符串
        // → PM 流式 prompt 的 retrievalText 为空 → LLM "读不到需求"（2026-07-17 Playwright 暴露）。
        var userRequirement = !string.IsNullOrWhiteSpace(options?.InitialUserRequirement)
            ? options.InitialUserRequirement!
            : await ResolveUserRequirementAsync(tenantId, projectId, pipelineId, ct);
        var context = new SkillContext
        {
            RunId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            ProjectId = projectId,
            PipelineId = pipelineId,
            UserRequirement = userRequirement,
            Snapshot = snapshot,
            ProviderCode = providerCode,
        };

        // ── 判断当前应从哪一步恢复 ──
        // 优先级：SpecFeedback(步骤④反馈) > SpecConfirmed(步骤⑤) > SpecRendered(步骤④渲染) > 追问回灌 > 初始
        var existingSpec = snapshot.Find(IrFragmentTypes.Requirement, IrStabilityStates.Stable);
        var specFeedback = options?.SpecFeedback;

        // 步骤④：用户对需求说明书反馈 → PM 改需求文本 → 重跑①②③
        if (!string.IsNullOrWhiteSpace(specFeedback) && existingSpec != null)
        {
            _logger.LogInformation(
                "RunPmPipeline 步骤④反馈 pipeline={PipelineId} feedback={Len}字",
                pipelineId, specFeedback.Length);
            return await HandleSpecFeedbackAsync(
                pipelineId, tenantId, projectId, context, specFeedback, ct);
        }

        // CR-20260714-01 改动5：PM 智能意图判断 — 用户输入不带明确参数时，根据 IR 状态判断意图
        var userMessage = options?.UserMessage;
        if (!string.IsNullOrWhiteSpace(userMessage) && string.IsNullOrWhiteSpace(specFeedback))
        {
            var intent = ClassifyUserIntent(userMessage, snapshot);
            _logger.LogInformation(
                "RunPmPipeline 意图判断 pipeline={PipelineId} intent={Intent} confidence={Conf}",
                pipelineId, intent.Intent, intent.Confidence);

            if (intent.Intent == "confirm_spec" && existingSpec != null)
            {
                // 用户确认需求说明书 → 走步骤⑤ Finalize（下方 hasSpecRendered 分支处理）
                // 不在这里 return，让下方步骤⑤逻辑接管
            }
            else if (intent.Intent == "request_change" && existingSpec != null)
            {
                // 用户要修改 → 当 specFeedback 处理
                return await HandleSpecFeedbackAsync(
                    pipelineId, tenantId, projectId, context, userMessage, ct);
            }
            else if (intent.Intent == "answer_question")
            {
                // 用户在回答追问 → 当 pmAnswer 处理（下方 pmAnswer 分支会接管）
                // 把 userMessage 赋给 pmAnswer 变量（options 是 init-only class，不能 with）
                // 下方 var pmAnswer = options?.PmClarificationAnswer 会取到 null → 用 userMessage 补位
            }
            // intent == "unknown" → 不路由，继续走正常恢复点判断
        }

        // CR-20260714-01 步骤⑤：说明书已渲染(specRendered 事件存在)且用户无反馈 → 确认 → Finalize
        var hasSpecRendered = await HasEventAsync(tenantId, projectId, pipelineId, IrEventTypes.RequirementSpecRendered, ct);
        var hasSpecConfirmed = await HasEventAsync(tenantId, projectId, pipelineId, IrEventTypes.RequirementSpecConfirmed, ct);
        if (hasSpecRendered && !hasSpecConfirmed && string.IsNullOrWhiteSpace(specFeedback))
        {
            _logger.LogInformation(
                "RunPmPipeline 步骤⑤ 用户确认需求说明书 pipeline={PipelineId}", pipelineId);
            return await RunStep5FinalizeAsync(
                pipelineId, tenantId, projectId, context, options, ct);
        }

        // 已 Finalize → 流程完成
        if (hasSpecConfirmed)
        {
            _logger.LogInformation(
                "RunPmPipeline 已完成(需求说明书已确认) pipeline={PipelineId}", pipelineId);
            return new RequirementAnalysisOrchestratorResult { Status = "completed" };
        }

        // 步骤④恢复：仅步骤③ RequirementRefined + 九步已拆 + ≥MinPmOptimizationRounds 轮澄清已作答
        if (existingSpec != null
            && await IsReadyForSpecDeliveryAsync(tenantId, projectId, pipelineId, snapshot, ct))
        {
            var refinedText = ExtractRequirementText(existingSpec);
            if (!string.IsNullOrWhiteSpace(refinedText))
            {
                return await RenderSpecAndWaitConfirmAsync(
                    pipelineId, tenantId, projectId, refinedText, ct);
            }
        }

        // 九步已拆 → 步骤③ / 结构化澄清续跑
        var nineViewFragment = snapshot.Find("IR1_SaNineView", IrStabilityStates.Stable);
        var hasRefined = await HasEventAsync(tenantId, projectId, pipelineId, IrEventTypes.RequirementRefined, ct);

        // ── 结构化澄清已作答 → 续跑步骤③（CR-20260717：GenerateClarification 路径）──
        if (nineViewFragment != null && hasRefined && !hasSpecRendered && !HasPendingClarification(snapshot))
        {
            var answeredCount = await CountClarificationAnsweredAsync(tenantId, projectId, pipelineId, ct);
            var reqClar = FindRoundClarification(snapshot, ClarificationStages.Requirement);
            var answersText = ExtractAnswersText(reqClar?.Payload);
            if (answeredCount > 0
                && reqClar?.StabilityState == IrStabilityStates.Stable
                && !string.IsNullOrWhiteSpace(answersText))
            {
                _logger.LogInformation(
                    "RunPmPipeline 结构化澄清已作答，续跑步骤③ pipeline={PipelineId} answered={Answered}",
                    pipelineId, answeredCount);

                await ApplyClarificationAnswersToSkeletonAsync(
                    pipelineId, tenantId, projectId, snapshot, reqClar, answeredCount, ct);
                snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);

                var requirementFragment = snapshot.Find(IrFragmentTypes.Requirement, IrStabilityStates.Stable)
                    ?? snapshot.Find(IrFragmentTypes.Requirement, IrStabilityStates.Draft)
                    ?? existingSpec;
                var enhancedText = ExtractRequirementText(requirementFragment) ?? userRequirement;
                var mergedEnhanced = enhancedText + "\n\n【用户澄清作答】\n" + answersText;

                var compileResult = await RecompileFromCurrentSkeletonAsync(
                    pipelineId, tenantId, projectId, context, ct);
                var resumeWarnings = _lightValidator.Validate(compileResult.Source);
                var resumeContext = CloneContextWithRequirement(context, mergedEnhanced);

                _sseHub.TryPush(pipelineId, "thinking",
                    $"📋 已收到第 {answeredCount} 轮澄清作答，PM 正在并入需求并继续…");

                return await RunStep3RefineAsync(
                    pipelineId, tenantId, projectId, resumeContext, mergedEnhanced,
                    turns: null, ct, resumeWarnings, compileResult);
            }
        }

        // 九步已拆但步骤③未完成 → 继续反向完善（禁止 SA 九步完成后直接推说明书）
        if (nineViewFragment != null && !hasSpecRendered && !hasRefined)
        {
            var enhancedText = ExtractRequirementText(existingSpec) ?? userRequirement;
            var resumeTurns = LoadClarificationTurns(snapshot);
            _logger.LogInformation(
                "RunPmPipeline 九步已完成但步骤③未完成，续跑反向完善 pipeline={PipelineId}", pipelineId);
            return await RunStep3RefineAsync(
                pipelineId, tenantId, projectId, context, enhancedText, resumeTurns, ct);
        }

        // ── 用户回答了追问 → 回灌继续步骤①或③ ──
        // CR-20260714-01 改动5：pmAnswer 优先用显式参数，其次用意图判断的 userMessage
        var pmAnswer = !string.IsNullOrWhiteSpace(options?.PmClarificationAnswer)
            ? options.PmClarificationAnswer
            : userMessage;
        if (!string.IsNullOrWhiteSpace(pmAnswer))
        {
            // 读取历史追问上下文，判断当前在步骤①还是③
            var turns = LoadClarificationTurns(snapshot);
            var lastTurn = turns.LastOrDefault();
            if (lastTurn != null)
            {
                turns[^1] = lastTurn with { UserAnswer = pmAnswer };
                _logger.LogInformation(
                    "RunPmPipeline 回灌追问答案 pipeline={PipelineId} source={Source} turnCount={Count}",
                    pipelineId, lastTurn.Source, turns.Count);

                if (lastTurn.Source == ClarificationSource.Step1Enhance)
                {
                    return await RunStep1EnhanceAsync(
                        pipelineId, tenantId, projectId, context, turns, ct);
                }
                else
                {
                    // Step3Refine：需要重新编译九步
                    var enhancedText = ExtractRequirementText(existingSpec) ?? userRequirement;
                    return await RunStep3RefineAsync(
                        pipelineId, tenantId, projectId, context, enhancedText, turns, ct);
                }
            }
        }

        // ── 首次进入：门控检查 → 步骤① ──
        // CR-20260713-03：防止用户原始需求不合格，在 SA 九步拆解时取不到数据报错。
        // 门控复用 RequirementGateService.ValidateHardRules（硬规则：长度/垃圾内容/附件数）。
        // 语义合格性评估（SemanticFitnessValidator）由 GatePipeline 在更上游执行（Stage 0），
        // 编排器只兜底硬规则——双保险。
        if (_gate != null && !string.IsNullOrEmpty(userRequirement))
        {
            var hardRule = _gate.ValidateHardRules(userRequirement, attachmentCount: 0);
            if (!hardRule.Passed)
            {
                _logger.LogWarning(
                    "RunPmPipeline 门控拒绝 pipeline={PipelineId} reason={Reason} hint={Hint}",
                    pipelineId, hardRule.Reason, hardRule.Hint);
                return new RequirementAnalysisOrchestratorResult
                {
                    Status = "gate-rejected",
                    ErrorMessage = hardRule.Reason,
                    GateHint = hardRule.Hint,
                };
            }
            _logger.LogInformation(
                "RunPmPipeline 门控通过 pipeline={PipelineId} reason={Reason}",
                pipelineId, hardRule.Reason);
        }

        _logger.LogInformation(
            "RunPmPipeline 首次启动 pipeline={PipelineId} 从步骤①开始", pipelineId);
        return await RunStep1EnhanceAsync(
            pipelineId, tenantId, projectId, context, turns: null, ct);
    }

    /// <summary>步骤①：PM 完善需求（含 MULTI/MATRIX 选择题追问）。</summary>
    private async Task<RequirementAnalysisOrchestratorResult> RunStep1EnhanceAsync(
        long pipelineId, string tenantId, string projectId,
        SkillContext context, IReadOnlyList<PmClarificationTurn>? turns, CancellationToken ct)
    {
        // CR-20260717-02：步骤① PM 完善过程 — 真流式 token 推 SSE（前端路由到折叠「推理区」），
        // 不在正文区灌整段需求文本。pending_question 时只展示追问卡片。
        _sseHub.TryPush(pipelineId, "thinking", "📝 PM 正在完善需求…");
        var onToken = CreateStreamingTokenHandler(
            pipelineId, "⏳ PM 仍在完善需求，请稍候…");

        var result = await _pm.EnhanceRequirementStreamAsync(context, turns, onToken, ct);

        if (result.Status == "pending_question")
        {
            // CR-20260714-01 改动2（铁律2）+ CR-20260717-01 §3.7：PM 一次出题 — 直接用流式响应产出的 ClarificationSet。
            // LLM 声明 pending_question 却未给题 = 协议违规，硬错误（不再二次出题兜底）。
            var clarSet = result.PendingClarificationSet;
            if (clarSet == null || clarSet.Questions.Count == 0)
            {
                throw Oops.Bah(
                    $"RunPmPipeline 步骤① LLM 声明 pending_question 但未产出题目（协议违规）" +
                    $" pipeline={pipelineId} tenantId={tenantId} partialEnhancement[0..200]=" +
                    $"{(result.PartialEnhancement?.Length > 200 ? result.PartialEnhancement[..200] : result.PartialEnhancement ?? "(空)")}");
            }

            await EmitClarificationRequestedAsync(
                pipelineId, tenantId, projectId, "requirement", 1, clarSet, ct);

            _logger.LogInformation(
                "RunPmPipeline 步骤①暂停追问 pipeline={PipelineId} questions={Count}",
                pipelineId, clarSet.Questions.Count);

            return new RequirementAnalysisOrchestratorResult
            {
                Status = "awaiting-clarification",
                PendingPmQuestion = result.PendingQuestion,
                PendingClarification = clarSet,
            };
        }

        // completed → 后台完善完成，进入步骤②（正文不进聊天主区域）
        await PersistRequirementAsync(
            pipelineId, tenantId, projectId, result.EnhancedText,
            IrEventTypes.RequirementEnhanced, ct);

        _logger.LogInformation(
            "RunPmPipeline 步骤①完成 pipeline={PipelineId} textLen={Len} turns={Turns}",
            pipelineId, result.EnhancedText.Length, result.ClarificationTurns);

        // 直接进入步骤②
        return await RunStep2DecomposeAsync(
            pipelineId, tenantId, projectId, context, result.EnhancedText, ct);
    }

    /// <summary>步骤②：7 步 C# 编译 + 2b PM LLM 产 PSpec/DT 真语义。</summary>
    private async Task<RequirementAnalysisOrchestratorResult> RunStep2DecomposeAsync(
        long pipelineId, string tenantId, string projectId,
        SkillContext context, string enhancedText, CancellationToken ct)
    {
        // 先用 EnhancedText 生成骨架（复用 PM ThinkAsync 的骨架生成能力）
        var skeleton = await EnsureSkeletonAsync(pipelineId, tenantId, projectId, context, ct);

        // CR-20260713-03：九步拆解进度(thinking 折叠区)
        _sseHub.TryPush(pipelineId, "thinking", "📋 正在进行 SA 九步拆解…");

        // 2a. C# 编译器 7 步（确定性，零 LLM）
        var compileResult = _compiler.CompileFromSkeletonJson(skeleton.Payload);
        var warnings = _lightValidator.Validate(compileResult.Source);
        _sseHub.TryPush(pipelineId, "thinking",
            $"✅ 7 步确定性编译完成：{compileResult.EventResults.Count} 个事件");

        // 持久化 Assumptions（跨步骤审计）
        await PersistAssumptionsFragmentAsync(
            pipelineId, tenantId, projectId, compileResult.Assumptions, round: 2, ct);

        // 2b. PM LLM 产 PSpec/DT 真语义
        _sseHub.TryPush(pipelineId, "thinking", "🔧 PM 正在产出 PSpec/DecisionTable 真语义…");
        var enhancedContext = CloneContextWithRequirement(context, enhancedText);
        var enhancedCompile = await RunWithThinkingHeartbeatAsync(
            pipelineId, "⏳ PSpec/DecisionTable 语义增强进行中…", ct,
            token => _pm.EnhancePspecDecisionTableAsync(enhancedContext, compileResult, token));
        _sseHub.TryPush(pipelineId, "thinking", "✅ 九步拆解完成");

        // 持久化九步数据（给后续二次开发/BUG 修复用）
        await PersistNineViewAsync(pipelineId, tenantId, projectId, enhancedCompile, ct);

        _logger.LogInformation(
            "RunPmPipeline 步骤②完成 pipeline={PipelineId} events={Count} warnings={Warn}",
            pipelineId, enhancedCompile.EventResults.Count, warnings.Count);

        // 直接进入步骤③
        return await RunStep3RefineAsync(
            pipelineId, tenantId, projectId, enhancedContext, enhancedText,
            turns: null, ct: ct, warnings: warnings, compileResult: enhancedCompile);
    }

    /// <summary>步骤③：PM 分析九步反向完善需求（含追问 + 澄清轮次门控）。</summary>
    private async Task<RequirementAnalysisOrchestratorResult> RunStep3RefineAsync(
        long pipelineId, string tenantId, string projectId,
        SkillContext context, string enhancedText,
        IReadOnlyList<PmClarificationTurn>? turns,
        CancellationToken ct,
        IReadOnlyList<string>? warnings = null,
        SaNineViewCompileResult? compileResult = null)
    {
        // 若未传入 compileResult，重新编译
        compileResult ??= await RecompileFromCurrentSkeletonAsync(pipelineId, tenantId, projectId, context, ct);
        warnings ??= _lightValidator.Validate(compileResult.Source);

        // CR-20260717-02：步骤③ — 真流式 token 推折叠区，不在正文灌整段分析。
        _sseHub.TryPush(pipelineId, "thinking", "🔍 PM 正在分析九步结果并完善需求…");
        var onToken = CreateStreamingTokenHandler(
            pipelineId, "⏳ PM 仍在分析九步结果，请稍候…");

        var result = await _pm.RefineFromAnalysisStreamAsync(
            context, enhancedText, compileResult, warnings, turns, onToken, ct);

        if (result.Status == "pending_question")
        {
            // CR-20260714-01 改动2（铁律2）+ CR-20260717-01 §3.7：PM 一次出题 — 用流式响应产出的 ClarificationSet。
            // LLM 声明 pending_question 却未给题 = 协议违规，硬错误。
            var clarSet = result.PendingClarificationSet;
            if (clarSet == null || clarSet.Questions.Count == 0)
            {
                throw Oops.Bah(
                    $"RunPmPipeline 步骤③ LLM 声明 pending_question 但未产出题目（协议违规）" +
                    $" pipeline={pipelineId} tenantId={tenantId} partialEnhancement[0..200]=" +
                    $"{(result.PartialEnhancement?.Length > 200 ? result.PartialEnhancement[..200] : result.PartialEnhancement ?? "(空)")}");
            }

            var clarRound = await CountClarificationAnsweredAsync(tenantId, projectId, pipelineId, ct) + 1;
            await EmitClarificationRequestedAsync(
                pipelineId, tenantId, projectId, "requirement", clarRound, clarSet, ct);

            _logger.LogInformation(
                "RunPmPipeline 步骤③暂停追问 pipeline={PipelineId} questions={Count}",
                pipelineId, clarSet.Questions.Count);

            return new RequirementAnalysisOrchestratorResult
            {
                Status = "awaiting-clarification",
                PendingPmQuestion = result.PendingQuestion,
                PendingClarification = clarSet,
            };
        }

        // completed → 完善完成，进入步骤④（正文不进聊天主区域）
        await PersistRequirementAsync(
            pipelineId, tenantId, projectId, result.EnhancedText,
            IrEventTypes.RequirementRefined, ct);

        _logger.LogInformation(
            "RunPmPipeline 步骤③完成 pipeline={PipelineId} gaps={Gaps}",
            pipelineId, result.CompletenessNotes.Count);

        var answeredRounds = await CountClarificationAnsweredAsync(tenantId, projectId, pipelineId, ct);
        if (answeredRounds < MinPmOptimizationRounds)
        {
            var nextRound = answeredRounds + 1;
            _logger.LogInformation(
                "RunPmPipeline 澄清轮次不足({Answered}/{Min})，专用出题路径 pipeline={PipelineId} round={Round}",
                answeredRounds, MinPmOptimizationRounds, pipelineId, nextRound);
            _sseHub.TryPush(pipelineId, "thinking",
                $"📋 已完成 {answeredRounds} 轮澄清，PM 继续第 {nextRound} 轮深度优化…");
            _sseHub.TryPush(pipelineId, "thinking", "📝 PM 正在基于九步分析准备结构化追问…");

            var previousAnswers = ExtractAnswersText(
                FindRoundClarification(
                    await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct),
                    ClarificationStages.Requirement)?.Payload);

            var clarSet = await GenerateStepClarificationAsync(
                pipelineId, tenantId, projectId, context,
                compileResult, warnings, previousAnswers,
                stage: "requirement", round: nextRound, ct, forceQuestions: true);

            if (clarSet.Questions.Count == 0)
            {
                throw Oops.Bah(
                    $"RunPmPipeline 步骤③ 强制澄清轮未产出题目" +
                    $" pipeline={pipelineId} answered={answeredRounds} required={MinPmOptimizationRounds}");
            }

            await EmitClarificationRequestedAsync(
                pipelineId, tenantId, projectId, "requirement", nextRound, clarSet, ct);

            _logger.LogInformation(
                "RunPmPipeline 步骤③强制澄清暂停 pipeline={PipelineId} questions={Count}",
                pipelineId, clarSet.Questions.Count);

            return new RequirementAnalysisOrchestratorResult
            {
                Status = "awaiting-clarification",
                PendingClarification = clarSet,
            };
        }

        return await RenderSpecAndWaitConfirmAsync(
            pipelineId, tenantId, projectId, result.EnhancedText, ct);
    }

    /// <summary>步骤④：渲染需求说明书 IR + 推确认按钮（不在聊天正文灌整份 markdown）。</summary>
    private async Task<RequirementAnalysisOrchestratorResult> RenderSpecAndWaitConfirmAsync(
        long pipelineId, string tenantId, string projectId, string specText, CancellationToken ct)
    {
        await PersistRequirementAsync(
            pipelineId, tenantId, projectId, specText,
            IrEventTypes.RequirementSpecRendered, ct);

        // CR-20260717-02：用户只需确认卡片 + 交付物预览，不在正文区逐行推几百行说明书。
        _sseHub.TryPush(pipelineId, "thinking", "✅ 需求说明书已生成，请在下方确认。");
        _sseHub.TryPush(pipelineId, "spec_confirm_requested", JsonSerializer.Serialize(new
        {
            specFragmentId = $"requirement:{projectId}",
            message = "需求说明书已生成，请确认通过或提出修改意见。",
            deliverablePath = "02-requirement-spec.md",
        }, JsonOptions));

        _logger.LogInformation(
            "RunPmPipeline 步骤④渲染完成，等用户确认 pipeline={PipelineId} specLen={Len}",
            pipelineId, specText.Length);

        return new RequirementAnalysisOrchestratorResult
        {
            Status = "awaiting-spec-confirm",
            RenderedSpec = specText,
        };
    }

    /// <summary>步骤④用户反馈：PM 据反馈改需求文本 → 重跑② → 回到④。</summary>
    private async Task<RequirementAnalysisOrchestratorResult> HandleSpecFeedbackAsync(
        long pipelineId, string tenantId, string projectId,
        SkillContext context, string feedback, CancellationToken ct)
    {
        // 简化策略：把用户反馈作为新一轮完善需求，重跑步骤①②③
        // （CR-20260713-03：阶段 B 先用简化版；阶段 C 可细化反馈处理）
        var feedbackRequirement = context.UserRequirement + "\n\n【用户对需求说明书的反馈】\n" + feedback;
        var feedbackContext = CloneContextWithRequirement(context, feedbackRequirement);

        _logger.LogInformation(
            "RunPmPipeline 步骤④反馈处理 pipeline={PipelineId} 重跑步骤①", pipelineId);

        return await RunStep1EnhanceAsync(pipelineId, tenantId, projectId, feedbackContext, turns: null, ct);
    }

    /// <summary>
    /// CR-20260714-01 步骤⑤：用户确认需求说明书后，执行 PM 终评 + Analyst Finalize → 进入架构设计。
    /// 迁移自旧三轮流程的 Finalize 能力（ReviewRequirementSpecAsync + RunRoundAnalystAsync）。
    /// </summary>
    private async Task<RequirementAnalysisOrchestratorResult> RunStep5FinalizeAsync(
        long pipelineId, string tenantId, string projectId,
        SkillContext context, RequirementAnalysisOptions? options, CancellationToken ct)
    {
        var orchestratorRunId = Guid.NewGuid().ToString("N");

        // 5a. 投 RequirementSpecConfirmed 事件（标记用户已确认，防重复 Finalize）
        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.RequirementSpecConfirmed,
            FragmentId = $"requirement-confirmed:{projectId}",
            FragmentType = IrFragmentTypes.Requirement,
            FragmentVersion = 1,
            Payload = JsonSerializer.Serialize(new
            {
                pipelineId,
                confirmedBy = "user",
                timestamp = DateTimeOffset.UtcNow.ToString("O"),
            }, JsonOptions),
            SkillId = "pm-skill",
        }, ct);

        _sseHub.TryPush(pipelineId, "thinking", "📋 PM 正在进行需求终评…");

        // 5b. PM 终评（复用 ReviewRequirementSpecAsync）
        var skillOptions = new SkillRunOptions
        {
            ProviderCode = options?.ProviderCode,
            UserRequirement = options?.InitialUserRequirement,
        };
        var review = await ReviewRequirementSpecAsync(
            pipelineId, tenantId, projectId, orchestratorRunId, skillOptions, ct: ct);

        _logger.LogInformation(
            "RunStep5 PM 终评 verdict={Verdict} score={Score} pipeline={PipelineId}",
            review.Verdict, review.Score, pipelineId);

        _sseHub.TryPush(pipelineId, "thinking", "🔧 正在执行工程保障 Finalize…");

        // 5c. Analyst Finalize（复用 RunRoundAnalystAsync，enableFinalization=true）
        var finalizeResult = await RunRoundAnalystAsync(
            3, pipelineId, tenantId, projectId, skillOptions,
            enableFinalization: true, ct);

        _logger.LogInformation(
            "RunStep5 Finalize 完成 pipeline={PipelineId}", pipelineId);

        // 5d. 推进架构设计阶段
        _sseHub.TryPush(pipelineId, "stage_transition", "design");

        return new RequirementAnalysisOrchestratorResult
        {
            OrchestratorRunId = orchestratorRunId,
            Status = "completed",
            SkillResults = new List<SkillRunResult> { finalizeResult },
        };
    }

    // ── 新流程辅助方法 ──

    /// <summary>从 IR 获取用户原始需求文本。</summary>
    private async Task<string> ResolveUserRequirementAsync(
        string tenantId, string projectId, long pipelineId, CancellationToken ct)
    {
        // 简化版：从 pipeline 元数据读取；实际可从 IrSnapshot 或项目表读
        var snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);
        var reqFragment = snapshot.Find(IrFragmentTypes.Requirement, IrStabilityStates.Stable);
        if (reqFragment != null)
        {
            var text = ExtractRequirementText(reqFragment);
            if (!string.IsNullOrWhiteSpace(text)) return text;
        }
        // 降级：返回占位（实际应由调用方传入）
        return string.Empty;
    }

    /// <summary>从 Requirement fragment 提取需求文本。</summary>
    private static string? ExtractRequirementText(IrSnapshotFragment fragment)
    {
        if (string.IsNullOrWhiteSpace(fragment.Payload)) return null;
        try
        {
            using var doc = JsonDocument.Parse(fragment.Payload);
            if (doc.RootElement.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                return t.GetString();
            // 兼容：payload 直接是纯文本
            return fragment.Payload;
        }
        catch (JsonException)
        {
            return fragment.Payload;
        }
    }

    /// <summary>持久化需求文本到 IR（步骤①/③/④共用）。</summary>
    private async Task PersistRequirementAsync(
        long pipelineId, string tenantId, string projectId,
        string text, string eventType, CancellationToken ct)
    {
        var fragmentId = $"requirement:{projectId}";
        var payload = JsonSerializer.Serialize(new { text, updatedAt = DateTimeOffset.UtcNow }, JsonOptions);
        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = eventType,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.Requirement,
            FragmentVersion = 1,
            Payload = payload,
        });
    }

    /// <summary>持久化追问轮次到 IR（新流程对话式追问）。</summary>
    private async Task PersistClarificationTurnAsync(
        long pipelineId, string tenantId, string projectId,
        PmClarificationTurn turn, string? partialEnhancement, CancellationToken ct)
    {
        var fragmentId = $"pm-clarification:{projectId}";
        var payload = JsonSerializer.Serialize(new
        {
            turn = new { turn.TurnId, turn.Question, turn.QuestionReason, source = turn.Source.ToString() },
            partialEnhancement,
        }, JsonOptions);
        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = "PmClarificationRequested",
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.Clarification,
            FragmentVersion = 1,
            Payload = payload,
        });
    }

    /// <summary>从 IR 加载历史追问轮次。</summary>
    private List<PmClarificationTurn> LoadClarificationTurns(IrSnapshot snapshot)
    {
        var turns = new List<PmClarificationTurn>();
        var clarFragment = snapshot.Find(IrFragmentTypes.Clarification, IrStabilityStates.Draft)
            ?? snapshot.Find(IrFragmentTypes.Clarification, IrStabilityStates.InProgress);
        if (clarFragment == null || string.IsNullOrWhiteSpace(clarFragment.Payload)) return turns;

        try
        {
            using var doc = JsonDocument.Parse(clarFragment.Payload);
            if (doc.RootElement.TryGetProperty("turn", out var t))
            {
                var source = t.TryGetProperty("source", out var s) && s.ValueKind == JsonValueKind.String
                    ? Enum.Parse<ClarificationSource>(s.GetString()!)
                    : ClarificationSource.Step1Enhance;
                turns.Add(new PmClarificationTurn
                {
                    TurnId = t.TryGetProperty("turnId", out var id) ? id.GetString() ?? "" : "",
                    Question = t.TryGetProperty("question", out var q) ? q.GetString() ?? "" : "",
                    QuestionReason = t.TryGetProperty("questionReason", out var r) ? r.GetString() : null,
                    Source = source,
                });
            }
        }
        catch (JsonException) { /* 容错 */ }
        return turns;
    }

    /// <summary>克隆 SkillContext 并替换 UserRequirement（SkillContext 是 class 非 record，不能 with）。</summary>
    private static SkillContext CloneContextWithRequirement(SkillContext context, string userRequirement)
        => new()
        {
            RunId = context.RunId,
            TenantId = context.TenantId,
            ProjectId = context.ProjectId,
            PipelineId = context.PipelineId,
            UserRequirement = userRequirement,
            Snapshot = context.Snapshot,
            ProviderCode = context.ProviderCode,
            SeedMatches = context.SeedMatches,
            PromptContext = context.PromptContext,
            EnableFinalization = context.EnableFinalization,
            EnableSemanticAnalysis = context.EnableSemanticAnalysis,
        };

    /// <summary>持久化九步数据到 IR（给后续二次开发/BUG 修复用）。</summary>
    private async Task PersistNineViewAsync(
        long pipelineId, string tenantId, string projectId,
        SaNineViewCompileResult compileResult, CancellationToken ct)
    {
        var fragmentId = $"nine-view:{projectId}";
        var payload = JsonSerializer.Serialize(new
        {
            projectSteps = compileResult.ProjectSteps,
            eventResults = compileResult.EventResults,
            bundleHash = compileResult.BundleHash,
        }, JsonOptions);
        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.SaNineViewCompiled,
            FragmentId = fragmentId,
            FragmentType = "IR1_SaNineView",
            FragmentVersion = 1,
            Payload = payload,
        });
    }

    /// <summary>确保 skeleton 存在（复用 PM ThinkAsync 骨架生成）。</summary>
    private async Task<IrSnapshotFragment> EnsureSkeletonAsync(
        long pipelineId, string tenantId, string projectId,
        SkillContext context, CancellationToken ct)
    {
        var snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);
        var skeleton = FindSkeletonAny(snapshot);
        if (skeleton != null && skeleton.StabilityState is IrStabilityStates.Stable or IrStabilityStates.Locked)
            return skeleton;

        // 跑 PM Skill 生成骨架
        if (skeleton == null)
        {
            await _harness.RunAsync("pm-skill", pipelineId, tenantId, projectId,
                new SkillRunOptions
                {
                    ProviderCode = context.ProviderCode,
                    UserRequirement = context.UserRequirement,
                }, ct);
            snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);
            skeleton = FindSkeletonAny(snapshot);
        }

        if (skeleton == null)
            throw Oops.Bah("PM Skill 已运行但未找到骨架 fragment");

        await StabilizeSkeletonAsync(pipelineId, tenantId, projectId, skeleton, ct);
        snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);
        return snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable)
            ?? FindSkeletonAny(snapshot)
            ?? throw Oops.Bah("骨架 Stabilize 后仍无法读取");
    }

    /// <summary>从当前 skeleton 重新编译九步。</summary>
    private async Task<SaNineViewCompileResult> RecompileFromCurrentSkeletonAsync(
        long pipelineId, string tenantId, string projectId,
        SkillContext context, CancellationToken ct)
    {
        var skeleton = await EnsureSkeletonAsync(pipelineId, tenantId, projectId, context, ct);
        return _compiler.CompileFromSkeletonJson(skeleton.Payload);
    }

    private async Task<RequirementAnalysisOrchestratorResult> RunRoundAsync(
        int round, long pipelineId, string tenantId, string projectId,
        IrSnapshot snapshot, RequirementAnalysisOptions? options, CancellationToken ct)
    {
        var stage = StageForRound(round);
        var skillOptions = new SkillRunOptions
        {
            ProviderCode = options?.ProviderCode,
            UserRequirement = options?.InitialUserRequirement,
        };

        // ── Round 1 前置：若无 Stable 骨架，先跑 PM（或复用 Draft）并 Stabilize ──
        // 注意：SkeletonCreated 投影稳定性为 Draft（IrProjectionEngine.UpsertSkeletonAsync），
        // 三轮编排视澄清轮为确认，不再要求单独 confirm-skeleton HITL。
        var skeleton = snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable);
        if (round == 1 && skeleton == null)
        {
            SkillRunResult? pmResult = null;
            skeleton = FindSkeletonAny(snapshot);
            if (skeleton == null)
            {
                if (!_registry.TryGet("pm-skill", out _))
                    throw new InvalidOperationException("PM Skill 未注册，无法启动 Round 1");

                pmResult = await _harness.RunAsync(
                    "pm-skill", pipelineId, tenantId, projectId, skillOptions, ct);

                snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);
                skeleton = FindSkeletonAny(snapshot);
            }

            if (skeleton == null)
                throw Oops.Bah("PM Skill 已运行但未找到骨架 fragment（请检查 SkeletonCreated 投影）");

            await StabilizeSkeletonAsync(pipelineId, tenantId, projectId, skeleton, ct);
            snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);
            skeleton = snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable)
                ?? FindSkeletonAny(snapshot)
                ?? throw Oops.Bah("骨架 Stabilize 后仍无法读取");

            // SA 全量 C# 编译（内存，零工程步骤）
            var compileResult = _compiler.CompileFromSkeletonJson(
                skeleton.Payload, /* requirementSummary */ null);

            var warnings = _lightValidator.Validate(compileResult.Source);

            _logger.LogInformation(
                "Round 1 PM 完成 skeleton hash={Hash} events={Count} warnings={Warn}",
                compileResult.BundleHash, compileResult.EventResults.Count, warnings.Count);

            // P1：Assumptions 跨轮落 IR，暂停恢复后可重建
            await PersistAssumptionsFragmentAsync(
                pipelineId, tenantId, projectId, compileResult.Assumptions, round, ct);

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
                SkillResults = pmResult != null ? new[] { pmResult } : Array.Empty<SkillRunResult>(),
            };
        }

        // Round 1 with pre-existing Stable skeleton (e.g., from confirm-skeleton):
        // skeleton 已确认 → 直接编译 + 生成澄清题 + 暂停等用户作答。
        // 严禁在此跑 analyst（语义分析是 Round 2 的职责），否则 FinalizeAsync
        // 即使 enableFinalization=false 也会写 AnalysisCompleted → SSE 推送 →
        // 前端误判需求分析完成 → 过早弹出需求说明书推进架构设计。
        if (round == 1)
        {
            _logger.LogInformation(
                "Round 1 骨架已确认（Stable），直接编译生成澄清题 pipeline={PipelineId}", pipelineId);

            var compileResult = _compiler.CompileFromSkeletonJson(skeleton!.Payload);
            var warnings = _lightValidator.Validate(compileResult.Source);

            _logger.LogInformation(
                "Round 1 编译完成 skeleton hash={Hash} events={Count} warnings={Warn}",
                compileResult.BundleHash, compileResult.EventResults.Count, warnings.Count);

            await PersistAssumptionsFragmentAsync(
                pipelineId, tenantId, projectId, compileResult.Assumptions, round, ct);

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
            };
        }

        // ── 检查本轮澄清是否已有用户作答（stable）──
        var roundClar = FindRoundClarification(snapshot, stage);
        if (roundClar is { StabilityState: IrStabilityStates.Stable })
        {
            // P0：作答写回骨架唯一源（Typed patches），再进入下一轮 / Finalize
            await ApplyClarificationAnswersToSkeletonAsync(
                pipelineId, tenantId, projectId, snapshot, roundClar, round, ct);
            snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);

            // Round 3 专属：用户已答最终确认题 → 此时才执行工程一次性保障（确认后落库，非确认前）
            // 架构哲学（KG 记载）："Round3 确认+出3题+工程一次性保障"——确认在先，保障在后。
            // FinalizeAsync 内含：投影→门禁→Materializer→DDD→一致性→质量→渲染需求分析书。
            // CR-20260712-01：PM 终评先跑，fail 则按 ForceConfirm 决定是否 Finalize（修复门控逃逸）。
            if (round == TotalRounds)
            {
                var review = await ReviewRequirementSpecAsync(
                    pipelineId, tenantId, projectId, Guid.NewGuid().ToString("N"), skillOptions,
                    forceConfirm: options?.ForceConfirm == true, forceReason: options?.ForceReason, ct: ct);
                var allowFinalize = review.Verdict == "pass" || options?.ForceConfirm == true;
                _logger.LogWarning(
                    "Round3 PM 终评 verdict={Verdict} score={Score} allowFinalize={Allow} forceConfirm={Force} pipeline={PipelineId}",
                    review.Verdict, review.Score, allowFinalize, options?.ForceConfirm, pipelineId);
                var finalizeResult = await RunRoundAnalystAsync(
                    round, pipelineId, tenantId, projectId, skillOptions, enableFinalization: allowFinalize, ct);
                _logger.LogInformation("Round {Round} 用户已确认，PM 终评 verdict={Verdict} allowFinalize={Allow}", round, review.Verdict, allowFinalize);
                return new RequirementAnalysisOrchestratorResult
                {
                    Status = allowFinalize ? "completed" : "pm-review-failed",
                    CurrentRound = round,
                    SkillResults = new[] { finalizeResult },
                };
            }

            // Round 1/2：本轮已完成（用户已答），继续下一轮
            _logger.LogInformation("Round {Round} 已完成（用户已作答并写回骨架），继续", round);
            return new RequirementAnalysisOrchestratorResult
            {
                Status = "completed",
                CurrentRound = round,
            };
        }

        // 本轮题已发出、用户尚未作答：禁止重跑 Analyst / 刷新 setId（否则作答永远对不上最新题）
        if (roundClar is { StabilityState: IrStabilityStates.InProgress })
        {
            ClarificationSet? pending = null;
            if (!string.IsNullOrWhiteSpace(roundClar.Payload))
            {
                try
                {
                    pending = JsonSerializer.Deserialize<ClarificationSet>(roundClar.Payload, JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Round {Round} InProgress 澄清 payload 反序列化失败，仍返回 awaiting-answer", round);
                }
            }

            _logger.LogInformation(
                "Round {Round} 澄清仍为 in-progress，短路返回 awaiting-answer（不重发出题）", round);
            return new RequirementAnalysisOrchestratorResult
            {
                Status = "awaiting-answer",
                CurrentRound = round,
                PendingClarification = pending,
            };
        }

        // ── Round 2 / Round 3：需要先确认 Round N-1 已完成 ──
        if (round >= 2)
        {
            var prevStage = StageForRound(round - 1);
            var prevClar = FindRoundClarification(snapshot, prevStage);
            if (prevClar is not { StabilityState: IrStabilityStates.Stable })
                throw new InvalidOperationException($"第 {round} 轮前置未满足：第 {round - 1} 轮澄清未完成");

            // GAP1: 将上一轮澄清答案写入骨架（复用既有 PM 管线：确定性基线 + LLM 补丁）
            await ApplyClarificationAnswersToSkeletonAsync(
                pipelineId, tenantId, projectId, snapshot, prevClar, round - 1, ct);
            snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);
        }

        // SA 全量重编译（C# 毫秒级，零工程步骤）
        skeleton ??= FindSkeletonAny(snapshot);
        if (skeleton == null)
            throw Oops.Bah($"第 {round} 轮缺少需求骨架，无法继续");
        if (skeleton.StabilityState is not (IrStabilityStates.Stable or IrStabilityStates.Locked))
        {
            await StabilizeSkeletonAsync(pipelineId, tenantId, projectId, skeleton, ct);
            snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);
            skeleton = FindSkeletonAny(snapshot) ?? skeleton;
        }

        var recompile = _compiler.CompileFromSkeletonJson(skeleton.Payload);
        var roundWarnings = _lightValidator.Validate(recompile.Source);

        // 收集上一轮用户答案文本 + 已填槽位（供选问去重）
        string? prevStageForAnswers = null;
        if (round >= 2) prevStageForAnswers = StageForRound(round - 1);
        var prevClarFragment = round >= 2 ? FindRoundClarification(snapshot, prevStageForAnswers!) : null;
        var prevAnswersText = ExtractAnswersText(prevClarFragment?.Payload);
        var prevFilled = ExtractFilledSlotIds(prevClarFragment?.Payload);
        if (prevFilled.Count > 0)
            prevAnswersText = (prevAnswersText ?? string.Empty) + "\n" + string.Join("\n", prevFilled);

        if (round < TotalRounds)
        {
            // Round 2：Analyst 受控语义分析写回骨架（非仅 Compile）；编排器不再旁路 EnhancePspec
            var analystResult = await RunRoundAnalystAsync(
                round, pipelineId, tenantId, projectId, skillOptions,
                enableFinalization: false, ct,
                enableSemanticAnalysis: true,
                userRequirementOverride: prevAnswersText);

            snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);
            skeleton = FindSkeletonAny(snapshot) ?? skeleton;
            recompile = _compiler.CompileFromSkeletonJson(skeleton.Payload);
            roundWarnings = _lightValidator.Validate(recompile.Source);

            await PersistAssumptionsFragmentAsync(
                pipelineId, tenantId, projectId, recompile.Assumptions, round, ct);

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

        // ── Round 3：最终确认（工程保障在用户确认后执行，见上方 stable 分支）──
        // 先出最终确认题（遗漏检查 + 假设项确认），用户答完后重跑进入 stable 分支执行工程落库。
        // 这符合架构哲学："确认+出题" 在先，"工程一次性保障" 在后。
        await PersistAssumptionsFragmentAsync(
            pipelineId, tenantId, projectId, recompile.Assumptions, round, ct);

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
    /// <summary>
    /// 跑一轮 Analyst Skill（Round 2 语义分析 / Round 3 工程保障）。
    /// enableFinalization=false → 跳过投影/门禁/Materializer。
    /// enableSemanticAnalysis=true → Round2 在 Compile 之上做受控 LLM 分析并写回 Skeleton。
    /// </summary>
    private async Task<SkillRunResult> RunRoundAnalystAsync(
        int round, long pipelineId, string tenantId, string projectId,
        SkillRunOptions skillOptions, bool enableFinalization, CancellationToken ct,
        bool enableSemanticAnalysis = false,
        string? userRequirementOverride = null)
    {
        _logger.LogInformation(
            "Round {Round} Analyst 启动 enableFinalization={Fin} enableSemanticAnalysis={Sem}",
            round, enableFinalization, enableSemanticAnalysis);
        var runOptions = new SkillRunOptions
        {
            UserRequirement = userRequirementOverride ?? skillOptions.UserRequirement,
            ProviderCode = skillOptions.ProviderCode,
            ArchGuardWarnings = skillOptions.ArchGuardWarnings,
            Bugfix = skillOptions.Bugfix,
            EnableFinalization = enableFinalization,
            EnableSemanticAnalysis = enableSemanticAnalysis,
        };
        return await _harness.RunAsync(
            "analyst-skill", pipelineId, tenantId, projectId, runOptions, ct);
    }

    private async Task<PmSpecReviewResult> ReviewRequirementSpecAsync(
        long pipelineId,
        string tenantId,
        string projectId,
        string orchestratorRunId,
        SkillRunOptions skillOptions,
        bool forceConfirm = false,
        string? forceReason = null,
        CancellationToken ct = default)
    {
        var snapshots = await _eventStore.ListSnapshotsAsync(projectId, tenantId, pipelineId.ToString(), ct);
        var requirementSpecMarkdown = BuildRequirementSpecReviewInput(snapshots);
        var context = new SkillContext
        {
            RunId = orchestratorRunId,
            TenantId = tenantId,
            ProjectId = projectId,
            PipelineId = pipelineId,
            UserRequirement = skillOptions.UserRequirement ?? string.Empty,
            ProviderCode = skillOptions.ProviderCode,
        };

        SaNineViewCompileResult? compileResult = null;
        var skeleton = snapshots.FirstOrDefault(s =>
            s.FragmentType == IrFragmentTypes.Skeleton && s.StabilityState == IrStabilityStates.Stable);
        if (skeleton != null)
        {
            try
            {
                var skeletonJson = skeleton.Payload switch
                {
                    JsonElement je when je.ValueKind == JsonValueKind.Object => je.GetRawText(),
                    JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString() ?? "{}",
                    string s => s,
                    null => "{}",
                    _ => JsonSerializer.Serialize(skeleton.Payload, JsonOptions),
                };
                // 双重引号包裹的 JSON 字符串
                if (skeletonJson.Length >= 2 && skeletonJson[0] == '"' && skeletonJson[^1] == '"')
                {
                    skeletonJson = JsonSerializer.Deserialize<string>(skeletonJson) ?? "{}";
                }
                if (!skeletonJson.TrimStart().StartsWith('{'))
                {
                    _logger.LogWarning(
                        "PM 终评跳过 compile：骨架 payload 非 JSON pipeline={PipelineId} preview={Preview}",
                        pipelineId, skeletonJson.Length > 80 ? skeletonJson[..80] : skeletonJson);
                }
                else
                {
                    compileResult = _compiler.CompileFromSkeletonJson(skeletonJson);
                }
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentException)
            {
                throw Oops.Bah(
                    $"PM 终评骨架编译失败（IR 骨架 JSON 损坏）: {ex.Message}" +
                    $" pipeline={pipelineId} tenantId={tenantId}", ex);
            }
        }

        var review = await _pm.ReviewSpecAsync(context, requirementSpecMarkdown, ct, compileResult);
        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.RequirementSpecPmReviewed,
            FragmentId = $"requirement-spec-review:{projectId}",
            FragmentType = IrFragmentTypes.EventSpec,
            FragmentVersion = 1,
            Payload = JsonSerializer.Serialize(new
            {
                score = review.Score,
                verdict = review.Verdict,
                gaps = review.Gaps,
                gapDetails = review.GapDetails,
                threshold = 85,
                pipelineId,
                reviewedBy = "pm-skill",
                forceConfirm,
                forceReason = forceConfirm ? forceReason : null,
            }, JsonOptions),
            SkillId = "pm-skill",
        }, ct);

        _logger.LogInformation(
            "PM 终评完成 pipeline={PipelineId} score={Score} verdict={Verdict} gaps={GapCount}",
            pipelineId, review.Score, review.Verdict, review.Gaps.Count);
        return review;
    }

    private static string BuildRequirementSpecReviewInput(IReadOnlyList<IrFragmentSnapshotDto> snapshots)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# 需求分析说明书快照");
        foreach (var snap in snapshots
            .Where(s => s.FragmentType == IrFragmentTypes.EventSpec)
            .OrderBy(s => s.FragmentId, StringComparer.Ordinal))
        {
            sb.AppendLine();
            sb.AppendLine($"## {snap.FragmentId}");
            sb.AppendLine(snap.Payload is string s ? s : JsonSerializer.Serialize(snap.Payload, JsonOptions));
        }

        return sb.ToString();
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
            SkillId = "pm-skill",
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
        return await _pm.GenerateClarificationAsync(
            round, stage, tenantId, projectId, pipelineId, compileResult, warnings, previousAnswersText, ct);
    }

    /// <summary>
    /// CR-20260713-03：新流程步骤①③的出题封装。
    /// 复用 PM GenerateClarificationAsync 产出 MULTI/MATRIX 题；
    /// compileResult 为 null 时(步骤①尚未九步拆解)用空 compileResult 兜底。
    /// </summary>
    private async Task<ClarificationSet> GenerateStepClarificationAsync(
        long pipelineId, string tenantId, string projectId,
        SkillContext context,
        SaNineViewCompileResult? compileResult,
        IReadOnlyList<string> warnings,
        string? previousAnswersText,
        string stage, int round,
        CancellationToken ct,
        bool forceQuestions = false)
    {
        // 步骤①尚未九步拆解，构造空 compileResult 兜底（PM 出题靠需求文本+种子，不强依赖九步）
        var effectiveCompile = compileResult ?? BuildEmptyCompileResult(context.UserRequirement);

        return await _pm.GenerateClarificationAsync(
            round, stage, tenantId, projectId, pipelineId,
            effectiveCompile, warnings, previousAnswersText, ct, forceQuestions);
    }

    private Func<string, CancellationToken, Task> CreateStreamingTokenHandler(
        long pipelineId, string heartbeatMessage)
    {
        var lastHeartbeat = DateTime.UtcNow;
        return (token, _) =>
        {
            if (!string.IsNullOrEmpty(token))
                _sseHub.TryPush(pipelineId, "token", token);

            var now = DateTime.UtcNow;
            if (now - lastHeartbeat >= StreamingHeartbeatInterval)
            {
                lastHeartbeat = now;
                _sseHub.TryPush(pipelineId, "thinking", heartbeatMessage);
            }

            return Task.CompletedTask;
        };
    }

    private async Task<T> RunWithThinkingHeartbeatAsync<T>(
        long pipelineId, string heartbeatMessage, CancellationToken ct,
        Func<CancellationToken, Task<T>> work)
    {
        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var heartbeatTask = PushPeriodicThinkingAsync(pipelineId, heartbeatMessage, heartbeatCts.Token);
        try
        {
            return await work(heartbeatCts.Token);
        }
        finally
        {
            heartbeatCts.Cancel();
            try { await heartbeatTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { /* expected */ }
        }
    }

    private async Task PushPeriodicThinkingAsync(
        long pipelineId, string message, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(StreamingHeartbeatInterval, ct).ConfigureAwait(false);
                _sseHub.TryPush(pipelineId, "thinking", message);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // heartbeat cancelled when work completes
        }
    }

    private static string? BuildPreviousAnswersText(IReadOnlyList<PmClarificationTurn>? turns)
    {
        if (turns == null || turns.Count == 0) return null;
        var answered = turns
            .Where(t => !string.IsNullOrWhiteSpace(t.UserAnswer))
            .ToList();
        if (answered.Count == 0) return null;
        return string.Join("\n\n", answered.Select(t => $"Q: {t.Question}\nA: {t.UserAnswer}"));
    }

    /// <summary>构造空 compileResult（步骤①出题兜底，PM 靠需求文本+种子出题）。</summary>
    private static SaNineViewCompileResult BuildEmptyCompileResult(string userRequirement)
    {
        var emptyModel = new PreAnalysisModel
        {
            BusinessEvents = new List<PreAnalysisBusinessEvent>
            {
                new() { EventId = "EV-PLACEHOLDER", EventName = "待拆解", ComplexityHint = "simple" },
            },
            EntityDrafts = new List<PreAnalysisEntityDraft>(),
            BusinessRules = new List<PreAnalysisBusinessRule>(),
            StateTransitions = new List<PreAnalysisStateTransition>(),
            RoleMatrix = new PreAnalysisRoleMatrix
            {
                Roles = new List<string>(),
            },
        };
        return new SaNineViewCompileResult
        {
            Source = emptyModel,
            ProjectSteps = new Dictionary<string, object>(),
            EventResults = new List<SaEventResult>(),
            CompileDurationMs = 0,
            BundleHash = "empty",
            Assumptions = new List<Assumption>(),
        };
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

        var (mergedEvents, extraAssumptions, mergeWarnings) =
            MergePspecEnhancements(compileResult.EventResults, response.Content);
        foreach (var w in mergeWarnings)
            _logger.LogWarning("PSpec/DT 合并校验: {Warning}", w);

        var mergedAssumptions = compileResult.Assumptions.ToList();
        mergedAssumptions.AddRange(extraAssumptions);

        return new SaNineViewCompileResult
        {
            Source = compileResult.Source,
            ProjectSteps = compileResult.ProjectSteps,
            EventResults = mergedEvents,
            CompileDurationMs = compileResult.CompileDurationMs,
            BundleHash = compileResult.BundleHash,
            Assumptions = mergedAssumptions,
        };
    }

    /// <summary>
    /// 将 LLM 精化合并进 EventResults：只追加允许字段；
    /// 若 LLM 试图覆盖主体键 → 忽略并 WARNING。
    /// </summary>
    private static (IReadOnlyList<SaEventResult> Events, List<Assumption> Assumptions, List<string> Warnings)
        MergePspecEnhancements(IReadOnlyList<SaEventResult> eventResults, string content)
    {
        var assumptions = new List<Assumption>();
        var warnings = new List<string>();
        var forbiddenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "processSpecs", "tables", "source", "main_logic", "input_spec", "output_spec",
            "conditions", "actions", "rules",
        };

        try
        {
            var json = ExtractJsonObject(content);
            using var doc = JsonDocument.Parse(json);
            var rebuilt = new List<SaEventResult>();

            foreach (var evt in eventResults)
            {
                if (!doc.RootElement.TryGetProperty(evt.EventId, out var enh)
                    || enh.ValueKind != JsonValueKind.Object)
                {
                    rebuilt.Add(evt);
                    continue;
                }

                foreach (var prop in enh.EnumerateObject())
                {
                    if (forbiddenKeys.Contains(prop.Name))
                        warnings.Add($"事件 {evt.EventId}: LLM 试图修改主体字段「{prop.Name}」，已忽略");
                    if (prop.Value.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var nested in prop.Value.EnumerateObject())
                        {
                            if (forbiddenKeys.Contains(nested.Name))
                                warnings.Add($"事件 {evt.EventId}: LLM 试图修改主体字段「{nested.Name}」，已忽略");
                        }
                    }
                }

                var steps = evt.Steps.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);

                if (enh.TryGetProperty("pspec", out var pspec) && pspec.ValueKind == JsonValueKind.Object)
                {
                    steps[SaStepNames.IntegrationPoints] = MergeStepJson(
                        steps.GetValueOrDefault(SaStepNames.IntegrationPoints),
                        pspec, new[] { "boundaries", "exceptions" }, warnings, evt.EventId, "PSpec");

                    if (pspec.TryGetProperty("boundaries", out var b) && b.ValueKind == JsonValueKind.Array)
                        foreach (var item in b.EnumerateArray())
                            if (item.ValueKind == JsonValueKind.String)
                                assumptions.Add(new Assumption(evt.EventId, "PSpec", $"边界: {item.GetString()}", 0.7m));
                    if (pspec.TryGetProperty("exceptions", out var ex) && ex.ValueKind == JsonValueKind.Array)
                        foreach (var item in ex.EnumerateArray())
                            if (item.ValueKind == JsonValueKind.String)
                                assumptions.Add(new Assumption(evt.EventId, "PSpec", $"异常: {item.GetString()}", 0.7m));
                }

                if (enh.TryGetProperty("decisionTable", out var dt) && dt.ValueKind == JsonValueKind.Object)
                {
                    steps[SaStepNames.WorkflowSpec] = MergeStepJson(
                        steps.GetValueOrDefault(SaStepNames.WorkflowSpec),
                        dt, new[] { "preconditions", "edge_cases" }, warnings, evt.EventId, "DecisionTable");

                    if (dt.TryGetProperty("preconditions", out var pc) && pc.ValueKind == JsonValueKind.Array)
                        foreach (var item in pc.EnumerateArray())
                            if (item.ValueKind == JsonValueKind.String)
                                assumptions.Add(new Assumption(evt.EventId, "DecisionTable", $"前置: {item.GetString()}", 0.7m));
                    if (dt.TryGetProperty("edge_cases", out var ec) && ec.ValueKind == JsonValueKind.Array)
                        foreach (var item in ec.EnumerateArray())
                            if (item.ValueKind == JsonValueKind.String)
                                assumptions.Add(new Assumption(evt.EventId, "DecisionTable", $"边界场景: {item.GetString()}", 0.7m));
                }

                rebuilt.Add(new SaEventResult
                {
                    EventId = evt.EventId,
                    EventName = evt.EventName,
                    Complexity = evt.Complexity,
                    Steps = steps,
                    Error = evt.Error,
                });
            }

            return (rebuilt, assumptions, warnings);
        }
        catch (Exception)
        {
            warnings.Add("PSpec/DT LLM JSON 解析失败，跳过合并");
            return (eventResults, ParsePspecEnhancementsAsAssumptions(content), warnings);
        }
    }

    /// <summary>将允许的追加字段合并进步骤 JSON（主体键保留，仅追加 allowedKeys）。</summary>
    private static object MergeStepJson(
        object? existing, JsonElement enhancement, string[] allowedKeys,
        List<string> warnings, string eventId, string stepLabel)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (existing != null)
            {
                var raw = existing is JsonElement je
                    ? je.GetRawText()
                    : JsonSerializer.Serialize(existing, JsonOptions);
                using var doc = JsonDocument.Parse(raw);
                foreach (var p in doc.RootElement.EnumerateObject())
                    dict[p.Name] = JsonSerializer.Deserialize<object>(p.Value.GetRawText());
            }
        }
        catch (JsonException)
        {
            warnings.Add($"事件 {eventId}: {stepLabel} 主体 JSON 解析失败，仅保留 LLM 追加字段");
        }

        foreach (var key in allowedKeys)
        {
            if (enhancement.TryGetProperty(key, out var val))
                dict[key] = JsonSerializer.Deserialize<object>(val.GetRawText());
        }

        return dict;
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
                                result.Add(new Assumption(eventId, "DecisionTable", $"边界场景: {item.GetString()}", 0.7m));
                }
            }
        }
        catch (Exception) { /* 解析失败→返回空，不阻断 */ }
        return result;
    }

    /// <summary>P1：将当前轮 Assumptions 写入 IR fragment，供暂停恢复后合并。</summary>
    private async Task PersistAssumptionsFragmentAsync(
        long pipelineId, string tenantId, string projectId,
        IReadOnlyList<Assumption> assumptions, int round, CancellationToken ct)
    {
        if (assumptions.Count == 0) return;

        var fragmentId = $"assumptions:{projectId}";
        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.AssumptionsCollected,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.Assumptions,
            FragmentVersion = round,
            Payload = JsonSerializer.Serialize(new
            {
                round,
                pipelineId,
                assumptions = assumptions.Select(a => new
                {
                    eventId = a.EventId,
                    sourceStep = a.SourceStep,
                    text = a.Text,
                    confidence = a.Confidence,
                }),
            }, JsonOptions),
            SkillId = "requirement-analysis-orchestrator",
        }, ct);

        _logger.LogInformation(
            "Round {Round} Assumptions 已落 IR count={Count} pipelineId={Id}",
            round, assumptions.Count, pipelineId);
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
                      - 如果问题是「对多个事件做同一维度的决策」（如"每个事件的审批策略是什么？"），必须使用 matrixSubItems 格式：
                        输出 questionFormat: "MATRIX_SINGLE"（每行单选）或 "MATRIX_MULTI"（每行多选），
                        并输出 matrixSubItems 数组，每元素 {"rowId":"event_xxx","rowLabel":"事件名"}
                        单事件/实体的问题使用 questionFormat: "SINGLE" 或 "MULTI"
                    输出 JSON 数组，每元素：
                    {"text","questionFormat","contextHint","defaultOption","matrixSubItems":[{"rowId","rowLabel"}],"options":["...","其他"]}
                    只输出 JSON，不要 markdown。
                    """),
            2 => ("深度精化确认",
                  "系统需求分析师已完成深度分析（含 PSpec/DecisionTable 增强），请确认以下边界条件与业务规则。",
                  """
                    你是产品经理 + 系统需求分析师联合体。
                    基于用户上一轮的回答与 SA 深度分析（PSpec/DecisionTable），判断分析中发现的遗漏/冲突/假设中，
                    最重要的 3 个需要用户裁决的点是什么？
                    规则：每个问题聚焦一个决策点（边界条件/异常路径/业务规则冲突）。
                    矩阵规则（同 Round 1）：
                      如果问题覆盖 2+ 个事件/实体的同一决策维度 → 使用 questionFormat "MATRIX_SINGLE"/"MATRIX_MULTI"
                      并输出 matrixSubItems 数组；单事件/实体 → "SINGLE"/"MULTI"
                    输出 JSON 数组，每元素：
                    {"text","questionFormat","contextHint","defaultOption","matrixSubItems":[{"rowId","rowLabel"}],"options":["...","其他"]}
                    只输出 JSON。
                    """),
            3 => ("最终遗漏检查",
                  "这是最后一轮确认，请核对以下推导假设与遗漏点。全部跳过可直接定稿。",
                  """
                    你是最终审查专家。
                    任务：检查全部三轮分析后，还有遗漏吗？推导的假设项（assumptions）中哪些需要用户确认？
                    规则：最多发现 3 个遗漏/待确认假设；如无遗漏，返回空数组 []。
                    矩阵规则（同 Round 1）：
                      如果问题覆盖 2+ 个事件/实体的同一决策维度 → 使用 questionFormat "MATRIX_SINGLE"/"MATRIX_MULTI"
                      并输出 matrixSubItems 数组；单事件/实体 → "SINGLE"/"MULTI"
                    输出 JSON 数组，每元素：
                    {"text","questionFormat","contextHint","defaultOption","matrixSubItems":[{"rowId","rowLabel"}],"options":["...","其他"]}
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

                var format = "SINGLE";
                if (el.TryGetProperty("format", out var fmtEl) && fmtEl.ValueKind == JsonValueKind.String)
                    format = (fmtEl.GetString() ?? "SINGLE").ToUpperInvariant();
                else if (el.TryGetProperty("questionFormat", out var qfEl) && qfEl.ValueKind == JsonValueKind.String)
                    format = (qfEl.GetString() ?? "SINGLE").ToUpperInvariant();

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
                    if (matrixItems.Count > 0 && format is "SINGLE" or "MULTI")
                        format = format == "MULTI" ? "MATRIX_MULTI" : "MATRIX_SINGLE";
                }

                var qType = format switch
                {
                    "MULTI" or "MATRIX_MULTI" => "multi",
                    "TEXT" => "text",
                    _ => "single",
                };

                var q = new ClarificationQuestion
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

    /// <summary>
    /// P9 兜底：LLM 未产出矩阵格式，但问题文本覆盖 ≥2 个事件名 → 自动合成矩阵行并升级 QuestionFormat。
    /// 这是 LLM 不听话时的保险——即便 prompt 已指令矩阵格式，LLM 仍可能忽略。
    /// </summary>
    private static void ApplyMatrixFallback(List<ClarificationQuestion> questions, SaNineViewCompileResult compileResult)
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

            // 已有矩阵格式的跳过
            if (q.MatrixSubItems is { Count: > 0 }) continue;
            // 非 SINGLE/MULTI 格式的跳过（text 型不适用矩阵）
            if (q.QuestionFormat is not ("SINGLE" or "MULTI")) continue;

            // 检查问题文本中包含哪些事件名（OrdinalIgnoreCase 容错 LLM 大小写不一致）
            var matchedEvents = eventNames
                .Where(en => q.Text.Contains(en, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchedEvents.Count < 2) continue;

            // 为每个匹配的事件创建矩阵行（record 的 init-only 属性需用 with 表达式替换）
            var matrixItems = matchedEvents.Select((en, j) => new MatrixSubItem
            {
                RowId = $"evt-{j + 1}",
                RowLabel = en,
            }).ToList();

            questions[i] = q with
            {
                MatrixSubItems = matrixItems,
                QuestionFormat = q.QuestionFormat == "MULTI" ? "MATRIX_MULTI" : "MATRIX_SINGLE",
            };
        }
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

    /// <summary>是否已有 AnalysisCompleted 且 finalized=true（Round 3 工程保障已执行）。</summary>
    private async Task<bool> HasFinalizedEngineeringAsync(
        string tenantId, string projectId, long pipelineId, CancellationToken ct)
    {
        var events = await _eventStore.ListEventsAsync(projectId, tenantId, pipelineId.ToString(), ct);
        foreach (var evt in events)
        {
            if (!string.Equals(evt.EventType, IrEventTypes.AnalysisCompleted, StringComparison.Ordinal))
                continue;
            var payload = evt.PayloadPreview;
            if (string.IsNullOrWhiteSpace(payload)) continue;
            try
            {
                using var doc = JsonDocument.Parse(payload);
                if (doc.RootElement.TryGetProperty("finalized", out var f)
                    && f.ValueKind == JsonValueKind.True)
                    return true;
            }
            catch (JsonException) { /* 忽略坏 payload */ }
        }
        return false;
    }

    /// <summary>
    /// CR-20260714-01：检查 IR 事件流中是否存在指定事件类型。
    /// </summary>
    private async Task<bool> HasEventAsync(
        string tenantId, string projectId, long pipelineId, string eventType, CancellationToken ct)
    {
        var events = await _eventStore.ListEventsAsync(projectId, tenantId, pipelineId.ToString(), ct);
        return events.Any(e => string.Equals(e.EventType, eventType, StringComparison.Ordinal));
    }

    private async Task<int> CountClarificationAnsweredAsync(
        string tenantId, string projectId, long pipelineId, CancellationToken ct)
    {
        var events = await _eventStore.ListEventsAsync(projectId, tenantId, pipelineId.ToString(), ct);
        return events.Count(e =>
            string.Equals(e.EventType, IrEventTypes.ClarificationAnswered, StringComparison.Ordinal));
    }

    private static bool HasPendingClarification(IrSnapshot snapshot)
        => snapshot.Find(IrFragmentTypes.Clarification, IrStabilityStates.InProgress) != null;

    /// <summary>
    /// 步骤④前置条件：步骤③ RequirementRefined + 九步 stable + ≥MinPmOptimizationRounds 轮 ClarificationAnswered + 无在途追问。
    /// </summary>
    private async Task<bool> IsReadyForSpecDeliveryAsync(
        string tenantId, string projectId, long pipelineId, IrSnapshot snapshot, CancellationToken ct)
    {
        if (HasPendingClarification(snapshot)) return false;
        if (!await HasEventAsync(tenantId, projectId, pipelineId, IrEventTypes.RequirementRefined, ct))
            return false;
        if (snapshot.Find("IR1_SaNineView", IrStabilityStates.Stable) == null) return false;
        var answered = await CountClarificationAnsweredAsync(tenantId, projectId, pipelineId, ct);
        return answered >= MinPmOptimizationRounds;
    }

    /// <summary>
    /// CR-20260714-01 改动5：PM 智能意图判断 — 根据用户输入文本 + 当前 IR 状态判断意图。
    /// 启发式规则（轻量，不调 LLM）：基于当前状态(待确认/待追问) + 文本特征判断。
    /// </summary>
    private static (string Intent, double Confidence) ClassifyUserIntent(
        string userInput, IrSnapshot snapshot)
    {
        var trimmed = userInput.Trim();

        // 检查当前状态
        var hasSpecRendered = snapshot.Find(IrFragmentTypes.Requirement, IrStabilityStates.Stable) != null;
        var hasPendingClarification = snapshot.Find(IrFragmentTypes.Clarification, IrStabilityStates.InProgress) != null;

        // 确认意图关键词（用户确认需求说明书）
        var confirmKeywords = new[] { "确认", "通过", "没问题", "可以", "同意", "ok", "OK", "好的", "行", "同意" };
        if (hasSpecRendered && confirmKeywords.Any(k => trimmed.Equals(k, StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith(k, StringComparison.OrdinalIgnoreCase)))
        {
            return ("confirm_spec", 0.9);
        }

        // 修改意图关键词（用户要改需求）
        var changeKeywords = new[] { "修改", "改", "不对", "调整", "增加", "删除", "换", "不要", "改成" };
        if (hasSpecRendered && changeKeywords.Any(k => trimmed.Contains(k, StringComparison.OrdinalIgnoreCase)))
        {
            return ("request_change", 0.85);
        }

        // 回答追问意图（当前有 pending clarification）
        if (hasPendingClarification)
        {
            return ("answer_question", 0.8);
        }

        // 默认：有 specRendered 时，短文本倾向确认，长文本倾向修改
        if (hasSpecRendered)
        {
            return trimmed.Length <= 10 ? ("confirm_spec", 0.6) : ("request_change", 0.6);
        }

        return ("unknown", 0.3);
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

    private static IReadOnlyList<string> ExtractFilledSlotIds(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return Array.Empty<string>();
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.TryGetProperty("filledSlotIds", out var arr)
                && arr.ValueKind == JsonValueKind.Array)
            {
                return arr.EnumerateArray()
                    .Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetString()!)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();
            }
        }
        catch (JsonException) { /* ignore */ }
        return Array.Empty<string>();
    }

    /// <summary>
    /// PM 作为完善主体：澄清作答 → RefineSkeleton Typed patches → 写回 Skeleton。
    /// LLM 失败时仍应用确定性槽位补丁（基线），不阻断三轮。
    /// </summary>
    private async Task ApplyClarificationAnswersToSkeletonAsync(
        long pipelineId, string tenantId, string projectId,
        IrSnapshot snapshot, IrSnapshotFragment roundClar, int round, CancellationToken ct)
    {
        var answersText = ExtractAnswersText(roundClar.Payload);
        var filledSlots = ExtractFilledSlotIds(roundClar.Payload).ToList();
        if (filledSlots.Count == 0)
            filledSlots = ClarificationAnswerPatchMapper.DetectFilledSlots(answersText).ToList();

        if (string.IsNullOrWhiteSpace(answersText) && filledSlots.Count == 0)
        {
            _logger.LogInformation(
                "Round {Round} 无可用澄清答案，跳过 PM 完善 pipeline={PipelineId}", round, pipelineId);
            return;
        }

        var skeleton = FindSkeletonAny(snapshot);
        if (skeleton == null)
        {
            _logger.LogWarning(
                "Round {Round} 无骨架，无法 PM 完善 pipeline={PipelineId}", round, pipelineId);
            return;
        }

        if (skeleton.StabilityState != IrStabilityStates.Stable
            && skeleton.StabilityState != IrStabilityStates.Locked)
        {
            await StabilizeSkeletonAsync(pipelineId, tenantId, projectId, skeleton, ct);
            snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);
            skeleton = FindSkeletonAny(snapshot) ?? skeleton;
        }

        var skeletonJson = ResolveSkeletonPayloadJson(skeleton.Payload);
        if (!skeletonJson.TrimStart().StartsWith('{'))
        {
            _logger.LogWarning(
                "Round {Round} 骨架非 JSON，跳过 PM 完善 pipeline={PipelineId}", round, pipelineId);
            return;
        }

        var context = new SkillContext
        {
            RunId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            ProjectId = projectId,
            PipelineId = pipelineId,
            UserRequirement = answersText,
        };
        var patches = await _pm.RefineSkeletonFromClarificationAsync(
            context, skeletonJson, answersText, filledSlots, ct);
        if (patches.Count == 0)
            return;

        var patched = AmendmentPatchApplier.ApplyToSkeletonJson(skeletonJson, patches);
        if (string.Equals(patched, skeletonJson, StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Round {Round} PM 完善未改变骨架（已幂等） pipeline={PipelineId}", round, pipelineId);
            return;
        }

        var compile = _compiler.CompileFromSkeletonJson(patched);
        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.SkeletonCreated,
            FragmentId = skeleton.FragmentId,
            FragmentType = IrFragmentTypes.Skeleton,
            Payload = patched,
            SkillId = "pm-skill",
        }, ct);
        // SkeletonCreated 会把投影打回 Draft，必须立刻 Stabilize 供下游 Find(Stable)
        await StabilizeSkeletonAsync(pipelineId, tenantId, projectId, skeleton, ct);

        _logger.LogInformation(
            "Round {Round} PM 完善已写回骨架 pipeline={PipelineId} patchCount={PatchCount} filledSlots={Slots} bundleHash={Hash}",
            round, pipelineId, patches.Count, string.Join(",", filledSlots), compile.BundleHash);
    }

    /// <summary>查找任意稳定性骨架（Stable → InProgress → Draft）。</summary>
    private static IrSnapshotFragment? FindSkeletonAny(IrSnapshot snapshot)
        => snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable)
           ?? snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.InProgress)
           ?? snapshot.Find(IrFragmentTypes.Skeleton);

    /// <summary>三轮编排内自动 Stabilize 骨架（等价于 confirm-skeleton，避免 Draft 空引用）。</summary>
    private async Task StabilizeSkeletonAsync(
        long pipelineId, string tenantId, string projectId,
        IrSnapshotFragment skeleton, CancellationToken ct)
    {
        if (skeleton.StabilityState is IrStabilityStates.Stable or IrStabilityStates.Locked)
            return;

        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.FragmentStabilized,
            FragmentId = skeleton.FragmentId,
            FragmentType = IrFragmentTypes.Skeleton,
            Payload = JsonSerializer.Serialize(new
            {
                fragmentId = skeleton.FragmentId,
                stabilityState = IrStabilityStates.Stable,
                confirmedBy = "requirement-analysis-orchestrator",
                pipelineId,
            }, JsonOptions),
            SkillId = "requirement-analysis-orchestrator",
        }, ct);

        _logger.LogInformation(
            "骨架已 Stabilize fragment={FragmentId} pipeline={PipelineId}",
            skeleton.FragmentId, pipelineId);
    }

    private static string ResolveSkeletonPayloadJson(object? payload)
    {
        return payload switch
        {
            string s => UnwrapJsonString(s),
            JsonElement je when je.ValueKind == JsonValueKind.Object => je.GetRawText(),
            JsonElement je when je.ValueKind == JsonValueKind.String => UnwrapJsonString(je.GetString() ?? "{}"),
            null => "{}",
            _ => UnwrapJsonString(JsonSerializer.Serialize(payload, JsonOptions)),
        };
    }

    private static string UnwrapJsonString(string raw)
    {
        var skeletonJson = raw?.Trim() ?? "{}";
        if (skeletonJson.Length >= 2 && skeletonJson[0] == '"' && skeletonJson[^1] == '"')
        {
            try
            {
                skeletonJson = JsonSerializer.Deserialize<string>(skeletonJson) ?? "{}";
            }
            catch (JsonException) { /* keep raw */ }
        }
        return skeletonJson;
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
