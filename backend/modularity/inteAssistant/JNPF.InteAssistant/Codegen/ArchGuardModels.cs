namespace JNPF.InteAssistant.Codegen;

public sealed class ArchGuardViolation
{
    public string RuleId { get; init; } = string.Empty;
    public string Severity { get; init; } = "warning";
    public string Message { get; init; } = string.Empty;
    public string? FilePath { get; init; }
    public string? Match { get; init; }
}

public sealed class ArchGuardScanResult
{
    public IReadOnlyList<ArchGuardViolation> Violations { get; init; } = Array.Empty<ArchGuardViolation>();
    public int CriticalCount => Violations.Count(v => v.Severity == "critical");
    public int WarningCount => Violations.Count(v => v.Severity == "warning");
    public bool Passed => CriticalCount == 0;
    public bool EventAppended { get; init; }
    public IReadOnlyList<ArchGuardViolation> CriticalViolations =>
        Violations.Where(v => v.Severity == "critical").ToList();
    public IReadOnlyList<ArchGuardViolation> WarningViolations =>
        Violations.Where(v => v.Severity == "warning").ToList();
}

public sealed class YamlStringOrList
{
    public List<string> Values { get; set; } = new();
}

public sealed class ArchGuardRulesDocument
{
    public string Version { get; set; } = "1.0";
    public string Engine { get; set; } = string.Empty;
    public List<ArchGuardRuleDefinition> Rules { get; set; } = new();
    public ArchGuardExecutionSection? Execution { get; set; }
}

public sealed class ArchGuardExecutionSection
{
    public string? CriticalAction { get; set; }
    public string? WarningAction { get; set; }
    public List<string> ScanOrder { get; set; } = new();
}

public sealed class ArchGuardRuleDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ArchGuardTargetEntry> Targets { get; set; } = new();
    public ArchGuardDetectSection? Detect { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Fix { get; set; } = string.Empty;
}

public sealed class ArchGuardTargetEntry
{
    public YamlStringOrList? PathGlob { get; set; }
    public string? FragmentType { get; set; }
}

public sealed class ArchGuardDetectSection
{
    public string Type { get; set; } = string.Empty;
    public string? Pattern { get; set; }
    public YamlStringOrList? Patterns { get; set; }
    public YamlStringOrList? ForbiddenPatterns { get; set; }
    public YamlStringOrList? AllowedExceptions { get; set; }
    public string? ChannelCAllowedSuffix { get; set; }
    public string? Flags { get; set; }
    public bool? RequireAny { get; set; }
    public string? WhenFileContains { get; set; }
    public YamlStringOrList? MustAlsoContainAny { get; set; }
    public string? MustContain { get; set; }
    public string? SkipGlob { get; set; }
    public string? SiblingPattern { get; set; }
    public string? StripSuffix { get; set; }
}
