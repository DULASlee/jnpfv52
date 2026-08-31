using JNPF.Runtime.Core.Events;

namespace JNPF.Runtime.Core;

public sealed record ExecutionFailedEvent(ExecutionId ExecutionId, Exception Exception, DateTime OccurredAtUtc) : IRuntimeEvent
{
    public static ExecutionFailedEvent Create(ExecutionId id, Exception ex) => new(id, ex, DateTime.UtcNow);
}
