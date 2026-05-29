using JNPF.Common.Enums;
using JNPF.ConfigurableOptions;

namespace JNPF.Common.Options;

/// <summary>
/// OSS配置.
/// </summary>
public sealed class OssOptions : IConfigurableOptions
{
    /// <summary>
    /// Minio桶.
    /// </summary>
    public string BucketName { get; set; }

    /// <summary>
    /// 提供者.
    /// </summary>
    public OSSProviderType Provider { get; set; } = OSSProviderType.Invalid;

    /// <summary>
    /// 地址.
    /// </summary>
    public string Endpoint { get; set; }

    /// <summary>
    /// 服务访问玥.
    /// </summary>
    public string AccessKey { get; set; }

    /// <summary>
    /// 服务密钥.
    /// </summary>
    public string SecretKey { get; set; }

    /// <summary>
    /// 地区.
    /// </summary>
    public string Region { get; set; }

    /// <summary>
    /// 是否启用https.
    /// </summary>
    public bool IsEnableHttps { get; set; }

    /// <summary>
    /// 是否启用缓存.
    /// </summary>
    public bool IsEnableCache { get; set; }
}