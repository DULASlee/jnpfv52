using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Entitys.Ir.Contracts;
using JNPF.InteAssistant.Entitys.Ir.Naming;
using JNPF.InteAssistant.Skills;

namespace JNPF.InteAssistant.Codegen.EntityDesign;

public static class EntityDesignProjector
{
    public const string SchemaVersion = "entity-field.v1";

    private static readonly JsonSerializerOptions CanonicalJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static EntityDesignProjection Project(IrSnapshot snapshot, EntityDesignProjectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(options);

        var skeletonSnap = snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable)
            ?? snapshot.Find(IrFragmentTypes.Skeleton);
        if (skeletonSnap == null)
            return Empty(options);

        var skeleton = SkeletonPayload.Parse(skeletonSnap.Payload);
        var ddlSnap = snapshot.Find(IrFragmentTypes.DDL, IrStabilityStates.Stable)
            ?? snapshot.Find(IrFragmentTypes.DDL);
        var ddl = ddlSnap == null ? new DdlPayload() : DdlPayload.Parse(ddlSnap.Payload);

        var ddlTables = ddl.Tables.ToDictionary(
            x => ResolveTableKey(x),
            x => x,
            StringComparer.OrdinalIgnoreCase);

        var rows = new List<EntityFieldDesign>();
        foreach (var entity in skeleton.EntityDrafts)
        {
            var tableName = EntityNamingPolicy.ResolveTableName(entity.TableName, entity.EntityName);
            ddlTables.TryGetValue(entity.EntityName, out var ddlTable);
            ddlTable ??= ddl.Tables.FirstOrDefault(x => string.Equals(x.TableName, tableName, StringComparison.OrdinalIgnoreCase));

            // 主源：Skeleton entityDrafts.fields
            var fields = entity.Fields;
            var fromDdlFallback = false;
            // 兜底源：当 Skeleton 无字段但 DDL 有列时，从 DDL 列合成字段定义
            // （LLM 常见产出：Skeleton 只给实体名，列由 DDL 承载）
            if (fields.Count == 0 && ddlTable is { Columns.Count: > 0 })
            {
                fields = SynthesizeFieldsFromDdl(ddlTable);
                fromDdlFallback = true;
            }

            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field.Name))
                    continue;

                var propertyName = EntityNamingPolicy.ToPropertyName(field.Name);
                var defaultColumnName = EntityNamingPolicy.ToDbColumnName(propertyName);
                var ddlColumn = FindDdlColumn(ddlTable, field.Name, propertyName, defaultColumnName);
                var dbColumnName = ddlColumn?.Name;
                if (string.IsNullOrWhiteSpace(dbColumnName))
                    dbColumnName = defaultColumnName;

                var csharpType = EntityTypeMapper.NormalizeNetType(field.Type);
                var sqlType = ddlColumn?.DataType;
                if (string.IsNullOrWhiteSpace(sqlType))
                    sqlType = EntityTypeMapper.ToSqlType(csharpType);

                var fk = FindForeignKey(ddlTable, dbColumnName, field.References);
                var reference = ResolveReference(field.References, fk);

                rows.Add(new EntityFieldDesign
                {
                    TenantId = options.TenantId,
                    ProjectId = options.ProjectId,
                    PipelineId = options.PipelineId,
                    SourceFragmentId = skeletonSnap.FragmentId,
                    SourceDdlFragmentId = ddlSnap?.FragmentId ?? string.Empty,
                    Source = fromDdlFallback ? FieldSource.DdlFallback : FieldSource.Skeleton,
                    EntityName = entity.EntityName,
                    EntityDisplayName = string.IsNullOrWhiteSpace(entity.DisplayName) ? entity.EntityName : entity.DisplayName,
                    TableName = ddlTable?.TableName ?? tableName,
                    FieldName = field.Name,
                    PropertyName = propertyName,
                    DbColumnName = dbColumnName,
                    CSharpType = EntityTypeMapper.ToNetType(sqlType),
                    SqlType = sqlType,
                    IsRequired = field.Required,
                    IsPrimaryKey = field.PrimaryKey || ddlColumn?.IsPrimaryKey == true,
                    IsNullable = ddlColumn?.IsNullable ?? !field.Required,
                    IsIdentity = ddlColumn?.IsIdentity ?? field.PrimaryKey,
                    References = reference.references,
                    ReferencesTable = reference.referencesTable,
                    ReferencesColumn = reference.referencesColumn,
                    FieldDescription = ddlColumn?.Description,
                    EntityDescription = entity.Description,
                });
            }
        }

        var hash = ComputeHash(rows);
        foreach (var row in rows)
            row.ProjectionHash = hash;

        return new EntityDesignProjection
        {
            TenantId = options.TenantId,
            ProjectId = options.ProjectId,
            PipelineId = options.PipelineId,
            ProjectionHash = hash,
            Fields = rows
                .OrderBy(x => x.EntityName, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(x => x.IsPrimaryKey)
                .ThenBy(x => x.FieldName, StringComparer.OrdinalIgnoreCase)
                .ToList(),
        };
    }

    private static EntityDesignProjection Empty(EntityDesignProjectionOptions options) => new()
    {
        TenantId = options.TenantId,
        ProjectId = options.ProjectId,
        PipelineId = options.PipelineId,
        ProjectionHash = ComputeHash(Array.Empty<EntityFieldDesign>()),
        Fields = Array.Empty<EntityFieldDesign>(),
    };

    private static string ResolveTableKey(TableDefinition table)
    {
        return !string.IsNullOrWhiteSpace(table.EntityName) ? table.EntityName : table.TableName;
    }

    /// <summary>
    /// 当 Skeleton entityDrafts 无 fields 时，从 DDL 列合成字段定义（兜底源）。
    /// 系统列（租户/流程）不在此过滤——由 TemplateContextBuilder 消费端统一过滤。
    /// </summary>
    private static List<FieldDraftContract> SynthesizeFieldsFromDdl(TableDefinition ddlTable)
    {
        var fields = new List<FieldDraftContract>(ddlTable.Columns.Count);
        foreach (var col in ddlTable.Columns)
        {
            if (string.IsNullOrWhiteSpace(col.Name))
                continue;

            fields.Add(new FieldDraftContract
            {
                Name = col.Name,
                Type = EntityTypeMapper.ToNetType(col.DataType),
                Required = !col.IsNullable && !col.IsPrimaryKey,
                PrimaryKey = col.IsPrimaryKey,
            });
        }

        return fields;
    }

    private static ColumnDefinition? FindDdlColumn(
        TableDefinition? table,
        string fieldName,
        string propertyName,
        string defaultColumnName)
    {
        if (table == null)
            return null;

        return table.Columns.FirstOrDefault(c =>
            string.Equals(c.Name, fieldName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(c.Name, propertyName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(c.Name, defaultColumnName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(EntityNamingPolicy.ToPropertyName(c.Name), propertyName, StringComparison.OrdinalIgnoreCase));
    }

    private static ForeignKeyDefinition? FindForeignKey(TableDefinition? table, string dbColumnName, string? references)
    {
        if (table == null)
            return null;

        return table.ForeignKeys.FirstOrDefault(fk =>
            string.Equals(fk.ColumnName, dbColumnName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(EntityNamingPolicy.ToDbColumnName(fk.ColumnName), dbColumnName, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(references)
                && references.StartsWith(fk.ReferencesTable, StringComparison.OrdinalIgnoreCase)));
    }

    private static (string? references, string? referencesTable, string? referencesColumn) ResolveReference(
        string? skeletonReference,
        ForeignKeyDefinition? fk)
    {
        if (fk != null)
        {
            return (
                $"{fk.ReferencesTable}.{fk.ReferencesColumn}",
                fk.ReferencesTable,
                fk.ReferencesColumn);
        }

        if (string.IsNullOrWhiteSpace(skeletonReference))
            return (null, null, null);

        var parts = skeletonReference.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return (
            skeletonReference,
            parts.Length > 0 ? EntityNamingPolicy.ResolveTableName(null, parts[0]) : null,
            parts.Length > 1 ? parts[1] : "id");
    }

    private static string ComputeHash(IReadOnlyList<EntityFieldDesign> rows)
    {
        var canonical = rows
            .OrderBy(x => x.EntityName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.FieldName, StringComparer.OrdinalIgnoreCase)
            .Select(x => new
            {
                x.EntityName,
                x.TableName,
                x.FieldName,
                x.PropertyName,
                x.DbColumnName,
                x.CSharpType,
                x.SqlType,
                x.IsRequired,
                x.IsPrimaryKey,
                x.IsNullable,
                x.IsIdentity,
                x.References,
                x.ReferencesTable,
                x.ReferencesColumn,
            });
        var json = JsonSerializer.Serialize(canonical, CanonicalJsonOptions);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
