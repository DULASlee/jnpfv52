namespace JNPF.Tests.PhaseB;

/// <summary>
/// StudioWorkspaceHelper 路径工具类单元测试 (8 用例).
/// 注意：T1-T2 依赖 KeyVariable.SystemPath（需要 App 配置初始化），
/// 若 App 未初始化则自动跳过.
/// </summary>
public static class StudioWorkspaceHelperTests
{
    static bool AppInitialized;

    static StudioWorkspaceHelperTests()
    {
        try
        {
            _ = JNPF.Common.Configuration.KeyVariable.SystemPath;
            AppInitialized = true;
        }
        catch
        {
            AppInitialized = false;
        }
    }

    // ── 路径计算 ──

    /// <summary>
    /// T1: GetPipelinePath 返回正确格式的路径.
    /// </summary>
    public static Task T1_GetPipelinePath_ReturnsCorrectFormat()
    {
        if (!AppInitialized)
        {
            TestRunner.Skip("T1: App 配置未初始化，跳过");
            return Task.CompletedTask;
        }

        var path = JNPF.InteAssistant.StudioWorkspaceHelper.GetPipelinePath("tenant1", "pipeline123");

        if (!path.Contains("StudioWorkspace"))
        { TestRunner.Fail("T1", $"路径应包含 StudioWorkspace: {path}"); return Task.CompletedTask; }
        if (!path.Contains("tenant1"))
        { TestRunner.Fail("T1", $"路径应包含 tenant1: {path}"); return Task.CompletedTask; }
        if (!path.Contains("pipeline123"))
        { TestRunner.Fail("T1", $"路径应包含 pipeline123: {path}"); return Task.CompletedTask; }

        TestRunner.Pass("T1: GetPipelinePath 返回正确格式");
        return Task.CompletedTask;
    }

    /// <summary>
    /// T2: GetPipelineSubPaths 返回全部四个子目录.
    /// </summary>
    public static Task T2_GetPipelineSubPaths_ReturnsAllFour()
    {
        if (!AppInitialized)
        {
            TestRunner.Skip("T2: App 配置未初始化，跳过");
            return Task.CompletedTask;
        }

        var (ir, generated, workspace, artifacts) =
            JNPF.InteAssistant.StudioWorkspaceHelper.GetPipelineSubPaths("t1", "p1");

        if (!ir.EndsWith("ir"))
        { TestRunner.Fail("T2", $"ir 路径应以 ir 结尾: {ir}"); return Task.CompletedTask; }
        if (!generated.EndsWith("generated"))
        { TestRunner.Fail("T2", $"generated 路径应以 generated 结尾: {generated}"); return Task.CompletedTask; }
        if (!workspace.EndsWith("workspace"))
        { TestRunner.Fail("T2", $"workspace 路径应以 workspace 结尾: {workspace}"); return Task.CompletedTask; }
        if (!artifacts.EndsWith("artifacts"))
        { TestRunner.Fail("T2", $"artifacts 路径应以 artifacts 结尾: {artifacts}"); return Task.CompletedTask; }

        TestRunner.Pass("T2: GetPipelineSubPaths 返回四个子目录");
        return Task.CompletedTask;
    }

    /// <summary>
    /// T9: R12 自锚定（projectId == pipelineId）走老三层路径.
    /// </summary>
    public static Task T9_SelfAnchored_UsesLegacyThreeLayerPath()
    {
        if (!AppInitialized)
        {
            TestRunner.Skip("T9: App 配置未初始化，跳过");
            return Task.CompletedTask;
        }

        var path = JNPF.InteAssistant.StudioWorkspaceHelper.GetPipelinePath("0", "311", "311");
        if (path.Contains($"{Path.DirectorySeparatorChar}311{Path.DirectorySeparatorChar}311"))
        {
            TestRunner.Fail("T9", $"自锚定不应出现双 311 层: {path}");
            return Task.CompletedTask;
        }
        if (!path.EndsWith($"{Path.DirectorySeparatorChar}311"))
        {
            TestRunner.Fail("T9", $"自锚定路径应以 pipelineId 结尾: {path}");
            return Task.CompletedTask;
        }

        TestRunner.Pass("T9: 自锚定走老三层路径");
        return Task.CompletedTask;
    }

