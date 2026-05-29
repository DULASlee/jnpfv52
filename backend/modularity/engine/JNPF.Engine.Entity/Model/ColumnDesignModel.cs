using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model;

/// <summary>
/// 列表设计模型.
/// </summary>
[SuppressSniffer]
public class ColumnDesignModel
{
    /// <summary>
    /// 打印模板ID.
    /// </summary>
    public List<string> printIds { get; set; }

    /// <summary>
    /// 查询列表.
    /// </summary>
    public List<IndexSearchFieldModel> searchList { get; set; }

    /// <summary>
    /// 数据过滤.
    /// </summary>
    public DataFilteringModel ruleList { get; set; }

    /// <summary>
    /// App数据过滤.
    /// </summary>
    public DataFilteringModel ruleListApp { get; set; }

    /// <summary>
    /// 复杂表头.
    /// </summary>
    public List<ComplexHeaderModel> complexHeaderList { get; set; }

    /// <summary>
    /// 显示列.
    /// </summary>
    public List<IndexGridFieldModel> columnList { get; set; }

    /// <summary>
    /// 默认显示列.
    /// </summary>
    public List<IndexGridFieldModel> defaultColumnList { get; set; }

    /// <summary>
    /// 默认排序.
    /// </summary>
    public List<DefaultSortFieldModel>? defaultSortConfig { get; set; }

    /// <summary>
    /// 列选项.
    /// </summary>
    public List<IndexGridFieldModel> columnOptions { get; set; }

    /// <summary>
    /// 列表布局
    /// 1-普通列表,2-左侧树形+普通表格,3-分组表格,4-编辑表格,5-树形表格.
    /// </summary>
    public int type { get; set; } = 1;

    /// <summary>
    /// 高级查询.
    /// </summary>
    public bool hasSuperQuery { get; set; }

    /// <summary>
    /// 子表样式
    /// 1-分组展示,2-折叠展示.
    /// </summary>
    public int childTableStyle { get; set; }

    /// <summary>
    /// 排序字段.
    /// </summary>
    public string defaultSidx { get; set; }

    /// <summary>
    /// 排序类型.
    /// </summary>
    public string sort { get; set; }

    /// <summary>
    /// 列表分页.
    /// </summary>
    public bool hasPage { get; set; }

    /// <summary>
    /// 分页条数.
    /// </summary>
    public int pageSize { get; set; }

    /// <summary>
    /// 左侧树标题.
    /// </summary>
    public string treeTitle { get; set; }

    /// <summary>
    /// 树数据来源.
    /// </summary>
    public string treeDataSource { get; set; }

    /// <summary>
    /// 树数据字典.
    /// </summary>
    public string treeDictionary { get; set; }

    /// <summary>
    /// 关联字段.
    /// </summary>
    public string treeRelation { get; set; }

    /// <summary>
    /// 数据接口.
    /// </summary>
    public string treePropsUrl { get; set; }

    /// <summary>
    /// 主键字段.
    /// </summary>
    public string treePropsValue { get; set; }

    /// <summary>
    /// 子级字段.
    /// </summary>
    public string treePropsChildren { get; set; }

    /// <summary>
    /// 显示字段.
    /// </summary>
    public string treePropsLabel { get; set; }

    /// <summary>
    /// 左侧树同步类型
    /// 0-同步,1-异步.
    /// </summary>
    public int treeSyncType { get; set; }

    /// <summary>
    /// 是否开启左侧树查询.
    /// </summary>
    public bool hasTreeQuery { get; set; }

    /// <summary>
    /// 左侧树异步接口.
    /// </summary>
    public string treeSyncInterfaceId { get; set; }

    /// <summary>
    /// 左侧树模板JSON.
    /// </summary>
    public List<object> treeSyncTemplateJson { get; set; }

    /// <summary>
    /// 左侧树模板JSON.
    /// </summary>
    public List<object> treeTemplateJson { get; set; }

    /// <summary>
    /// 分组字段.
    /// </summary>
    public string groupField { get; set; }

    /// <summary>
    /// 树形表格 - 父级字段.
    /// </summary>
    public string parentField { get; set; }

    /// <summary>
    /// 列表权限.
    /// </summary>
    public bool useColumnPermission { get; set; }

    /// <summary>
    /// 表单权限.
    /// </summary>
    public bool useFormPermission { get; set; }

    /// <summary>
    /// 按钮权限.
    /// </summary>
    public bool useBtnPermission { get; set; }

    /// <summary>
    /// 数据权限.
    /// </summary>
    public bool useDataPermission { get; set; }

    /// <summary>
    /// 按钮配置.
    /// </summary>
    public List<ButtonConfigModel> btnsList { get; set; }

    /// <summary>
    /// 列按钮配.
    /// </summary>
    public List<ButtonConfigModel> columnBtnsList { get; set; }

    /// <summary>
    /// 自定义按钮配置.
    /// </summary>
    public List<ButtonConfigModel> customBtnsList { get; set; }

    /// <summary>
    /// 是否合计.
    /// </summary>
    public bool showSummary { get; set; }

    /// <summary>
    /// 列表合计字段.
    /// </summary>
    public List<string> summaryField { get; set; }

    /// <summary>
    /// 上传数据模板json.
    /// </summary>
    public UploaderTemplateJsonModel uploaderTemplateJson { get; set; }

    /// <summary>
    /// 溢出省略.
    /// </summary>
    public bool showOverflow { get; set; }
}