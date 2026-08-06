using System.Collections.Concurrent;
using System.IO;
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
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Runtime;
using JNPF.InteAssistant.Sa;
using JNPF.InteAssistant.Studio;
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

    /// <summary>从当前 IR 重新渲染并落盘 02-requirement-spec.md（预览/下载前刷新用）。</summary>
    Task<RequirementSpecRefreshResult> RefreshSpecDeliverableAsync(
        long pipelineId, string tenantId, string projectId, CancellationToken ct = default);

    /// <summary>刷新并返回正式版 Markdown 全文（预览专用，不落盘二次读）。</summary>
    Task<RequirementSpecRefreshResult> GetRequirementSpecContentAsync(
        long pipelineId, string tenantId, string projectId, CancellationToken ct = default);
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
    /// [已废止] 历史兼容字段；RunAsync 已固定走 RunPmPipelineAsync，此开关无效果。
    /// </summary>
    [Obsolete("新 PM 流程已是唯一主链，无需再传 UseNewPipeline")]
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

    /// <summary>[已废止] 历史兼容字段，RunAsync 已固定走新 PM 主链。</summary>
    [Obsolete("无需再传 UseNewPipeline")]
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
    /// <summary>S2 正式交付物相对路径（PipelineDeliverableService 唯一源）。</summary>
    public const string FormalSpecRelativePath = "02-requirement-spec.md";

    /// <summary>正式版说明书封面标题（RequirementDocumentRenderer）。</summary>
    public const string FormalSpecTitleMarker = "# 需求分析规格说明书";

    /// <summary>正式版说明书 CTA 固定文本（RequirementDocumentRenderer.RenderConfirmCta）。</summary>
    public const string FormalSpecCtaMarker = "请你确认需求分析说明书";

    /// <summary>SA 门控通过后落盘的合并需求（PipelineDeliverableService 唯一源）。</summary>
    private const string MergedGateRequirementRelativePath = "00-merged-requirement.md";

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
    private readonly IPipelineDeliverableService? _deliverables;
    private readonly IRequirementDocumentRenderer? _documentRenderer;
    private readonly IDddProjection? _dddProjection;
    private readonly IRequirementSpecStateResolver? _specResolver;
    private readonly IPipelineS2ProgressStore? _progressStore;

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
        RequirementGateService? gate = null,
        IPipelineDeliverableService? deliverables = null,
        IRequirementDocumentRenderer? documentRenderer = null,
        IDddProjection? dddProjection = null,
        IRequirementSpecStateResolver? specResolver = null,
        IPipelineS2ProgressStore? progressStore = null)
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
        _deliverables = deliverables;
        _documentRenderer = documentRenderer;
        _dddProjection = dddProjection;
        _specResolver = specResolver;
        _progressStore = progressStore;
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

            // ══ CR-20260714-01：新 4 步 PM 流程为唯一主链 ══
            if (options?.ForceRefinalize == true)
            {
                _logger.LogInformation(
                    "ForceRefinalize 运维回填 pipeline={PipelineId}", pipelineId);
                var skillOptions = await BuildFinalizeSkillRunOptionsAsync(
                    options, context: null, tenantId, projectId, pipelineId, ct);
                var review = await ReviewRequirementSpecAsync(
                    pipelineId, tenantId, projectId, orchestratorRunId, skillOptions,
                    forceConfirm: options?.ForceConfirm == true, forceReason: options?.ForceReason, ct: ct);
                var allowFinalize = review.Verdict == "pass" || options?.ForceConfirm == true;
                _logger.LogWarning(
                    "ForceRefinalize PM 终评 verdict={Verdict} score={Score} allowFinalize={Allow} pipeline={PipelineId}",
                    review.Verdict, review.Score, allowFinalize, pipelineId);
                var finalizeResult = await RunRoundAnalystAsync(
                    TotalRounds, pipelineId, tenantId, projectId, skillOptions,
                    enableFinalization: allowFinalize, ct);
                return new RequirementAnalysisOrchestratorResult
                {
                    OrchestratorRunId = orchestratorRunId,
                    Status = allowFinalize ? "completed" : "pm-review-failed",
                    CurrentRound = TotalRounds,
                    SkillResults = new List<SkillRunResult> { finalizeResult },
                };
            }

            _logger.LogInformation(
                "RequirementAnalysis 编排器启动 pipeline={PipelineId}", pipelineId);
            var newResult = await RunPmPipelineAsync(pipelineId, tenantId, projectId, options, ct);
            return new RequirementAnalysisOrchestratorResult
            {
                OrchestratorRunId = orchestratorRunId,
                Status = newResult.Status,
                PendingPmQuestion = newResult.PendingPmQuestion,
                PendingClarification = newResult.PendingClarification,
                RenderedSpec = newResult.RenderedSpec,
                SkillResults = newResult.SkillResults,
                ErrorMessage = newResult.ErrorMessage,
                GateHint = newResult.GateHint,
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

    /// <inheritdoc />
    public Task<RequirementSpecRefreshResult> RefreshSpecDeliverableAsync(
        long pipelineId, string tenantId, string projectId, CancellationToken ct = default)
        => RenderAndSaveFormalSpecAsync(pipelineId, tenantId, projectId, includeMarkdown: false, ct);

    /// <inheritdoc />
    public async Task<RequirementSpecRefreshResult> GetRequirementSpecContentAsync(
        long pipelineId, string tenantId, string projectId, CancellationToken ct = default)
    {
        if (_specResolver != null)
        {
            var snap = await _specResolver.ResolveAsync(
                tenantId, projectId, pipelineId, includeFormalMarkdown: true, ct);
            if (!string.IsNullOrWhiteSpace(snap.FormalMarkdown)
                && IsFormalRequirementSpecMarkdown(snap.FormalMarkdown))
            {
                return ToRefreshResult(snap, snap.FormalMarkdown, rendered: true);
            }

            if (snap.Phase >= RequirementSpecPhase.Rendered && snap.BlockReason != null)
            {
                return new RequirementSpecRefreshResult
                {
                    Success = false,
                    ErrorMessage = snap.BlockReason,
                    Phase = snap.Phase,
                    PipelineStage = snap.PipelineStage,
                    ContentHash = snap.ContentHash,
                    CanUserConfirm = snap.CanUserConfirm,
                    CanUserFeedback = snap.CanUserFeedback,
                    AwaitingUser = snap.AwaitingUser,
                };
            }
        }

        var refreshed = await RenderAndSaveFormalSpecAsync(
            pipelineId, tenantId, projectId, includeMarkdown: true, ct);
        if (!refreshed.Success || _specResolver == null)
            return refreshed;

        var after = await _specResolver.ResolveAsync(tenantId, projectId, pipelineId, ct);
        return refreshed with
        {
            Phase = after.Phase,
            PipelineStage = after.PipelineStage,
            ContentHash = after.ContentHash,
            CanUserConfirm = after.CanUserConfirm,
            CanUserFeedback = after.CanUserFeedback,
            AwaitingUser = after.AwaitingUser,
        };
    }

    private static RequirementSpecRefreshResult ToRefreshResult(
        RequirementSpecSnapshot snap, string markdown, bool rendered) =>
        new()
        {
            Success = true,
            RelativePath = snap.RelativePath,
            ContentLength = snap.ContentLength ?? markdown.Length,
            Rendered = rendered,
            Markdown = markdown,
            Phase = snap.Phase,
            PipelineStage = snap.PipelineStage,
            ContentHash = snap.ContentHash,
            CanUserConfirm = snap.CanUserConfirm,
            CanUserFeedback = snap.CanUserFeedback,
            AwaitingUser = snap.AwaitingUser,
        };

    private async Task<RequirementSpecRefreshResult> RenderAndSaveFormalSpecAsync(
        long pipelineId, string tenantId, string projectId, bool includeMarkdown, CancellationToken ct)
    {
        if (_deliverables == null)
        {
            return new RequirementSpecRefreshResult
            {
                Success = false,
                ErrorMessage = "交付物服务未就绪",
            };
        }

        if (!await HasSpecRenderedMarkerAsync(tenantId, projectId, pipelineId, ct))
        {
            return new RequirementSpecRefreshResult
            {
                Success = false,
                ErrorMessage = "步骤④尚未完成（缺少 RequirementSpecRendered），无法生成正式版需求说明书",
            };
        }

        if (_documentRenderer == null || _dddProjection == null)
        {
            return new RequirementSpecRefreshResult
            {
                Success = false,
                ErrorMessage = "正式渲染器未就绪，请联系管理员检查 RequirementDocumentRenderer 注入",
            };
        }

        var specText = await ResolveSpecRenderInputAsync(tenantId, projectId, pipelineId, ct);
        if (string.IsNullOrWhiteSpace(specText))
        {
            return new RequirementSpecRefreshResult
            {
                Success = false,
                ErrorMessage = "无法解析步骤③完善后的需求文本，请重新运行需求分析",
            };
        }

        string documentMarkdown;
        try
        {
            documentMarkdown = await BuildConfirmSpecMarkdownAsync(
                pipelineId, tenantId, projectId, specText, ct, requireFormal: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RefreshSpec 正式渲染失败 pipeline={PipelineId}", pipelineId);
            return new RequirementSpecRefreshResult
            {
                Success = false,
                ErrorMessage = "正式版需求说明书渲染失败：" + ex.Message,
            };
        }

        if (!IsFormalRequirementSpecMarkdown(documentMarkdown))
        {
            return new RequirementSpecRefreshResult
            {
                Success = false,
                ErrorMessage = "渲染结果不符合正式版格式（缺少封面或确认 CTA），拒绝落盘",
            };
        }

        await _deliverables.SaveRequirementSpecAsync(tenantId, pipelineId, documentMarkdown, ct);
        _logger.LogInformation(
            "RefreshSpec 正式版已落盘 pipeline={PipelineId} len={Len}",
            pipelineId, documentMarkdown.Length);

        return new RequirementSpecRefreshResult
        {
            Success = true,
            RelativePath = FormalSpecRelativePath,
            ContentLength = documentMarkdown.Length,
            Rendered = true,
            Markdown = includeMarkdown ? documentMarkdown : null,
        };
    }

    /// <summary>
    /// 步骤⑤专用：读取用户已确认的正式版《需求分析说明书》（02 交付物正文）。
    /// S2 阶段一切角色围绕此交付物转；IR 仅保留 RequirementSpecRendered 等审计标记，正文不在 IR 兜底。
    /// </summary>
    private async Task<string> ResolveConfirmedFormalSpecMarkdownAsync(
        string tenantId, string projectId, long pipelineId, CancellationToken ct)
    {
        if (!await HasEventAsync(tenantId, projectId, pipelineId, IrEventTypes.RequirementSpecRendered, ct))
        {
            throw Oops.Bah(
                "步骤④尚未完成，无法确认需求说明书。请先完成澄清并生成正式版说明书后再确认。");
        }

        var deliverablePath = Path.Combine(
            StudioWorkspaceHelper.GetDeliverablesPath(tenantId, projectId, pipelineId.ToString()),
            FormalSpecRelativePath);
        StudioWorkspaceHelper.AssertWithinDeliverables(
            deliverablePath, tenantId, projectId, pipelineId.ToString());

        if (File.Exists(deliverablePath))
        {
            var fromFile = await File.ReadAllTextAsync(deliverablePath, ct);
            if (IsFormalRequirementSpecMarkdown(fromFile))
            {
                _logger.LogInformation(
                    "步骤⑤ 读取正式版说明书交付物 pipeline={PipelineId} len={Len}",
                    pipelineId, fromFile.Length);
                return fromFile;
            }

            _logger.LogWarning(
                "02 交付物存在但非正式版格式，将重新渲染 pipeline={PipelineId}", pipelineId);
        }

        if (_deliverables == null || _documentRenderer == null)
        {
            throw Oops.Bah(
                "正式版需求说明书不可用，且渲染服务未就绪。请先预览/下载确认后再提交。");
        }

        var refresh = await RenderAndSaveFormalSpecAsync(
            pipelineId, tenantId, projectId, includeMarkdown: true, ct);
        if (!refresh.Success || string.IsNullOrWhiteSpace(refresh.Markdown))
        {
            throw Oops.Bah(refresh.ErrorMessage ?? "无法加载正式版需求分析说明书，请先预览/下载确认后再提交");
        }

        if (!IsFormalRequirementSpecMarkdown(refresh.Markdown))
        {
            throw Oops.Bah("正式版需求分析说明书格式校验失败，拒绝进入 Finalize");
        }

        return refresh.Markdown;
    }

    /// <summary>P4 阶段 2：Resolver 与 legacy HasEvent 对照 + drift 告警。</summary>
    private async Task<RequirementSpecSnapshot?> ResolveSpecSnapshotAsync(
        long pipelineId, string tenantId, string projectId, CancellationToken ct)
    {
        if (_specResolver == null) return null;
        try
        {
            var spec = await _specResolver.ResolveAsync(tenantId, projectId, pipelineId, ct);
            var legacyRendered = await HasEventAsync(
                tenantId, projectId, pipelineId, IrEventTypes.RequirementSpecRendered, ct);
            var legacyConfirmed = await HasEventAsync(
                tenantId, projectId, pipelineId, IrEventTypes.RequirementSpecConfirmed, ct);
            _logger.LogInformation(
                "SpecStateResolver pipeline={PipelineId} phase={Phase} stage={Stage} hasRow={HasRow} canConfirm={CanConfirm} legacyR={LegacyR} legacyC={LegacyC} block={Block}",
                pipelineId, spec.Phase, spec.PipelineStage, spec.HasProgressRow,
                spec.CanUserConfirm, legacyRendered, legacyConfirmed, spec.BlockReason);
            if (spec.HasProgressRow)
            {
                var legacyRenderedPhase = legacyRendered && !legacyConfirmed
                    ? RequirementSpecPhase.Rendered
                    : legacyConfirmed
                        ? RequirementSpecPhase.Confirmed
                        : RequirementSpecPhase.Absent;
                if (legacyRendered && spec.Phase != legacyRenderedPhase
                    && spec.Phase != RequirementSpecPhase.Finalized
                    && spec.Phase != RequirementSpecPhase.PmReviewed)
                {
                    _logger.LogWarning(
                        "SpecStateResolver drift pipeline={PipelineId} rowPhase={Row} legacyHint={Legacy}",
                        pipelineId, spec.Phase, legacyRenderedPhase);
                }
            }

            return spec;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not OperationCanceledException)
        {
            _logger.LogWarning(ex, "SpecStateResolver 失败 pipeline={PipelineId}", pipelineId);
            return null;
        }
    }

    private async Task UpdateS2ProgressAsync(S2ProgressUpdate update, CancellationToken ct)
    {
        if (_progressStore == null) return;
        try
        {
            await _progressStore.UpsertAsync(update, ct);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not OperationCanceledException)
        {
            _logger.LogWarning(ex, "S2 progress upsert 失败 pipeline={PipelineId}", update.PipelineId);
        }
    }

    /// <summary>PM 流水线步骤衔接：L2 进度 + skill_progress SSE（前端折叠区展示 + 断点恢复）。</summary>
    private async Task TransitionPmStepAsync(
        long pipelineId,
        string tenantId,
        string projectId,
        PmStepTransition transition,
        CancellationToken ct)
    {
        if (transition.UpdateProgress && transition.PipelineStage.HasValue)
        {
            await UpdateS2ProgressAsync(
                ProgressPatch(tenantId, projectId, pipelineId) with
                {
                    PipelineStage = transition.PipelineStage,
                    SpecPhase = transition.SpecPhase,
                    ClarRound = transition.ClarRound,
                    AwaitingUser = transition.AwaitingUser,
                }, ct);
        }

        var payload = JsonSerializer.Serialize(new
        {
            skillId = "pm-skill",
            phase = transition.Phase,
            pmStep = transition.Step,
            nextStep = transition.NextStep,
            clarRound = transition.ClarRound,
            percent = transition.Percent,
            message = transition.Message,
            pipelineStage = transition.PipelineStage?.ToString(),
        }, JsonOptions);
        _sseHub.TryPush(pipelineId, SseEventType.SkillProgress, payload);

        _logger.LogInformation(
            "PmStep transition pipeline={PipelineId} step={Step} phase={Phase} stage={Stage} msg={Message}",
            pipelineId, transition.Step, transition.Phase, transition.PipelineStage, transition.Message);
    }

    private sealed record PmStepTransition
    {
        public required int Step { get; init; }
        public required string Phase { get; init; }
        public required string Message { get; init; }
        public required int Percent { get; init; }
        public S2PipelineStage? PipelineStage { get; init; }
        public RequirementSpecPhase SpecPhase { get; init; } = RequirementSpecPhase.Refining;
        public int? NextStep { get; init; }
        public int? ClarRound { get; init; }
        public bool AwaitingUser { get; init; }
        public bool UpdateProgress { get; init; } = true;
    }

    private static S2ProgressUpdate ProgressPatch(
        string tenantId, string projectId, long pipelineId) =>
        new() { TenantId = tenantId, ProjectId = projectId, PipelineId = pipelineId };

    /// <summary>P4 阶段 1 shadow：Resolver Phase 与现网分支对照，不改变推进逻辑。</summary>
    private async Task LogSpecResolverShadowAsync(
        long pipelineId, string tenantId, string projectId, CancellationToken ct)
    {
        _ = await ResolveSpecSnapshotAsync(pipelineId, tenantId, projectId, ct);
    }

    /// <summary>步骤④/刷新专用：解析用于渲染的需求文本（禁止用步骤①中间态冒充最终版）。</summary>
    private async Task<string?> ResolveSpecRenderInputAsync(
        string tenantId, string projectId, long pipelineId, CancellationToken ct)
    {
        foreach (var eventType in new[]
                 {
                     IrEventTypes.RequirementRefined,
                     IrEventTypes.RequirementSpecRendered,
                 })
        {
            var payload = await _eventStore.GetLatestEventPayloadAsync(
                projectId, tenantId, pipelineId.ToString(), eventType, ct);
            var text = ExtractRequirementTextFromPayload(payload);
            if (!string.IsNullOrWhiteSpace(text)) return text;
        }

        var snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);
        var reqFragment = snapshot.Find(IrFragmentTypes.Requirement, IrStabilityStates.Stable)
            ?? snapshot.Find(IrFragmentTypes.Requirement);
        return ExtractRequirementText(reqFragment);
    }

    private static bool IsFormalRequirementSpecMarkdown(string? markdown)
        => !string.IsNullOrWhiteSpace(markdown)
           && markdown.Contains(FormalSpecTitleMarker, StringComparison.Ordinal)
           && markdown.Contains(FormalSpecCtaMarker, StringComparison.Ordinal);

    /// <summary>
    /// 新 4 步线性 PM 流程（RunPmPipelineAsync）。
    /// 暂停点：awaiting-clarification / awaiting-spec-confirm / completed。
    /// </summary>
    // ════════════════════════════════════════════════════════════════════
    // CR-20260713-03 阶段 B：新 4 步线性 PM 流程
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
        var specSnapshot = await ResolveSpecSnapshotAsync(pipelineId, tenantId, projectId, ct);

        var hasSpecRendered = specSnapshot != null
            ? specSnapshot.Phase is RequirementSpecPhase.Rendered
                or RequirementSpecPhase.Confirmed
                or RequirementSpecPhase.PmReviewed
                or RequirementSpecPhase.Finalized
            : await HasEventAsync(tenantId, projectId, pipelineId, IrEventTypes.RequirementSpecRendered, ct);
        var hasSpecConfirmed = specSnapshot != null
            ? specSnapshot.Phase is RequirementSpecPhase.Confirmed
                or RequirementSpecPhase.PmReviewed
                or RequirementSpecPhase.Finalized
            : await HasEventAsync(tenantId, projectId, pipelineId, IrEventTypes.RequirementSpecConfirmed, ct);

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

        // 步骤⑤ / 已完成：L2 progress + Resolver 为主，legacy HasEvent 兜底
        if (string.IsNullOrWhiteSpace(specFeedback))
        {
            if (specSnapshot != null)
            {
                if (specSnapshot.Phase == RequirementSpecPhase.Rendered && specSnapshot.BlockReason == null)
                {
                    _logger.LogInformation(
                        "RunPmPipeline 步骤⑤ 用户确认需求说明书 pipeline={PipelineId}", pipelineId);
                    return await RunStep5FinalizeAsync(
                        pipelineId, tenantId, projectId, context, options, ct);
                }

                if (specSnapshot.Phase is RequirementSpecPhase.Confirmed or RequirementSpecPhase.PmReviewed
                    && specSnapshot.CanFinalize)
                {
                    _logger.LogInformation(
                        "RunPmPipeline 步骤⑤ 续跑 Finalize pipeline={PipelineId} phase={Phase}",
                        pipelineId, specSnapshot.Phase);
                    return await RunStep5FinalizeAsync(
                        pipelineId, tenantId, projectId, context, options, ct);
                }

                if (specSnapshot.Phase == RequirementSpecPhase.Finalized)
                {
                    _logger.LogInformation(
                        "RunPmPipeline 已完成 pipeline={PipelineId}", pipelineId);
                    return new RequirementAnalysisOrchestratorResult { Status = "completed" };
                }
            }
            else if (hasSpecRendered && !hasSpecConfirmed)
            {
                _logger.LogInformation(
                    "RunPmPipeline 步骤⑤(legacy) 用户确认需求说明书 pipeline={PipelineId}", pipelineId);
                return await RunStep5FinalizeAsync(
                    pipelineId, tenantId, projectId, context, options, ct);
            }
            else if (hasSpecConfirmed)
            {
                _logger.LogInformation(
                    "RunPmPipeline 已完成(legacy) pipeline={PipelineId}", pipelineId);
                return new RequirementAnalysisOrchestratorResult { Status = "completed" };
            }
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
        var hasNineView = nineViewFragment != null
            || await HasEventAsync(tenantId, projectId, pipelineId, IrEventTypes.SaNineViewCompiled, ct);
        var hasRefined = await HasEventAsync(tenantId, projectId, pipelineId, IrEventTypes.RequirementRefined, ct);
        var reqClar = FindRoundClarification(snapshot, ClarificationStages.Requirement);
        var answerState = await LoadRequirementClarificationAnswerStateAsync(
            tenantId, projectId, pipelineId, reqClar, ct);
        var answeredCount = answerState.AnsweredCount;
        var mergedAnswersText = answerState.MergedAnswersText;
        var pendingRound = ExtractPendingClarificationRound(reqClar);
        var isContinue = IsContinueResumeKeyword(userMessage);
        var hasPendingRequirementClar = pendingRound > 0;
        var stalePending = hasPendingRequirementClar && answeredCount >= pendingRound;

        // ── 已答满最小轮次且步骤③已完成 → 推说明书（用户说「继续」时常见）──
        if (hasNineView && hasRefined && answeredCount >= MinPmOptimizationRounds && !hasSpecRendered
            && (isContinue || stalePending))
        {
            var refinedText = ExtractRequirementText(existingSpec)
                ?? await ResolveUserRequirementAsync(tenantId, projectId, pipelineId, ct);
            if (!string.IsNullOrWhiteSpace(refinedText))
            {
                _logger.LogInformation(
                    "RunPmPipeline 澄清已满 {Answered} 轮且已 Refine，推说明书 pipeline={PipelineId}",
                    answeredCount, pipelineId);
                return await RenderSpecAndWaitConfirmAsync(
                    pipelineId, tenantId, projectId, refinedText, ct);
            }
        }

        var hasStructuredAnswersReady = answeredCount > 0
            && !string.IsNullOrWhiteSpace(mergedAnswersText);
        var hasEnhanced = await HasEventAsync(
            tenantId, projectId, pipelineId, IrEventTypes.RequirementEnhanced, ct);

        // ── 步骤①结构化澄清已作答，尚未 Enhanced/九步 → 续跑步骤①（禁止误跳步骤③）──
        if (!hasNineView && !hasEnhanced && !hasSpecRendered && hasStructuredAnswersReady
            && (isContinue || stalePending || !hasPendingRequirementClar))
        {
            var step1Turns = BuildPmTurnsFromStructuredClarification(
                reqClar, mergedAnswersText, ClarificationSource.Step1Enhance);
            _logger.LogInformation(
                "RunPmPipeline 步骤①澄清已作答，续跑完善需求 pipeline={PipelineId} answered={Answered}",
                pipelineId, answeredCount);
            await TransitionPmStepAsync(
                pipelineId, tenantId, projectId,
                new PmStepTransition
                {
                    Step = 1,
                    Phase = "handoff",
                    Message = $"🔗 步骤①衔接：已收到第 {answeredCount} 轮澄清作答，PM 继续完善需求…",
                    Percent = 12,
                    PipelineStage = S2PipelineStage.PmEnhanceRunning,
                    ClarRound = answeredCount,
                    NextStep = 1,
                }, ct);
            return await RunStep1EnhanceAsync(
                pipelineId, tenantId, projectId, context, step1Turns, ct);
        }

        // ── 结构化澄清已作答 / 用户说「继续」→ 续跑步骤③（优先于 replay 旧 pending 题）──
        if (hasNineView && !hasSpecRendered && hasStructuredAnswersReady
            && (isContinue || !hasPendingRequirementClar || stalePending))
        {
            _logger.LogInformation(
                "RunPmPipeline 结构化澄清已作答，续跑步骤③ pipeline={PipelineId} answered={Answered} pendingRound={Pending} isContinue={Continue}",
                pipelineId, answeredCount, pendingRound, isContinue);

            await TransitionPmStepAsync(
                pipelineId, tenantId, projectId,
                new PmStepTransition
                {
                    Step = 3,
                    Phase = "handoff",
                    Message = $"🔗 步骤③衔接：已收到第 {answeredCount} 轮澄清作答，正在合并答案并更新骨架…",
                    Percent = 52,
                    PipelineStage = S2PipelineStage.ClarificationRoundAnswered,
                    ClarRound = answeredCount,
                    NextStep = 3,
                }, ct);

            var answerFragment = reqClar?.StabilityState == IrStabilityStates.Stable
                ? reqClar
                : BuildSyntheticClarificationFragment(reqClar, mergedAnswersText, answeredCount);
            if (string.IsNullOrWhiteSpace(ExtractAnswersText(answerFragment.Payload)))
                answerFragment = BuildSyntheticClarificationFragment(reqClar, mergedAnswersText, answeredCount);

            await RunWithThinkingHeartbeatAsync(
                pipelineId, "⏳ PM 正在将澄清答案并入需求骨架（可能需要 1–2 分钟）…", ct,
                async token =>
                {
                    await ApplyClarificationAnswersToSkeletonAsync(
                        pipelineId, tenantId, projectId, snapshot, answerFragment, answeredCount, token);
                    return true;
                });

            await TransitionPmStepAsync(
                pipelineId, tenantId, projectId,
                new PmStepTransition
                {
                    Step = 2,
                    Phase = "progress",
                    Message = "📋 步骤②衔接：骨架已更新，正在重新编译九步分析…",
                    Percent = 38,
                    PipelineStage = S2PipelineStage.SaDecomposeRunning,
                    UpdateProgress = false,
                }, ct);
            snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);

            var requirementFragment = snapshot.Find(IrFragmentTypes.Requirement, IrStabilityStates.Stable)
                ?? snapshot.Find(IrFragmentTypes.Requirement, IrStabilityStates.Draft)
                ?? existingSpec;
            var enhancedText = await ResolveEnhancedRequirementTextAsync(
                tenantId, projectId, pipelineId, requirementFragment, context.UserRequirement, ct);
            var mergedEnhanced = BuildRequirementWithClarificationAnswers(enhancedText, mergedAnswersText);

            var compileResult = await RunWithThinkingHeartbeatAsync(
                pipelineId, "⏳ 九步重编译进行中，请稍候…", ct,
                async token => await RecompileFromCurrentSkeletonAsync(
                    pipelineId, tenantId, projectId, context, token));
            var resumeWarnings = _lightValidator.Validate(compileResult.Source);
            var resumeContext = CloneContextWithRequirement(context, mergedEnhanced);

            await TransitionPmStepAsync(
                pipelineId, tenantId, projectId,
                new PmStepTransition
                {
                    Step = 3,
                    Phase = "started",
                    Message = "🔍 步骤③：答案已并入，PM 基于九步结果反向完善需求…",
                    Percent = 55,
                    PipelineStage = S2PipelineStage.PmRefineRunning,
                }, ct);

            var step3Turns = BuildPmTurnsFromStructuredClarification(
                reqClar, mergedAnswersText, ClarificationSource.Step3Refine);

            return await RunStep3RefineAsync(
                pipelineId, tenantId, projectId, resumeContext, mergedEnhanced,
                step3Turns, ct, resumeWarnings, compileResult);
        }

        // ── 真正待答（待答轮次 > 已答轮次）→ 重推当前轮卡片 ──
        if (hasPendingRequirementClar && pendingRound > answeredCount && !hasSpecRendered)
        {
            return await ReturnPendingRequirementClarificationAsync(
                pipelineId, tenantId, projectId, reqClar, pmStep: hasNineView ? 3 : 1, ct);
        }

        // 九步已拆但步骤③未完成 → 继续反向完善（禁止 SA 九步完成后直接推说明书）
        if (hasNineView && !hasSpecRendered && !hasRefined && answeredCount == 0)
        {
            var enhancedText = ExtractRequirementText(existingSpec)
                ?? await ResolveUserRequirementAsync(tenantId, projectId, pipelineId, ct);
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
        if (!string.IsNullOrWhiteSpace(pmAnswer) && !IsContinueResumeKeyword(pmAnswer))
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
                    var enhancedText = ExtractRequirementText(existingSpec)
                        ?? await ResolveUserRequirementAsync(tenantId, projectId, pipelineId, ct);
                    return await RunStep3RefineAsync(
                        pipelineId, tenantId, projectId, context, enhancedText, turns, ct);
                }
            }

            // ADR-005 结构化澄清卡片：LoadClarificationTurns 无 turn 字段，从 IR 答案回灌
            if (hasStructuredAnswersReady || !string.IsNullOrWhiteSpace(pmAnswer))
            {
                var answerText = hasStructuredAnswersReady ? mergedAnswersText : pmAnswer!;
                var source = hasNineView || hasEnhanced
                    ? ClarificationSource.Step3Refine
                    : ClarificationSource.Step1Enhance;
                var resumeTurns = BuildPmTurnsFromStructuredClarification(reqClar, answerText, source);
                _logger.LogInformation(
                    "RunPmPipeline 结构化答案回灌 pipeline={PipelineId} source={Source}",
                    pipelineId, source);
                await TransitionPmStepAsync(
                    pipelineId, tenantId, projectId,
                    new PmStepTransition
                    {
                        Step = source == ClarificationSource.Step1Enhance ? 1 : 3,
                        Phase = "handoff",
                        Message = source == ClarificationSource.Step1Enhance
                            ? "🔗 步骤①衔接：已收到澄清作答，PM 继续完善需求…"
                            : "🔗 步骤③衔接：已收到澄清作答，PM 继续反向完善…",
                        Percent = source == ClarificationSource.Step1Enhance ? 12 : 52,
                        PipelineStage = source == ClarificationSource.Step1Enhance
                            ? S2PipelineStage.PmEnhanceRunning
                            : S2PipelineStage.PmRefineRunning,
                        NextStep = source == ClarificationSource.Step1Enhance ? 1 : 3,
                    }, ct);
                if (source == ClarificationSource.Step1Enhance)
                {
                    return await RunStep1EnhanceAsync(
                        pipelineId, tenantId, projectId, context, resumeTurns, ct);
                }

                var enhancedForStep3 = ExtractRequirementText(existingSpec)
                    ?? await ResolveUserRequirementAsync(tenantId, projectId, pipelineId, ct);
                return await RunStep3RefineAsync(
                    pipelineId, tenantId, projectId, context, enhancedForStep3, resumeTurns, ct);
            }
        }

        // ── 已有 PM 进度但无匹配恢复点 → 九步后续跑步骤③；步骤①暂停则回完善需求 ──
        var hasPmProgress = hasNineView || hasEnhanced || answeredCount > 0;
        if (hasPmProgress)
        {
            if (!hasNineView && !hasEnhanced && hasStructuredAnswersReady)
            {
                var fallbackTurns = BuildPmTurnsFromStructuredClarification(
                    reqClar, mergedAnswersText, ClarificationSource.Step1Enhance);
                _logger.LogInformation(
                    "RunPmPipeline 兜底续跑步骤① pipeline={PipelineId} answered={Answered}",
                    pipelineId, answeredCount);
                await TransitionPmStepAsync(
                    pipelineId, tenantId, projectId,
                    new PmStepTransition
                    {
                        Step = 1,
                        Phase = "handoff",
                        Message = "🔗 步骤①衔接（兜底）：已收到澄清作答，PM 继续完善需求…",
                        Percent = 12,
                        PipelineStage = S2PipelineStage.PmEnhanceRunning,
                        NextStep = 1,
                    }, ct);
                return await RunStep1EnhanceAsync(
                    pipelineId, tenantId, projectId, context, fallbackTurns, ct);
            }

            _logger.LogWarning(
                "RunPmPipeline 检测到 PM 进度但无精确恢复点，续跑步骤③ pipeline={PipelineId} answered={Answered}",
                pipelineId, answeredCount);
            await TransitionPmStepAsync(
                pipelineId, tenantId, projectId,
                new PmStepTransition
                {
                    Step = 3,
                    Phase = "handoff",
                    Message = "🔗 步骤③衔接（兜底）：检测到已有九步进度，PM 从九步结果续跑…",
                    Percent = 52,
                    PipelineStage = S2PipelineStage.PmRefineRunning,
                    NextStep = 3,
                }, ct);
            var enhancedText = ExtractRequirementText(existingSpec)
                ?? await ResolveUserRequirementAsync(tenantId, projectId, pipelineId, ct);
            return await RunStep3RefineAsync(
                pipelineId, tenantId, projectId, context, enhancedText, turns: null, ct);
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
        await TransitionPmStepAsync(
            pipelineId, tenantId, projectId,
            new PmStepTransition
            {
                Step = 1,
                Phase = "started",
                Message = "🚦 门控已通过，启动 PM 需求分析流水线（步骤①→⑤）",
                Percent = 2,
                PipelineStage = S2PipelineStage.GatePassed,
            }, ct);
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
        await TransitionPmStepAsync(
            pipelineId, tenantId, projectId,
            new PmStepTransition
            {
                Step = 1,
                Phase = "started",
                Message = "📝 步骤①：PM 正在完善需求（流式推理见下方）…",
                Percent = 5,
                PipelineStage = S2PipelineStage.PmEnhanceRunning,
            }, ct);
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

            await TransitionPmStepAsync(
                pipelineId, tenantId, projectId,
                new PmStepTransition
                {
                    Step = 1,
                    Phase = "awaiting_user",
                    Message = "⏸️ 步骤①暂停：请在下方澄清卡片作答，作答后将自动续跑完善需求",
                    Percent = 18,
                    PipelineStage = S2PipelineStage.PmEnhanceAwaitingUser,
                    AwaitingUser = true,
                    ClarRound = 1,
                }, ct);

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

        await TransitionPmStepAsync(
            pipelineId, tenantId, projectId,
            new PmStepTransition
            {
                Step = 1,
                Phase = "completed",
                Message = "✅ 步骤①完成：需求文本已完善并写入 IR",
                Percent = 20,
                PipelineStage = S2PipelineStage.PmEnhanceRunning,
            }, ct);
        await TransitionPmStepAsync(
            pipelineId, tenantId, projectId,
            new PmStepTransition
            {
                Step = 1,
                Phase = "handoff",
                Message = "🔗 衔接 → 步骤②：基于完善后的需求进行 SA 九步拆解",
                Percent = 22,
                NextStep = 2,
                UpdateProgress = false,
            }, ct);

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
        await TransitionPmStepAsync(
            pipelineId, tenantId, projectId,
            new PmStepTransition
            {
                Step = 2,
                Phase = "started",
                Message = "📋 步骤②：SA 九步拆解启动（7 步确定性编译 + PSpec/决策表语义增强）",
                Percent = 25,
                PipelineStage = S2PipelineStage.SaDecomposeRunning,
            }, ct);

        // 2a. C# 编译器 7 步（确定性，零 LLM）
        var compileResult = _compiler.CompileFromSkeletonJson(skeleton.Payload);
        var warnings = _lightValidator.Validate(compileResult.Source);
        await TransitionPmStepAsync(
            pipelineId, tenantId, projectId,
            new PmStepTransition
            {
                Step = 2,
                Phase = "progress",
                Message = $"✅ 步骤②·7 步确定性编译完成：{compileResult.EventResults.Count} 个业务事件",
                Percent = 35,
                UpdateProgress = false,
            }, ct);

        // 持久化 Assumptions（跨步骤审计）
        await PersistAssumptionsFragmentAsync(
            pipelineId, tenantId, projectId, compileResult.Assumptions, round: 2, ct);

        // 2b. PM LLM 产 PSpec/DT 真语义
        await TransitionPmStepAsync(
            pipelineId, tenantId, projectId,
            new PmStepTransition
            {
                Step = 2,
                Phase = "progress",
                Message = "🔧 步骤②·PM 正在产出 PSpec / DecisionTable 真语义…",
                Percent = 42,
                UpdateProgress = false,
            }, ct);
        var enhancedContext = CloneContextWithRequirement(context, enhancedText);
        var enhancedCompile = await RunWithThinkingHeartbeatAsync(
            pipelineId, "⏳ PSpec/DecisionTable 语义增强进行中…", ct,
            token => _pm.EnhancePspecDecisionTableAsync(enhancedContext, compileResult, token));

        // 持久化九步数据（给后续二次开发/BUG 修复用）
        await PersistNineViewAsync(pipelineId, tenantId, projectId, enhancedCompile, ct);

        _logger.LogInformation(
            "RunPmPipeline 步骤②完成 pipeline={PipelineId} events={Count} warnings={Warn}",
            pipelineId, enhancedCompile.EventResults.Count, warnings.Count);

        await TransitionPmStepAsync(
            pipelineId, tenantId, projectId,
            new PmStepTransition
            {
                Step = 2,
                Phase = "completed",
                Message = "✅ 步骤②完成：九步拆解与 PSpec/决策表已落盘",
                Percent = 50,
                PipelineStage = S2PipelineStage.SaDecomposeDone,
            }, ct);
        await TransitionPmStepAsync(
            pipelineId, tenantId, projectId,
            new PmStepTransition
            {
                Step = 2,
                Phase = "handoff",
                Message = "🔗 衔接 → 步骤③：PM 将基于九步结果反向完善需求",
                Percent = 52,
                NextStep = 3,
                UpdateProgress = false,
            }, ct);

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
        await TransitionPmStepAsync(
            pipelineId, tenantId, projectId,
            new PmStepTransition
            {
                Step = 3,
                Phase = "started",
                Message = "🔍 步骤③：PM 分析九步结果并反向完善需求…",
                Percent = 55,
                PipelineStage = S2PipelineStage.PmRefineRunning,
            }, ct);
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

            var clarRound = (await LoadRequirementClarificationAnswerStateAsync(
                tenantId, projectId, pipelineId, null, ct)).AnsweredCount + 1;
            await EmitClarificationRequestedAsync(
                pipelineId, tenantId, projectId, "requirement", clarRound, clarSet, ct);

            _logger.LogInformation(
                "RunPmPipeline 步骤③暂停追问 pipeline={PipelineId} questions={Count}",
                pipelineId, clarSet.Questions.Count);

            await TransitionPmStepAsync(
                pipelineId, tenantId, projectId,
                new PmStepTransition
                {
                    Step = 3,
                    Phase = "awaiting_user",
                    Message = $"⏸️ 步骤③暂停：第 {clarRound} 轮结构化追问，请在下方澄清卡片作答",
                    Percent = 58,
                    PipelineStage = S2PipelineStage.PmRefineAwaitingUser,
                    AwaitingUser = true,
                    ClarRound = clarRound,
                }, ct);

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

        await TransitionPmStepAsync(
            pipelineId, tenantId, projectId,
            new PmStepTransition
            {
                Step = 3,
                Phase = "completed",
                Message = "✅ 步骤③完成：需求已基于九步分析反向完善",
                Percent = 70,
                PipelineStage = S2PipelineStage.PmRefineRunning,
            }, ct);

        var answeredRounds = (await LoadRequirementClarificationAnswerStateAsync(
            tenantId, projectId, pipelineId, null, ct)).AnsweredCount;
        if (answeredRounds < MinPmOptimizationRounds)
        {
            var nextRound = answeredRounds + 1;
            _logger.LogInformation(
                "RunPmPipeline 澄清轮次不足({Answered}/{Min})，专用出题路径 pipeline={PipelineId} round={Round}",
                answeredRounds, MinPmOptimizationRounds, pipelineId, nextRound);
            await TransitionPmStepAsync(
                pipelineId, tenantId, projectId,
                new PmStepTransition
                {
                    Step = 3,
                    Phase = "progress",
                    Message = $"📋 步骤③·深度优化：已完成 {answeredRounds} 轮澄清，启动第 {nextRound} 轮结构化追问…",
                    Percent = 65,
                    PipelineStage = S2PipelineStage.PmRefineRunning,
                    ClarRound = nextRound,
                }, ct);

            var previousAnswers = ExtractAnswersText(
                FindRoundClarification(
                    await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct),
                    ClarificationStages.Requirement)?.Payload);

            var clarSet = await RunWithThinkingHeartbeatAsync(
                pipelineId, $"⏳ PM 正在准备第 {nextRound} 轮结构化追问…", ct,
                token => GenerateStepClarificationAsync(
                    pipelineId, tenantId, projectId, context,
                    compileResult, warnings, previousAnswers,
                    stage: "requirement", round: nextRound, token, forceQuestions: true));

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

            await TransitionPmStepAsync(
                pipelineId, tenantId, projectId,
                new PmStepTransition
                {
                    Step = 3,
                    Phase = "awaiting_user",
                    Message = $"⏸️ 步骤③·第 {nextRound} 轮深度优化：请在下方澄清卡片作答",
                    Percent = 58,
                    PipelineStage = S2PipelineStage.ClarificationRoundAwaiting,
                    AwaitingUser = true,
                    ClarRound = nextRound,
                }, ct);

            return new RequirementAnalysisOrchestratorResult
            {
                Status = "awaiting-clarification",
                PendingClarification = clarSet,
            };
        }

        await TransitionPmStepAsync(
            pipelineId, tenantId, projectId,
            new PmStepTransition
            {
                Step = 3,
                Phase = "handoff",
                Message = "🔗 衔接 → 步骤④：澄清轮次已满足，开始渲染需求说明书",
                Percent = 72,
                NextStep = 4,
                UpdateProgress = false,
            }, ct);

        return await RenderSpecAndWaitConfirmAsync(
            pipelineId, tenantId, projectId, result.EnhancedText, ct);
    }

    /// <summary>步骤④：用 RequirementDocumentRenderer 产出正式 02（含九步+澄清附录）。</summary>
    private async Task<string> BuildConfirmSpecMarkdownAsync(
        long pipelineId, string tenantId, string projectId, string specText, CancellationToken ct,
        bool requireFormal = false)
    {
        if (_documentRenderer == null || _dddProjection == null)
        {
            if (requireFormal)
                throw new InvalidOperationException("RequirementDocumentRenderer 或 DddProjection 未注入");
            return specText;
        }

        try
        {
            var triple = new PipelineTriple(tenantId, projectId, pipelineId);
            var snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);
            var context = new SkillContext
            {
                RunId = Guid.NewGuid().ToString("N"),
                TenantId = tenantId,
                ProjectId = projectId,
                PipelineId = pipelineId,
                UserRequirement = specText,
                Snapshot = snapshot,
            };
            var compileResult = await RecompileFromCurrentSkeletonAsync(
                pipelineId, tenantId, projectId, context, ct);
            var identity = compileResult.Source.ResolveIdentity(pipelineTitle: null, requirementText: specText);
            var renderCompile = new SaNineViewCompileResult
            {
                Source = identity,
                ProjectSteps = compileResult.ProjectSteps,
                EventResults = compileResult.EventResults,
                Assumptions = compileResult.Assumptions,
                BundleHash = compileResult.BundleHash,
                CompileDurationMs = compileResult.CompileDurationMs,
            };

            var projection = EntityDesignProjector.Project(snapshot, new EntityDesignProjectionOptions
            {
                TenantId = tenantId,
                ProjectId = projectId,
                PipelineId = pipelineId.ToString(),
            });
            var dddResult = _dddProjection.Project(renderCompile, projection);
            var clarificationAnswers = await LoadClarificationAppendicesForRenderAsync(
                tenantId, projectId, pipelineId, ct);
            var answeredRounds = (await LoadRequirementClarificationAnswerStateAsync(
                tenantId, projectId, pipelineId, null, ct)).AnsweredCount;
            var roundNumber = Math.Max(answeredRounds, MinPmOptimizationRounds);
            var previewScore = new QualityScore
            {
                StructureScore = 75,
                CoverageScore = 70,
                ConsistencyScore = 85,
                DepthScore = 70,
                DddScore = Math.Round((decimal)(dddResult.OverallConfidence * 100), 2),
            };

            var rendered = _documentRenderer.Render(
                triple, renderCompile, dddResult, projection,
                Array.Empty<ConsistencyFinding>(), previewScore,
                roundNumber, clarificationAnswers, ct);

            if (requireFormal && !IsFormalRequirementSpecMarkdown(rendered))
                throw new InvalidOperationException("渲染器产出缺少正式版封面或 CTA");

            return rendered;
        }
        catch (Exception ex) when (!requireFormal)
        {
            _logger.LogWarning(ex, "步骤④正式渲染失败，回退 raw spec pipeline={PipelineId}", pipelineId);
            return specText;
        }
    }

    private async Task<IReadOnlyList<ClarificationAnswerAppendix>> LoadClarificationAppendicesForRenderAsync(
        string tenantId, string projectId, long pipelineId, CancellationToken ct)
    {
        var result = new List<ClarificationAnswerAppendix>();
        var payloads = await _eventStore.ListFullEventPayloadsAsync(
            projectId, tenantId, pipelineId.ToString(), IrEventTypes.ClarificationAnswered, ct);
        foreach (var payload in payloads)
        {
            if (!IsRequirementStageClarificationPayload(payload))
                continue;
            try
            {
                using var doc = JsonDocument.Parse(payload!);
                var root = doc.RootElement;
                var stage = root.TryGetProperty("stage", out var stageEl) && stageEl.ValueKind == JsonValueKind.String
                    ? stageEl.GetString() ?? ""
                    : "";
                if (!RequirementAnalysisStages.IsRequirementAnalysisStage(stage))
                    continue;
                var round = root.TryGetProperty("round", out var roundEl) && roundEl.TryGetInt32(out var r) ? r : 0;
                var answersText = ExtractAnswersText(payload);
                if (string.IsNullOrWhiteSpace(answersText))
                    continue;
                result.Add(new ClarificationAnswerAppendix(stage, round, answersText));
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "澄清作答 payload 解析失败 pipeline={PipelineId}", pipelineId);
            }
        }

        return result
            .GroupBy(x => $"{x.Stage}:{x.Round}", StringComparer.Ordinal)
            .Select(g => g.Last())
            .OrderBy(x => x.Round)
            .ToList();
    }

    /// <summary>步骤④：渲染需求说明书 IR + 推确认按钮（不在聊天正文灌整份 markdown）。</summary>
    private async Task<RequirementAnalysisOrchestratorResult> RenderSpecAndWaitConfirmAsync(
        long pipelineId, string tenantId, string projectId, string specText, CancellationToken ct)
    {
        await TransitionPmStepAsync(
            pipelineId, tenantId, projectId,
            new PmStepTransition
            {
                Step = 4,
                Phase = "started",
                Message = "📄 步骤④：正在渲染正式版需求说明书（含九步与澄清附录）…",
                Percent = 75,
                PipelineStage = S2PipelineStage.SpecRendering,
            }, ct);

        if (_deliverables != null && !string.IsNullOrWhiteSpace(specText))
        {
            try
            {
                var documentMarkdown = await BuildConfirmSpecMarkdownAsync(
                    pipelineId, tenantId, projectId, specText, ct, requireFormal: true);
                await _deliverables.SaveRequirementSpecAsync(tenantId, pipelineId, documentMarkdown, ct);
                var contentHash = RequirementSpecDeliverableMarkdownReader.ComputeSha256Hex(documentMarkdown);
                _logger.LogInformation(
                    "RunPmPipeline 步骤④ {Path} 已落盘 pipeline={PipelineId} len={Len}",
                    FormalSpecRelativePath, pipelineId, documentMarkdown.Length);

                await TransitionPmStepAsync(
                    pipelineId, tenantId, projectId,
                    new PmStepTransition
                    {
                        Step = 4,
                        Phase = "awaiting_user",
                        Message = "✅ 步骤④完成：需求说明书已生成，请在下方卡片确认或提出修改",
                        Percent = 85,
                        PipelineStage = S2PipelineStage.SpecAwaitingUserConfirm,
                        SpecPhase = RequirementSpecPhase.Rendered,
                        AwaitingUser = true,
                    }, ct);

                // 同步 hash/length 到 progress 行（Transition 不覆盖 ContentHash）
                await UpdateS2ProgressAsync(
                    ProgressPatch(tenantId, projectId, pipelineId) with
                    {
                        PipelineStage = S2PipelineStage.SpecAwaitingUserConfirm,
                        SpecPhase = RequirementSpecPhase.Rendered,
                        ContentHash = contentHash,
                        ContentLength = documentMarkdown.Length,
                        AwaitingUser = true,
                    }, ct);

                var specVersion = 1;
                if (_progressStore != null)
                {
                    var row = await _progressStore.TryGetAsync(tenantId, projectId, pipelineId, ct);
                    if (row != null && row.SpecVersion > 0)
                        specVersion = row.SpecVersion;
                }

                await PersistSpecRenderedMetadataAsync(
                    pipelineId, tenantId, projectId, specVersion, contentHash, documentMarkdown.Length, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "RunPmPipeline 步骤④正式版落盘失败 pipeline={PipelineId}", pipelineId);
                throw Oops.Bah("正式版需求说明书渲染失败，请稍后重试或联系管理员");
            }
        }

        _sseHub.TryPush(pipelineId, "spec_confirm_requested", JsonSerializer.Serialize(new
        {
            specFragmentId = RequirementSpecConstants.WorkingRequirementFragmentId(pipelineId),
            message = "需求说明书已生成，请确认通过或提出修改意见。",
            deliverablePath = FormalSpecRelativePath,
            phase = nameof(RequirementSpecPhase.Rendered),
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

        await TransitionPmStepAsync(
            pipelineId, tenantId, projectId,
            new PmStepTransition
            {
                Step = 4,
                Phase = "handoff",
                Message = "🔗 步骤④反馈：将按您的修改意见重跑步骤①→③，再生成新版说明书",
                Percent = 10,
                NextStep = 1,
                SpecPhase = RequirementSpecPhase.Superseded,
                UpdateProgress = false,
            }, ct);

        var currentVersion = 1;
        if (_progressStore != null)
        {
            var row = await _progressStore.TryGetAsync(tenantId, projectId, pipelineId, ct);
            if (row != null && row.SpecVersion > 0)
                currentVersion = row.SpecVersion;
        }

        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.RequirementSpecSuperseded,
            FragmentId = RequirementSpecConstants.SpecStateFragmentId(pipelineId),
            FragmentType = IrFragmentTypes.RequirementSpecState,
            FragmentVersion = 1,
            Payload = JsonSerializer.Serialize(new
            {
                pipelineId,
                reason = "user_feedback",
                timestamp = DateTimeOffset.UtcNow.ToString("O"),
            }, JsonOptions),
            SkillId = "pm-skill",
        }, ct);

        var nextVersion = currentVersion + 1;
        await UpdateS2ProgressAsync(
            ProgressPatch(tenantId, projectId, pipelineId) with
            {
                PipelineStage = S2PipelineStage.PmEnhanceRunning,
                SpecPhase = RequirementSpecPhase.Superseded,
                SpecVersion = nextVersion,
                ContentHash = null,
                ContentLength = null,
                AwaitingUser = false,
            }, ct);

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
        var preConfirmHash = _progressStore != null
            ? (await _progressStore.TryGetAsync(tenantId, projectId, pipelineId, ct))?.ContentHash
            : null;
        var preConfirmVersion = 1;
        if (_progressStore != null)
        {
            var row = await _progressStore.TryGetAsync(tenantId, projectId, pipelineId, ct);
            if (row != null && row.SpecVersion > 0)
            {
                preConfirmVersion = row.SpecVersion;
                preConfirmHash ??= row.ContentHash;
            }
        }

        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.RequirementSpecConfirmed,
            FragmentId = RequirementSpecConstants.SpecStateFragmentId(pipelineId),
            FragmentType = IrFragmentTypes.RequirementSpecState,
            FragmentVersion = preConfirmVersion,
            Payload = JsonSerializer.Serialize(new
            {
                pipelineId,
                specVersion = preConfirmVersion,
                contentHash = preConfirmHash,
                confirmedBy = "user",
                timestamp = DateTimeOffset.UtcNow.ToString("O"),
            }, JsonOptions),
            SkillId = "pm-skill",
        }, ct);

        await TransitionPmStepAsync(
            pipelineId, tenantId, projectId,
            new PmStepTransition
            {
                Step = 5,
                Phase = "started",
                Message = "🔗 步骤⑤：用户已确认说明书，启动 PM 终评与工程 Finalize",
                Percent = 88,
                PipelineStage = S2PipelineStage.SpecConfirmed,
                SpecPhase = RequirementSpecPhase.Confirmed,
            }, ct);

        // 5b. PM 终评 + 5c Analyst Finalize：输入 MUST 为已确认正式版 02，禁止 IR/上下文兜底
        var formalSpecMarkdown = await ResolveConfirmedFormalSpecMarkdownAsync(
            tenantId, projectId, pipelineId, ct);
        var skillOptions = new SkillRunOptions
        {
            ProviderCode = options?.ProviderCode,
            UserRequirement = formalSpecMarkdown,
        };
        var review = await ReviewRequirementSpecAsync(
            pipelineId, tenantId, projectId, orchestratorRunId, skillOptions,
            forceConfirm: options?.ForceConfirm == true,
            forceReason: options?.ForceReason,
            ct: ct);

        _logger.LogInformation(
            "RunStep5 PM 终评 verdict={Verdict} score={Score} forceConfirm={Force} pipeline={PipelineId}",
            review.Verdict, review.Score, options?.ForceConfirm == true, pipelineId);

        var reviewProgressMsg = review.Score >= 85 || review.Verdict == "pass"
            ? $"📋 步骤⑤·PM 终评完成（{review.Verdict} / {review.Score} 分），进入工程保障…"
            : options?.ForceConfirm == true
            ? $"📋 步骤⑤·PM 终评 {review.Score} 分（未达 85），已按赶进度强制确认继续 Finalize…"
            : $"📋 步骤⑤·PM 终评完成（{review.Verdict} / {review.Score} 分），进入工程保障…";

        await TransitionPmStepAsync(
            pipelineId, tenantId, projectId,
            new PmStepTransition
            {
                Step = 5,
                Phase = "progress",
                Message = reviewProgressMsg,
                Percent = 92,
                PipelineStage = S2PipelineStage.PmFinalReview,
                SpecPhase = RequirementSpecPhase.PmReviewed,
            }, ct);

        await TransitionPmStepAsync(
            pipelineId, tenantId, projectId,
            new PmStepTransition
            {
                Step = 5,
                Phase = "progress",
                Message = "🔧 步骤⑤·正在执行工程保障 Finalize（投影 / 门禁 / 物化）…",
                Percent = 96,
                PipelineStage = S2PipelineStage.EngineeringFinalize,
                SpecPhase = RequirementSpecPhase.PmReviewed,
            }, ct);

        // 5c. Analyst Finalize（复用 RunRoundAnalystAsync，enableFinalization=true）
        var finalizeResult = await RunRoundAnalystAsync(
            3, pipelineId, tenantId, projectId, skillOptions,
            enableFinalization: true, ct);

        _logger.LogInformation(
            "RunStep5 Finalize 完成 pipeline={PipelineId}", pipelineId);

        await TransitionPmStepAsync(
            pipelineId, tenantId, projectId,
            new PmStepTransition
            {
                Step = 5,
                Phase = "completed",
                Message = "✅ 步骤⑤完成：需求分析已 Finalize，可进入架构设计阶段",
                Percent = 100,
                PipelineStage = S2PipelineStage.S2Complete,
                SpecPhase = RequirementSpecPhase.Finalized,
            }, ct);

        var finalizeHash = _progressStore != null
            ? (await _progressStore.TryGetAsync(tenantId, projectId, pipelineId, ct))?.ContentHash
            : null;
        if (string.IsNullOrWhiteSpace(finalizeHash) && _specResolver != null)
        {
            var finalizedSnap = await _specResolver.ResolveAsync(tenantId, projectId, pipelineId, ct);
            finalizeHash = finalizedSnap.ContentHash;
        }

        var finalizeVersion = 1;
        if (_progressStore != null)
        {
            var row = await _progressStore.TryGetAsync(tenantId, projectId, pipelineId, ct);
            if (row != null && row.SpecVersion > 0)
                finalizeVersion = row.SpecVersion;
        }

        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.StageConfirmed,
            FragmentId = RequirementSpecConstants.SpecStateFragmentId(pipelineId),
            FragmentType = IrFragmentTypes.RequirementSpecState,
            FragmentVersion = finalizeVersion,
            Payload = JsonSerializer.Serialize(new
            {
                stage = "S2",
                specVersion = finalizeVersion,
                contentHash = finalizeHash,
                confirmedBy = options?.ForceConfirm == true ? "pm-pipeline-force-confirm" : "pm-pipeline-finalize",
                forceConfirm = options?.ForceConfirm == true,
                forceReason = options?.ForceConfirm == true ? options.ForceReason : null,
                pmReviewScore = review.Score,
                pipelineId,
                timestamp = DateTimeOffset.UtcNow.ToString("O"),
            }, JsonOptions),
            SkillId = "requirement-analysis-orchestrator",
        }, ct);

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
        var snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);
        var reqFragment = snapshot.Find(IrFragmentTypes.Requirement, IrStabilityStates.Stable)
            ?? snapshot.Find(IrFragmentTypes.Requirement);
        if (reqFragment != null)
        {
            var text = ExtractRequirementText(reqFragment);
            if (!string.IsNullOrWhiteSpace(text)) return text;
        }

        // CR-20260718：历史 pipeline 可能尚未投影 Requirement fragment，从事件流读完整 payload
        foreach (var eventType in new[]
                 {
                     IrEventTypes.RequirementRefined,
                     IrEventTypes.RequirementEnhanced,
                     IrEventTypes.RequirementSpecRendered,
                 })
        {
            var payload = await _eventStore.GetLatestEventPayloadAsync(
                projectId, tenantId, pipelineId.ToString(), eventType, ct);
            var text = ExtractRequirementTextFromPayload(payload);
            if (!string.IsNullOrWhiteSpace(text)) return text;
        }

        var skeleton = snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable)
            ?? FindSkeletonAny(snapshot);
        var summary = ExtractRequirementSummaryFromSkeleton(skeleton?.Payload);
        if (!string.IsNullOrWhiteSpace(summary)) return summary;

        // CR-20260718：门控已通过但 IR 尚未投影 Requirement 时（用户发「继续」续连），
        // 从 S0 合并交付物读取——与 sa-gate handoff 的 InitialUserRequirement 同源。
        var mergedGateText = await TryReadMergedGateRequirementAsync(tenantId, projectId, pipelineId, ct);
        if (!string.IsNullOrWhiteSpace(mergedGateText))
        {
            _logger.LogInformation(
                "ResolveUserRequirement 从门控合并交付物读取 pipeline={PipelineId} len={Len}",
                pipelineId, mergedGateText.Length);
            return mergedGateText;
        }

        return string.Empty;
    }

    private static async Task<string?> TryReadMergedGateRequirementAsync(
        string tenantId, string projectId, long pipelineId, CancellationToken ct)
    {
        var pipelineKey = pipelineId.ToString();
        var path = Path.Combine(
            StudioWorkspaceHelper.GetDeliverablesPath(tenantId, projectId, pipelineKey),
            MergedGateRequirementRelativePath);
        StudioWorkspaceHelper.AssertWithinDeliverables(path, tenantId, projectId, pipelineKey);
        if (!File.Exists(path))
            return null;

        var text = await File.ReadAllTextAsync(path, ct);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    /// <summary>
    /// Finalize 路径：新流程优先读 02 正式交付物；无步骤④标记时降级 PM 步骤用需求文本（旧 pipeline 运维）。
    /// </summary>
    private async Task<SkillRunOptions> BuildFinalizeSkillRunOptionsAsync(
        RequirementAnalysisOptions? options,
        SkillContext? context,
        string tenantId,
        string projectId,
        long pipelineId,
        CancellationToken ct)
    {
        if (await HasEventAsync(tenantId, projectId, pipelineId, IrEventTypes.RequirementSpecRendered, ct))
        {
            var formalSpec = await ResolveConfirmedFormalSpecMarkdownAsync(tenantId, projectId, pipelineId, ct);
            return new SkillRunOptions
            {
                ProviderCode = options?.ProviderCode,
                UserRequirement = formalSpec,
            };
        }

        return await BuildSkillRunOptionsAsync(options, context, tenantId, projectId, pipelineId, ct);
    }

    /// <summary>
    /// PM 步骤①–③用：构造 SkillRunOptions（ refined 需求文本，非 02 正式交付物）。
    /// SkillHarness 禁止 DB 兜底，编排器 MUST 在此处显式填齐后再调用 harness。
    /// </summary>
    private async Task<SkillRunOptions> BuildSkillRunOptionsAsync(
        RequirementAnalysisOptions? options,
        SkillContext? context,
        string tenantId,
        string projectId,
        long pipelineId,
        CancellationToken ct)
    {
        string userRequirement;
        if (!string.IsNullOrWhiteSpace(options?.InitialUserRequirement))
            userRequirement = options.InitialUserRequirement!;
        else if (!string.IsNullOrWhiteSpace(context?.UserRequirement))
            userRequirement = context!.UserRequirement;
        else
            userRequirement = await ResolveUserRequirementAsync(tenantId, projectId, pipelineId, ct);

        if (string.IsNullOrWhiteSpace(userRequirement))
        {
            throw Oops.Bah(
                $"需求文本为空，无法启动 Skill。请先完成门控/澄清并确保 IR 已写入 Requirement 事件。pipelineId={pipelineId}");
        }

        return new SkillRunOptions
        {
            ProviderCode = options?.ProviderCode,
            UserRequirement = userRequirement,
        };
    }

    private static string? ExtractRequirementSummaryFromSkeleton(string? skeletonPayload)
    {
        if (string.IsNullOrWhiteSpace(skeletonPayload)) return null;
        try
        {
            var json = skeletonPayload.Trim();
            if (json.Length >= 2 && json[0] == '"' && json[^1] == '"')
                json = JsonSerializer.Deserialize<string>(json) ?? "{}";
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("requirementSummary", out var sum)
                && sum.ValueKind == JsonValueKind.String)
            {
                var text = sum.GetString();
                return string.IsNullOrWhiteSpace(text) ? null : text;
            }
        }
        catch (JsonException) { /* ignore */ }
        return null;
    }

    private static string? ExtractRequirementTextFromPayload(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                return t.GetString();
        }
        catch (JsonException) { /* ignore */ }
        return null;
    }

    /// <summary>续跑步骤③时合并基线需求与多轮澄清作答。</summary>
    private static string BuildRequirementWithClarificationAnswers(string? baseText, string? answersText)
    {
        if (string.IsNullOrWhiteSpace(answersText))
            return baseText ?? string.Empty;
        if (string.IsNullOrWhiteSpace(baseText))
            return answersText;
        return baseText + "\n\n【用户澄清作答】\n" + answersText;
    }

    /// <summary>从 fragment / 事件 / 上下文解析可用于 Refine 的需求文本（禁止 null 传入 Extract）。</summary>
    private async Task<string> ResolveEnhancedRequirementTextAsync(
        string tenantId, string projectId, long pipelineId,
        IrSnapshotFragment? requirementFragment, string? contextRequirement, CancellationToken ct)
    {
        var text = ExtractRequirementText(requirementFragment);
        if (!string.IsNullOrWhiteSpace(text)) return text;

        text = await ResolveUserRequirementAsync(tenantId, projectId, pipelineId, ct);
        if (!string.IsNullOrWhiteSpace(text)) return text;

        return contextRequirement ?? string.Empty;
    }

    /// <summary>从 Requirement fragment 提取需求文本。</summary>
    private static string? ExtractRequirementText(IrSnapshotFragment? fragment)
    {
        if (fragment == null || string.IsNullOrWhiteSpace(fragment.Payload)) return null;
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

    /// <summary>持久化 working 需求文本到 IR（步骤①/③；禁止用于步骤④正式版）。</summary>
    private async Task PersistWorkingRequirementAsync(
        long pipelineId, string tenantId, string projectId,
        string text, string eventType, CancellationToken ct)
    {
        var fragmentId = RequirementSpecConstants.WorkingRequirementFragmentId(pipelineId);
        var payload = JsonSerializer.Serialize(new
        {
            text,
            pipelineId,
            updatedAt = DateTimeOffset.UtcNow.ToString("O"),
        }, JsonOptions);
        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = eventType,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.Requirement,
            FragmentVersion = 1,
            Payload = payload,
            SkillId = "pm-skill",
        }, ct);
    }

    /// <summary>步骤④：仅 metadata，正文在 02 交付物。</summary>
    private async Task PersistSpecRenderedMetadataAsync(
        long pipelineId, string tenantId, string projectId,
        int specVersion, string contentHash, int contentLength, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new
        {
            specVersion,
            contentHash,
            contentLength,
            relativePath = FormalSpecRelativePath,
            pipelineId,
            updatedAt = DateTimeOffset.UtcNow.ToString("O"),
        }, JsonOptions);
        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.RequirementSpecRendered,
            FragmentId = RequirementSpecConstants.SpecStateFragmentId(pipelineId),
            FragmentType = IrFragmentTypes.RequirementSpecState,
            FragmentVersion = specVersion,
            Payload = payload,
            SkillId = "pm-skill",
        }, ct);
    }

    private async Task<bool> HasSpecRenderedMarkerAsync(
        string tenantId, string projectId, long pipelineId, CancellationToken ct)
    {
        if (_specResolver != null)
        {
            var snap = await _specResolver.ResolveAsync(tenantId, projectId, pipelineId, ct);
            if (snap.Phase >= RequirementSpecPhase.Rendered && snap.Phase != RequirementSpecPhase.Superseded)
                return true;
        }

        return await HasEventAsync(tenantId, projectId, pipelineId, IrEventTypes.RequirementSpecRendered, ct);
    }

    /// <summary>持久化需求文本到 IR（步骤①/③/④共用）。</summary>
    [Obsolete("Use PersistWorkingRequirementAsync or PersistSpecRenderedMetadataAsync")]
    private async Task PersistRequirementAsync(
        long pipelineId, string tenantId, string projectId,
        string text, string eventType, CancellationToken ct)
    {
        if (eventType == IrEventTypes.RequirementSpecRendered)
        {
            throw new InvalidOperationException("RequirementSpecRendered 禁止写入全文，请用 PersistSpecRenderedMetadataAsync");
        }

        await PersistWorkingRequirementAsync(pipelineId, tenantId, projectId, text, eventType, ct);
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

    /// <summary>
    /// ADR-005 结构化澄清卡片作答 → PM 对话式 turns（步骤①/③续跑）。
    /// EmitClarificationRequestedAsync 不写 PmClarificationTurn，须从 ClarificationAnswered 回灌。
    /// </summary>
    private static List<PmClarificationTurn> BuildPmTurnsFromStructuredClarification(
        IrSnapshotFragment? reqClar, string mergedAnswersText, ClarificationSource source)
    {
        return new List<PmClarificationTurn>
        {
            new()
            {
                Question = ExtractClarificationQuestionSummary(reqClar),
                UserAnswer = mergedAnswersText,
                Source = source,
            },
        };
    }

    private static string ExtractClarificationQuestionSummary(IrSnapshotFragment? reqClar)
    {
        if (reqClar != null && !string.IsNullOrWhiteSpace(reqClar.Payload))
        {
            try
            {
                var set = JsonSerializer.Deserialize<ClarificationSet>(reqClar.Payload, JsonOptions);
                if (set?.Questions is { Count: > 0 })
                {
                    if (!string.IsNullOrWhiteSpace(set.Title))
                        return set.Title;
                    return string.Join("；", set.Questions.Select(q => q.Text));
                }
            }
            catch (JsonException) { /* 已作答 payload 非 ClarificationSet */ }
        }

        return "结构化澄清追问";
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
        IrSnapshotFragment? inProgress = null;
        IrSnapshotFragment? stable = null;
        foreach (var f in snapshot.Fragments)
        {
            if (f.FragmentType != IrFragmentTypes.Clarification
                || f.FragmentId?.StartsWith(prefix, StringComparison.Ordinal) != true)
                continue;

            if (f.StabilityState == IrStabilityStates.InProgress)
                inProgress = f;
            else if (f.StabilityState == IrStabilityStates.Stable)
                stable = f;
        }

        // 待答卡片优先；已作答后仅 stable 存在
        return inProgress ?? stable;
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
        => (await LoadRequirementClarificationAnswerStateAsync(
            tenantId, projectId, pipelineId, null, ct)).AnsweredCount;

    private sealed record RequirementClarificationAnswerState(
        int AnsweredCount, int MaxAnsweredRound, string MergedAnswersText);

    private async Task<RequirementClarificationAnswerState> LoadRequirementClarificationAnswerStateAsync(
        string tenantId, string projectId, long pipelineId,
        IrSnapshotFragment? reqClar, CancellationToken ct)
    {
        var parts = new List<string>();
        var count = 0;
        var maxRound = 0;

        var payloads = await _eventStore.ListFullEventPayloadsAsync(
            projectId, tenantId, pipelineId.ToString(), IrEventTypes.ClarificationAnswered, ct);
        foreach (var payload in payloads)
        {
            if (!IsRequirementStageClarificationPayload(payload))
                continue;
            var text = ExtractAnswersText(payload);
            if (string.IsNullOrWhiteSpace(text))
                continue;
            var round = ExtractAnswerRound(payload);
            count++;
            var effectiveRound = round > 0 ? round : count;
            maxRound = Math.Max(maxRound, effectiveRound);
            parts.Add($"【第 {effectiveRound} 轮澄清作答】\n{text}");
        }

        if (parts.Count == 0)
        {
            var fromFragment = ExtractAnswersText(reqClar?.Payload);
            if (!string.IsNullOrWhiteSpace(fromFragment))
            {
                count = 1;
                maxRound = ExtractAnswerRound(reqClar?.Payload);
                if (maxRound <= 0) maxRound = 1;
                parts.Add($"【第 {maxRound} 轮澄清作答】\n{fromFragment}");
            }
        }

        return new RequirementClarificationAnswerState(
            count, maxRound, string.Join("\n\n", parts));
    }

    private static bool IsRequirementStageClarificationPayload(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return false;
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (!doc.RootElement.TryGetProperty("stage", out var stageEl)
                || stageEl.ValueKind != JsonValueKind.String)
            {
                // 兼容无 stage 字段的历史事件（343 等）
                return true;
            }
            var stage = stageEl.GetString();
            return RequirementAnalysisStages.IsRequirementAnalysisStage(stage);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static int ExtractAnswerRound(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return 0;
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.TryGetProperty("round", out var roundEl)
                && roundEl.TryGetInt32(out var round))
                return round;
        }
        catch (JsonException) { /* ignore */ }
        return 0;
    }

    private static int ExtractPendingClarificationRound(IrSnapshotFragment? reqClar)
    {
        if (reqClar == null
            || reqClar.StabilityState != IrStabilityStates.InProgress
            || string.IsNullOrWhiteSpace(reqClar.Payload))
            return 0;

        try
        {
            var set = JsonSerializer.Deserialize<ClarificationSet>(reqClar.Payload, JsonOptions);
            return set?.Round ?? 0;
        }
        catch (JsonException)
        {
            return 0;
        }
    }

    private static bool HasPendingClarification(IrSnapshot snapshot)
        => snapshot.Find(IrFragmentTypes.Clarification, IrStabilityStates.InProgress) != null;

    private static bool IsContinueResumeKeyword(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.Trim();
        return trimmed.Equals("继续", StringComparison.Ordinal)
            || trimmed.Equals("继续分析", StringComparison.Ordinal)
            || trimmed.Equals("ok", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("好的", StringComparison.Ordinal);
    }

    private Task<RequirementAnalysisOrchestratorResult> ReturnPendingRequirementClarificationAsync(
        long pipelineId, string tenantId, string projectId,
        IrSnapshotFragment? reqClar, int pmStep, CancellationToken ct)
    {
        ClarificationSet? pending = null;
        if (reqClar != null && !string.IsNullOrWhiteSpace(reqClar.Payload))
        {
            try
            {
                pending = JsonSerializer.Deserialize<ClarificationSet>(reqClar.Payload, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex,
                    "RunPmPipeline 待答澄清 payload 反序列化失败 pipeline={PipelineId}", pipelineId);
            }
        }

        var round = pending?.Round ?? 1;
        _logger.LogInformation(
            "RunPmPipeline 澄清仍为 in-progress，重推 SSE pipeline={PipelineId} round={Round} questions={Count}",
            pipelineId, round, pending?.Questions.Count ?? 0);
        _ = TransitionPmStepAsync(
            pipelineId, tenantId, projectId,
            new PmStepTransition
            {
                Step = pmStep,
                Phase = "awaiting_user",
                Message = $"⏸️ 步骤{pmStep}·第 {round} 轮澄清题等待作答，请在下方卡片中选择…",
                Percent = 58,
                PipelineStage = pmStep >= 3
                    ? S2PipelineStage.PmRefineAwaitingUser
                    : S2PipelineStage.PmEnhanceAwaitingUser,
                AwaitingUser = true,
                ClarRound = round,
            },
            ct);

        if (pending != null)
            _sseHub.TryPush(pipelineId, "clarification_requested",
                JsonSerializer.Serialize(pending, JsonOptions));

        return Task.FromResult(new RequirementAnalysisOrchestratorResult
        {
            Status = "awaiting-clarification",
            PendingClarification = pending,
        });
    }

    private static IrSnapshotFragment BuildSyntheticClarificationFragment(
        IrSnapshotFragment? reqClar, string answersText, int round)
        => new()
        {
            FragmentId = reqClar?.FragmentId ?? $"clarification:{ClarificationStages.Requirement}:synthetic",
            FragmentType = IrFragmentTypes.Clarification,
            StabilityState = IrStabilityStates.Stable,
            Payload = JsonSerializer.Serialize(new { answersText, round }, JsonOptions),
        };

    /// <summary>
    /// 步骤④前置条件：步骤③ RequirementRefined + 九步 stable + ≥MinPmOptimizationRounds 轮 ClarificationAnswered + 无在途追问。
    /// </summary>
    private async Task<bool> IsReadyForSpecDeliveryAsync(
        string tenantId, string projectId, long pipelineId, IrSnapshot snapshot, CancellationToken ct)
    {
        if (HasPendingClarification(snapshot)) return false;
        if (!await HasEventAsync(tenantId, projectId, pipelineId, IrEventTypes.RequirementRefined, ct))
            return false;
        if (snapshot.Find("IR1_SaNineView", IrStabilityStates.Stable) == null
            && !await HasEventAsync(tenantId, projectId, pipelineId, IrEventTypes.SaNineViewCompiled, ct))
            return false;
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

        // 确认意图关键词（用户确认需求说明书 / 明确要求进入架构设计）
        var architectureAdvanceKeywords = new[] { "进入架构", "架构设计", "开始架构", "启动架构" };
        if (hasSpecRendered && architectureAdvanceKeywords.Any(k =>
                trimmed.Contains(k, StringComparison.OrdinalIgnoreCase)))
        {
            return ("confirm_spec", 0.95);
        }

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
