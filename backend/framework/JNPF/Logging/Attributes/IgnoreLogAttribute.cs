namespace JNPF.Logging.Attributes;

/// <summary>
/// 忽略日志
/// 结合AllowAnonymous一起使用
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class IgnoreLogAttribute : Attribute
{

}