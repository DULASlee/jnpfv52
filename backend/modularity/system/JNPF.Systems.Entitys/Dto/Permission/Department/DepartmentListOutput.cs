using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Department;

/// <summary>
/// 部门树列表输出.
/// </summary>
[SuppressSniffer]
public class DepartmentListOutput
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
    /// 部门名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 部门编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 部门经理（姓名/账号）.
    /// </summary>
    public string manager { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 排序号.
    /// </summary>
    public long? sortCode { get; set; }
}