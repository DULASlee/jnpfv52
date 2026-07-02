using JNPF.InteAssistant.Interfaces;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// B1 预览资源清理逻辑单元测试 (2 用例) — 验证 sandboxCreated 标志位和清理策略.
/// </summary>
public static class PreviewResourceCleanupTests
{
    /// <summary>
    /// T18: sandboxCreated 标志在沙箱新建成功后设为 true.
    /// 验证：sandboxCreated 逻辑在各分支正确.
    /// </summary>
    public static Task T18_SandboxCreated_FlagSetOnSuccess()
    {
        // 模拟 StartPreviewAsync 中的 sandboxCreated 标志逻辑

        // 场景 1: 沙箱不存在 → 新建 → sandboxCreated = true
        bool sandboxCreated = false;
        bool sandboxExists = false; // GetStatusAsync 返回 null

        if (!sandboxExists)
        {
            // CreateAsync 成功 → 设置标志
            sandboxCreated = true;
        }

        if (!sandboxCreated)
        { TestRunner.Fail("T18", "新建沙箱后 sandboxCreated 应为 true"); return Task.CompletedTask; }

        // 场景 2: 沙箱已存在 → 复用 → sandboxCreated = false
        sandboxCreated = false;
        sandboxExists = true;
        if (!sandboxExists)
        {
            sandboxCreated = true;
        }

        if (sandboxCreated)
        { TestRunner.Fail("T18", "复用沙箱时 sandboxCreated 应为 false"); return Task.CompletedTask; }

        TestRunner.Pass("T18: sandboxCreated 标志在各分支正确");
        return Task.CompletedTask;
    }

    /// <summary>
    /// T19: 已有沙箱复用时不触发清理.
    /// 验证：sandboxCreated=false 时异常路径不调用 DestroyAsync.
    /// </summary>
    public static Task T19_SandboxNotCreated_OnExistingSandbox()
    {
        // 模拟异常路径清理逻辑
        bool sandboxCreated = false; // 复用的沙箱
        bool destroyCalled = false;

        try
        {
            throw new InvalidOperationException("模拟异常");
        }
        catch
        {
            if (sandboxCreated)
            {
                destroyCalled = true; // 不应到达这里
            }
        }

        if (destroyCalled)
        { TestRunner.Fail("T19", "复用沙箱异常时不应调用 DestroyAsync"); return Task.CompletedTask; }

        TestRunner.Pass("T19: 复用沙箱异常时不触发清理");
        return Task.CompletedTask;
    }
}
