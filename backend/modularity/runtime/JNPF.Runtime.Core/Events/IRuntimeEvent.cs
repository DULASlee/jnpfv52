namespace JNPF.Runtime.Core.Events;

public interface IRuntimeEvent
{
    DateTime OccurredAtUtc { get; }
}
