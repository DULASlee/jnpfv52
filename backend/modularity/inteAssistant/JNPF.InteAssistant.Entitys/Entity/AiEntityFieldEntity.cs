using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// Domain 1 read model for entity-field design projection.
/// </summary>
[SugarTable("ai_entity_field", TableDescription = "AI实体字段设计读模型")]
public class AiEntityFieldEntity
{
    [SugarColumn(ColumnName = "F_Id", IsPrimaryKey = true)]
    public string Id { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_TenantId")]
    public string TenantId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_ProjectId")]
    public string ProjectId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_PIPELINE_ID")]
    public string PipelineId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_SchemaVersion")]
    public string SchemaVersion { get; set; } = "entity-field.v1";

    [SugarColumn(ColumnName = "F_ProjectionHash")]
    public string ProjectionHash { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_SourceFragmentId")]
    public string SourceFragmentId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_SourceDdlFragmentId", IsNullable = true)]
    public string? SourceDdlFragmentId { get; set; }

    [SugarColumn(ColumnName = "F_EntityName")]
    public string EntityName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_EntityDisplayName", IsNullable = true)]
    public string? EntityDisplayName { get; set; }

    [SugarColumn(ColumnName = "F_TableName")]
    public string TableName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_FieldName")]
    public string FieldName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_PropertyName")]
    public string PropertyName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_DbColumnName")]
    public string DbColumnName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_CSharpType")]
    public string CSharpType { get; set; } = "string";

    [SugarColumn(ColumnName = "F_SqlType")]
    public string SqlType { get; set; } = "NVARCHAR(255)";

    [SugarColumn(ColumnName = "F_IsRequired")]
    public bool IsRequired { get; set; }

    [SugarColumn(ColumnName = "F_IsPrimaryKey")]
    public bool IsPrimaryKey { get; set; }

    [SugarColumn(ColumnName = "F_IsNullable")]
    public bool IsNullable { get; set; } = true;

    [SugarColumn(ColumnName = "F_IsIdentity")]
    public bool IsIdentity { get; set; }

    [SugarColumn(ColumnName = "F_References", IsNullable = true)]
    public string? References { get; set; }

    [SugarColumn(ColumnName = "F_ReferencesTable", IsNullable = true)]
    public string? ReferencesTable { get; set; }

    [SugarColumn(ColumnName = "F_ReferencesColumn", IsNullable = true)]
    public string? ReferencesColumn { get; set; }

    [SugarColumn(ColumnName = "F_CreatorTime")]
    public DateTime CreatorTime { get; set; } = DateTime.UtcNow;

    [SugarColumn(ColumnName = "F_LastModifyTime", IsNullable = true)]
    public DateTime? LastModifyTime { get; set; }

    [SugarColumn(ColumnName = "F_DeleteMark")]
    public bool DeleteMark { get; set; }
}
