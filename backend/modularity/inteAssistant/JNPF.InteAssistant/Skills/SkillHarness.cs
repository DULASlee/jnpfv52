using JNPF.InteAssistant.Infrastructure.Telemetry;
using JNPF.Common.Core.MultiTenancy;
using System.Diagnostics;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Infrastructure.Messaging;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Runtime;
using JNPF.InteAssistant.Skills.Cognitive;
using JNPF.InteAssistant.Studio;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Skills;

public interface ISkillHarness
{
    Task<SkillRunResult> RunAsync(
        string skillId,
        long pipelineId,
        string tenantId,
        string projectId,
        SkillRunOptions options,
        CancellationToken ct = default);
}

public sealed class SkillRunOptions
{
    public string? UserRequirement { get; init; }
    public string? ProviderCode { get; init; }
    /// <summary>ArchGuard warning 级违规，供 tester-skill metadata（A3 Warning 透传）。</summary>
    public IReadOnlyList<SkillArchWarning>? ArchGuardWarnings { get; init; }
    /// <summary>阶段五 bugfix-skill 序列点 diff。</summary>
    public BugfixRunContext? Bugfix { get; init; }
    /// <summary>
    /// 需求分析三轮编排器专用：是否执行最终工程保障（投影/门禁/Materializer）。
    /// Round 1/2=false（零工程步骤），Round 3=true（一次性保障）。默认 true 保持非编排器调用兼容。
    /// </summary>
    public bool EnableFinalization { get; init; } = true;

    /// <summary>
    /// Round 2 专用：Analyst 在 compile 之上做受控语义分析并写回 Skeleton（非仅 Compile）。
    /// </summary>
    public bool EnableSemanticAnalysis { get; init; }
}

public sealed class SkillArchWarning
{
    public string RuleId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? FilePath { get; init; }
}

