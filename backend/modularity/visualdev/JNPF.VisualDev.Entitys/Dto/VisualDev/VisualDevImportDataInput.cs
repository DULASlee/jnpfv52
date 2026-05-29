namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 可视化开发导入数据输入.
/// </summary>
public class VisualDevImportDataInput
{
    /// <summary>
    /// 流程id.
    /// </summary>
    public string flowId { get; set; }

    /// <summary>
    /// 数据集合.
    /// </summary>
    public List<Dictionary<string, object>> list { get; set; }

    /// <summary>
    /// 是否触发集成助手参数
    /// 误删.
    /// </summary>
    public bool isInteAssis { get; set; } = true;
}
