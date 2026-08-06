using JNPF.InteAssistant.Entitys.Dto.Skills;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 需求说明书 Phase 唯一读模型（ADF P3）。
/// 编排器/前端 MUST 通过本接口解析 Phase，禁止 scattered HasEventAsync。
/// </summary>
public interface IRequirementSpecStateResolver
{
    Task<RequirementSpecSnapshot> ResolveAsync(
        string tenantId,
        string projectId,
        long pipelineId,
        CancellationToken ct = default);

    /// <summary>是否加载 02 全文（预览/Finalize）；默认 false 仅 metadata。</summary>
    Task<RequirementSpecSnapshot> ResolveAsync(
        string tenantId,
        string projectId,
        long pipelineId,
        bool includeFormalMarkdown,
        CancellationToken ct = default);
}
