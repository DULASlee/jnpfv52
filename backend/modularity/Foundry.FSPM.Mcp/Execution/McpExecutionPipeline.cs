// =============================================================================
//  Foundry.FSPM.Mcp — Execution/McpExecutionPipeline
// =============================================================================
//
//  MCP-06-07: THE shared execution pipeline for fspm_understand /
//  fspm_construct / fspm_verify:
//
//    Request → Validate → CreateContext → ResolveWorkspace → Gateway
//        → ProjectResult → PersistEvidence → Response
//
//  The pipeline owns the exception boundary (all stage exceptions go to
//  IMcpExceptionMapper); Tools must NOT double-wrap.
//
//  P6 workspace policy: resolution ALWAYS runs and its outcome is handed
//  to the gateway delegate, but an unresolvable path does NOT fail the
//  call here — enforcement belongs to real gateway bodies (P8+). This
//  preserves the frozen AwaitingContract behavior with synthetic paths.
// =============================================================================

using Foundry.FSPM.Mcp.Errors;
using Foundry.FSPM.Mcp.Validation;
using Foundry.FSPM.Mcp.Workspace;
using ModelContextProtocol.Protocol;

namespace Foundry.FSPM.Mcp.Execution;

internal sealed class McpExecutionPipeline
{
    private readonly McpExecutionContextFactory _contextFactory;
    private readonly IMcpWorkspaceResolver _workspaceResolver;
    private readonly IMcpExceptionMapper _exceptionMapper;

    public McpExecutionPipeline(
        McpExecutionContextFactory? contextFactory = null,
        IMcpWorkspaceResolver? workspaceResolver = null,
        IMcpExceptionMapper? exceptionMapper = null)
    {
        _contextFactory = contextFactory ?? new McpExecutionContextFactory();
        _workspaceResolver = workspaceResolver ?? new McpWorkspaceResolver();
        _exceptionMapper = exceptionMapper ?? new McpExceptionMapper();
    }

    public async Task<CallToolResult> ExecuteAsync(
        string toolName,
        string workspaceRoot,
        object request,
        Func<McpValidationResult?> validate,
        Func<McpExecutionContext, McpWorkspaceOutcome, Task<object>> invoke,
        Func<McpExecutionContext, object, Task>? persistEvidence = null)
    {
        try
        {
            var failure = validate();
            if (failure is { IsValid: false })
                return McpOperationResult.InvalidRequest(failure);

            var context = _contextFactory.Create(toolName, workspaceRoot, request);
            var workspace = _workspaceResolver.Resolve(workspaceRoot);

            object payload = await invoke(context, workspace).ConfigureAwait(false);

            if (persistEvidence is not null)
                await persistEvidence(context, payload).ConfigureAwait(false);

            return McpOperationResult.Success(payload);
        }
        catch (Exception ex)
        {
            return _exceptionMapper.Map(ex, toolName);
        }
    }
}
