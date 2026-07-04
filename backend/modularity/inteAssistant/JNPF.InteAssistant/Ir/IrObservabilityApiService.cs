using System.Text.Json;
using JNPF.Common.Core.MultiTenancy;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Dto.Skills;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Infrastructure.Background;
using JNPF.InteAssistant.Infrastructure.Security;
using JNPF.InteAssistant.Skills;
using JNPF.InteAssistant.Constraints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SqlSugar;

namespace JNPF.InteAssistant.Ir;

/// <summary>
/// IR 观测台 REST API（阶段一 P1-B03）
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "IrObservability", Order = 194)]
[Route("api/studio/ir")]
public class IrObservabilityApiService : IDynamicApiController, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly ISqlSugarClient _db;
    private readonly IIrEventStoreService _eventStore;
    private readonly IIrProjectionEngine _projection;
    private readonly ITenantGuard _tenantGuard;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _configuration;
    private readonly IEventSpecRevisionService _revisionService;
    private readonly IBackgroundTaskRunner _taskRunner;
    private readonly IAnalystAffectedStepsRerunService _rerunService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConstraintEngineService _constraintEngine;
    private readonly IIrDiffEngine _diffEngine;

    public IrObservabilityApiService(
        ISqlSugarClient db,
        IIrEventStoreService eventStore,
        IIrProjectionEngine projection,
        ITenantGuard tenantGuard,
        IWebHostEnvironment env,
        IConfiguration configuration,
        IEventSpecRevisionService revisionService,
        IBackgroundTaskRunner taskRunner,
        IAnalystAffectedStepsRerunService rerunService,
        IHttpContextAccessor httpContextAccessor,
        IConstraintEngineService constraintEngine,
        IIrDiffEngine diffEngine)
    {
        _db = db;
        _eventStore = eventStore;
        _projection = projection;
        _tenantGuard = tenantGuard;
        _env = env;
        _configuration = configuration;
        _revisionService = revisionService;
        _taskRunner = taskRunner;
        _rerunService = rerunService;
        _httpContextAccessor = httpContextAccessor;
        _constraintEngine = constraintEngine;
        _diffEngine = diffEngine;
    }

    /// <summary>阶段五 P5-B01 — 两序列点 IR 快照 diff。</summary>
    [HttpGet("{pipelineId:long}/diff")]
    public async Task<IrDiffResult> GetDiffAsync(
        long pipelineId, [FromQuery] int from, [FromQuery] int to, [FromQuery] bool forceUnlock = false)
    {
        if (from >= to)
            throw Oops.Bah("from 须小于 to");

        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);
        return await _diffEngine.CompareAsync(
            projectId,
            tenantId,
            from,
            to,
            new IrDiffOptions { ForceUnlock = forceUnlock, PropagateDownstream = true });
    }

    [HttpGet("{pipelineId:long}/events")]
    public async Task<List<IrEventDto>> GetEventsAsync(long pipelineId)
    {
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);
        return await _eventStore.ListEventsAsync(projectId, tenantId);
    }

    [HttpGet("{pipelineId:long}/snapshots")]
    public async Task<List<IrFragmentSnapshotDto>> GetSnapshotsAsync(long pipelineId)
    {
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);
        return await _eventStore.ListSnapshotsAsync(projectId, tenantId);
    }

    [HttpGet("{pipelineId:long}/snapshots/{fragmentId}")]
    public async Task<IrFragmentSnapshotDto?> GetSnapshotAtVersionAsync(
        long pipelineId, string fragmentId, [FromQuery] int? version)
    {
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);
        return await _eventStore.GetSnapshotAtVersionAsync(projectId, tenantId, fragmentId, version);
    }

    [HttpGet("{pipelineId:long}/stability")]
    public async Task<IrStabilityDto?> GetStabilityAsync(long pipelineId)
    {
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);
        return await _eventStore.GetStabilityAsync(projectId, tenantId);
    }

    [HttpGet("{pipelineId:long}/diagnostics")]
    public async Task<IrDiagnosticsDto> GetDiagnosticsAsync(long pipelineId)
    {
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);
        var project = await _db.Queryable<AiProjectEntity>()
            .FirstAsync(x => x.Id == projectId && x.TenantId == tenantId && !x.DeleteMark);

        var routes = await _db.Queryable<AiRouteTableEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId)
            .ToListAsync();

        var eventCount = await _db.Queryable<AiIrEventEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId)
            .CountAsync();

        var snapshotCount = await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId && !x.DeleteMark)
            .CountAsync();

        return new IrDiagnosticsDto
        {
            PipelineId = pipelineId,
            ProjectId = projectId,
            TenantId = tenantId,
            WorkspacePath = StudioWorkspaceHelper.GetPipelinePath(tenantId, projectId),
            RouteTable = routes.Select(r => new IrRouteEntryDto
            {
                Path = r.EtcdKey,
                Target = r.SandboxEndpoint ?? r.SandboxId,
            }).ToArray(),
            EventCount = eventCount,
            SnapshotCount = snapshotCount,
        };
    }

    [HttpPost("{pipelineId:long}/rebuild")]
    public async Task<IrRebuildResultDto> RebuildAsync(long pipelineId)
    {
        EnsureDevToolsEnabled();
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);
        return await _projection.RebuildAsync(tenantId, projectId);
    }

    [HttpPost("{pipelineId:long}/events/{fragmentId}/revise")]
    public async Task<ReviseEventSpecResult> ReviseEventSpecAsync(
        long pipelineId, string fragmentId, [FromBody] ReviseEventSpecInput input)
    {
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);
        var result = await _revisionService.ReviseAsync(projectId, tenantId, fragmentId, input);

        if (input.AutoRerunAffected == true && result.AffectedSteps.Count > 0)
        {
            var eventId = fragmentId.StartsWith("eventspec:", StringComparison.OrdinalIgnoreCase)
                ? fragmentId["eventspec:".Length..]
                : fragmentId;
            var tenantSnapshot = RequestContext.Capture(_httpContextAccessor).TenantId;
            var taskName = $"revise-rerun:{pipelineId}:{fragmentId}";

            _taskRunner.Run(taskName, async (ctx, ct) =>
            {
                var effectiveTenant = !string.IsNullOrWhiteSpace(ctx.TenantId) && ctx.TenantId != "-1"
                    ? ctx.TenantId
                    : tenantSnapshot ?? tenantId;
                await _rerunService.RunAsync(
                    effectiveTenant, projectId, pipelineId, eventId,
                    new RerunAffectedStepsInput { Steps = result.AffectedSteps.ToList() }, ct);
            }, timeout: TimeSpan.FromMinutes(10));
        }

        return result;
    }

    [HttpPost("{pipelineId:long}/simulate")]
    public async Task<IrEventDto> SimulateAsync(long pipelineId, [FromBody] SimulateIrEventInput input)
    {
        EnsureDevToolsEnabled();

        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);
        var saStepName = input.SaStepName;
        if (input.EventType == IrEventTypes.SaStepCompleted && string.IsNullOrWhiteSpace(saStepName))
        {
            saStepName = await InferNextSaStepAsync(projectId, tenantId);
        }

        var request = BuildSimulateRequest(input with { SaStepName = saStepName });
        var evt = await _eventStore.AppendAsync(projectId, tenantId, request);

        return new IrEventDto
        {
            EventId = evt.Id,
            EventType = evt.EventType,
            FragmentId = evt.FragmentId,
            FragmentType = evt.FragmentType,
            FragmentVersion = evt.FragmentVersion,
            SaStepName = evt.SaStepName,
            CreatedAt = evt.CreatedAt,
            PayloadPreview = evt.Payload.Length > 500 ? evt.Payload[..500] + "…" : evt.Payload,
        };
    }

    [HttpPost("{pipelineId:long}/events/{fragmentId}/ack-inferred-rules")]
    public async Task<object> AckInferredRulesAsync(long pipelineId, string fragmentId)
    {
        EnsureDevToolsEnabled();
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);

        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.InferredRulesAcknowledged,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.EventSpec,
            Payload = JsonSerializer.Serialize(new
            {
                fragmentId,
                acknowledgedBy = "dev-hitl",
                acknowledgedAt = DateTime.UtcNow,
            }, JsonOptions),
            SkillId = "dev-tools",
        });

        return new { status = "acknowledged", fragmentId };
    }

    /// <summary>ConstraintEngine 手动校验（Dev / 设计 Skill 后）</summary>
    [HttpPost("{pipelineId:long}/constraints/check")]
    public async Task<ConstraintCheckResultDto> CheckConstraintsAsync(
        long pipelineId, [FromBody] ConstraintCheckInput? input, CancellationToken ct)
    {
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);
        var result = await _constraintEngine.CheckProjectAsync(
            projectId,
            tenantId,
            input?.Persist ?? true,
            "constraint-engine",
            ct);

        return new ConstraintCheckResultDto
        {
            Violations = result.Violations.Select(v => new ConstraintViolationDto
            {
                RuleId = v.RuleId,
                Severity = v.Severity,
                Message = v.Message,
                FragmentType = v.FragmentType,
                FragmentId = v.FragmentId,
            }).ToList(),
            CriticalCount = result.CriticalCount,
            WarningCount = result.WarningCount,
            Passed = result.Passed,
            EventAppended = result.EventAppended,
        };
    }

    private async Task<(string ProjectId, string TenantId)> ResolveProjectAsync(long pipelineId)
    {
        var tenantId = TenantResolver.Resolve().ToString();
        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .FirstAsync(x => x.Id == pipelineId.ToString());

        if (pipeline == null)
            throw Oops.Oh("流水线不存在");

        if (!_tenantGuard.VerifyOwnership(pipeline, tenantId)
            && !TenantResolver.IsSuperTenant())
            throw Oops.Oh("无权访问该流水线");

        var pipelineTenantId = pipeline.TenantId ?? tenantId;
        if (!string.Equals(pipelineTenantId, tenantId, StringComparison.Ordinal)
            && !TenantResolver.IsSuperTenant())
            throw Oops.Oh("跨租户访问被拒绝");

        return (pipelineId.ToString(), pipelineTenantId);
    }

    private void EnsureDevToolsEnabled()
    {
        var featureFlag = _configuration.GetValue<bool>("Features:IrDevTools");
        if (!_env.IsDevelopment() && !featureFlag)
            throw Oops.Oh("Simulate API 仅在开发环境可用", 404);
    }

    private static AppendIrEventRequest BuildSimulateRequest(SimulateIrEventInput input)
    {
        var eventType = input.EventType;
        var saStepName = input.SaStepName;
        var useInvalidPayload = input.UseInvalidPayload;
        var fragmentIdOverride = input.FragmentId;
        var withInferredRules = input.WithInferredRules;
        var withAutoSeedEvent = input.WithAutoSeedEvent;
        var injectLayerViolation = input.InjectLayerViolation;

        const string defaultSkeletonFragment = "skeleton:SK-001";
        var fragmentId = fragmentIdOverride ?? defaultSkeletonFragment;
        var skeletonPayload = JsonSerializer.Serialize(new
        {
            context = "https://schema.jnpf.ai/ir/v1",
            id = defaultSkeletonFragment,
            skeletonId = "SK-001",
            version = 1,
            businessEvents = withAutoSeedEvent
                ? new object[]
                {
                    new { eventId = "BE-AUTO", eventName = "LeaveRequestSubmitted", complexityHint = "auto", dependsOn = Array.Empty<string>() },
                }
                : new object[]
                {
                    new { eventId = "BE-001", eventName = "LeaveRequestSubmitted", complexityHint = "simple", dependsOn = Array.Empty<string>() },
                },
            roleMatrix = new[] { new { roleName = "Employee", responsibilities = new[] { "提交请假" } } },
            entityDrafts = new[] { new { entityName = "LeaveRequest", tableName = "OA_LEAVE_REQUEST", fields = Array.Empty<object>() } },
        }, JsonOptions);

        var eventSpecFragment = fragmentId.StartsWith("eventspec:", StringComparison.Ordinal)
            ? fragmentId
            : "eventspec:BE-001";
        var eventSpecPayload = JsonSerializer.Serialize(new
        {
            eventId = eventSpecFragment.Split(':')[1],
            eventName = "LeaveRequestSubmitted",
            version = 1,
            confirmedFields = useInvalidPayload
                ? Array.Empty<object>()
                : new[] { new { name = "id", type = "string", required = true } },
            businessRules = withInferredRules
                ? new[] { new { ruleId = "R-inf", description = "推断规则", source = "inferred" } }
                : new[] { new { ruleId = "R1", description = "test", source = "seed-data" } },
            ioiInvariants = useInvalidPayload
                ? new[] { new { name = "bad", expression = "INVALID_EXPR" } }
                : Array.Empty<object>(),
            saStepsCompleted = IrSaSteps.All,
        }, JsonOptions);

        var invalidPayload = JsonSerializer.Serialize(new { skeletonId = "SK-001", version = 1 }, JsonOptions);

        return eventType switch
        {
            IrEventTypes.SkeletonCreated => new AppendIrEventRequest
            {
                EventType = IrEventTypes.SkeletonCreated,
                FragmentId = defaultSkeletonFragment,
                FragmentType = IrFragmentTypes.Skeleton,
                FragmentVersion = 1,
                Payload = useInvalidPayload ? invalidPayload : skeletonPayload,
                SkillId = "pm-skill",
            },
            IrEventTypes.SaStepCompleted => new AppendIrEventRequest
            {
                EventType = IrEventTypes.SaStepCompleted,
                FragmentId = fragmentId,
                FragmentType = fragmentId.StartsWith("eventspec:", StringComparison.Ordinal)
                    ? IrFragmentTypes.EventSpec
                    : IrFragmentTypes.Skeleton,
                FragmentVersion = 1,
                Payload = JsonSerializer.Serialize(new { source = "simulate" }, JsonOptions),
                SkillId = "sa-skill",
                SaStepName = saStepName ?? IrSaSteps.All[0],
            },
            IrEventTypes.EventSpecConfirmed => new AppendIrEventRequest
            {
                EventType = IrEventTypes.EventSpecConfirmed,
                FragmentId = eventSpecFragment,
                FragmentType = IrFragmentTypes.EventSpec,
                FragmentVersion = 1,
                Payload = eventSpecPayload,
                SkillId = "analyst-skill",
            },
            IrEventTypes.EventSpecRevised => new AppendIrEventRequest
            {
                EventType = IrEventTypes.EventSpecRevised,
                FragmentId = defaultSkeletonFragment,
                FragmentType = IrFragmentTypes.Skeleton,
                FragmentVersion = 2,
                Payload = skeletonPayload,
                SkillId = "sa-skill",
            },
            IrEventTypes.ArchitectureDecisionRecorded => new AppendIrEventRequest
            {
                EventType = IrEventTypes.ArchitectureDecisionRecorded,
                FragmentId = fragmentIdOverride ?? "architecture:phase3-sim",
                FragmentType = IrFragmentTypes.Architecture,
                FragmentVersion = 1,
                Payload = JsonSerializer.Serialize(new
                {
                    context = "https://schema.jnpf.ai/ir/v1",
                    id = fragmentIdOverride ?? "architecture:phase3-sim",
                    pattern = "layered",
                    modules = new[] { new { name = "LeaveModule", layer = "application" } },
                    candidates = new[]
                    {
                        new { name = "layered-mvp", score = 0.9 },
                        new { name = "cqrs-lite", score = 0.7 },
                    },
                    selectedIndex = 0,
                    stabilityState = IrStabilityStates.Stable,
                }, JsonOptions),
                SkillId = DesignSkillIds.Architect,
            },
            IrEventTypes.DDLStabilized => BuildSimulatedDdlEvent(fragmentIdOverride, injectLayerViolation),
            IrEventTypes.UIDesignStabilized => new AppendIrEventRequest
            {
                EventType = IrEventTypes.UIDesignStabilized,
                FragmentId = fragmentIdOverride ?? "formPage:phase3-sim",
                FragmentType = IrFragmentTypes.FormPageIR,
                FragmentVersion = 1,
                Payload = JsonSerializer.Serialize(new
                {
                    context = "https://schema.jnpf.ai/ir/v1",
                    id = fragmentIdOverride ?? "formPage:phase3-sim",
                    pageName = "LeaveRequestForm",
                    fields = new[]
                    {
                        new { fieldId = "id", label = "编号", component = "Input" },
                        new { fieldId = "reason", label = "请假事由", component = "Textarea" },
                        new { fieldId = "days", label = "请假天数", component = "InputNumber" },
                        new { fieldId = "status", label = "状态", component = "Select" },
                    },
                    stabilityState = IrStabilityStates.Stable,
                }, JsonOptions),
                SkillId = DesignSkillIds.UiDesign,
            },
            _ => throw Oops.Bah($"不支持的模拟事件类型: {eventType}"),
        };
    }

    private static AppendIrEventRequest BuildSimulatedDdlEvent(string? fragmentIdOverride, bool injectLayerViolation)
    {
        var fragmentId = fragmentIdOverride ?? "ddl:phase3-sim";
        var ddl = injectLayerViolation
            ? "CREATE TABLE [dbo].[T] ([F_Id] NVARCHAR(50)); ALTER TABLE T ADD CONSTRAINT FK1 FOREIGN KEY (X) REFERENCES [dbo].[UserController];"
            : "CREATE TABLE [dbo].[OA_LEAVE_REQUEST] (\n  [F_Id] NVARCHAR(50) NOT NULL PRIMARY KEY,\n  [F_TenantId] NVARCHAR(50) NOT NULL,\n  [F_Reason] NVARCHAR(500) NULL,\n  [F_Days] INT NOT NULL DEFAULT 1,\n  [F_Status] NVARCHAR(20) NOT NULL DEFAULT N'draft'\n);";

        return new AppendIrEventRequest
        {
            EventType = IrEventTypes.DDLStabilized,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.DDL,
            FragmentVersion = 1,
            Payload = JsonSerializer.Serialize(new
            {
                context = "https://schema.jnpf.ai/ir/v1",
                id = fragmentId,
                dialect = "sqlserver",
                ddl,
                tableNames = new[] { injectLayerViolation ? "T" : "OA_LEAVE_REQUEST" },
                stabilityState = IrStabilityStates.Stable,
            }, JsonOptions),
            SkillId = DesignSkillIds.DbDesign,
        };
    }

    private async Task<string> InferNextSaStepAsync(string projectId, string tenantId)
    {
        var stability = await _eventStore.GetStabilityAsync(projectId, tenantId);
        var completed = stability?.SaStepsCompleted?.ToHashSet(StringComparer.Ordinal)
            ?? new HashSet<string>(StringComparer.Ordinal);

        foreach (var step in IrSaSteps.All)
        {
            if (!completed.Contains(step))
                return step;
        }

        return IrSaSteps.All[^1];
    }
}
