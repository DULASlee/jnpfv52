namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 在线开发集成助手列表.
/// </summary>
public class VisualDevIntergrateListOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 是否启用流程.
    /// </summary>
    public int enableFlow { get; set; }
}