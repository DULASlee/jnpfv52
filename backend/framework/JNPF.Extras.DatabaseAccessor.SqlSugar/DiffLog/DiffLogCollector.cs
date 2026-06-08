namespace JNPF.Extras.DatabaseAccessor.SqlSugar.DiffLog;

/// <summary>
/// DiffLog 收集器实现（Scoped 生命周期）。
/// 内部使用 List 暂存，线程安全由 Scoped 的请求隔离保证。
/// </summary>
public class DiffLogCollector : IDiffLogCollector
{
    private readonly List<DiffLogData> _buffer = new();

    public void Collect(DiffLogData data)
    {
        if (data != null)
        {
            _buffer.Add(data);
        }
    }

    public IList<DiffLogData> GetAndClear()
    {
        if (_buffer.Count == 0) return Array.Empty<DiffLogData>();

        var snapshot = new List<DiffLogData>(_buffer);
        _buffer.Clear();
        return snapshot;
    }

    public bool HasPendingData => _buffer.Count > 0;
}
