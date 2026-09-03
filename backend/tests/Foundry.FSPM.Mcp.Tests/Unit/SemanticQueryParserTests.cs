// =============================================================================
//  Foundry.FSPM.Mcp.Tests — Unit/SemanticQueryParserTests
// =============================================================================
//
//  MCP-08-02: target parser tests (no server needed).
// =============================================================================

using Foundry.FSPM.Mcp.Mapping;
using Xunit;

namespace Foundry.FSPM.Mcp.Tests.Unit;

public class SemanticQueryParserTests
{
    [Fact]
    public void Parse_TypeTarget_YieldsTypeQuery()
    {
        var (query, validation) = SemanticQueryParser.Parse("User");

        Assert.True(validation.IsValid);
        Assert.NotNull(query);
        Assert.Equal("User", query!.TypeName);
        Assert.Null(query.MemberName);
        Assert.True(query.IsTypeQuery);
    }

    [Theory]
    [InlineData("User.Login")]
    [InlineData("User.UserName")]
    [InlineData("User.Password")]
    public void Parse_MemberTargets_YieldMemberQuery(string target)
    {
        var (query, validation) = SemanticQueryParser.Parse(target);

        Assert.True(validation.IsValid);
        Assert.NotNull(query);
        Assert.Equal("User", query!.TypeName);
        Assert.False(string.IsNullOrWhiteSpace(query.MemberName));
        Assert.False(query.IsTypeQuery);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("User.")]
    [InlineData(".Login")]
    [InlineData("A.B.C")]
    public void Parse_MalformedTargets_Fail(string? target)
    {
        var (query, validation) = SemanticQueryParser.Parse(target);

        Assert.False(validation.IsValid);
        Assert.Null(query);
        Assert.Equal("target", validation.Field);
    }
}