    /// <summary>
    /// T10: R12 非自锚定（bugfix/enhancement）走新四层路径.
    /// </summary>
    public static Task T10_NonSelfAnchored_UsesFourLayerPath()
    {
        if (!AppInitialized)
        {
            TestRunner.Skip("T10: App 配置未初始化，跳过");
            return Task.CompletedTask;
        }

        var path = JNPF.InteAssistant.StudioWorkspaceHelper.GetPipelinePath("0", "100", "101");
        var expectedSegment = $"100{Path.DirectorySeparatorChar}101";
        if (!path.Contains(expectedSegment))
        {
            TestRunner.Fail("T10", $"非自锚定应含 projectId/pipelineId 层: {path}");
            return Task.CompletedTask;
        }

        TestRunner.Pass("T10: 非自锚定走新四层路径");
        return Task.CompletedTask;
    }

    // ── 路径安全校验 ──

    /// <summary>
    /// T3: AssertWithinWorkspace 允许合法路径.
    /// </summary>
    public static Task T3_AssertWithinWorkspace_AllowsValidPath()
    {
        if (!AppInitialized)
        {
            TestRunner.Skip("T3: App 配置未初始化，跳过");
            return Task.CompletedTask;
        }

        var basePath = JNPF.InteAssistant.StudioWorkspaceHelper.GetPipelinePath("t1", "p1");
        var validPath = Path.Combine(basePath, "generated", "test.vue");

        try
        {
            JNPF.InteAssistant.StudioWorkspaceHelper.AssertWithinWorkspace(validPath, "t1", "p1");
        }
        catch (InvalidOperationException ex)
        {
            TestRunner.Fail("T3", $"合法路径不应抛异常: {ex.Message}");
            return Task.CompletedTask;
        }

        TestRunner.Pass("T3: AssertWithinWorkspace 允许合法路径");
        return Task.CompletedTask;
    }

    /// <summary>
    /// T4: AssertWithinWorkspace 拦截路径穿越 (../).
    /// </summary>
    public static Task T4_AssertWithinWorkspace_BlocksTraversal()
    {
        if (!AppInitialized)
        {
            TestRunner.Skip("T4: App 配置未初始化，跳过");
            return Task.CompletedTask;
        }

        var basePath = JNPF.InteAssistant.StudioWorkspaceHelper.GetPipelinePath("t1", "p1");
        var traversalPath = Path.Combine(basePath, "..", "..", "etc", "passwd");

        try
        {
            JNPF.InteAssistant.StudioWorkspaceHelper.AssertWithinWorkspace(traversalPath, "t1", "p1");
            TestRunner.Fail("T4", "路径穿越应抛异常");
        }
        catch (InvalidOperationException)
        {
            TestRunner.Pass("T4: AssertWithinWorkspace 拦截路径穿越");
        }

        return Task.CompletedTask;
    }

    // ── 前端文件注入 ──

