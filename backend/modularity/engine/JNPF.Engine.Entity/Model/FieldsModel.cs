using JNPF.DependencyInjection;
using Newtonsoft.Json;

namespace JNPF.Engine.Entity.Model;

/// <summary>
/// 组件模型.
/// </summary>
[SuppressSniffer]
public class FieldsModel
{
    /// <summary>
    /// 配置.
    /// </summary>
    public ConfigModel __config__ { get; set; }

    /// <summary>
    /// 插槽.
    /// </summary>
    public SlotModel __slot__ { get; set; }

    /// <summary>
    /// 前.
    /// </summary>
    public string addonBefore { get; set; }

    /// <summary>
    /// 后.
    /// </summary>
    public string addonAfter { get; set; }

    /// <summary>
    /// 占位提示.
    /// </summary>
    public string placeholder { get; set; }

    /// <summary>
    /// 样式.
    /// </summary>
    public object style { get; set; }

    /// <summary>
    /// 是否可清除.
    /// </summary>
    public bool clearable { get; set; }

    /// <summary>
    /// 前图标.
    /// </summary>
    //[JsonProperty(propertyName: "prefix-icon")]
    public string prefixIcon { get; set; }

    /// <summary>
    /// 后图标.
    /// </summary>
    //[JsonProperty(propertyName: "suffix-icon")]
    public string suffixIcon { get; set; }

    /// <summary>
    /// 最大长度.
    /// </summary>
    public long? maxlength { get; set; }

    /// <summary>
    /// 是否显示输入字数统计.
    /// </summary>
    //[JsonProperty(propertyName: "show-word-limit")]
    public bool showWordlimit { get; set; }

    /// <summary>
    /// 是否只读.
    /// </summary>
    public bool @readonly { get; set; }

    /// <summary>
    /// 是否禁用.
    /// </summary>
    public bool disabled { get; set; }

    /// <summary>
    /// 签章控件属性.
    /// </summary>
    public bool disaabled { get; set; }

    /// <summary>
    /// 设置默认值为空字符串.
    /// </summary>
    public string __vModel__ { get; set; } = string.Empty;

    /// <summary>
    /// 类型.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 自适应内容高度.
    /// </summary>
    public object autoSize { get; set; }

    /// <summary>
    /// 计数器步长.
    /// </summary>
    public decimal? step { get; set; }

    /// <summary>
    /// 是否只能输入 step 的倍数.
    /// </summary>
    //[JsonProperty(propertyName: "step-strictly")]
    public bool stepstrictly { get; set; }

    /// <summary>
    /// 控制按钮位置.
    /// </summary>
    //[JsonProperty(propertyName: "controlsPosition")]
    public string controlsPosition { get; set; }

    /// <summary>
    /// 控制.
    /// </summary>
    public bool controls { get; set; }

    /// <summary>
    /// 文本样式.
    /// </summary>
    public object textStyle { get; set; }

    /// <summary>
    /// 字体样式.
    /// </summary>
    public string fontStyle { get; set; }

    /// <summary>
    /// 是否显示中文大写.
    /// </summary>
    public bool showChinese { get; set; }

    /// <summary>
    /// 边框.
    /// </summary>
    public bool border { get; set; }

    /// <summary>
    /// 选项样式.
    /// </summary>
    public string optionType { get; set; }

    /// <summary>
    /// 是否显示密码.
    /// </summary>
    //[JsonProperty(propertyName: "show-password")]
    public bool showPassword { get; set; }

    /// <summary>
    /// 使用扫码.
    /// </summary>
    public bool useScan { get; set; }

    /// <summary>
    /// 使用掩码.
    /// </summary>
    public bool useMask { get; set; }

    /// <summary>
    /// 掩码配置.
    /// </summary>
    public MaskConfigModel maskConfig { get; set; }

    /// <summary>
    /// 规格.
    /// </summary>
    public string size { get; set; }

    /// <summary>
    /// 是否可搜索.
    /// </summary>
    public bool filterable { get; set; }

    /// <summary>
    /// 是否多选.
    /// </summary>
    public bool multiple { get; set; }

    /// <summary>
    /// 是否多选查询.
    /// </summary>
    public bool searchMultiple { get; set; }

    ///// <summary>
    ///// 配置选项.
    ///// </summary>
    //public PropsModel props { get; set; }

    /// <summary>
    /// 配置选项.
    /// </summary>
    public PropsBeanModel props { get; set; }

    /// <summary>
    /// 输入框中是否显示选中值的完整路径.
    /// </summary>
    //[JsonProperty(propertyName: "show-all-levels")]
    public bool showAllLevels { get; set; }

