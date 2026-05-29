using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 签章管理.
/// </summary>
[SugarTable("BASE_SIGNATURE")]
public class SignatureEntity : CLDSEntityBase
{
    /// <summary>
    /// 名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 签章.
    /// </summary>
    [SugarColumn(ColumnName = "F_ICON")]
    public string Icon { get; set; }

    /// <summary>
    /// 授权人（一对多）.
    /// </summary>
    [Navigate(NavigateType.OneToMany, nameof(SignatureUserEntity.SignatureId), nameof(Id))]
    public List<SignatureUserEntity> SignatureUser { get; set; }
}
