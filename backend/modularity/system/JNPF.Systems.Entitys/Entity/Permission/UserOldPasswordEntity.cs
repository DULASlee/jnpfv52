using JNPF.Common.Contracts;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using SqlSugar;

namespace JNPF.Systems.Entitys.Entity.Permission;

/// <summary>
/// 用户旧密码记录表.
/// </summary>
[SugarTable("BASE_USER_OLD_PASSWORD")]
public class UserOldPasswordEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 用户ID.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID")]
    public string UserId { get; set; }

    /// <summary>
    /// 用户ID.
    /// </summary>
    [SugarColumn(ColumnName = "F_ACCOUNT")]
    public string Account { get; set; }

    /// <summary>
    /// 账户.
    /// </summary>
    [SugarColumn(ColumnName = "F_OLD_PASSWORD")]
    public string OldPassword { get; set; }

    /// <summary>
    /// 秘钥.
    /// </summary>
    [SugarColumn(ColumnName = "F_SECRETKEY")]
    public string Secretkey { get; set; }
}
