using JNPF.Common.Core.MultiTenancy;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.Skills;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Infrastructure.Background;
using JNPF.InteAssistant.Infrastructure.Messaging;
using JNPF.InteAssistant.Infrastructure.Security;
using JNPF.InteAssistant.Llm;
using JNPF.InteAssistant.Runtime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System.Text.Json;
using JNPF;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 设计 Skill REST API（阶段三 P3-B01）
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "StudioDesignSkills", Order = 193)]
[Route("api/studio/skills")]
public class DesignSkillsApiService : IDynamicApiController, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly ISqlSugarClient _db;
    private readonly IDesignSkillOrchestrator _orchestrator;
    private readonly ISkillHarness _harness;
    private readonly IBackgroundTaskRunner _taskRunner;
    private readonly ITenantGuard _tenantGuard;
    private readonly ITenantPipelineQuotaGuard _quotaGuard;
    private readonly ISkillRunGuard _runGuard;
    private readonly ISkillLlmBudgetGuard _budgetGuard;
    private readonly IPipelineSseChannelHub _sseHub;
    private readonly ISseSenderFactory _sseSenderFactory;
    private readonly IUserRequirementLoader _requirementLoader;
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
        IPipelineSseChannelHub sseHub,
        ISseSenderFactory sseSenderFactory,
        IUserRequirementLoader requirementLoader,
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
        _sseHub = sseHub;
        _sseSenderFactory = sseSenderFactory;
        _requirementLoader = requirementLoader;
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

    /// <summary>ADR-005 P3：总体设计澄清 Skill（两阶段，提问 + 阶段二约束引擎锁定）。</summary>
    [HttpPost("system-design-clarification/{pipelineId:long}/run")]
    public Task<object> RunSystemDesignClarificationAsync(long pipelineId, [FromBody] SkillRunRequest? request)
        => RunSingleSkillAsync(DesignSkillIds.SystemDesignClarification, pipelineId, request);

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
        if (!status.CanRunDesign)
        {
            _quotaGuard.Release(tenantId, pipelineId);
            throw Oops.Bah(BuildCannotRunDesignReason(status))
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

        var channel = _sseHub.ReplaceChannel(pipelineId);

        _taskRunner.Run(taskName, async (ctx, ct) =>
        {
            using var scope = App.RootServices.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<IDesignSkillOrchestrator>();
            using var sse = _sseSenderFactory.Create(pipelineId.ToString(), channel);
            sse.TrySend("design_orchestrator_started", JsonSerializer.Serialize(new
            {
                pipelineId,
                runId,
                source = "design-orchestrator-run",
            }, JsonOptions));
            try
            {
                var result = await orchestrator.RunAsync(pipelineId, tenantId, projectId, new DesignOrchestratorOptions
                {
                    ProviderCode = request?.ProviderCode,
                }, ct);
                if (result.Status is "completed")
                {
                    sse.TrySend("stage_transition", "architecture");
                    sse.TrySend("design_orchestrator_completed", JsonSerializer.Serialize(result, JsonOptions));
                }
                else
                {
                    sse.TrySend("design_orchestrator_failed", JsonSerializer.Serialize(new
                    {
                        status = result.Status,
                        message = result.ErrorMessage ?? "设计编排未完成",
                    }, JsonOptions));
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                sse.TrySend("design_orchestrator_failed", JsonSerializer.Serialize(new
                {
                    status = "failed",
                    message = ex.Message,
                }, JsonOptions));
            }
            finally
            {
                sse.Complete();
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

        // 单 Skill 入口同样走 25 §6 门禁（澄清重跑除外：架构/总体澄清阶段一可在 Finalize 后）
        var isClarification = skillId is DesignSkillIds.SystemDesignClarification;
        if (!isClarification)
        {
            var status = await _orchestrator.GetStatusAsync(pipelineId, tenantId, projectId, CancellationToken.None);
            if (!status.CanRunDesign)
            {
                throw Oops.Bah(BuildCannotRunDesignReason(status))
                    .StatusCode(StatusCodes.Status400BadRequest);
            }
        }

        // 与编排器对齐：先建 SSE 通道，Harness/Architect 的 TryPush 才能送达前端
        var channel = _sseHub.ReplaceChannel(pipelineId);

        _taskRunner.Run(taskName, async (ctx, ct) =>
        {
            using var scope = App.RootServices.CreateScope();
            var harness = scope.ServiceProvider.GetRequiredService<ISkillHarness>();
            var requirementLoader = scope.ServiceProvider.GetRequiredService<IUserRequirementLoader>();
            using var sse = _sseSenderFactory.Create(pipelineId.ToString(), channel);
            sse.TrySend("skill_run_started", JsonSerializer.Serialize(new
            {
                pipelineId,
                skillId,
                runId,
                source = "design-single-skill-run",
            }, JsonOptions));
            try
            {
                var userRequirement = request?.UserRequirement;
                if (string.IsNullOrWhiteSpace(userRequirement))
                    userRequirement = await requirementLoader.LoadAsync(tenantId, projectId, pipelineId, ct);

                await harness.RunAsync(skillId, pipelineId, tenantId, projectId, new SkillRunOptions
                {
                    UserRequirement = userRequirement,
                    ProviderCode = request?.ProviderCode,
                }, ct);
                sse.TrySend("skill_run_completed", JsonSerializer.Serialize(new
                {
                    pipelineId,
                    skillId,
                    runId,
                    status = "completed",
                }, JsonOptions));
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                sse.TrySend("skill_run_failed", JsonSerializer.Serialize(new
                {
                    pipelineId,
                    skillId,
                    runId,
                    status = "failed",
                    message = ex.Message,
                }, JsonOptions));
            }
            finally
            {
                sse.Complete();
            }
        }, timeout: TimeSpan.FromMinutes(15));

        return new
        {
            runId,
            skillId,
            pipelineId,
            status = "running",
        };
    }

    /// <summary>按优先级拼拒绝原因：Finalize → 字段投影 → 质量门控。</summary>
    private static string BuildCannotRunDesignReason(DesignOrchestratorStatus status)
    {
        if (!status.AnalysisFinalized)
            return AnalysisFinalizedGate.NotFinalizedMessage;
        if (!status.HasEntityFields)
            return "ai_entity_field 无投影字段，请先完成 Round 3 工程保障";
        if (!status.QualityGatePasses)
        {
            if (status.QualityCriticalCount > 0)
                return $"一致性存在 {status.QualityCriticalCount} 条 CRITICAL，禁止启动设计 Skill";
            return $"质量门控未通过：总分={status.QualityTotalScore:0.#}（须≥60）、结构分须≥70";
        }
        if (!status.PmReviewGatePasses)
        {
            if (status.PmReviewScore is > 0)
                return $"PM 终评 {status.PmReviewScore} 分（须≥85）。请补充需求说明书后重试，或在确认卡片使用「强制确认」";
            return "PM 终评尚未通过：请先完成需求说明书确认与 Finalize";
        }
        return "设计前置条件未满足";
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

        // R12：MUST 返回真实 ProjectId（F_PROJECT_ID），禁止用 pipelineId 冒充
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
