using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.ModuleDataAuthorize;

/// <summary>
/// 功能数据权限创建输入.
/// </summary>
[SuppressSniffer]
public class ModuleDataAuthorizeCrInput
{
    /// <summary>
    /// 功能主键.
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
    /// 绑定表格.
    /// </summary>
    public string bindTable { get; set; }

    /// <summary>
    /// 子表控件.
    /// </summary>
    public string childTableKey { get; set; }

    /// <summary>
    /// 时间格式.
    /// </summary>
    public string format { get; set; }
}