using JNPF.Common.Configuration;
using JNPF.InteAssistant.Interfaces;
using System.IO.Compression;

namespace JNPF.InteAssistant;

/// <summary>
/// AI 开发工作区路径工具类
/// 纯函数、零状态、零依赖。所有路径计算集中于此。
///
/// R12 三元组铁律（2026-07-07，宪法级）：
///   路径公式 MUST 四层 {SystemPath}/StudioWorkspace/{tenantId}/{projectId}/{pipelineId}/
///   greenfield 自锚定（projectId == pipelineId）走老三层路径，保持历史数据兼容
///   bugfix/enhancement（projectId != pipelineId）走新四层路径
///   详见 .cursor/rules/triple-key-iron-law.mdc
/// </summary>
public static class StudioWorkspaceHelper
{
    // ─── 子目录名常量 ───

    private const string IrDir = "ir";
    private const string GeneratedDir = "generated";
    private const string WorkspaceDir = "workspace";
    private const string ArtifactsDir = "artifacts";
    private const string DeliverablesDir = "deliverables";

    // ─── R12 自锚定检测 ───

    /// <summary>
    /// 检测 pipeline 是否处于 greenfield 自锚定状态（projectId == pipelineId）。
    /// 自锚定 pipeline 走老三层路径 {tenantId}/{pipelineId}/ 保持历史数据兼容；
    /// 非自锚定（bugfix/enhancement）走新四层路径 {tenantId}/{projectId}/{pipelineId}/。
    /// </summary>
    /// <param name="projectId">项目 ID（来自 AiPipelineEntity.F_PROJECT_ID）</param>
    /// <param name="pipelineId">流水线 ID（来自 AiPipelineEntity.F_ID）</param>
    public static bool IsSelfAnchored(string projectId, string pipelineId)
    {
        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(pipelineId))
            return true; // 缺值视为自锚定，走老路径（防御）

