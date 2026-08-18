using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Common;
using JNPF.InteAssistant.Entitys.Entity;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// 将 BASE_AI_PIPELINE 同步到 BASE_AI_GENERATED_PROJECT，供「已生成系统」与多任务恢复使用。
/// </summary>
public interface IGeneratedProjectRegistry
{
    Task UpsertFromPipelineAsync(long pipelineId, string tenantId, long userId, string? userName = null);

    Task UpdateDeliveryArtifactsAsync(long pipelineId, string? sandboxUrl, string? sourceZipUrl);
}

public sealed class GeneratedProjectRegistry : IGeneratedProjectRegistry, ITransient
{
    private readonly ISqlSugarClient _db;

    public GeneratedProjectRegistry(ISqlSugarClient db) => _db = db;

    public async Task UpsertFromPipelineAsync(long pipelineId, string tenantId, long userId, string? userName = null)
    {
        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .FirstAsync(x => x.Id == pipelineId.ToString());
        if (pipeline == null) return;

        var stageCode = pipeline.CurrentStage ?? PipelineStage.Requirement;
        var stageNum = MapStageNumber(stageCode);
        var now = DateTime.Now;
        var existing = await _db.Queryable<GeneratedProjectEntity>()
            .FirstAsync(x => x.F_Id == pipelineId && x.F_TenantId == tenantId);

        if (existing == null)
        {
            await _db.Insertable(new GeneratedProjectEntity
            {
                F_Id = pipelineId,
                F_TenantId = tenantId,
                F_UserId = userId,
                F_ProjectName = string.IsNullOrWhiteSpace(pipeline.Name) ? $"流水线 #{pipelineId}" : pipeline.Name,
                F_Description = pipeline.Name,
                F_PipelineStatus = stageCode,
                F_CurrentStage = stageNum,
                F_IsRead = true,
                F_UpdateCount = 0,
                F_CreatorTime = pipeline.CreatorTime ?? now,
                F_CreatorUserId = userId,
                F_CreatorUserName = userName,
                F_ModifyTime = now,
            }).ExecuteCommandAsync();
            return;
        }

        existing.F_ProjectName = string.IsNullOrWhiteSpace(pipeline.Name) ? existing.F_ProjectName : pipeline.Name;
        existing.F_PipelineStatus = stageCode;
        existing.F_CurrentStage = stageNum;
        existing.F_ModifyTime = now;
        await _db.Updateable(existing).ExecuteCommandAsync();
    }

    public async Task UpdateDeliveryArtifactsAsync(long pipelineId, string? sandboxUrl, string? sourceZipUrl)
    {
        var row = await _db.Queryable<GeneratedProjectEntity>().FirstAsync(x => x.F_Id == pipelineId);
        if (row == null) return;

        if (!string.IsNullOrWhiteSpace(sandboxUrl)) row.F_SandboxUrl = sandboxUrl;
        if (!string.IsNullOrWhiteSpace(sourceZipUrl)) row.F_SourceZipUrl = sourceZipUrl;
        row.F_ModifyTime = DateTime.Now;
        await _db.Updateable(row).ExecuteCommandAsync();
    }

    private static int MapStageNumber(string stageCode) => stageCode switch
    {
        PipelineStage.Requirement => 1,
        PipelineStage.Architecture => 2,
        PipelineStage.Design => 3,
        PipelineStage.Development => 4,
        PipelineStage.Delivery => 5,
        _ => 1,
    };
}
