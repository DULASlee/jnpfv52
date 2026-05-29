namespace JNPF.Systems.Entitys.Model.DataBase;

/// <summary>
/// 数据 表信息 和 表字段 模型.
/// </summary>
public class DbTableAndFieldModel : DbTableModel
{
    public List<DbTableFieldModel> FieldList { get; set; }
}
