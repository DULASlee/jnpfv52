using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Role;

/// <summary>
/// 角色创建输入.
/// </summary>
[SuppressSniffer]
public class RoleCrInput
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
    /// 类型.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 有效标记.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 组织Id集合.
    /// </summary>
    public List<List<string>> organizeIdsTree { get; set; }

    /// <summary>
    /// 全局标识 1:全局 0 组织.
    /// </summary>
    public int globalMark { get; set; }
}