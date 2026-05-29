using JNPF.WebSockets;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// WebSocket 中间件拓展.
/// </summary>
public static class WebSocketApplicationBuilderExtensions
{
    /// <summary>
    /// 映射 WebSocket 管理.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="path"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static IApplicationBuilder MapWebSocketManager(
        this IApplicationBuilder app,
        PathString path,
        WebSocketHandler handler)
    {
        return app.Map(path, (_app) => _app.UseMiddleware<WebSocketMiddleware>(handler));
    }
}