using JNPF.Common.Filter;

namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 在线开发列表查询输入.
/// </summary>
public class VisualDevListQueryInput : PageInputBase
{
    /// <summary>
    /// 功能类型
    /// 1-Web设计,2-App设计,3-流程表单,4-Web表单,5-App表单.
    /// </summary>
    public int type { get; set; } = 1;

    /// <summary>
    /// 分类.
    /// </summary>
    public string? category { get; set; }

    /// <summary>
    /// 模型分类
    /// 0-在线开发(无表),1-表单设计(有表)
    /// 集成助手.
    /// </summary>
    public string? model { get; set; }

    /// <summary>
    /// 页面类型
    /// 1、普通表单，2、流程表单，4、数据视图.
    /// </summary>
    public int? webType { get; set; }

    /// <summary>
    /// 状态(0-未发步，1-已发布，2-已修改).
    /// </summary>
    public int? isRelease { get; set; }

    /// <summary>
    /// 开启流程(集成助手).
    /// </summary>
    public int? enableFlow { get; set; }

    /// <summary>
    /// 查询备份数据
    /// </summary>
    public string parentId { get; set; }
}