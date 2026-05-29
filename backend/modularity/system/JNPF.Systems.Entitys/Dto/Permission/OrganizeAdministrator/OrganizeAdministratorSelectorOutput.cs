using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Permission.OrganizeAdministrator;

/// <summary>
/// 二级管理组织编辑选择输出.
/// </summary>
[SuppressSniffer]
public class OrganizeAdministratorSelectorOutput : TreeModel
{
    /// <summary>
    /// 分类.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 主键.
    /// </summary>
    public string organizeId { get; set; }

    /// <summary>
    /// 组织树.
    /// </summary>
    public string organizeIdTree { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 本层级查看.
    /// </summary>
    public int thisLayerSelect { get; set; }

    /// <summary>
    /// 子层级查看.
    /// </summary>
    public int subLayerSelect { get; set; }

    /// <summary>
    /// 本层级添加.
    /// </summary>
    public int thisLayerAdd { get; set; }

    /// <summary>
    /// 本层级编辑.
    /// </summary>
    public int thisLayerEdit { get; set; }

    /// <summary>
    /// 本层级删除.
    /// </summary>
    public int thisLayerDelete { get; set; }

    /// <summary>
    /// 子层级添加.
    /// </summary>
    public int subLayerAdd { get; set; }

    /// <summary>
    /// 子层级编辑.
    /// </summary>
    public int subLayerEdit { get; set; }

    /// <summary>
    /// 子层级删除.
    /// </summary>
    public int subLayerDelete { get; set; }
}