    /// <summary>
    /// 选项分隔符.
    /// </summary>
    public string separator { get; set; }

    /// <summary>
    /// 是否为时间范围选择，仅对<see cref="el-time-picker"/>有效.
    /// </summary>
    public bool isrange { get; set; }

    /// <summary>
    /// 选择范围时的分隔符.
    /// </summary>
    [JsonProperty(propertyName: "range-separator")]
    public string rangeseparator { get; set; }

    /// <summary>
    /// 范围选择时开始日期/时间的占位内容.
    /// </summary>
    [JsonProperty(propertyName: "start-placeholder")]
    public string startplaceholder { get; set; }

    /// <summary>
    /// 范围选择时开始日期/时间的占位内容.
    /// </summary>
    [JsonProperty(propertyName: "end-placeholder")]
    public string endplaceholder { get; set; }

    /// <summary>
    /// 显示绑定值的格式.
    /// </summary>
    public string format { get; set; }

    /// <summary>
    /// 实际绑定值的格式.
    /// </summary>
    [JsonProperty(propertyName: "value-format")]
    public string valueformat { get; set; }

    /// <summary>
    /// 当前时间日期选择器特有的选项.
    /// </summary>
    [JsonProperty(propertyName: "picker-options")]
    public object pickeroptions { get; set; }

    /// <summary>
    /// 最大值.
    /// </summary>
    // [JsonProperty(propertyName: "count")]
    public int? max { get; set; }

    /// <summary>
    /// 是否允许半选.
    /// </summary>
    // [JsonProperty(propertyName: "allow-half")]
    public bool allowHalf { get; set; }

    /// <summary>
    /// 是否显示文本.
    /// </summary>
    [JsonProperty(propertyName: "show-text")]
    public bool showtext { get; set; }

    /// <summary>
    /// 是否显示分数.
    /// </summary>
    [JsonProperty(propertyName: "show-score")]
    public bool showScore { get; set; }

    /// <summary>
    /// 是否支持透明度选择.
    /// </summary>
    // [JsonProperty(propertyName: "show-alpha")]
    public bool showAlpha { get; set; }

    /// <summary>
    /// 颜色的格式.
    /// </summary>
    // [JsonProperty(propertyName: "color-format")]
    public string colorFormat { get; set; }

    /// <summary>
    /// 颜色.
    /// </summary>
    public string color { get; set; }

    /// <summary>
    /// switch 打开时的背景色.
    /// </summary>
    [JsonProperty(propertyName: "active-color")]
    public string activecolor { get; set; }

    /// <summary>
    /// switch 关闭时的背景色.
    /// </summary>
    [JsonProperty(propertyName: "inactive-color")]
    public string inactivecolor { get; set; }

    /// <summary>
    /// switch 打开时的值.
    /// </summary>
    //[JsonProperty(propertyName: "active-value")]
    public int? activeValue { get; set; }

    /// <summary>
    /// switch 关闭时的值.
    /// </summary>
    //[JsonProperty(propertyName: "inactive-value")]
    public int? inactiveValue { get; set; }

    /// <summary>
    /// 最小值.
    /// </summary>
    public int? min { get; set; }

    /// <summary>
    /// 是否显示间断点.
    /// </summary>
    [JsonProperty(propertyName: "show-stops")]
    public bool showstops { get; set; }

    /// <summary>
    /// 是否为范围选择
    /// 滑块.
    /// </summary>
    public bool range { get; set; }

    /// <summary>
    /// 可接受上传类型.
    /// </summary>
    public string accept { get; set; }

    /// <summary>
    /// 是否显示上传提示.
    /// </summary>
    public bool showTip { get; set; }

    /// <summary>
    /// 文件大小.
    /// </summary>
    public int? fileSize { get; set; }

    /// <summary>
    /// 文件大小单位.
    /// </summary>
    public string sizeUnit { get; set; }

    /// <summary>
    /// 最大上传个数.
    /// </summary>
    public int? limit { get; set; }

    /// <summary>
    /// 评分 最大值.
    /// </summary>
    public int count { get; set; }

    /// <summary>
    /// 文案的位置.
    /// </summary>
    //[JsonProperty(propertyName: "content-position")]
    public string contentPosition { get; set; }

    /// <summary>
    /// 上传按钮文本.
    /// </summary>
    public string buttonText { get; set; }

    /// <summary>
    /// 等级.
    /// </summary>
    public int level { get; set; }

    /// <summary>
    /// 配置项.
    /// </summary>
    public List<Dictionary<string, object>> options { get; set; }

    /// <summary>
    /// 设置阴影显示时机.
    /// </summary>
    public string shadow { get; set; }

