namespace JNPF.InteAssistant.Entitys.Dto.Skills;

public sealed class PmSpecReviewResult
{
    public int Score { get; init; }
    public string Verdict { get; init; } = "";
    public List<string> Gaps { get; init; } = new();
    public List<PmSpecReviewGap> GapDetails { get; init; } = new();
}

public sealed class PmSpecReviewGap
{
    public string Source { get; init; } = "llm";
    public string Message { get; init; } = "";
}

public sealed class AmendmentUnderstanding
{
    public List<string> Features { get; init; } = new();
    public List<string> Flows { get; init; } = new();
    public List<string> EntitiesOrTables { get; init; } = new();
    public string SummaryMarkdown { get; init; } = "";
    public string Severity { get; init; } = "patch";
    public List<AmendmentPatch> Patches { get; init; } = new();
}

public sealed class PmAmendProposeResult
{
    public string ProposalId { get; init; } = "";
    public AmendmentUnderstanding Understanding { get; init; } = new();
}

public sealed class PmAmendProposeRequest
{
    public string UserMessage { get; init; } = "";
    public string? ProviderCode { get; init; }
}

public sealed class PmAmendApplyRequest
{
    public string ProposalId { get; init; } = "";
    public AmendmentUnderstanding? Understanding { get; init; }
    public string? UserMessage { get; init; }
    public string? ProviderCode { get; init; }
}
