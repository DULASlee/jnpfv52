using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model;

/// <summary>
/// 配置模型.
/// </summary>
[SuppressSniffer]
public class ConfigModel
{
    /// <summary>
    /// 标题名.
    /// </summary>
    public string label { get; set; }

    /// <summary>
    /// 标题提示.
    /// </summary>
    public string tipLabel { get; set; }

    /// <summary>
    /// 标题宽度.
    /// </summary>
    public int? labelWidth { get; set; }

    /// <summary>
    /// 是否显示标题.
    /// </summary>
    public bool showLabel { get; set; }

    /// <summary>
    /// 控件名.
    /// </summary>
    public string tag { get; set; }

    /// <summary>
    /// 控件图标.
    /// </summary>
    public string tagIcon { get; set; }

    /// <summary>
    /// 是否必填.
    /// </summary>
    public bool required { get; set; }

    /// <summary>
    /// 布局类型.
    /// </summary>
    public string layout { get; set; }

    /// <summary>
    /// object数据类型.
    /// </summary>
    public string dataType { get; set; }

    /// <summary>
    /// 控件宽度.
    /// </summary>
    public int span { get; set; }

    /// <summary>
    /// jnpf识别符.
    /// </summary>
    public string jnpfKey { get; set; }

    /// <summary>
    /// 数据字典类型.
    /// </summary>
    public string dictionaryType { get; set; }

    /// <summary>
    /// 控件ID.
    /// </summary>
    public int? formId { get; set; }

    /// <summary>
    /// 控件标识符.
    /// </summary>
    public long? renderKey { get; set; }

    /// <summary>
    /// 父级VModel.
    /// </summary>
    public string parentVModel { get; set; }

    /// <summary>
    /// 是否子表.
    /// </summary>
    public bool isSubTable { get; set; }

    /// <summary>
    /// 验证规则.
    /// </summary>
    public List<RegListModel> regList { get; set; }

    /// <summary>
    /// 默认值.
    /// </summary>
    public object defaultValue { get; set; }

    /// <summary>
    /// 当前默认值.
    /// </summary>
    public bool defaultCurrent { get; set; }

    /// <summary>
    /// 远端数据接口.
    /// </summary>
    public string propsUrl { get; set; }

    /// <summary>
    /// 是否显示子表标题.
    /// </summary>
    public bool showTitle { get; set; }

    /// <summary>
    /// 数据库子表名称.
    /// </summary>
    public string tableName { get; set; }

    /// <summary>
    /// 子集.
    /// </summary>
    public List<FieldsModel> children { get; set; }

    /// <summary>
    /// 单据规则必须填.
    /// </summary>
    public string rule { get; set; }

    /// <summary>
    /// 是否隐藏.
    /// </summary>
    public bool noShow { get; set; } = false;

    /// <summary>
    /// 验证时机.
    /// </summary>
    public object trigger { get; set; }

    /// <summary>
    /// 被选中(适用于tab和折叠面板).
    /// </summary>
    public object active { get; set; }

    /// <summary>
    /// 列宽度.
    /// </summary>
    public int? columnWidth { get; set; }

    /// <summary>
    /// 关联表.
    /// </summary>
    public string relationTable { get; set; }

    /// <summary>
    /// 请求端可见 pc、app.
    /// </summary>
    public List<string> visibility { get; set; }

    /// <summary>
    /// 表格边框样式.
    /// </summary>
    public string borderType { get; set; }

    /// <summary>
    /// 表格边框颜色.
    /// </summary>
    public string borderColor { get; set; }

    /// <summary>
    /// 表格背景颜色.
    /// </summary>
    public string backgroundColor { get; set; }

    /// <summary>
    /// 表格边框宽度.
    /// </summary>
    public int borderWidth { get; set; }

    /// <summary>
    /// 跨列.
    /// </summary>
    public int colspan { get; set; }

    /// <summary>
    /// 跨行.
    /// </summary>
    public int rowspan { get; set; }

    /// <summary>
    /// 是否合并.
    /// </summary>
    public bool merged { get; set; }

    /// <summary>
    /// 是否唯一.
    /// </summary>
    public bool unique { get; set; }

    /// <summary>
    /// 联动模板json.
    /// </summary>
    public List<LinkageConfig> templateJson { get; set; }

    /// <summary>
    /// 开始时间规则开关.
    /// </summary>
    public bool startTimeRule { get; set; } = false;

    /// <summary>
    /// 开始时间单位：1-年,2-月,3-日/1-时,2-分,3-秒.
    /// </summary>
    public int startTimeTarget { get; set; }

    /// <summary>
    /// 开始时间类型：1-特定时间,2-表单字段,3-填写当前时间,4-当前时间前,5-当前时间后.
    /// </summary>
    public int startTimeType { get; set; }

    /// <summary>
    /// 开始时间值.
    /// </summary>
    public string startTimeValue { get; set; }

    /// <summary>
    /// 开始时间关联字段.
    /// </summary>
    public string startRelationField { get; set; }

    /// <summary>
    /// 结束时间规则开关.
    /// </summary>
    public bool endTimeRule { get; set; } = false;

    /// <summary>
    /// 结束时间单位：1-年,2-月,3-日/1-时,2-分,3-秒.
    /// </summary>
    public int endTimeTarget { get; set; }

    /// <summary>
    /// 结束时间类型：1-特定时间,2-表单字段,3-填写当前时间,4-当前时间前,5-当前时间后.
    /// </summary>
    public int endTimeType { get; set; }

    /// <summary>
    /// 结束时间值.
    /// </summary>
    public string endTimeValue { get; set; }

    /// <summary>
    /// 结束时间关联字段.
    /// </summary>
    public string endRelationField { get; set; }

    /// <summary>
    /// 复杂表头.
    /// </summary>
    public List<ComplexHeaderModel> complexHeaderList { get; set; }

    /// <summary>
    /// 对齐方式.
    /// </summary>
    public string tableAlgin { get; set; }

    /// <summary>
    /// 冻结方式.
    /// </summary>
    public string tableFixed { get; set; }
}

/// <summary>
/// 联动配置.
/// </summary>
public class LinkageConfig
{
    /// <summary>
    /// 默认值.
    /// </summary>
    public string defaultValue { get; set; }

    /// <summary>
    /// 字段.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 数据类型.
    /// </summary>
    public string dataType { get; set; }

    /// <summary>
    /// 是否必填.
    /// </summary>
    public int required { get; set; }

    /// <summary>
    /// 字段名.
    /// </summary>
    public string fieldName { get; set; }

    /// <summary>
    /// 关联表单字段.
    /// </summary>
    public string relationField { get; set; }

    /// <summary>
    /// jnpf识别符.
    /// </summary>
    public string jnpfKey { get; set; }

    /// <summary>
    /// 子表复杂表头.
    /// </summary>
    public List<ComplexHeaderModel> complexHeaderList { get; set; }

    /// <summary>
    /// 参数来源.
    /// </summary>
    public int sourceType { get; set; }

    /// <summary>
    /// 后端自生成字段
    /// 是否子表控件.
    /// </summary>
    public bool isChildren { get; set; }

    /// <summary>
    /// 后端自生成字段
    /// 是否多选.
    /// </summary>
    public bool IsMultiple { get; set; }
}