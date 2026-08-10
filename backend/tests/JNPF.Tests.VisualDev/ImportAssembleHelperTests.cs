using JNPF.Common.Const;
using JNPF.Engine.Entity.Model;
using JNPF.VisualDev.Engine.Import;
using Xunit;

namespace JNPF.Tests.VisualDev;

/// <summary>
/// Characterization tests for shared ImportDataAssemble extract helpers (W3).
/// </summary>
public class ImportAssembleHelperTests
{
    private static FieldsModel SelectField(string label = "选项", bool multiple = false, string jnpfKey = JnpfKeyConst.SELECT)
        => new()
        {
            __vModel__ = "f_select",
            multiple = multiple,
            selectType = "all",
            __config__ = new ConfigModel { label = label, jnpfKey = jnpfKey },
        };

    private static List<Dictionary<string, string>> OptionCache(params (string key, string label)[] pairs)
        => pairs.Select(p => new Dictionary<string, string> { [p.key] = p.label }).ToList();

    [Fact]
    public void AppendMismatch_AggregatesWithComma()
    {
        var row = new Dictionary<string, object>();
        ImportAssembleErrors.AppendMismatch(row, "A");
        ImportAssembleErrors.AppendMismatch(row, "B");
        Assert.Equal("A: 值无法匹配,B: 值无法匹配", row[ImportAssembleErrors.ErrorKey]);
    }

    [Fact]
    public void Resolve_PrefersJnpfKeyThenVModel()
    {
        var v = SelectField();
        v.__config__.jnpfKey = JnpfKeyConst.SELECT;
        v.__vModel__ = "myField";
        var cache = new Dictionary<string, List<Dictionary<string, string>>>
        {
            [JnpfKeyConst.SELECT] = OptionCache(("k1", "一")),
            ["myField"] = OptionCache(("k2", "二")),
        };
        var byKey = ImportFieldCacheLookup.Resolve(v, cache);
        Assert.Single(byKey);
        Assert.Equal("一", byKey[0]["k1"]);

        cache.Remove(JnpfKeyConst.SELECT);
        var byVModel = ImportFieldCacheLookup.Resolve(v, cache);
        Assert.Equal("二", byVModel[0]["k2"]);
    }

    [Fact]
    public void MapSelectLike_MapsLabelToKey()
    {
        var v = SelectField();
        var row = new Dictionary<string, object>();
        ImportOptionValueMapper.MapSelectLike(
            v, "f_select", "一", OptionCache(("id1", "一")), row, clearWhenEmpty: false);
        Assert.Equal("id1", row["f_select"]);
    }

    [Fact]
    public void MapSelectLike_ClearWhenEmpty_OnlyForCodeGenSemantics()
    {
        var v = SelectField();
        var keep = new Dictionary<string, object> { ["f_select"] = "stale" };
        ImportOptionValueMapper.MapSelectLike(v, "f_select", null, OptionCache(("id1", "一")), keep, clearWhenEmpty: false);
        Assert.Equal("stale", keep["f_select"]);

        var clear = new Dictionary<string, object> { ["f_select"] = "stale" };
        ImportOptionValueMapper.MapSelectLike(v, "f_select", null, OptionCache(("id1", "一")), clear, clearWhenEmpty: true);
        Assert.Null(clear["f_select"]);
    }

    [Fact]
    public void MapOrgPathSelect_UsesLastPathSegment()
    {
        var v = SelectField(jnpfKey: JnpfKeyConst.DEPSELECT);
        var row = new Dictionary<string, object>();
        ImportOptionValueMapper.MapOrgPathSelect(
            v, "f_dep", "公司/研发部", OptionCache(("d1", "研发部")), row, clearWhenEmpty: false);
        Assert.Equal("d1", row["f_dep"]);
    }

    [Fact]
    public void MapOrgPathSelect_Mismatch_AppendsError()
    {
        var v = SelectField(label: "部门", jnpfKey: JnpfKeyConst.DEPSELECT);
        var row = new Dictionary<string, object>();
        ImportOptionValueMapper.MapOrgPathSelect(
            v, "f_dep", "未知", OptionCache(("d1", "研发部")), row, clearWhenEmpty: false);
        Assert.Equal("部门: 值无法匹配", row[ImportAssembleErrors.ErrorKey]);
    }

    [Fact]
    public void IsAllOrCustomSelectType_AcceptsEmptyAllCustom()
    {
        var v = SelectField();
        v.selectType = null;
        Assert.True(ImportOptionValueMapper.IsAllOrCustomSelectType(v));
        v.selectType = "custom";
        Assert.True(ImportOptionValueMapper.IsAllOrCustomSelectType(v));
        v.selectType = "other";
        Assert.False(ImportOptionValueMapper.IsAllOrCustomSelectType(v));
    }

    [Fact]
    public void TryAdd_SkipsWhenKeyExists()
    {
        var res = new Dictionary<string, List<Dictionary<string, string>>>
        {
            ["f"] = OptionCache(("k", "v")),
        };
        Assert.False(ImportCacheBagHelpers.TryAdd(res, "f", OptionCache(("k2", "v2"))));
        Assert.Single(res["f"]);
    }

