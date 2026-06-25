using SqlSugar;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// 模型供应商配置实体
/// 对应表: BASE_AI_MODEL_PROVIDER
/// </summary>
[SugarTable("BASE_AI_MODEL_PROVIDER")]
public class ModelProviderEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public long F_Id { get; set; }

    [SugarColumn(Length = 50, IsNullable = false)]
    public string F_ProviderCode { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string F_Name { get; set; }

    [SugarColumn(Length = 500, IsNullable = false)]
    public string F_BaseUrl { get; set; }

    [SugarColumn(Length = 500, IsNullable = false)]
    public string F_ApiKey { get; set; }

    [SugarColumn(Length = 100, IsNullable = true)]
    public string F_DefaultModel { get; set; }

    /// <summary>
    /// API 协议格式: openai / anthropic / ollama
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = false)]
    public string F_ApiFormat { get; set; } = "openai";

    public long F_MaxTokens { get; set; } = 1000000;

    [SugarColumn(Length = 38, IsNullable = false, DecimalDigits = 2)]
    public decimal F_Temperature { get; set; } = 0.7m;

    [SugarColumn(Length = 20, IsNullable = false)]
    public string F_Status { get; set; } = "healthy";

    public int F_Priority { get; set; } = 1;

    public bool F_Enabled { get; set; } = true;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string F_Description { get; set; }

    public DateTime? F_LastTestTime { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string F_LastTestResult { get; set; }

    public DateTime F_CreatorTime { get; set; }
    public long? F_CreatorUserId { get; set; }
    public DateTime? F_ModifyTime { get; set; }
    public long? F_ModifyUserId { get; set; }
    public bool F_DeleteMark { get; set; }
}
