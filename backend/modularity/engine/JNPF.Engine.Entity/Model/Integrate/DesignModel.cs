using JNPF.Common.Dtos.Message;
using JNPF.DependencyInjection;
using System.ComponentModel;

namespace JNPF.Engine.Entity.Model.Integrate;

/// <summary>
/// 系统集成设计模型.
/// </summary>
[SuppressSniffer]
public class DesignModel
{
    /// <summary>
    /// 类型.
    /// </summary>
    public NodeType type { get; set; }

    /// <summary>
    /// 内容.
    /// </summary>
    public string content { get; set; }

    /// <summary>
    /// 节点属性.
    /// </summary>
    public NodeAttribute properties { get; set; }

    /// <summary>
    /// 节点ID.
    /// </summary>
    public string nodeId { get; set; }

    /// <summary>
    /// 父节点ID.
    /// </summary>
    public string prevId { get; set; }

    /// <summary>
    /// 子节点.
    /// </summary>
    public DesignModel childNode { get; set; }
}

/// <summary>
/// 节点属性.
/// </summary>
[SuppressSniffer]
public class NodeAttribute : NodeGenerality
{
    /// <summary>
    /// 通知人类型.
    /// </summary>
    public List<int> msgUserType { get; set; }

    /// <summary>
    /// 通知人.
    /// </summary>
    public List<string> msgUserIds { get; set; }

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

/// <summary>
/// 节点通用属性.
/// </summary>
[SuppressSniffer]
public class NodeGenerality
{
    /// <summary>
    /// 消息id.
    /// </summary>
    public string msgId { get; set; }

    /// <summary>
    /// 开启流程
    /// 0-关闭,1-开启.
    /// </summary>
    public int enableFlow { get; set; }

    /// <summary>
    /// 流程ID.
    /// </summary>
    public string flowId { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string msgName { get; set; }

    /// <summary>
    /// 模板json.
    /// </summary>
    public List<object> templateJson { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string title { get; set; }

    /// <summary>
    /// 表单类型
    /// 1-在线开发普通表单,2-在线开发流程表单,3-数据接口.
    /// </summary>
    public int formType { get; set; }

    /// <summary>
    /// 触发表单/接口id.
    /// </summary>
    public string formId { get; set; }

    /// <summary>
    /// 触发表单/接口名称.
    /// </summary>
    public string formName { get; set; }

    /// <summary>
    /// 表单/接口字段.
    /// </summary>
    public List<object> formFieldList { get; set; }

    /// <summary>
    /// 转移列表.
    /// </summary>
    public List<transferConfig> transferList { get; set; }

    /// <summary>
    /// 接口参数.
    /// </summary>
    public List<object> interfaceTemplateJson { get; set; }

    /// <summary>
    /// 触发事件
    /// 1-新增,2-修改,3-删除.
    /// </summary>
    public int triggerEvent { get; set; }

    /// <summary>
    /// 触发条件规则.
    /// </summary>
    public List<object> ruleList { get; set; }

    /// <summary>
    /// 触发条件规则匹配逻辑.
    /// </summary>
    public string ruleMatchLogic { get; set; }

    /// <summary>
    /// 未找到数据时
    /// 0-跳过,1-新增一条数据.
    /// </summary>
    public int unFoundRule { get; set; }

    /// <summary>
    /// 数据存在时
    /// 0-不新增数据,1-新增一条数据.
    /// </summary>
    public int addRule { get; set; }

    /// <summary>
    /// 数据存在时
    /// 0-删除未找到的数据,1-删除已找到的数据.
    /// </summary>
    public int deleteRule { get; set; }

    /// <summary>
    /// 发起人.
    /// </summary>
    public List<string> initiator { get; set; }
}

/// <summary>
/// 消息配置.
/// </summary>
[SuppressSniffer]
public class MessageConfig
{
    /// <summary>
    /// 通知类型
    /// 3-默认,1-自定义,0-关闭.
    /// </summary>
    public int on { get; set; }

    /// <summary>
    /// 消息id.
    /// </summary>
    public string msgId { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string msgName { get; set; }

    /// <summary>
    /// 模板json.
    /// </summary>
    public List<MessageSendModel> templateJson { get; set; }
}

/// <summary>
/// 转移配置.
/// </summary>
[SuppressSniffer]
public class transferConfig
{
    /// <summary>
    /// 目标字段.
    /// </summary>
    public string targetField { get; set; }

    /// <summary>
    /// 目标字段标题.
    /// </summary>
    public string targetFieldLabel { get; set; }

    /// <summary>
    /// 来源类型
    /// 1-字段,2-自定义.
    /// </summary>
    public int sourceType { get; set; }

    /// <summary>
    /// 来源值.
    /// </summary>
    public string sourceValue { get; set; }

    /// <summary>
    /// 是否必填.
    /// </summary>
    public bool required { get; set; }
}

/// <summary>
/// 节点类型.
/// </summary>
[SuppressSniffer]
public enum NodeType
{
    /// <summary>
    /// 添加数据.
    /// </summary>
    [Description("addData")]
    addData,

    /// <summary>
    /// 数据接口.
    /// </summary>
    [Description("dataInterface")]
    dataInterface,

    /// <summary>
    /// 删除数据.
    /// </summary>
    [Description("deleteData")]
    deleteData,

    /// <summary>
    /// 结束.
    /// </summary>
    [Description("end")]
    end,

    /// <summary>
    /// 获取数据.
    /// </summary>
    [Description("getData")]
    getData,

    /// <summary>
    /// 信息.
    /// </summary>
    [Description("message")]
    message,

    /// <summary>
    /// 开始.
    /// </summary>
    [Description("start")]
    start,

    /// <summary>
    /// 更新数据.
    /// </summary>
    [Description("updateData")]
    updateData,

    /// <summary>
    /// 发起流程.
    /// </summary>
    [Description("launchFlow")]
    launchFlow,
}

/// <summary>
/// 数据接口字段模型.
/// </summary>
[SuppressSniffer]
public class InterfaceFieldModel
{
    /// <summary>
    /// 替换值.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 字段.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 默认值.
    /// </summary>
    public string defaultValue { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string label { get; set; }
}