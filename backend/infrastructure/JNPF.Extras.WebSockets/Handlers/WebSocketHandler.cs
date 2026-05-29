using JNPF.Extras.WebSockets.Models;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace JNPF.WebSockets;

/// <summary>
/// WebSocket 处理程序.
/// </summary>
public abstract class WebSocketHandler
{
    /// <summary>
    /// WebSocket 连接管理.
    /// </summary>
    protected WebSocketConnectionManager WebSocketConnectionManager { get; set; }

    /// <summary>
    /// 初始化一个<see cref="WebSocketHandler"/>类型的新实例.
    /// </summary>
    public WebSocketHandler(WebSocketConnectionManager webSocketConnectionManager)
    {
        WebSocketConnectionManager = webSocketConnectionManager;
    }

    /// <summary>
    /// 连接.
    /// </summary>
    /// <param name="socket">socket.</param>
    /// <returns></returns>
    public virtual async Task OnConnected(WebSocketClient socket)
    {
        WebSocketConnectionManager.AddSocket(socket);
    }

    /// <summary>
    /// 连接.
    /// </summary>
    /// <param name="socketId">连接ID.</param>
    /// <param name="socket">socket.</param>
    /// <returns></returns>
    public virtual async Task OnConnected(string socketId, WebSocketClient socket)
    {
        WebSocketConnectionManager.AddSocket(socketId, socket);
    }

    /// <summary>
    /// 断开连接.
    /// </summary>
    /// <param name="socket"></param>
    /// <returns></returns>
    public virtual async Task OnDisconnected(WebSocket socket)
    {
        string socketId = WebSocketConnectionManager.GetId(socket);
        if (!string.IsNullOrWhiteSpace(socketId))
        {
            var socketClient = WebSocketConnectionManager.GetSocketById(socketId);
            await WebSocketConnectionManager.RemoveSocket(socketId).ConfigureAwait(false);
            switch (!string.IsNullOrEmpty(socketClient.TenantId) && !string.IsNullOrEmpty(socketClient.UserId))
            {
                case true:
                    WebSocketConnectionManager.RemoveFromUser(socketId, string.Format("{0}-{1}", socketClient.TenantId, socketClient.UserId));
                    WebSocketConnectionManager.RemoveFromTenant(socketId, socketClient.TenantId);
                    break;
            }
        }
    }

    /// <summary>
    /// 发送消息给指定 id 的 socket.
    /// </summary>
    /// <param name="client">socket 客户端.</param>
    /// <param name="message">消息.</param>
    /// <returns></returns>
    public async Task SendMessageAsync(WebSocketClient client, string message)
    {
        if (client.WebSocket.State != WebSocketState.Open)
            return;
        byte[] encodedMessage = Encoding.UTF8.GetBytes(message);
        try
        {
            await client.WebSocket.SendAsync(
                buffer: new ArraySegment<byte>(
                    array: encodedMessage,
                    offset: 0,
                    count: encodedMessage.Length),
                messageType: WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: CancellationToken.None).ConfigureAwait(false);
        }
        catch (WebSocketException e)
        {
            if (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                await OnDisconnected(client.WebSocket);
            }
        }
    }

    /// <summary>
    /// 给指定 socket 发送信息.
    /// </summary>
    /// <param name="socketId">连接ID.</param>
    /// <param name="message">消息内容.</param>
    /// <returns></returns>
    public async Task SendMessageAsync(string socketId, string message)
    {
        var socket = WebSocketConnectionManager.GetSocketById(socketId);
        if (socket != null)
            await SendMessageAsync(socket, message).ConfigureAwait(false);
    }

    /// <summary>
    /// 发送消息 全频道.
    /// </summary>
    /// <param name="message">消息内容.</param>
    /// <returns></returns>
    public async Task SendMessageToAllAsync(string message)
    {
        foreach (var pair in WebSocketConnectionManager.GetAll())
        {
            try
            {
                if (pair.Value.WebSocket.State == WebSocketState.Open)
                    await SendMessageAsync(pair.Value, message).ConfigureAwait(false);
            }
            catch (WebSocketException e)
            {
                if (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                {
                    await OnDisconnected(pair.Value.WebSocket);
                }
            }
        }
    }

    /// <summary>
    /// 给指定用户组发送信息.
    /// </summary>
    /// <param name="userID">用户ID.</param>
    /// <param name="message">消息内容.</param>
    /// <returns></returns>
    public async Task SendMessageToUserAsync(string userID, string message)
    {
        var sockets = WebSocketConnectionManager.GetAllFromUser(userID);
        if (sockets != null)
        {
            foreach (var socket in sockets)
            {
                await SendMessageAsync(socket, message);
            }
        }
    }

    /// <summary>
    /// 给指定租户组发送信息.
    /// </summary>
    /// <param name="tenantID">租户ID.</param>
    /// <param name="message">消息内容.</param>
    /// <returns></returns>
    public async Task SendMessageToTenantAsync(string tenantID, string message)
    {
        var sockets = WebSocketConnectionManager.GetAllFromTenant(tenantID);
        if (sockets != null)
        {
            foreach (var socket in sockets)
            {
                await SendMessageAsync(socket, message);
            }
        }
    }

    /// <summary>
    /// 给指定租户组发送信息.
    /// </summary>
    /// <param name="tenantID">租户ID.</param>
    /// <param name="message">消息内容.</param>
    /// <param name="except">除了某个用户.</param>
    /// <returns></returns>
    public async Task SendMessageToTenantAsync(string tenantID, string message, string except)
    {
        var sockets = WebSocketConnectionManager.GetAllFromTenant(tenantID);
        if (sockets != null)
        {
            foreach (var id in sockets)
            {
                if (id != except)
                    await SendMessageAsync(id, message);
            }
        }
    }

    /// <summary>
    /// 获取租户组内全部用户ID.
    /// </summary>
    /// <param name="tenantID"></param>
    /// <returns></returns>
    public List<string> GetAllUserIdFromTenant(string tenantID)
    {
        List<string> connectionList = new List<string>();
        foreach (var item in WebSocketConnectionManager.GetAllFromTenant(tenantID))
        {
            var client = WebSocketConnectionManager.GetSocketById(item);
            if (client != null && !connectionList.Any(it => it.Equals(client.UserId)))
                connectionList.Add(client.UserId);
        }

        return connectionList;
    }

    /// <summary>
    /// 接收信息.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="result"></param>
    /// <param name="receivedMessage"></param>
    /// <returns></returns>
    public virtual async Task ReceiveAsync(WebSocketClient client, WebSocketReceiveResult result, string receivedMessage)
    {
        try
        {
            await SendMessageAsync(client, receivedMessage).ConfigureAwait(false);
        }
        catch (TargetParameterCountException)
        {
            await SendMessageAsync(client, JsonSerializer.Serialize(new { method = "error", msg = $"does not take parameters!" })).ConfigureAwait(false);
        }
        catch (ArgumentException)
        {
            await SendMessageAsync(client, JsonSerializer.Serialize(new { method = "error", msg = $"takes different arguments!" })).ConfigureAwait(false);
        }
    }
}