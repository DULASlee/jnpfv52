using System.Collections.Concurrent;
using JNPF.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace JNPF.InteAssistant.Runtime;

public interface ITenantPipelineQuotaGuard
{
    bool TryAcquire(string tenantId, long pipelineId, out string? rejectReason, out IReadOnlyList<long> activePipelineIds);
    void Release(string tenantId, long pipelineId);
}

/// <summary>
/// 每租户并发 running pipeline 配额（P2.5-B02，默认 3）。
/// </summary>
public sealed class TenantPipelineQuotaGuard : ITenantPipelineQuotaGuard, ISingleton
{
    private readonly int _maxConcurrent;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<long, int>> _refCounts = new();

    public TenantPipelineQuotaGuard(IConfiguration configuration)
    {
        _maxConcurrent = configuration.GetValue("StudioRuntime:MaxConcurrentPipelinesPerTenant", 3);
        if (_maxConcurrent < 1) _maxConcurrent = 1;
    }

    public bool TryAcquire(string tenantId, long pipelineId, out string? rejectReason, out IReadOnlyList<long> activePipelineIds)
    {
        rejectReason = null;
        var normalizedTenant = NormalizeTenant(tenantId);
        var tenantMap = _refCounts.GetOrAdd(normalizedTenant, _ => new ConcurrentDictionary<long, int>());

        lock (tenantMap)
        {
            var active = tenantMap.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
            activePipelineIds = active;

            if (!active.Contains(pipelineId) && active.Count >= _maxConcurrent)
            {
                rejectReason = $"TENANT_PIPELINE_QUOTA_EXCEEDED: 租户 {normalizedTenant} 已有 {_maxConcurrent} 条 pipeline 在运行";
                return false;
            }

            tenantMap.AddOrUpdate(pipelineId, 1, (_, c) => c + 1);
            activePipelineIds = tenantMap.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
            return true;
        }
    }

    public void Release(string tenantId, long pipelineId)
    {
        var normalizedTenant = NormalizeTenant(tenantId);
        if (!_refCounts.TryGetValue(normalizedTenant, out var tenantMap))
            return;

        lock (tenantMap)
        {
            if (!tenantMap.TryGetValue(pipelineId, out var count))
                return;

            if (count <= 1)
                tenantMap.TryRemove(pipelineId, out _);
            else
                tenantMap[pipelineId] = count - 1;
        }
    }

    private static string NormalizeTenant(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || tenantId is "default" or "0")
            return "1";
        return tenantId;
    }
}
