using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// 系统集成节点
/// 版 本：v3.5.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2023-9-4.
/// </summary>
[SugarTable("BASE_INTEGRATE_NODE", TableDescription = "系统集成节点")]
public class IntegrateNodeEntity : CLDSEntityBase
{
    /// <summary>
    /// 任务主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_ID")]
    public string TaskId { get; set; }

    /// <summary>
    /// 表单主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_FORM_ID")]
    public string FormId { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_TYPE")]
    public string NodeType { get; set; }

    /// <summary>
    /// 开始时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_START_TIME")]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_END_TIME")]
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 异常提示.
    /// </summary>
    [SugarColumn(ColumnName = "F_ERROR_MSG")]
    public string ErrorMsg { get; set; }

    /// <summary>
    /// 节点编号.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_CODE")]
    public string NodeCode { get; set; }

    /// <summary>
    /// 节点名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_NAME")]
    public string NodeName { get; set; }

    /// <summary>
    /// 下节点.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_NEXT")]
    public string NodeNext { get; set; }

    /// <summary>
    /// 是否完成
    /// 0-失败,1-成功.
    /// </summary>
    [SugarColumn(ColumnName = "F_RESULT_TYPE")]
    public int ResultType { get; set; }

    /// <summary>
    /// 节点属性Json.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_PROPERTY_JSON")]
    public string NodePropertyJson { get; set; }

    /// <summary>
    /// 描述或说明.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 父节点id.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_ID")]
    public string ParentId { get; set; }

    /// <summary>
    /// 能否重试
    /// 0-重试,1-否.
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_RETRY")]
    public int IsRetry { get; set; }
}