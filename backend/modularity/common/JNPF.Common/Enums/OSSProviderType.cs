using JNPF.DependencyInjection;
using System.ComponentModel;

namespace JNPF.Common.Enums;

/// <summary>
/// OSS提供者类型.
/// </summary>
[SuppressSniffer]
public enum OSSProviderType
{
    /// <summary>
    /// 本地.
    /// </summary>
    [Description("本地")]
    Invalid,

    /// <summary>
    /// Minio.
    /// </summary>
    [Description("Minio")]
    Minio,

    /// <summary>
    /// 阿里云.
    /// </summary>
    [Description("阿里云")]
    Aliyun,

    /// <summary>
    /// 腾讯云.
    /// </summary>
    [Description("腾讯云")]
    QCloud,

    /// <summary>
    /// 七牛.
    /// </summary>
    [Description("七牛")]
    Qiniu,

    /// <summary>
    /// 华为云.
    /// </summary>
    [Description("华为云")]
    HuaweiCloud
}