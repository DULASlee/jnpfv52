using JNPF.WorkFlow.Entitys.Model.Conifg;

namespace JNPF.WorkFlow.Entitys.Model.Properties;

public class MsgProperties
{
    /// <summary>
    /// 审核通过.
    /// </summary>
    public MsgConfig? approveMsgConfig { get; set; } = new MsgConfig();

    /// <summary>
    /// 审核驳回.
    /// </summary>
    public MsgConfig? rejectMsgConfig { get; set; } = new MsgConfig();

    /// <summary>
    /// 审核抄送.
    /// </summary>
    public MsgConfig? copyMsgConfig { get; set; } = new MsgConfig();

    /// <summary>
    /// 审核超时.
    /// </summary>
    public MsgConfig? overTimeMsgConfig { get; set; } = new MsgConfig();

    /// <summary>
    /// 审核提醒.
    /// </summary>
    public MsgConfig? noticeMsgConfig { get; set; } = new MsgConfig();

    /// <summary>
    /// 发起通知.
    /// </summary>
    public MsgConfig? launchMsgConfig { get; set; } = new MsgConfig();

    /// <summary>
    /// 审核.
    /// </summary>
    public MsgConfig? waitMsgConfig { get; set; } = new MsgConfig();

    /// <summary>
    /// 结束.
    /// </summary>
    public MsgConfig? endMsgConfig { get; set; } = new MsgConfig();
}
