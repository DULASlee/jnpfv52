using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.ModuleColumn;

/// <summary>
/// 功能列表创建输入.
/// </summary>
[SuppressSniffer]
public class ModuleColumnCrInput
{
    /// <summary>
    /// 菜单id.
    /// </summary>
    public string moduleId { get; set; }

    /// <summary>
    /// 绑定表格描述.
    /// </summary>
    public string bindTableName { get; set; }

    /// <summary>
    /// 字段名称.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 字段注解.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 状态(1-可用，0-不可用).
    /// </summary>
    public int enabledMark { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 绑定表格.
    /// </summary>
    public string bindTable { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 规则(0:主表，1：副表).
    /// </summary>
    public int? fieldRule { get; set; }

    /// <summary>
    /// 子表控件.
    /// </summary>
    public string childTableKey { get; set; }
}