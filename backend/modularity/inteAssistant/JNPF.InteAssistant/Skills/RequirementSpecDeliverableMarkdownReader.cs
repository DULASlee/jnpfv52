using System.Security.Cryptography;
using System.Text;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.Skills;

namespace JNPF.InteAssistant.Skills;

public sealed class RequirementSpecDeliverableMarkdownReader : IRequirementSpecMarkdownReader, ITransient
{
    public async Task<(bool Exists, string? Markdown, string? ContentHash, int ContentLength)> TryReadFormalAsync(
        string tenantId,
        string projectId,
        long pipelineId,
        CancellationToken ct = default)
    {
        var deliverablePath = Path.Combine(
            StudioWorkspaceHelper.GetDeliverablesPath(tenantId, projectId, pipelineId.ToString()),
            RequirementSpecConstants.RelativePath);
        StudioWorkspaceHelper.AssertWithinDeliverables(
            deliverablePath, tenantId, projectId, pipelineId.ToString());

        if (!File.Exists(deliverablePath))
            return (false, null, null, 0);

        var markdown = await File.ReadAllTextAsync(deliverablePath, ct);
        var hash = ComputeSha256Hex(markdown);
        return (true, markdown, hash, markdown.Length);
    }

    internal static string ComputeSha256Hex(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
