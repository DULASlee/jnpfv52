using JNPF.Common.Dtos.OAuth;
using JNPF.DataEncryption;
using JNPF.OAuth.Helpers;
using JNPF.Systems.Entitys.Enum;
using SqlSugar;
using Xunit;

namespace JNPF.Tests.OAuth;

/// <summary>
/// Characterization: Login / GetConfigCode pure shaping helpers.
/// </summary>
public class LoginFlowHelpersTests
{
    [Fact]
    public void NormalizeHost_StripsSchemeAndWww()
    {
        Assert.Equal("a.example.com", LoginFlowHelpers.NormalizeHost("https://www.a.example.com"));
        Assert.Equal("example.com", LoginFlowHelpers.NormalizeHost("http://example.com"));
    }

    [Fact]
    public void TryRewriteAccountFromDomainHost_SubdomainRewrites()
    {
        var ok = LoginFlowHelpers.TryRewriteAccountFromDomainHost(
            "https://acme.example.com",
            "https://www.example.com",
            "alice",
            out var tenantId,
            out var account);

        Assert.True(ok);
        Assert.Equal("acme", tenantId);
        Assert.Equal("acme@alice", account);
    }

    [Fact]
    public void TryRewriteAccountFromDomainHost_ExactHost_NoRewrite()
    {
        var ok = LoginFlowHelpers.TryRewriteAccountFromDomainHost(
            "https://example.com",
            "https://example.com",
            "alice",
            out _,
            out var account);

        Assert.False(ok);
        Assert.Equal("alice", account);
    }

    [Theory]
    [InlineData("tenant1@bob", "tenant1", "bob")]
    [InlineData("solo", "solo", "admin")]
    public void SplitTenantAccount_LegacyRules(string input, string tenant, string account)
    {
        var (t, a) = LoginFlowHelpers.SplitTenantAccount(input);
        Assert.Equal(tenant, t);
        Assert.Equal(account, a);
    }

    [Theory]
    [InlineData(false, null, true)]
    [InlineData(false, "", true)]
    [InlineData(false, "password", true)]
    [InlineData(false, "official", false)]
    [InlineData(true, null, false)]
    public void ShouldAesDecryptPassword_MatchesLoginGate(bool social, string? grant, bool expected)
        => Assert.Equal(expected, LoginFlowHelpers.ShouldAesDecryptPassword(social, grant));

    [Fact]
    public void ResolvePasswordForCompare_OfficialUsesRaw()
    {
        var raw = LoginFlowHelpers.ResolvePasswordForCompare("plain", "secret", false, "official");
        Assert.Equal("plain", raw);
    }

    [Fact]
    public void ResolvePasswordForCompare_OrdinaryUsesMd5()
    {
        var expected = MD5Encryption.Encrypt("plain" + "secret");
        var actual = LoginFlowHelpers.ResolvePasswordForCompare("plain", "secret", false, null);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EvaluateDelayLock_StillLocked_ThrowsMinutes()
    {
        var now = new DateTime(2026, 8, 8, 12, 0, 0);
        var outcome = LoginFlowHelpers.EvaluateDelayLock(
            ErrorStrategy.Delay,
            now.AddMinutes(5),
            now,
            out var minutes);

        Assert.Equal(LoginDelayLockOutcome.ThrowStillLocked, outcome);
        Assert.Equal(5, minutes);
    }

    [Fact]
    public void EvaluateDelayLock_Expired_Clears()
    {
        var now = new DateTime(2026, 8, 8, 12, 0, 0);
        var outcome = LoginFlowHelpers.EvaluateDelayLock(
            ErrorStrategy.Delay,
            now.AddMinutes(-1),
            now,
            out _);

        Assert.Equal(LoginDelayLockOutcome.ClearLockCounters, outcome);
    }

    [Fact]
    public void EvaluateDelayLock_NullUnlock_NoOp()
    {
        var outcome = LoginFlowHelpers.EvaluateDelayLock(
            ErrorStrategy.Delay,
            null,
            DateTime.UtcNow,
            out var minutes);

        Assert.Equal(LoginDelayLockOutcome.None, outcome);
        Assert.Equal(0, minutes);
    }

    [Fact]
    public void IsIpBlockedByWhitelist_BlocksNonAdminOutsideList()
    {
        Assert.True(LoginFlowHelpers.IsIpBlockedByWhitelist(true, 0, "1.1.1.1,2.2.2.2", "9.9.9.9"));
        Assert.False(LoginFlowHelpers.IsIpBlockedByWhitelist(true, 0, "1.1.1.1,2.2.2.2", "1.1.1.1"));
        Assert.False(LoginFlowHelpers.IsIpBlockedByWhitelist(true, 1, "1.1.1.1", "9.9.9.9"));
        Assert.False(LoginFlowHelpers.IsIpBlockedByWhitelist(false, 0, "1.1.1.1", "9.9.9.9"));
    }

    [Fact]
    public void ResolveTheme_NullFallsBackToClassic()
    {
        Assert.Equal("classic", LoginFlowHelpers.ResolveTheme(null));
        Assert.Equal("dark", LoginFlowHelpers.ResolveTheme("dark"));
    }

    [Fact]
    public void UpsertGlobalTenantCache_AddWhenMissing()
    {
        var list = new List<GlobalTenantCacheModel>();
        var options = new ConnectionConfigOptions { ConfigId = "cfg" };
        var output = new TenantInterFaceOutput { tenantName = "Acme", type = 1 };

        LoginFlowHelpers.UpsertGlobalTenantCache(
            list, false, "t1", 2, options, output, updateExtendedFields: true);

        Assert.Single(list);
        Assert.Equal("t1", list[0].TenantId);
        Assert.Equal(2, list[0].SingleLogin);
        Assert.Equal("Acme", list[0].tenantName);
        Assert.Equal(1, list[0].type);
    }

    [Fact]
    public void UpsertGlobalTenantCache_PartialUpdateSkipsExtended()
    {
        var list = new List<GlobalTenantCacheModel>
        {
            new()
            {
                TenantId = "t1",
                SingleLogin = 1,
                tenantName = "Old",
                type = 0,
            },
        };
        var options = new ConnectionConfigOptions { ConfigId = "cfg2" };
        var output = new TenantInterFaceOutput { tenantName = "New", type = 2 };

        LoginFlowHelpers.UpsertGlobalTenantCache(
            list, true, "t1", 3, options, output, updateExtendedFields: false);

        Assert.Equal(3, list[0].SingleLogin);
        Assert.Equal("cfg2", list[0].connectionConfig.ConfigId);
        Assert.Equal("Old", list[0].tenantName);
        Assert.Equal(0, list[0].type);
    }

    [Fact]
    public void UpsertGlobalTenantCache_FullUpdateWritesExtended()
    {
        var list = new List<GlobalTenantCacheModel>
        {
            new() { TenantId = "t1", tenantName = "Old", type = 0 },
        };
        var output = new TenantInterFaceOutput { tenantName = "New", type = 2 };

        LoginFlowHelpers.UpsertGlobalTenantCache(
            list, true, "t1", 1, new ConnectionConfigOptions(), output, updateExtendedFields: true);

        Assert.Equal("New", list[0].tenantName);
        Assert.Equal(2, list[0].type);
    }
}
