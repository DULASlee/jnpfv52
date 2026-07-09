namespace JNPF.InteAssistant.Codegen.EntityDesign;

/// <summary>投影字段来源（用于 R2 跨层一致性门控判断）。</summary>
public enum FieldSource
{
    /// <summary>来自 Skeleton entityDrafts.fields（主源）。</summary>
    Skeleton,
    /// <summary>来自 DDL 列合成（Skeleton 无 fields 时的兜底源）。</summary>
    DdlFallback,
}

public sealed class EntityFieldDesign
{
    public string TenantId { get; init; } = string.Empty;
    public string ProjectId { get; init; } = string.Empty;
    public string PipelineId { get; init; } = string.Empty;
    public string SchemaVersion { get; init; } = EntityDesignProjector.SchemaVersion;
    public string ProjectionHash { get; set; } = string.Empty;
    public string SourceFragmentId { get; init; } = string.Empty;
    public string SourceDdlFragmentId { get; init; } = string.Empty;
    /// <summary>字段来源：Skeleton（主源）或 DdlFallback（兜底）。</summary>
    public FieldSource Source { get; init; } = FieldSource.Skeleton;
    public string EntityName { get; init; } = string.Empty;
    public string EntityDisplayName { get; init; } = string.Empty;
    public string TableName { get; init; } = string.Empty;
    public string FieldName { get; init; } = string.Empty;
    public string PropertyName { get; init; } = string.Empty;
    public string DbColumnName { get; init; } = string.Empty;
    public string CSharpType { get; init; } = "string";
    public string SqlType { get; init; } = "NVARCHAR(255)";
    public bool IsRequired { get; init; }
    public bool IsPrimaryKey { get; init; }
    public bool IsNullable { get; init; } = true;
    public bool IsIdentity { get; init; }
    public string? References { get; init; }
    public string? ReferencesTable { get; init; }
    public string? ReferencesColumn { get; init; }
    public string? FieldDescription { get; init; }
    public string? EntityDescription { get; init; }
}

public sealed class EntityDesignProjection
{
    public string TenantId { get; init; } = string.Empty;
    public string ProjectId { get; init; } = string.Empty;
    public string PipelineId { get; init; } = string.Empty;
    public string SchemaVersion { get; init; } = EntityDesignProjector.SchemaVersion;
    public string ProjectionHash { get; init; } = string.Empty;
    public IReadOnlyList<EntityFieldDesign> Fields { get; init; } = Array.Empty<EntityFieldDesign>();

    public IReadOnlyList<EntityFieldDesign> ForEntity(string entityName)
    {
        return Fields
            .Where(x => string.Equals(x.EntityName, entityName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.IsPrimaryKey)
            .ThenBy(x => x.FieldName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<string> TableNames()
    {
        return Fields
            .Select(x => x.TableName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

public sealed class EntityDesignProjectionOptions
{
    public required string TenantId { get; init; }
    public required string ProjectId { get; init; }
    public required string PipelineId { get; init; }
}
