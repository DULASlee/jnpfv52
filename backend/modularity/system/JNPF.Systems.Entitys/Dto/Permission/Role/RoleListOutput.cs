using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Role;

/// <summary>
/// 角色列表输出.
/// </summary>
[SuppressSniffer]
public class RoleListOutput
{
    /// <summary>
    /// 获取节点id.
    /// </summary>
    /// <returns></returns>
    public string id { get; set; }

    /// <summary>
    /// 获取节点父id.
    /// </summary>
    /// <returns></returns>
    public string parentId { get; set; }

    /// <summary>
    /// 子节点数量.
    /// </summary>
    public int num { get; set; }

    /// <summary>
    /// 唯一Id.
    /// </summary>
    public string onlyId { get; set; }

    /// <summary>
    /// 组织Id.
    /// </summary>
    public string organizeId { get; set; }

    /// <summary>
    /// 组织树.
    /// </summary>
    public string organizeInfo { get; set; }

    /// <summary>
    /// 组织树.
    /// </summary>
    public string organize { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 有效标记.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; } = "icon-ym icon-ym-tree-department1";
}

public class RoleListTreeOutput : TreeModel
{
    /// <summary>
    /// 唯一Id.
    /// </summary>
    public string onlyId { get; set; }

    /// <summary>
    /// 组织Id.
    /// </summary>
    public string organizeId { get; set; }

    /// <summary>
    /// 组织树.
    /// </summary>
    public string organizeInfo { get; set; }

    /// <summary>
    /// 组织树.
    /// </summary>
    public string organize { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 有效标记.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; } = "icon-ym icon-ym-tree-department1";
}