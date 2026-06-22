// 文件：Infrastructure/Background/TaskState.cs
// 命名空间：JNPF.InteAssistant.Infrastructure.Background
// 职责：后台任务状态（内部使用）

namespace JNPF.InteAssistant.Infrastructure.Background;

/// <summary>后台任务状态</summary>
public sealed class TaskState
{
    public CancellationTokenSource Cts { get; init; } = null!;
    public CancellationTokenSource LinkedCts { get; init; } = null!;
    public DateTime StartTime { get; init; } = DateTime.UtcNow;
}
