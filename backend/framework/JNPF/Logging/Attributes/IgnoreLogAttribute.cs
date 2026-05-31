namespace JNPF.Logging.Attributes;

/// <summary>
/// 忽略日志 - 已废弃，请使用 [LogPolicy(LogPolicy.IgnoreAll)] 或 [LogPolicy(LogPolicy.Minimal)] 替代。
/// </summary>
[Obsolete("Use [LogPolicy(LogPolicy.IgnoreAll)] or [LogPolicy(LogPolicy.Minimal)] instead.")]
[SuppressSniffer, AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class IgnoreLogAttribute : Attribute
{
}