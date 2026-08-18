using JNPF.Common.Dtos.OAuth;
using JNPF.Common.Extension;
using JNPF.DataEncryption;
using JNPF.Systems.Entitys.Enum;
using SqlSugar;

namespace JNPF.OAuth.Helpers;

/// <summary>
/// Pure helpers for OAuthService.Login / GetConfigCode shaping
/// (domain tenant rewrite, account split, delay-lock, whitelist, password compare, tenant cache upsert).
/// DB / token / remote calls stay in the service.
/// </summary>
public static class LoginFlowHelpers
{
    /// <summary>
    /// Strip scheme and www. prefix from host / configured domain.
    /// </summary>
    public static string NormalizeHost(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("https://", string.Empty)
            .Replace("http://", string.Empty)
            .Replace("www.", string.Empty);
    }

    /// <summary>
    /// Domain-based multi-tenant: when host contains configured address and differs,
    /// rewrite account to {subdomain}@{account} and return subdomain as tenantId.
    /// </summary>
    public static bool TryRewriteAccountFromDomainHost(
        string? host,
        string? configuredAddress,
        string account,
        out string tenantId,
        out string rewrittenAccount)
    {
        tenantId = string.Empty;
        rewrittenAccount = account;

        var normalizedHost = NormalizeHost(host);
        var normalizedAddress = NormalizeHost(configuredAddress);
        if (string.IsNullOrEmpty(normalizedHost) || string.IsNullOrEmpty(normalizedAddress))
            return false;

        if (normalizedHost.Contains(normalizedAddress) && !normalizedHost.Equals(normalizedAddress))
        {
            tenantId = normalizedHost.Split(".").FirstOrDefault() ?? string.Empty;
            rewrittenAccount = string.Format("{0}@{1}", tenantId, account);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Split tenant@account. No @ → tenantId=whole, account=admin (legacy).
    /// </summary>
    public static (string TenantId, string Account) SplitTenantAccount(string account)
    {
        var parts = (account ?? string.Empty).Split('@');
        var tenantId = parts.FirstOrDefault() ?? string.Empty;
        var userAccount = parts.Length == 1 ? "admin" : parts[1];
        return (tenantId, userAccount);
    }

    /// <summary>
    /// Whether ordinary login should AES-decrypt the password form field.
    /// </summary>
    public static bool ShouldAesDecryptPassword(bool isSocialsLoginCallBack, string? grantType)
        => !isSocialsLoginCallBack && (grantType.IsNullOrEmpty() || !grantType.Equals("official"));

    /// <summary>
    /// Password value used for DB compare (MD5+secret vs raw for social/official).
    /// </summary>
    public static string ResolvePasswordForCompare(
        string password,
        string secretKey,
        bool isSocialsLoginCallBack,
        string? grantType)
    {
        if (isSocialsLoginCallBack || (grantType.IsNotEmptyOrNull() && grantType.Equals("official")))
            return password;

        return MD5Encryption.Encrypt(password + secretKey);
    }

    /// <summary>
    /// Delay-lock evaluation. Preserves legacy three-branch shape (incl. null UnLockTime no-op branch).
    /// </summary>
    public static LoginDelayLockOutcome EvaluateDelayLock(
        ErrorStrategy lockType,
        DateTime? unlockTime,
        DateTime now,
        out int unlockMinutes)
    {
        unlockMinutes = 0;
        if (!lockType.Equals(ErrorStrategy.Delay))
            return LoginDelayLockOutcome.None;

        // Legacy first branch: condition UnLockTime.IsNullOrEmpty — body rarely/never hits for null DateTime?.
        if (unlockTime.IsNullOrEmpty())
        {
            if (unlockTime > now)
            {
                unlockMinutes = ((unlockTime - now)?.TotalMinutes).ParseToInt();
                if (unlockMinutes < 1) unlockMinutes = 1;
                return LoginDelayLockOutcome.ThrowStillLocked;
            }

            if (unlockTime <= now)
                return LoginDelayLockOutcome.ClearLockCounters;
        }

        if (unlockTime.IsNotEmptyOrNull() && unlockTime > now)
        {
            unlockMinutes = ((unlockTime - now)?.TotalMinutes).ParseToInt();
            if (unlockMinutes < 1) unlockMinutes = 1;
            return LoginDelayLockOutcome.ThrowStillLocked;
        }

        if (unlockTime.IsNotEmptyOrNull() && unlockTime <= now)
            return LoginDelayLockOutcome.ClearLockCounters;

        return LoginDelayLockOutcome.None;
    }

    /// <summary>
    /// Whitelist gate: switch on + non-admin + IP not listed → blocked.
    /// </summary>
    public static bool IsIpBlockedByWhitelist(
        bool whitelistSwitch,
        int isAdministrator,
        string? whiteListIp,
        string ip)
    {
        int whitelistSwitchInt = Convert.ToInt32(whitelistSwitch);
        return whitelistSwitchInt.Equals(1)
            && isAdministrator.Equals(0)
            && !(whiteListIp ?? string.Empty).Split(",").Contains(ip);
    }

    /// <summary>
    /// Theme fallback for login response.
    /// </summary>
    public static string ResolveTheme(string? theme) => theme == null ? "classic" : theme;

    /// <summary>
    /// Add or update global tenant cache entry (shared by Login full update / GetConfig partial update).
    /// </summary>
    public static void UpsertGlobalTenantCache(
        List<GlobalTenantCacheModel> list,
        bool tenantExistsInCache,
        string tenantId,
        int singleLogin,
        ConnectionConfigOptions options,
        TenantInterFaceOutput tenantOutput,
        bool updateExtendedFields)
    {
        ArgumentNullException.ThrowIfNull(list);
        tenantOutput ??= new TenantInterFaceOutput();

        if (!tenantExistsInCache)
        {
            list.Add(new GlobalTenantCacheModel
            {
                TenantId = tenantId,
                SingleLogin = singleLogin,
                connectionConfig = options,
                type = tenantOutput.type,
                tenantName = tenantOutput.tenantName,
                validTime = tenantOutput.validTime,
                domain = tenantOutput.domain,
                accountNum = tenantOutput.accountNum,
                moduleIdList = tenantOutput.moduleIdList,
                urlAddressList = tenantOutput.urlAddressList,
                unitInfoJson = tenantOutput.unitInfoJson,
                userInfoJson = tenantOutput.userInfoJson,
            });
            return;
        }

        list.FindAll(it => it.TenantId.Equals(tenantId)).ForEach(item =>
        {
            item.TenantId = tenantId;
            item.SingleLogin = singleLogin;
            item.connectionConfig = options;
            if (!updateExtendedFields)
                return;

            item.type = tenantOutput.type;
            item.tenantName = tenantOutput.tenantName;
            item.validTime = tenantOutput.validTime;
            item.domain = tenantOutput.domain;
            item.accountNum = tenantOutput.accountNum;
            item.moduleIdList = tenantOutput.moduleIdList;
            item.urlAddressList = tenantOutput.urlAddressList;
            item.unitInfoJson = tenantOutput.unitInfoJson;
            item.userInfoJson = tenantOutput.userInfoJson;
        });
    }
}

/// <summary>
/// Outcome of delay-lock evaluation for Login.
/// </summary>
public enum LoginDelayLockOutcome
{
    None = 0,
    ThrowStillLocked = 1,
    ClearLockCounters = 2,
}
