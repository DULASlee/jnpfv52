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
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Runtime;
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
        ISkillRunGuard runGuard)
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
    }

    [HttpPost("pm/{pipelineId:long}/run")]
    public Task<object> RunPmAsync(long pipelineId, [FromBody] SkillRunRequest? request)
        => RunSkillAsync("pm-skill", pipelineId, request);

    [HttpPost("analyst/{pipelineId:long}/run")]
    public Task<object> RunAnalystAsync(long pipelineId, [FromBody] SkillRunRequest? request)
        => RunSkillAsync("analyst-skill", pipelineId, request);

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
        var snapshots = await _eventStore.ListSnapshotsAsync(projectId, tenantId);
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
            await RunSkillAsync("analyst-skill", pipelineId, null);

        return new { status = "confirmed", fragmentId = skeleton.FragmentId, autoRunAnalyst = request?.AutoRunAnalyst == true };
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

        _taskRunner.Run(taskName, async (ctx, ct) =>
        {
            try
            {
                var projectId = pipelineId.ToString();
                var options = new SkillRunOptions
                {
                    UserRequirement = request?.UserRequirement,
                    ProviderCode = request?.ProviderCode,
                };
                await _harness.RunAsync(skillId, pipelineId, tenantId, projectId, options, ct);
            }
            finally
            {
                _quotaGuard.Release(tenantId, pipelineId);
            }
        }, timeout: TimeSpan.FromMinutes(15));

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
        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .FirstAsync(x => x.Id == pipelineId.ToString());

        if (pipeline == null)
            throw Oops.Oh("流水线不存在");

        var tenantId = ResolveEffectiveTenantId(bgCtx, tenantSnapshot, pipeline.TenantId);

        if (!_tenantGuard.VerifyOwnership(pipeline, tenantId) && !IsSuperTenantFromContext(bgCtx, tenantSnapshot))
            throw Oops.Oh("无权访问该流水线");

        var pipelineTenantId = pipeline.TenantId ?? tenantId;
        if (!string.Equals(pipelineTenantId, tenantId, StringComparison.Ordinal)
            && !IsSuperTenantFromContext(bgCtx, tenantSnapshot))
            throw Oops.Oh("跨租户访问被拒绝");

        return (pipelineId.ToString(), pipelineTenantId);
    }

    private static string ResolveEffectiveTenantId(RequestContext? bgCtx, string? tenantSnapshot, string? pipelineTenantId)
    {
        foreach (var candidate in new[] { bgCtx?.TenantId, tenantSnapshot, pipelineTenantId })
        {
            if (!string.IsNullOrWhiteSpace(candidate) && candidate != "-1")
                return candidate;
        }

        var resolved = TenantResolver.Resolve();
        return resolved >= 0 ? resolved.ToString() : (pipelineTenantId ?? "-1");
    }

    private static bool IsSuperTenantFromContext(RequestContext? bgCtx, string? tenantSnapshot = null)
    {
        foreach (var raw in new[] { bgCtx?.TenantId, tenantSnapshot })
        {
            if (!string.IsNullOrWhiteSpace(raw) && long.TryParse(raw, out var tid)
                && tid == TenantResolver.PlatformTenantId)
                return true;
        }

        return TenantResolver.IsSuperTenant();
    }
}
