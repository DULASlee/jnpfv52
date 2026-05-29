using JNPF.DependencyInjection;
using SqlSugar;

namespace JNPF.Common.Dtos.OAuth;

/// <summary>
/// 多租户网络连接输出.
/// </summary>
[SuppressSniffer]
public class TenantInterFaceOutput
{
    /// <summary>
    /// DotNet.
    /// </summary>
    public string dotnet { get; set; }

    /// <summary>
    /// 配置连接
    /// </summary>
    public List<TenantLinkModel>? linkList { get; set; }

    /// <summary>
    /// tenantId.
    /// </summary>
    public string tenantId { get; set; }

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
    /// 用户Id.
    /// </summary>
    public string userId { get; set; }

    /// <summary>
    /// 新密码（前端加密后的）.
    /// </summary>
    public string userPassword { get; set; }

    /// <summary>
    /// 用户名.
    /// </summary>
    public string realName { get; set; }

    /// <summary>
    /// 手机.
    /// </summary>
    public string mobilePhone { get; set; }

    /// <summary>
    /// 邮箱.
    /// </summary>
    public string email { get; set; }

    /// <summary>
    /// 性别.
    /// </summary>
    public string gender { get; set; }

    /// <summary>
    /// 连接配置.
    /// </summary>
    public ConnectionConfigOptions options { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public TenantInterFaceCode wl_qrcode { get; set; }
}