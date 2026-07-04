namespace JNPF.InteAssistant.Entitys.Dto.Ir;

public record IrEventDto
{
    public string EventId { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string? FragmentId { get; init; }
    public string? FragmentType { get; init; }
    public int FragmentVersion { get; init; }
    public string? SkillId { get; init; }
    public string? SaStepName { get; init; }
    public DateTime CreatedAt { get; init; }
    public string PayloadPreview { get; init; } = string.Empty;
}

public record IrFragmentSnapshotDto
{
    public string FragmentId { get; init; } = string.Empty;
    public string FragmentType { get; init; } = string.Empty;
    public string StabilityState { get; init; } = "draft";
    public int CurrentVersion { get; init; }
    public string[]? SaStepsCompleted { get; init; }
    public object? Payload { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record IrDiagnosticsDto
{
    public long PipelineId { get; init; }
    public string? ProjectId { get; init; }
    public string? TenantId { get; init; }
    public string? WorkspacePath { get; init; }
    public IrRouteEntryDto[]? RouteTable { get; init; }
    public int EventCount { get; init; }
    public int SnapshotCount { get; init; }
    public IrRebuildResultDto? LastRebuild { get; init; }
}

public record IrRebuildResultDto
{
    public int EventCount { get; init; }
    public int FragmentCount { get; init; }
    public long ElapsedMs { get; init; }
    public bool? PassedPerformanceGate { get; init; }
}

public record ConstraintViolationDto
{
    public string RuleId { get; init; } = string.Empty;
    public string Severity { get; init; } = "warning";
    public string Message { get; init; } = string.Empty;
    public string? FragmentType { get; init; }
    public string? FragmentId { get; init; }
}

public record ConstraintCheckResultDto
{
    public IReadOnlyList<ConstraintViolationDto> Violations { get; init; } = Array.Empty<ConstraintViolationDto>();
    public int CriticalCount { get; init; }
    public int WarningCount { get; init; }
    public bool Passed { get; init; }
    public bool EventAppended { get; init; }
}

public record ConstraintCheckInput
{
    public bool Persist { get; init; } = true;
}

public record IrRouteEntryDto
{
    public string Path { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
}

public record SimulateIrEventInput
{
    public string EventType { get; init; } = string.Empty;
    public string? SaStepName { get; init; }
    public bool UseInvalidPayload { get; init; }
    /// <summary>默认 skeleton:SK-001；EventSpec 场景用 eventspec:BE-001</summary>
    public string? FragmentId { get; init; }
    /// <summary>EventSpecConfirmed 模拟：businessRules[].source=inferred</summary>
    public bool WithInferredRules { get; init; }
    /// <summary>SkeletonCreated 含 complexityHint=auto 事件（D9 种子路径）</summary>
    public bool WithAutoSeedEvent { get; init; }
    /// <summary>DDLStabilized 模拟：注入 C-001 分层违规 DDL（阶段三 D7）</summary>
    public bool InjectLayerViolation { get; init; }
}

public record ReviseEventSpecInput
{
    /// <summary>fieldNameOrDescription | fieldTypeOrConstraint | stateMachine | businessProcess | entityRelation | rolePermission</summary>
    public string RevisionType { get; init; } = string.Empty;

    /// <summary>JSON 对象字符串，合并进 EventSpec payload</summary>
    public string? PayloadPatch { get; init; }

    /// <summary>修订完成后是否自动重跑受影响 SA 步骤</summary>
    public bool? AutoRerunAffected { get; init; }
}

public record ReviseEventSpecResult
{
    public string EventId { get; init; } = string.Empty;
    public string FragmentId { get; init; } = string.Empty;
    public string RevisionType { get; init; } = string.Empty;
    public IReadOnlyList<string> AffectedSteps { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RetainedSteps { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RemovedSteps { get; init; } = Array.Empty<string>();
    public int NewVersion { get; init; }
    public bool AutoRerunRequested { get; init; }
}

public record IrStabilityDto
{
    public string FragmentId { get; init; } = string.Empty;
    public string FragmentType { get; init; } = string.Empty;
    public string StabilityState { get; init; } = "draft";
    public string[] SaStepsCompleted { get; init; } = Array.Empty<string>();
    public int RequiredSteps { get; init; }
    public int CompletedCount { get; init; }
    public bool IsStable { get; init; }
}

public record AppendIrEventRequest
{
    public string EventType { get; init; } = string.Empty;
    public string? FragmentId { get; init; }
    public string? FragmentType { get; init; }
    public int FragmentVersion { get; init; } = 1;
    public string Payload { get; init; } = "{}";
    public string? SkillId { get; init; }
    public string? SaStepName { get; init; }
}