public sealed class SkillRunResult
{
    public string RunId { get; init; } = string.Empty;
    public string SkillId { get; init; } = string.Empty;
    public string Status { get; init; } = "completed";
    public int EventsAppended { get; init; }
    public long TokenConsumed { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class SkillHarness : ISkillHarness, ITransient
{
    /// <summary>SqlSugar InitMappingInfo 非线程安全；并行 stage-confirm Skill 落 run 记录时需串行。</summary>
    private static readonly SemaphoreSlim InsertRunLock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ISkillRegistry _registry;
    private readonly IIrProjectionEngine _projection;
    private readonly IIrEventStoreService _eventStore;
    private readonly IIrSchemaValidator _schemaValidator;
    private readonly IContextBuilderService _contextBuilder;
    private readonly ISkillExecutionLogger _skillLogger;
    private readonly ISkillRunGuard _runGuard;
    private readonly IPipelineSseChannelHub _sseHub;
    private readonly ISqlSugarClient _db;
    private readonly IExperienceRecorder _experience;
    private readonly ISkillDeliverableCoordinator _deliverableCoordinator;
    private readonly ILogger<SkillHarness> _logger;

    public SkillHarness(
        ISkillRegistry registry,
        IIrProjectionEngine projection,
        IIrEventStoreService eventStore,
        IIrSchemaValidator schemaValidator,
        IContextBuilderService contextBuilder,
        ISkillExecutionLogger skillLogger,
        ISkillRunGuard runGuard,
        IPipelineSseChannelHub sseHub,
        ISqlSugarClient db,
        IExperienceRecorder experience,
        ISkillDeliverableCoordinator deliverableCoordinator,
        ILogger<SkillHarness> logger)
    {
        _registry = registry;
        _projection = projection;
        _eventStore = eventStore;
        _schemaValidator = schemaValidator;
        _contextBuilder = contextBuilder;
        _skillLogger = skillLogger;
        _runGuard = runGuard;
        _sseHub = sseHub;
        _db = db;
        _experience = experience;
        _deliverableCoordinator = deliverableCoordinator;
        _logger = logger;
    }

    public async Task<SkillRunResult> RunAsync(
        string skillId,
        long pipelineId,
        string tenantId,
        string projectId,
        SkillRunOptions options,
        CancellationToken ct = default)
    {
        var runId = Guid.NewGuid().ToString("N");
        if (!_runGuard.TryAcquire(tenantId, pipelineId, skillId, runId, out var conflictRunId))
            throw Oops.Oh($"Skill {skillId} 已在运行中 (runId={conflictRunId})")
                .StatusCode(StatusCodes.Status409Conflict);

        var sw = Stopwatch.StartNew();
        // P6-O01 skill.run OTel Span（零依赖，ActivitySource 未注册时返回 null，无开销）
        using var activity = StudioActivitySource.StartSkillRun(skillId, runId, projectId, tenantId, pipelineId);
        using var execScope = SkillExecutionScope.Begin(
            runId, tenantId, projectId, pipelineId, skillId, ct);
        using var logScope = _skillLogger.BeginScope(runId, tenantId, projectId, pipelineId, skillId);
        var skill = _registry.GetRequired(skillId);
        var collected = new List<AppendIrEventRequest>();
        // H3: 当前架构 Skill 为确定性执行（零 LLM），token 跟踪尚未实现。
        // tokenConsumed 始终为 0，待未来引入 LLM 调用后由调用方通过 SkillRunOptions 传入。
        long tokenConsumed = 0;

        await InsertRunAsync(runId, tenantId, projectId, pipelineId, skillId, ct);

        try
        {
            _skillLogger.LogPhase("RunStart", "started", 0);

            var snapshot = await BuildSnapshotAsync(tenantId, projectId, pipelineId.ToString(), ct);
            var inputValidation = await skill.ValidateInputAsync(snapshot, ct);
            if (!inputValidation.IsValid)
                throw Oops.Bah(inputValidation.ErrorMessage ?? "Skill 输入校验失败");

            _skillLogger.LogPhase("ValidateInput", "passed", sw.ElapsedMilliseconds);

            var requirement = options.UserRequirement ?? await LoadUserRequirementAsync(pipelineId, ct);
            var seeds = await _contextBuilder.FindSeedMatchesAsync(requirement, ct);
            var promptContext = _contextBuilder.Build(skill.InformationNeeds, snapshot, seeds);

            var context = new SkillContext
            {
                RunId = runId,
                TenantId = tenantId,
                ProjectId = projectId,
                PipelineId = pipelineId,
                UserRequirement = requirement,
                Snapshot = snapshot,
                ArchGuardWarnings = options.ArchGuardWarnings,
                Bugfix = options.Bugfix,
                SeedMatches = seeds,
                PromptContext = promptContext,
                ProviderCode = options.ProviderCode,
                EnableFinalization = options.EnableFinalization,
                EnableSemanticAnalysis = options.EnableSemanticAnalysis,
            };

            PushSkillProgress(pipelineId, skillId, runId, "reason", 10, "Skill 推理中…");

            await foreach (var evt in skill.ReasonAsync(context, ct))
            {
                ct.ThrowIfCancellationRequested();
                _schemaValidator.Validate(evt.EventType, evt.Payload);

                var appendRequest = evt with { SkillId = evt.SkillId ?? skillId };
                await _eventStore.AppendAsync(projectId, tenantId, appendRequest, ct);
                collected.Add(appendRequest);

                _skillLogger.LogPhase("AppendIr", "ok", sw.ElapsedMilliseconds,
                    appendRequest.FragmentId, appendRequest.SaStepName, appendRequest.EventType);
            }

            var outputValidation = await skill.ValidateOutputAsync(collected, ct);
            if (!outputValidation.IsValid)
                throw Oops.Bah(outputValidation.ErrorMessage ?? "Skill 产出校验失败");

            _skillLogger.LogPhase("ValidateOutput", "passed", sw.ElapsedMilliseconds);

            await CompleteRunAsync(runId, "completed", tokenConsumed, collected.Count, null, ct);
            await _deliverableCoordinator.AfterSkillCompletedAsync(
                skillId, tenantId, pipelineId, projectId, collected, ct);
            PushSkillProgress(pipelineId, skillId, runId, "completed", 100, "Skill 完成");

            return new SkillRunResult
            {
                RunId = runId,
                SkillId = skillId,
                Status = "completed",
                EventsAppended = collected.Count,
                TokenConsumed = tokenConsumed,
            };
        }
        catch (AbortSkillChainException ex)
        {
            _logger.LogWarning(
                "Skill chain aborted: {SkillId} pipeline={PipelineId} phase={Phase}",
                skillId,
                pipelineId,
                ex.Phase);
            _skillLogger.LogPhase("RunAborted", ex.Phase, sw.ElapsedMilliseconds, message: ex.Message);
            await CompleteRunAsync(runId, "aborted", tokenConsumed, collected.Count, ex.Message, ct);
            await TryRecordFailureAsync(projectId, tenantId, skillId, runId, ex, ct);
            PushSkillProgress(pipelineId, skillId, runId, "aborted", 100, ex.Message);
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Skill run failed: {SkillId} pipeline={PipelineId}", skillId, pipelineId);
            _skillLogger.LogPhase("RunFailed", "error", sw.ElapsedMilliseconds, message: ex.Message);
            await CompleteRunAsync(runId, "failed", tokenConsumed, collected.Count, ex.Message, ct);
            await TryRecordFailureAsync(projectId, tenantId, skillId, runId, ex, ct);
            PushSkillProgress(pipelineId, skillId, runId, "failed", 100, ex.Message);
            throw;
        }
        catch (OperationCanceledException)
        {
            _skillLogger.LogPhase("RunFailed", "cancelled", sw.ElapsedMilliseconds);
            await CompleteRunAsync(runId, "cancelled", tokenConsumed, collected.Count, "cancelled", ct);
            await TryRecordFailureAsync(projectId, tenantId, skillId, runId,
                new OperationCanceledException("Skill run cancelled"), ct);
            throw;
        }
        finally
        {
            _runGuard.Release(tenantId, pipelineId, skillId);
        }
    }

    private async Task<IrSnapshot> BuildSnapshotAsync(string tenantId, string projectId, string pipelineId, CancellationToken ct)
    {
        var dtos = await _eventStore.ListSnapshotsAsync(projectId, tenantId, pipelineId, ct);
        var fragments = dtos.Select(d => new IrSnapshotFragment
        {
            FragmentId = d.FragmentId,
            FragmentType = d.FragmentType,
            StabilityState = d.StabilityState,
            Payload = d.Payload is string s ? s : JsonSerializer.Serialize(d.Payload, JsonOptions),
            SaStepsCompleted = d.SaStepsCompleted ?? Array.Empty<string>(),
        }).ToList();
        return new IrSnapshot { Fragments = fragments };
    }

    private async Task<string> LoadUserRequirementAsync(long pipelineId, CancellationToken ct)
    {
        var tenantId = TenantResolver.Resolve().ToString();
        var msg = await _db.Queryable<AiPipelineMessageEntity>()
            .Where(x => x.PipelineId == pipelineId.ToString() && x.TenantId == tenantId && x.Role == "user")
            .OrderByDescending(x => x.CreatorTime)
            .FirstAsync(ct);
        return msg?.Content ?? string.Empty;
    }

    private async Task InsertRunAsync(string runId, string tenantId, string projectId, long pipelineId, string skillId, CancellationToken ct)
    {
        await InsertRunLock.WaitAsync(ct);
        try
        {
            await _db.Insertable(new AiSkillRunEntity
            {
                Id = runId,
                TenantId = tenantId,
                ProjectId = projectId,
                PipelineId = pipelineId.ToString(),
                SkillId = skillId,
                Status = "running",
                StartedAt = DateTime.UtcNow,
            }).ExecuteCommandAsync(ct);
        }
        finally
        {
            InsertRunLock.Release();
        }
    }

    private async Task CompleteRunAsync(
        string runId, string status, long tokenConsumed, int eventCount, string? error, CancellationToken ct)
    {
        await _db.Updateable<AiSkillRunEntity>()
            .SetColumns(x => new AiSkillRunEntity
            {
                Status = status,
                CompletedAt = DateTime.UtcNow,
                TokenConsumed = tokenConsumed,
                ErrorMessage = error,
                Metadata = JsonSerializer.Serialize(new { eventCount }, JsonOptions),
            })
            .Where(x => x.Id == runId)
            .ExecuteCommandAsync(ct);
    }

    private async Task TryRecordFailureAsync(
        string projectId, string tenantId, string skillId, string runId, Exception ex, CancellationToken ct)
    {
        try
        {
            await _experience.RecordFailureAsync(
                projectId, tenantId, skillId, runId,
                SkillExperienceClassifier.Classify(ex),
                ex.Message,
                ct);
        }
        catch (Exception recordEx)
        {
            _logger.LogWarning(recordEx, "SkillFailureRecorded 写入失败（非阻断） runId={RunId}", runId);
        }
    }

    private void PushSkillProgress(long pipelineId, string skillId, string runId, string phase, int percent, string message)
    {
        var payload = JsonSerializer.Serialize(new
        {
            skillId,
            runId,
            phase,
            percent,
            message,
        }, JsonOptions);
        _sseHub.TryPush(pipelineId, SseEventType.SkillProgress, payload);
    }
}
