namespace JNPF.Runtime.Core.Observation;

/// <summary>
/// Execution 观察器接口。
/// 
/// 用于收集和持久化 Execution 执行记录。
/// Runtime 必须"知道发生了什么"，但不负责替专家决定"业务上应该怎么做"。
/// </summary>
public interface IExecutionObserver
{
    /// <summary>
    /// 记录执行开始。
    /// </summary>
    void RecordStarted(ExecutionRecord record);

    /// <summary>
    /// 记录执行完成。
    /// </summary>
    void RecordCompleted(ExecutionRecord record);

    /// <summary>
    /// 记录执行失败。
    /// </summary>
    void RecordFailed(ExecutionRecord record);

    /// <summary>
    /// 获取会话的所有记录。
    /// </summary>
    IReadOnlyList<ExecutionRecord> GetRecordsForSession(Guid sessionId);

    /// <summary>
    /// 获取执行的所有记录。
    /// </summary>
    IReadOnlyList<ExecutionRecord> GetRecordsForExecution(ExecutionId executionId);
}

/// <summary>
/// Execution 观察器默认实现。
/// 
/// 内存存储，用于开发/测试环境。
/// 生产环境应实现持久化版本。
/// </summary>
public sealed class InMemoryExecutionObserver : IExecutionObserver
{
    private readonly List<ExecutionRecord> _records = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public void RecordStarted(ExecutionRecord record)
    {
        lock (_lock)
        {
            _records.Add(record);
        }
    }

    /// <inheritdoc />
    public void RecordCompleted(ExecutionRecord record)
    {
        lock (_lock)
        {
            // 查找并更新开始记录
            var started = _records.FirstOrDefault(r => 
                r.ExecutionId == record.ExecutionId && 
                r.CompletedAtUtc == null);
            
            if (started != null)
            {
                _records.Remove(started);
            }
            _records.Add(record);
        }
    }

    /// <inheritdoc />
    public void RecordFailed(ExecutionRecord record)
    {
        lock (_lock)
        {
            // 查找并更新开始记录
            var started = _records.FirstOrDefault(r => 
                r.ExecutionId == record.ExecutionId && 
                r.CompletedAtUtc == null);
            
            if (started != null)
            {
                _records.Remove(started);
            }
            _records.Add(record);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ExecutionRecord> GetRecordsForSession(Guid sessionId)
    {
        lock (_lock)
        {
            return _records
                .Where(r => r.SessionId == sessionId)
                .OrderBy(r => r.StartedAtUtc)
                .ToList();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ExecutionRecord> GetRecordsForExecution(ExecutionId executionId)
    {
        lock (_lock)
        {
            return _records
                .Where(r => r.ExecutionId == executionId)
                .OrderBy(r => r.StartedAtUtc)
                .ToList();
        }
    }
}
