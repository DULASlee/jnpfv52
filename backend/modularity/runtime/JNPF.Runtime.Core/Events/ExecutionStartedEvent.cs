using JNPF.Runtime.Core.Events;

namespace JNPF.Runtime.Core;

public sealed record ExecutionStartedEvent(ExecutionId ExecutionId, Guid SessionId, DateTime OccurredAtUtc) : IRuntimeEvent
{
    public static ExecutionStartedEvent Create(ExecutionId id, Guid sessionId) => new(id, sessionId, DateTime.UtcNow);
}
