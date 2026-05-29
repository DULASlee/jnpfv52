using System.ComponentModel;

namespace JNPF.TaskScheduler.Entitys.Enum;

/// <summary>
/// http请求类型.
/// </summary>
public enum RequestTypeEnum
{
    /// <summary>
    /// 内置.
    /// </summary>
    [Description("内置")]
    BuiltIn = 0,

    /// <summary>
    /// 脚本.
    /// </summary>
    [Description("脚本")]
    Script = 1,

    /// <summary>
    /// HTTP请求.
    /// </summary>
    [Description("HTTP请求")]
    Http = 2,
}