using System.Collections.Generic;
using JNPF.Options;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace JNPF.Tests.Common;

/// <summary>
/// M11 特性开关基建 — RuntimeFoundationOptions 绑定契约测试（规格 4.1.8 验收①）.
/// 契约：C-M11-Options@v1（docs/architecture/contract-registry.md）.
/// </summary>
public class RuntimeFoundationOptionsTests
{
    /// <summary>
    /// BR-1 默认关闭兜底：配置节缺失时四位开关全部为 false（安全侧倒）.
    /// </summary>
    [Fact]
    public void MissingSection_DefaultsAllFalse()
    {
        var configuration = new ConfigurationBuilder().Build();

        var options = configuration.GetSection(RuntimeFoundationOptions.Section)
            .Get<RuntimeFoundationOptions>() ?? new RuntimeFoundationOptions();

        Assert.False(options.ExceptionBoundary);
        Assert.False(options.OutboxSweeper);
        Assert.False(options.OutboundResilience);
        Assert.False(options.QueryableLogging);
    }

    /// <summary>
    /// 显式配置正确绑定：按位读取，互不串扰.
    /// </summary>
    [Fact]
    public void ExplicitSection_BindsCorrectly()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{RuntimeFoundationOptions.Section}:ExceptionBoundary"] = "true",
                [$"{RuntimeFoundationOptions.Section}:OutboxSweeper"] = "false",
                [$"{RuntimeFoundationOptions.Section}:OutboundResilience"] = "true",
                [$"{RuntimeFoundationOptions.Section}:QueryableLogging"] = "true",
            })
            .Build();

        var options = configuration.GetSection(RuntimeFoundationOptions.Section)
            .Get<RuntimeFoundationOptions>();

        Assert.NotNull(options);
        Assert.True(options!.ExceptionBoundary);
        Assert.False(options.OutboxSweeper);
        Assert.True(options.OutboundResilience);
        Assert.True(options.QueryableLogging);
    }
}
