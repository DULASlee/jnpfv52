namespace JNPF.InteAssistant.Skills.Cognitive.Mcp;

/// <summary>
/// MCP 工具描述符（施工包 21 §3.2 经络层 manifest）。
/// </summary>
public sealed class McpToolDescriptor
{
    /// <summary>工具唯一名，约定 "domain.action" 形式（如 kg.search-seeds）。</summary>
    public required string Name { get; init; }

    public required string Description { get; init; }

    /// <summary>入参 JSON Schema 摘要（人读为主，R0 不做机器校验）。</summary>
    public string ArgumentsSchema { get; init; } = "{}";
}

/// <summary>
/// MCP 工具调用结果。失败通过 IsSuccess=false 表达，路由层不抛业务异常，
/// 由技能自行决定失败语义（红线 RL-1：不得静默降级为假数据）。
/// </summary>
public sealed class McpToolResult
{
    public bool IsSuccess { get; init; }

    /// <summary>成功时的结果 JSON。</summary>
    public string ContentJson { get; init; } = "{}";

    public string? Error { get; init; }

    public static McpToolResult Ok(string contentJson) => new() { IsSuccess = true, ContentJson = contentJson };

    public static McpToolResult Fail(string error) => new() { IsSuccess = false, Error = error };
}

/// <summary>
/// 进程内 MCP 工具实现契约——实现类标记 ITransient 即被 InProcMcpClient 聚合。
/// </summary>
public interface IMcpToolHandler
{
    McpToolDescriptor Descriptor { get; }

    Task<McpToolResult> ExecuteAsync(string argumentsJson, CancellationToken ct = default);
}

/// <summary>
/// 传输无关的 MCP 客户端调用面。R0 为 InProc 实现；
/// R5 若升级 HTTP 传输，仅替换实现，技能代码不动。
/// </summary>
public interface IMcpClient
{
    IReadOnlyList<McpToolDescriptor> ListTools();

    Task<McpToolResult> CallToolAsync(string toolName, string argumentsJson, CancellationToken ct = default);
}
