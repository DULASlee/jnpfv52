using System.Reflection;
using JNPF.Common.Core.Manager;

namespace JNPF.Tests.Agent;

/// <summary>
/// Stub for IUserManager using DispatchProxy.
///
/// v5 (Chief Architect P1-1): Task&lt;T&gt; and ValueTask&lt;T&gt; for VALUE-TYPE T
/// must return default(T) via Activator.CreateInstance(tArg), NOT null.
/// The previous v4 implementation passed `new object?[] { null }` which fails
/// when T is int, Guid, bool, etc.
/// </summary>
public class UserManagerStub : DispatchProxy
{
    public string StubUserId { get; set; } = "test-user-id";

    public static IUserManager Build(string userId = "test-user-id")
    {
        // .NET 8 DispatchProxy.Create<T, TProxy>() returns T (the interface).
        // Use reflection to set the userId since we can't cast at compile time.
        var proxy = System.Reflection.DispatchProxy.Create<IUserManager, UserManagerStub>();
        if (proxy is null)
            throw new InvalidOperationException("DispatchProxy.Create returned null");
        var userIdProp = typeof(UserManagerStub).GetProperty(nameof(ProxyUserId), BindingFlags.NonPublic | BindingFlags.Instance)!;
        if (userIdProp is null)
            throw new InvalidOperationException($"Property {nameof(ProxyUserId)} not found on UserManagerStub");
        userIdProp.SetValue(proxy, userId);
        return proxy;
    }

    private string ProxyUserId { get; set; } = "test-user-id";

    protected override object? Invoke(MethodInfo method, object?[]? args)
    {
        var returnType = method.ReturnType;

        if (method.Name == "get_UserId") return ProxyUserId;

        if (returnType == typeof(Task)) return Task.CompletedTask;

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var tArg = returnType.GetGenericArguments()[0];
            // [P1-1] default(T) — Activator.CreateInstance handles value types;
            // for reference types it returns null (the correct Task.FromResult<T> default).
            var defaultValue = tArg.IsValueType ? Activator.CreateInstance(tArg) : null;
            var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(tArg);
            return fromResult.Invoke(null, new object?[] { defaultValue });
        }

        if (returnType == typeof(ValueTask)) return default(ValueTask);

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            var tArg = returnType.GetGenericArguments()[0];
            // [P1-1] ValueTask<T> ctor takes a T; default(T) for value types.
            var defaultValue = tArg.IsValueType ? Activator.CreateInstance(tArg) : null;
            var vtCtor = returnType.GetConstructor(new[] { tArg })
                ?? throw new InvalidOperationException($"No constructor for ValueTask<{tArg.Name}>");
            return vtCtor.Invoke(new object?[] { defaultValue });
        }

        if (returnType == typeof(string)) return string.Empty;
        if (returnType.IsValueType) return Activator.CreateInstance(returnType);
        return null;
    }
}