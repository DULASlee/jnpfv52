namespace JNPF.Common.Core.Manager.User.Conditions;

/// <summary>Generic token strategy — Ids are supplied by the caller via context.</summary>
public sealed class TokenConditionStrategy : IConditionStrategy
{
    public TokenConditionStrategy(string itemType) => ItemType = itemType;

    public string ItemType { get; }

    public void Append(List<object> conditionalList, ConditionStrategyContext context)
        => ConditionClauseAppender.AppendIds(conditionalList, context);
}
