using JNPF.Common.Dtos.Datainterface;
using JNPF.Common.Models;
using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.CodeGen;

/// <summary>
/// 数据库表列.
/// </summary>
[SuppressSniffer]
public class TableColumnConfigModel
{

    #region 通用参数

    /// <summary>
    /// 功能名称.
    /// </summary>
    public string ClassName { get; set; }

    /// <summary>
    /// 功能名称(首字母小写).
    /// </summary>
    public string LowerClassName => string.IsNullOrWhiteSpace(ClassName) ? null : ClassName.Substring(0, 1).ToLower() + ClassName[1..];

    /// <summary>
    /// 字段名-大写(剔除"F_","f_").
    /// </summary>
    public string ColumnName { get; set; }

    /// <summary>
    /// 关联字段名.
    /// </summary>
    public string RelationColumnName { get; set; }

    /// <summary>
    /// 数据库字段名(首字母小写).
    /// </summary>
    public string LowerColumnName => string.IsNullOrWhiteSpace(ColumnName) ? null : ColumnName.Substring(0, 1).ToLower() + ColumnName[1..];

    /// <summary>
    /// 原本名称.
    /// </summary>
    public string OriginalColumnName { get; set; }

    /// <summary>
    /// 控件标题.
    /// </summary>
    public string ControlLabel { get; set; }

    /// <summary>
    /// 数据库中类型.
    /// </summary>
    public string DataType { get; set; }

    /// <summary>
    /// .NET字段类型.
    /// </summary>
    public string NetType { get; set; }

    /// <summary>
    /// 字段描述.
    /// </summary>
    public string ColumnComment { get; set; }

    /// <summary>
    /// 是否是查询条件.
    /// </summary>
    public bool QueryWhether { get; set; }

    /// <summary>
    /// 查询方式
    /// 1-等于,2-模糊,3-范围.
    /// </summary>
    public int QueryType { get; set; }

    /// <summary>
    /// 是否查询多选.
    /// </summary>
    public bool QueryMultiple { get; set; }

    /// <summary>
    /// 是否展示.
    /// </summary>
    public bool IsShow { get; set; }

    /// <summary>
    /// 是否多选.
    /// </summary>
    public bool IsMultiple { get; set; }

    /// <summary>
    /// 是否主键.
    /// </summary>
    public bool PrimaryKey { get; set; }

    /// <summary>
    /// 是否外键字段.
    /// </summary>
    public bool ForeignKeyField { get; set; }

    /// <summary>
    /// 控件Key.
    /// </summary>
    public string jnpfKey { get; set; }

    /// <summary>
    /// 控件Key
    /// 用途：数据转换时 字段名+控件key 单驼峰命名规则.
    /// </summary>
    public string upperJnpfKey => string.IsNullOrWhiteSpace(jnpfKey) ? null : jnpfKey.Substring(0, 1).ToUpper() + jnpfKey[1..];

    /// <summary>
    /// 单据规则.
    /// </summary>
    public string Rule { get; set; }

    /// <summary>
    /// 表名称.
    /// </summary>
    public string TableName { get; set; }

    /// <summary>
    /// 格式化表名称
    /// 子表或者副表使用.
    /// </summary>
    public string FormatTableName { get; set; }

    /// <summary>
    /// 格式化表名称
    /// 子表或者副表使用.
    /// </summary>
    public string LowerFormatTableName => string.IsNullOrWhiteSpace(FormatTableName) ? null : FormatTableName.Substring(0, 1).ToLower() + FormatTableName[1..];

    /// <summary>
    /// 是否yyyy-MM-dd HH:mm:ss.
    /// </summary>
    public bool IsDateTime { get; set; }

    /// <summary>
    /// 时间格式化.
    /// </summary>
    public string Format { get; set; }

    /// <summary>
    /// 开关控件 属性 - 开启展示值.
    /// </summary>
    public string ActiveTxt { get; set; }

    /// <summary>
    /// 开关控件 属性 - 关闭展示值.
    /// </summary>
    public string InactiveTxt { get; set; }

    /// <summary>
    /// 静态数据JSON.
    /// </summary>
    public List<StaticDataModel> StaticData { get; set; }

    /// <summary>
    /// 控件数据来源
    /// 部分控件的数据源
    /// 例如 静态数据,数据字段,远端数据.
    /// </summary>
    public string ControlsDataType { get; set; }

    /// <summary>
    /// 数据来源ID.
    /// </summary>
    public string propsUrl { get; set; }

    /// <summary>
    /// 指定选项的值为选项对象的某个属性值.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// 是否转换数据.
    /// </summary>
    public bool IsConversion { get; set; }

    /// <summary>
    /// 是否详情转换数据.
    /// </summary>
    public bool IsDetailConversion { get; set; }

    /// <summary>
    /// 是否系统控件.
    /// </summary>
    public bool IsSystemControl { get; set; }

    /// <summary>
    /// 是否唯一.
    /// </summary>
    public bool IsUnique { get; set; }

    /// <summary>
    /// 是否更新.
    /// </summary>
    public bool IsUpdate { get; set; }

    /// <summary>
    /// 控制解析.
    /// </summary>
    public bool IsControlParsing { get; set; }

    /// <summary>
    /// 是否联动(正).
    /// </summary>
    public bool IsLinkage { get; set; }

    /// <summary>
    /// 联动配置.
    /// </summary>
    public List<DataInterfaceParameter> LinkageConfig { get; set; }

    /// <summary>
    /// 控件属性类型 0:展示数据，1:存储数据.
    /// </summary>
    public bool isStorage { get; set; }

    #endregion

    #region 副表使用

    /// <summary>
    /// 是否副表.
    /// </summary>
    public bool IsAuxiliary { get; set; }

    /// <summary>
    /// 表编号
    /// 子表或者副表使用.
    /// </summary>
    public int TableNo { get; set; }

    #endregion

    #region 子表使用

    /// <summary>
    /// 子表控件Key.
    /// </summary>
    public string ChildControlKey { get; set; }

    #endregion

    #region 数据导出导入

    /// <summary>
    /// 导出配置.
    /// </summary>
    public CodeGenFieldsModel ImportConfig { get; set; }

    /// <summary>
    /// 是否导入字段.
    /// </summary>
    public bool IsImportField { get; set; }

    /// <summary>
    /// 指定选项标签为选项对象的某个属性值.
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// 指定选项的子选项为选项对象的某个属性值.
    /// </summary>
    public string Children { get; set; }

    /// <summary>
    /// 选项分隔符.
    /// </summary>
    public string Separator { get; set; }

    #endregion

    /// <summary>
    /// 展示字段.
    /// </summary>
    public string ShowField { get; set; }

    /// <summary>
    /// 是否树形父级字段.
    /// </summary>
    public bool IsTreeParentField { get; set; }

    /// <summary>
    /// 系统控件 - 所属组织 属性 - 显示内容
    /// all ：显示组织， last ： 显示部门.
    /// </summary>
    public string ShowLevel { get; set; }

    /// <summary>
    /// 级联是否展示全路径.
    /// </summary>
    public bool ShowAllLevels { get; set; }
}