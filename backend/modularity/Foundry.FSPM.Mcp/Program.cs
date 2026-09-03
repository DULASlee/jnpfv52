// =============================================================================
//  Foundry.FSPM.Mcp — stdio MCP Server entry point
// =============================================================================
//
//  Phase A1.1: Minimal real .NET engineering — prove the MCP project can
//  build and start as a stdio MCP server. NO MCP Tools registered yet.
//
//  Spec authority: docs/superpowers/specs/2026-09-03-fspm-mcp-stdio-adapter-design.md
//  v2 §3.1 (Program.cs) + v2 §1 (Process Stream Contract, frozen).
//
//  Process Stream Contract (FROZEN — Iron Rule):
//    stdin  ← MCP JSON-RPC frames (from MCP client)
//    stdout → ONLY MCP JSON-RPC frames (to MCP client)
//    stderr → ALL diagnostics, logs, startup errors
//
//  Therefore:
//    - Console.WriteLine(...) to stdout is FORBIDDEN.
//    - ILogger<T> via Microsoft.Extensions.Logging is the only allowed surface.
//    - LogToStandardErrorThreshold = LogLevel.Trace (verified by T07).
//
//  This program intentionally registers zero MCP Tools in Phase A1.1.
//  Phase A2 adds the three Tools (fspm_understand / fspm_construct /
//  fspm_verify) wired through the frozen Compiler/Semantic contract.
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// MCP requires logs on stderr; stdout is reserved for JSON-RPC framing.
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Register MCP server with stdio transport. Tools are discovered via
// [McpServerToolType] attribute in the assembly; Phase A1.1 has none.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync().ConfigureAwait(false);