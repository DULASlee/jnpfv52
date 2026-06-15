namespace JNPF.InteAssistant.Entitys.Enum;

/// <summary>
/// 流水线阶段状态枚举
/// 用于 F_STAGE_STATUS 字段
/// </summary>
public enum PipelineStatus
{
    /// <summary>等待开始</summary>
    Pending = 0,

    /// <summary>执行中</summary>
    Running = 1,

    /// <summary>等待人工确认</summary>
    Review = 2,

    /// <summary>已确认</summary>
    Approved = 3,

    /// <summary>被否决</summary>
    Rejected = 4,

    /// <summary>熔断</summary>
    Blocked = 5,

    /// <summary>超时未操作</summary>
    Stale = 6,

    /// <summary>部署中</summary>
    Deploying = 7,

    /// <summary>完成</summary>
    Completed = 8,

    /// <summary>失败</summary>
    Failed = 9,

    /// <summary>校验中（过渡态）</summary>
    Validating = 10,

    /// <summary>已放弃（终止态）</summary>
    Abandoned = 11
}