    /// <summary>
    /// app卡片容器标题.
    /// </summary>
    public string header { get; set; }

    /// <summary>
    /// 分组标题的内容.
    /// </summary>
    public string content { get; set; }

    /// <summary>
    /// 分组标题的标题提示.
    /// </summary>
    public string helpMessage { get; set; }

    /// <summary>
    /// 标题提示.
    /// </summary>
    public string tipLabel { get; set; }

    /// <summary>
    /// 关联表单id.
    /// </summary>
    public string modelId { get; set; }

    /// <summary>
    /// 关联表单字段.
    /// </summary>
    public string relationField { get; set; }

    /// <summary>
    /// 关联表单属性 显示 字段.
    /// </summary>
    public string showField { get; set; }

    /// <summary>
    /// 流程ID.
    /// </summary>
    public string flowId { get; set; }

    /// <summary>
    /// 查询类型
    /// 1-等于,2-模糊,3-范围,.
    /// </summary>
    public int? searchType { get; set; }

    /// <summary>
    /// 数据接口ID.
    /// </summary>
    public string interfaceId { get; set; }

    /// <summary>
    /// 列表配置.
    /// </summary>
    public List<ColumnOptionsModel> columnOptions { get; set; }

    /// <summary>
    /// 是否分页.
    /// </summary>
    public bool hasPage { get; set; }

    /// <summary>
    /// 页数.
    /// </summary>
    public int? pageSize { get; set; }

    /// <summary>
    /// 弹窗选择主键.
    /// </summary>
    public string propsValue { get; set; }

