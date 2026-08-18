using JNPF.Common.Const;
using JNPF.Common.Security;
using JNPF.Engine.Entity.Model;
using JNPF.VisualDev.Transfer;
using Xunit;

namespace JNPF.Tests.VisualDev;

/// <summary>
/// Characterization: SaveDataToDataByFId in-memory map / child-table transfer.
/// </summary>
public class FlowFormDataMapperTests
{
    private static FieldsModel Field(string vModel, string jnpfKey = JnpfKeyConst.COMINPUT)
        => new()
        {
            __vModel__ = vModel,
            __config__ = new ConfigModel { jnpfKey = jnpfKey },
        };

    [Theory]
    [InlineData("leaveApply", "-")]
    [InlineData("salesOrder", "-")]
    [InlineData("crmOrder", "-")]
    [InlineData("normalForm", "tablefield")]
    public void ResolveChildTableSplitKey_SpecialForms(string enCode, string expected)
        => Assert.Equal(expected, FlowFormDataMapper.ResolveChildTableSplitKey(enCode, "other"));

    [Fact]
    public void CoerceSystemFormFieldsToComInput_SkipsModifyAndTable()
    {
        var fields = new List<FieldsModel>
        {
            Field("a", JnpfKeyConst.NUMINPUT),
            Field("m", JnpfKeyConst.MODIFYTIME),
            Field("t", JnpfKeyConst.TABLE),
        };
        FlowFormDataMapper.CoerceSystemFormFieldsToComInput(fields);
        Assert.Equal(JnpfKeyConst.COMINPUT, fields[0].__config__.jnpfKey);
        Assert.Equal(JnpfKeyConst.MODIFYTIME, fields[1].__config__.jnpfKey);
        Assert.Equal(JnpfKeyConst.TABLE, fields[2].__config__.jnpfKey);
    }

