namespace JNPF.InteAssistant.Codegen;

/// <summary>
/// 解析 wwwroot/Template 下 .vm 物理路径。
/// </summary>
public static class VmTemplateCatalog
{
    public static string ResolvePath(string templateRoot, string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateRoot))
            throw new ArgumentException("templateRoot 不能为空", nameof(templateRoot));

        if (string.IsNullOrWhiteSpace(templateId))
            throw new ArgumentException("templateId 不能为空", nameof(templateId));

        if (!VmTemplateIds.LockedBackendTemplates.Contains(templateId, StringComparer.Ordinal))
            throw new ArgumentOutOfRangeException(nameof(templateId), $"未锁定的模板 ID: {templateId}");

        var path = Path.Combine(templateRoot, templateId.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
            throw new FileNotFoundException($"模板文件不存在: {path}", path);

        return path;
    }

    /// <summary>
    /// 默认模板根：backend/application/JNPF.API.Entry/wwwroot/Template
    /// </summary>
    public static string ResolveDefaultTemplateRoot(string? repoRoot = null)
    {
        repoRoot ??= ResolveRepoRoot();
        return Path.Combine(
            repoRoot,
            "backend",
            "application",
            "JNPF.API.Entry",
            "wwwroot",
            "Template");
    }

    public static string ResolveRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var marker = Path.Combine(dir.FullName, "backend", "application", "JNPF.API.Entry");
            if (Directory.Exists(marker))
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("无法定位 JNPF 仓库根目录（缺少 JNPF.API.Entry）");
    }
}
