using JNPF.Common.Filter;
using JNPF.Engine.Entity.Model;
using JNPF.VisualDev.Query;
using Xunit;

namespace JNPF.Tests.VisualDev;

public class ListResultShapeHelpersTests
{
    [Fact]
    public void RewritePermissionFieldNames_AssignsReplace_LogicalToPhysical()
    {
        var json = "[{\"FieldName\":\"name\",\"Value\":\"a\"},]";
        var map = new Dictionary<string, string> { ["name"] = "mt.name" };
        var rewritten = ListResultShapeHelpers.RewritePermissionFieldNames(json, map);
        Assert.Contains("\"FieldName\":\"mt.name\",", rewritten);
        Assert.DoesNotContain("\"FieldName\":\"name\",", rewritten);
    }

    [Fact]
    public void RewritePermissionFieldNames_NoMatch_Unchanged()
    {
        var json = "[{\"FieldName\":\"other\",}]";
        var map = new Dictionary<string, string> { ["name"] = "mt.name" };
        Assert.Equal(json, ListResultShapeHelpers.RewritePermissionFieldNames(json, map));
    }

    [Fact]
    public void ApplyInMemorySort_Desc()
    {
        var list = new List<Dictionary<string, object>>
        {
            new() { ["n"] = 1 },
            new() { ["n"] = 3 },
            new() { ["n"] = 2 },
        };
        var sorted = ListResultShapeHelpers.ApplyInMemorySort(list, "n", "desc");
        Assert.Equal(3, sorted[0]["n"]);
        Assert.Equal(1, sorted[2]["n"]);
    }

    [Fact]
    public void ApplyInMemoryPaging_TakesSlice()
    {
        var page = new PageResult<Dictionary<string, object>>
        {
            list = Enumerable.Range(1, 5).Select(i => new Dictionary<string, object> { ["i"] = i }).ToList(),
        };
        ListResultShapeHelpers.ApplyInMemoryPaging(page, pageSize: 2, currentPage: 2, takePageSlice: true);
        Assert.Equal(5, page.pagination.total);
        Assert.Equal(2, page.list.Count);
        Assert.Equal(3, page.list[0]["i"]);
    }

    [Fact]
    public void FilterProcessReviewCompleted_KeepsState2()
    {
        var list = new List<Dictionary<string, object>>
        {
            new() { ["flowState"] = 2, ["id"] = "a" },
            new() { ["flowState"] = 1, ["id"] = "b" },
        };
        var filtered = ListResultShapeHelpers.FilterProcessReviewCompleted(list);
        Assert.Single(filtered);
        Assert.Equal("a", filtered[0]["id"]);
    }

    [Fact]
    public void FilterOnlyId_ProjectsId()
    {
        var list = new List<Dictionary<string, object>>
        {
            new() { ["id"] = "x", ["name"] = "n" },
        };
        var filtered = ListResultShapeHelpers.FilterOnlyId(list);
        Assert.Single(filtered[0]);
        Assert.Equal("x", filtered[0]["id"]);
    }

    [Fact]
    public void ResolveGroupShowField_PrefersLeftFixed()
    {
        var fields = new List<IndexGridFieldModel>
        {
            new() { __vModel__ = "a", @fixed = "right" },
            new() { __vModel__ = "b", @fixed = "left" },
        };
        Assert.Equal("b", ListResultShapeHelpers.ResolveGroupShowField(fields));
    }

    [Fact]
    public void AttachTreeParentMirror_CopiesParent()
    {
        var list = new List<Dictionary<string, object>>
        {
            new() { ["pid"] = "0" },
        };
        ListResultShapeHelpers.AttachTreeParentMirror(list, "pid");
        Assert.Equal("0", list[0]["pid_pid"]);
    }
}
