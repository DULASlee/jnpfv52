using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// 评测指标定义（DB-28）
/// 版 本：v5.2.0
/// </summary>
[SugarTable("EVAL_METRIC", TableDescription = "评测指标定义")]
public class EvalMetricEntity : TenantCLDSEntityBase
{
    /// <summary>指标编码（如 pass_rate, latency_p95）</summary>
    [SugarColumn(ColumnName = "F_METRIC_CODE")]
    public string MetricCode { get; set; }

    /// <summary>指标名称</summary>
    [SugarColumn(ColumnName = "F_METRIC_NAME")]
    public string MetricName { get; set; }

    /// <summary>指标类型: numeric/boolean/string</summary>
    [SugarColumn(ColumnName = "F_METRIC_TYPE")]
    public string MetricType { get; set; }

    /// <summary>告警阈值</summary>
    [SugarColumn(ColumnName = "F_THRESHOLD_WARN")]
    public decimal? ThresholdWarn { get; set; }

    /// <summary>严重阈值</summary>
    [SugarColumn(ColumnName = "F_THRESHOLD_CRIT")]
    public decimal? ThresholdCrit { get; set; }

    /// <summary>单位: %, ms, count</summary>
    [SugarColumn(ColumnName = "F_UNIT")]
    public string? Unit { get; set; }

    /// <summary>指标描述</summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }
}
