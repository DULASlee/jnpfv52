using JNPF.Extras.WebSockets.Models;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace JNPF.WebSockets;

/// <summary>
/// WebSocket 连接管理.
/// </summary>
public class WebSocketConnectionManager
{
    /// <summary>
    /// 全部 socket 池.
    /// </summary>
    private ConcurrentDictionary<string, WebSocketClient> _sockets = new ConcurrentDictionary<string, WebSocketClient>();

    /// <summary>
    /// 用户组 socket 池.
    /// </summary>
    private ConcurrentDictionary<string, List<string>> _users = new ConcurrentDictionary<string, List<string>>();

    /// <summary>
    /// 租户组 socket 池.
    /// </summary>
    private ConcurrentDictionary<string, List<string>> _tenant = new ConcurrentDictionary<string, List<string>>();

    /// <summary>
    /// 获取指定 id 的 socket.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public WebSocketClient GetSocketById(string id)
    {
        if (_sockets.TryGetValue(id, out WebSocketClient socket))
            return socket;
        else
            return null;
    }

    /// <summary>
    /// 获取全部socket.
    /// </summary>
    /// <returns></returns>
    public ConcurrentDictionary<string, WebSocketClient> GetAll()
    {
        return _sockets;
    }

    /// <summary>
    /// 获取租户组内全部socket.
    /// </summary>
    /// <param name="tenantID"></param>
    /// <returns></returns>
    public List<string> GetAllFromTenant(string tenantID)
    {
        if (_tenant.ContainsKey(tenantID))
        {
            return _tenant[tenantID];
        }

        return default;
    }

    /// <summary>
    /// 获取用户组内全部socket.
    /// </summary>
    /// <param name="userID"></param>
    /// <returns></returns>
    public List<string> GetAllFromUser(string userID)
    {
        if (_users.ContainsKey(userID))
        {
            return _users[userID];
        }

        return default;
    }

    /// <summary>
    /// 根据 socket 获取其 id.
    /// </summary>
    /// <param name="socket"></param>
    /// <returns></returns>
    public string GetId(WebSocket socket)
    {
        return _sockets.FirstOrDefault(p => p.Value.WebSocket == socket).Key;
    }

    /// <summary>
    /// 添加无连接ID socket.
    /// </summary>
    /// <param name="socket"></param>
    public void AddSocket(WebSocketClient socket)
    {
        _sockets.TryAdd(CreateConnectionId(), socket);
    }

    /// <summary>
    /// 添加带连接 id socket.
    /// </summary>
    /// <param name="socketID"></param>
    /// <param name="socket"></param>
    public void AddSocket(string socketID, WebSocketClient socket)
    {
        _sockets.TryAdd(socketID, socket);
    }

    /// <summary>
    /// 将连接添加至租户组.
    /// </summary>
    /// <param name="socketID"></param>
    /// <param name="tenantID"></param>
    public void AddToTenant(string socketID, string tenantID)
    {
        if (_tenant.ContainsKey(tenantID))
        {
            if (!_tenant[tenantID].Contains(socketID)) _tenant[tenantID].Add(socketID);
            return;
        }

        _tenant.TryAdd(tenantID, new List<string> { socketID });
    }

    /// <summary>
    /// 移除租户内某个连接.
    /// </summary>
    /// <param name="socketID"></param>
    /// <param name="tenantID"></param>
    public void RemoveFromTenant(string socketID, string tenantID)
    {
        if (_tenant.ContainsKey(tenantID))
        {
            _tenant[tenantID].Remove(socketID);
        }
    }

    /// <summary>
    /// 将连接添加至用户组
    /// 用户可以多端登录.
    /// </summary>
    /// <param name="socketID"></param>
    /// <param name="userID">格式为租户ID+用户ID.</param>
    public void AddToUser(string socketID, string userID)
    {
        if (_users.ContainsKey(userID))
        {
            if (!_users[userID].Contains(socketID)) _users[userID].Add(socketID);
            return;
        }

        _users.TryAdd(userID, new List<string> { socketID });
    }

    /// <summary>
    /// 移除用户组内某个连接.
    /// </summary>
    /// <param name="socketID"></param>
    /// <param name="userID">格式为租户ID+用户ID.</param>
    public void RemoveFromUser(string socketID, string userID)
    {
        if (_users.ContainsKey(userID))
        {
            _users[userID].Remove(socketID);
        }
    }

    /// <summary>
    /// 移除某个 socket.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task RemoveSocket(string id)
    {
        if (id == null) return;

        if (_sockets.TryRemove(id, out WebSocketClient client))
        {
            if (client.WebSocket.State != WebSocketState.Open) return;

            await client.WebSocket.CloseAsync(closeStatus: WebSocketCloseStatus.NormalClosure,
                                    statusDescription: "Closed by the WebSocketManager",
                                    cancellationToken: CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 生成连接ID.
    /// </summary>
    /// <returns></returns>
    private string CreateConnectionId()
    {
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// 获取全部客户端数量.
    /// </summary>
    /// <returns></returns>
    public int GetSocketClientCount()
    {
        return _sockets.Count();
    }

    /// <summary>
    /// 返回用户组客户端数量.
    /// </summary>
    /// <param name="userID"></param>
    /// <returns></returns>
    public int GetSocketClientToUserCount(string userID)
    {
        if (_users.ContainsKey(userID))
            return _users[userID].Count();
        return 0;
    }
}