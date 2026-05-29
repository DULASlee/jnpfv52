using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.ModuleDataAuthorize;

/// <summary>
/// 功能权限数据列表输出.
/// </summary>
[SuppressSniffer]
public class ModuleDataAuthorizeListOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 字段名称.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 字段注释.
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
    /// 条件内容.
    /// </summary>
    public string conditionName { get; set; }

    /// <summary>
    /// 条件符号.
    /// </summary>
    public string conditionSymbolName { get; set; }

    /// <summary>
    /// 绑定表格.
    /// </summary>
    public string bindTable { get; set; }

    /// <summary>
    /// 规则(0:主表，1：副表).
    /// </summary>
    public int? fieldRule { get; set; }

    /// <summary>
    /// 日期类型.
    /// </summary>
    public string format { get; set; }
}