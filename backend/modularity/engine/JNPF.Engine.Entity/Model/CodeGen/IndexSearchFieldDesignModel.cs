using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.CodeGen;

/// <summary>
/// 代码生成Index查询列设计.
/// </summary>
[SuppressSniffer]
public class IndexSearchFieldDesignModel
{
    /// <summary>
    /// 真实名字.
    /// </summary>
    public string OriginalName { get; set; }

    /// <summary>
    /// 列名.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 首字母小写列名.
    /// </summary>
    public string LowerName { get; set; }

    /// <summary>
    /// 控件名.
    /// </summary>
    public string Tag { get; set; }

    /// <summary>
    /// 可清除的.
    /// </summary>
    public string Clearable { get; set; }

    /// <summary>
    /// 时间格式化.
    /// </summary>
    public string Format { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public string @Type { get; set; }

    /// <summary>
    /// 时间输出类型.
    /// </summary>
    public string ValueFormat { get; set; }

    /// <summary>
    /// 数据类型.
    /// </summary>
    public string DataType { get; set; }

    /// <summary>
    /// 标题名.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// 查询控件Key.
    /// </summary>
    public string QueryControlsKey { get; set; }

    /// <summary>
    /// 选项配置.
    /// </summary>
    public PropsBeanModel Props { get; set; }

    /// <summary>
    /// 序号.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// 输入框中是否显示选中值的完整路径.
    /// </summary>
    public string ShowAllLevels { get; set; }

    /// <summary>
    /// 等级.
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// 是否子查询.
    /// </summary>
    public bool IsChildQuery { get; set; }

    /// <summary>
    /// 选择类型.
    /// </summary>
    public string SelectType { get; set; }

    /// <summary>
    /// 是否自定义选择.
    /// </summary>
    public bool IsCustomSelect => SelectType == "all" ? false : true;

    /// <summary>
    /// 是否多选.
    /// </summary>
    public bool IsMultiple { get; set; }

    /// <summary>
    /// 是否关键词搜索.
    /// </summary>
    public bool IsKeyword { get; set; }

    /// <summary>
    /// 可选分组.
    /// </summary>
    public string AbleIds { get; set; }

    /// <summary>
    /// jnpf控件KEY.
    /// </summary>
    public string jnpfKey { get; set; }

    /// <summary>
    /// 关联表单字段.
    /// </summary>
    public string RelationField { get; set; }

    /// <summary>
    /// 数据接口ID.
    /// </summary>
    public string InterfaceId { get; set; }

    /// <summary>
    /// 显示条数.
    /// </summary>
    public int? Total { get; set; }

    /// <summary>
    /// 搜索默认值.
    /// </summary>
    public string DefaultValues { get; set; }
}