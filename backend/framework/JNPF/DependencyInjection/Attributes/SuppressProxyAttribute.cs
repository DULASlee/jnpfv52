namespace JNPF.DependencyInjection;

/// <summary>
/// 跳过全局代理
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Class)]
public class SuppressProxyAttribute : Attribute
{
}