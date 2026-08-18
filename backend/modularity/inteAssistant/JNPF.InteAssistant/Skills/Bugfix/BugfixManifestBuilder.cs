using System.Text.Json;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;

namespace JNPF.InteAssistant.Skills.Bugfix;

public static class BugfixManifestBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string BuildBugReportedPayload(string projectId, string runId, string? description)
        => JsonSerializer.Serialize(new
        {
            projectId,
            runId,
            description = description ?? string.Empty,
            reportedAt = DateTime.UtcNow.ToString("O"),
        }, JsonOptions);

    public static string BuildBugRootCauseLocatedPayload(
        string projectId,
        string runId,
        string rootCauseLayer,
        string? revisionType,
        IrDiffResult diff)
        => JsonSerializer.Serialize(new
        {
            projectId,
            runId,
            rootCauseLayer,
            revisionType,
            fromSequence = diff.FromSequence,
            toSequence = diff.ToSequence,
            changedCount = diff.Changed.Count,
            invalidatedCount = diff.Invalidated.Count,
        }, JsonOptions);

    public static string BuildAffectedFragmentsMarkedPayload(string projectId, string runId, IrDiffResult diff)
        => JsonSerializer.Serialize(new
        {
            projectId,
            runId,
            added = diff.Added,
            changed = diff.Changed,
            invalidated = diff.Invalidated,
            markedAt = DateTime.UtcNow.ToString("O"),
        }, JsonOptions);

    public static string BuildBugFixedPayload(string projectId, string runId, IrDiffResult diff)
        => JsonSerializer.Serialize(new
        {
            projectId,
            runId,
            affectedFragmentCount = diff.Changed.Count + diff.Invalidated.Count + diff.Added.Count,
            fixedAt = DateTime.UtcNow.ToString("O"),
        }, JsonOptions);
}
