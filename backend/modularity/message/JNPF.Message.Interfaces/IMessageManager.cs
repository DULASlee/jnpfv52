using JNPF.Common.Dtos.Message;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.Message.Entitys;

namespace JNPF.Message.Interfaces;

/// <summary>
/// 消息中心处理接口类.
/// </summary>
public interface IMessageManager
{
    Task SendDefaultMsg(List<string> toUserIds, List<MessageEntity> messageList);

    Task<string> SendDefinedMsg(MessageSendModel messageSendModel, Dictionary<string, object> bodyDic, IntegrateTaskEntity? taskEntity = null);

    Task<List<MessageSendModel>> GetMessageSendModels(string sendConfigId);

    Task ForcedOffline(string connectionId);

    List<MessageEntity> GetMessageList(string enCode, List<string> toUserIds, Dictionary<string, string> paramDic, int type, Dictionary<string, object> bodyDic = null, int flowType = 1);
}
