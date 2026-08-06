using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.Skills;
using JNPF.InteAssistant.Entitys.Entity;
using SqlSugar;

namespace JNPF.InteAssistant.Skills;

/// <summary>SqlSugar 实现：L2 进度表 upsert（三元组唯一行）。</summary>
public sealed class PipelineS2ProgressStore : IPipelineS2ProgressStore, ITransient
{
    private readonly ISqlSugarClient _db;

    public PipelineS2ProgressStore(ISqlSugarClient db) => _db = db;

    public async Task<AiPipelineS2ProgressEntity?> TryGetAsync(
        string tenantId,
        string projectId,
        long pipelineId,
        CancellationToken ct = default)
    {
        var pipelineKey = pipelineId.ToString();
        var rows = await _db.Queryable<AiPipelineS2ProgressEntity>()
            .Where(x => x.TenantId == tenantId
                        && x.ProjectId == projectId
                        && x.PipelineId == pipelineKey
                        && (x.DeleteMark == null || x.DeleteMark == 0))
            .Take(1)
            .ToListAsync(ct);
        return rows.FirstOrDefault();
    }

    public async Task UpsertAsync(S2ProgressUpdate update, CancellationToken ct = default)
    {
        var existing = await TryGetAsync(update.TenantId, update.ProjectId, update.PipelineId, ct);

        if (existing == null)
        {
            var row = new AiPipelineS2ProgressEntity
            {
                TenantId = update.TenantId,
                ProjectId = update.ProjectId,
                PipelineId = update.PipelineId.ToString(),
                PipelineStage = (int)(update.PipelineStage ?? S2PipelineStage.GatePending),
                SpecPhase = (int)(update.SpecPhase ?? RequirementSpecPhase.Absent),
                ClarRound = update.ClarRound ?? 0,
                SpecVersion = update.SpecVersion ?? 1,
                ContentHash = update.ContentHash,
                ContentLength = update.ContentLength,
                AwaitingUser = update.AwaitingUser ?? false,
                // 显式填 NOT NULL 字段——CLDEntityBase.Create() 不填这些（只填 Id/CreatorTime/SortCode），
                // 而 BASE_AI_PIPELINE_S2_PROGRESS 的 F_DELETE_MARK/F_ENABLED_MARK 是 NOT NULL，
                // 不填会触发 SqlException「不能将值 NULL 插入列 F_DELETE_MARK」→ upsert 持续失败 →
                // orchestrator 在 UpdateS2ProgressAsync 抛异常 → 前端 SSE 永不等 done → 卡死。
                // 见 warning-20260718.json 12:44-13:28 pipeline=409 实例。
                DeleteMark = 0,
                EnabledMark = 1,
            };
            row.Create();
            // 双保险：Create() 也不覆盖 DeleteMark/EnabledMark，再确保一次非空
            row.DeleteMark ??= 0;
            row.EnabledMark ??= 1;
            await _db.Insertable(row).ExecuteCommandAsync(ct);
            return;
        }

        if (update.PipelineStage.HasValue)
            existing.PipelineStage = (int)update.PipelineStage.Value;
        if (update.SpecPhase.HasValue)
            existing.SpecPhase = (int)update.SpecPhase.Value;
        if (update.ClarRound.HasValue)
            existing.ClarRound = update.ClarRound.Value;
        if (update.SpecVersion.HasValue)
            existing.SpecVersion = update.SpecVersion.Value;
        if (update.ContentHash != null)
            existing.ContentHash = update.ContentHash;
        if (update.ContentLength.HasValue)
            existing.ContentLength = update.ContentLength;
        if (update.AwaitingUser.HasValue)
            existing.AwaitingUser = update.AwaitingUser.Value;

        existing.LastModify();
        await _db.Updateable(existing).ExecuteCommandAsync(ct);
    }
}
