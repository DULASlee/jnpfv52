using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.Portal;

/// <summary>
/// 门户设计信息输出.
/// </summary>
[SuppressSniffer]
public class PortalInfoOutput
{
    /// <summary>
    /// ID.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 表单JSON.
    /// </summary>
    public string formData { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 类型(0-页面设计,1-自定义路径).
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 静态页面路径.
    /// </summary>
    public string customUrl { get; set; }

    /// <summary>
    /// 链接类型(0-页面,1-外链).
    /// </summary>
    public int? linkType { get; set; }

    /// <summary>
    /// 锁定（0-锁定，1-自定义）.
    /// </summary>
    public int? enabledLock { get; set; }

    /// <summary>
    /// 发布选中平台.
    /// </summary>
    public string platformRelease { get; set; }

    /// <summary>
    /// pc菜单发布标识.
    /// </summary>
    public int pcIsRelease { get; set; }

    /// <summary>
    /// pc菜单发布标识.
    /// </summary>
    public int appIsRelease { get; set; }

    /// <summary>
    /// pc门户发布标识.
    /// </summary>
    public int pcPortalIsRelease { get; set; }

    /// <summary>
    /// app门户发布标识.
    /// </summary>
    public int appPortalIsRelease { get; set; }

    /// <summary>
    /// pc已发布菜单名称.
    /// </summary>
    public string pcReleaseName {  get; set; }

    /// <summary>
    /// app已发布菜单名称.
    /// </summary>
    public string appReleaseName { get; set; }

    /// <summary>
    /// pc已发布门户名称.
    /// </summary>
    public string pcPortalReleaseName { get; set; }

    /// <summary>
    /// app已发布门户名称.
    /// </summary>
    public string appPortalReleaseName { get; set; }
}
