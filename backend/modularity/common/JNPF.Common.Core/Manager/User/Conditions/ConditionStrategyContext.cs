using JNPF.Common.Enums;
using SqlSugar;

namespace JNPF.Common.Core.Manager.User.Conditions;

/// <summary>
/// Runtime inputs for field-value condition strategies (@userId / @organizeId / …).
/// </summary>
public sealed class ConditionStrategyContext
{
    public string ItemField { get; init; } = string.Empty;

    public QueryType ItemMethod { get; init; }

    public ConditionalType ConditionalType { get; init; }

    /// <summary>Group logic: and / or.</summary>
    public string Logic { get; init; } = "and";

    public bool IsCurrentRole { get; set; }

    /// <summary>Resolved id list for this token (single or many).</summary>
    public IReadOnlyList<string> Ids { get; init; } = Array.Empty<string>();
}
