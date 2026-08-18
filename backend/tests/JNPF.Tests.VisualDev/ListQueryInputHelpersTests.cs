using JNPF.Common.Const;
using JNPF.Engine.Entity.Model;
using JNPF.VisualDev.Query;
using Xunit;

namespace JNPF.Tests.VisualDev;

/// <summary>
/// Characterization: GetListResult query enrichment / page size / flow id remap.
/// </summary>
public class ListQueryInputHelpersTests
{
    private static FieldsModel Field(string vModel)
        => new()
        {
            __vModel__ = vModel,
            __config__ = new ConfigModel { jnpfKey = JnpfKeyConst.COMINPUT, label = vModel },
        };

    [Fact]
    public void EnrichSearchListFromQuery_AddsMissingKeys()
    {
        var search = new List<IndexSearchFieldModel>
        {
            new() { id = "name" },
        };
        var fields = new List<FieldsModel> { Field("name"), Field("age") };
        var query = new Dictionary<string, object> { ["age"] = "1", ["name"] = "a" };

        ListQueryInputHelpers.EnrichSearchListFromQuery(query, search, fields);

        Assert.Equal(2, search.Count);
        Assert.Contains(search, s => s.id == "age");
    }

    [Fact]
    public void EnrichSearchListFromQuery_SkipsKeyword()
    {
        var search = new List<IndexSearchFieldModel>();
        var fields = new List<FieldsModel> { Field("name") };
        var query = new Dictionary<string, object> { [JnpfKeyConst.JNPFKEYWORD] = "x" };

        ListQueryInputHelpers.EnrichSearchListFromQuery(query, search, fields);
        Assert.Empty(search);
    }

    [Fact]
    public void EnrichSearchListFromQuery_NullQuery_NoThrow()
    {
        var search = new List<IndexSearchFieldModel>();
        ListQueryInputHelpers.EnrichSearchListFromQuery(null, search, new List<FieldsModel>());
        Assert.Empty(search);
    }

    [Theory]
    [InlineData(5, true, 20, 999999)]
    [InlineData(1, false, 20, 999999)]
    [InlineData(1, true, 20, 20)]
    public void ResolveEffectivePageSize_TreeOrNoPage(int type, bool hasPage, int size, int expected)
        => Assert.Equal(expected, ListQueryInputHelpers.ResolveEffectivePageSize(size, hasPage, type));

    [Fact]
    public void RemapPrimaryKeysByValue_WritesMatchingKey()
    {
        var list = new List<Dictionary<string, object>>
        {
            new() { ["f_id"] = "10" },
            new() { ["f_id"] = "20" },
        };
        var map = new List<KeyValuePair<string, string>>
        {
            new("flow-a", "10"),
            new("flow-b", "20"),
        };

        ListQueryInputHelpers.RemapPrimaryKeysByValue(list, "f_id", map);
        Assert.Equal("flow-a", list[0]["f_id"]);
        Assert.Equal("flow-b", list[1]["f_id"]);
    }

    [Fact]
    public void RemapPrimaryKeysByValue_AcceptsDictionary()
    {
        var list = new List<Dictionary<string, object>> { new() { ["f_id"] = "10" } };
        var map = new Dictionary<string, string> { ["flow-a"] = "10" };
        ListQueryInputHelpers.RemapPrimaryKeysByValue(list, "f_id", map);
        Assert.Equal("flow-a", list[0]["f_id"]);
    }
}
