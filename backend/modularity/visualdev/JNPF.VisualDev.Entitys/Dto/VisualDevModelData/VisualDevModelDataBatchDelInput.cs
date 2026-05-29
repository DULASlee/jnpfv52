namespace JNPF.VisualDev.Entitys.Dto.VisualDevModelData;

/// <summary>
/// 在线开发模型数据批量删除输入.
/// </summary>
public class VisualDevModelDataBatchDelInput
{
    /// <summary>
    /// 待删除id数组.
    /// </summary>
    public List<string>? ids { get; set; }

    /// <summary>
    /// 是否触发集成助手参数
    /// 误删.
    /// </summary>
    public bool isInteAssis { get; set; } = true;

    /// <summary>
    /// 删除规则（集成助手）.
    /// 0：删除其他，1：正常删除.
    /// </summary>
    public int deleteRule { get; set; } = 1;
}
