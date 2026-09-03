// =============================================================================
//  Foundry.FSPM.Mcp — Validation/McpRequestValidator
// =============================================================================
//
//  MCP-06-02: default implementation of IMcpRequestValidator.
//
//  Qualified-name shape (frozen for P8 TargetParser reuse):
//    "User" | "User.Login"  (1–2 dot-separated C# identifier segments)
//  Anything else (empty segments, >2 segments, whitespace, illegal chars)
//  is INVALID at the Tool boundary.
// =============================================================================

using System.Text.RegularExpressions;

namespace Foundry.FSPM.Mcp.Validation;

public sealed partial class McpRequestValidator : IMcpRequestValidator
{
    public McpValidationResult ValidateRequired(string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            return McpValidationResult.Fail(fieldName, "fieldName is required.");
        if (string.IsNullOrWhiteSpace(value))
            return McpValidationResult.Fail(fieldName, $"{fieldName} is required.");
        return McpValidationResult.Ok(fieldName);
    }

    public McpValidationResult ValidateQualifiedName(string fieldName, string? value)
    {
        var required = ValidateRequired(fieldName, value);
        if (!required.IsValid)
            return required;

        string v = value!.Trim();
        string[] segments = v.Split('.');
        if (segments.Length is < 1 or > 2 || segments.Any(s => !IdentifierRegex().IsMatch(s)))
        {
            return McpValidationResult.Fail(
                fieldName,
                $"{fieldName} must be 'Entity' or 'Entity.Member' (1-2 C# identifier segments).");
        }

        return McpValidationResult.Ok(fieldName);
    }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex IdentifierRegex();
}
