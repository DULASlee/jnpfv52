using SqlSugar;

namespace JNPF.InteAssistant.Studio;

[SugarTable("BASE_AI_MCP_CONFIG")]
public class McpConfigEntity
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "F_Id")]
    public long F_Id { get; set; }

    [SugarColumn(ColumnName = "F_Name")]
    public string F_Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_Endpoint")]
    public string F_Endpoint { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_Protocol")]
    public string F_Protocol { get; set; } = "sse";

    [SugarColumn(ColumnName = "F_AuthType")]
    public string? F_AuthType { get; set; }

    [SugarColumn(ColumnName = "F_AuthConfig")]
    public string? F_AuthConfig { get; set; }

    [SugarColumn(ColumnName = "F_Status")]
    public string F_Status { get; set; } = "disconnected";

    [SugarColumn(ColumnName = "F_LastTestTime")]
    public DateTime? F_LastTestTime { get; set; }

    [SugarColumn(ColumnName = "F_LastTestResult")]
    public string? F_LastTestResult { get; set; }

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
