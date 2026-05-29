namespace JNPF.Systems.Entitys.Dto.System.ModuleDataAuthorizeLink;

public class ModuleDataAuthorizeLinkTableOutput
{
    /// <summary>
    /// 数据源连接主键.
    /// </summary>
    public string linkId { get; set; }

    /// <summary>
    /// 表名.
    /// </summary>
    public List<string> linkTables { get; set; } = new List<string>();
}
