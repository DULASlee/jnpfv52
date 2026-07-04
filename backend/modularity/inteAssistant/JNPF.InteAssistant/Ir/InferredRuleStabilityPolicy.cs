using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using SqlSugar;

namespace JNPF.InteAssistant.Ir;

public interface IInferredRuleStabilityPolicy
{
    Task<bool> CanStabilizeAsync(
        AiIrFragmentSnapshotEntity snapshot,
        string projectId,
        string tenantId,
        CancellationToken ct = default);

    Task<int> CountUnacknowledgedInferredAsync(
        AiIrFragmentSnapshotEntity snapshot,
        string projectId,
        string tenantId,
        CancellationToken ct = default);
}

/// <summary>
/// inferred 规则 soft-block stable（P2.5-B03）。
/// </summary>
public sealed class InferredRuleStabilityPolicy : IInferredRuleStabilityPolicy, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly ISqlSugarClient _db;

    public InferredRuleStabilityPolicy(ISqlSugarClient db) => _db = db;

    public async Task<bool> CanStabilizeAsync(
        AiIrFragmentSnapshotEntity snapshot,
        string projectId,
        string tenantId,
        CancellationToken ct = default)
    {
        var inferredCount = CountInferredRules(snapshot.IrContent);
        if (inferredCount == 0)
            return true;

        var acked = await HasAcknowledgementAsync(snapshot.FragmentId, projectId, tenantId, ct);
        return acked;
    }

    public async Task<int> CountUnacknowledgedInferredAsync(
        AiIrFragmentSnapshotEntity snapshot,
        string projectId,
        string tenantId,
        CancellationToken ct = default)
    {
        var inferredCount = CountInferredRules(snapshot.IrContent);
        if (inferredCount == 0)
            return 0;

        var acked = await HasAcknowledgementAsync(snapshot.FragmentId, projectId, tenantId, ct);
        return acked ? 0 : inferredCount;
    }

    private async Task<bool> HasAcknowledgementAsync(
        string fragmentId, string projectId, string tenantId, CancellationToken ct)
    {
        return await _db.Queryable<AiIrEventEntity>()
            .AnyAsync(x =>
                x.ProjectId == projectId
                && x.TenantId == tenantId
                && x.FragmentId == fragmentId
                && x.EventType == IrEventTypes.InferredRulesAcknowledged, ct);
    }

    private static int CountInferredRules(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return 0;

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (!doc.RootElement.TryGetProperty("businessRules", out var rules)
                && !doc.RootElement.TryGetProperty("BusinessRules", out rules))
            {
                return 0;
            }

            var count = 0;
            foreach (var rule in rules.EnumerateArray())
            {
                var source = rule.TryGetProperty("source", out var s1) ? s1.GetString()
                    : rule.TryGetProperty("Source", out var s2) ? s2.GetString() : null;
                if (string.Equals(source, "inferred", StringComparison.OrdinalIgnoreCase))
                    count++;
            }

            return count;
        }
        catch
        {
            return 0;
        }
    }
}
