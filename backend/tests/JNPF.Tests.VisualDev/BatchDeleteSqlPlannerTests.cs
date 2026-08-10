using JNPF.VisualDev.Delete;
using Xunit;

namespace JNPF.Tests.VisualDev;

public class BatchDeleteSqlPlannerTests
{
    [Fact]
    public void BuildHardDeleteSql_NormalRule_UsesInClause()
    {
        var sql = BatchDeleteSqlPlanner.BuildHardDeleteSql(
            "main_t",
            "f_id",
            new List<(string, string)> { ("child_t", "f_pid") },
            new[] { "a", "b" },
            deleteRule: 1);

        Assert.Equal(2, sql.Count);
        Assert.Equal("delete from main_t where f_id in ('a','b');", sql[0]);
        Assert.Equal("delete from child_t where f_pid in ('a','b');", sql[1]);
    }

    [Fact]
    public void BuildHardDeleteSql_InverseRule_UsesNotInClause()
    {
        var sql = BatchDeleteSqlPlanner.BuildHardDeleteSql(
            "main_t",
            "f_id",
            Array.Empty<(string, string)>(),
            new[] { "x" },
            deleteRule: 0);

        Assert.Single(sql);
        Assert.Equal("delete from main_t where f_id not in ('x');", sql[0]);
    }

    [Fact]
    public void BuildLogicalDeleteSql_NormalRule_MarksDeleteColumns()
    {
        var when = new DateTime(2026, 8, 7, 12, 0, 0);
        var sql = BatchDeleteSqlPlanner.BuildLogicalDeleteSql(
            "main_t",
            "f_id",
            "u1",
            when,
            new[] { "id1" },
            deleteRule: 1);

        Assert.Contains("update main_t set f_delete_mark=1", sql);
        Assert.Contains("f_delete_user_id='u1'", sql);
        Assert.Contains("f_id in ('id1')", sql);
        Assert.DoesNotContain("not in", sql);
    }

    [Fact]
    public void BuildClearAllTablesSql_EmitsDeletePerTable()
    {
        var sql = BatchDeleteSqlPlanner.BuildClearAllTablesSql(new[] { "t1", "t2" });
        Assert.Equal(new[] { "delete from t1", "delete from t2" }, sql);
    }
}
