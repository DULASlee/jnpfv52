namespace JNPF.InteAssistant.Codegen;

/// <summary>
/// workspace/generated 路径约定（A1 §2）。
/// </summary>
public static class CodegenWorkspacePaths
{
    public static string ResolveRepoRoot() => VmTemplateCatalog.ResolveRepoRoot();

    public static string ResolveBackendRoot(string tenantId, string projectId, string? repoRoot = null)
    {
        repoRoot ??= ResolveRepoRoot();
        return Path.Combine(repoRoot, "workspace", "generated", tenantId, projectId, "backend");
    }

    public static string ToArtifactRootRelative(string tenantId, string projectId) =>
        $"workspace/generated/{tenantId}/{projectId}/backend";
}
