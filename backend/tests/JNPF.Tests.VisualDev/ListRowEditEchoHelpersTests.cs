using JNPF.Common.Const;
using JNPF.Engine.Entity.Model;
using JNPF.VisualDev.Query;
using Xunit;

namespace JNPF.Tests.VisualDev;

public class ListRowEditEchoHelpersTests
{
    [Fact]
    public void AttachSuffixCopies_SkipsRowIndex()
    {
        var list = new List<Dictionary<string, object>>
        {
            new() { ["name"] = "a", ["RowIndex"] = 1 },
        };
        ListRowEditEchoHelpers.AttachSuffixCopies(list, "RID");
        Assert.Equal("a", list[0]["nameRID"]);
        Assert.False(list[0].ContainsKey("RowIndexRID"));
    }

    [Fact]
    public void RebuildEchoRows_MapsSuffixToNamePair()
    {
        var list = new List<Dictionary<string, object>>
        {
            new()
            {
                ["name"] = "显示名",
                ["nameRID"] = "[\"k1\"]",
                ["id"] = "id1",
                ["flowState"] = 2,
            },
        };
        var fields = new List<FieldsModel>();
        var rebuilt = ListRowEditEchoHelpers.RebuildEchoRows(list, "RID", fields);
        Assert.Single(rebuilt);
        Assert.Equal("显示名", rebuilt[0]["name_name"]);
        Assert.IsType<List<object>>(rebuilt[0]["name"]);
        Assert.Equal("id1", rebuilt[0]["id"]);
        Assert.Equal(2, rebuilt[0]["flowState"]);
    }

    [Fact]
    public void RebuildEchoRows_KeepsSystemFieldsByJnpfKey()
    {
        var list = new List<Dictionary<string, object>>
        {
            new()
            {
                ["f_creator"] = "u1",
                ["f_creatorRID"] = "u1",
            },
        };
        var fields = new List<FieldsModel>
        {
            new()
            {
                __vModel__ = "f_creator",
                __config__ = new ConfigModel { jnpfKey = JnpfKeyConst.CREATEUSER },
            },
        };
        var rebuilt = ListRowEditEchoHelpers.RebuildEchoRows(list, "RID", fields);
        Assert.Equal("u1", rebuilt[0]["f_creator"]);
    }

    [Fact]
    public void RebuildEchoRows_EmptySuffixValue_WritesNullPair()
    {
        var list = new List<Dictionary<string, object>>
        {
            new() { ["name"] = "x", ["nameRID"] = null! },
        };
        var rebuilt = ListRowEditEchoHelpers.RebuildEchoRows(list, "RID", Array.Empty<FieldsModel>());
        Assert.Null(rebuilt[0]["name"]);
        Assert.Null(rebuilt[0]["name_name"]);
    }

    [Fact]
    public void BuildSystemEchoVModels_IndexesMatchingKeys()
    {
        var fields = new List<FieldsModel>
        {
            new() { __vModel__ = "t1", __config__ = new ConfigModel { jnpfKey = JnpfKeyConst.TIME } },
            new() { __vModel__ = "n1", __config__ = new ConfigModel { jnpfKey = JnpfKeyConst.COMINPUT } },
        };
        var set = ListRowEditEchoHelpers.BuildSystemEchoVModels(fields);
        Assert.Contains("t1", set);
        Assert.DoesNotContain("n1", set);
    }
}
