using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.OrganizeAdministrator;

/// <summary>
/// 机构分级管理创建输入.
/// </summary>
[SuppressSniffer]
public class OrganizeAdminIsTratorCrInput : TreeModel
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
    /// 机构类型.
    /// </summary>
    public string organizeType { get; set; }

    /// <summary>
    /// 本层添加.
    /// </summary>
    public bool thisLayerAdd { get; set; }

    /// <summary>
    /// 本层编辑.
    /// </summary>
    public bool thisLayerEdit { get; set; }

    /// <summary>
    /// 本层删除.
    /// </summary>
    public bool thisLayerDelete { get; set; }

    /// <summary>
    /// 本层查看.
    /// </summary>
    public bool thisLayerSelect { get; set; }

    /// <summary>
    /// 子层添加.
    /// </summary>
    public bool subLayerAdd { get; set; }

    /// <summary>
    /// 子层编辑.
    /// </summary>
    public bool subLayerEdit { get; set; }

    /// <summary>
    /// 子层删除.
    /// </summary>
    public bool subLayerDelete { get; set; }

    /// <summary>
    /// 本层查看.
    /// </summary>
    public bool subLayerSelect { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    public string description { get; set; }
}