namespace JNPF.Runtime.Core.Events;

public interface IRuntimeEventHandler
{
    Task HandleAsync(IRuntimeEvent evt, CancellationToken ct = default);
}
