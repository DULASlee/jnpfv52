using JNPF.Common.Const;
using JNPF.Engine.Entity.Model;
using JNPF.VisualDev.Engine.Import;
using JNPF.VisualDev.Query;
using Xunit;

namespace JNPF.Tests.VisualDev;

/// <summary>
/// Characterization: GenerateFeilds pure helpers (W2).
/// </summary>
public class SystemFieldGenerateHelpersTests
{
    private static FieldsModel Field(string jnpfKey, string vModel = "f1")
        => new()
        {
            __vModel__ = vModel,
            __config__ = new ConfigModel { jnpfKey = jnpfKey },
        };

    [Fact]
    public void ForceCreateSemantics_UsesIsNotEmptyOrNullLegacy()
    {
        Assert.False(SystemFieldGenerateHelpers.ForceCreateSemantics(null));
        // Legacy IsNotEmptyOrNull(empty List) is true (ToString is type name, not "")
        Assert.True(SystemFieldGenerateHelpers.ForceCreateSemantics(new List<string>()));
        Assert.True(SystemFieldGenerateHelpers.ForceCreateSemantics(new List<string> { "a" }));
    }

    [Fact]
    public void StripSystemFieldsOnUpdate_RemovesCreateKeys()
    {
        var fields = new List<FieldsModel>
        {
            Field(JnpfKeyConst.CREATEUSER, "cu"),
            Field(JnpfKeyConst.CREATETIME, "ct"),
            Field(JnpfKeyConst.CURRORGANIZE, "org"),
        };
        var map = new Dictionary<string, object>
        {
            ["cu"] = "u1",
            ["ct"] = "t",
            ["org"] = "o",
            ["keep"] = 1,
        };
        SystemFieldGenerateHelpers.StripSystemFieldsOnUpdate(fields, map, systemControlList: null);
        Assert.False(map.ContainsKey("cu"));
        Assert.False(map.ContainsKey("ct"));
        Assert.False(map.ContainsKey("org"));
        Assert.True(map.ContainsKey("keep"));
    }

    [Fact]
    public void StripSystemFieldsOnUpdate_KeepsCurrWhenSystemControlListSet()
    {
        var fields = new List<FieldsModel> { Field(JnpfKeyConst.CURRPOSITION, "pos") };
        var map = new Dictionary<string, object> { ["pos"] = "p1" };
        SystemFieldGenerateHelpers.StripSystemFieldsOnUpdate(fields, map, new List<string> { "x" });
        Assert.Equal("p1", map["pos"]);
    }

    [Fact]
    public void StripSystemFieldsOnUpdate_StripsChildTableSystemKeys()
    {
        var table = Field(JnpfKeyConst.TABLE, "table1");
        table.__config__.children = new List<FieldsModel>
        {
            Field(JnpfKeyConst.CREATEUSER, "cu"),
            Field(JnpfKeyConst.COMINPUT, "name"),
        };
        var map = new Dictionary<string, object>
        {
            ["table1"] = new List<Dictionary<string, object>>
            {
                new() { ["cu"] = "u1", ["name"] = "n" },
            },
        };
        SystemFieldGenerateHelpers.StripSystemFieldsOnUpdate(new List<FieldsModel> { table }, map, null);
        var rows = Assert.IsType<List<Dictionary<string, object>>>(map["table1"]);
        Assert.False(rows[0].ContainsKey("cu"));
        Assert.Equal("n", rows[0]["name"]);
    }

    [Theory]
    [InlineData(true, true, false, true)]
    [InlineData(false, false, false, true)] // no id
    [InlineData(false, true, true, true)] // empty value
    [InlineData(false, true, false, false)] // keep existing
    public void ShouldGenerateChildBillRule(bool isCreate, bool hasId, bool emptyValue, bool expected)
    {
        var row = new Dictionary<string, object> { ["bill"] = emptyValue ? "" : "BN" };
        if (hasId) row["id"] = "1";
        Assert.Equal(
            expected,
            SystemFieldGenerateHelpers.ShouldGenerateChildBillRule(isCreate, row, "bill"));
    }

    [Fact]
    public void ApplyBillNumber_ReusesMissingRuleMessage()
    {
        var map = new Dictionary<string, object>();
        SystemFieldGenerateHelpers.ApplyBillNumber(map, "b", "OK-1");
        Assert.Equal("OK-1", map["b"]);
        SystemFieldGenerateHelpers.ApplyBillNumber(map, "b2", ImportSystemFieldAssembler.MissingBillRuleMessage);
        Assert.Equal(string.Empty, map["b2"]);
    }

    [Fact]
    public void ApplyCreateModify_RespectsIsCreate()
    {
        var now = new DateTime(2026, 8, 7, 10, 0, 0);
        var create = new Dictionary<string, object>();
        SystemFieldGenerateHelpers.ApplyCreateUser(create, "cu", "u1", isCreate: true);
        SystemFieldGenerateHelpers.ApplyCreateTime(create, "ct", now, isCreate: true);
        SystemFieldGenerateHelpers.ApplyModifyUser(create, "mu", "u1", isCreate: true);
        Assert.Equal("u1", create["cu"]);
        Assert.Equal("2026-08-07 10:00:00", create["ct"]);
        Assert.False(create.ContainsKey("mu"));

        var update = new Dictionary<string, object>();
        SystemFieldGenerateHelpers.ApplyModifyUser(update, "mu", "u2", isCreate: false);
        SystemFieldGenerateHelpers.ApplyModifyTime(update, "mt", now, isCreate: false);
        Assert.Equal("u2", update["mu"]);
        Assert.Equal("2026-08-07 10:00:00", update["mt"]);
    }

    [Fact]
    public void TryTakeFlowDelegate_CopiesDelegateKey()
    {
        var map = new Dictionary<string, object>
        {
            [SystemFieldGenerateHelpers.FlowDelegateCurrPosition] = "pos-x",
        };
        Assert.True(SystemFieldGenerateHelpers.TryTakeFlowDelegate(
            map, SystemFieldGenerateHelpers.FlowDelegateCurrPosition, "f_pos"));
        Assert.Equal("pos-x", map["f_pos"]);
        Assert.False(SystemFieldGenerateHelpers.TryTakeFlowDelegate(
            map, SystemFieldGenerateHelpers.FlowDelegateCurrOrganize, "f_org"));
    }

    [Fact]
    public void OrganizeTreeToJsonOrEmpty_AndEnsureUpload()
    {
        Assert.Equal(string.Empty, SystemFieldGenerateHelpers.OrganizeTreeToJsonOrEmpty(null));
        Assert.Contains("a", SystemFieldGenerateHelpers.OrganizeTreeToJson("a,b"));
        var map = new Dictionary<string, object>();
        SystemFieldGenerateHelpers.EnsureUploadDefault(map, "file");
        Assert.IsType<string[]>(map["file"]);
        Assert.Empty((string[])map["file"]);
    }
}
