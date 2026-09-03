// =============================================================================
//  Foundry.FSPM.Mcp — Mapping/SemanticQueryParser
// =============================================================================
//
//  MCP-08-02: parses "User" / "User.Login" into SemanticQuery. Pure string
//  parsing — NO Roslyn, NO compilation, NO resolution (those are Core's).
//  Shape validation reuses the shared IMcpRequestValidator so the Tool
//  boundary and the parser never disagree.
// =============================================================================

using Foundry.FSPM.Mcp.Validation;

namespace Foundry.FSPM.Mcp.Mapping;

internal sealed class SemanticQuery
{
    internal SemanticQuery(string typeName, string? memberName)
    {
        TypeName = typeName;
        MemberName = memberName;
    }

    public string TypeName { get; }
    public string? MemberName { get; }
    public bool IsTypeQuery => MemberName is null;
}

internal static class SemanticQueryParser
{
    private static readonly IMcpRequestValidator Validator = new McpRequestValidator();

    public static (SemanticQuery? Query, McpValidationResult Validation) Parse(string? target)
    {
        var shape = Validator.ValidateQualifiedName("target", target);
        if (!shape.IsValid)
            return (null, shape);

        string[] segments = target!.Trim().Split('.');
        if (segments.Length == 1)
            return (new SemanticQuery(segments[0], null), McpValidationResult.Ok("target"));

        return (new SemanticQuery(segments[0], segments[1]), McpValidationResult.Ok("target"));
    }
}
