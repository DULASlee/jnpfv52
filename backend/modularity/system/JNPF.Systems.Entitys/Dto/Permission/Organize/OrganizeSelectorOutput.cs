using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Organize;

/// <summary>
/// 机构树列表输出 .
/// </summary>
[SuppressSniffer]
public class OrganizeSelectorOutput : TreeModel
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 组织树.
    /// </summary>
    public string organizeIdTree { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; } = "icon-ym icon-ym-tree-department1";

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
