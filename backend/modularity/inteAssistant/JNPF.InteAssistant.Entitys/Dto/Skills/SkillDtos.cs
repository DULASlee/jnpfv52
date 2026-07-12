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

/// <summary>阶段五 bugfix-skill API 请求体。</summary>
public record BugfixRunRequest
{
    public int FromSequence { get; init; }
    public int ToSequence { get; init; }
    public string? RootCauseLayer { get; init; }
    public string? RevisionType { get; init; }
    public string? Description { get; init; }
    public bool ForceUnlock { get; init; }
    public string? ProviderCode { get; init; }
}

public record ConfirmSkeletonRequest
{
    /// <summary>确认后自动启动 analyst-skill</summary>
    public bool AutoRunAnalyst { get; init; }

    /// <summary>关联的 PM Skill runId（可选，用于 SkillReviewRecorded 血缘）</summary>
    public string? RunId { get; init; }
}

public record ConfirmRequirementSpecRequest
{
    /// <summary>确认后自动启动设计 Skill（architect-skill）</summary>
    public bool AutoRunDesign { get; init; }

    /// <summary>PM 终评低于 85 时，用户显式强制确认放行。</summary>
    public bool ForceConfirm { get; init; }

    public string? RunId { get; init; }
}

/// <summary>Skill 产物人工/Guard 评审（R4 进化层）。</summary>
public record SkillReviewInput
{
    public string SkillId { get; init; } = string.Empty;
    public string Verdict { get; init; } = "approved";
    public string? DetailJson { get; init; }
    public string? RunId { get; init; }
}

/// <summary>经验事件列表项（SkillReviewRecorded / SkillFailureRecorded / HumanCorrectionRecorded）。</summary>
public record SkillExperienceEventDto
{
    public string EventId { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string? SkillId { get; init; }
    public string? FragmentId { get; init; }
    public string Payload { get; init; } = "{}";
    public DateTime CreatedAt { get; init; }
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
