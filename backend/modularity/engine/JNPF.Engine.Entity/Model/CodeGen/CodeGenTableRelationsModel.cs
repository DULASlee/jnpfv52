using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.CodeGen;

/// <summary>
/// 代码生成表关系模型.
/// </summary>
[SuppressSniffer]
public class CodeGenTableRelationsModel
{
    /// <summary>
    /// 功能名称.
    /// </summary>
    public string ClassName { get; set; }

    /// <summary>
    /// 功能名称(首字母小写).
    /// </summary>
    public string LowerClassName => string.IsNullOrWhiteSpace(ClassName) ? null : ClassName.Substring(0, 1).ToLower() + ClassName[1..];

    /// <summary>
    /// 表名.
    /// </summary>
    public string TableName { get; set; }

    /// <summary>
    /// 关联主表.
    /// </summary>
    public string RelationTable { get; set; }

    /// <summary>
    /// 原始表名称.
    /// </summary>
    public string OriginalTableName { get; set; }

    /// <summary>
    /// 控件绑定模型.
    /// </summary>
    public string ControlModel { get; set; }

    /// <summary>
    /// 表名(首字母小写).
    /// </summary>
    public string LowerTableName => string.IsNullOrWhiteSpace(TableName) ? null : TableName.Substring(0, 1).ToLower() + TableName[1..];

    /// <summary>
    /// 主键.
    /// </summary>
    public string PrimaryKey { get; set; }

    /// <summary>
    /// 主键(首字母小写).
    /// </summary>
    public string LowerPrimaryKey => string.IsNullOrWhiteSpace(PrimaryKey) ? null : PrimaryKey.Substring(0, 1).ToLower() + PrimaryKey[1..];

    /// <summary>
    /// 表描述.
    /// </summary>
    public string TableComment { get; set; }

    /// <summary>
    /// 控件内表描述.
    /// </summary>
    public string ControlTableComment { get; set; }

    /// <summary>
    /// 外键字段.
    /// </summary>
    public string TableField { get; set; }

    /// <summary>
    /// 关联主键.
    /// </summary>
    public string LowerTableField => string.IsNullOrWhiteSpace(TableField) ? null : TableField.Substring(0, 1).ToLower() + TableField[1..];

    /// <summary>
    /// 原始外键字段.
    /// </summary>
    public string OriginalTableField { get; set; }

    /// <summary>
    /// 关联主键.
    /// </summary>
    public string RelationField { get; set; }

    /// <summary>
    /// 原始关联主键.
    /// </summary>
    public string OriginalRelationField { get; set; }

    /// <summary>
    /// 关联主键.
    /// </summary>
    public string LowerRelationField => string.IsNullOrWhiteSpace(RelationField) ? null : RelationField.Substring(0, 1).ToLower() + RelationField[1..];

    /// <summary>
    /// 子表控件配置.
    /// </summary>
    public List<TableColumnConfigModel> ChilderColumnConfigList { get; set; }

    /// <summary>
    /// 子表有限字段长度.
    /// </summary>
    public int ChilderColumnConfigListCount { get; set; }

    /// <summary>
    /// 编号.
    /// </summary>
    public int TableNo { get; set; }

    /// <summary>
    /// 是否查询条件.
    /// </summary>
    public bool IsQueryWhether { get; set; }

    /// <summary>
    /// 是否显示字段.
    /// </summary>
    public bool IsShowField { get; set; }

    /// <summary>
    /// 是否唯一.
    /// </summary>
    public bool IsUnique { get; set; }

    /// <summary>
    /// 是否数据转换.
    /// </summary>
    public bool IsConversion { get; set; }

    /// <summary>
    /// 是否详情数据转换.
    /// </summary>
    public bool IsDetailConversion { get; set; }

    /// <summary>
    /// 是否导入数据.
    /// </summary>
    public bool IsImportData { get; set; }

    /// <summary>
    /// 是否系统控件.
    /// </summary>
    public bool IsSystemControl { get; set; }

    /// <summary>
    /// 是否更新.
    /// </summary>
    public bool IsUpdate { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public bool IsControlParsing { get; set; }

    /// <summary>
    /// 是否查询条件多选.
    /// </summary>
    public bool IsSearchMultiple { get; set; }

    /// <summary>
    /// 被引用的字段数量.
    /// </summary>
    public int FieldCount { get; set; }
}