    [Fact]
    public void BuildSwitchPairs_ActiveInactive()
    {
        var pairs = ImportCacheBagHelpers.BuildSwitchPairs("开", "关");
        Assert.Equal(2, pairs.Count);
        Assert.Equal("开", pairs[0]["1"]);
        Assert.Equal("关", pairs[1]["0"]);
    }

    [Fact]
    public void BuildStaticOptionPairs_MapsProps()
    {
        var options = new List<Dictionary<string, object>>
        {
            new() { ["id"] = "a", ["fullName"] = "甲" },
        };
        var pairs = ImportCacheBagHelpers.BuildStaticOptionPairs(options, "id", "fullName");
        Assert.Equal("甲", pairs[0]["a"]);
    }

    [Fact]
    public void BuildDictionaryPairs_VisualDevExact_KeepsEmptyWhenPropsMiss()
    {
        var rows = new[] { ("id1", "e1", "名1") };
        var pairs = ImportCacheBagHelpers.BuildDictionaryPairs(
            rows, "other", ImportCacheBagHelpers.DictionaryPropsMode.VisualDevExact);
        Assert.Single(pairs);
        Assert.Empty(pairs[0]);
    }

    [Fact]
    public void BuildDictionaryPairs_EncodeCaseInsensitive()
    {
        var rows = new[] { ("id1", "e1", "名1") };
        var byEncode = ImportCacheBagHelpers.BuildDictionaryPairs(
            rows, "Encode", ImportCacheBagHelpers.DictionaryPropsMode.EncodeCaseInsensitive);
        Assert.Equal("名1", byEncode[0]["e1"]);
        var byId = ImportCacheBagHelpers.BuildDictionaryPairs(
            rows, "id", ImportCacheBagHelpers.DictionaryPropsMode.EncodeCaseInsensitive);
        Assert.Equal("名1", byId[0]["id1"]);
    }

    [Fact]
    public void BuildRelationFormRedisKey_ConcatenatesParts()
    {
        var key = ImportCacheBagHelpers.BuildRelationFormRedisKey("t1", "relationForm", "rk");
        Assert.Equal(CommonConst.VISUALDEV + "t1_relationForm_rk", key);
    }

    [Fact]
    public void BuildRelationFormRedisKey_NullRenderKey_ConcatEmpty()
    {
        var key = ImportCacheBagHelpers.BuildRelationFormRedisKey("t1", "relationForm", null);
        Assert.Equal(CommonConst.VISUALDEV + "t1_relationForm_", key);
    }

    [Fact]
    public void MapPopup_MultiColumnRow_UsesRelationFieldToPropsValue()
    {
        var v = SelectField(jnpfKey: JnpfKeyConst.POPUPTABLESELECT);
        v.propsValue = "id";
        v.relationField = "name";
        var cache = new List<Dictionary<string, string>>
        {
            new() { ["id"] = "pk1", ["name"] = "显示名", ["code"] = "C1" },
        };
        var row = new Dictionary<string, object>();
        ImportPopupValueMapper.Map(v, "f_pop", "显示名", cache, row, clearWhenEmpty: false);
        Assert.Equal("pk1", row["f_pop"]);
    }

    [Fact]
    public void MapPopup_Mismatch_AppendsError_DoesNotWriteColumnName()
    {
        var v = SelectField(label: "弹窗", jnpfKey: JnpfKeyConst.POPUPSELECT);
        v.propsValue = "id";
        v.relationField = "name";
        var cache = new List<Dictionary<string, string>>
        {
            new() { ["id"] = "pk1", ["name"] = "显示名" },
        };
        var row = new Dictionary<string, object>();
        ImportPopupValueMapper.Map(v, "f_pop", "不存在", cache, row, clearWhenEmpty: false);
        Assert.False(row.ContainsKey("f_pop"));
        Assert.Equal("弹窗: 值无法匹配", row[ImportAssembleErrors.ErrorKey]);
    }

    [Fact]
    public void ResolveDisplayField_FallsBackToColumnOptions()
    {
        var v = SelectField(jnpfKey: JnpfKeyConst.POPUPSELECT);
        v.relationField = null;
        v.columnOptions = new List<ColumnOptionsModel> { new() { value = "name" } };
        var field = ImportPopupValueMapper.ResolveDisplayField(v, new List<Dictionary<string, string>>());
        Assert.Equal("name", field);
    }

    [Fact]
    public void MapPopup_Multiple_CommaSplit()
    {
        var v = SelectField(jnpfKey: JnpfKeyConst.POPUPTABLESELECT);
        v.multiple = true;
        v.propsValue = "id";
        v.relationField = "name";
        var cache = new List<Dictionary<string, string>>
        {
            new() { ["id"] = "1", ["name"] = "甲" },
            new() { ["id"] = "2", ["name"] = "乙" },
        };
        var row = new Dictionary<string, object>();
        ImportPopupValueMapper.Map(v, "f_pop", "甲,乙", cache, row, clearWhenEmpty: false);
        var list = Assert.IsType<List<object>>(row["f_pop"]);
        Assert.Equal(new object[] { "1", "2" }, list);
    }
}
