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
/// 部署 Skill REST API（阶段五 P5-B03）
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "StudioDeploySkills", Order = 196)]
[Route("api/studio/skills")]
public class DeploySkillsApiService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ISkillHarness _harness;
    private readonly IBackgroundTaskRunner _taskRunner;
    private readonly ITenantGuard _tenantGuard;
    private readonly ITenantPipelineQuotaGuard _quotaGuard;
    private readonly ISkillRunGuard _runGuard;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeploySkillsApiService(
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

    [HttpPost("deploy/{pipelineId:long}/run")]
    public Task<object> RunDeployAsync(long pipelineId, [FromBody] SkillRunRequest? request)
        => RunSkillAsync(pipelineId, request);

    private async Task<object> RunSkillAsync(long pipelineId, SkillRunRequest? request)
    {
        var runId = Guid.NewGuid().ToString("N");
        var taskName = $"deploy-skill:{pipelineId}:{runId}";
        var tenantSnapshot = RequestContext.Capture(_httpContextAccessor).TenantId;
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId, tenantSnapshot);

        if (_runGuard.IsRunning(tenantId, pipelineId, DeploySkillIds.Deploy))
            throw Oops.Oh($"Skill {DeploySkillIds.Deploy} 已在运行中")
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
                    DeploySkillIds.Deploy,
                    pipelineId,
                    tenantId,
                    projectId,
                    new SkillRunOptions { ProviderCode = request?.ProviderCode },
                    ct);
            }
            finally
            {
                _quotaGuard.Release(tenantId, pipelineId);
            }
        }, timeout: TimeSpan.FromMinutes(25));

        return new
        {
            runId,
            skillId = DeploySkillIds.Deploy,
            pipelineId,
            status = "running",
            message = "deploy-skill 已启动",
        };
    }

    private async Task<(string projectId, string tenantId)> ResolveProjectAsync(
        long pipelineId, string? tenantSnapshot = null)
    {
        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .FirstAsync(x => x.Id == pipelineId.ToString());
        if (pipeline == null)
            throw Oops.Oh("流水线不存在");

        var tenantId = ResolveEffectiveTenantId(tenantSnapshot, pipeline.TenantId);
        if (!_tenantGuard.VerifyOwnership(pipeline, tenantId) && !TenantResolver.IsSuperTenant())
            throw Oops.Oh("无权访问该流水线");

        // 三元组：projectId 解析与 PipelineTripleResolver 一致（pipeline.ProjectId 为空时回退到 pipelineId）
        var projectId = string.IsNullOrWhiteSpace(pipeline.ProjectId)
            ? pipelineId.ToString()
            : pipeline.ProjectId;
        return (projectId, pipeline.TenantId ?? tenantId);
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
