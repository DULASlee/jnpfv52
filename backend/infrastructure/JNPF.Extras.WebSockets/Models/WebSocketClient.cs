using JNPF.Common.Enums;
using System.Net.WebSockets;

namespace JNPF.Extras.WebSockets.Models;

/// <summary>
/// WebSocket客户端信息.
/// </summary>
public class WebSocketClient
{
    /// <summary>
    /// 连接Id.
    /// </summary>
    public string ConnectionId { get; set; }

    /// <summary>
    /// 用户Id.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// 用户账号.
    /// </summary>
    public string Account { get; set; }

    /// <summary>
    /// 头像.
    /// </summary>
    public string HeadIcon { get; set; }

    /// <summary>
    /// 用户名称.
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// 租户ID.
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// 登录IP.
    /// </summary>
    public string LoginIpAddress { get; set; }

    /// <summary>
    /// 登录设备.
    /// </summary>
    public string LoginPlatForm { get; set; }

    /// <summary>
    /// 登录时间.
    /// </summary>
    public string LoginTime { get; set; }

    /// <summary>
    /// 移动端.
    /// </summary>
    public bool IsMobileDevice { get; set; }

    /// <summary>
    /// 单一登录方式（1：后登录踢出先登录 2：同时登录）.
    /// </summary>
    public LoginMethod SingleLogin { get; set; }

    /// <summary>
    /// 单点登录标识.
    /// </summary>
    public string onlineTicket { get; set; }

    /// <summary>
    /// WebSocket对象.
    /// </summary>
    public WebSocket WebSocket { get; set; }

    public string SystemId { get; set; }
}