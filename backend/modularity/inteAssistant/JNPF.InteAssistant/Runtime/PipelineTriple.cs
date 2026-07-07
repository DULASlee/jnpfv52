namespace JNPF.InteAssistant.Runtime;

/// <summary>
/// SA / IR / Skill 统一三元组血缘（对齐 20260705_SA_三元组与冻结恢复.sql）。
/// </summary>
public sealed record PipelineTriple(string TenantId, string ProjectId, long PipelineId)
{
    public long ProjectIdNumeric => long.TryParse(ProjectId, out var n) ? n : PipelineId;
}
