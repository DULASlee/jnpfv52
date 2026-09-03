// =============================================================================
//  Foundry.FSPM.Mcp — Properties/AssemblyInfo
// =============================================================================
//
//  MCP-06-03: unit tests may see internal adapter models. This does NOT
//  make them public contracts (V6.1-02 ladder step ③).
// =============================================================================

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Foundry.FSPM.Mcp.Tests")]
