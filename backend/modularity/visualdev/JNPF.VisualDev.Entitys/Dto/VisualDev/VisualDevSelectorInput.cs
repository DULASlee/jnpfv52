namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 在线开发下拉框输入.
/// </summary>
public class VisualDevSelectorInput
{
    /// <summary>
    /// 功能类型
    /// 1-Web设计,2-App设计,3-流程表单,4-Web表单,5-App表单.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 页面类型 多个以 , 号隔开.
    /// </summary>
    public string webType { get; set; }
}
