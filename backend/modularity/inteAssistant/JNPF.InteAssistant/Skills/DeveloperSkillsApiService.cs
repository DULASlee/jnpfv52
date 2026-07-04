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
/// 开发 Skill REST API（阶段四 P4-B01a/b）
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "StudioDeveloperSkills", Order = 194)]
[Route("api/studio/skills")]
public class DeveloperSkillsApiService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly IDeveloperSkillOrchestrator _orchestrator;
    private readonly IBackgroundTaskRunner _taskRunner;
    private readonly ITenantGuard _tenantGuard;
    private readonly ITenantPipelineQuotaGuard _quotaGuard;
    private readonly ISkillRunGuard _runGuard;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeveloperSkillsApiService(
        ISqlSugarClient db,
        IDeveloperSkillOrchestrator orchestrator,
        IBackgroundTaskRunner taskRunner,
        ITenantGuard tenantGuard,
        ITenantPipelineQuotaGuard quotaGuard,
        ISkillRunGuard runGuard,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _orchestrator = orchestrator;
        _taskRunner = taskRunner;
        _tenantGuard = tenantGuard;
        _quotaGuard = quotaGuard;
        _runGuard = runGuard;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost("developer/{pipelineId:long}/run")]
    public Task<object> RunDeveloperAsync(long pipelineId, [FromBody] SkillRunRequest? request)
        => RunOrchestratorAsync(pipelineId, request);

    [HttpGet("developer/{pipelineId:long}/status")]
    public async Task<DeveloperOrchestratorStatus> GetDeveloperStatusAsync(long pipelineId)
    {
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);
        return await _orchestrator.GetStatusAsync(pipelineId, tenantId, projectId, CancellationToken.None);
    }

    private async Task<object> RunOrchestratorAsync(long pipelineId, SkillRunRequest? request)
    {
        var runId = Guid.NewGuid().ToString("N");
        var taskName = $"developer-orchestrator:{pipelineId}:{runId}";
        var tenantSnapshot = RequestContext.Capture(_httpContextAccessor).TenantId;
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId, tenantSnapshot: tenantSnapshot);

        if (_runGuard.IsRunning(tenantId, pipelineId, DevelopmentSkillIds.Developer))
            throw Oops.Oh($"Skill {DevelopmentSkillIds.Developer} 已在运行中")
                .StatusCode(StatusCodes.Status409Conflict);

        if (!_quotaGuard.TryAcquire(tenantId, pipelineId, out var rejectReason, out var activePipelineIds))
            throw Oops.Oh(rejectReason ?? "租户 pipeline 配额已满")
                .StatusCode(StatusCodes.Status429TooManyRequests)
                .WithData(new { code = "TENANT_PIPELINE_QUOTA_EXCEEDED", activePipelineIds });

        _taskRunner.Run(taskName, async (ctx, ct) =>
        {
            try
            {
                await _orchestrator.RunAsync(
                    pipelineId,
                    tenantId,
                    projectId,
                    new DeveloperOrchestratorOptions { ProviderCode = request?.ProviderCode },
                    ct);
            }
            finally
            {
                _quotaGuard.Release(tenantId, pipelineId);
            }
        }, timeout: TimeSpan.FromMinutes(30));

        return new
        {
            runId,
            skillId = DevelopmentSkillIds.Developer,
            pipelineId,
            status = "running",
            message = "Developer 编排（codegen + sandbox build + arch-guard）已启动",
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
