using System.Collections.Concurrent;
using System.Reflection;
using SqlSugar;

namespace JNPF.Tests.Agent;

/// <summary>
/// Stub for ISqlSugarRepository&lt;T&gt; using DispatchProxy.
///
/// v5 (Chief Architect B5): BuildListQuery path ONLY calls AsSugarClient().
/// All OTHER calls THROW (fail-fast) and are recorded in UnexpectedCalls.
/// Gate D asserts UnexpectedCalls.Count == 0 — proves audit completeness.
///
/// Returning `default` for unknown methods (v4 behaviour) silently fabricates
/// test behaviour when production code unexpectedly invokes a method we
/// didn't anticipate. That is exactly the kind of false-evidence the
/// closure gates must prevent.
/// </summary>
public class SqlSugarRepositoryStub : DispatchProxy
{
    public ISqlSugarClient SugarClient { get; set; } = null!;
    public ConcurrentBag<string> UnexpectedCalls { get; } = new();

    public static ISqlSugarRepository<T> Build<T>(ISqlSugarClient client) where T : class, new()
    {
        // .NET 8 DispatchProxy.Create<T, TProxy>() returns T (the interface), not TProxy.
        // We can't cast the returned reference to SqlSugarRepositoryStub at compile time,
        // because SqlSugarRepositoryStub does NOT implement ISqlSugarRepository<T> at
        // compile time — DispatchProxy generates a runtime class that bridges them.
        // Workaround: store the SugarClient via reflection.
        var proxy = System.Reflection.DispatchProxy.Create<ISqlSugarRepository<T>, SqlSugarRepositoryStub>();
        var sugarClientProp = typeof(SqlSugarRepositoryStub).GetProperty(nameof(SugarClient), BindingFlags.Public | BindingFlags.Instance)!;
        sugarClientProp.SetValue(proxy, client);
        return proxy;
    }

    /// <summary>
    /// Recover the concrete proxy from the interface reference, so tests can
    /// inspect UnexpectedCalls. Returns null only if the cast fails (which
    /// would mean the proxy is from a different DispatchProxy factory).
    /// </summary>
    public static SqlSugarRepositoryStub? AsConcrete<T>(ISqlSugarRepository<T> proxy) where T : class, new()
    {
        // Same compile-time inheritance limitation as Build — use reflection.
        return proxy as SqlSugarRepositoryStub;
    }

    protected override object? Invoke(MethodInfo method, object?[]? args)
    {
        if (method.Name == "AsSugarClient") return SugarClient;

        var declaringType = method.DeclaringType?.Name ?? "<unknown>";
        var paramSig = string.Join(",", method.GetParameters().Select(p => p.ParameterType.Name));
        var signature = $"{declaringType}.{method.Name}({paramSig})";
        UnexpectedCalls.Add(signature);

        throw new InvalidOperationException(
            $"SqlSugarRepositoryStub: unexpected repository call '{signature}'. " +
            $"Audit confirms BuildListQuery only invokes AsSugarClient(). " +
            $"If this is a legitimate new call path, update the audit comment " +
            $"in SqlSugarRepositoryStub.cs and adjust Gate D assertions accordingly.");
    }
}