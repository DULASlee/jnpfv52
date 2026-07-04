using JNPF.Common.Core.MultiTenancy;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.Skills;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Infrastructure.Background;
using JNPF.InteAssistant.Infrastructure.Security;
using JNPF.InteAssistant.Runtime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 阶段五 P5-B02 — bugfix-skill REST API。
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "StudioBugfixSkills", Order = 195)]
[Route("api/studio/skills")]
public class BugfixSkillsApiService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ISkillHarness _harness;
    private readonly IBackgroundTaskRunner _taskRunner;
    private readonly ITenantGuard _tenantGuard;
    private readonly ITenantPipelineQuotaGuard _quotaGuard;
    private readonly ISkillRunGuard _runGuard;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BugfixSkillsApiService(
        ISqlSugarClient db,
        ISkillHarness harness,
        IBackgroundTaskRunner taskRunner,
        ITenantGuard tenantGuard,
        ITenantPipelineQuotaGuard quotaGuard,
        ISkillRunGuard runGuard,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _harness = harness;
        _taskRunner = taskRunner;
        _tenantGuard = tenantGuard;
        _quotaGuard = quotaGuard;
        _runGuard = runGuard;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost("bugfix/{pipelineId:long}/run")]
    public async Task<object> RunBugfixAsync(long pipelineId, [FromBody] BugfixRunRequest request)
    {
        if (request.FromSequence >= request.ToSequence)
            throw Oops.Bah("fromSequence 须小于 toSequence");

        var runId = Guid.NewGuid().ToString("N");
        var taskName = $"bugfix-skill:{pipelineId}:{runId}";
        var tenantSnapshot = RequestContext.Capture(_httpContextAccessor).TenantId;
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId, tenantSnapshot);

        if (_runGuard.IsRunning(tenantId, pipelineId, BugfixSkillIds.Bugfix))
            throw Oops.Oh($"Skill {BugfixSkillIds.Bugfix} 已在运行中")
                .StatusCode(StatusCodes.Status409Conflict);

        if (!_quotaGuard.TryAcquire(tenantId, pipelineId, out var rejectReason, out var activePipelineIds))
            throw Oops.Oh(rejectReason ?? "租户 pipeline 配额已满")
                .StatusCode(StatusCodes.Status429TooManyRequests)
                .WithData(new { code = "TENANT_PIPELINE_QUOTA_EXCEEDED", activePipelineIds });

        _taskRunner.Run(taskName, async (ctx, ct) =>
        {
            try
            {
                await _harness.RunAsync(
                    BugfixSkillIds.Bugfix,
                    pipelineId,
                    tenantId,
                    projectId,
                    new SkillRunOptions
                    {
                        ProviderCode = request.ProviderCode,
                        Bugfix = new BugfixRunContext
                        {
                            FromSequence = request.FromSequence,
                            ToSequence = request.ToSequence,
                            RootCauseLayer = request.RootCauseLayer,
                            RevisionType = request.RevisionType,
                            Description = request.Description,
                            ForceUnlock = request.ForceUnlock,
                        },
                    },
                    ct);
            }
            finally
            {
                _quotaGuard.Release(tenantId, pipelineId);
            }
        }, timeout: TimeSpan.FromMinutes(15));

        return new
        {
            runId,
            skillId = BugfixSkillIds.Bugfix,
            pipelineId,
            status = "running",
            message = "Bugfix skill（diff + AffectedFragmentsMarked）已启动",
            fromSequence = request.FromSequence,
            toSequence = request.ToSequence,
        };
    }

    private async Task<(string ProjectId, string TenantId)> ResolveProjectAsync(
        long pipelineId, string? tenantSnapshot = null)
    {
        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .FirstAsync(x => x.Id == pipelineId.ToString());

        if (pipeline == null)
            throw Oops.Oh("流水线不存在");

        var tenantId = ResolveEffectiveTenantId(tenantSnapshot, pipeline.TenantId);
        if (!_tenantGuard.VerifyOwnership(pipeline, tenantId) && !TenantResolver.IsSuperTenant())
            throw Oops.Oh("无权访问该流水线");

        return (pipelineId.ToString(), pipeline.TenantId ?? tenantId);
    }

    private static string ResolveEffectiveTenantId(string? tenantSnapshot, string? pipelineTenantId)
    {
        foreach (var candidate in new[] { tenantSnapshot, pipelineTenantId })
        {
            if (!string.IsNullOrWhiteSpace(candidate) && candidate != "-1")
                return candidate;
        }

        var resolved = TenantResolver.Resolve();
        return resolved >= 0 ? resolved.ToString() : (pipelineTenantId ?? "-1");
    }
}
