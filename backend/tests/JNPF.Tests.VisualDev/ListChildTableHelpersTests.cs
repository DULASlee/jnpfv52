using JNPF.Common.Const;
using JNPF.Engine.Entity.Model;
using JNPF.VisualDev.Query;
using SqlSugar;
using Xunit;

namespace JNPF.Tests.VisualDev;

public class ListChildTableHelpersTests
{
    [Fact]
    public void CollectParentIds_PreservesOrder()
    {
        var list = new List<Dictionary<string, object>>
        {
            new() { ["id"] = "a" },
            new() { ["id"] = "b" },
        };
        Assert.Equal(new object[] { "a", "b" }, ListChildTableHelpers.CollectParentIds(list, "id"));
    }

    [Fact]
    public void BuildChildTableSelectColumns_TakesSuffixAfterLastDash()
    {
        var fields = new List<FieldsModel>
        {
            new()
            {
                __vModel__ = "tableField111",
                __config__ = new ConfigModel { jnpfKey = JnpfKeyConst.TABLE, tableName = "child_t" },
            },
            new()
            {
                __vModel__ = "tableField111-f_name",
                __config__ = new ConfigModel { jnpfKey = JnpfKeyConst.COMINPUT, tableName = "child_t" },
            },
            new()
            {
                __vModel__ = "tableField111-nested-f_qty",
                __config__ = new ConfigModel { jnpfKey = JnpfKeyConst.COMINPUT, tableName = "child_t" },
            },
        };

        var map = ListChildTableHelpers.BuildChildTableSelectColumns(fields);
        Assert.True(map.ContainsKey("child_t"));
        Assert.Equal(new[] { "f_name", "f_qty" }, map["child_t"]);
    }

    [Fact]
    public void BuildRelationFields_KeepsFirstTableFieldOnly()
    {
        var childModels = new List<FieldsModel>
        {
            new() { __config__ = new ConfigModel { tableName = "child_t" } },
            new() { __config__ = new ConfigModel { tableName = "child_t" } },
        };
        var tables = new List<TableModel>
        {
            new() { table = "child_t", tableField = "f_pid" },
        };

        var map = ListChildTableHelpers.BuildRelationFields(childModels, tables);
        Assert.Single(map);
        Assert.Equal("f_pid", map["child_t"]);
    }

    [Fact]
    public void RewriteQuotedMapKeys_ReplacesQuotedLogicalKeys()
    {
        var json = "{\"FieldName\":\"logical\",\"x\":1}";
        var map = new Dictionary<string, string> { ["logical"] = "phys.col" };
        var rewritten = ListChildTableHelpers.RewriteQuotedMapKeys(json, map);
        Assert.Contains("\"phys.col\"", rewritten);
        Assert.DoesNotContain("\"logical\"", rewritten);
    }

    [Fact]
    public void RewriteChildFieldNames_MapsLogicalToPhysical()
    {
        var collections = new List<ConditionalCollections>
        {
            new()
            {
                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>
                {
                    new(WhereType.And, new ConditionalModel { FieldName = "tableField111-f_name" }),
                },
            },
        };
        var childFields = new Dictionary<string, string>
        {
            ["tableField111-f_name"] = "child_t.f_name",
        };

        ListChildTableHelpers.RewriteChildFieldNames(collections, childFields);
        Assert.Equal("child_t.f_name", collections[0].ConditionalList[0].Value.FieldName);
    }

    [Fact]
    public void BuildChildTableInSql_AppendsRelationAndJoinsIds()
    {
        var cols = new List<string> { "f_name" };
        var sql = ListChildTableHelpers.BuildChildTableInSql(cols, "child_t", "f_pid", new object[] { "1", "2" });
        Assert.Equal(new[] { "f_name", "f_pid" }, cols);
        Assert.Equal("select f_name,f_pid from child_t where f_pid in('1','2')", sql);
    }

    [Fact]
    public void AppendAndWhereFragment_OnlyWhenWherePresent()
    {
        var baseSql = "select a from t where id in('1')";
        Assert.Equal(baseSql, ListChildTableHelpers.AppendAndWhereFragment(baseSql, "SELECT * FROM x"));
        var with = ListChildTableHelpers.AppendAndWhereFragment(baseSql, "SELECT * FROM x WHERE f_name = N'a'");
        Assert.Equal(" select a from t where id in('1') and  f_name = N'a' ", with);
    }

    [Fact]
    public void FilterObjectsContainingTablePrefix_KeepsMatching()
    {
        var all = new List<object>
        {
            new Dictionary<string, object> { ["FieldName"] = "child_t.f_name" },
            new Dictionary<string, object> { ["FieldName"] = "main.f_title" },
        };
        var filtered = ListChildTableHelpers.FilterObjectsContainingTablePrefix(all, "child_t");
        Assert.Single(filtered);
    }

    [Fact]
    public void MatchRowsByRelation_StringEquals()
    {
        var rows = new List<Dictionary<string, object>>
        {
            new() { ["f_pid"] = 1, ["f_name"] = "a" },
            new() { ["f_pid"] = "2", ["f_name"] = "b" },
        };
        var matched = ListChildTableHelpers.MatchRowsByRelation(rows, "f_pid", 1);
        Assert.Single(matched);
        Assert.Equal("a", matched[0]["f_name"]);
    }

    [Fact]
    public void StripChildRowMeta_RemovesMarkerAndFk()
    {
        var datas = new List<Dictionary<string, object>>
        {
            new()
            {
                ["f_name"] = "x",
                ["f_pid"] = "1",
                ["JnpfKeyConst_MainData"] = "{}",
            },
        };
        var cleaned = ListChildTableHelpers.StripChildRowMeta(datas, "f_pid");
        Assert.Single(cleaned);
        Assert.Equal("x", cleaned[0]["f_name"]?.ToString());
        Assert.False(cleaned[0].ContainsKey("f_pid"));
        Assert.False(cleaned[0].ContainsKey("JnpfKeyConst_MainData"));
        Assert.True(datas[0].ContainsKey("f_pid"));
    }
}
