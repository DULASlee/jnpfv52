namespace JNPF.Logging.Attributes;

/// <summary>
/// Log recording policy, replaces [IgnoreLog] and default behavior.
/// </summary>
[Flags]
public enum LogPolicy
{
    /// <summary>Record request params, response, operator, elapsed (default).</summary>
    Full = 0,

    /// <summary>Don't record request params (password/token interfaces).</summary>
    IgnoreRequest = 1,

    /// <summary>Don't record response (large data interfaces).</summary>
    IgnoreResponse = 2,

    /// <summary>Neither request params nor response (only operator, time, URL, result code).</summary>
    Minimal = IgnoreRequest | IgnoreResponse,

    /// <summary>Don't record at all (health check, heartbeat).</summary>
    IgnoreAll = 4,

    /// <summary>Force record even under high load (financial/permission ops).</summary>
    Force = 8
}

/// <summary>
/// Mark on Service methods to control operation log recording policy.
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class LogPolicyAttribute : Attribute
{
    public LogPolicy Policy { get; }

    public LogPolicyAttribute(LogPolicy policy = LogPolicy.Full)
    {
        Policy = policy;
    }
}
