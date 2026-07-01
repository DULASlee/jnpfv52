using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Options;
using JNPF.DependencyInjection;
using SqlSugar;

namespace JNPF.Common.Configuration;

/// <summary>
/// Key常量.
/// </summary>
[SuppressSniffer]
public class KeyVariable
{
    private static readonly TenantOptions _tenant = App.GetConfig<TenantOptions>("Tenant", true);

    private static readonly AppOptions _jnfp = App.GetConfig<AppOptions>("JNPF_App", true);

    private static readonly OssOptions Oss = App.GetConfig<OssOptions>("OSS", true);

    /// <summary>
    /// 多租户模式.
    /// </summary>
    public static bool MultiTenancy
    {
        get
        {
            return _tenant.MultiTenancy;
        }
    }

    /// <summary>
    /// 多租户模式.
    /// </summary>
    public static string MultiTenancyType
    {
        get
        {
            return _tenant.MultiTenancyType;
        }
    }

    /// <summary>
    /// AI 开发工作区根目录名.
    /// </summary>
    public const string StudioWorkspaceRoot = "StudioWorkspace";

    /// <summary>
    /// 系统文件路径.
    /// </summary>
    public static string SystemPath
    {
        get
        {
            return Oss.Provider.Equals(OSSProviderType.Invalid) ? (string.IsNullOrEmpty(_jnfp.SystemPath) ? Directory.GetCurrentDirectory() : _jnfp.SystemPath) : string.Empty;
        }
    }

    /// <summary>
    /// 系统文件路径.
    /// </summary>
    public static string MultiSystemPath
    {
        get
        {
            string path = string.Empty;
            if (Oss.Provider.Equals(OSSProviderType.Invalid))
            {
                path = (string.IsNullOrEmpty(_jnfp.SystemPath) ? Directory.GetCurrentDirectory() : _jnfp.SystemPath);
            }
            else
            {
                var httpContext = App.HttpContext;
                if (httpContext != null)
                {
                    string tenantId = httpContext?.User.FindFirst("TenantId")?.Value;
                    string zxSystemId = httpContext?.User.FindFirst("ZxSystemId")?.Value;
                    if (_tenant.MultiSystem && zxSystemId.IsNotEmptyOrNull())
                    {
                        path = Path.Combine(path, zxSystemId);
                    }
                    if (_tenant.MultiTenancy && zxSystemId.IsNotEmptyOrNull())
                    {
                        path = Path.Combine(path, tenantId);
                    }
                }
                 
            }

            return path;


        }
    }

    /// <summary>
    /// 允许上传图片类型.
    /// </summary>
    public static List<string> AllowImageType
    {
        get
        {
            return string.IsNullOrEmpty(_jnfp.AllowUploadImageType.ToString()) ? new List<string>() : _jnfp.AllowUploadImageType;
        }
    }

    /// <summary>
    /// 允许上传文件类型.
    /// </summary>
    public static List<string> AllowUploadFileType
    {
        get
        {
            return string.IsNullOrEmpty(_jnfp.AllowUploadFileType.ToString()) ? new List<string>() : _jnfp.AllowUploadFileType;
        }
    }

    /// <summary>
    /// 微信允许上传文件类型.
    /// </summary>
    public static List<string> WeChatUploadFileType
    {
        get
        {
            return string.IsNullOrEmpty(_jnfp.WeChatUploadFileType.ToString()) ? new List<string>() : _jnfp.WeChatUploadFileType;
        }
    }

    /// <summary>
    /// 过滤上传文件名称特殊字符.
    /// </summary>
    public static List<string> SpecialString
    {
        get
        {
            return string.IsNullOrEmpty(_jnfp.SpecialString.ToString()) ? new List<string>() : _jnfp.SpecialString;
        }
    }

    /// <summary>
    /// MinIO桶.
    /// </summary>
    public static string BucketName
    {
        get
        {
            return string.IsNullOrEmpty(Oss.BucketName) ? string.Empty : Oss.BucketName;
        }
    }

    /// <summary>
    /// 文件储存类型.
    /// </summary>
    public static OSSProviderType FileStoreType
    {
        get
        {
            return string.IsNullOrEmpty(Oss.Provider.ToString()) ? OSSProviderType.Invalid : Oss.Provider;
        }
    }

    /// <summary>
    /// App版本.
    /// </summary>
    public static string AppVersion
    {
        get
        {
            return string.IsNullOrEmpty(App.Configuration["JNPF_APP:AppVersion"]) ? string.Empty : App.Configuration["JNPF_APP:AppVersion"];
        }
    }

    /// <summary>
    /// 文件储存类型.
    /// </summary>
    public static string AppUpdateContent
    {
        get
        {
            return string.IsNullOrEmpty(App.Configuration["JNPF_APP:AppUpdateContent"]) ? string.Empty : App.Configuration["JNPF_APP:AppUpdateContent"];
        }
    }
}