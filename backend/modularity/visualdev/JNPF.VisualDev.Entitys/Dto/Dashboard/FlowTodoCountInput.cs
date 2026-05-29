namespace JNPF.VisualDev.Entitys.Dto.Dashboard;

/// <summary>
/// 我的待办输出实体类.
/// </summary>
public class FlowTodoCountInput
{
    /// <summary>
    /// 待我审核.
    /// </summary>
    public List<string> toBeReviewedType { get; set; }

    /// <summary>
    /// 已办事宜.
    /// </summary>
    public List<string> flowDoneType { get; set; }

    /// <summary>
    /// 抄送.
    /// </summary>
    public List<string> flowCirculateType { get; set; }

    /// <summary>
    /// 公告分类.
    /// </summary>
    public List<string> typeList { get; set; }
}
