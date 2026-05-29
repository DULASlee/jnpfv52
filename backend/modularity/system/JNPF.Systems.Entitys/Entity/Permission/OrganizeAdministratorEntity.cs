using JNPF.Common.Contracts;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using SqlSugar;

namespace JNPF.Systems.Entitys.Permission;

/// <summary>
/// 分级管理
/// 版 本：V3.2.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021.09.27.
/// </summary>
[SugarTable("BASE_ORGANIZE_ADMINISTRATOR")]
public class OrganizeAdministratorEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 用户ID.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID")]
    public string UserId { get; set; }

    /// <summary>
    /// 机构ID.
    /// </summary>
    [SugarColumn(ColumnName = "F_ORGANIZE_ID")]
    public string OrganizeId { get; set; }

    /// <summary>
    /// 机构类型 默认：组织， System：业务平台， Module：菜单.
    /// </summary>
    [SugarColumn(ColumnName = "F_ORGANIZE_TYPE")]
    public string OrganizeType { get; set; }

    /// <summary>
    /// 本层级添加.
    /// </summary>
    [SugarColumn(ColumnName = "F_THIS_LAYER_ADD")]
    public int ThisLayerAdd { get; set; }

    /// <summary>
    /// 本层级编辑.
    /// </summary>
    [SugarColumn(ColumnName = "F_THIS_LAYER_EDIT")]
    public int ThisLayerEdit { get; set; }

    /// <summary>
    /// 本层级删除.
    /// </summary>
    [SugarColumn(ColumnName = "F_THIS_LAYER_DELETE")]
    public int ThisLayerDelete { get; set; }

    /// <summary>
    /// 子层级添加.
    /// </summary>
    [SugarColumn(ColumnName = "F_SUB_LAYER_ADD")]
    public int SubLayerAdd { get; set; }

    /// <summary>
    /// 子层级编辑.
    /// </summary>
    [SugarColumn(ColumnName = "F_SUB_LAYER_EDIT")]
    public int SubLayerEdit { get; set; }

    /// <summary>
    /// 子层级删除.
    /// </summary>
    [SugarColumn(ColumnName = "F_SUB_LAYER_DELETE")]
    public int SubLayerDelete { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 本层级查看.
    /// </summary>
    [SugarColumn(ColumnName = "F_THIS_LAYER_SELECT")]
    public int ThisLayerSelect { get; set; }

    /// <summary>
    /// 子层级查看.
    /// </summary>
    [SugarColumn(ColumnName = "F_SUB_LAYER_SELECT")]
    public int SubLayerSelect { get; set; }

    /// <summary>
    /// 管理组.
    /// </summary>
    [SugarColumn(ColumnName = "F_MANAGER_GROUP")]
    public string ManagerGroup { get; set; }
}