        return string.Equals(projectId, pipelineId, StringComparison.OrdinalIgnoreCase);
    }

    // ─── 路径计算（R12 三元组主入口）───

    /// <summary>
    /// 【R12 主入口】获取流水线工作区根路径（三元组）。
    /// 路径规则：
    ///   - 自锚定（projectId == pipelineId，即 greenfield 默认）：
    ///     {SystemPath}/StudioWorkspace/{tenantId}/{pipelineId}/  （老三层路径，向后兼容）
    ///   - 非自锚定（bugfix / enhancement / fork）：
    ///     {SystemPath}/StudioWorkspace/{tenantId}/{projectId}/{pipelineId}/  （新四层路径）
    /// </summary>
    public static string GetPipelinePath(string tenantId, string projectId, string pipelineId)
    {
        if (IsSelfAnchored(projectId, pipelineId))
        {
            // 自锚定：走老三层路径，保持历史数据兼容（pipeline 311 等）
            return Path.Combine(
                KeyVariable.SystemPath,
                KeyVariable.StudioWorkspaceRoot,
                tenantId,
                pipelineId);
        }

        // R12 新四层路径：bugfix/enhancement/fork 必须独立 projectId 层
        return Path.Combine(
            KeyVariable.SystemPath,
            KeyVariable.StudioWorkspaceRoot,
            tenantId,
            projectId,
            pipelineId);
    }

    /// <summary>
    /// 【向后兼容】获取流水线工作区根路径（二元组，自锚定 greenfield 模式）。
    /// 等价于 GetPipelinePath(tenantId, pipelineId, pipelineId)。
    /// [Obsolete] 新代码 MUST 使用三元组重载；本重载仅用于历史调用点渐进迁移。
    /// </summary>
    [Obsolete("R12 三元组铁律：新代码 MUST 使用 GetPipelinePath(tenantId, projectId, pipelineId) 三元重载。本二元重载仅作向后兼容，自锚定 pipeline 隐式等价 projectId=pipelineId。")]
    public static string GetPipelinePath(string tenantId, string pipelineId)
    {
        // 自锚定：projectId 隐式等于 pipelineId，走老三层路径
        return Path.Combine(
            KeyVariable.SystemPath,
            KeyVariable.StudioWorkspaceRoot,
            tenantId,
            pipelineId);
    }

    /// <summary>
    /// 【R12 主入口】获取四个子目录完整路径（三元组）。
    /// </summary>
    public static (string Ir, string Generated, string Workspace, string Artifacts)
        GetPipelineSubPaths(string tenantId, string projectId, string pipelineId)
    {
        var root = GetPipelinePath(tenantId, projectId, pipelineId);
        return (
            Path.Combine(root, IrDir),
            Path.Combine(root, GeneratedDir),
            Path.Combine(root, WorkspaceDir),
            Path.Combine(root, ArtifactsDir)
        );
    }

    /// <summary>
    /// 【向后兼容】获取四个子目录完整路径（二元组）。
    /// [Obsolete] 新代码 MUST 使用三元组重载。
    /// </summary>
    [Obsolete("R12 三元组铁律：新代码 MUST 使用 GetPipelineSubPaths(tenantId, projectId, pipelineId) 三元重载。")]
    public static (string Ir, string Generated, string Workspace, string Artifacts)
        GetPipelineSubPaths(string tenantId, string pipelineId)
    {
        var root = GetPipelinePath(tenantId, pipelineId);
        return (
            Path.Combine(root, IrDir),
            Path.Combine(root, GeneratedDir),
            Path.Combine(root, WorkspaceDir),
            Path.Combine(root, ArtifactsDir)
        );
    }

    // ─── 目录生命周期 ───

    /// <summary>【R12 主入口】deliverables/ 子目录（三元组）</summary>
    public static string GetDeliverablesPath(string tenantId, string projectId, string pipelineId) =>
        Path.Combine(GetPipelinePath(tenantId, projectId, pipelineId), DeliverablesDir);

    /// <summary>【向后兼容】deliverables/ 子目录（二元组）</summary>
    [Obsolete("R12 三元组铁律：新代码 MUST 使用 GetDeliverablesPath(tenantId, projectId, pipelineId) 三元重载。")]
    public static string GetDeliverablesPath(string tenantId, string pipelineId) =>
        Path.Combine(GetPipelinePath(tenantId, pipelineId), DeliverablesDir);

    /// <summary>【R12 主入口】创建子目录（三元组，幂等）</summary>
    public static void EnsureDirectories(string tenantId, string projectId, string pipelineId)
    {
        var (ir, generated, workspace, artifacts) = GetPipelineSubPaths(tenantId, projectId, pipelineId);
        Directory.CreateDirectory(ir);
        Directory.CreateDirectory(generated);
        Directory.CreateDirectory(workspace);
        Directory.CreateDirectory(artifacts);
        EnsureDeliverablesDirectory(tenantId, projectId, pipelineId);
    }

    /// <summary>【向后兼容】创建子目录（二元组）</summary>
    [Obsolete("R12 三元组铁律：新代码 MUST 使用 EnsureDirectories(tenantId, projectId, pipelineId) 三元重载。")]
    public static void EnsureDirectories(string tenantId, string pipelineId)
    {
        var (ir, generated, workspace, artifacts) = GetPipelineSubPaths(tenantId, pipelineId);
        Directory.CreateDirectory(ir);
        Directory.CreateDirectory(generated);
        Directory.CreateDirectory(workspace);
        Directory.CreateDirectory(artifacts);
        EnsureDeliverablesDirectory(tenantId, pipelineId);
    }

    /// <summary>【R12 主入口】确保 deliverables 目录存在（三元组）</summary>
    public static void EnsureDeliverablesDirectory(string tenantId, string projectId, string pipelineId) =>
        Directory.CreateDirectory(GetDeliverablesPath(tenantId, projectId, pipelineId));

    /// <summary>【向后兼容】确保 deliverables 目录存在（二元组）</summary>
    [Obsolete("R12 三元组铁律：新代码 MUST 使用 EnsureDeliverablesDirectory(tenantId, projectId, pipelineId) 三元重载。")]
    public static void EnsureDeliverablesDirectory(string tenantId, string pipelineId) =>
        Directory.CreateDirectory(GetDeliverablesPath(tenantId, pipelineId));

    /// <summary>【R12 主入口】交付物路径安全校验（三元组，防路径穿越）</summary>
    public static void AssertWithinDeliverables(string filePath, string tenantId, string projectId, string pipelineId)
    {
        var root = GetDeliverablesPath(tenantId, projectId, pipelineId);
        var resolvedRoot = Path.GetFullPath(root)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var resolvedTarget = Path.GetFullPath(filePath);
        if (!resolvedTarget.StartsWith(resolvedRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"禁止访问 deliverables 工作区外路径: {filePath}");
    }

    /// <summary>【向后兼容】交付物路径安全校验（二元组）</summary>
    [Obsolete("R12 三元组铁律：新代码 MUST 使用 AssertWithinDeliverables(filePath, tenantId, projectId, pipelineId) 三元重载。")]
    public static void AssertWithinDeliverables(string filePath, string tenantId, string pipelineId)
    {
        var root = GetDeliverablesPath(tenantId, pipelineId);
        var resolvedRoot = Path.GetFullPath(root)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var resolvedTarget = Path.GetFullPath(filePath);
        if (!resolvedTarget.StartsWith(resolvedRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"禁止访问 deliverables 工作区外路径: {filePath}");
    }

    // ─── 路径安全校验 ───

    /// <summary>
    /// 【R12 主入口】断言目标路径在工作区内（三元组，防路径穿越）。
    /// 使用 Path.GetFullPath 解析 ../ 后再做前缀匹配。
    /// </summary>
    public static void AssertWithinWorkspace(string filePath, string tenantId, string projectId, string pipelineId)
    {
        var workspaceRoot = GetPipelinePath(tenantId, projectId, pipelineId);
        var resolvedWorkspace = Path.GetFullPath(workspaceRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        var resolvedTarget = Path.GetFullPath(filePath);

        if (!resolvedTarget.StartsWith(resolvedWorkspace, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"AI 流水线禁止写入工作区外路径: {filePath}. 允许前缀: {resolvedWorkspace}");
        }
    }

    /// <summary>【向后兼容】断言目标路径在工作区内（二元组）</summary>
    [Obsolete("R12 三元组铁律：新代码 MUST 使用 AssertWithinWorkspace(filePath, tenantId, projectId, pipelineId) 三元重载。")]
    public static void AssertWithinWorkspace(string filePath, string tenantId, string pipelineId)
    {
        var workspaceRoot = GetPipelinePath(tenantId, pipelineId);
        var resolvedWorkspace = Path.GetFullPath(workspaceRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        var resolvedTarget = Path.GetFullPath(filePath);

        if (!resolvedTarget.StartsWith(resolvedWorkspace, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"AI 流水线禁止写入工作区外路径: {filePath}. 允许前缀: {resolvedWorkspace}");
        }
    }

    // ─── 目录→文件列表转换（供 SandboxManager 使用）───

    /// <summary>
    /// 递归读取目录下所有文件，转换为 GeneratedFile 列表。
    /// 返回空列表（非 null）当目录不存在或为空时。
    /// </summary>
    public static List<GeneratedFile> ReadFilesFromDirectory(string directoryPath)
    {
        var files = new List<GeneratedFile>();

        if (!Directory.Exists(directoryPath))
            return files;

        foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(directoryPath, filePath)
                .Replace(Path.DirectorySeparatorChar, '/');
            var bytes = File.ReadAllBytes(filePath);

            files.Add(new GeneratedFile
            {
                FilePath = relativePath,
                Content = System.Text.Encoding.UTF8.GetString(bytes),
                FileType = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant()
            });
        }

        return files;
    }

    // ─── 交付打包 ───

    /// <summary>
    /// 【R12 主入口】将 generated/ 目录打包为 zip，返回 zip 文件完整路径（三元组）。
    /// zip 文件放在 artifacts/ 子目录中。
    /// </summary>
    /// <exception cref="InvalidOperationException">generated/ 目录为空时抛出</exception>
    public static string CreateDeliveryZip(string tenantId, string projectId, string pipelineId)
    {
        var (_, generated, _, artifacts) = GetPipelineSubPaths(tenantId, projectId, pipelineId);

        if (!Directory.Exists(generated) || !Directory.EnumerateFileSystemEntries(generated).Any())
            throw new InvalidOperationException("无生成产物可交付");

        var zipFileName = $"delivery-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
        var zipPath = Path.Combine(artifacts, zipFileName);

        if (File.Exists(zipPath))
            File.Delete(zipPath);

        ZipFile.CreateFromDirectory(generated, zipPath);
        return zipPath;
    }

    /// <summary>
    /// 【向后兼容】将 generated/ 目录打包为 zip（二元组）。
    /// [Obsolete] 新代码 MUST 使用三元组重载。
    /// </summary>
    [Obsolete("R12 三元组铁律：新代码 MUST 使用 CreateDeliveryZip(tenantId, projectId, pipelineId) 三元重载。")]
    public static string CreateDeliveryZip(string tenantId, string pipelineId)
    {
        var (_, generated, _, artifacts) = GetPipelineSubPaths(tenantId, pipelineId);

        if (!Directory.Exists(generated) || !Directory.EnumerateFileSystemEntries(generated).Any())
            throw new InvalidOperationException("无生成产物可交付");

        var zipFileName = $"delivery-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
        var zipPath = Path.Combine(artifacts, zipFileName);

        if (File.Exists(zipPath))
            File.Delete(zipPath);

        ZipFile.CreateFromDirectory(generated, zipPath);
        return zipPath;
    }

    // ─── 清理 ───

    /// <summary>
    /// 【R12 主入口】删除整个工作区目录（三元组）。异常安全：失败仅记录，不抛异常。
    /// </summary>
    public static void DeleteWorkspace(string tenantId, string projectId, string pipelineId)
    {
        try
        {
            var root = GetPipelinePath(tenantId, projectId, pipelineId);
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"StudioWorkspace cleanup failed for {tenantId}/{projectId}/{pipelineId}: {ex.Message}");
        }
    }

    /// <summary>
    /// 【向后兼容】删除整个工作区目录（二元组）。
    /// [Obsolete] 新代码 MUST 使用三元组重载。
    /// </summary>
    [Obsolete("R12 三元组铁律：新代码 MUST 使用 DeleteWorkspace(tenantId, projectId, pipelineId) 三元重载。")]
    public static void DeleteWorkspace(string tenantId, string pipelineId)
    {
        try
        {
            var root = GetPipelinePath(tenantId, pipelineId);
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"StudioWorkspace cleanup failed for {tenantId}/{pipelineId}: {ex.Message}");
        }
    }

    // ─── AI 开发上下文标记（文件桥接，供 guard-write.mjs 读取）───

    private static readonly string AiDevContextFilePath = Path.Combine(
        Directory.GetCurrentDirectory(), ".claude", "ai-dev-context.json");

    /// <summary>【R12 主入口】写入 AI 开发上下文文件（三元组）</summary>
    public static void WriteAiDevContext(string tenantId, string projectId, string pipelineId)
    {
        var contextDir = Path.GetDirectoryName(AiDevContextFilePath);
        if (!string.IsNullOrEmpty(contextDir) && !Directory.Exists(contextDir))
            Directory.CreateDirectory(contextDir);

        var workspacePath = GetPipelinePath(tenantId, projectId, pipelineId);
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            pipelineId,
            projectId,
            tenantId,
            workspacePath,
            timestamp = DateTime.UtcNow.ToString("o")
        });
        File.WriteAllText(AiDevContextFilePath, json);
    }

    /// <summary>
    /// 【向后兼容】写入 AI 开发上下文文件（二元组）。
    /// [Obsolete] 新代码 MUST 使用三元组重载。
    /// </summary>
    [Obsolete("R12 三元组铁律：新代码 MUST 使用 WriteAiDevContext(tenantId, projectId, pipelineId) 三元重载。")]
    public static void WriteAiDevContext(string tenantId, string pipelineId)
    {
        var contextDir = Path.GetDirectoryName(AiDevContextFilePath);
        if (!string.IsNullOrEmpty(contextDir) && !Directory.Exists(contextDir))
            Directory.CreateDirectory(contextDir);

        var workspacePath = GetPipelinePath(tenantId, pipelineId);
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            pipelineId,
            tenantId,
            workspacePath,
            timestamp = DateTime.UtcNow.ToString("o")
        });
        File.WriteAllText(AiDevContextFilePath, json);
    }

    /// <summary>
    /// 清除 AI 开发上下文文件，退出 L4 白名单模式。
    /// </summary>
    public static void ClearAiDevContext()
    {
        try
        {
            if (File.Exists(AiDevContextFilePath))
                File.Delete(AiDevContextFilePath);
        }
        catch
        {
            // 静默处理
        }
    }

    // ─── 前端文件注入（供预览工程使用）───

    /// <summary>
    /// 将 generated/ 目录下的前端文件注入到壳工程的 src/views/ 目录.
    /// 支持 .vue / .ts / .css / .scss / .less 文件.
    /// 保持相对路径结构.
    /// </summary>
    /// <param name="generatedDir">AI 生成的代码目录</param>
    /// <param name="previewProjectDir">studio-preview 工程根目录</param>
    public static void InjectFrontendFiles(string generatedDir, string previewProjectDir)
    {
        if (!Directory.Exists(generatedDir))
            return;

        var viewsDir = Path.Combine(previewProjectDir, "src", "views");
        Directory.CreateDirectory(viewsDir);

        var extensions = new[] { "*.vue", "*.ts", "*.css", "*.scss", "*.less" };
        foreach (var pattern in extensions)
        {
            foreach (var file in Directory.EnumerateFiles(generatedDir, pattern, SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(generatedDir, file);
                var dest = Path.Combine(viewsDir, relativePath);
                var destDir = Path.GetDirectoryName(dest);
                if (!string.IsNullOrEmpty(destDir))
                    Directory.CreateDirectory(destDir);
                File.Copy(file, dest, overwrite: true);
            }
        }
    }
}
