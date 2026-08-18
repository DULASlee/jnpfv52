namespace JNPF.InteAssistant.Entitys.Dto.Skills;

/// <summary>S2 流水线细粒度进度（编排器/前端展示用，与 SpecPhase 分离）。</summary>
public enum S2PipelineStage
{
    GatePending = 0,
    GatePassed = 1,
    PmEnhanceRunning = 10,
    PmEnhanceAwaitingUser = 11,
    SaDecomposeRunning = 20,
    SaDecomposeDone = 21,
    PmRefineRunning = 30,
    PmRefineAwaitingUser = 31,
    ClarificationRoundAwaiting = 40,
    ClarificationRoundAnswered = 41,
    SpecRendering = 50,
    SpecAwaitingUserConfirm = 51,
    SpecConfirmed = 60,
    PmFinalReview = 61,
    EngineeringFinalize = 62,
    S2Complete = 99,
}

/// <summary>S2《需求分析说明书》生命周期阶段（唯一 Phase 枚举）。</summary>
public enum RequirementSpecPhase
{
    Absent = 0,
    Refining = 1,
    Rendered = 2,
    Confirmed = 3,
    PmReviewed = 4,
    Finalized = 5,
    Superseded = 6,
}

/// <summary>说明书合法状态转换（P4 阶段 3 Transition handlers 使用）。</summary>
public enum RequirementSpecTransition
{
    StartRefining,
    Render,
    Confirm,
    PmReview,
    Finalize,
    Supersede,
    ResumeAfterSupersede,
}

public static class RequirementSpecConstants
{
    public const string RelativePath = "02-requirement-spec.md";
    public const string FormalTitleMarker = "# 需求分析规格说明书";
    public const string FormalCtaMarker = "请你确认需求分析说明书";
    public static string SpecStateFragmentId(long pipelineId) => $"requirement-spec-state:{pipelineId}";
    public static string WorkingRequirementFragmentId(long pipelineId) => $"requirement:{pipelineId}";
}

/// <summary>Resolver 只读快照 — 编排器/前端唯一 Phase 来源（P4 阶段 1+）。</summary>
public sealed record RequirementSpecSnapshot
{
    public RequirementSpecPhase Phase { get; init; }
    public S2PipelineStage? PipelineStage { get; init; }
    public int ClarRound { get; init; }
    public bool AwaitingUser { get; init; }
    public bool HasProgressRow { get; init; }
    public int Version { get; init; }
    public string RelativePath { get; init; } = RequirementSpecConstants.RelativePath;
    public string? ContentHash { get; init; }
    public int? ContentLength { get; init; }
    public string? FormalMarkdown { get; init; }
    public string? WorkingText { get; init; }
    public bool CanUserConfirm { get; init; }
    public bool CanUserFeedback { get; init; }
    public bool CanFinalize { get; init; }
    public string? BlockReason { get; init; }
}

/// <summary>L2 进度表 upsert 补丁（仅写非 null 字段）。</summary>
public sealed record S2ProgressUpdate
{
    public required string TenantId { get; init; }
    public required string ProjectId { get; init; }
    public required long PipelineId { get; init; }
    public S2PipelineStage? PipelineStage { get; init; }
    public RequirementSpecPhase? SpecPhase { get; init; }
    public int? ClarRound { get; init; }
    public int? SpecVersion { get; init; }
    public string? ContentHash { get; init; }
    public int? ContentLength { get; init; }
    public bool? AwaitingUser { get; init; }
}

public sealed record FormalSpecGateResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Violations { get; init; } = Array.Empty<string>();
}
