using System.Text.Json.Serialization;

namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 可视化开发列表输出.
/// </summary>
public class VisualDevListOutput
{
    /// <summary>
    /// 获取节点id.
    /// </summary>
    /// <returns></returns>
    public string id { get; set; }

    /// <summary>
    /// 获取节点父id.
    /// </summary>
    /// <returns></returns>
    public string parentId { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string? fullName { get; set; }

    /// <summary>
    /// 编号.
    /// </summary>
    public string? enCode { get; set; }

    /// <summary>
    /// 状态(0-禁用，1-开启).
    /// </summary>
    public int state { get; set; }

    /// <summary>
    /// 功能类型
    /// 1-Web设计,2-App设计,3-流程表单,4-Web表单,5-App表单.
    /// </summary>
    public int type { get; set; }

    /// <summary>
    /// 模式.
    /// </summary>
    public int? webType { get; set; }

    /// <summary>
    /// 分类ID.
    /// </summary>
    public string? category { get; set; }

    /// <summary>
    /// 创建人ID（内部传递用，不序列化到前端）.
    /// </summary>
    [JsonIgnore]
    public string? creatorUserId { get; set; }

    /// <summary>
    /// 修改人ID（内部传递用，不序列化到前端）.
    /// </summary>
    [JsonIgnore]
    public string? lastModifyUserId { get; set; }

    /// <summary>
    /// 数据库表JSON.
    /// </summary>
    public string? tables { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    public string? description { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string? creatorUser { get; set; }

    /// <summary>
    /// 修改时间.
    /// </summary>
    public DateTime? lastModifyTime { get; set; }

    /// <summary>
    /// 修改人.
    /// </summary>
    public string? lastModifyUser { get; set; }

    /// <summary>
    /// 删除标记.
    /// </summary>
    [JsonIgnore]
    public int? deleteMark { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// Pc是否发布.
    /// </summary>
    public int pcIsRelease { get; set; }

    /// <summary>
    /// App是否发布.
    /// </summary>
    public int appIsRelease { get; set; }

    /// <summary>
    /// App是否发布 1 已发布,其他 未发布.
    /// </summary>
    public string pcReleaseName;

    /// <summary>
    /// App是否发布 1 已发布,其他 未发布.
    /// </summary>
    public string appReleaseName;

    /// <summary>
    /// 是否发布.
    /// </summary>
    public int? isRelease { get; set; }

    /// <summary>
    /// 发布选中平台.
    /// </summary>
    public string platformRelease { get; set; }

    /// <summary>
    /// 是否启用流程.
    /// </summary>
    public int enableFlow { get; set; }

    /// <summary>
    /// 接口id.
    /// </summary>
    public string interfaceId { get; set; }

    /// <summary>
    /// 接口名称.
    /// </summary>
    public string interfaceName { get; set; }

    /// <summary>
    /// 接口参数.
    /// </summary>
    public string interfaceParam { get; set; }

    /// <summary>
    /// 是否显示包名(java用,.net不显示).
    /// </summary>
    public bool hasPackage { get; set; } = false;
}
