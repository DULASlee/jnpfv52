namespace JNPF.Systems.Entitys.Dto.System.ModuleDataAuthorizeLink;

public class ModuleDataAuthorizeLinkInfoOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 数据源连接主键.
    /// </summary>
    public string linkId { get; set; }

    /// <summary>
    /// 表名.
    /// </summary>
    public string linkTables { get; set; }

    /// <summary>
    /// 权限类型(1:列表权限，2：数据权限，3：表单权限).
    /// </summary>
    public string dataType { get; set; }

    /// <summary>
    /// 菜单主键.
    /// </summary>
    public string moduleId { get; set; }
}
