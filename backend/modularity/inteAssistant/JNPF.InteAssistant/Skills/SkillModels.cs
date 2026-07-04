using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;

namespace JNPF.InteAssistant.Skills;

public sealed class SkillInformationNeeds
{
    public IReadOnlyList<string> IrFragmentTypes { get; init; } = Array.Empty<string>();
    public string RequiredStability { get; init; } = IrStabilityStates.Draft;
    public IReadOnlyList<string>? CanFilterByDomain { get; init; }
}

public sealed class SkillOutputDeclaration
{
    public IReadOnlyList<string> IrEventTypes { get; init; } = Array.Empty<string>();
}

public sealed class SkillValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }

    public static SkillValidationResult Ok() => new() { IsValid = true };
    public static SkillValidationResult Fail(string message) => new() { IsValid = false, ErrorMessage = message };
}

public sealed class IrSnapshotFragment
{
    public string FragmentId { get; init; } = string.Empty;
    public string FragmentType { get; init; } = string.Empty;
    public string StabilityState { get; init; } = IrStabilityStates.Draft;
    public string Payload { get; init; } = "{}";
    public string[] SaStepsCompleted { get; init; } = Array.Empty<string>();
}

public sealed class IrSnapshot
{
    public static IrSnapshot Empty { get; } = new();

    public IReadOnlyList<IrSnapshotFragment> Fragments { get; init; } = Array.Empty<IrSnapshotFragment>();

    public IrSnapshotFragment? Find(string fragmentType, string? minStability = null)
    {
        foreach (var f in Fragments)
        {
            if (!string.Equals(f.FragmentType, fragmentType, StringComparison.Ordinal))
                continue;
            if (minStability != null && StabilityRank(f.StabilityState) < StabilityRank(minStability))
                continue;
            return f;
        }

        return null;
    }

    public static int StabilityRank(string state) => state switch
    {
        IrStabilityStates.Locked => 4,
        IrStabilityStates.Stable => 3,
        IrStabilityStates.InProgress => 2,
        _ => 1,
    };
}

public sealed class SeedTemplateMatch
{
    public string TemplateId { get; init; } = string.Empty;
    public string Industry { get; init; } = string.Empty;
    public string EventNamePattern { get; init; } = string.Empty;
    public string ComplexityHint { get; init; } = "simple";
    public decimal CoverageScore { get; init; }
    public string TemplateJson { get; init; } = "{}";
}

/// <summary>阶段五 bugfix-skill 运行参数。</summary>
public sealed class BugfixRunContext
{
    public int FromSequence { get; init; }
    public int ToSequence { get; init; }
    public string? RootCauseLayer { get; init; }
    public string? RevisionType { get; init; }
    public string? Description { get; init; }
    public bool ForceUnlock { get; init; }
}

public sealed class SkillContext
{
    public required string RunId { get; init; }
    public required string TenantId { get; init; }
    public required string ProjectId { get; init; }
    public required long PipelineId { get; init; }
    public required string UserRequirement { get; init; }
    public IrSnapshot Snapshot { get; init; } = IrSnapshot.Empty;
    public IReadOnlyList<SkillArchWarning>? ArchGuardWarnings { get; init; }
    public IReadOnlyList<SeedTemplateMatch> SeedMatches { get; init; } = Array.Empty<SeedTemplateMatch>();
    public PromptContext PromptContext { get; init; } = PromptContext.Empty;
    public string? ProviderCode { get; init; }
    /// <summary>阶段五 bugfix-skill 序列点 diff 参数。</summary>
    public BugfixRunContext? Bugfix { get; init; }
}

public sealed class PromptContext
{
    public static PromptContext Empty { get; } = new();

    public IReadOnlyList<IrSnapshotFragment> IrFragments { get; init; } = Array.Empty<IrSnapshotFragment>();
    public IReadOnlyList<SeedTemplateMatch> SeedData { get; init; } = Array.Empty<SeedTemplateMatch>();
    public string CompressedSummary { get; init; } = string.Empty;
}

public interface IBaseSkill
{
    string SkillId { get; }
    string Version { get; }
    SkillInformationNeeds InformationNeeds { get; }
    SkillOutputDeclaration Outputs { get; }

    Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default);
    IAsyncEnumerable<AppendIrEventRequest> ReasonAsync(SkillContext context, CancellationToken ct = default);
    Task<SkillValidationResult> ValidateOutputAsync(IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct = default);
}
