using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Department;

/// <summary>
/// 部门下拉树输出 .
/// </summary>
[SuppressSniffer]
public class DepartmentSelectorOutput : TreeModel
{
    /// <summary>
    /// 部门名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 当前节点标识.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 组织树.
    /// </summary>
    public string organizeIdTree { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; } = "icon-ym icon-ym-tree-department1";

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 组织树名称.
    /// </summary>
    public string organize { get; set; }

    /// <summary>
    /// 组织树Id.
    /// </summary>
    public List<string> organizeIds { get; set; }

    /// <summary>
    /// 是否可选.
    /// </summary>
    public bool disabled = false;
}