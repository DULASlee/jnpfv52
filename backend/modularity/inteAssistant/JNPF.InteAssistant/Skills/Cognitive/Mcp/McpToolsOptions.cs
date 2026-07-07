namespace JNPF.InteAssistant.Skills.Cognitive.Mcp;

/// <summary>
/// MCP 工具 Manifest 配置（R5：Configurations/McpTools.json）。
/// </summary>
public sealed class McpToolsOptions
{
    public const string SectionName = "McpTools";

    /// <summary>进程内 MCP 网关基址，HTTP 传输缺省 endpoint 时拼接 /api/studio/mcp/call。</summary>
    public string GatewayBaseUrl { get; init; } = "http://localhost:5000";

    /// <summary>按工具名覆盖传输方式；未列出的工具默认 inproc。</summary>
    public Dictionary<string, McpToolRouteOptions> Tools { get; init; } = new(StringComparer.Ordinal);
}

public sealed class McpToolRouteOptions
{
    /// <summary>inproc | http</summary>
    public string Transport { get; init; } = "inproc";

    /// <summary>HTTP 传输专用；为空时使用 GatewayBaseUrl + /api/studio/mcp/call。</summary>
    public string? Endpoint { get; init; }

    public string? Description { get; init; }
}
