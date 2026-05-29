using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.Entity.System;

/// <summary>
/// 接口认证用户
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_DATA_INTERFACE_USER")]
[Tenant(ClaimConst.TENANTID)]
public class DataInterfaceUserEntity : CLDEntityBase
{
    /// <summary>
    /// 用户主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID")]
    public string? UserId { get; set; }

    /// <summary>
    /// 用户密钥.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_KEY")]
    public string? UserKey { get; set; }

    /// <summary>
    /// 接口认证主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_OAUTH_ID")]
    public string? OauthId { get; set; }
}
