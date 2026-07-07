using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.InteAssistant.Skills.Cognitive.Mcp;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// MCP 进程内工具 HTTP 网关——供 HttpMcpTransport 与跨进程消费者调用（施工包 21 R5）。
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "StudioMcpGateway", Order = 192)]
[Route("api/studio/mcp")]
public class McpGatewayService : IDynamicApiController, ITransient
{
    private readonly InProcMcpClient _inProc;

    public McpGatewayService(InProcMcpClient inProc) => _inProc = inProc;

    [HttpGet("tools")]
    public object ListTools() => _inProc.ListTools();

    [HttpPost("call")]
    public Task<McpToolResult> CallTool([FromBody] McpCallRequest input, CancellationToken ct = default)
    {
        var toolName = input?.ToolName ?? string.Empty;
        var args = input?.ArgumentsJson ?? "{}";
        return _inProc.CallToolAsync(toolName, args, ct);
    }
}
