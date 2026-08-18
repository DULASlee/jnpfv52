using JNPF.Engine.Entity.Model;
using JNPF.VisualDev.Entitys.Dto.VisualDevModelData;
using JNPF.VisualDev.Query;
using Xunit;

namespace JNPF.Tests.VisualDev;

/// <summary>
/// Characterization: GetListQuerySql pure fragments / projection remaps.
/// </summary>
public class ListQuerySqlFragmentHelpersTests
{
    [Fact]
    public void BuildSelectFrom_FallsBackToPrimary()
    {
        Assert.Equal("select f_id from t_order ", ListQuerySqlFragmentHelpers.BuildSelectFrom(null, "f_id", "t_order"));
        Assert.Equal("select oid from t_order ", ListQuerySqlFragmentHelpers.BuildSelectFrom("oid", "f_id", "t_order"));
    }

    [Fact]
    public void BuildPrimaryInSubquery_Wraps()
        => Assert.Equal(" (f_id in (select 1))", ListQuerySqlFragmentHelpers.BuildPrimaryInSubquery("f_id", "select 1"));

    [Fact]
    public void BuildChildTableEmptyOrMatch_IncludesNotIn()
    {
        var sql = ListQuerySqlFragmentHelpers.BuildChildTableEmptyOrMatch("f_id", "select x", "pid", "child");
        Assert.Contains("NOT IN ( SELECT pid FROM child )", sql);
        Assert.Contains("f_id in (select x)", sql);
    }

    [Fact]
    public void IsEmptyOrNullConditionalTypeJson_Detects11And14()
    {
        Assert.True(ListQuerySqlFragmentHelpers.IsEmptyOrNullConditionalTypeJson("{\"ConditionalType\":11}"));
        Assert.True(ListQuerySqlFragmentHelpers.IsEmptyOrNullConditionalTypeJson("{\"ConditionalType\":14}"));
        Assert.False(ListQuerySqlFragmentHelpers.IsEmptyOrNullConditionalTypeJson("{\"ConditionalType\":1}"));
    }

    [Fact]
    public void MergeWhereIntoExistingInSubquery_PreservesLegacyShape()
    {
        var oldSql = "(f_id IN (select a from t WHERE a=1))";
        var itemWhere = "SELECT * FROM @ WHERE b=2";
        var merged = ListQuerySqlFragmentHelpers.MergeWhereIntoExistingInSubquery(oldSql, itemWhere);
        Assert.Equal("(f_id IN (select a from t WHERE b=2and a=1))", merged);
    }

    [Fact]
    public void InjectWhereIntoExistingSubquery_PreservesLegacyShape()
    {
        var itemSql = "(f_id IN (select a from t WHERE))";
        var itemWhere = "SELECT * FROM @ WHERE b=2";
        var injected = ListQuerySqlFragmentHelpers.InjectWhereIntoExistingSubquery(itemSql, itemWhere);
        Assert.Equal("(f_id IN (select a from t WHERE b=2))", injected);
    }

    [Fact]
    public void WrapOuterListQuery_JoinsFilters()
    {
        var sql = ListQuerySqlFragmentHelpers.WrapOuterListQuery(
            "select 1",
            new[] { "a=1", "b=2" },
            "and (s)",
            "and (d)",
            "and (p)");
        Assert.Equal("select * from (select 1) mt where a=1 and b=2 and (s) and (d) and (p)", sql);
    }

    [Fact]
    public void RewriteMainTablePermissionFieldNames_MapsAlias()
    {
        var json = "{\"FieldName\":\"main.name\",}";
        var map = new Dictionary<string, string> { ["FIELD_0"] = "name" };
        var result = ListQuerySqlFragmentHelpers.RewriteMainTablePermissionFieldNames(json, map, "main");
        Assert.Equal("{\"FieldName\":\"FIELD_0\",}", result);
    }

    [Fact]
    public void RewriteJoinedPermissionFieldNames_UsesAllTableFieldsThenStrip()
    {
        var json = "{\"FieldName\":\"mt.age\",}";
        var map = new Dictionary<string, string> { ["FIELD_1"] = "age" };
        var all = new Dictionary<string, string> { ["age"] = "mt.age" };
        var result = ListQuerySqlFragmentHelpers.RewriteJoinedPermissionFieldNames(json, map, all, "mt");
        Assert.Equal("{\"FieldName\":\"FIELD_1\",}", result);
    }

    [Fact]
    public void RemapSearchListFieldAliases_WritesPcAndApp()
    {
        var fields = new List<string>();
        var pc = new IndexSearchFieldModel { id = "old", __vModel__ = "old", prop = "old" };
        var app = new IndexSearchFieldModel { id = "old", __vModel__ = "old", prop = "old" };
        ListQuerySqlProjectionHelpers.RemapSearchListFieldAliases(pc, app, "t.col", fields);
        Assert.Equal("t.col", pc.id);
        Assert.Equal("t.col", app.__vModel__);
        Assert.Equal(new[] { "t.col", "t.col" }, fields);
    }

    [Fact]
    public void RemapQueryInputsToFieldAlias_RewritesJsonAndSearch()
    {
        var input = new VisualDevModelListQueryInput
        {
            queryJson = "{\"name\":1}",
            superQueryJson = "{\"field\":\"name\"}",
            dataRuleJson = "{\"FieldName\":\"name\"}",
            sidx = "name desc",
        };
        var search = new List<IndexSearchFieldModel>
        {
            new() { id = "name", __vModel__ = "name" },
        };
        var q = new Dictionary<string, object> { ["name"] = 1 };

        ListQuerySqlProjectionHelpers.RemapQueryInputsToFieldAlias(
            input, q, search, "name", "FIELD_0", remapSearchId: true);

        Assert.Equal("{\"FIELD_0\":1}", input.queryJson);
        Assert.Equal("{\"field\":\"FIELD_0\"}", input.superQueryJson);
        Assert.Equal("{\"FieldName\":\"FIELD_0\"}", input.dataRuleJson);
        Assert.Equal("FIELD_0 desc", input.sidx);
        Assert.Equal("FIELD_0", search[0].id);
        Assert.Equal("FIELD_0", search[0].__vModel__);
    }

    [Fact]
    public void BuildAuxiliaryJoinPredicates_BuildsEquals()
    {
        var tables = new List<TableModel>
        {
            new() { table = "child", relationField = "f_id", tableField = "pid" },
        };
        var keys = ListQuerySqlProjectionHelpers.BuildAuxiliaryJoinPredicates(
            new[] { "child" }, tables, "main");
        Assert.Equal(new[] { "main.f_id=child.pid" }, keys);
    }
}
