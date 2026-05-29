using JNPF.Common.Filter;

namespace JNPF.VisualDev.Entitys.Dto.VisualDevModelData;

/// <summary>
/// 在线开发功能模块列表查询输入.
/// </summary>
public class VisualDevModelListQueryInput : PageInputBase
{
    /// <summary>
    /// 选择导出数据ids.
    /// </summary>
    public List<string> selectIds { get; set; }

    /// <summary>
    /// 选择导出数据key.
    /// </summary>
    public List<string> selectKey { get; set; }

    /// <summary>
    /// 导出类型.
    /// </summary>
    public string dataType { get; set; } = "0";

    /// <summary>
    /// 数据过滤.
    /// </summary>
    public string dataRuleJson { get; set; }

    /// <summary>
    /// 高级查询.
    /// </summary>
    public virtual string superQueryJson { get; set; }

    /// <summary>
    /// 菜单ID.
    /// </summary>
    public string? menuId { get; set; }

    /// <summary>
    /// 集成助手使用.
    /// 是否为流程审核完成.
    /// 0：不是,1：是.
    /// </summary>
    public int isProcessReviewCompleted { get; set; } = 0;

    /// <summary>
    /// 集成助手使用.
    /// 是否只给id.
    /// 0：不是,1：是.
    /// </summary>
    public int isOnlyId { get; set; } = 0;

    /// <summary>
    /// 集成助手使用.
    /// 集成助手数据标识.
    /// 0：不是,1：是.
    /// </summary>
    public int isInteAssisData { get; set; } = 0;

    /// <summary>
    /// 集成助手使用.
    /// 是否转换数据.
    /// 0：转换,1：不转换.
    /// </summary>
    public int isConvertData { get; set; } = 0;
}