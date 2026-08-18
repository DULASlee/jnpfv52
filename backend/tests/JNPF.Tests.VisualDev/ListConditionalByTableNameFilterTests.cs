using JNPF.VisualDev.Query;
using SqlSugar;
using Xunit;

namespace JNPF.Tests.VisualDev;

public class ListConditionalByTableNameFilterTests
{
    [Fact]
    public void Filter_KeepsWhenFieldContainsTableName_WithoutStrip()
    {
        var list = new List<IConditionalModel>
        {
            new ConditionalModel { FieldName = "child_t.f_name" },
            new ConditionalModel { FieldName = "main.f_title" },
        };

        ListConditionalByTableNameFilter.Filter(list, "child_t");
        Assert.Single(list);
        Assert.Equal("child_t.f_name", ((ConditionalModel)list[0]).FieldName);
    }

    [Fact]
    public void Filter_ContainsIsSubstring_NotQualifiedDot()
    {
        // Quirk vs CodeGen: Contains("child") also matches "childish.col"
        var list = new List<IConditionalModel>
        {
            new ConditionalModel { FieldName = "childish.col" },
        };

        ListConditionalByTableNameFilter.Filter(list, "child");
        Assert.Single(list);
        Assert.Equal("childish.col", ((ConditionalModel)list[0]).FieldName);
    }

    [Fact]
    public void Filter_RemoveAtDoesNotSkipNextLeaf()
    {
        // Without i-- after RemoveAt(0): index advances to 1 and skips other.b → wrongly keeps other.b.
        var list = new List<IConditionalModel>
        {
            new ConditionalModel { FieldName = "other.a" },
            new ConditionalModel { FieldName = "other.b" },
            new ConditionalModel { FieldName = "keep.c" },
        };

        ListConditionalByTableNameFilter.Filter(list, "keep");
        Assert.Single(list);
        Assert.Equal("keep.c", ((ConditionalModel)list[0]).FieldName);
    }

    [Fact]
    public void Filter_TreeFirstChildUsesWhereTypeNull()
    {
        var tree = new ConditionalTree
        {
            ConditionalList = new List<KeyValuePair<WhereType, IConditionalModel>>
            {
                new(WhereType.And, new ConditionalModel { FieldName = "t1.f_a" }),
                new(WhereType.Or, new ConditionalModel { FieldName = "t1.f_b" }),
            },
        };
        var list = new List<IConditionalModel> { tree };

        ListConditionalByTableNameFilter.Filter(list, "t1");
        Assert.Single(list);
        var kept = (ConditionalTree)list[0];
        Assert.Equal(2, kept.ConditionalList.Count);
        Assert.Equal(WhereType.Null, kept.ConditionalList[0].Key);
        Assert.Equal(WhereType.Or, kept.ConditionalList[1].Key);
    }

    [Fact]
    public void Filter_TreeDropsEmptyAfterChildRemoval()
    {
        var tree = new ConditionalTree
        {
            ConditionalList = new List<KeyValuePair<WhereType, IConditionalModel>>
            {
                new(WhereType.And, new ConditionalModel { FieldName = "other.x" }),
            },
        };
        var list = new List<IConditionalModel> { tree, new ConditionalModel { FieldName = "t1.y" } };

        ListConditionalByTableNameFilter.Filter(list, "t1");
        Assert.Single(list);
        Assert.Equal("t1.y", ((ConditionalModel)list[0]).FieldName);
    }

    [Fact]
    public void Filter_IgnoresConditionalCollectionsNodes()
    {
        // Legacy RunService only handled Tree + Model — Collections pass through untouched.
        var list = new List<IConditionalModel>
        {
            new ConditionalCollections
            {
                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>
                {
                    new(WhereType.And, new ConditionalModel { FieldName = "other.z" }),
                },
            },
        };

        ListConditionalByTableNameFilter.Filter(list, "t1");
        Assert.Single(list);
        Assert.IsType<ConditionalCollections>(list[0]);
    }
}
