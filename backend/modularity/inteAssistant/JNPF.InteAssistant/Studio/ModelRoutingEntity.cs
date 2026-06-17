using SqlSugar;

namespace JNPF.InteAssistant.Studio;

[SugarTable("BASE_AI_MODEL_ROUTING")]
public class ModelRoutingEntity
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "F_Id")]
    public long F_Id { get; set; }

    [SugarColumn(ColumnName = "F_Stage")]
    public int F_Stage { get; set; }

    [SugarColumn(ColumnName = "F_StageName")]
    public string? F_StageName { get; set; }

    [SugarColumn(ColumnName = "F_Provider")]
    public string F_Provider { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_Model")]
    public string F_Model { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_Priority")]
    public int F_Priority { get; set; } = 1;

    [SugarColumn(ColumnName = "F_MaxRetries")]
    public int F_MaxRetries { get; set; } = 3;

    [SugarColumn(ColumnName = "F_TimeoutMs")]
    public int F_TimeoutMs { get; set; } = 60000;

    [SugarColumn(ColumnName = "F_CircuitBreakerThreshold")]
    public int F_CircuitBreakerThreshold { get; set; } = 3;

    [SugarColumn(ColumnName = "F_CircuitBreakerResetMs")]
    public int F_CircuitBreakerResetMs { get; set; } = 300000;

    [SugarColumn(ColumnName = "F_Enabled")]
    public bool F_Enabled { get; set; } = true;

    [SugarColumn(ColumnName = "F_CreatorTime")]
    public DateTime F_CreatorTime { get; set; }

    [SugarColumn(ColumnName = "F_CreatorUserId")]
    public long? F_CreatorUserId { get; set; }

    [SugarColumn(ColumnName = "F_ModifyTime")]
    public DateTime? F_ModifyTime { get; set; }

    [SugarColumn(ColumnName = "F_ModifyUserId")]
    public long? F_ModifyUserId { get; set; }

    [SugarColumn(ColumnName = "F_DeleteMark")]
    public bool F_DeleteMark { get; set; }
}
