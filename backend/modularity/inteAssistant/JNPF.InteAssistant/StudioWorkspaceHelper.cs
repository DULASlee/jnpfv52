using JNPF.Common.Configuration;
using JNPF.InteAssistant.Interfaces;
using System.IO.Compression;

namespace JNPF.InteAssistant;

/// <summary>
/// AI 开发工作区路径工具类
/// 纯函数、零状态、零依赖。所有路径计算集中于此。
/// </summary>
public static class StudioWorkspaceHelper
{
    // ─── 子目录名常量 ───

    private const string IrDir = "ir";
    private const string GeneratedDir = "generated";
    private const string WorkspaceDir = "workspace";
    private const string ArtifactsDir = "artifacts";

    // ─── 路径计算 ───

    /// <summary>
    /// 获取流水线工作区根路径: {SystemPath}/StudioWorkspace/{tenantId}/{pipelineId}/
    /// </summary>
    public static string GetPipelinePath(string tenantId, string pipelineId)
    {
        return Path.Combine(
            KeyVariable.SystemPath,
            KeyVariable.StudioWorkspaceRoot,
            tenantId,
            pipelineId);
    }

    /// <summary>
    /// 获取四个子目录完整路径
    /// </summary>
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

    /// <summary>
    /// 创建四个子目录（幂等，已存在则跳过）
    /// </summary>
    public static void EnsureDirectories(string tenantId, string pipelineId)
    {
        var (ir, generated, workspace, artifacts) = GetPipelineSubPaths(tenantId, pipelineId);
        Directory.CreateDirectory(ir);
        Directory.CreateDirectory(generated);
        Directory.CreateDirectory(workspace);
        Directory.CreateDirectory(artifacts);
    }

    // ─── 路径安全校验 ───

    /// <summary>
    /// 断言目标路径在工作区内，防止路径穿越。
    /// 使用 Path.GetFullPath 解析 ../ 后再做前缀匹配。
    /// </summary>
    /// <exception cref="InvalidOperationException">目标路径不在工作区内</exception>
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
    /// 将 generated/ 目录打包为 zip，返回 zip 文件完整路径。
    /// zip 文件放在 artifacts/ 子目录中。
    /// </summary>
    /// <exception cref="InvalidOperationException">generated/ 目录为空时抛出</exception>
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
    /// 删除整个工作区目录。异常安全：失败仅记录，不抛异常。
    /// </summary>
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

    /// <summary>
    /// 写入 AI 开发上下文文件，供 guard-write.mjs 读取以激活 L4 白名单规则。
    /// </summary>
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
