using JNPF.Common.Core.Manager.User.Conditions;
using SqlSugar;
using Xunit;

namespace JNPF.Tests.Common;

/// <summary>
/// Characterization: CodeGen authorize conditional split by table name (W1).
/// </summary>
public class ConditionalByTableNameFilterTests
{
    private static ConditionalModel Model(string field)
        => new() { FieldName = field, ConditionalType = ConditionalType.Equal, FieldValue = "1" };

    private static ConditionalCollections Coll(params string[] fields)
        => new()
        {
            ConditionalList = fields
                .Select(f => new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, Model(f)))
                .ToList(),
        };

    [Fact]
    public void NullTableName_RemovesDottedFields_KeepsBare()
    {
        var list = new List<IConditionalModel>
        {
            Model("f_id"),
            Model("child.f_name"),
        };
        ConditionalByTableNameFilter.Filter(list, null);
        Assert.Single(list);
        var m = Assert.IsType<ConditionalModel>(list[0]);
        Assert.Equal("f_id", m.FieldName);
    }

    [Fact]
    public void NamedTable_KeepsMatching_AndStripsPrefix_EvenAfterPrecedingRemoval()
    {
        // Non-matching first leaf must not skip the following matches (legacy RemoveAt without i--).
        var list = new List<IConditionalModel>
        {
            Model("main.f_id"),
            Model("child.f_name"),
            Model("child.f_code"),
        };
        ConditionalByTableNameFilter.Filter(list, "child");
        Assert.Equal(2, list.Count);
        Assert.Equal(new[] { "f_name", "f_code" }, list.Cast<ConditionalModel>().Select(m => m.FieldName).ToArray());
    }

    [Fact]
    public void Collections_AdjacentRemovals_DoNotSkipNextLeaf()
    {
        var list = new List<IConditionalModel>
        {
            Coll("drop.f_x", "keep.f_a", "drop.f_y", "keep.f_b"),
        };
        ConditionalByTableNameFilter.Filter(list, "keep");
        var coll = Assert.IsType<ConditionalCollections>(list[0]);
        Assert.Equal(new[] { "f_a", "f_b" }, coll.ConditionalList.Select(x => x.Value.FieldName).ToArray());
    }

    [Fact]
    public void Collections_NullTable_DropsQualifiedLeaves()
    {
        var list = new List<IConditionalModel>
        {
            Coll("f_id", "t1.f_x"),
        };
        ConditionalByTableNameFilter.Filter(list, null);
        var coll = Assert.IsType<ConditionalCollections>(list[0]);
        Assert.Single(coll.ConditionalList);
        Assert.Equal("f_id", coll.ConditionalList[0].Value.FieldName);
    }

    [Fact]
    public void Collections_NamedTable_StripsAndKeeps()
    {
        var list = new List<IConditionalModel>
        {
            Coll("t1.f_a", "t2.f_b"),
        };
        ConditionalByTableNameFilter.Filter(list, "t1");
        var coll = Assert.IsType<ConditionalCollections>(list[0]);
        Assert.Single(coll.ConditionalList);
        Assert.Equal("f_a", coll.ConditionalList[0].Value.FieldName);
    }

    [Fact]
    public void Tree_PrunesEmptyChildren()
    {
        var tree = new ConditionalTree
        {
            ConditionalList = new List<KeyValuePair<WhereType, IConditionalModel>>
            {
                new(WhereType.And, Model("other.f_x")),
                new(WhereType.Or, Model("keep.f_y")),
            },
        };
        var list = new List<IConditionalModel> { tree };
        ConditionalByTableNameFilter.Filter(list, "keep");
        Assert.Single(list);
        var t = Assert.IsType<ConditionalTree>(list[0]);
        Assert.Single(t.ConditionalList);
        var leaf = Assert.IsType<ConditionalModel>(t.ConditionalList[0].Value);
        Assert.Equal("f_y", leaf.FieldName);
    }

    [Fact]
    public void CollectBindTableNames_SkipsEmptySchemes()
    {
        var names = ConditionalByTableNameFilter.CollectBindTableNames(new[]
        {
            (false, Enumerable.Empty<string?>()),
            (true, new string?[] { "t1", null, "t2" }),
        });
        Assert.Equal(new[] { "t1", "t2" }, names);
    }

    [Fact]
    public void EmptyCollectionRemoval_DoesNotSkipFollowingSibling()
    {
        var list = new List<IConditionalModel>
        {
            Coll("t1.f_x"),
            Model("f_id"),
        };
        ConditionalByTableNameFilter.Filter(list, null);
        Assert.Single(list);
        var m = Assert.IsType<ConditionalModel>(list[0]);
        Assert.Equal("f_id", m.FieldName);
    }
}
