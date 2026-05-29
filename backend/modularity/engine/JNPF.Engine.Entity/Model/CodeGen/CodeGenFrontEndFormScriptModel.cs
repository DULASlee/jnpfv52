using JNPF.DependencyInjection;
using System.ComponentModel;

namespace JNPF.Engine.Entity.Model.CodeGen;

/// <summary>
/// 代码生成表单类型定义模型.
/// </summary>
[SuppressSniffer]
public class CodeGenFrontEndFormScriptModel
{
    /// <summary>
    /// 表单控件设计.
    /// </summary>
    public List<FormControlDesignModel> FormControlDesign { get; set; }

    /// <summary>
    /// 表单数据.
    /// </summary>
    public List<CodeGenFrontEndFormState> DataForm { get; set; }

    /// <summary>
    /// 行内编辑表单数据.
    /// </summary>
    public List<CodeGenFrontEndFormState> InlineEditorDataForm { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public List<CodeGenFrontEndDataRule> DataRules { get; set; }

    /// <summary>
    /// 是否开启添加子表配置.
    /// </summary>
    public bool HasSubTableDataTransfer { get; set; }

    /// <summary>
    /// 是否开启Option.
    /// </summary>
    public bool HasOptions { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public List<CodeGenFrontEndFormState> Options { get; set; }

    /// <summary>
    /// 数据选项.
    /// </summary>
    public List<CodeGenFrontEndDataOption> DataOptions { get; set; }

    /// <summary>
    /// 联动(正)
    /// 正:受别的控件影响
    /// 反:该控件影响别的控件.
    /// </summary>
    public List<CodeGenFrontEndLinkageConfig> Linkage { get; set; }

    /// <summary>
    /// 是否开启数据字段.
    /// </summary>
    public bool HasDictionary { get; set; }

    /// <summary>
    /// 是否开启远端数据.
    /// </summary>
    public bool HasDynamic { get; set; }

    /// <summary>
    /// 是否开启折叠面板.
    /// </summary>
    public bool HasCollapse { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public List<CodeGenFrontEndFormState> Collapses { get; set; }

    /// <summary>
    /// 是否开启子表.
    /// </summary>
    public bool HasSubTable { get; set; }

    /// <summary>
    /// 是否开启子表合计.
    /// </summary>
    public bool HasSubTableSummary { get; set; }

    /// <summary>
    /// 子表设计.
    /// </summary>
    public List<CodeGenFrontEndSubTableDesignModel> SubTableDesign { get; set; }

    /// <summary>
    /// 子表控件组.
    /// </summary>
    public List<CodeGenFrontEndSubTableControlModel> SubTableControls { get; set; }

    /// <summary>
    /// 子表联动Options.
    /// </summary>
    public List<string> SubTableLinkageOptions { get; set; }

    /// <summary>
    /// 子表表头.
    /// </summary>
    public List<CodeGenFrontEndSubTableHeaderModel> SubTableHeader { get; set; }

    /// <summary>
    /// 是否开始特殊时间.
    /// </summary>
    public bool HasSpecialTime { get; set; }

    /// <summary>
    /// 是否开始特殊日期.
    /// </summary>
    public bool HasSpecialDate { get; set; }
}

/// <summary>
/// 代码生成表单类型定义.
/// </summary>
[SuppressSniffer]
public class CodeGenFrontEndFormState
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 值.
    /// </summary>
    public object Value { get; set; }
}

/// <summary>
/// 代码生成表单类型定义.
/// </summary>
[SuppressSniffer]
public class CodeGenFrontEndDataRule
{
    public string Name { get; set; }

    public List<CodeGenFrontEndDataRequiredModel> Required { get; set; }

    public List<CodeGenFrontEndDataRuleModel> Rule { get; set; }
}

/// <summary>
/// 代码生成前端子表设计模型.
/// </summary>
[SuppressSniffer]
public class CodeGenFrontEndSubTableDesignModel
{
    /// <summary>
    /// 子表名称.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 是否开启子表合计.
    /// </summary>
    public bool HasSummary { get; set; }

    /// <summary>
    /// 合计字段.
    /// </summary>
    public string SummaryField { get; set; }

    /// <summary>
    /// 子表添加类型
    /// 0-常规添加,1-数据传递.
    /// </summary>
    public int AddType { get; set; }

    /// <summary>
    /// 控件组.
    /// </summary>
    public List<CodeGenFrontEndSubTableControlModel> Controls { get; set; }

    /// <summary>
    /// 子表表头.
    /// </summary>
    public List<CodeGenFrontEndSubTableHeaderModel> Header { get; set; }

    /// <summary>
    /// 子表数据传递配置.
    /// </summary>
    public CodeGenFrontEndFormState DataTransfer { get; set; }

    /// <summary>
    /// 子表默认数据.
    /// </summary>
    public List<CodeGenFrontEndFormState> DataForm { get; set; }

    /// <summary>
    /// 联动Options.
    /// </summary>
    public List<string> LinkageOptions { get; set; }

    /// <summary>
    /// 子表复杂表头.
    /// </summary>
    public string ComplexColumns { get; set; }

    /// <summary>
    /// 子表列按钮.
    /// </summary>
    public List<ChildTableBtnsList>? ColumnBtnsList { get; set; }

    /// <summary>
    /// 子表底部按钮.
    /// </summary>
    public List<ChildTableBtnsList>? FooterBtnsList { get; set; }

    /// <summary>
    /// 是否存在批量删除按钮.
    /// </summary>
    public bool IsAnyBatchRemove { get; set; }

    /// <summary>
    /// 是否存在控件必填验证.
    /// </summary>
    public bool IsAnyRequired { get; set; }

    /// <summary>
    /// 验证规则.
    /// </summary>
    public List<RegListModel> RegList { get; set; }

}

/// <summary>
/// 代码生成前端子表表头模型.
/// </summary>
public class CodeGenFrontEndSubTableHeaderModel
{
    /// <summary>
    /// 标题.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 数据索引.
    /// </summary>
    public string DataIndex { get; set; }

    /// <summary>
    /// 字段key.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// 标题提示.
    /// </summary>
    public string TipLabel { get; set; }

    /// <summary>
    /// 是否必填.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// 是否千位符.
    /// </summary>
    public bool Thousand { get; set; }

    /// <summary>
    /// 冻结方式.
    /// </summary>
    public string TableFixed { get; set; }

    /// <summary>
    /// 对齐方式.
    /// </summary>
    public string Align { get; set; }
}

/// <summary>
/// 代码生成 前端数据选项.
/// </summary>
[SuppressSniffer]
public class CodeGenFrontEndDataOption
{
    /// <summary>
    /// 数据类型.
    /// </summary>
    public CodeGenFrontEndDataType DataType { get; set; }

    /// <summary>
    /// 模版JSON.
    /// </summary>
    public string TemplateJson { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 选项值.
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// 是否子表.
    /// </summary>
    public bool IsSubTable { get; set; }

    /// <summary>
    /// 子表名称.
    /// </summary>
    public string SubTableName { get; set; }

    /// <summary>
    /// 是否联动控件(正).
    /// </summary>
    public bool IsLinkage { get; set; }

    /// <summary>
    /// 会否受子表内部联动.
    /// </summary>
    public bool IsSubTableLinkage { get; set; }

    /// <summary>
    /// 是否列表Option
    /// 行内编辑使用.
    /// </summary>
    public bool IsColumnOption { get; set; }
}

/// <summary>
/// 代码生成 前端数据类型.
/// </summary>
[SuppressSniffer]
public enum CodeGenFrontEndDataType
{
    /// <summary>
    /// 数据字典.
    /// </summary>
    [Description("dictionary")]
    dictionary,

    /// <summary>
    /// 静态数据.
    /// </summary>
    [Description("static")]
    @static,

    /// <summary>
    /// 远程数据.
    /// </summary>
    [Description("dynamic")]
    dynamic,
}

/// <summary>
/// 代码生成 前端联动(正)模型.
/// </summary>
[SuppressSniffer]
public class CodeGenFrontEndLinkageConfig
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 是否子表.
    /// </summary>
    public bool IsSubTable { get; set; }

    /// <summary>
    /// 子表名称.
    /// </summary>
    public string SubTableName { get; set; }

    /// <summary>
    /// 联动反向关系.
    /// </summary>
    public List<LinkageConfig> LinkageRelationship { get; set; } = new List<LinkageConfig>();
}

/// <summary>
/// 代码生成子表控件模型.
/// </summary>
[SuppressSniffer]
public class CodeGenFrontEndSubTableControlModel
{
    /// <summary>
    /// 控件名称.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 首字母小写列名.
    /// </summary>
    public string LowerName => string.IsNullOrWhiteSpace(Name) ? null : Name.Substring(0, 1).ToLower() + Name[1..];

    /// <summary>
    /// jnpfKey.
    /// </summary>
    public string jnpfKey { get; set; }

    /// <summary>
    /// 是否多选.
    /// </summary>
    public bool Multiple { get; set; }

    /// <summary>
    /// 是否必填.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// 验证规则.
    /// </summary>
    public List<RegListModel> RegList { get; set; }

    /// <summary>
    /// 控件标题.
    /// </summary>
    public string Label { get; set; }

}