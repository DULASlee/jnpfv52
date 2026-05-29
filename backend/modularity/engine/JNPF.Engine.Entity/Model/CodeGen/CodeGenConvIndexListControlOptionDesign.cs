using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.CodeGen;

/// <summary>
/// 代码生成常规Index列表控件Options设计.
/// </summary>
[SuppressSniffer]
public class CodeGenConvIndexListControlOptionDesign
{
    /// <summary>
    /// 列名.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 首字母小写列名.
    /// </summary>
    public string LowerName => string.IsNullOrWhiteSpace(Name) ? null : Name.Substring(0, 1).ToLower() + Name[1..];

    /// <summary>
    /// Options名称.
    /// </summary>
    public string OptionsName { get; set; }

    /// <summary>
    /// 数据类型.
    /// </summary>
    public string DataType { get; set; }

    /// <summary>
    /// jnpf控件key.
    /// </summary>
    public string jnpfKey { get; set; }

    /// <summary>
    /// 内容.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 字典类型.
    /// </summary>
    public string DictionaryType { get; set; }

    /// <summary>
    /// 是否静态数据.
    /// </summary>
    public bool IsStatic { get; set; }

    /// <summary>
    /// 是否Props.
    /// </summary>
    public bool IsProps { get; set; }

    /// <summary>
    /// 选项配置.
    /// </summary>
    public string Props { get; set; }

    /// <summary>
    /// 查询选项配置.
    /// </summary>
    public string QueryProps { get; set; }

    /// <summary>
    /// 是否展示在列表页.
    /// </summary>
    public bool IsIndex { get; set; }

    /// <summary>
    /// 是否子表控件.
    /// </summary>
    public bool IsChildren { get; set; }

    /// <summary>
    /// 是否联动重复
    /// 目前用于子表联动控件Options.
    /// </summary>
    public bool IsLinkedRepeat { get; set; }

    /// <summary>
    /// 是否被联动(反).
    /// </summary>
    public bool IsLinked { get; set; }

    /// <summary>
    /// 是否联动(正).
    /// </summary>
    public bool IsLinkage { get; set; }

    /// <summary>
    /// 模板json.
    /// </summary>
    public string TemplateJson { get; set; }
}

/// <summary>
/// 折叠面板与tab 标签 title.
/// </summary>
[SuppressSniffer]
public class CodeGenConvIndexListCollapseTitleModel
{
    public string title { get; set; }
}