    /// <summary>
    /// T5: InjectFrontendFiles 正确复制 Vue/TS/CSS 文件.
    /// </summary>
    public static Task T5_InjectFrontendFiles_CopiesVueFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"test-inject-{Guid.NewGuid():N}");
        var previewDir = Path.Combine(Path.GetTempPath(), $"test-preview-{Guid.NewGuid():N}");

        try
        {
            var generatedDir = Path.Combine(tempDir, "generated");
            Directory.CreateDirectory(Path.Combine(generatedDir, "pages"));
            Directory.CreateDirectory(Path.Combine(generatedDir, "components"));
            File.WriteAllText(Path.Combine(generatedDir, "pages", "Home.vue"), "<template><div>Home</div></template>");
            File.WriteAllText(Path.Combine(generatedDir, "components", "Button.vue"), "<template><button>Click</button></template>");
            File.WriteAllText(Path.Combine(generatedDir, "utils.ts"), "export const foo = 1;");

            JNPF.InteAssistant.StudioWorkspaceHelper.InjectFrontendFiles(generatedDir, previewDir);

            var viewsDir = Path.Combine(previewDir, "src", "views");
            if (!Directory.Exists(viewsDir))
            { TestRunner.Fail("T5", "views 目录未创建"); return Task.CompletedTask; }

            var homeDest = Path.Combine(viewsDir, "pages", "Home.vue");
            var buttonDest = Path.Combine(viewsDir, "components", "Button.vue");
            var utilsDest = Path.Combine(viewsDir, "utils.ts");

            if (!File.Exists(homeDest))
            { TestRunner.Fail("T5", "Home.vue 未复制"); return Task.CompletedTask; }
            if (!File.Exists(buttonDest))
            { TestRunner.Fail("T5", "Button.vue 未复制"); return Task.CompletedTask; }
            if (!File.Exists(utilsDest))
            { TestRunner.Fail("T5", "utils.ts 未复制"); return Task.CompletedTask; }

            var content = File.ReadAllText(homeDest);
            if (!content.Contains("Home"))
            { TestRunner.Fail("T5", "文件内容不匹配"); return Task.CompletedTask; }

            TestRunner.Pass("T5: InjectFrontendFiles 正确复制文件");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
            try { Directory.Delete(previewDir, true); } catch { }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// T6: InjectFrontendFiles 空目录优雅返回.
    /// </summary>
    public static Task T6_InjectFrontendFiles_EmptyDirReturnsGracefully()
    {
        var emptyDir = Path.Combine(Path.GetTempPath(), $"test-empty-{Guid.NewGuid():N}");
        var previewDir = Path.Combine(Path.GetTempPath(), $"test-preview-empty-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(emptyDir);
            JNPF.InteAssistant.StudioWorkspaceHelper.InjectFrontendFiles(emptyDir, previewDir);
            TestRunner.Pass("T6: InjectFrontendFiles 空目录优雅返回");
        }
        catch (Exception ex)
        {
            TestRunner.Fail("T6", $"空目录不应抛异常: {ex.Message}");
        }
        finally
        {
            try { Directory.Delete(emptyDir, true); } catch { }
            try { Directory.Delete(previewDir, true); } catch { }
        }

        return Task.CompletedTask;
    }

    // ── ReadFilesFromDirectory ──

    /// <summary>
    /// T7: ReadFilesFromDirectory 返回正确的文件列表.
    /// </summary>
    public static Task T7_ReadFilesFromDirectory_ReturnsCorrectList()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"test-readfiles-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(Path.Combine(tempDir, "sub"));
            File.WriteAllText(Path.Combine(tempDir, "index.vue"), "index");
            File.WriteAllText(Path.Combine(tempDir, "sub", "detail.vue"), "detail");

            var files = JNPF.InteAssistant.StudioWorkspaceHelper.ReadFilesFromDirectory(tempDir);

            if (files.Count != 2)
            { TestRunner.Fail("T7", $"期望 2 个文件, 实际 {files.Count}"); return Task.CompletedTask; }

            var hasIndex = files.Any(f => f.FilePath == "index.vue");
            var hasDetail = files.Any(f => f.FilePath == "sub/detail.vue");

            if (!hasIndex || !hasDetail)
            { TestRunner.Fail("T7", "文件路径不匹配"); return Task.CompletedTask; }

            TestRunner.Pass("T7: ReadFilesFromDirectory 返回正确列表");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// T8: ReadFilesFromDirectory 空目录返回空列表.
    /// </summary>
    public static Task T8_ReadFilesFromDirectory_EmptyDirReturnsEmpty()
    {
        var emptyDir = Path.Combine(Path.GetTempPath(), $"test-readfiles-empty-{Guid.NewGuid():N}");
        Directory.CreateDirectory(emptyDir);

        try
        {
            var files = JNPF.InteAssistant.StudioWorkspaceHelper.ReadFilesFromDirectory(emptyDir);
            if (files.Count != 0)
            { TestRunner.Fail("T8", $"空目录应返回空列表, 实际 {files.Count}"); return Task.CompletedTask; }

            TestRunner.Pass("T8: ReadFilesFromDirectory 空目录返回空列表");
        }
        finally
        {
            try { Directory.Delete(emptyDir, true); } catch { }
        }

        return Task.CompletedTask;
    }
}
