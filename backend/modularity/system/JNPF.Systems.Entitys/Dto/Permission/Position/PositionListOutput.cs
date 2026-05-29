using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Position;

/// <summary>
/// 岗位列表输出.
/// </summary>
[SuppressSniffer]
public class PositionListOutput
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
    /// 岗位名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 岗位编号.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 岗位类型.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 部门.
    /// </summary>
    public string department { get; set; }

    /// <summary>
    /// 有效标志.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 组织Id.
    /// </summary>
    public string organizeId { get; set; }
}

public class PositionTreeOutput : TreeModel
{
    /// <summary>
    /// 岗位名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 岗位编号.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 岗位类型.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 部门.
    /// </summary>
    public string department { get; set; }

    /// <summary>
    /// 有效标志.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 组织Id.
    /// </summary>
    public string organizeId { get; set; }
}