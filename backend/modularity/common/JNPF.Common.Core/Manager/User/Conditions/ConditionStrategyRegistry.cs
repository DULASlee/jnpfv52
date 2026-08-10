namespace JNPF.Common.Core.Manager.User.Conditions;

/// <summary>
/// Decision table for GetConditionAsync field-value tokens.
/// </summary>
public static class ConditionStrategyRegistry
{
    public const string UserId = "@userId";
    public const string UserAndSubordinates = "@userAraSubordinates";
    public const string OrganizeId = "@organizeId";
    public const string OrganizationAndSub = "@organizationAndSuborganization";
    public const string BranchManageOrganize = "@branchManageOrganize";
    public const string BranchManageOrganizeAndSub = "@branchManageOrganizeAndSub";

    private static readonly Dictionary<string, IConditionStrategy> Strategies =
        new(StringComparer.Ordinal)
        {
            [UserId] = new TokenConditionStrategy(UserId),
            [UserAndSubordinates] = new TokenConditionStrategy(UserAndSubordinates),
            [OrganizeId] = new TokenConditionStrategy(OrganizeId),
            [OrganizationAndSub] = new TokenConditionStrategy(OrganizationAndSub),
            [BranchManageOrganize] = new TokenConditionStrategy(BranchManageOrganize),
            [BranchManageOrganizeAndSub] = new TokenConditionStrategy(BranchManageOrganizeAndSub),
        };

    public static bool TryGet(string itemType, out IConditionStrategy strategy)
        => Strategies.TryGetValue(itemType, out strategy!);
}
