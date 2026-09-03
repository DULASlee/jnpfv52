// =============================================================================
//  Foundry.FSPM.Mcp — Tools/Requests/UnderstandRequest
// =============================================================================
//
//  MCP-08-01: frozen Understand request shape (Workspace / Project / Target).
//  Forward-looking record for the P8 gateway body; the Tool entry keeps its
//  frozen primitive signature and maps into this record (wired in MCP-08-03).
// =============================================================================

namespace Foundry.FSPM.Mcp.Tools.Requests;

internal sealed record UnderstandRequest(string WorkspaceRoot, string? ProjectName, string Target);
