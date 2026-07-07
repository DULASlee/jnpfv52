using JNPF.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Skills.Cognitive.Mcp;

/// <summary>
/// 传输路由 MCP 客户端——按 Manifest 在 InProc 与 HTTP 间分流（施工包 21 R5）。
/// 技能仅依赖 IMcpClient，切换传输零改动。
/// </summary>
public sealed class RoutingMcpClient : IMcpClient, ITransient
{
    private readonly InProcMcpClient _inProc;
    private readonly HttpMcpTransport _http;
    private readonly McpToolsOptions _options;
    private readonly ILogger<RoutingMcpClient> _logger;

    public RoutingMcpClient(
        InProcMcpClient inProc,
        HttpMcpTransport http,
        IConfiguration configuration,
        ILogger<RoutingMcpClient> logger)
    {
        _inProc = inProc;
        _http = http;
        _logger = logger;
        _options = configuration.GetSection(McpToolsOptions.SectionName).Get<McpToolsOptions>()
            ?? new McpToolsOptions();
    }

    public IReadOnlyList<McpToolDescriptor> ListTools()
    {
        var map = _inProc.ListTools().ToDictionary(d => d.Name, d => d, StringComparer.Ordinal);

        foreach (var (name, route) in _options.Tools)
        {
            if (string.Equals(route.Transport, "http", StringComparison.OrdinalIgnoreCase))
            {
                map[name] = new McpToolDescriptor
                {
                    Name = name,
                    Description = route.Description ?? $"HTTP 传输工具 ({route.Endpoint ?? "gateway"})",
                };
            }
        }

        return map.Values.OrderBy(d => d.Name, StringComparer.Ordinal).ToList();
    }

    public async Task<McpToolResult> CallToolAsync(
        string toolName,
        string argumentsJson,
        CancellationToken ct = default)
    {
        if (TryGetHttpRoute(toolName, out var endpoint))
        {
            _logger.LogDebug("MCP route {Tool} => HTTP {Endpoint}", toolName, endpoint);
            return await _http.CallToolAsync(endpoint, toolName, argumentsJson, ct);
        }

        return await _inProc.CallToolAsync(toolName, argumentsJson, ct);
    }

    private bool TryGetHttpRoute(string toolName, out string endpoint)
    {
        endpoint = string.Empty;
        if (!_options.Tools.TryGetValue(toolName, out var route))
            return false;
        if (!string.Equals(route.Transport, "http", StringComparison.OrdinalIgnoreCase))
            return false;

        endpoint = string.IsNullOrWhiteSpace(route.Endpoint)
            ? $"{_options.GatewayBaseUrl.TrimEnd('/')}/api/studio/mcp/call"
            : route.Endpoint;
        return true;
    }
}
