using JNPF.DependencyInjection;

namespace SqlSugar;

/// <summary>
/// 全局租户缓存模型.
/// </summary>
[SuppressSniffer]
public class GlobalTenantCacheModel
{
    /// <summary>
    /// 租户ID.
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// 登录方式.
    /// </summary>
    public int SingleLogin { get; set; }

    /// <summary>
    /// 连接配置.
    /// </summary>
    public ConnectionConfigOptions connectionConfig { get; set; }

    /// <summary>
    /// 租户名.
    /// </summary>
    public string tenantName { get; set; }

    /// <summary>
    /// 租户有效期.
    /// </summary>
    public string validTime { get; set; }

    /// <summary>
    /// 域名.
    /// </summary>
    public string domain { get; set; }

    /// <summary>
    /// 类型(0-库隔离 1-id隔离 2-连接隔离).
    /// </summary>
    public int? type { get; set; } = 0;

    /// <summary>
    /// 账号额度.
    /// </summary>
    public int? accountNum { get; set; } = 0;

    /// <summary>
    /// 菜单id.
    /// </summary>
    public List<string> moduleIdList { get; set; } = new List<string>();

    /// <summary>
    /// 菜单地址.
    /// </summary>
    public List<string> urlAddressList { get; set; } = new List<string>();

    /// <summary>
    /// 单位信息.
    /// </summary>
    public object unitInfoJson { get; set; }

    /// <summary>
    /// 联系人信息.
    /// </summary>
    public object userInfoJson { get; set; }

    /// <summary>
    /// 系统应用Id
    /// </summary>
    public string SystemId { get; set; }
}