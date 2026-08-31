using JNPF.Runtime.Core.Events;

namespace JNPF.Runtime.Core;

public sealed record ExecutionCancelledEvent(ExecutionId ExecutionId, DateTime OccurredAtUtc) : IRuntimeEvent
{
    public static ExecutionCancelledEvent Create(ExecutionId id) => new(id, DateTime.UtcNow);
}
