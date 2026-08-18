using JNPF.Common.Const;
using JNPF.Engine.Entity.Model;
using JNPF.VisualDev.Transfer;
using Xunit;

namespace JNPF.Tests.VisualDev;

public class FlowFormMapRuleMergerTests
{
    [Fact]
    public void MergeAutoMappedFields_AddsMatchingVModelOnce()
    {
        var oldFields = new List<FieldsModel>
        {
            Field("name", false),
            Field("age", false),
        };
        var newFields = new List<FieldsModel>
        {
            Field("name", false),
            Field("age", true), // multiple mismatch → skip
        };

        var map = FlowFormMapRuleMerger.MergeAutoMappedFields(oldFields, newFields, null);

        Assert.Single(map);
        Assert.Equal("name", map[0]["name"]);
    }

    [Fact]
    public void MergeAutoMappedFields_DoesNotDuplicateExistingKeysOrValues()
    {
        var oldFields = new List<FieldsModel> { Field("a", false), Field("b", false) };
        var newFields = new List<FieldsModel> { Field("a", false), Field("b", false) };
        var existing = new List<Dictionary<string, string>>
        {
            new() { ["a"] = "mappedA" },
        };

        var map = FlowFormMapRuleMerger.MergeAutoMappedFields(oldFields, newFields, existing);

        Assert.Equal(2, map.Count);
        Assert.Contains(map, d => d.ContainsKey("b") && d["b"] == "b");
        Assert.DoesNotContain(map, d => d.ContainsKey("a") && d["a"] == "a");
    }

    [Fact]
    public void IndexByVModel_FirstWins()
    {
        var fields = new List<FieldsModel> { Field("x", false), Field("x", true) };
        var index = FlowFormMapRuleMerger.IndexByVModel(fields);
        Assert.Single(index);
        Assert.False(index["x"].multiple);
    }

    [Theory]
    [InlineData(JnpfKeyConst.COMINPUT, JnpfKeyConst.TEXTAREA, true)]
    [InlineData(JnpfKeyConst.NUMINPUT, JnpfKeyConst.DATE, false)]
    [InlineData(JnpfKeyConst.DATE, JnpfKeyConst.DATE, true)]
    public void CanTransfer_MatchesLegacyRules(string from, string to, bool expected)
    {
        var oldM = Field("f", false, from);
        var newM = Field("f", false, to);
        Assert.Equal(expected, FlowFormDataTransferRules.CanTransfer(oldM, newM));
    }

    [Fact]
    public void CanTransfer_TreeMulti_DoesNotAllowCheckbox()
    {
        var oldM = Field("f", true, JnpfKeyConst.TREESELECT);
        var newM = Field("f", true, JnpfKeyConst.CHECKBOX);
        Assert.False(FlowFormDataTransferRules.CanTransfer(oldM, newM));
    }

    [Fact]
    public void CanTransfer_PopupMulti_DoesNotAllowCheckbox()
    {
        var oldM = Field("f", true, JnpfKeyConst.POPUPTABLESELECT);
        var newM = Field("f", true, JnpfKeyConst.CHECKBOX);
        Assert.False(FlowFormDataTransferRules.CanTransfer(oldM, newM));
    }

    [Fact]
    public void CanTransfer_Checkbox_AllowsCheckbox()
    {
        var oldM = Field("f", true, JnpfKeyConst.CHECKBOX);
        var newM = Field("f", true, JnpfKeyConst.CHECKBOX);
        Assert.True(FlowFormDataTransferRules.CanTransfer(oldM, newM));
    }

    [Fact]
    public void MergeAutoMappedFields_MatchesAnyMultipleOnDuplicateVModel()
    {
        var oldFields = new List<FieldsModel> { Field("dup", true) };
        var newFields = new List<FieldsModel>
        {
            Field("dup", false), // first-wins would wrongly reject
            Field("dup", true),
        };

        var map = FlowFormMapRuleMerger.MergeAutoMappedFields(oldFields, newFields, null);
        Assert.Single(map);
        Assert.Equal("dup", map[0]["dup"]);
    }

    private static FieldsModel Field(string vModel, bool multiple, string jnpfKey = JnpfKeyConst.COMINPUT)
        => new()
        {
            __vModel__ = vModel,
            multiple = multiple,
            __config__ = new ConfigModel { jnpfKey = jnpfKey },
        };
}
