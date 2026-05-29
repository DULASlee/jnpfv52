using JNPF;
using JNPF.Common.Options;
using OnceMi.AspNetCore.OSS;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// OSS服务配置拓展.
/// </summary>
public static class OSSServiceConfigureExtensions
{
    /// <summary>
    /// OSS服务配置.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection OSSServiceConfigure(this IServiceCollection services)
    {
        // 获取选项
        OssOptions oss = App.GetConfig<OssOptions>("OSS", true);

        string fileStoreType = oss.Provider.ToString();
        OSSProvider provider = (OSSProvider)oss.Provider;
        string endpoint = oss.Endpoint;
        string accessKey = oss.AccessKey;
        string secretKey = oss.SecretKey;
        string region = oss.Region;
        bool isEnableHttps = oss.IsEnableHttps;
        bool isEnableCache = oss.IsEnableCache;

        services.AddOSSService(fileStoreType, option =>
        {
            option.Provider = provider; // 服务器
            option.Endpoint = endpoint; // 地址
            option.AccessKey = accessKey; // 服务访问玥
            option.SecretKey = secretKey; // 服务密钥
            option.Region = region;
            option.IsEnableHttps = isEnableHttps; // 是否启用https
            option.IsEnableCache = isEnableCache; // 是否启用缓存
        });

        return services;
    }
}