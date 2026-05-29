namespace JNPF.Common.Dtos;

/// <summary>
/// 数据导入输入.
/// </summary>
public class DataImportInput
{
    /// <summary>
    /// 流程Id.
    /// </summary>
    public string flowId { get; set; }

    /// <summary>
    /// 数据集合.
    /// </summary>
    public List<Dictionary<string, object>> list { get; set; }
}