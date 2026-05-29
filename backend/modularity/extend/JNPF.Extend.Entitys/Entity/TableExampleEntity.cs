using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Extend.Entitys;

/// <summary>
/// 表格示例数据
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01 .
/// </summary>
[SugarTable("EXT_TABLE_EXAMPLE")]
public class TableExampleEntity : CLDSEntityBase
{
    /// <summary>
    /// 交互日期.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_INTERACTION_DATE")]
    public DateTime? InteractionDate { get; set; }

    /// <summary>
    /// 项目编码.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_PROJECT_CODE")]
    public string? ProjectCode { get; set; }

    /// <summary>
    /// 项目名称.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_PROJECT_NAME")]
    public string? ProjectName { get; set; }

    /// <summary>
    /// 负责人.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_PRINCIPAL")]
    public string? Principal { get; set; }

    /// <summary>
    /// 立顶人.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_JACK_STANDS")]
    public string? JackStands { get; set; }

    /// <summary>
    /// 项目类型.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_PROJECT_TYPE")]
    public string? ProjectType { get; set; }

    /// <summary>
    /// 项目阶段.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_PROJECT_PHASE")]
    public string? ProjectPhase { get; set; }

    /// <summary>
    /// 客户名称.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_CUSTOMER_NAME")]
    public string? CustomerName { get; set; }

    /// <summary>
    /// 费用金额.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_COST_AMOUNT")]
    public decimal? CostAmount { get; set; }

    /// <summary>
    /// 已用金额.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_TUNES_AMOUNT")]
    public decimal? TunesAmount { get; set; }

    /// <summary>
    /// 预计收入.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_PROJECTED_INCOME")]
    public decimal? ProjectedIncome { get; set; }

    /// <summary>
    /// 登记人.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_REGISTRANT")]
    public string? Registrant { get; set; }

    /// <summary>
    /// 登记时间.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_REGISTER_DATE")]
    public DateTime? RegisterDate { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }

    /// <summary>
    /// 标记.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_SIGN")]
    public string? Sign { get; set; }

    /// <summary>
    /// 批注列表Json.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_POSTIL_JSON")]
    public string? PostilJson { get; set; }

    /// <summary>
    /// 批注总数.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_POSTIL_COUNT")]
    public int? PostilCount { get; set; }
}