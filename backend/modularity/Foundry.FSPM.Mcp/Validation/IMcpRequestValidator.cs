// =============================================================================
//  Foundry.FSPM.Mcp — Validation/IMcpRequestValidator
// =============================================================================
//
//  MCP-06-02: the ONE request validator shared by all three Tools.
//
//  Boundary rule (frozen by the 12/12 contract tests, which call the Tools
//  with well-formed but non-existent paths like "D:/tmp/contract-probe"
//  and expect success): the Tool-entry validator rejects null / empty /
//  malformed values ONLY. Filesystem existence is NOT its job — that
//  belongs to McpWorkspaceResolver (MCP-06-05) and the Core gateways.
// =============================================================================

namespace Foundry.FSPM.Mcp.Validation;

/// <summary>
/// Validation verdict for one request field.
/// </summary>
public sealed class McpValidationResult
{
    private McpValidationResult(bool isValid, string field, string message)
    {
        IsValid = isValid;
        Field = field;
        Message = message;
    }

    public bool IsValid { get; }
    public string Field { get; }
    public string Message { get; }

    public static McpValidationResult Ok(string field) =>
        new(true, field, string.Empty);

    public static McpValidationResult Fail(string field, string message) =>
        new(false, field, message);

    /// <summary>
    /// Returns the first failing result, or null when all pass.
    /// Used by Tools to feed a single failure into the pipeline.
    /// </summary>
    public static McpValidationResult? FirstInvalid(params McpValidationResult[] results)
    {
        foreach (var result in results)
        {
            if (!result.IsValid)
                return result;
        }

        return null;
    }
}

/// <summary>
/// Unified request validator for fspm_understand / fspm_construct /
/// fspm_verify. All Tools must validate through this interface — no
/// scattered ad-hoc checks.
/// </summary>
public interface IMcpRequestValidator
{
    McpValidationResult ValidateRequired(string fieldName, string? value);

    McpValidationResult ValidateQualifiedName(string fieldName, string? value);
}
