using JNPF.VisualDev.Query;
using Xunit;

namespace JNPF.Tests.VisualDev;

public class ListQueryFieldAliasRewriterTests
{
    [Fact]
    public void ReplaceQuotedKey_RewritesOnlyQuotedToken()
    {
        var json = "{\"name\":\"alice\",\"tableName\":\"t1\"}";
        var result = ListQueryFieldAliasRewriter.ReplaceQuotedKey(json, "name", "mt.name");
        Assert.Equal("{\"mt.name\":\"alice\",\"tableName\":\"t1\"}", result);
    }

    [Fact]
    public void RewriteAll_AppliesMapInOrder()
    {
        var json = "{\"a\":1,\"b\":2}";
        var map = new Dictionary<string, string> { ["a"] = "t.a", ["b"] = "t.b" };
        var result = ListQueryFieldAliasRewriter.RewriteAll(json, map);
        Assert.Equal("{\"t.a\":1,\"t.b\":2}", result);
    }

    [Fact]
    public void ReplaceQuotedKey_NullOrEmpty_NoThrow()
    {
        Assert.Null(ListQueryFieldAliasRewriter.ReplaceQuotedKey(null!, "a", "b"));
        Assert.Equal(string.Empty, ListQueryFieldAliasRewriter.ReplaceQuotedKey(string.Empty, "a", "b"));
    }
}
