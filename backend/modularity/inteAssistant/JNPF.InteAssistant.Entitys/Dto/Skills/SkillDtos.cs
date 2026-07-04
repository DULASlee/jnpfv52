namespace JNPF.InteAssistant.Entitys.Dto.Skills;

public record SkillRunDto
{
    public string RunId { get; init; } = string.Empty;
    public string SkillId { get; init; } = string.Empty;
    public string Status { get; init; } = "running";
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public long TokenConsumed { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Metadata { get; init; }
}

public record SkillRunRequest
{
    public string? UserRequirement { get; init; }
    public string? ProviderCode { get; init; }
}

public record ConfirmSkeletonRequest
{
    /// <summary>确认后自动启动 analyst-skill</summary>
    public bool AutoRunAnalyst { get; init; }
}

public record SeedTemplateDto
{
    public string TemplateId { get; init; } = string.Empty;
    public string Industry { get; init; } = string.Empty;
    public string EventNamePattern { get; init; } = string.Empty;
    public string ComplexityHint { get; init; } = "simple";
    public decimal CoverageScore { get; init; }
}

public record RerunAffectedStepsInput
{
    /// <summary>显式指定重跑步骤；为空时从最近 EventSpecRevised 或 RevisionType 推断</summary>
    public IReadOnlyList<string>? Steps { get; init; }

    /// <summary>fieldTypeOrConstraint 等，与 EventSpecRevisionPlanner 一致</summary>
    public string? RevisionType { get; init; }
}

public record RerunAffectedStepsResult
{
    public string RunId { get; init; } = string.Empty;
    public string FragmentId { get; init; } = string.Empty;
    public string EventId { get; init; } = string.Empty;
    public IReadOnlyList<string> RerunSteps { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SaStepsCompleted { get; init; } = Array.Empty<string>();
    public bool EventSpecReconfirmed { get; init; }
}
