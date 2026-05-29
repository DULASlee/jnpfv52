using JNPF.DependencyInjection;

namespace JNPF.Common.Enums;

/// <summary>
/// http请求类型.
/// </summary>
[SuppressSniffer]
public enum RequestType
{
    /// <summary>
    /// 执行内部方法.
    /// </summary>
    Api = 0,

    /// <summary>
    /// GET请求.
    /// </summary>
    Sql = 1,

    /// <summary>
    /// POST请求.
    /// </summary>
    Run = 2,
}