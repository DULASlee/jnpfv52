using JNPF.Message.Entitys.Entity;

namespace JNPF.Message.Interfaces.Message;

/// <summary>
/// 系统消息
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
public interface IShortLinkService
{
    Task<MessageShortLinkEntity> Create(string userId, string bodyText);

    Task<string> CreateToken(string userId, string tenantId);
}