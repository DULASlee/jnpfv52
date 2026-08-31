namespace JNPF.Runtime.Core.Events;

public interface IRuntimeEventPublisher
{
    void Publish(IRuntimeEvent evt);
    IDisposable Subscribe(IRuntimeEventHandler handler);
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IRuntimeEvent;
}
