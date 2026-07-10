using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Common;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Infrastructure.Background;
using JNPF.InteAssistant.Infrastructure.Security;
using JNPF.InteAssistant.Runtime;
using JNPF.InteAssistant.Skills;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Pipeline;

/// <summary>
/// SUP-01a：阶段确认后触发下一步 Skill（仅编排调度，IR 读写由各 Skill/Orchestrator 负责）。
/// </summary>
public interface IStageConfirmSkillTrigger
{
    /// <param name="confirmedStage">用户确认通过时的阶段（推进前）</param>
    /// <param name="nextStage">推进后的当前阶段</param>
    Task<StageConfirmTriggerResult> TriggerAfterConfirmAsync(
        long pipelineId,
        string tenantId,
        string confirmedStage,
        string? nextStage,
        CancellationToken ct = default);
}

public sealed class StageConfirmTriggerResult
{
    public IReadOnlyList<string> TriggeredSkillIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> BackgroundTaskNames { get; init; } = Array.Empty<string>();
}

public sealed class StageConfirmSkillTrigger : IStageConfirmSkillTrigger, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ISkillHarness _harness;
    private readonly IDeveloperSkillOrchestrator _developerOrchestrator;
    private readonly IBackgroundTaskRunner _taskRunner;
    private readonly ITenantPipelineQuotaGuard _quotaGuard;
    private readonly ISkillRunGuard _runGuard;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StageConfirmSkillTrigger> _logger;

    public StageConfirmSkillTrigger(
        ISqlSugarClient db,
        ISkillHarness harness,
        IDeveloperSkillOrchestrator developerOrchestrator,
        IBackgroundTaskRunner taskRunner,
        ITenantPipelineQuotaGuard quotaGuard,
        ISkillRunGuard runGuard,
        IConfiguration configuration,
        ILogger<StageConfirmSkillTrigger> logger)
    {
        _db = db;
        _harness = harness;
        _developerOrchestrator = developerOrchestrator;
        _taskRunner = taskRunner;
        _quotaGuard = quotaGuard;
        _runGuard = runGuard;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<StageConfirmTriggerResult> TriggerAfterConfirmAsync(
        long pipelineId,
        string tenantId,
        string confirmedStage,
        string? nextStage,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nextStage))
            return new StageConfirmTriggerResult();

        // R12：真实 ProjectId，禁止用 pipelineId 冒充
        var projectId = await ResolveProjectIdAsync(pipelineId, ct);
        var triggered = new List<string>();
        var tasks = new List<string>();

        switch (confirmedStage)
        {
            case PipelineStage.Requirement:
                ScheduleSingleSkill(DesignSkillIds.Architect, pipelineId, tenantId, projectId, triggered, tasks);
                break;

            case PipelineStage.Architecture:
                ScheduleDesignPhaseSkills(pipelineId, tenantId, projectId, triggered, tasks);
                break;

            case PipelineStage.Design:
                // 产品铁律：确认总体设计后自动 Dev→Test→Deploy，中间无 HITL
                ScheduleDeveloperOrchestrator(pipelineId, tenantId, projectId, triggered, tasks);
                break;

            case PipelineStage.Development:
                // 兜底重试：若自动链未跑完部署，开发阶段确认可再触发 deploy
                ScheduleDelivery(pipelineId, tenantId, projectId, triggered, tasks);
                break;

            default:
                _logger.LogDebug(
                    "阶段确认无 Skill 触发: PipelineId={PipelineId}, Confirmed={Confirmed}",
                    pipelineId, confirmedStage);
                break;
        }

        return new StageConfirmTriggerResult
        {
            TriggeredSkillIds = triggered,
            BackgroundTaskNames = tasks,
        };
    }

    private async Task<string> ResolveProjectIdAsync(long pipelineId, CancellationToken ct)
    {
        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.Id == pipelineId.ToString())
            .Select(x => new { x.ProjectId })
            .FirstAsync(ct);

        if (pipeline == null || string.IsNullOrWhiteSpace(pipeline.ProjectId))
            return pipelineId.ToString();

        return pipeline.ProjectId;
    }

    private void ScheduleSingleSkill(
        string skillId,
        long pipelineId,
        string tenantId,
        string projectId,
        List<string> triggered,
        List<string> tasks)
    {
        if (_runGuard.IsRunning(tenantId, pipelineId, skillId))
        {
            _logger.LogInformation("Skill 已在运行，跳过重复触发: {SkillId}, Pipeline={PipelineId}", skillId, pipelineId);
            return;
        }

        if (!_quotaGuard.TryAcquire(tenantId, pipelineId, out var rejectReason, out _))
        {
            _logger.LogWarning("配额不足，跳过 Skill 触发: {SkillId}, Pipeline={PipelineId}, Reason={Reason}",
                skillId, pipelineId, rejectReason);
            return;
        }

        var runId = Guid.NewGuid().ToString("N");
        var taskName = $"stage-confirm:{pipelineId}:{skillId}:{runId}";
        tasks.Add(taskName);
        triggered.Add(skillId);

        _taskRunner.Run(taskName, async (_, ct) =>
        {
            try
            {
                await _harness.RunAsync(skillId, pipelineId, tenantId, projectId, new SkillRunOptions(), ct);
            }
            finally
            {
                _quotaGuard.Release(tenantId, pipelineId);
            }
        }, timeout: TimeSpan.FromMinutes(30));

        _logger.LogInformation(
            "阶段确认已调度 Skill: PipelineId={PipelineId}, SkillId={SkillId}, Task={Task}",
            pipelineId, skillId, taskName);
    }

    /// <summary>
    /// 架构确认 → db/ui 并行 → system-design 串行（对齐 DesignSkillOrchestrator）。
    /// </summary>
    private void ScheduleDesignPhaseSkills(
        long pipelineId,
        string tenantId,
        string projectId,
        List<string> triggered,
        List<string> tasks)
    {
        if (!_quotaGuard.TryAcquire(tenantId, pipelineId, out var rejectReason, out _))
        {
            _logger.LogWarning("配额不足，跳过设计阶段 Skill: Pipeline={PipelineId}, Reason={Reason}",
                pipelineId, rejectReason);
            return;
        }

        var runId = Guid.NewGuid().ToString("N");
        var taskName = $"stage-confirm:design-phase:{pipelineId}:{runId}";
        tasks.Add(taskName);
        triggered.Add(DesignSkillIds.DbDesign);
        triggered.Add(DesignSkillIds.UiDesign);
        triggered.Add(DesignSkillIds.SystemDesign);

        var providerCode = _configuration.GetValue<string>("AI:DefaultProvider") ?? "mimo";
        var skillOptions = new SkillRunOptions { ProviderCode = providerCode };

        _taskRunner.Run(taskName, async (_, ct) =>
        {
            try
            {
                var parallel = new[] { DesignSkillIds.DbDesign, DesignSkillIds.UiDesign };
                await Task.WhenAll(parallel.Select(skillId =>
                    _harness.RunAsync(skillId, pipelineId, tenantId, projectId, skillOptions, ct)));

                await _harness.RunAsync(
                    DesignSkillIds.SystemDesign, pipelineId, tenantId, projectId, skillOptions, ct);
            }
            finally
            {
                _quotaGuard.Release(tenantId, pipelineId);
            }
        }, timeout: TimeSpan.FromMinutes(45));

        _logger.LogInformation(
            "阶段确认已调度设计阶段 Skill 链: PipelineId={PipelineId}, Task={Task}",
            pipelineId, taskName);
    }

    private void ScheduleDeveloperOrchestrator(
        long pipelineId,
        string tenantId,
        string projectId,
        List<string> triggered,
        List<string> tasks)
    {
        if (_runGuard.IsRunning(tenantId, pipelineId, DevelopmentSkillIds.Developer))
        {
            _logger.LogInformation("Developer 编排已在运行: Pipeline={PipelineId}", pipelineId);
            return;
        }

        if (!_quotaGuard.TryAcquire(tenantId, pipelineId, out var rejectReason, out _))
        {
            _logger.LogWarning("配额不足，跳过 Developer 编排: Pipeline={PipelineId}, Reason={Reason}",
                pipelineId, rejectReason);
            return;
        }

        var runId = Guid.NewGuid().ToString("N");
        var taskName = $"stage-confirm:developer-orchestrator:{pipelineId}:{runId}";
        tasks.Add(taskName);
        triggered.Add(DevelopmentSkillIds.Developer);
        triggered.Add(DevelopmentSkillIds.Tester);
        triggered.Add(DeploySkillIds.Deploy);

        _taskRunner.Run(taskName, async (_, ct) =>
        {
            try
            {
                await _developerOrchestrator.RunAsync(
                    pipelineId, tenantId, projectId, new DeveloperOrchestratorOptions(), ct);
            }
            finally
            {
                _quotaGuard.Release(tenantId, pipelineId);
            }
        }, timeout: TimeSpan.FromMinutes(60));

        _logger.LogInformation(
            "阶段确认已调度自动交付链(Dev→Test→Deploy): PipelineId={PipelineId}, Task={Task}",
            pipelineId, taskName);
    }

    private void ScheduleDelivery(
        long pipelineId,
        string tenantId,
        string projectId,
        List<string> triggered,
        List<string> tasks)
    {
        if (_runGuard.IsRunning(tenantId, pipelineId, DeploySkillIds.Deploy))
        {
            _logger.LogInformation("deploy-skill 已在运行: Pipeline={PipelineId}", pipelineId);
            return;
        }

        if (!_quotaGuard.TryAcquire(tenantId, pipelineId, out var rejectReason, out _))
        {
            _logger.LogWarning("配额不足，跳过 deploy-skill: Pipeline={PipelineId}, Reason={Reason}",
                pipelineId, rejectReason);
            return;
        }

        var runId = Guid.NewGuid().ToString("N");
        var taskName = $"stage-confirm:deploy-skill:{pipelineId}:{runId}";
        tasks.Add(taskName);
        triggered.Add(DeploySkillIds.Deploy);

        _taskRunner.Run(taskName, async (_, ct) =>
        {
            try
            {
                await _harness.RunAsync(
                    DeploySkillIds.Deploy,
                    pipelineId,
                    tenantId,
                    projectId,
                    new SkillRunOptions(),
                    ct);
            }
            finally
            {
                _quotaGuard.Release(tenantId, pipelineId);
            }
        }, timeout: TimeSpan.FromMinutes(25));

        _logger.LogInformation(
            "阶段确认已调度 deploy-skill: PipelineId={PipelineId}, Task={Task}",
            pipelineId, taskName);
    }
}
