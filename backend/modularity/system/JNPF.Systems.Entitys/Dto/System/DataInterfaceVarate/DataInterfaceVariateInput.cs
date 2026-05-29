namespace JNPF.Systems.Entitys.Dto.System.DataInterfaceVarate;

public class DataInterfaceVariateInput
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
}
