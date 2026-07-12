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
    /// 三元组范围内是否已有投影字段（25 §6 下游消费契约：设计前须可读 ai_entity_field）。
    /// </summary>
    public async Task<int> CountFieldsAsync(
        string tenantId, string projectId, string pipelineId, CancellationToken ct = default)
    {
        return await _db.Queryable<AiEntityFieldEntity>()
            .Where(x => x.TenantId == tenantId
                        && x.ProjectId == projectId
                        && x.PipelineId == pipelineId
                        && !x.DeleteMark)
            .CountAsync(ct);
    }

    /// <summary>列出投影字段（UI/Tester 消费，禁止各自 parse IR JSON 当唯一源）。</summary>
    public async Task<List<AiEntityFieldEntity>> ListFieldsAsync(
        string tenantId, string projectId, string pipelineId, CancellationToken ct = default)
    {
        return await _db.Queryable<AiEntityFieldEntity>()
            .Where(x => x.TenantId == tenantId
                        && x.ProjectId == projectId
                        && x.PipelineId == pipelineId
                        && !x.DeleteMark)
            .OrderBy(x => x.EntityName)
            .OrderBy(x => x.FieldName)
            .ToListAsync(ct);
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
        var tenantId = projection.TenantId;
        var projectId = projection.ProjectId;
        var pipelineId = projection.PipelineId;

        // 避免 Storageable + 匿名 WhereColumns：部分 SqlSugar 版本会生成错误列名 SQL。
        // 同三元组先软删再插入，保持 UX_ai_entity_field_triple_field 唯一约束可满足。
        // H2: 软删+插入包裹事务，中途崩溃回滚软删除，保证原子性。
        _db.Ado.BeginTran();
        try
        {
            await _db.Updateable<AiEntityFieldEntity>()
                .SetColumns(x => x.DeleteMark == true)
                .SetColumns(x => x.LastModifyTime == now)
                .Where(x => x.TenantId == tenantId
                            && x.ProjectId == projectId
                            && x.PipelineId == pipelineId
                            && !x.DeleteMark)
                .ExecuteCommandAsync(ct);

            await _db.Insertable(entities).ExecuteCommandAsync(ct);

            _db.Ado.CommitTran();
        }
        catch
        {
            _db.Ado.RollbackTran();
            throw;
        }
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
