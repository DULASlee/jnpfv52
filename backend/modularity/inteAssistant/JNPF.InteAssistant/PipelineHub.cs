using JNPF.InstantMessaging;
using Microsoft.AspNetCore.SignalR;

namespace JNPF.InteAssistant;

/// <summary>
/// Pipeline SignalR Hub
/// 用于实时推送流水线状态变更事件
/// </summary>
[MapHub("/hubs/pipeline")]
public class PipelineHub : Hub
{
    /// <summary>
    /// 加入租户组（按租户隔离推送）
    /// </summary>
    public async Task JoinTenantGroup(string tenantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
    }

    /// <summary>
    /// 离开租户组
    /// </summary>
    public async Task LeaveTenantGroup(string tenantId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
    }

    /// <summary>
    /// 加入指定流水线组（按流水线 ID 定向推送）
    /// </summary>
    public async Task JoinPipelineGroup(string pipelineId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"pipeline_{pipelineId}");
    }

    /// <summary>
    /// 离开流水线组
    /// </summary>
    public async Task LeavePipelineGroup(string pipelineId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"pipeline_{pipelineId}");
    }
}
