using System.Collections.Concurrent;
using JNPF.DependencyInjection;

namespace JNPF.InteAssistant.Skills;

public interface ISkillRunGuard
{
    bool TryAcquire(string tenantId, long pipelineId, string skillId, string runId, out string? conflictRunId);
    void Release(string tenantId, long pipelineId, string skillId);
    bool IsRunning(string tenantId, long pipelineId, string skillId);
}

/// <summary>
/// 单 pipeline + skill 互斥锁（P2-R02）
/// </summary>
public sealed class SkillRunGuard : ISkillRunGuard, ISingleton
{
    private readonly ConcurrentDictionary<string, string> _activeRuns = new();

    private static string Key(string tenantId, long pipelineId, string skillId)
        => $"{tenantId}:{pipelineId}:{skillId}";

    public bool TryAcquire(string tenantId, long pipelineId, string skillId, string runId, out string? conflictRunId)
    {
        var key = Key(tenantId, pipelineId, skillId);
        if (_activeRuns.TryAdd(key, runId))
        {
            conflictRunId = null;
            return true;
        }

        conflictRunId = _activeRuns.GetValueOrDefault(key);
        return false;
    }

    public void Release(string tenantId, long pipelineId, string skillId)
        => _activeRuns.TryRemove(Key(tenantId, pipelineId, skillId), out _);

    public bool IsRunning(string tenantId, long pipelineId, string skillId)
        => _activeRuns.ContainsKey(Key(tenantId, pipelineId, skillId));
}
