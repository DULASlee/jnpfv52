using JNPF.InteAssistant.Entitys.Dto.Skills;
using JNPF.InteAssistant.Entitys.Entity;

namespace JNPF.InteAssistant.Skills;

/// <summary>L2 S2 流水线进度存储（每 pipeline 一行，编排器唯一 Write 入口）。</summary>
public interface IPipelineS2ProgressStore
{
    Task<AiPipelineS2ProgressEntity?> TryGetAsync(
        string tenantId,
        string projectId,
        long pipelineId,
        CancellationToken ct = default);

    Task UpsertAsync(S2ProgressUpdate update, CancellationToken ct = default);
}
