namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 可视化开发导入数据输出.
/// </summary>
public class VisualDevImportDataOutput
{
    /// <summary>
    /// 导入成功条数.
    /// </summary>
    public int snum { get; set; }

    /// <summary>
    /// 导入失败条数.
    /// </summary>
    public int fnum { get; set; }

    /// <summary>
    /// 导入结果状态(0：成功，1：失败).
    /// </summary>
    public int resultType { get; set; }

    /// <summary>
    /// 失败结果集合.
    /// </summary>
    public List<Dictionary<string, object>> failResult { get; set; }
}
