using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.Integrate;

/// <summary>
/// 集成事件节点模型.
/// </summary>
[SuppressSniffer]
public class IntegratedEventNodeModel
{
    /// <summary>
    /// 节点ID.
    /// </summary>
    public string nodeId { get; set; }

    /// <summary>
    /// 父节点ID.
    /// </summary>
    public string prevId { get; set; }

    /// <summary>
    /// 下个节点.
    /// </summary>
    public string nextId { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public NodeType type { get; set; }

    /// <summary>
    /// 集成类型
    /// 1-触发,2-定时.
    /// </summary>
    public int integrateType { get; set; }

    /// <summary>
    /// 开始时间.
    /// </summary>
    public DateTime? startTime { get; set; }

    /// <summary>
    /// 结束时间.
    /// </summary>
    public DateTime? endTime { get; set; }

    /// <summary>
    /// 节点属性.
    /// </summary>
    public IntegrateProperties properties { get; set; }
}

[SuppressSniffer]
public class IntegrateProperties
{
    /// <summary>
    /// 标题.
    /// </summary>
    public string title;

    /// <summary>
    /// 触发表单/接口id.
    /// </summary>
    public string formId;

    /// <summary>
    /// 表单类型
    /// 1-在线开发普通表单,2-在线开发流程表单,3-数据接口.
    /// </summary>
    public int formType = 1;

    /// <summary>
    /// 表单/接口字段.
    /// </summary>
    public List<object> formFieldList { get; set; }

    /// <summary>
    /// 接口参数.
    /// </summary>
    public List<object> interfaceTemplateJson { get; set; }

    /// <summary>
    /// 触发条件规则.
    /// </summary>
    public List<object> ruleList { get; set; }

    /// <summary>
    /// 触发事件
    /// 1-新增,2-修改,3-删除.
    /// </summary>
    public int triggerEvent { get; set; }

    /// <summary>
    /// 0-不新增 1-新增.
    /// </summary>
    public int addRule { get; set; }

    /// <summary>
    /// 0-不更新 1-新增.
    /// </summary>
    public int unFoundRule { get; set; }

    /// <summary>
    /// 0-删除未找到 1-删除已找到.
    /// </summary>
    public int deleteRule { get; set; }

    /// <summary>
    /// 消息模版ID.
    /// </summary>
    public string msgId;

    /// <summary>
    /// 通知人类型.
    /// </summary>
    public List<string> msgUserType { get; set; }

    /// <summary>
    /// 通知人.
    /// </summary>
    public List<string> msgUserIds { get; set; }

    public List<object> templateJson { get; set; }

    /// <summary>
    /// 执行失败通知.
    /// </summary>
    public MessageConfig failMsgConfig { get; set; }

    /// <summary>
    /// 开始执行通知.
    /// </summary>
    public MessageConfig startMsgConfig { get; set; }

    /// <summary>
    /// 触发开始时间.
    /// </summary>
    public DateTime? startTime { get; set; }

    /// <summary>
    /// cron表达式.
    /// </summary>
    public string cron { get; set; }

    /// <summary>
    /// 触发结束时间类型.
    /// </summary>
    public int endTimeType { get; set; }

    /// <summary>
    /// 触发次数.
    /// </summary>
    public int endLimit { get; set; }

    /// <summary>
    /// 触发结束时间.
    /// </summary>
    public DateTime? endTime { get; set; }

    /// <summary>
    /// 触发指定事件.
    /// </summary>
    public int integrateType { get; set; }
}