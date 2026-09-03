// =============================================================================
//  Foundry.FSPM.Mcp.Tests — Unit/McpRequestValidatorTests
// =============================================================================
//
//  MCP-06-02: validator unit tests (no server needed).
// =============================================================================

using Foundry.FSPM.Mcp.Validation;
using Xunit;

namespace Foundry.FSPM.Mcp.Tests.Unit;

public class McpRequestValidatorTests
{
    private readonly IMcpRequestValidator _validator = new McpRequestValidator();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateRequired_RejectsNullEmptyWhitespace(string? value)
    {
        var result = _validator.ValidateRequired("workspaceRoot", value);

        Assert.False(result.IsValid);
        Assert.Equal("workspaceRoot", result.Field);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Fact]
    public void ValidateRequired_AcceptsNonEmpty()
    {
        var result = _validator.ValidateRequired("workspaceRoot", "D:/w");

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("User")]
    [InlineData("User.Login")]
    [InlineData("User.UserName")]
    [InlineData("_U1")]
    public void ValidateQualifiedName_AcceptsEntityShapes(string value)
    {
        var result = _validator.ValidateQualifiedName("target", value);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("User.")]
    [InlineData(".Login")]
    [InlineData(".")]
    [InlineData("A.B.C")]
    [InlineData("User Login")]
    [InlineData("9User")]
    [InlineData("_U1.$x")]
    public void ValidateQualifiedName_RejectsMalformed(string? value)
    {
        var result = _validator.ValidateQualifiedName("target", value);

        Assert.False(result.IsValid);
        Assert.Equal("target", result.Field);
    }
}
