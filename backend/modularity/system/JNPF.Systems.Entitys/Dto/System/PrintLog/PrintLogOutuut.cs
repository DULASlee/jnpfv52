namespace JNPF.Systems.Entitys.Dto.System.PrintLog;

public class PrintLogOutuut
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 打印人.
    /// </summary>
    public string printMan { get; set; }

    /// <summary>
    /// 打印时间.
    /// </summary>
    public DateTime? printTime { get; set; }

    /// <summary>
    /// 打印条数.
    /// </summary>
    public int? printNum { get; set; }

    /// <summary>
    /// 打印功能名称.
    /// </summary>
    public string printTitle { get; set; }

    /// <summary>
    /// 打印模板id.
    /// </summary>
    public string printId { get; set; }
}
