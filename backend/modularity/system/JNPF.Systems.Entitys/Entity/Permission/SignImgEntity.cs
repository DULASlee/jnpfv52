using JNPF.Common.Contracts;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using SqlSugar;

namespace JNPF.Systems.Entitys.Entity.Permission;

/// <summary>
/// 用户签名类.
/// </summary>
[SugarTable("BASE_SIGN_IMG")]
public class SignImgEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 签名.
    /// </summary>
    [SugarColumn(ColumnName = "F_SIGN_IMG")]
    public string SignImg { get; set; }

    /// <summary>
    /// 是否默认(0:否，1：是).
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_DEFAULT")]
    public int? IsDefault { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }
}
