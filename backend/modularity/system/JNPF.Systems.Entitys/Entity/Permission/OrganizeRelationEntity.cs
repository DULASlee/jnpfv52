using JNPF.Common.Contracts;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using SqlSugar;

namespace JNPF.Systems.Entitys.Permission;

/// <summary>
/// 用户关系映射.
/// </summary>
[SugarTable("BASE_ORGANIZE_RELATION")]
public class OrganizeRelationEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 获取或设置 组织Id.
    /// </summary>
    [SugarColumn(ColumnName = "F_ORGANIZE_ID", ColumnDescription = "组织Id")]
    public string OrganizeId { get; set; }

    /// <summary>
    /// 对象类型（角色：Role、岗位：Position）.
    /// </summary>
    [SugarColumn(ColumnName = "F_OBJECT_TYPE", ColumnDescription = "对象类型（角色：Role、岗位：Position）")]
    public string ObjectType { get; set; }

    /// <summary>
    /// 获取或设置 对象主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_OBJECT_ID", ColumnDescription = "对象主键")]
    public string ObjectId { get; set; }
}