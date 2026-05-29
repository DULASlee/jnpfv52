using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Message.Entitys.Entity;

/// <summary>
/// 个推用户表.
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_USER_DEVICE")]
public class UserDeviceEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 设备id.
    /// </summary>
    [SugarColumn(ColumnName = "F_CLIENT_ID")]
    public string? ClientId { get; set; }

    /// <summary>
    /// 用户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID")]
    public string? UserId { get; set; }
}
