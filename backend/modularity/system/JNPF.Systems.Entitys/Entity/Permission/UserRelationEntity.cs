using JNPF.Common.Contracts;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using SqlSugar;

namespace JNPF.Systems.Entitys.Permission;

/// <summary>
/// 用户关系映射.
/// </summary>
[SugarTable("BASE_USER_RELATION")]
public class UserRelationEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 获取或设置 用户编号.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID", ColumnDescription = "用户编号")]
    public string UserId { get; set; }

    /// <summary>
    /// 获取或设置 对象类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_OBJECT_TYPE", ColumnDescription = "对象类型")]
    public string ObjectType { get; set; }

    /// <summary>
    /// 获取或设置 对象主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_OBJECT_ID", ColumnDescription = "对象主键")]
    public string ObjectId { get; set; }
}