using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.CodeGen;

/// <summary>
/// 代码生成常规Index列表列设计.
/// </summary>
[SuppressSniffer]
public class IndexColumnDesign
{
    /// <summary>
    /// 表名称.
    /// </summary>
    public string TableName { get; set; }

    /// <summary>
    /// 控件名称.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Options名称.
    /// </summary>
    public string OptionsName { get; set; }

    /// <summary>
    /// 首字母小写列名.
    /// </summary>
    public string LowerName { get; set; }

    /// <summary>
    /// 控件Key.
    /// </summary>
    public string jnpfKey { get; set; }

    /// <summary>
    /// 文本.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// 宽度.
    /// </summary>
    public string Width { get; set; }

    /// <summary>
    /// Align.
    /// </summary>
    public string Align { get; set; }

    /// <summary>
    /// 时间格式化.
    /// </summary>
    public string Format { get; set; }

    /// <summary>
    /// 是否排序.
    /// </summary>
    public string IsSort { get; set; }

    /// <summary>
    /// 是否子表.
    /// </summary>
    public bool IsChildTable { get; set; }

    /// <summary>
    /// 固定.
    /// </summary>
    public string Fixed { get; set; }

    /// <summary>
    /// 子表配置.
    /// </summary>
    public List<IndexColumnDesign> ChildTableDesigns { get; set; }

    /// <summary>
    /// 复杂表头配置.
    /// </summary>
    public List<IndexColumnDesign> ComplexColumns { get; set; }

    /// <summary>
    /// 关联表单模板ID.
    /// </summary>
    public string ModelId { get; set; }

    /// <summary>
    /// 是否开启千位符.
    /// </summary>
    public bool Thousands { get; set; }

    /// <summary>
    /// 精度.
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// 使用扫码.
    /// </summary>
    public string UseScan { get; set; }

    /// <summary>
    /// 使用掩码.
    /// </summary>
    public string UseMask { get; set; }

    /// <summary>
    /// 掩码配置.
    /// </summary>
    public string MaskConfig { get; set; }

    /// <summary>
    /// 当前位置.
    /// </summary>
    public int CurrentIndex { get; set; }
}