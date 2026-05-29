using JNPF.WebSockets;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// WebSocket服务集合拓展.
/// </summary>
public static class WebSocketServiceCollectionExtensions
{
    public static IServiceCollection AddWebSocketManager(this IServiceCollection services)
    {
        services.AddTransient<WebSocketConnectionManager>();

        var types = AppDomain.CurrentDomain.GetAssemblies()
              .SelectMany(a => a.GetTypes().Where(t => t.BaseType == typeof(WebSocketHandler)))
              .ToArray();

        foreach (var type in types)
        {
            services.AddSingleton(type);
        }

        return services;
    }
}