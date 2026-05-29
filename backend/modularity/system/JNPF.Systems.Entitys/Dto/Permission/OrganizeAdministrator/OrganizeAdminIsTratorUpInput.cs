using JNPF.Common.Security;

namespace JNPF.Systems.Entitys.Dto.OrganizeAdministrator;

/// <summary>
/// 更新机构分级管理输入.
/// </summary>
public class OrganizeAdminIsTratorUpInput : OrganizeAdminCrInput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 权限组织集合.
    /// </summary>
    public List<OrganizeAdminCrInput> orgAdminModel { get; set; }

    /// <summary>
    /// 菜单集合.
    /// </summary>
    public List<string> moduleIds { get; set; }

    /// <summary>
    /// 应用集合.
    /// </summary>
    public List<string> systemIds { get; set; }

    /// <summary>
    /// 管理组.
    /// </summary>
    public string managerGroup { get; set; }
}

/// <summary>
/// 机构分级管理创建输入.
/// </summary>
public class OrganizeAdminCrInput : TreeModel
{
    /// <summary>
    /// 用户主键.
    /// </summary>
    public string userId { get; set; }

    /// <summary>
    /// 机构主键.
    /// </summary>
    public string organizeId { get; set; }

    /// <summary>
    /// 本层添加.
    /// </summary>
    public int thisLayerAdd { get; set; }

    /// <summary>
    /// 本层编辑.
    /// </summary>
    public int thisLayerEdit { get; set; }

    /// <summary>
    /// 本层删除.
    /// </summary>
    public int thisLayerDelete { get; set; }

    /// <summary>
    /// 本层查看.
    /// </summary>
    public int thisLayerSelect { get; set; }

    /// <summary>
    /// 子层添加.
    /// </summary>
    public int subLayerAdd { get; set; }

    /// <summary>
    /// 子层编辑.
    /// </summary>
    public int subLayerEdit { get; set; }

    /// <summary>
    /// 子层删除.
    /// </summary>
    public int subLayerDelete { get; set; }

    /// <summary>
    /// 本层查看.
    /// </summary>
    public int subLayerSelect { get; set; }

}