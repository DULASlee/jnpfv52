using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Entity;
using SqlSugar;

namespace JNPF.InteAssistant.Codegen.EntityDesign;

/// <summary>
/// Persists <see cref="EntityDesignProjection"/> to the <c>ai_entity_field</c> CQRS Read Model.
/// Uses SqlSugar <c>Storageable</c> for batch upsert by triple-key + EntityName + FieldName.
///
/// <para>
/// R12: Every row is isolated by (TenantId, ProjectId, PipelineId).
/// The unique index UX_ai_entity_field_triple_field enforces one row per (tenant, project, pipeline, entity, field).
/// </para>
/// </summary>
public sealed class EntityDesignRepository : ITransient
{
    private readonly ISqlSugarClient _db;

    public EntityDesignRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    /// <summary>
    /// Batch upsert projection fields to <c>ai_entity_field</c>.
    /// Insert for new rows; update mutable columns (ProjectionHash, types, nullability, FK refs,
    /// LastModifyTime) for existing rows identified by the unique business key.
    /// </summary>
    public async Task PersistAsync(EntityDesignProjection projection, CancellationToken ct = default)
    {
        if (projection.Fields.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var entities = projection.Fields.Select(f => MapToEntity(f, now)).ToList();

        // SqlSugar Storageable: splits entities into InsertList / UpdateList based on
        // whether a row with matching business key already exists in the DB.
        var storage = _db.Storageable(entities)
            .WhereColumns(it => new { it.TenantId, it.ProjectId, it.PipelineId, it.EntityName, it.FieldName })
            .ToStorage();

        await storage.AsInsertable.ExecuteCommandAsync(ct);

        // Update only mutable columns; preserve Id, identity columns, and CreatorTime.
        await storage.AsUpdateable
            .IgnoreColumns(it => new
            {
                it.Id,
                it.TenantId,
                it.ProjectId,
                it.PipelineId,
                it.EntityName,
                it.FieldName,
                it.CreatorTime,
            })
            .ExecuteCommandAsync(ct);
    }

    private static AiEntityFieldEntity MapToEntity(EntityFieldDesign f, DateTime now) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        TenantId = f.TenantId,
        ProjectId = f.ProjectId,
        PipelineId = f.PipelineId,
        SchemaVersion = f.SchemaVersion,
        ProjectionHash = f.ProjectionHash,
        SourceFragmentId = f.SourceFragmentId,
        SourceDdlFragmentId = string.IsNullOrWhiteSpace(f.SourceDdlFragmentId) ? null : f.SourceDdlFragmentId,
        EntityName = f.EntityName,
        EntityDisplayName = string.IsNullOrWhiteSpace(f.EntityDisplayName) ? null : f.EntityDisplayName,
        TableName = f.TableName,
        FieldName = f.FieldName,
        PropertyName = f.PropertyName,
        DbColumnName = f.DbColumnName,
        CSharpType = f.CSharpType,
        SqlType = f.SqlType,
        IsRequired = f.IsRequired,
        IsPrimaryKey = f.IsPrimaryKey,
        IsNullable = f.IsNullable,
        IsIdentity = f.IsIdentity,
        References = f.References,
        ReferencesTable = f.ReferencesTable,
        ReferencesColumn = f.ReferencesColumn,
        CreatorTime = now,
        LastModifyTime = now,
        DeleteMark = false,
    };
}