    /// <summary>
    /// 折叠菜单.
    /// </summary>
    public bool accordion { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string title { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// Tab位置.
    /// </summary>
    //[JsonProperty(propertyName: "tab-position")]
    public string tabPosition { get; set; }

    /// <summary>
    /// 精度.
    /// </summary>
    public int? precision { get; set; }

    /// <summary>
    /// 开关控件 属性 - 开启展示值.
    /// </summary>
    public string activeTxt { get; set; }

    /// <summary>
    /// 开关控件 属性 - 关闭展示值.
    /// </summary>
    public string inactiveTxt { get; set; }

    /// <summary>
    /// 系统控件 - 所属组织 属性 - 显示内容
    /// all ：显示组织， last ： 显示部门.
    /// </summary>
    public string showLevel { get; set; }

    /// <summary>
    /// 对齐方式.
    /// </summary>
    public string align { get; set; }

    /// <summary>
    /// 是否开启合计.
    /// </summary>
    //[JsonProperty(propertyName: "show-summary")]
    public bool showSummary { get; set; }

    /// <summary>
    /// 合计数组.
    /// </summary>
    public List<string> summaryField { get; set; }

    /// <summary>
    /// 弹窗类型.
    /// </summary>
    public string popupType { get; set; }

    /// <summary>
    /// 弹窗标题.
    /// </summary>
    public string popupTitle { get; set; }

    /// <summary>
    /// 弹窗宽度.
    /// </summary>
    public string popupWidth { get; set; }

    /// <summary>
    /// 链接地址.
    /// </summary>
    public string href { get; set; }

    /// <summary>
    /// 打开方式.
    /// </summary>
    public string target { get; set; }

    /// <summary>
    /// 提示按钮开关.
    /// </summary>
    public bool closable { get; set; }

    /// <summary>
    /// 是否显示图标.
    /// </summary>
    //[JsonProperty(propertyName: "show-icon")]
    public bool showIcon { get; set; }

    /// <summary>
    /// 可选范围.
    /// </summary>
    public string selectType { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public List<string> ableRelationIds { get; set; }

    /// <summary>
    /// 新用户选择控件.
    /// </summary>
    public List<object> ableIds { get; set; }

    /// <summary>
    /// 子表添加数据类型
    /// 0-常规,1-数据传递.
    /// </summary>
    public int addType { get; set; }

    /// <summary>
    /// 联动模板json.
    /// </summary>
    public List<LinkageConfig> templateJson { get; set; }

    /// <summary>
    /// 路径类型.
    /// </summary>
    public string pathType { get; set; }

    /// <summary>
    /// 是否开启 分用户存储
    /// 0-关闭,1-开启.
    /// </summary>
    public int isAccount { get; set; } = -1;

    /// <summary>
    /// 文件夹名.
    /// </summary>
    public string folder { get; set; }

    /// <summary>
    /// 单选 radio buttonStyle.
    /// </summary>
    public string buttonStyle { get; set; }

    /// <summary>
    /// 控件属性类型 0:展示数据，1:存储数据.
    /// </summary>
    public int isStorage { get; set; }

    /// <summary>
    /// 排列方式.
    /// </summary>
    public string direction { get; set; }

    /// <summary>
    /// 显示条数.
    /// </summary>
    public int? total { get; set; }

    /// <summary>
    /// 千位分隔.
    /// </summary>
    public bool thousands { get; set; }

    /// <summary>
    /// 大写金额.
    /// </summary>
    public bool isAmountChinese { get; set; }

    /// <summary>
    /// 辅助文本.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 按钮文字.
    /// </summary>
    public string closeText { get; set; }

    /// <summary>
    /// 上传提示.
    /// </summary>
    public string tipText { get; set; }

    /// <summary>
    /// 关键词查询.
    /// </summary>
    public bool isKeyword { get; set; }

    /// <summary>
    /// iframe控件 高度.
    /// </summary>
    public decimal? height { get; set; }

    /// <summary>
    /// iframe 边框.
    /// </summary>
    public string borderType { get; set; }

    /// <summary>
    /// iframe 边框颜色.
    /// </summary>
    public string borderColor { get; set; }

    /// <summary>
    /// iframe 边框宽度.
    /// </summary>
    public int borderWidth { get; set; }

    /// <summary>
    /// 开启微调(定位控件属性).
    /// </summary>
    public bool enableLocationScope { get; set; }

    /// <summary>
    /// 自动定位(定位控件属性).
    /// </summary>
    public bool autoLocation { get; set; }

    /// <summary>
    /// 微调范围(定位控件属性).
    /// </summary>
    public string adjustmentScope { get; set; }

    /// <summary>
    /// 定位区域(定位控件属性).
    /// </summary>
    public bool enableDesktopLocation { get; set; }

    /// <summary>
    /// 定位区域范围(定位控件属性).
    /// </summary>
    public List<object> locationScope { get; set; }

    /// <summary>
    /// 子表列按钮.
    /// </summary>
    public List<ChildTableBtnsList> columnBtnsList { get; set; }

    /// <summary>
    /// 子表底部按钮.
    /// </summary>
    public List<ChildTableBtnsList> footerBtnsList { get; set; }

    /// <summary>
    /// 二维码控件属性.
    /// </summary>
    public string colorDark { get; set; }

    /// <summary>
    /// 二维码控件属性.
    /// </summary>
    public string colorLight { get; set; }

    /// <summary>
    /// 二维码、条形码控件属性.
    /// </summary>
    public int width { get; set; }

    /// <summary>
    /// 二维码、条形码控件属性.
    /// </summary>
    public string dataType { get; set; }

    /// <summary>
    /// 二维码、条形码控件属性.
    /// </summary>
    public string staticText { get; set; }

    /// <summary>
    /// 条形码控件属性.
    /// </summary>
    public string lineColor { get; set; }

    /// <summary>
    /// 条形码控件属性.
    /// </summary>
    public string background { get; set; }

    /// <summary>
    /// 单行输入和多行输入 统计字符.
    /// </summary>
    public bool showCount { get; set; }

    /// <summary>
    /// 后端自我创建字段、用于统一处理减少循环判断
    /// 是否查询字段.
    /// </summary>
    [JsonIgnore]
    public bool isQueryField { get; set; }

    /// <summary>
    /// 后端自我创建字段、用于统一处理减少循环判断
    /// 是否列表展示.
    /// </summary>
    [JsonIgnore]
    public bool isIndexShow { get; set; }

    /// <summary>
    /// 后端自我创建字段、用于统一处理减少循环判断
    /// 是否被联动(反).
    /// </summary>
    [JsonIgnore]
    public bool IsLinked { get; set; }

    /// <summary>
    /// 后端自我创建字段、用于统一处理减少循环判断
    /// 是否联动(正).
    /// </summary>
    [JsonIgnore]
    public bool IsLinkage { get; set; }

    /// <summary>
    /// 后端自我创建字段、用于统一处理减少循环判断
    /// 联动反向关系.
    /// </summary>
    [JsonIgnore]
    public List<LinkageConfig> linkageReverseRelationship { get; set; } = new List<LinkageConfig>();

    /// <summary>
    /// 后端自我创建字段、用于统一处理减少循环
    /// 上级__vModel__.
    /// </summary>
    [JsonIgnore]
    public string superiorVModel { get; set; }

    /// <summary>
    /// 后端自我创建字段、用于统一处理关联表单属性与弹窗选择属性.
    /// </summary>
    [JsonIgnore]
    public string relational { get; set; }

    /// <summary>
    /// 后端自我创建字段、用于存储子表主键名.
    /// </summary>
    [JsonIgnore]
    public string TablePrimaryKey { get; set; }
}