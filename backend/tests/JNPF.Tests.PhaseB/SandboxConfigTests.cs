using JNPF.InteAssistant.Interfaces;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// SandboxConfig / SandboxInfo / SandboxInstance 数据模型单元测试 (3 用例).
/// </summary>
public static class SandboxConfigTests
{
    /// <summary>
    /// T15: PreviewPort 默认值为 4173.
    /// </summary>
    public static Task T15_PreviewPort_DefaultValue()
    {
        var config = new SandboxConfig { Id = "t15", TenantId = "t1" };

        if (config.PreviewPort != 4173)
        { TestRunner.Fail("T15", $"PreviewPort 默认值应为 4173, 实际 {config.PreviewPort}"); return Task.CompletedTask; }
        if (config.Port != 8080)
        { TestRunner.Fail("T15", $"Port 默认值应为 8080, 实际 {config.Port}"); return Task.CompletedTask; }

        TestRunner.Pass("T15: PreviewPort 默认值 4173, Port 默认值 8080");
        return Task.CompletedTask;
    }

    /// <summary>
    /// T16: PreviewUrl 拼接正确.
    /// </summary>
    public static Task T16_PreviewUrl_FormattedCorrectly()
    {
        var info = new SandboxInfo
        {
            SandboxId = "test",
            Host = "localhost",
            PreviewUrl = "http://localhost:32768",
        };

        if (!info.PreviewUrl.StartsWith("http://localhost:"))
        { TestRunner.Fail("T16", $"PreviewUrl 格式不正确: {info.PreviewUrl}"); return Task.CompletedTask; }

        TestRunner.Pass("T16: PreviewUrl 格式正确");
        return Task.CompletedTask;
    }

    /// <summary>
    /// T17: SandboxInstance 生命周期含 PreviewUrl.
    /// </summary>
    public static Task T17_SandboxInstance_LifecycleWithPreview()
    {
        var instance = new SandboxInstance
        {
            Id = "test-lifecycle",
            Status = "creating",
            PreviewUrl = null,
        };

        // creating → ready
        instance.Status = "ready";
        instance.PreviewUrl = "http://localhost:4173";
        if (instance.Status != "ready")
        { TestRunner.Fail("T17", "状态未变为 ready"); return Task.CompletedTask; }
        if (string.IsNullOrEmpty(instance.PreviewUrl))
        { TestRunner.Fail("T17", "PreviewUrl 未设置"); return Task.CompletedTask; }

        // ready → destroying
        instance.Status = "destroying";
        if (instance.Status != "destroying")
        { TestRunner.Fail("T17", "状态未变为 destroying"); return Task.CompletedTask; }

        // destroying → destroyed (PreviewUrl 保持不变，前端可显示最后已知 URL)
        instance.Status = "destroyed";
        if (instance.Status != "destroyed")
        { TestRunner.Fail("T17", "状态未变为 destroyed"); return Task.CompletedTask; }

        TestRunner.Pass("T17: SandboxInstance 生命周期含 PreviewUrl");
        return Task.CompletedTask;
    }
}
