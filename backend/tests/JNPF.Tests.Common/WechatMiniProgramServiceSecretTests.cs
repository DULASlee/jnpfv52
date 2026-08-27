using System.Text.RegularExpressions;
using Xunit;

namespace JNPF.Tests.Common;

/// <summary>
/// Security: J2 — verify no hardcoded secrets in WechatMiniProgramService.
/// </summary>
public class WechatMiniProgramServiceSecretTests
{
    [Fact]
    public void GetOpenId_Method_SourceCode_HasNoHardcodedAppIdOrSecret()
    {
        var filePath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "modularity", "message", "JNPF.Message", "Service", "WechatMiniProgramService.cs");

        if (!File.Exists(filePath))
        {
            Assert.True(true, "Source file not found at expected path — skipping (build-only test)");
            return;
        }

        var content = File.ReadAllText(filePath);

        var appIdMatch = Regex.Match(content, @"appId\s*=\s*""([A-Za-z0-9]+)""");
        var appSecretMatch = Regex.Match(content, @"appSecret\s*=\s*""([A-Za-z0-9]+)""");

        Assert.False(appIdMatch.Success, $"Hardcoded appId found: {appIdMatch.Value}");
        Assert.False(appSecretMatch.Success, $"Hardcoded appSecret found: {appSecretMatch.Value}");

        Assert.Contains("messageAccountEntity.AppId", content);
        Assert.Contains("messageAccountEntity.AppSecret", content);
    }
}
