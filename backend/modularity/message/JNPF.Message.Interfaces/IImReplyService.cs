namespace JNPF.Message.Interfaces;

/// <summary>
/// 业务抽象：消息会话.
/// </summary>
public interface IImReplyService
{
    /// <summary>
    /// 强制下线.
    /// </summary>
    /// <param name="connectionId"></param>
    void ForcedOffline(string connectionId);
}