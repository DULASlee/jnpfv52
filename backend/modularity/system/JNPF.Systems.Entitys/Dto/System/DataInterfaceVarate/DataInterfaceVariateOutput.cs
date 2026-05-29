namespace JNPF.Systems.Entitys.Dto.System.DataInterfaceVarate;

public class DataInterfaceVariateOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 数据接口id.
    /// </summary>
    public string interfaceId { get; set; }

    /// <summary>
    /// 变量名.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 表达式.
    /// </summary>
    public string expression { get; set; }

    /// <summary>
    /// 变量值.
    /// </summary>
    public string value { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string creatorUser { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 修改时间.
    /// </summary>
    public DateTime? lastModifyTime { get; set; }
}
