using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 单据规则
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_BILL_RULE")]
public class BillRuleEntity : CLDSEntityBase
{
    /// <summary>
    /// 单据名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 单据编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 单据前缀.
    /// </summary>
    [SugarColumn(ColumnName = "F_PREFIX")]
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// 日期格式.
    /// </summary>
    [SugarColumn(ColumnName = "F_DATE_FORMAT")]
    public string DateFormat { get; set; }

    /// <summary>
    /// 流水位数.
    /// </summary>
    [SugarColumn(ColumnName = "F_DIGIT")]
    public int? Digit { get; set; }

    /// <summary>
    /// 流水起始.
    /// </summary>
    [SugarColumn(ColumnName = "F_START_NUMBER")]
    public string StartNumber { get; set; }

    /// <summary>
    /// 流水范例.
    /// </summary>
    [SugarColumn(ColumnName = "F_EXAMPLE")]
    public string Example { get; set; }

    /// <summary>
    /// 当前流水号.
    /// </summary>
    [SugarColumn(ColumnName = "F_THIS_NUMBER")]
    public int? ThisNumber { get; set; } = 0;

    /// <summary>
    /// 输出流水号.
    /// </summary>
    [SugarColumn(ColumnName = "F_OUTPUT_NUMBER")]
    public string OutputNumber { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 分类id.
    /// </summary>
    [SugarColumn(ColumnName = "F_CATEGORY")]
    public string? Category { get; set; }

    /// <summary>
    /// 方式.
    /// 1：时间格式，2：随机数编号，3：UUID.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int Type { get; set; }

    /// <summary>
    /// 随机数位数.
    /// </summary>
    [SugarColumn(ColumnName = "F_RANDOM_DIGIT")]
    public int? RandomDigit { get; set; }

    /// <summary>
    /// 随机数类型.
    /// 1：纯数字，2：字母加数字.
    /// </summary>
    [SugarColumn(ColumnName = "F_RANDOM_TYPE")]
    public int? RandomType { get; set; }

    /// <summary>
    /// 后缀.
    /// </summary>
    [SugarColumn(ColumnName = "F_SUFFIX")]
    public string Suffix { get; set; } = string.Empty;
}