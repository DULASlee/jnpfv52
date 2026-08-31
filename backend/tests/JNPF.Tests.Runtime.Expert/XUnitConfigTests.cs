using Xunit;

namespace JNPF.Tests.Agent;

public sealed class XUnitConfigTests
{
    [Fact]
    public void XUnit_RunnerConfig_IsCopiedToOutput()
    {
        var outputDir = Path.GetDirectoryName(typeof(XUnitConfigTests).Assembly.Location)!;
        var configPath = Path.Combine(outputDir, "xunit.runner.json");
        Assert.True(File.Exists(configPath), $"xunit.runner.json not copied to output. Looked at: {configPath}");
        var content = File.ReadAllText(configPath);
        Assert.Contains("\"parallelizeTestCollections\": false", content);
        Assert.Contains("\"parallelizeAssembly\": false", content);
    }
}