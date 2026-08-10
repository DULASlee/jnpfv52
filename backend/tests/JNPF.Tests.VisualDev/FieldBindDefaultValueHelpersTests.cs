using JNPF.Common.Const;
using JNPF.Common.Security;
using JNPF.Systems.Entitys.Permission;
using JNPF.VisualDev.Query;
using Xunit;

namespace JNPF.Tests.VisualDev;

/// <summary>
/// Characterization: FieldBindDefaultValue defaultCurrent binding (W2).
/// </summary>
public class FieldBindDefaultValueHelpersTests
{
    private static Dictionary<string, object> Field(
        string jnpfKey,
        bool defaultCurrent = true,
        bool multiple = false,
        string? selectType = null)
    {
        var item = new Dictionary<string, object>
        {
            ["__config__"] = new Dictionary<string, object>
            {
                ["jnpfKey"] = jnpfKey,
                ["defaultCurrent"] = defaultCurrent,
            },
            ["multiple"] = multiple,
        };
        if (selectType != null)
            item["selectType"] = selectType;
        return item;
    }

    private static object? GetDefault(Dictionary<string, object> item)
        => item["__config__"].ToObject<Dictionary<string, object>>()["defaultValue"];

    [Fact]
    public void UserSelect_Single_SetsUserId()
    {
        var list = new List<Dictionary<string, object>> { Field(JnpfKeyConst.USERSELECT) };
        FieldBindDefaultValueHelpers.Bind(
            ref list, "u1", "d1", new List<string>(), new List<string>(), new List<string>(),
            new List<UserRelationEntity>(), preferredPositionId: null);
        Assert.Equal("u1", GetDefault(list[0]));
    }

    [Fact]
    public void UsersSelect_Multiple_UsesUserSuffix()
    {
        var list = new List<Dictionary<string, object>> { Field(JnpfKeyConst.USERSSELECT, multiple: true) };
        FieldBindDefaultValueHelpers.Bind(
            ref list, "u1", "d1", new List<string>(), new List<string>(), new List<string>(),
            new List<UserRelationEntity>(), null);
        // __config__ ToObject round-trip may yield JArray
        var val = GetDefault(list[0]).ToObject<List<string>>();
        Assert.Equal(new[] { "u1--user" }, val);
    }

    [Fact]
    public void DepSelect_Custom_ClearsWhenNotInAbleList()
    {
        var item = Field(JnpfKeyConst.DEPSELECT, selectType: "custom");
        item["ableDepIds"] = new List<string> { "other" };
        var list = new List<Dictionary<string, object>> { item };
        FieldBindDefaultValueHelpers.Bind(
            ref list, "u1", "d1", new List<string>(), new List<string>(), new List<string>(),
            new List<UserRelationEntity>(), null);
        Assert.Null(GetDefault(list[0]));
    }

    [Fact]
    public void PosSelect_PrefersPreferredPositionWhenInList()
    {
        var list = new List<Dictionary<string, object>> { Field(JnpfKeyConst.POSSELECT) };
        FieldBindDefaultValueHelpers.Bind(
            ref list, "u1", "d1", new List<string> { "p1", "p2" }, new List<string>(), new List<string>(),
            new List<UserRelationEntity>(), preferredPositionId: "p2");
        Assert.Equal("p2", GetDefault(list[0]));
    }

    [Fact]
    public void PosSelect_FallsBackToFirstWhenPreferredMissing()
    {
        var list = new List<Dictionary<string, object>> { Field(JnpfKeyConst.POSSELECT) };
        FieldBindDefaultValueHelpers.Bind(
            ref list, "u1", "d1", new List<string> { "p1", "p2" }, new List<string>(), new List<string>(),
            new List<UserRelationEntity>(), preferredPositionId: "p9");
        Assert.Equal("p1", GetDefault(list[0]));
    }

    [Fact]
    public void UserSelect_Custom_RequiresRelationHit()
    {
        var item = Field(JnpfKeyConst.USERSELECT, selectType: "custom");
        item["ableDepIds"] = new List<string> { "d1" };
        item["ablePosIds"] = new List<string>();
        item["ableUserIds"] = new List<string>();
        item["ableRoleIds"] = new List<string>();
        item["ableGroupIds"] = new List<string>();
        var list = new List<Dictionary<string, object>> { item };
        var relations = new List<UserRelationEntity>
        {
            new() { UserId = "u1", ObjectId = "d1" },
        };
        FieldBindDefaultValueHelpers.Bind(
            ref list, "u1", "d1", new List<string>(), new List<string>(), new List<string>(),
            relations, null);
        Assert.Equal("u1", GetDefault(list[0]));

        list = new List<Dictionary<string, object>> { item };
        FieldBindDefaultValueHelpers.Bind(
            ref list, "u1", "d1", new List<string>(), new List<string>(), new List<string>(),
            new List<UserRelationEntity>(), null);
        Assert.Null(GetDefault(list[0]));
    }

    [Fact]
    public void TableChild_RecursionUsesChildMultiple_NotParent()
    {
        // Legacy: TABLE branch writes using parent.multiple, then layout recursion re-binds
        // children with the child's own multiple — end state follows the child flag.
        var child = Field(JnpfKeyConst.USERSELECT, multiple: false);
        var table = new Dictionary<string, object>
        {
            ["__config__"] = new Dictionary<string, object>
            {
                ["jnpfKey"] = JnpfKeyConst.TABLE,
                ["defaultCurrent"] = false,
                ["children"] = new List<Dictionary<string, object>> { child },
            },
            ["multiple"] = true,
        };
        var list = new List<Dictionary<string, object>> { table };
        FieldBindDefaultValueHelpers.Bind(
            ref list, "u1", "d1", new List<string>(), new List<string>(), new List<string>(),
            new List<UserRelationEntity>(), null);
        var cfg = list[0]["__config__"].ToObject<Dictionary<string, object>>();
        var children = cfg["children"].ToObject<List<Dictionary<string, object>>>();
        var childCfg = children[0]["__config__"].ToObject<Dictionary<string, object>>();
        Assert.Equal("u1", childCfg["defaultValue"]);
    }

    [Fact]
    public void SkipsWhenDefaultCurrentFalse()
    {
        var list = new List<Dictionary<string, object>> { Field(JnpfKeyConst.USERSELECT, defaultCurrent: false) };
        FieldBindDefaultValueHelpers.Bind(
            ref list, "u1", "d1", new List<string>(), new List<string>(), new List<string>(),
            new List<UserRelationEntity>(), null);
        var cfg = list[0]["__config__"].ToObject<Dictionary<string, object>>();
        Assert.False(cfg.ContainsKey("defaultValue"));
    }
}
