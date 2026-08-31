using JNPF.Runtime.Core;
using Xunit;

namespace JNPF.Tests.Runtime.Core.Execution;

public class ConcurrencyTests
{
    [Fact]
    public async Task ParallelRegister_ShouldBeThreadSafe()
    {
        var registry = new ExecutionHookRegistry();
        var tasks = Enumerable.Range(0, 100)
            .Select(i => Task.Run(() =>
            {
                registry.Register(new TestHook(ExecutionHookType.Before, i % 10));
            }));

        await Task.WhenAll(tasks);

        var hooks = registry.GetAllHooks();
        Assert.Equal(100, hooks.Count);
    }

    [Fact]
    public async Task ParallelUnregister_ShouldBeThreadSafe()
    {
        var registry = new ExecutionHookRegistry();
        var hooks = Enumerable.Range(0, 100)
            .Select(i => new TestHook(ExecutionHookType.Before, i))
            .ToList();

        foreach (var hook in hooks)
        {
            registry.Register(hook);
        }

        var tasks = hooks.Select(h => Task.Run(() => registry.Unregister(h)));
        await Task.WhenAll(tasks);

        Assert.True(registry.IsEmpty);
    }

    [Fact]
    public async Task ParallelRegisterAndUnregister_ShouldBeThreadSafe()
    {
        var registry = new ExecutionHookRegistry();
        var allHooks = Enumerable.Range(0, 50)
            .Select(i => new TestHook(ExecutionHookType.Before, i))
            .ToList();

        var tasks = new List<Task>();
        foreach (var hook in allHooks)
        {
            registry.Register(hook);
            tasks.Add(Task.Run(() => registry.Unregister(hook)));
        }

        await Task.WhenAll(tasks);

        // Some may have been unregistered, but no crash should occur
        // Final count may vary due to race conditions, but should be deterministic
    }

    [Fact]
    public void GetHooks_ShouldBeThreadSafe()
    {
        var registry = new ExecutionHookRegistry();
        for (int i = 0; i < 100; i++)
        {
            registry.Register(new TestHook(ExecutionHookType.Before, i));
        }

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    var _ = registry.GetHooks(ExecutionHookType.Before).Count;
                }
            }));

        Task.WaitAll(tasks.ToArray());

        // No exception should be thrown
    }

    [Fact]
    public void IsEmpty_ShouldBeThreadSafe()
    {
        var registry = new ExecutionHookRegistry();

        var tasks = new List<Task>();
        tasks.Add(Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                registry.Register(new TestHook(ExecutionHookType.Before, i));
            }
        }));
        tasks.Add(Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                var hooks = registry.GetAllHooks();
            }
        }));
        tasks.Add(Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                var empty = registry.IsEmpty;
            }
        }));

        Task.WaitAll(tasks.ToArray());

        // No exception should be thrown
    }
}
