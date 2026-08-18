namespace JNPF.Common.Core.Manager.User.Conditions;

/// <summary>
/// Decision-table entry for data-permission field tokens (e.g. @userId).
/// ItemType holds the token string for registry lookup.
/// </summary>
public interface IConditionStrategy
{
    /// <summary>Token such as @userId / @organizeId / @organizationAndSuborganization.</summary>
    string ItemType { get; }

    /// <summary>
    /// Append anonymous clause objects (JsonToConditionalModels shape) into conditionalList.
    /// </summary>
    void Append(List<object> conditionalList, ConditionStrategyContext context);
}
