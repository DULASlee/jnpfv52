namespace JNPF.InteAssistant.Skills;

/// <summary>读取 L1 交付物 02-requirement-spec.md（正文唯一源）。</summary>
public interface IRequirementSpecMarkdownReader
{
    Task<(bool Exists, string? Markdown, string? ContentHash, int ContentLength)> TryReadFormalAsync(
        string tenantId,
        string projectId,
        long pipelineId,
        CancellationToken ct = default);
}
