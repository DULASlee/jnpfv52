using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.BillRule;

/// <summary>
/// 单据规则创建输入.
/// </summary>
[SuppressSniffer]
public class BillRuleCrInput
{
    /// <summary>
    /// 业务名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 业务编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 流水前缀.
    /// </summary>
    public string prefix { get; set; }

    /// <summary>
    /// 流水日期.
    /// </summary>
    public string dateFormat { get; set; }

    /// <summary>
    /// 流水位数.
    /// </summary>
    public int? digit { get; set; }

    /// <summary>
    /// 流水起始.
    /// </summary>
    public string startNumber { get; set; }

    /// <summary>
    /// 流水范例.
    /// </summary>
    public string example { get; set; }

    /// <summary>
    /// 流水状态.
    /// </summary>
    public int enabledMark { get; set; }

    /// <summary>
    /// 流水说明.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    public string? category { get; set; }

    /// <summary>
    /// 方式.
    /// 0：数据格式，1：随机数编号，2：UUID.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 随机数位数.
    /// </summary>
    public int? randomDigit { get; set; }

    /// <summary>
    /// 随机数类型.
    /// 1：纯数字，2：字母加数字.
    /// </summary>
    public int? randomType { get; set; }

    /// <summary>
    /// 后缀.
    /// </summary>
    public string suffix { get; set; }
}