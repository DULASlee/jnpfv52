using JNPF.Common.Core.Manager.User.Conditions;
using JNPF.Common.Enums;
using SqlSugar;
using Xunit;

namespace JNPF.Tests.Common;

/// <summary>
/// W1 characterization: 5 data-permission scenarios + WHERE snapshot invariants.
/// </summary>
public class UserManagerDataPermissionTests
{
    private const string PrimaryKey = "f_id";

    [Fact]
    public void Scenario_Admin_FullPermission_EmptyWhere()
    {
        var models = DataPermissionShortCircuits.Admin();
        Assert.Empty(models);
        Assert.Equal(string.Empty, DataPermissionWhereSnapshot.FromModels(models));
    }

    [Fact]
    public void Scenario_AllData_AllowAll_SnapshotStable()
    {
        var models = DataPermissionShortCircuits.AllowAll(PrimaryKey);
        var snap = DataPermissionWhereSnapshot.FromModels(models);
        Assert.Contains("f_id|", snap);
        Assert.Contains("|0", snap);
        var leaf = Assert.IsType<ConditionalCollections>(models[0]);
        var cm = Assert.IsType<ConditionalModel>(leaf.ConditionalList[0].Value);
        Assert.Equal(PrimaryKey, cm.FieldName);
        Assert.Equal(ConditionalType.NoEqual, cm.ConditionalType);
        Assert.Equal("0", cm.FieldValue);
        Assert.Equal(snap, DataPermissionWhereSnapshot.FromModels(models));
    }

    [Fact]
    public void Scenario_NoAuthorize_DenyAll_EqualZero()
    {
        var models = DataPermissionShortCircuits.DenyAll(PrimaryKey);
        var leaf = Assert.IsType<ConditionalCollections>(models[0]);
        var cm = Assert.IsType<ConditionalModel>(leaf.ConditionalList[0].Value);
        Assert.Equal(PrimaryKey, cm.FieldName);
        Assert.Equal(ConditionalType.Equal, cm.ConditionalType);
        Assert.Equal("0", cm.FieldValue);
    }

    [Fact]
    public void Scenario_SelfOnly_UserIdStrategy_Clauses()
    {
        var list = new List<object>();
        Assert.True(ConditionStrategyRegistry.TryGet(ConditionStrategyRegistry.UserId, out var strategy));
        var ctx = new ConditionStrategyContext
        {
            ItemField = "f_creator_user_id",
            ItemMethod = QueryType.Equal,
            ConditionalType = ConditionalType.Like,
            Logic = "and",
            IsCurrentRole = true,
            Ids = new[] { "user-001" },
        };
        strategy.Append(list, ctx);
        Assert.Single(list);
        var json = list.ToJsonForAssert();
        Assert.Contains("user-001", json);
        Assert.Contains("f_creator_user_id", json);
    }

    [Fact]
    public void Scenario_OrganizeOnly_OrganizeIdStrategy()
    {
        var list = new List<object>();
        Assert.True(ConditionStrategyRegistry.TryGet(ConditionStrategyRegistry.OrganizeId, out var strategy));
        var ctx = new ConditionStrategyContext
        {
            ItemField = "f_organize_id",
            ItemMethod = QueryType.Equal,
            ConditionalType = ConditionalType.Like,
            Logic = "and",
            IsCurrentRole = true,
            Ids = new[] { "org-100" },
        };
        strategy.Append(list, ctx);
        Assert.Single(list);
        Assert.Contains("org-100", list.ToJsonForAssert());
    }

    [Fact]
    public void Scenario_OrganizeAndSub_MultiId_OrChain()
    {
        var list = new List<object>();
        Assert.True(ConditionStrategyRegistry.TryGet(ConditionStrategyRegistry.OrganizationAndSub, out var strategy));
        var ctx = new ConditionStrategyContext
        {
            ItemField = "f_organize_id",
            ItemMethod = QueryType.Equal,
            ConditionalType = ConditionalType.Like,
            Logic = "and",
            IsCurrentRole = true,
            Ids = new[] { "org-1", "org-1-a", "org-1-b" },
        };
        strategy.Append(list, ctx);
        Assert.Equal(3, list.Count);
        var json = list.ToJsonForAssert();
        Assert.Contains("org-1", json);
        Assert.Contains("org-1-a", json);
        Assert.Contains("org-1-b", json);
    }

    [Fact]
    public void AllowAll_ToSqlWhere_ContainsNoEqualInvariant()
    {
        var models = DataPermissionShortCircuits.AllowAll(PrimaryKey);
        var where = DataPermissionWhereSnapshot.ToSqlWhere(models);
        Assert.Contains("f_id", where, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0", where);
        // Snapshot must be stable across calls
        Assert.Equal(where, DataPermissionWhereSnapshot.ToSqlWhere(models));
    }

    [Fact]
    public void DenyAll_ToSqlWhere_ContainsEqualInvariant()
    {
        var models = DataPermissionShortCircuits.DenyAll(PrimaryKey);
        var where = DataPermissionWhereSnapshot.ToSqlWhere(models);
        Assert.Contains("f_id", where, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0", where);
        Assert.Equal(where, DataPermissionWhereSnapshot.ToSqlWhere(models));
    }

}

internal static class JsonAssertExtensions
{
    public static string ToJsonForAssert(this List<object> list)
        => Newtonsoft.Json.JsonConvert.SerializeObject(list);
}
