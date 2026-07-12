using System.Text;
using System.Text.Json;
using JNPF.Common.Core.MultiTenancy;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Common;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Dto.Skills;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Infrastructure.Background;
using JNPF.InteAssistant.Infrastructure.Security;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Runtime;
using JNPF.InteAssistant.Sa;
using JNPF.InteAssistant.Skills.Cognitive;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Skills;

[ApiDescriptionSettings(Tag = "Studio", Name = "StudioSkills", Order = 193)]
[Route("api/studio/skills")]
public class SkillsApiService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ISkillHarness _harness;
    private readonly IBackgroundTaskRunner _taskRunner;
    private readonly ITenantGuard _tenantGuard;
    private readonly IDomainSeedService _seedService;
    private readonly IIrEventStoreService _eventStore;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAnalystAffectedStepsRerunService _rerunService;
    private readonly ITenantPipelineQuotaGuard _quotaGuard;
    private readonly ISkillRunGuard _runGuard;
    private readonly IExperienceRecorder _experience;
    private readonly IPipelineTripleResolver _tripleResolver;
    private readonly ISaMaterializationService _materializationService;
    private readonly IRequirementAnalysisOrchestrator _requirementOrchestrator;
    private readonly PmSkillService _pm;
    private readonly ISaNineViewCompiler _compiler;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public SkillsApiService(
        ISqlSugarClient db,
        ISkillHarness harness,
        IBackgroundTaskRunner taskRunner,
        ITenantGuard tenantGuard,
        IDomainSeedService seedService,
        IIrEventStoreService eventStore,
        IHttpContextAccessor httpContextAccessor,
        IAnalystAffectedStepsRerunService rerunService,
        ITenantPipelineQuotaGuard quotaGuard,
        ISkillRunGuard runGuard,
        IExperienceRecorder experience,
        IPipelineTripleResolver tripleResolver,
        ISaMaterializationService materializationService,
        IRequirementAnalysisOrchestrator requirementOrchestrator,
        ISaNineViewCompiler compiler,
        PmSkillService pm)
    {
        _db = db;
        _harness = harness;
        _taskRunner = taskRunner;
        _tenantGuard = tenantGuard;
        _seedService = seedService;
        _eventStore = eventStore;
        _httpContextAccessor = httpContextAccessor;
        _rerunService = rerunService;
        _quotaGuard = quotaGuard;
        _runGuard = runGuard;
        _experience = experience;
        _tripleResolver = tripleResolver;
        _materializationService = materializationService;
        _requirementOrchestrator = requirementOrchestrator;
        _compiler = compiler;
        _pm = pm;
    }

    [HttpPost("pm/{pipelineId:long}/run")]
    public Task<object> RunPmAsync(long pipelineId, [FromBody] SkillRunRequest? request)
        => RunSkillAsync("pm-skill", pipelineId, request);

    [HttpPost("analyst/{pipelineId:long}/run")]
    public Task<object> RunAnalystAsync(long pipelineId, [FromBody] SkillRunRequest? request)
        => RunSkillAsync("analyst-skill", pipelineId, request);

    /// <summary>
    /// 三轮需求分析编排器入口（27 号 §5）。
    /// 首次调用从 Round 1 开始，每轮出题后返回 awaiting-answer；
    /// 用户作答后再次调用恢复下一轮（幂等：据 IR 状态定位未完成轮次）。
    /// </summary>
    [HttpPost("requirement-analysis/{pipelineId:long}/run")]
    public async Task<object> RunRequirementAnalysisAsync(
        long pipelineId, [FromBody] RequirementAnalysisRunRequest? request)
    {
        var runId = Guid.NewGuid().ToString("N");
        var taskName = $"req-analysis:{pipelineId}:{runId}";
        var tenantSnapshot = RequestContext.Capture(_httpContextAccessor).TenantId;

        _taskRunner.Run(taskName, async (ctx, ct) =>
        {
            var (projectId, tenantId) = await ResolveProjectAsync(pipelineId, ctx, tenantSnapshot);
            var options = new RequirementAnalysisOptions
            {
                ProviderCode = request?.ProviderCode,
                CurrentRoundAnswers = request?.Answers,
                ForceRefinalize = request?.ForceRefinalize == true,
            };
            await _requirementOrchestrator.RunAsync(pipelineId, tenantId, projectId, options, ct);
        }, timeout: TimeSpan.FromMinutes(35));

        return new
        {
            runId,
            pipelineId,
            status = "running",
            message = "三轮需求分析编排器已启动",
        };
    }

    /// <summary>
    /// EventSpecRevised 后重跑受影响 SA 步骤（D11）
    /// </summary>
    [HttpPost("analyst/{pipelineId:long}/events/{eventId}/rerun-affected")]
    public Task<object> RerunAffectedStepsAsync(
        long pipelineId, string eventId, [FromBody] RerunAffectedStepsInput? input)
    {
        var runId = Guid.NewGuid().ToString("N");
        var taskName = $"analyst-rerun:{pipelineId}:{eventId}:{runId}";
        var tenantSnapshot = RequestContext.Capture(_httpContextAccessor).TenantId;

        _taskRunner.Run(taskName, async (ctx, ct) =>
        {
            var (projectId, tenantId) = await ResolveProjectAsync(pipelineId, ctx, tenantSnapshot);
            await _rerunService.RunAsync(tenantId, projectId, pipelineId, eventId, input, ct);
        }, timeout: TimeSpan.FromMinutes(10));

        return Task.FromResult<object>(new
        {
            runId,
            pipelineId,
            eventId,
            status = "running",
            message = "受影响 SA 步骤重跑已启动",
        });
    }

    [HttpPost("pm/{pipelineId:long}/confirm-skeleton")]
    public async Task<object> ConfirmSkeletonAsync(long pipelineId, [FromBody] ConfirmSkeletonRequest? request)
    {
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);
        var snapshots = await _eventStore.ListSnapshotsAsync(projectId, tenantId, pipelineId.ToString());
        var skeleton = snapshots.FirstOrDefault(s =>
            s.FragmentType == IrFragmentTypes.Skeleton || s.FragmentId?.StartsWith("skeleton:", StringComparison.Ordinal) == true);

        if (skeleton == null)
            throw Oops.Bah("无 IR-0 骨架，请先运行 PM Skill");

        if (skeleton.StabilityState is IrStabilityStates.Stable or IrStabilityStates.Locked)
            return new { status = "already_stable", fragmentId = skeleton.FragmentId };

        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.StageConfirmed,
            FragmentId = skeleton.FragmentId,
            FragmentType = skeleton.FragmentType,
            FragmentVersion = skeleton.CurrentVersion,
            Payload = JsonSerializer.Serialize(new
            {
                fragmentId = skeleton.FragmentId,
                stage = "IR-0",
                confirmedBy = "user-hitl",
            }, JsonOptions),
            SkillId = "pm-skill",
        });

        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.FragmentStabilized,
            FragmentId = skeleton.FragmentId,
            FragmentType = skeleton.FragmentType,
            FragmentVersion = skeleton.CurrentVersion,
            Payload = JsonSerializer.Serialize(new
            {
                fragmentId = skeleton.FragmentId,
                stabilityState = IrStabilityStates.Stable,
                confirmedBy = "user-hitl",
            }, JsonOptions),
            SkillId = "pm-skill",
        });

        if (request?.AutoRunAnalyst == true)
        {
            // 30 号 W1：生产主路径切到三轮需求分析编排器，禁止默认旧 analyst-skill
            await RunRequirementAnalysisAsync(pipelineId, null);
        }

        await _experience.RecordReviewAsync(
            projectId, tenantId, "pm-skill",
            request?.RunId ?? $"hitl-skeleton-{pipelineId}",
            "approved",
            JsonSerializer.Serialize(new
            {
                fragmentId = skeleton.FragmentId,
                source = "confirm-skeleton",
                autoRunAnalyst = request?.AutoRunAnalyst == true,
                nextSkill = "requirement-analysis",
            }, JsonOptions));

        return new
        {
            status = "confirmed",
            fragmentId = skeleton.FragmentId,
            autoRunAnalyst = request?.AutoRunAnalyst == true,
            nextAction = request?.AutoRunAnalyst == true ? "continue-requirement-analysis" : null,
            message = request?.AutoRunAnalyst == true
                ? "骨架已确认，三轮需求分析编排器已启动"
                : "骨架已确认",
        };
    }

    [HttpPost("analyst/{pipelineId:long}/confirm-requirement-spec")]
    public async Task<object> ConfirmRequirementSpecAsync(long pipelineId, [FromBody] ConfirmRequirementSpecRequest? request)
    {
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);
        var snapshots = await _eventStore.ListSnapshotsAsync(projectId, tenantId, pipelineId.ToString());
        var eventSpecs = snapshots
            .Where(s => s.FragmentType == IrFragmentTypes.EventSpec && s.StabilityState is IrStabilityStates.Stable or IrStabilityStates.Locked)
            .ToList();

        if (eventSpecs.Count == 0)
            throw Oops.Bah("无稳定 EventSpec，请先完成 Analyst Skill 生成需求分析说明书");

        var pmReview = await LoadLatestPmReviewAsync(projectId, tenantId, pipelineId);
        var forceConfirm = request?.ForceConfirm == true;
        if (!forceConfirm && (pmReview == null || pmReview.Score < 85))
        {
            var gapsText = pmReview?.Gaps is { Count: > 0 }
                ? "；缺口：" + string.Join("；", pmReview.Gaps)
                : "；请等待 PM 终评完成或选择强制确认";
            throw Oops.Bah($"PM 终评分数不足 85，当前分数：{pmReview?.Score.ToString() ?? "未评审"}{gapsText}");
        }

        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.StageConfirmed,
            Payload = JsonSerializer.Serialize(new
            {
                stage = "S2",
                confirmedBy = "user-hitl",
                eventSpecCount = eventSpecs.Count,
                forceConfirm,
                pmScore = pmReview?.Score,
            }, JsonOptions),
            SkillId = "analyst-skill",
        });

        if (request?.AutoRunDesign == true)
            await RunSkillAsync(DesignSkillIds.Architect, pipelineId, null);

        await _experience.RecordReviewAsync(
            projectId, tenantId, "analyst-skill",
            request?.RunId ?? $"hitl-requirement-spec-{pipelineId}",
            "approved",
            JsonSerializer.Serialize(new
            {
                source = "confirm-requirement-spec",
                autoRunDesign = request?.AutoRunDesign == true,
                eventSpecCount = eventSpecs.Count,
                forceConfirm,
                pmScore = pmReview?.Score,
            }, JsonOptions));

        var taskName = $"sa-materialize:{pipelineId}";
        var tenantSnapshot = RequestContext.Capture(_httpContextAccessor).TenantId;
        _taskRunner.Run(taskName, async (ctx, ct) =>
        {
            var triple = await _tripleResolver.ResolveAsync(pipelineId, ctx, tenantSnapshot, ct);
            await _materializationService.MaterializeAfterConfirmAsync(triple, ct);
        }, timeout: TimeSpan.FromMinutes(10));

        return new { status = "confirmed", stage = "S2", autoRunDesign = request?.AutoRunDesign == true, materialization = "enqueued" };
    }

    [HttpPost("requirement-analysis/{pipelineId:long}/amend/propose")]
    public async Task<object> ProposeRequirementAmendmentAsync(
        long pipelineId, [FromBody] PmAmendProposeRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserMessage))
            throw Oops.Bah("补充需求不能为空");

        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);
        if (await HasS2ConfirmedAsync(projectId, tenantId, pipelineId))
            throw Oops.Bah("需求分析说明书已确认，补充需求请通过二次开发/fork 流程处理");

        var context = new SkillContext
        {
            RunId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            ProjectId = projectId,
            PipelineId = pipelineId,
            UserRequirement = request.UserMessage,
            ProviderCode = request.ProviderCode,
        };
        var result = await _pm.AmendProposeAsync(context, request.UserMessage, CancellationToken.None);
        using var execScope = SkillExecutionScope.Begin(
            context.RunId, tenantId, projectId, pipelineId, "pm-skill", CancellationToken.None);
        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.RequirementAmendmentProposed,
            FragmentId = $"req-amend:{result.ProposalId}",
            FragmentType = IrFragmentTypes.Clarification,
            Payload = JsonSerializer.Serialize(new
            {
                result.ProposalId,
                userMessage = request.UserMessage,
                result.Understanding,
                pipelineId,
            }, JsonOptions),
            SkillId = "pm-skill",
        });

        return result;
    }

    [HttpPost("requirement-analysis/{pipelineId:long}/amend/apply")]
    public async Task<object> ApplyRequirementAmendmentAsync(
        long pipelineId, [FromBody] PmAmendApplyRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ProposalId))
            throw Oops.Bah("ProposalId 不能为空");

        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);
        if (await HasS2ConfirmedAsync(projectId, tenantId, pipelineId))
            throw Oops.Bah("需求分析说明书已确认，补充需求请通过二次开发/fork 流程处理");

        var appliedCount = (await _eventStore.ListEventsAsync(projectId, tenantId, pipelineId.ToString()))
            .Count(e => string.Equals(e.EventType, IrEventTypes.RequirementAmendmentApplied, StringComparison.Ordinal));
        if (appliedCount >= 3)
            throw Oops.Bah("本次需求分析最多允许应用 3 次补充修改，请确认当前说明书或进入二次开发流程");

        var understanding = request.Understanding ?? new AmendmentUnderstanding
        {
            SummaryMarkdown = request.UserMessage ?? string.Empty,
            Features = string.IsNullOrWhiteSpace(request.UserMessage)
                ? new List<string>()
                : new List<string> { request.UserMessage! },
        };
        var context = new SkillContext
        {
            RunId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            ProjectId = projectId,
            PipelineId = pipelineId,
            UserRequirement = request.UserMessage ?? understanding.SummaryMarkdown,
            ProviderCode = request.ProviderCode,
        };
        var deltaText = await _pm.ApplyAmendmentAsync(context, understanding, CancellationToken.None);
        string? patchedSkeletonJson = null;
        string? patchedBundleHash = null;
        string? patchedSkeletonFragmentId = null;
        if (understanding.Patches.Count > 0)
        {
            var snapshots = await _eventStore.ListSnapshotsAsync(projectId, tenantId, pipelineId.ToString());
            var skeleton = snapshots.FirstOrDefault(s =>
                s.FragmentType == IrFragmentTypes.Skeleton && s.StabilityState == IrStabilityStates.Stable);
            if (skeleton == null)
                throw Oops.Bah("未找到稳定需求骨架，无法应用类型化补丁");

            patchedSkeletonFragmentId = skeleton.FragmentId;
            var skeletonJson = ResolveSkeletonPayloadJson(skeleton.Payload);
            if (!skeletonJson.TrimStart().StartsWith('{'))
                throw Oops.Bah("需求骨架 payload 不是合法 JSON，无法应用类型化补丁");
            patchedSkeletonJson = AmendmentPatchApplier.ApplyToSkeletonJson(skeletonJson, understanding.Patches);
            var patchedCompile = _compiler.CompileFromSkeletonJson(patchedSkeletonJson);
            patchedBundleHash = patchedCompile.BundleHash;
        }

        using var execScope = SkillExecutionScope.Begin(
            context.RunId, tenantId, projectId, pipelineId, "pm-skill", CancellationToken.None);

        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.RequirementAmendmentApplied,
            FragmentId = $"req-amend:{request.ProposalId}",
            FragmentType = IrFragmentTypes.Clarification,
            Payload = JsonSerializer.Serialize(new
            {
                request.ProposalId,
                understanding,
                deltaText,
                patchedSkeletonJson,
                patchedBundleHash,
                patchCount = understanding.Patches.Count,
                pipelineId,
                appliedBy = "user-hitl",
            }, JsonOptions),
            SkillId = "pm-skill",
        });

        if (!string.IsNullOrWhiteSpace(patchedSkeletonJson) && !string.IsNullOrWhiteSpace(patchedSkeletonFragmentId))
        {
            await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
            {
                EventType = IrEventTypes.SkeletonCreated,
                FragmentId = patchedSkeletonFragmentId,
                FragmentType = IrFragmentTypes.Skeleton,
                Payload = patchedSkeletonJson,
                SkillId = "pm-skill",
            });
            await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
            {
                EventType = IrEventTypes.FragmentStabilized,
                FragmentId = patchedSkeletonFragmentId,
                FragmentType = IrFragmentTypes.Skeleton,
                Payload = JsonSerializer.Serialize(new
                {
                    reason = "typed-amendment-patch",
                    patchCount = understanding.Patches.Count,
                    request.ProposalId,
                }, JsonOptions),
                SkillId = "pm-skill",
            });
        }

        await SaveClarificationAsUserMessageAsync(pipelineId, projectId, deltaText);
        var rerunResult = await _requirementOrchestrator.RunAsync(
            pipelineId,
            tenantId,
            projectId,
            new RequirementAnalysisOptions
            {
                ProviderCode = request.ProviderCode,
                ForceRefinalize = true,
            },
            CancellationToken.None);

        if (string.Equals(rerunResult.Status, "failed", StringComparison.OrdinalIgnoreCase))
            throw Oops.Oh(rerunResult.ErrorMessage ?? "补充需求已应用，但重新生成需求分析说明书失败");

        return new
        {
            status = "applied",
            request.ProposalId,
            deltaText,
            nextAction = "refresh-requirement-spec",
            reviewRefreshed = true,
        };
    }

    // ════════════════════════════════════════════════════════════════
    // ADR-005 交互式澄清问答：用户作答端点
    //
    // 关键题（ClarificationQuestion.Required=true）硬门控：未作答则 Oops.Bah 拒绝推进。
    // 作答后写 ClarificationAnswered IR 事件 + 把答案格式化为 user message 存入对话历史，
    // 前端据此重新发起 sa-gate 触发下一轮 maturity 评估（提问→作答→再评估闭环）。
    // ════════════════════════════════════════════════════════════════

    /// <summary>提交一轮澄清问答的答案（关键题必答，否则拒绝推进）。</summary>
    [HttpPost("clarification/{pipelineId:long}/answer")]
    public async Task<object> AnswerClarificationAsync(
        long pipelineId, [FromBody] AnswerClarificationRequest? request)
    {
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);

        if (request == null || string.IsNullOrWhiteSpace(request.SetId))
            throw Oops.Bah("SetId 不能为空");

        // 1. 取回原始 ClarificationSet（按 setId 从 IR 事件流定位）— skipAll 也需要它来判定 stage
        var set = await LoadClarificationSetAsync(projectId, tenantId, pipelineId.ToString(), request.SetId);
        if (set == null)
            throw Oops.Bah($"未找到提问集合 {request.SetId}，可能已过期或已作答");

        var stage = set.Stage;
        var skipFragmentId = $"clarification:{stage}:{projectId}";

        // 逃生口：用户选择"全部跳过直接分析" → 写一条 SkipAll 答案事件，前端据 stage 决定后续
        if (request.SkipAll)
        {
            // 架构阶段 skipAll：注入 JNPF 平台默认架构假设（30 号 §SG3 打回修复：禁止空 answersText）
            // 总体设计阶段 skipAll：注入 JNPF 平台默认总体设计假设（30 号 §SG4 打回修复：同 SG3 模式）
            var skipAllAnswersText = stage switch
            {
                ClarificationStages.Architecture
                    => "（用户选择全部跳过，沿用 JNPF 平台默认架构假设：模块化单体部署 + SQL Server + Redis 缓存 + 分层架构 pattern=layered）",
                ClarificationStages.SystemDesign
                    => "（用户选择全部跳过，沿用 JNPF 平台默认总体设计假设：部署拓扑=单机部署（低并发<100），集成方式=REST API，非功能需求=JNPF 平台内置安全/日志/缓存默认策略）",
                _ => "（用户选择全部跳过，沿用默认假设）",
            };

            await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
            {
                EventType = IrEventTypes.ClarificationAnswered,
                FragmentId = skipFragmentId,
                FragmentType = IrFragmentTypes.Clarification,
                Payload = JsonSerializer.Serialize(new
                {
                    setId = request.SetId,
                    stage,
                    skippedAll = true,
                    answersText = skipAllAnswersText,
                    confirmedBy = "user-hitl",
                }, JsonOptions),
                SkillId = stage switch
                {
                    ClarificationStages.Requirement => "requirement-gate",
                    ClarificationStages.Architecture => DesignSkillIds.Architect,
                    ClarificationStages.SystemDesign => DesignSkillIds.SystemDesignClarification,
                    _ when RequirementAnalysisStages.IsRequirementAnalysisStage(stage)
                        => "requirement-analysis-orchestrator",
                    _ => DesignSkillIds.Architect,
                },
            });

            // 需求阶段：写一条 user message 引导下一轮进入 refine（ForceRefine 关键词）
            if (stage == ClarificationStages.Requirement)
                await SaveClarificationAsUserMessageAsync(pipelineId, projectId, "开始分析");

            return new AnswerClarificationResult
            {
                Status = "skipped",
                SetId = request.SetId,
                FragmentId = skipFragmentId,
                StabilityState = IrStabilityStates.Stable,
                // skipAll 也要推进流程：需求阶段重新评估，架构/总体设计阶段重跑对应 Skill
                TriggerNextRound = true,
                Stage = stage,
                NextAction = ResolveClarificationNextAction(stage),
            };
        }

        // 2. 关键题硬门控：required 题必须作答
        var answeredIds = (request.Answers ?? new()).Select(a => a.QuestionId).ToHashSet(StringComparer.Ordinal);
        var skipped = (request.SkippedQuestionIds ?? new()).ToHashSet(StringComparer.Ordinal);

        foreach (var q in set.Questions.Where(x => x.Required))
        {
            if (!answeredIds.Contains(q.Id))
                throw Oops.Bah($"关键问题「{q.Text}」必须作答才能继续");
        }
        // required 题不允许出现在 skipped 列表（防止前端误传）
        var invalidSkip = skipped.Intersect(set.Questions.Where(x => x.Required).Select(x => x.Id)).ToList();
        if (invalidSkip.Count > 0)
            throw Oops.Bah("关键问题不允许跳过：" + string.Join("、", invalidSkip));

        // 3. 校验选项合法性（防注入/乱传 id）
        var validOptionByQuestion = set.Questions.ToDictionary(
            q => q.Id, q => q.Options.Select(o => o.Id).ToHashSet(StringComparer.Ordinal),
            StringComparer.Ordinal);
        foreach (var ans in request.Answers ?? new())
        {
            if (!validOptionByQuestion.TryGetValue(ans.QuestionId, out var validOpts))
                throw Oops.Bah($"答案引用了不存在的问题：{ans.QuestionId}");
            foreach (var oid in ans.OptionIds ?? new())
                if (!validOpts.Contains(oid))
                    throw Oops.Bah($"答案引用了不存在的选项：{oid}");

            // 矩阵行校验：每一行的 SelectedOption 必须引用合法选项
            if (ans.MatrixRowAnswers is { Count: > 0 })
            {
                var matrixQ = set.Questions.FirstOrDefault(x => x.Id == ans.QuestionId);
                if (matrixQ?.MatrixSubItems is { Count: > 0 })
                {
                    var validRowIds = matrixQ.MatrixSubItems.Select(r => r.RowId).ToHashSet(StringComparer.Ordinal);
                    foreach (var rowAns in ans.MatrixRowAnswers)
                    {
                        if (!validRowIds.Contains(rowAns.RowId))
                            throw Oops.Bah($"矩阵行答案引用了不存在的行：{rowAns.RowId}");
                        if (!string.IsNullOrWhiteSpace(rowAns.SelectedOption))
                        {
                            // MATRIX_MULTI：逗号分隔多 ID
                            var rowOptionIds = rowAns.SelectedOption.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            foreach (var rid in rowOptionIds)
                                if (!validOpts.Contains(rid.Trim()))
                                    throw Oops.Bah($"矩阵行「{rowAns.RowLabel}」引用了不存在的选项：{rid}");
                        }
                    }
                }
            }
        }

        // 4. 写 ClarificationAnswered IR 事件（fragment 进入 stable）
        var fragmentId = $"clarification:{set.Stage}:{projectId}";
        var allRequiredAnswered = set.Questions.Where(x => x.Required).All(x => answeredIds.Contains(x.Id));
        var messageText = FormatAnswersAsUserMessage(set, request);
        var filledSlotIds = ClarificationAnswerPatchMapper.DetectFilledSlots(
            messageText, set.TargetSlotIds);
        // 用户实际作答（非 skip）时，本轮 TargetSlotIds 视为已覆盖，避免下一轮重复问同一槽
        if (!request.SkipAll && set.TargetSlotIds is { Count: > 0 })
        {
            filledSlotIds = ClarificationAnswerPatchMapper.DetectFilledSlots(
                messageText + "\n" + string.Join("\n", set.TargetSlotIds),
                set.TargetSlotIds);
        }

        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.ClarificationAnswered,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.Clarification,
            FragmentVersion = set.Round,
            Payload = JsonSerializer.Serialize(new
            {
                setId = set.SetId,
                stage = set.Stage,
                round = set.Round,
                answers = request.Answers,
                skippedQuestionIds = request.SkippedQuestionIds,
                allRequiredAnswered,
                answersText = messageText,
                filledSlotIds,
                targetSlotIds = set.TargetSlotIds,
                confirmedBy = "user-hitl",
            }, JsonOptions),
            SkillId = set.Stage switch
            {
                ClarificationStages.Requirement => "requirement-gate",
                ClarificationStages.Architecture => DesignSkillIds.Architect,
                ClarificationStages.SystemDesign => DesignSkillIds.SystemDesignClarification,
                _ when RequirementAnalysisStages.IsRequirementAnalysisStage(set.Stage)
                    => "requirement-analysis-orchestrator",
                _ => "requirement-gate",
            },
        });

        // 5. 把答案格式化为 user message 存入对话历史（下一轮 maturity 评估的输入）
        await SaveClarificationAsUserMessageAsync(pipelineId, projectId, messageText);

        await _experience.RecordReviewAsync(
            projectId, tenantId, "requirement-gate",
            $"clarification-{request.SetId}",
            "approved",
            JsonSerializer.Serialize(new
            {
                source = "clarification-answer",
                setId = request.SetId,
                stage = set.Stage,
                round = set.Round,
                answeredCount = (request.Answers?.Count ?? 0),
                skippedCount = (request.SkippedQuestionIds?.Count ?? 0),
            }, JsonOptions));

        return new AnswerClarificationResult
        {
            Status = "answered",
            SetId = set.SetId,
            FragmentId = fragmentId,
            StabilityState = allRequiredAnswered ? IrStabilityStates.Stable : IrStabilityStates.InProgress,
            Stage = set.Stage,
            // 需求阶段：前端重新发 sa-gate 做下一轮 maturity 评估
            // 架构阶段：前端重新运行 architect-skill（阶段二 ToT）
            // 总体设计阶段：前端重新运行 system-design-clarification-skill（阶段二约束引擎 + 锁定）
            // 三轮需求分析：前端续跑 requirement-analysis/run
            TriggerNextRound = true,
            NextAction = ResolveClarificationNextAction(set.Stage),
        };
    }

    /// <summary>按澄清 stage 解析前端 nextAction（含三轮需求分析编排器）。</summary>
    private static string ResolveClarificationNextAction(string stage) => stage switch
    {
        ClarificationStages.Requirement => "re-evaluate",
        ClarificationStages.Architecture => "rerun-architect",
        ClarificationStages.SystemDesign => "rerun-system-design-clarification",
        _ when RequirementAnalysisStages.IsRequirementAnalysisStage(stage)
            => "continue-requirement-analysis",
        _ => "none",
    };

    /// <summary>从 IR 事件流按 setId 加载原始 ClarificationSet（ClarificationRequested 事件 payload）。</summary>
    private async Task<ClarificationSet?> LoadClarificationSetAsync(
        string projectId, string tenantId, string pipelineId, string setId)
    {
        // 从 IR fragment snapshot 加载（payload 完整，不受 IrEventDto.PayloadPreview 500 字符截断影响）。
        // ClarificationRequested 事件投递后，IrProjectionEngine.UpsertClarificationAsync 把完整
        // ClarificationSet JSON 存到了 IR1_Clarification fragment 的 IrContent。
        var snapshots = await _eventStore.ListSnapshotsAsync(projectId, tenantId, pipelineId);
        foreach (var snap in snapshots.Where(s => s.FragmentType == IrFragmentTypes.Clarification))
        {
            var payloadJson = snap.Payload is string s ? s : JsonSerializer.Serialize(snap.Payload ?? "{}");
            try
            {
                using var doc = JsonDocument.Parse(payloadJson);
                if (doc.RootElement.TryGetProperty("setId", out var idEl)
                    && idEl.ValueKind == JsonValueKind.String
                    && string.Equals(idEl.GetString(), setId, StringComparison.Ordinal))
                {
                    return JsonSerializer.Deserialize<ClarificationSet>(payloadJson, JsonOptions);
                }
            }
            catch (JsonException) { /* 跳过损坏 payload */ }
        }
        return null;
    }

    /// <summary>把一轮答案格式化为可读的 user message，存入对话历史。</summary>
    private async Task SaveClarificationAsUserMessageAsync(long pipelineId, string projectId, string content)
    {
        var pipelineIdStr = pipelineId.ToString();
        var tenantId = TenantResolver.Resolve().ToString();
        var maxSeq = await _db.Queryable<AiPipelineMessageEntity>()
            .Where(x => x.PipelineId == pipelineIdStr && x.TenantId == tenantId && x.Stage == PipelineStage.Requirement)
            .MaxAsync(x => (int?)x.Sequence) ?? 0;

        var msg = new AiPipelineMessageEntity
        {
            PipelineId = pipelineIdStr,
            ProjectId = projectId,
            Stage = PipelineStage.Requirement,
            Role = "user",
            Content = content,
            Sequence = maxSeq + 1,
            DeleteMark = 0,
        };
        msg.Creator();
        await _db.Insertable(msg).ExecuteCommandAsync();
    }

    private static string FormatAnswersAsUserMessage(ClarificationSet set, AnswerClarificationRequest request)
    {
        var sb = new StringBuilder();
        sb.Append("【需求澄清补充 — 第").Append(set.Round).Append("轮】\n");

        var optionLabelById = new Dictionary<string, (string qId, string label)>(StringComparer.Ordinal);
        foreach (var q in set.Questions)
            foreach (var o in q.Options)
                optionLabelById[$"{q.Id}:{o.Id}"] = (q.Id, o.Label);

        foreach (var ans in request.Answers ?? new())
        {
            var q = set.Questions.FirstOrDefault(x => x.Id == ans.QuestionId);
            if (q == null) continue;

            // 回传槽位 id，供 DetectFilledSlots / 下一轮选问
            if (!string.IsNullOrWhiteSpace(q.ContextHint) && q.ContextHint.Contains("[slot:", StringComparison.Ordinal))
            {
                var start = q.ContextHint.IndexOf("[slot:", StringComparison.Ordinal);
                var end = q.ContextHint.IndexOf(']', start);
                if (end > start)
                    sb.Append(q.ContextHint.AsSpan(start, end - start + 1)).Append(' ');
            }

            if (ans.MatrixRowAnswers is { Count: > 0 })
            {
                // 矩阵题格式化：逐行列出作答
                sb.Append("- ").Append(q.Text).Append("（逐行作答）：\n");
                foreach (var rowAns in ans.MatrixRowAnswers)
                {
                    sb.Append("    - ").Append(rowAns.RowLabel).Append("：");
                    var rowLabels = new List<string>();
                    if (!string.IsNullOrWhiteSpace(rowAns.SelectedOption))
                    {
                        var ids = rowAns.SelectedOption.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (var rid in ids)
                        {
                            if (optionLabelById.TryGetValue($"{q.Id}:{rid.Trim()}", out var meta))
                                rowLabels.Add(meta.label);
                        }
                    }
                    sb.Append(rowLabels.Count > 0 ? string.Join("、", rowLabels) : "（未选）");
                    if (!string.IsNullOrWhiteSpace(rowAns.FreeText))
                        sb.Append("（补充：").Append(rowAns.FreeText).Append("）");
                    sb.Append('\n');
                }
            }
            else
            {
                sb.Append("- ").Append(q.Text).Append("：");
                var labels = new List<string>();
                foreach (var oid in ans.OptionIds ?? new())
                {
                    if (optionLabelById.TryGetValue($"{q.Id}:{oid}", out var meta))
                        labels.Add(meta.label);
                }
                sb.Append(string.Join("、", labels));
                if (!string.IsNullOrWhiteSpace(ans.FreeText))
                    sb.Append("（补充：").Append(ans.FreeText).Append("）");
                sb.Append('\n');
            }
        }

        foreach (var skippedId in request.SkippedQuestionIds ?? new())
        {
            var q = set.Questions.FirstOrDefault(x => x.Id == skippedId);
            if (q != null)
                sb.Append("- ").Append(q.Text).Append("：（跳过，沿用默认假设）\n");
        }

        return sb.ToString();
    }

    /// <summary>R4：记录 Skill 产物评审结论（人/Guard 裁决）。</summary>
    [HttpPost("{pipelineId:long}/runs/{runId}/review")]
    public async Task<object> RecordSkillReviewAsync(
        long pipelineId, string runId, [FromBody] SkillReviewInput input)
    {
        if (string.IsNullOrWhiteSpace(input.SkillId))
            throw Oops.Bah("SkillId 不能为空");
        if (string.IsNullOrWhiteSpace(input.Verdict))
            throw Oops.Bah("Verdict 不能为空");

        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);
        await _experience.RecordReviewAsync(
            projectId, tenantId, input.SkillId, runId,
            input.Verdict, input.DetailJson ?? "{}", CancellationToken.None);

        return new
        {
            status = "recorded",
            eventType = IrEventTypes.SkillReviewRecorded,
            pipelineId,
            runId,
            skillId = input.SkillId,
            verdict = input.Verdict,
        };
    }

    /// <summary>R4：列出 pipeline 关联的三类经验事件。</summary>
    [HttpGet("{pipelineId:long}/experience-events")]
    public async Task<List<SkillExperienceEventDto>> ListExperienceEventsAsync(long pipelineId)
    {
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);
        var events = await _eventStore.ListEventsAsync(projectId, tenantId, pipelineId.ToString());
        var experienceTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            IrEventTypes.SkillReviewRecorded,
            IrEventTypes.SkillFailureRecorded,
            IrEventTypes.HumanCorrectionRecorded,
        };

        return events
            .Where(e => experienceTypes.Contains(e.EventType))
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new SkillExperienceEventDto
            {
                EventId = e.EventId,
                EventType = e.EventType,
                SkillId = e.SkillId,
                FragmentId = e.FragmentId,
                Payload = e.PayloadPreview ?? "{}",
                CreatedAt = e.CreatedAt,
            })
            .ToList();
    }

    /// <summary>G7：取消 pipeline 上所有后台 Skill 任务</summary>
    [HttpPost("{pipelineId:long}/cancel")]
    public async Task<object> CancelPipelineSkillsAsync(long pipelineId)
    {
        var (_, tenantId) = await ResolveProjectAsync(pipelineId);
        var needle = $":{pipelineId}:";
        var cancelled = new List<string>();

        foreach (var taskName in _taskRunner.GetAllActive().Keys.ToList())
        {
            if (!taskName.Contains(needle, StringComparison.Ordinal))
                continue;

            _taskRunner.CancelTask(taskName);
            cancelled.Add(taskName);
        }

        return new
        {
            pipelineId,
            tenantId,
            cancelledCount = cancelled.Count,
            tasks = cancelled,
        };
    }

    [HttpGet("{pipelineId:long}/runs")]
    public async Task<List<SkillRunDto>> ListRunsAsync(long pipelineId)
    {
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);
        var runs = await _db.Queryable<AiSkillRunEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId)
            .OrderByDescending(x => x.StartedAt)
            .Take(50)
            .ToListAsync();

        return runs.Select(r => new SkillRunDto
        {
            RunId = r.Id,
            SkillId = r.SkillId,
            Status = r.Status,
            StartedAt = r.StartedAt,
            CompletedAt = r.CompletedAt,
            TokenConsumed = r.TokenConsumed,
            ErrorMessage = r.ErrorMessage,
            Metadata = r.Metadata,
        }).ToList();
    }

    [HttpGet("seed/templates")]
    public async Task<object> ListSeedTemplates([FromQuery] string? keyword, [FromQuery] string? industry)
    {
        await _seedService.EnsureSeedDataAsync();
        var query = _db.Queryable<AiSeedTemplateEntity>().Where(x => !x.DeleteMark);
        if (!string.IsNullOrWhiteSpace(industry))
            query = query.Where(x => x.Industry == industry);

        var items = await query.Take(100).ToListAsync();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            items = items.Where(x =>
                x.EventNamePattern.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.TemplateId.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return new { total = items.Count, items };
    }

    private async Task<PmSpecReviewResult?> LoadLatestPmReviewAsync(
        string projectId, string tenantId, long pipelineId)
    {
        var evt = await _db.Queryable<AiIrEventEntity>()
            .Where(x => x.ProjectId == projectId
                        && x.TenantId == tenantId
                        && x.PipelineId == pipelineId.ToString()
                        && x.EventType == IrEventTypes.RequirementSpecPmReviewed)
            .OrderByDescending(x => x.Sequence)
            .FirstAsync();
        if (evt == null)
            return null;

        try
        {
            return PmSkillService.ParseSpecReviewResult(evt.Payload);
        }
        catch (JsonException)
        {
            return new PmSpecReviewResult
            {
                Score = 0,
                Verdict = "fail",
                Gaps = new List<string> { "PM 终评事件 payload 解析失败" },
                GapDetails = new List<PmSpecReviewGap>
                {
                    new() { Source = "llm", Message = "PM 终评事件 payload 解析失败" },
                },
            };
        }
    }

    private static string ResolveSkeletonPayloadJson(object? payload)
    {
        var raw = payload switch
        {
            JsonElement je when je.ValueKind == JsonValueKind.Object => je.GetRawText(),
            JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString() ?? "{}",
            string s => s,
            null => "{}",
            _ => JsonSerializer.Serialize(payload, JsonOptions),
        };
        if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
            raw = JsonSerializer.Deserialize<string>(raw) ?? "{}";
        return raw;
    }

    private async Task<bool> HasS2ConfirmedAsync(string projectId, string tenantId, long pipelineId)
    {
        var events = await _eventStore.ListEventsAsync(projectId, tenantId, pipelineId.ToString());
        foreach (var evt in events)
        {
            if (!string.Equals(evt.EventType, IrEventTypes.StageConfirmed, StringComparison.Ordinal))
                continue;
            try
            {
                using var doc = JsonDocument.Parse(evt.PayloadPreview);
                if (doc.RootElement.TryGetProperty("stage", out var stage)
                    && string.Equals(stage.GetString(), "S2", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch (JsonException)
            {
                continue;
            }
        }

        return false;
    }

    private async Task<object> RunSkillAsync(string skillId, long pipelineId, SkillRunRequest? request)
    {
        var runId = Guid.NewGuid().ToString("N");
        var taskName = $"skill:{pipelineId}:{skillId}:{runId}";
        var tenantSnapshot = RequestContext.Capture(_httpContextAccessor).TenantId;
        var (_, tenantId) = await ResolveProjectAsync(pipelineId, tenantSnapshot: tenantSnapshot);

        if (_runGuard.IsRunning(tenantId, pipelineId, skillId))
            throw Oops.Oh($"Skill {skillId} 已在运行中")
                .StatusCode(StatusCodes.Status409Conflict);

        if (!_quotaGuard.TryAcquire(tenantId, pipelineId, out var rejectReason, out var activePipelineIds))
            throw Oops.Oh(rejectReason ?? "租户 pipeline 配额已满")
                .StatusCode(StatusCodes.Status429TooManyRequests)
                .WithData(new { code = "TENANT_PIPELINE_QUOTA_EXCEEDED", activePipelineIds });

        // analyst-skill 内部轮询 SA 最多 30 分钟，BackgroundTask 留 5 分钟缓冲
        var skillTimeout = skillId == "analyst-skill"
            ? TimeSpan.FromMinutes(35)
            : TimeSpan.FromMinutes(15);

        _taskRunner.Run(taskName, async (ctx, ct) =>
        {
            try
            {
                var triple = await _tripleResolver.ResolveAsync(pipelineId, ctx, tenantSnapshot, ct);
                var options = new SkillRunOptions
                {
                    UserRequirement = request?.UserRequirement,
                    ProviderCode = request?.ProviderCode,
                };
                await _harness.RunAsync(skillId, pipelineId, triple.TenantId, triple.ProjectId, options, ct);
            }
            finally
            {
                _quotaGuard.Release(tenantId, pipelineId);
            }
        }, timeout: skillTimeout);

        return new
        {
            runId,
            skillId,
            pipelineId,
            status = "running",
            message = "Skill 已在后台启动",
        };
    }

    private async Task<(string ProjectId, string TenantId)> ResolveProjectAsync(
        long pipelineId,
        RequestContext? bgCtx = null,
        string? tenantSnapshot = null)
    {
        var triple = await _tripleResolver.ResolveAsync(pipelineId, bgCtx, tenantSnapshot);
        return (triple.ProjectId, triple.TenantId);
    }
}
