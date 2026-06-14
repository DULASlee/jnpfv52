using SqlSugar;

namespace JNPF.API.Entry.Entities;

/// <summary>
/// AI 调用审计日志实体。
/// 记录所有 LLM 调用的请求/响应摘要、Token 用量、延迟和错误信息。
/// 表名：BASE_AI_Call_LOG
/// </summary>
[SugarTable("BASE_AI_Call_LOG")]
public class AiCallLogEntity
{
    /// <summary>雪花 ID</summary>
    [SugarColumn(IsPrimaryKey = true, ColumnName = "F_ID")]
    public long Id { get; set; }

    /// <summary>租户 ID</summary>
    [SugarColumn(ColumnName = "F_TENANT_ID", Length = 50)]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>LLM 供应商（deepseek/tongyi/openai/ollama）</summary>
    [SugarColumn(ColumnName = "F_PROVIDER", Length = 50)]
    public string Provider { get; set; } = string.Empty;

    /// <summary>模型名称</summary>
    [SugarColumn(ColumnName = "F_MODEL", Length = 100)]
    public string Model { get; set; } = string.Empty;

    /// <summary>提示词 Token 数</summary>
    [SugarColumn(ColumnName = "F_PROMPT_TOKENS")]
    public int PromptTokens { get; set; }

    /// <summary>补全 Token 数</summary>
    [SugarColumn(ColumnName = "F_COMPLETION_TOKENS")]
    public int CompletionTokens { get; set; }

    /// <summary>总 Token 数</summary>
    [SugarColumn(ColumnName = "F_TOTAL_TOKENS")]
    public int TotalTokens { get; set; }

    /// <summary>请求延迟（毫秒）</summary>
    [SugarColumn(ColumnName = "F_LATENCY_MS")]
    public long LatencyMs { get; set; }

    /// <summary>调用是否成功</summary>
    [SugarColumn(ColumnName = "F_SUCCESS")]
    public bool Success { get; set; }

    /// <summary>错误信息</summary>
    [SugarColumn(ColumnName = "F_ERROR_MESSAGE", Length = 2000, IsNullable = true)]
    public string? ErrorMessage { get; set; }

    /// <summary>请求摘要（截取前 200 字）</summary>
    [SugarColumn(ColumnName = "F_REQUEST_SUMMARY", Length = 200, IsNullable = true)]
    public string? RequestSummary { get; set; }

    /// <summary>响应摘要（截取前 200 字）</summary>
    [SugarColumn(ColumnName = "F_RESPONSE_SUMMARY", Length = 200, IsNullable = true)]
    public string? ResponseSummary { get; set; }

    // ─── 审计字段 ───

    /// <summary>创建用户 ID</summary>
    [SugarColumn(ColumnName = "F_CREATE_USER_ID", IsNullable = true)]
    public string? CreateUserId { get; set; }

    /// <summary>创建时间</summary>
    [SugarColumn(ColumnName = "F_CREATE_TIME")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    /// <summary>修改用户 ID</summary>
    [SugarColumn(ColumnName = "F_MODIFY_USER_ID", IsNullable = true)]
    public string? ModifyUserId { get; set; }

    /// <summary>修改时间</summary>
    [SugarColumn(ColumnName = "F_MODIFY_TIME", IsNullable = true)]
    public DateTime? ModifyTime { get; set; }

    /// <summary>逻辑删除标记</summary>
    [SugarColumn(ColumnName = "F_IS_DELETED")]
    public bool IsDeleted { get; set; }
}
