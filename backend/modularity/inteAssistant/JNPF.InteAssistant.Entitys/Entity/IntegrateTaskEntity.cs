using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// 系统集成任务
/// 版 本：v3.5.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2023-9-4.
/// </summary>
[SugarTable("BASE_INTEGRATE_TASK", TableDescription = "系统集成任务")]
public class IntegrateTaskEntity : CLDSEntityBase
{
    /// <summary>
    /// 实例主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_PROCESS_ID")]
    public string ProcessId { get; set; }

    /// <summary>
    /// 父节点时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_TIME")]
    public DateTime? ParentTime { get; set; }

    /// <summary>
    /// 父节点id.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_ID")]
    public string ParentId { get; set; }

    /// <summary>
    /// 执行时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_EXECUTION_TIME")]
    public DateTime? ExecutionTime { get; set; }

    /// <summary>
    /// 集成模板.
    /// </summary>
    [SugarColumn(ColumnName = "F_TEMPLATE_JSON")]
    public string TemplateJson { get; set; }

    /// <summary>
    /// 数据.
    /// </summary>
    [SugarColumn(ColumnName = "F_DATA")]
    public string Data { get; set; }

    /// <summary>
    /// 数据主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_DATA_ID")]
    public string DataId { get; set; }

    /// <summary>
    /// 集成类型
    /// 1-触发,2-定时.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int Type { get; set; }

    /// <summary>
    /// 集成主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_INTEGRATE_ID")]
    public string IntegrateId { get; set; }

    /// <summary>
    /// 结果
    /// 0-失败,1-成功.
    /// </summary>
    [SugarColumn(ColumnName = "F_RESULT_TYPE")]
    public int ResultType { get; set; }

    /// <summary>
    /// 描述或说明.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }
}