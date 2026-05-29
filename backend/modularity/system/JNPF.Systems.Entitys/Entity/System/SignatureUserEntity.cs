using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 签章授权人.
/// </summary>
[SugarTable("BASE_SIGNATURE_USER")]
public class SignatureUserEntity : CLDSEntityBase
{
    /// <summary>
    /// 签章id.
    /// </summary>
    [SugarColumn(ColumnName = "F_SIGNATURE_ID")]
    public string SignatureId { get; set; }

    /// <summary>
    /// 授权人.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID")]
    public string UserId { get; set; }
}