    [Fact]
    public void ApplyMapRules_MissingModel_SetsEmptyString()
    {
        var form = new Dictionary<string, object> { ["a"] = "keep" };
        var map = new List<Dictionary<string, string>> { new() { ["a"] = "b" } };
        var oldIdx = new Dictionary<string, FieldsModel>(); // missing
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { Field("b") });

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);
        Assert.Equal(string.Empty, form["a"]);
    }

    [Fact]
    public void ApplyMapRules_ChildToChild_CopiesByRowIndex()
    {
        var oldField = Field("tableField111-f_name");
        var newField = Field("tableField222-f_title");
        var form = new Dictionary<string, object>
        {
            ["tableField111"] = new List<Dictionary<string, object>>
            {
                new() { ["f_name"] = "r1" },
                new() { ["f_name"] = "r2" },
            },
        };
        var map = new List<Dictionary<string, string>>
        {
            new() { ["tableField111-f_name"] = "tableField222-f_title" },
        };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { oldField });
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { newField });

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);

        var rows = form["tableField222"].ToObject<List<Dictionary<string, object>>>();
        Assert.Equal(2, rows.Count);
        Assert.Equal("r1", rows[0]["f_title"]);
        Assert.Equal("r2", rows[1]["f_title"]);
    }

    [Fact]
    public void ApplyMapRules_MainToChild_AddsOrUpdatesFirstRow()
    {
        var oldField = Field("title");
        var newField = Field("tableField111-f_name");
        var form = new Dictionary<string, object> { ["title"] = "hello" };
        var map = new List<Dictionary<string, string>>
        {
            new() { ["title"] = "tableField111-f_name" },
        };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { oldField });
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { newField });

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);

        var rows = form["tableField111"].ToObject<List<Dictionary<string, object>>>();
        Assert.Single(rows);
        Assert.Equal("hello", rows[0]["f_name"]);
    }

    [Fact]
    public void ApplyMapRules_ChildToMain_TakesFirstRowValue()
    {
        var oldField = Field("tableField111-f_name");
        var newField = Field("title");
        var form = new Dictionary<string, object>
        {
            ["tableField111"] = new List<Dictionary<string, object>>
            {
                new() { ["f_name"] = "first" },
                new() { ["f_name"] = "second" },
            },
        };
        var map = new List<Dictionary<string, string>>
        {
            new() { ["tableField111-f_name"] = "title" },
        };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { oldField });
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { newField });

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);
        Assert.Equal("first", form["tableField111-f_name"]);
    }

    [Fact]
    public void ApplyMapRules_IncompatibleMain_NullsOldValue()
    {
        var oldField = Field("qty", JnpfKeyConst.NUMINPUT);
        var newField = Field("day", JnpfKeyConst.DATE);
        var form = new Dictionary<string, object> { ["qty"] = 3 };
        var map = new List<Dictionary<string, string>> { new() { ["qty"] = "day" } };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { oldField });
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { newField });

        FlowFormDataMapper.ApplyMapRules(form, map, oldIdx, newIdx, FlowFormDataMapper.DefaultChildTableSplitKey);
        Assert.Null(form["qty"]);
    }

    [Fact]
    public void BuildResult_RemapsKeysAndIncludesChildTableBucket()
    {
        var form = new Dictionary<string, object>
        {
            ["oldName"] = "v",
            ["x"] = "ignored",
            ["tableField111"] = new List<Dictionary<string, object>> { new() { ["f_a"] = 1 } },
        };
        var map = new List<Dictionary<string, string>>
        {
            new() { ["oldName"] = "newName" },
            new() { ["x"] = "tableField111-f_a" },
        };

        var res = FlowFormDataMapper.BuildResult(form, map, FlowFormDataMapper.DefaultChildTableSplitKey);
        Assert.Equal("v", res["newName"]);
        Assert.True(res.ContainsKey("tableField111"));
    }

    [Fact]
    public void ApplyPrevNodeFormId_WritesIdOntoMappedKey()
    {
        var form = new Dictionary<string, object> { ["id"] = "ID-1" };
        var res = new Dictionary<string, object>();
        var map = new List<Dictionary<string, string>>
        {
            new() { ["@prevNodeFormId"] = "prevId" },
        };

        FlowFormDataMapper.ApplyPrevNodeFormId(res, form, map);
        Assert.Equal("ID-1", res["prevId"]);
    }

    [Fact]
    public void ApplyPrevNodeFormId_TableFieldKey_WritesEachChildRow()
    {
        var form = new Dictionary<string, object> { ["id"] = "ID-9" };
        var res = new Dictionary<string, object>
        {
            ["tableField111"] = new List<Dictionary<string, object>>
            {
                new() { ["f_ref"] = "old" },
                new() { ["f_ref"] = "old2" },
            },
        };
        var map = new List<Dictionary<string, string>>
        {
            new() { ["@prevNodeFormId"] = "tableField111-f_ref" },
        };

        FlowFormDataMapper.ApplyPrevNodeFormId(res, form, map);
        var rows = res["tableField111"].ToObject<List<Dictionary<string, object>>>();
        Assert.Equal("ID-9", rows[0]["f_ref"]);
        Assert.Equal("ID-9", rows[1]["f_ref"]);
        Assert.Equal("ID-9", res["tableField111-f_ref"]);
    }

    [Fact]
    public void ApplyMapRules_SpecialFormSplitKey_SkipsIncompatibleNulling()
    {
        var oldField = Field("qty", JnpfKeyConst.NUMINPUT);
        var newField = Field("day", JnpfKeyConst.DATE);
        var form = new Dictionary<string, object> { ["qty"] = 3 };
        var map = new List<Dictionary<string, string>> { new() { ["qty"] = "day" } };
        var oldIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { oldField });
        var newIdx = FlowFormMapRuleMerger.IndexByVModel(new[] { newField });

        FlowFormDataMapper.ApplyMapRules(
            form, map, oldIdx, newIdx, FlowFormDataMapper.SpecialFormChildTableSplitKey);
        Assert.Equal(3, form["qty"]);
    }
}
