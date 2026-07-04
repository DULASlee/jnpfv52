namespace JNPF.InteAssistant.Ir;

/// <summary>
/// EventSpecRevised 受影响 SA 步骤判定表（P2-B10 / 文档 9 §4.8）
/// </summary>
public static class EventSpecRevisionPlanner
{
    public const string FieldNameOrDescription = "fieldNameOrDescription";
    public const string FieldTypeOrConstraint = "fieldTypeOrConstraint";
    public const string StateMachine = "stateMachine";
    public const string BusinessProcess = "businessProcess";
    public const string EntityRelation = "entityRelation";
    public const string RolePermission = "rolePermission";

    private static readonly Dictionary<string, string[]> AffectedSteps = new(StringComparer.OrdinalIgnoreCase)
    {
        [FieldNameOrDescription] = new[] { "CommandQuery" },
        [FieldTypeOrConstraint] = new[] { "CommandQuery", "DataModel" },
        [StateMachine] = new[] { "UISpec", "WorkflowSpec" },
        [BusinessProcess] = new[] { "EventCatalog", "WorkflowSpec", "IntegrationPoints" },
        [EntityRelation] = new[] { "DataModel", "UISpec" },
        [RolePermission] = new[] { "DomainModel", "UISpec" },
    };

    public static IReadOnlyList<string> GetAffectedSteps(string revisionType)
    {
        if (string.IsNullOrWhiteSpace(revisionType))
            return Array.Empty<string>();

        return AffectedSteps.TryGetValue(revisionType.Trim(), out var steps)
            ? steps
            : Array.Empty<string>();
    }

    /// <summary>
    /// 从已完成步骤中移除受影响步骤，保留未受影响步骤。
    /// </summary>
    public static List<string> TrimCompletedSteps(IEnumerable<string> completed, IEnumerable<string> affected)
    {
        var affectedSet = affected.ToHashSet(StringComparer.Ordinal);
        return completed.Where(s => !affectedSet.Contains(s)).ToList();
    }

    public static bool IsKnownRevisionType(string revisionType)
        => !string.IsNullOrWhiteSpace(revisionType) && AffectedSteps.ContainsKey(revisionType.Trim());
}
