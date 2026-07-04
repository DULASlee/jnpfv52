using JNPF.Common.Core.MultiTenancy;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.Skills;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Infrastructure.Background;
using JNPF.InteAssistant.Infrastructure.Background;
using JNPF.InteAssistant.Infrastructure.Security;
using JNPF.InteAssistant.Llm;
using JNPF.InteAssistant.Runtime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 设计 Skill REST API（阶段三 P3-B01）
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "StudioDesignSkills", Order = 193)]
[Route("api/studio/skills")]
public class DesignSkillsApiService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly IDesignSkillOrchestrator _orchestrator;
    private readonly ISkillHarness _harness;
    private readonly IBackgroundTaskRunner _taskRunner;
    private readonly ITenantGuard _tenantGuard;
    private readonly ITenantPipelineQuotaGuard _quotaGuard;
    private readonly ISkillRunGuard _runGuard;
    private readonly ISkillLlmBudgetGuard _budgetGuard;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DesignSkillsApiService(
        ISqlSugarClient db,
        IDesignSkillOrchestrator orchestrator,
        ISkillHarness harness,
        IBackgroundTaskRunner taskRunner,
        ITenantGuard tenantGuard,
        ITenantPipelineQuotaGuard quotaGuard,
        ISkillRunGuard runGuard,
        ISkillLlmBudgetGuard budgetGuard,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _orchestrator = orchestrator;
        _harness = harness;
        _taskRunner = taskRunner;
        _tenantGuard = tenantGuard;
        _quotaGuard = quotaGuard;
        _runGuard = runGuard;
        _budgetGuard = budgetGuard;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost("design/{pipelineId:long}/run")]
    public Task<object> RunDesignAsync(long pipelineId, [FromBody] SkillRunRequest? request)
        => RunOrchestratorAsync(pipelineId, request);

    [HttpGet("design/{pipelineId:long}/status")]
    public async Task<DesignOrchestratorStatus> GetDesignStatusAsync(long pipelineId)
    {
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId);
        return await _orchestrator.GetStatusAsync(pipelineId, tenantId, projectId, CancellationToken.None);
    }

    [HttpPost("architect/{pipelineId:long}/run")]
    public Task<object> RunArchitectAsync(long pipelineId, [FromBody] SkillRunRequest? request)
        => RunSingleSkillAsync(DesignSkillIds.Architect, pipelineId, request);

    [HttpPost("db-design/{pipelineId:long}/run")]
    public Task<object> RunDbDesignAsync(long pipelineId, [FromBody] SkillRunRequest? request)
        => RunSingleSkillAsync(DesignSkillIds.DbDesign, pipelineId, request);

    [HttpPost("ui-design/{pipelineId:long}/run")]
    public Task<object> RunUiDesignAsync(long pipelineId, [FromBody] SkillRunRequest? request)
        => RunSingleSkillAsync(DesignSkillIds.UiDesign, pipelineId, request);

    [HttpPost("system-design/{pipelineId:long}/run")]
    public Task<object> RunSystemDesignAsync(long pipelineId, [FromBody] SkillRunRequest? request)
        => RunSingleSkillAsync(DesignSkillIds.SystemDesign, pipelineId, request);

    private async Task<object> RunOrchestratorAsync(long pipelineId, SkillRunRequest? request)
    {
        var runId = Guid.NewGuid().ToString("N");
        var taskName = $"design-orchestrator:{pipelineId}:{runId}";
        var tenantSnapshot = RequestContext.Capture(_httpContextAccessor).TenantId;
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId, tenantSnapshot: tenantSnapshot);

        if (!_quotaGuard.TryAcquire(tenantId, pipelineId, out var rejectReason, out var activePipelineIds))
            throw Oops.Oh(rejectReason ?? "租户 pipeline 配额已满")
                .StatusCode(StatusCodes.Status429TooManyRequests)
                .WithData(new { code = "TENANT_PIPELINE_QUOTA_EXCEEDED", activePipelineIds });

        var status = await _orchestrator.GetStatusAsync(pipelineId, tenantId, projectId, CancellationToken.None);
        if (!status.Ir1Stable)
        {
            _quotaGuard.Release(tenantId, pipelineId);
            throw Oops.Oh("IR-1 未 stable，请先完成 Analyst Skill")
                .StatusCode(StatusCodes.Status400BadRequest);
        }

        try
        {
            await _budgetGuard.ValidateProjectBudgetAsync(projectId, tenantId, 0.95, CancellationToken.None);
        }
        catch
        {
            _quotaGuard.Release(tenantId, pipelineId);
            throw;
        }

        _taskRunner.Run(taskName, async (ctx, ct) =>
        {
            try
            {
                await _orchestrator.RunAsync(pipelineId, tenantId, projectId, new DesignOrchestratorOptions
                {
                    ProviderCode = request?.ProviderCode,
                }, ct);
            }
            finally
            {
                _quotaGuard.Release(tenantId, pipelineId);
            }
        }, timeout: TimeSpan.FromMinutes(30));

        return new
        {
            runId,
            pipelineId,
            status = "running",
            message = "设计 Skill 编排已启动",
        };
    }

    private async Task<object> RunSingleSkillAsync(string skillId, long pipelineId, SkillRunRequest? request)
    {
        var runId = Guid.NewGuid().ToString("N");
        var taskName = $"skill:{pipelineId}:{skillId}:{runId}";
        var tenantSnapshot = RequestContext.Capture(_httpContextAccessor).TenantId;
        var (projectId, tenantId) = await ResolveProjectAsync(pipelineId, tenantSnapshot: tenantSnapshot);

        if (_runGuard.IsRunning(tenantId, pipelineId, skillId))
            throw Oops.Oh($"Skill {skillId} 已在运行中")
                .StatusCode(StatusCodes.Status409Conflict);

        _taskRunner.Run(taskName, async (ctx, ct) =>
        {
            await _harness.RunAsync(skillId, pipelineId, tenantId, projectId, new SkillRunOptions
            {
                UserRequirement = request?.UserRequirement,
                ProviderCode = request?.ProviderCode,
            }, ct);
        }, timeout: TimeSpan.FromMinutes(15));

        return new
        {
            runId,
            skillId,
            pipelineId,
            status = "running",
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
