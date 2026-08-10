using JNPF.Common.Const;
using JNPF.Engine.Entity.Model;
using JNPF.VisualDev.Engine.Import;
using Xunit;

namespace JNPF.Tests.VisualDev;

public class ImportPathSelectMapperTests
{
    private static List<Dictionary<string, string>> Cache(params (string key, string label)[] pairs)
        => pairs.Select(p => new Dictionary<string, string> { [p.key] = p.label }).ToList();

    private static FieldsModel Address(int level = 2, bool multiple = false)
        => new()
        {
            __vModel__ = "f_addr",
            level = level,
            multiple = multiple,
            __config__ = new ConfigModel { label = "地址", jnpfKey = JnpfKeyConst.ADDRESS },
        };

    [Fact]
    public void MapComOrAddress_MapsPathToKeySegments()
    {
        // ADDRESS gate: slash count must equal level ("省/市" → 1 slash → level 1).
        var v = Address(level: 1);
        var row = new Dictionary<string, object>();
        ImportPathSelectMapper.MapComOrAddress(
            v, "f_addr", "省/市", Cache(("k1,k2", "省/市")), row,
            clearWhenEmpty: false, allowMunicipalDistrictShortcut: false);
        var list = Assert.IsType<List<string>>(row["f_addr"]);
        Assert.Equal(new[] { "k1", "k2" }, list);
    }

    [Fact]
    public void MapComOrAddress_MunicipalShortcut_OnlyWhenAllowed()
    {
        var v = Address(level: 3);
        var cache = Cache(("a,b,c", "市/市辖区/区"));
        var denied = new Dictionary<string, object>();
        ImportPathSelectMapper.MapComOrAddress(
            v, "f_addr", "市/市辖区/区", cache, denied,
            clearWhenEmpty: false, allowMunicipalDistrictShortcut: false);
        // slash count 2 != level 3 → mismatch when shortcut off
        Assert.Equal("地址: 值无法匹配", denied[ImportAssembleErrors.ErrorKey]);

        var allowed = new Dictionary<string, object>();
        ImportPathSelectMapper.MapComOrAddress(
            v, "f_addr", "市/市辖区/区", cache, allowed,
            clearWhenEmpty: false, allowMunicipalDistrictShortcut: true);
        Assert.False(allowed.ContainsKey(ImportAssembleErrors.ErrorKey));
        Assert.True(allowed.ContainsKey("f_addr"));
    }

    [Fact]
    public void MapComOrAddress_CodeGenEmpty_Clears()
    {
        var v = Address();
        var row = new Dictionary<string, object> { ["f_addr"] = "x" };
        ImportPathSelectMapper.MapComOrAddress(
            v, "f_addr", null, Cache(("k", "省/市")), row,
            clearWhenEmpty: true, allowMunicipalDistrictShortcut: true);
        Assert.Null(row["f_addr"]);
    }

    [Fact]
    public void MapUsersSelect_FallsBackToLastPathSegment()
    {
        var v = new FieldsModel
        {
            __vModel__ = "f_users",
            selectType = "all",
            multiple = false,
            __config__ = new ConfigModel { label = "用户", jnpfKey = JnpfKeyConst.USERSSELECT },
        };
        var row = new Dictionary<string, object>();
        ImportPathSelectMapper.MapUsersSelect(
            v, "f_users", "公司/张三", Cache(("u1", "张三")), row);
        Assert.Equal("u1", row["f_users"]);
    }

    [Fact]
    public void MapCascader_SplitsBySeparator()
    {
        var v = new FieldsModel
        {
            __vModel__ = "f_cas",
            separator = "/",
            multiple = false,
            __config__ = new ConfigModel { label = "级联", jnpfKey = JnpfKeyConst.CASCADER },
        };
        var row = new Dictionary<string, object>();
        ImportPathSelectMapper.MapCascader(
            v, "f_cas", "一/二", Cache(("1", "一"), ("2", "二")), row, clearWhenEmpty: false);
        var list = Assert.IsType<List<object>>(row["f_cas"]);
        Assert.Equal(new object[] { "1", "2" }, list);
    }

    [Fact]
    public void BuildLabelToKeyIndex_DuplicateLabel_FirstRowWins()
    {
        var cache = Cache(("first", "同名"), ("second", "同名"));
        var index = ImportPathSelectMapper.BuildLabelToKeyIndex(cache);
        Assert.Equal("first", index["同名"]);
    }

    [Fact]
    public void MapSelectLike_TreeSelect_ClearWhenEmpty_CodeGenOnly()
    {
        var v = new FieldsModel
        {
            __vModel__ = "f_tree",
            multiple = false,
            __config__ = new ConfigModel { label = "树", jnpfKey = JnpfKeyConst.TREESELECT },
        };
        var keep = new Dictionary<string, object> { ["f_tree"] = "stale" };
        ImportOptionValueMapper.MapSelectLike(v, "f_tree", null, Cache(("k", "节点")), keep, clearWhenEmpty: false);
        Assert.Equal("stale", keep["f_tree"]);

        var clear = new Dictionary<string, object> { ["f_tree"] = "stale" };
        ImportOptionValueMapper.MapSelectLike(v, "f_tree", null, Cache(("k", "节点")), clear, clearWhenEmpty: true);
        Assert.Null(clear["f_tree"]);
    }
}
