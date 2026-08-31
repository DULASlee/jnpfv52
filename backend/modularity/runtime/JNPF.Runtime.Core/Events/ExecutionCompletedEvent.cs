using JNPF.Runtime.Core.Events;

namespace JNPF.Runtime.Core;

public sealed record ExecutionCompletedEvent(ExecutionId ExecutionId, ExecutionResult Result, DateTime OccurredAtUtc) : IRuntimeEvent
{
    public static ExecutionCompletedEvent Create(ExecutionId id, ExecutionResult result) => new(id, result, DateTime.UtcNow);
}
