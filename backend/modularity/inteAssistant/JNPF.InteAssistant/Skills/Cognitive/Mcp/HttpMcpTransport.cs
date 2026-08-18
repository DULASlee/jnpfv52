using System.Net.Http.Json;
using System.Text.Json;
using JNPF.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Skills.Cognitive.Mcp;

/// <summary>
/// MCP HTTP 传输层——POST 到远端网关或独立工具端点（施工包 21 R5）。
/// </summary>
public sealed class HttpMcpTransport : ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpMcpTransport> _logger;

    public HttpMcpTransport(IHttpClientFactory httpClientFactory, ILogger<HttpMcpTransport> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<McpToolResult> CallToolAsync(
        string endpoint,
        string toolName,
        string argumentsJson,
        CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(nameof(HttpMcpTransport));
        client.Timeout = TimeSpan.FromMinutes(5);

        using var response = await client.PostAsJsonAsync(
            endpoint,
            new McpCallRequest { ToolName = toolName, ArgumentsJson = argumentsJson },
            JsonOptions,
            ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("MCP HTTP {Tool} => HTTP {Status}: {Body}", toolName, (int)response.StatusCode, body);
            return McpToolResult.Fail($"MCP HTTP {toolName} 返回 {(int)response.StatusCode}: {body}");
        }

        return ParseToolResult(body, toolName);
    }

    public static McpToolResult ParseToolResult(string body, string toolName)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var data) || root.TryGetProperty("Data", out data))
                return DeserializeResult(data);

            return DeserializeResult(root);
        }
        catch (Exception ex)
        {
            return McpToolResult.Fail($"MCP HTTP {toolName} 响应解析失败: {ex.Message}");
        }
    }

    private static McpToolResult DeserializeResult(JsonElement payload)
    {
        var result = JsonSerializer.Deserialize<McpToolResult>(payload.GetRawText(), JsonOptions);
        return result ?? McpToolResult.Fail("MCP HTTP 响应为空");
    }
}

public sealed class McpCallRequest
{
    public required string ToolName { get; init; }

    public string ArgumentsJson { get; init; } = "{}";
}
