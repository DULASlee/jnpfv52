namespace JNPF.InteAssistant.Runtime;

/// <summary>
/// Skill 后台线程 AsyncLocal 执行上下文（P2.5-B01）。
/// </summary>
public sealed class SkillExecutionScope : IDisposable
{
    private static readonly AsyncLocal<SkillExecutionScope?> Current = new();

    public static SkillExecutionScope? CurrentScope => Current.Value;

    public required string RunId { get; init; }
    public required string TenantId { get; init; }
    public required string ProjectId { get; init; }
    public required long PipelineId { get; init; }
    public required string SkillId { get; init; }
    public CancellationToken CancellationToken { get; init; }

    public static SkillExecutionScope Begin(
        string runId,
        string tenantId,
        string projectId,
        long pipelineId,
        string skillId,
        CancellationToken ct)
    {
        var scope = new SkillExecutionScope
        {
            RunId = runId,
            TenantId = tenantId,
            ProjectId = projectId,
            PipelineId = pipelineId,
            SkillId = skillId,
            CancellationToken = ct,
        };
        Current.Value = scope;
        return scope;
    }

    public void Dispose() => Current.Value = null;
}
