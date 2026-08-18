using JNPF.DependencyInjection;

namespace JNPF.InteAssistant.Skills;

public interface IEventParallelScheduler
{
    Task RunAsync(
        IReadOnlyList<string> eventIds,
        Func<string, CancellationToken, Task> worker,
        CancellationToken ct = default);
}

/// <summary>
/// 按 project 并行调度业务事件（max=5，P2-B08）
/// </summary>
public sealed class EventParallelScheduler : IEventParallelScheduler, ISingleton
{
    private const int MaxConcurrency = 5;

    public async Task RunAsync(
        IReadOnlyList<string> eventIds,
        Func<string, CancellationToken, Task> worker,
        CancellationToken ct = default)
    {
        using var semaphore = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);
        var tasks = eventIds.Select(async eventId =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                await worker(eventId, ct);
            }
            finally
            {
                semaphore.Release();
            }
        });
        await Task.WhenAll(tasks);
    }
}
