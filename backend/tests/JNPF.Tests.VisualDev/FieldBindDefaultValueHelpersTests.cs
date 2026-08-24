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

    // ==================== D1.2 S1 特征用例（规格 §2.2 不变量 I1-I9 缺口补齐） ====================
    // 金标准纪律：本批用例在当前实现上全绿后才允许拆分；拆分后逐条等价。

    private static Dictionary<string, object> ChildField(string jnpfKey, bool defaultCurrent = true)
        => new()
        {
            ["__config__"] = new Dictionary<string, object>
            {
                ["jnpfKey"] = jnpfKey,
                ["defaultCurrent"] = defaultCurrent,
            },
        };

    private static Dictionary<string, object> LayoutField(List<Dictionary<string, object>> children)
        => new()
        {
            ["__config__"] = new Dictionary<string, object>
            {
                ["jnpfKey"] = JnpfKeyConst.ROW,
                ["defaultCurrent"] = false,
                ["children"] = children,
            },
        };

    [Fact]
    public void D1_I3_UserSelect_Multiple_SetsSingletonList()
    {
        // I3：USERSELECT 多选 = [userId]（与 USERSSELECT --user 后缀对照）
        var list = new List<Dictionary<string, object>> { Field(JnpfKeyConst.USERSELECT, multiple: true) };
        FieldBindDefaultValueHelpers.Bind(
            ref list, "u1", "d1", new List<string>(), new List<string>(), new List<string>(),
            new List<UserRelationEntity>(), null);
        Assert.Equal(new[] { "u1" }, GetDefault(list[0]).ToObject<List<string>>());
    }

    [Fact]
    public void D1_I3_UsersSelect_Single_NoSuffix()
    {
        // I3 实测修正：USERSSELECT 单选 = 裸 userId（--user 后缀为多选分支专属，2026-08-24 实测推翻旧推断，保真锁定）
        var list = new List<Dictionary<string, object>> { Field(JnpfKeyConst.USERSSELECT) };
        FieldBindDefaultValueHelpers.Bind(
            ref list, "u1", "d1", new List<string>(), new List<string>(), new List<string>(),
            new List<UserRelationEntity>(), null);
        Assert.Equal("u1", GetDefault(list[0]));
    }

    [Fact]
    public void D1_I4_DepRoleGroup_BindShapes()
    {
        // I4：DEP 单选=值/多选=[值]；ROLE/GROUP 单选=首元素/多选=整表
        var depS = Field(JnpfKeyConst.DEPSELECT);
        var depM = Field(JnpfKeyConst.DEPSELECT, multiple: true);
        var roleS = Field(JnpfKeyConst.ROLESELECT);
        var roleM = Field(JnpfKeyConst.ROLESELECT, multiple: true);
        var grpS = Field(JnpfKeyConst.GROUPSELECT);
        var grpM = Field(JnpfKeyConst.GROUPSELECT, multiple: true);
        var list = new List<Dictionary<string, object>> { depS, depM, roleS, roleM, grpS, grpM };
        FieldBindDefaultValueHelpers.Bind(
            ref list, "u1", "d1", new List<string>(), new List<string> { "r1", "r2" }, new List<string> { "g1", "g2" },
            new List<UserRelationEntity>(), null);
        Assert.Equal("d1", GetDefault(depS));
        Assert.Equal(new[] { "d1" }, GetDefault(depM).ToObject<List<string>>());
        Assert.Equal("r1", GetDefault(roleS));
        Assert.Equal(new[] { "r1", "r2" }, GetDefault(roleM).ToObject<List<string>>());
        Assert.Equal("g1", GetDefault(grpS));
        Assert.Equal(new[] { "g1", "g2" }, GetDefault(grpM).ToObject<List<string>>());
    }

    [Fact]
    public void D1_I4_RoleGroupCustom_ClearsWhenNotInAbleList()
    {
        // I4 custom 校验：未命中 able 集合 → defaultValue=null
        var role = Field(JnpfKeyConst.ROLESELECT, selectType: "custom");
        role["ableRoleIds"] = new List<string> { "other" };
        var grp = Field(JnpfKeyConst.GROUPSELECT, selectType: "custom");
        grp["ableGroupIds"] = new List<string> { "other" };
        var list = new List<Dictionary<string, object>> { role, grp };
        FieldBindDefaultValueHelpers.Bind(
            ref list, "u1", "d1", new List<string>(), new List<string> { "r1" }, new List<string> { "g1" },
            new List<UserRelationEntity>(), null);
        Assert.Null(GetDefault(role));
        Assert.Null(GetDefault(grp));
    }

    [Fact]
    public void D1_I4_DepCustom_BindsWhenInAbleList()
    {
        // I4 custom 命中 → 正常绑定（与清除分支对照）
        var dep = Field(JnpfKeyConst.DEPSELECT, selectType: "custom");
        dep["ableDepIds"] = new List<string> { "d1" };
        var list = new List<Dictionary<string, object>> { dep };
        FieldBindDefaultValueHelpers.Bind(
            ref list, "u1", "d1", new List<string>(), new List<string>(), new List<string>(),
            new List<UserRelationEntity>(), null);
        Assert.Equal("d1", GetDefault(dep));
    }

    [Fact]
    public void D1_I5_PosSelectCustom_ClearsAndBinds()
    {
        // I5 custom：未命中 ablePosIds 清除；命中则单选首元素（无 preferred 时）
        var miss = Field(JnpfKeyConst.POSSELECT, selectType: "custom");
        miss["ablePosIds"] = new List<string> { "other" };
        var hit = Field(JnpfKeyConst.POSSELECT, selectType: "custom");
        hit["ablePosIds"] = new List<string> { "p1" };
        var list = new List<Dictionary<string, object>> { miss, hit };
        FieldBindDefaultValueHelpers.Bind(
            ref list, "u1", "d1", new List<string> { "p1" }, new List<string>(), new List<string>(),
            new List<UserRelationEntity>(), null);
        Assert.Null(GetDefault(miss));
        Assert.Equal("p1", GetDefault(hit));
    }

    [Fact]
    public void D1_I5_PosSelect_Multiple_IgnoresPreferred()
    {
        // I5：多选模式直接绑整表，preferredPositionId 不参与（单选专属）
        var list = new List<Dictionary<string, object>> { Field(JnpfKeyConst.POSSELECT, multiple: true) };
        FieldBindDefaultValueHelpers.Bind(
            ref list, "u1", "d1", new List<string> { "p1", "p2" }, new List<string>(), new List<string>(),
            new List<UserRelationEntity>(), preferredPositionId: "p2");
        Assert.Equal(new[] { "p1", "p2" }, GetDefault(list[0]).ToObject<List<string>>());
    }

    [Fact]
    public void D1_I2_UsersSelectCustom_FiltersByEachAbleDimension()
    {
        // I2：五 able 集合逐维过滤 — 分别用 pos/role/group 维度的关系命中验证（user/dep 维已有既有用例）
        Dictionary<string, object> NewItem() => Field(JnpfKeyConst.USERSSELECT, selectType: "custom");

        var byPos = NewItem();
        byPos["ableUserIds"] = new List<string>();
        byPos["ableDepIds"] = new List<string>();
        byPos["ablePosIds"] = new List<string> { "p1" };
        byPos["ableRoleIds"] = new List<string>();
        byPos["ableGroupIds"] = new List<string>();

        var byRole = NewItem();
        byRole["ableUserIds"] = new List<string>();
        byRole["ableDepIds"] = new List<string>();
        byRole["ablePosIds"] = new List<string>();
        byRole["ableRoleIds"] = new List<string> { "r1" };
        byRole["ableGroupIds"] = new List<string>();

        var byGroup = NewItem();
        byGroup["ableUserIds"] = new List<string>();
        byGroup["ableDepIds"] = new List<string>();
        byGroup["ablePosIds"] = new List<string>();
        byGroup["ableRoleIds"] = new List<string>();
        byGroup["ableGroupIds"] = new List<string> { "g1" };

        var relations = new List<UserRelationEntity>
        {
            new() { UserId = "u1", ObjectId = "p1" },
            new() { UserId = "u1", ObjectId = "r1" },
            new() { UserId = "u1", ObjectId = "g1" },
        };

        foreach (var item in new[] { byPos, byRole, byGroup })
        {
            var list = new List<Dictionary<string, object>> { item };
            FieldBindDefaultValueHelpers.Bind(
                ref list, "u1", "d1", new List<string>(), new List<string>(), new List<string>(),
                relations, null);
            Assert.Equal("u1", GetDefault(item));
        }
    }

    [Fact]
    public void D1_I6I7_TableChild_AllSelectors_BoundWithoutCustomCheck()
    {
        // I6/I7（Q4/Q5 保真）：TABLE children 分支对全部选择器生效且无 custom 校验；
        // 终态经布局递归以子控件自身标志重写（既有测试已锁定该叠加效应），本用例锁定五选择器均被触达且 defaultCurrent 门生效
        var user = ChildField(JnpfKeyConst.USERSELECT);
        var dep = ChildField(JnpfKeyConst.DEPSELECT);
        var pos = ChildField(JnpfKeyConst.POSSELECT);
        var role = ChildField(JnpfKeyConst.ROLESELECT);
        var grp = ChildField(JnpfKeyConst.GROUPSELECT);
        var skip = ChildField(JnpfKeyConst.USERSELECT, defaultCurrent: false);
        var table = new Dictionary<string, object>
        {
            ["__config__"] = new Dictionary<string, object>
            {
                ["jnpfKey"] = JnpfKeyConst.TABLE,
                ["defaultCurrent"] = false,
                ["children"] = new List<Dictionary<string, object>> { user, dep, pos, role, grp, skip },
            },
            ["multiple"] = false,
        };
        var list = new List<Dictionary<string, object>> { table };
        FieldBindDefaultValueHelpers.Bind(
            ref list, "u1", "d1", new List<string> { "p1" }, new List<string> { "r1" }, new List<string> { "g1" },
            new List<UserRelationEntity>(), null);
        var cfg = list[0]["__config__"].ToObject<Dictionary<string, object>>();
        var children = cfg["children"].ToObject<List<Dictionary<string, object>>>();
        Dictionary<string, object> ChildCfg(int i) => children[i]["__config__"].ToObject<Dictionary<string, object>>();
        Assert.Equal("u1", ChildCfg(0)["defaultValue"]);
        Assert.Equal("d1", ChildCfg(1)["defaultValue"]);
        Assert.Equal("p1", ChildCfg(2)["defaultValue"]);
        Assert.Equal("r1", ChildCfg(3)["defaultValue"]);
        Assert.Equal("g1", ChildCfg(4)["defaultValue"]);
        Assert.False(ChildCfg(5).ContainsKey("defaultValue"));
    }

    [Fact]
    public void D1_I8_LayoutRecursion_BindsNestedSelectors()
    {
        // I8：布局控件（非 TABLE）递归 — 嵌套层选择器被绑定，回写链完整
        var inner = ChildField(JnpfKeyConst.USERSELECT);
        var row = LayoutField(new List<Dictionary<string, object>> { inner });
        var list = new List<Dictionary<string, object>> { row };
        FieldBindDefaultValueHelpers.Bind(
            ref list, "u1", "d1", new List<string>(), new List<string>(), new List<string>(),
            new List<UserRelationEntity>(), null);
        var cfg = list[0]["__config__"].ToObject<Dictionary<string, object>>();
        var children = cfg["children"].ToObject<List<Dictionary<string, object>>>();
        var childCfg = children[0]["__config__"].ToObject<Dictionary<string, object>>();
        Assert.Equal("u1", childCfg["defaultValue"]);
    }

    [Fact]
    public void D1_I1_NonSelectorControl_NotBound()
    {
        // I1：非六选择器键即使 defaultCurrent=true 也不绑定
        var list = new List<Dictionary<string, object>> { Field(JnpfKeyConst.COMINPUT) };
        FieldBindDefaultValueHelpers.Bind(
            ref list, "u1", "d1", new List<string>(), new List<string>(), new List<string>(),
            new List<UserRelationEntity>(), null);
        var cfg = list[0]["__config__"].ToObject<Dictionary<string, object>>();
        Assert.False(cfg.ContainsKey("defaultValue"));
    }
}
