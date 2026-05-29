using JNPF.Common.Contracts;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using SqlSugar;

namespace JNPF.Systems.Entitys.Permission;

/// <summary>
/// 用户第三方登录 .
/// </summary>
[SugarTable("BASE_SOCIALS_USERS")]
public class SocialsUsersEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 用户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID")]
    public string UserId { get; set; }

    /// <summary>
    /// 第三方类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_SOCIAL_TYPE")]
    public string SocialType { get; set; }

    /// <summary>
    /// 第三方账号id.
    /// </summary>
    [SugarColumn(ColumnName = "F_Social_Id")]
    public string SocialId { get; set; }

    /// <summary>
    /// 第三方账号.
    /// </summary>
    [SugarColumn(ColumnName = "F_Social_Name")]
    public string SocialName { get; set; }
}