using JNPF.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Skills.Cognitive.Mcp;

/// <summary>
/// 进程内 MCP 客户端——DI 聚合全部 IMcpToolHandler，按工具名路由（施工包 21 §3.2）。
/// 对外由 <see cref="RoutingMcpClient"/> 实现 <see cref="IMcpClient"/>。
/// </summary>
public sealed class InProcMcpClient : ITransient
{
    private readonly IReadOnlyDictionary<string, IMcpToolHandler> _handlers;
    private readonly ILogger<InProcMcpClient> _logger;

    public InProcMcpClient(IEnumerable<IMcpToolHandler> handlers, ILogger<InProcMcpClient> logger)
    {
        _handlers = handlers
            .GroupBy(h => h.Descriptor.Name, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        _logger = logger;
    }

    public IReadOnlyList<McpToolDescriptor> ListTools()
        => _handlers.Values.Select(h => h.Descriptor).OrderBy(d => d.Name, StringComparer.Ordinal).ToList();

    public async Task<McpToolResult> CallToolAsync(string toolName, string argumentsJson, CancellationToken ct = default)
    {
        if (!_handlers.TryGetValue(toolName, out var handler))
            return McpToolResult.Fail($"未注册的 MCP 工具: {toolName}");

        try
        {
            var result = await handler.ExecuteAsync(argumentsJson, ct);
            _logger.LogInformation(
                "MCP tool {Tool} => {Status}", toolName, result.IsSuccess ? "ok" : $"fail:{result.Error}");
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MCP tool {Tool} 执行异常", toolName);
            return McpToolResult.Fail($"{toolName} 执行异常: {ex.Message}");
        }
    }
}
