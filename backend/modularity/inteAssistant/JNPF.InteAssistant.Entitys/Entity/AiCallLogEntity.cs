using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// AI 调用日志
/// 版 本：v5.2.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2026-6-12
/// </summary>
[SugarTable("BASE_AI_CALL_LOG", TableDescription = "AI调用日志")]
public class AiCallLogEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 模型名称（如 gpt-4o, deepseek-v3）
    /// </summary>
    [SugarColumn(ColumnName = "F_MODEL")]
    public string Model { get; set; }

    /// <summary>
    /// 输入 Token 数
    /// </summary>
    [SugarColumn(ColumnName = "F_PROMPT_TOKENS")]
    public int? PromptTokens { get; set; }

    /// <summary>
    /// 输出 Token 数
    /// </summary>
    [SugarColumn(ColumnName = "F_COMPLETION_TOKENS")]
    public int? CompletionTokens { get; set; }

    /// <summary>
    /// 延迟毫秒
    /// </summary>
    [SugarColumn(ColumnName = "F_LATENCY_MS")]
    public long? LatencyMs { get; set; }

    /// <summary>
    /// HTTP 状态码
    /// </summary>
    [SugarColumn(ColumnName = "F_STATUS_CODE")]
    public int? StatusCode { get; set; }

    /// <summary>
    /// 请求体 JSON
    /// </summary>
    [SugarColumn(ColumnName = "F_REQUEST_BODY")]
    public string RequestBody { get; set; }

    /// <summary>
    /// 响应体 JSON
    /// </summary>
    [SugarColumn(ColumnName = "F_RESPONSE_BODY")]
    public string ResponseBody { get; set; }
}
