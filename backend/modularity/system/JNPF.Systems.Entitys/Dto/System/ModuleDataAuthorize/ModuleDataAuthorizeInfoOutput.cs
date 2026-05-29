using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.ModuleDataAuthorize;

/// <summary>
/// 功能权限数据信息输出.
/// </summary>
[SuppressSniffer]
public class ModuleDataAuthorizeInfoOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 菜单id.
    /// </summary>
    public string moduleId { get; set; }

    /// <summary>
    /// 字段名称.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 字段注解.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 字段类型.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 条件符号.
    /// </summary>
    public string conditionSymbol { get; set; }

    /// <summary>
    /// 条件内容.
    /// </summary>
    public string conditionText { get; set; }

    /// <summary>
    /// 字段说明.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 规则(0:主表，1：副表).
    /// </summary>
    public int? fieldRule { get; set; }

    /// <summary>
    /// 表名.
    /// </summary>
    public string bindTable { get; set; }

    /// <summary>
    /// 子表关联字段.
    /// </summary>
    public string childTableKey { get; set; }

    /// <summary>
    /// 时间格式.
    /// </summary>
    public string format { get; set; }
}