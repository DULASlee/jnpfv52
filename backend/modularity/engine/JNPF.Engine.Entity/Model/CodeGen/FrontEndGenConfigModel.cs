using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.CodeGen;

/// <summary>
/// 前端生成配置模型.
/// </summary>
[SuppressSniffer]
public class FrontEndGenConfigModel
{
    /// <summary>
    /// 命名空间.
    /// </summary>
    public string NameSpace { get; set; }

    /// <summary>
    /// 类型名称.
    /// </summary>
    public string ClassName { get; set; }

    /// <summary>
    /// 表单.
    /// </summary>
    public string FormRef { get; set; }

    /// <summary>
    /// 表单模型.
    /// </summary>
    public string FormModel { get; set; }

    /// <summary>
    /// 尺寸.
    /// </summary>
    public string Size { get; set; }

    /// <summary>
    /// 布局方式-文本定位.
    /// </summary>
    public string LabelPosition { get; set; }

    /// <summary>
    /// 布局方式-文本宽度.
    /// </summary>
    public int LabelWidth { get; set; }

    /// <summary>
    /// 表单规则.
    /// </summary>
    public string FormRules { get; set; }

    /// <summary>
    /// 列表布局
    /// 1-普通列表,2-左侧树形+普通表格,3-分组表格,4-行内编辑,5-树形表格.
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// 左侧树绑定字段.
    /// </summary>
    public string TreeRelation { get; set; }

    /// <summary>
    /// 左侧树是否自定义范围.
    /// </summary>
    public string TreeSelectType { get; set; }

    /// <summary>
    /// 左侧树自定义范围.
    /// </summary>
    public string TreeAbleIds { get; set; }

    /// <summary>
    /// 左侧树控件类型.
    /// </summary>
    public string TreeJnpfKey { get; set; }

    /// <summary>
    /// 左侧树标题.
    /// </summary>
    public string TreeTitle { get; set; }

    /// <summary>
    /// 左侧树数据源绑定字段.
    /// </summary>
    public string TreePropsValue { get; set; }

    /// <summary>
    /// 左侧树数据来源.
    /// </summary>
    public string TreeDataSource { get; set; }

    /// <summary>
    /// 是否左侧树绑定字段查询条件多选.
    /// </summary>
    public bool IsTreeRelationMultiple { get; set; }

    /// <summary>
    /// 树数据字典.
    /// </summary>
    public string TreeDictionary { get; set; }

    /// <summary>
    /// 数据接口.
    /// </summary>
    public string TreePropsUrl { get; set; }

    /// <summary>
    /// 子级字段.
    /// </summary>
    public string TreePropsChildren { get; set; }

    /// <summary>
    /// 显示字段.
    /// </summary>
    public string TreePropsLabel { get; set; }

    /// <summary>
    /// 左侧树关联字段控件KEY.
    /// </summary>
    public string TreeRelationControlKey { get; set; }

    /// <summary>
    /// 左侧树是否存在查询内.
    /// </summary>
    public bool IsExistQuery { get; set; }

    /// <summary>
    /// 是否分页.
    /// </summary>
    public bool HasPage { get; set; }

    /// <summary>
    /// 表单列表.
    /// </summary>
    public List<FormScriptDesignModel> FormList { get; set; }

    /// <summary>
    /// 弹窗类型.
    /// </summary>
    public string PopupType { get; set; }

    /// <summary>
    /// 主键.
    /// </summary>
    public string PrimaryKey { get; set; }

    /// <summary>
    /// 表单主键.
    /// </summary>
    public string FormPrimaryKey { get; set; }

    /// <summary>
    /// 查询列设计.
    /// </summary>
    public List<IndexSearchFieldDesignModel> SearchColumnDesign { get; set; }

    /// <summary>
    /// App排序设计.
    /// </summary>
    public List<IndexSearchFieldDesignModel> SortFieldDesign { get; set; }

    /// <summary>
    /// 头部按钮设计.
    /// </summary>
    public List<IndexButtonDesign> TopButtonDesign { get; set; }

    /// <summary>
    /// 头部按钮设计.
    /// </summary>
    public List<IndexButtonDesign> ColumnButtonDesign { get; set; }

    /// <summary>
    /// 列表设计.
    /// </summary>
    public List<IndexColumnDesign> ColumnDesign { get; set; }

    /// <summary>
    /// 复杂表头.
    /// </summary>
    public List<ComplexHeaderModel> complexHeaderList { get; set; }

    /// <summary>
    /// 列表主表控件Option.
    /// </summary>
    public List<CodeGenConvIndexListControlOptionDesign> OptionsList { get; set; }

    /// <summary>
    /// 表单全部控件.
    /// </summary>
    public List<FormControlDesignModel> FormAllContols { get; set; }

    /// <summary>
    /// 表单全部控件,行内编辑复制表头使用.
    /// </summary>
    public List<FormControlDesignModel> ComplexFormAllContols { get; set; }

    /// <summary>
    /// 是否有批量删除.
    /// </summary>
    public bool IsBatchRemoveDel { get; set; }

    /// <summary>
    /// 是否有批量打印.
    /// </summary>
    public bool IsBatchPrint { get; set; }

    /// <summary>
    /// 批量打印IDS.
    /// </summary>
    public string PrintIds { get; set; }

    /// <summary>
    /// 是否有导出.
    /// </summary>
    public bool IsDownload { get; set; }

    /// <summary>
    /// 是否有删除.
    /// </summary>
    public bool IsRemoveDel { get; set; }

    /// <summary>
    /// 是否有详情.
    /// </summary>
    public bool IsDetail { get; set; }

    /// <summary>
    /// 是否有编辑.
    /// </summary>
    public bool IsEdit { get; set; }

    /// <summary>
    /// 是否有排序.
    /// </summary>
    public bool IsSort { get; set; }

    /// <summary>
    /// 是否有新增.
    /// </summary>
    public bool IsAdd { get; set; }

    /// <summary>
    /// 是否有导入.
    /// </summary>
    public bool IsUpload { get; set; }

    /// <summary>
    /// 是否开启控件默认值.
    /// </summary>
    public bool IsDefaultFormControl { get; set; }

    /// <summary>
    /// 是否开启查询默认值.
    /// </summary>
    public bool IsDefaultSearchField { get; set; }

    /// <summary>
    /// 查询默认值列表.
    /// </summary>
    public List<DefaultSearchFieldModel> DefaultSearchList { get; set; }

    /// <summary>
    /// 是否开启按钮权限.
    /// </summary>
    public bool UseBtnPermission { get; set; }

    /// <summary>
    /// 是否开启列表权限.
    /// </summary>
    public bool UseColumnPermission { get; set; }

    /// <summary>
    /// 是否开启表单权限.
    /// </summary>
    public bool UseFormPermission { get; set; }

    /// <summary>
    /// 提交按钮文本.
    /// </summary>
    public string CancelButtonText { get; set; }

    /// <summary>
    /// 确认按钮文本.
    /// </summary>
    public string ConfirmButtonText { get; set; }

    /// <summary>
    /// 普通弹窗表单宽度.
    /// </summary>
    public string GeneralWidth { get; set; }

    /// <summary>
    /// 全屏弹窗表单宽度.
    /// </summary>
    public string FullScreenWidth { get; set; }

    /// <summary>
    /// drawer宽度.
    /// </summary>
    public string DrawerWidth { get; set; }

    /// <summary>
    /// 表单样式.
    /// </summary>
    public string FormStyle { get; set; }

    /// <summary>
    /// 是否合计.
    /// </summary>
    public bool IsSummary { get; set; }

    /// <summary>
    /// 分页大小.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 排序方式.
    /// </summary>
    public string Sort { get; set; }

    /// <summary>
    /// 是否开启打印.
    /// </summary>
    public bool HasPrintBtn { get; set; }

    /// <summary>
    /// 打印按钮文本.
    /// </summary>
    public string PrintButtonText { get; set; }

    /// <summary>
    /// 打印模板ID.
    /// </summary>
    public string PrintId { get; set; }

    /// <summary>
    /// 是否子表数据传递.
    /// </summary>
    public bool IsChildDataTransfer { get; set; }

    /// <summary>
    /// 是否子表查询.
    /// </summary>
    public bool IsChildTableQuery { get; set; }

    /// <summary>
    /// 是否子表显示.
    /// </summary>
    public bool IsChildTableShow { get; set; }

    /// <summary>
    /// 是否列展示字段.
    /// </summary>
    public string ColumnList { get; set; }

    /// <summary>
    /// 高级查询.
    /// </summary>
    public bool HasSuperQuery { get; set; }

    /// <summary>
    /// 列选项.
    /// </summary>
    public string ColumnOptions { get; set; }

    /// <summary>
    /// 是否行内编辑.
    /// </summary>
    public bool IsInlineEditor { get; set; }

    /// <summary>
    /// 列表类型.
    /// </summary>
    public int IndexDataType { get; set; }

    /// <summary>
    /// 分组字段名.
    /// </summary>
    public string GroupField { get; set; }

    /// <summary>
    /// 分组显示字段名..
    /// </summary>
    public string GroupShowField { get; set; }

    /// <summary>
    /// 自增长策略.
    /// </summary>
    public int PrimaryKeyPolicy { get; set; }

    /// <summary>
    /// 是否关联表单.
    /// </summary>
    public bool IsRelationForm { get; set; }

    /// <summary>
    /// 子表样式
    /// 1-分组展示,2-折叠展示.
    /// </summary>
    public int ChildTableStyle { get; set; }

    /// <summary>
    /// 是否冻结.
    /// </summary>
    public bool IsFixed { get; set; }

    /// <summary>
    /// 是否存在子表正则.
    /// </summary>
    public bool IsChildrenRegular { get; set; }

    /// <summary>
    /// 左侧树同步类型
    /// 0-同步,1-异步.
    /// </summary>
    public int TreeSynType { get; set; }

    /// <summary>
    /// 是否开启左侧树查询.
    /// </summary>
    public bool HasTreeQuery { get; set; }

    /// <summary>
    /// 左侧树异步接口.
    /// </summary>
    public CodeGenColumnData ColumnData { get; set; }

    /// <summary>
    /// 是否开启合计.
    /// </summary>
    public bool ShowSummary { get; set; }

    /// <summary>
    /// 列表合计字段.
    /// </summary>
    public List<string> SummaryField { get; set; }

    /// <summary>
    /// 表单控件默认值.
    /// </summary>
    public DefaultFormControlModel DefaultFormControlList { get; set; }

    /// <summary>
    /// 流程引擎表单字段json.
    /// </summary>
    public string PropertyJson { get; set; }

    /// <summary>
    /// 表单真实控件(剔除布局控件).
    /// </summary>
    public List<CodeGenFormRealControlModel> FormRealControl { get; set; }

    /// <summary>
    /// 查询条件查询差异列表.
    /// </summary>
    public List<IndexSearchFieldModel> QueryCriteriaQueryVarianceList { get; set; }

    /// <summary>
    /// 日期特殊属性.
    /// </summary>
    public bool IsDateSpecialAttribute { get; set; }

    /// <summary>
    /// 时间特殊属性.
    /// </summary>
    public bool IsTimeSpecialAttribute { get; set; }

    /// <summary>
    /// 全千位符字段.
    /// </summary>
    public string AllThousandsField { get; set; }

    /// <summary>
    /// 是否存在子表千位符字段.
    /// </summary>
    public bool IsChildrenThousandsField { get; set; }

    /// <summary>
    /// 子表指定日期格式集合.
    /// </summary>
    public List<CodeGenSpecifyDateFormatSetModel> SpecifyDateFormatSet { get; set; }

    /// <summary>
    /// APP列表千位符字段.
    /// </summary>
    public string AppThousandField { get; set; }

    /// <summary>
    /// 是否 确定并继续操作.
    /// </summary>
    public bool HasConfirmAndAddBtn { get; set; }

    /// <summary>
    /// 确定并继续操作 文本.
    /// </summary>
    public string ConfirmAndAddText { get; set; }

    /// <summary>
    /// 溢出省略.
    /// </summary>
    public bool ShowOverflow { get; set; }
}

public class CodeGenColumnData
{
    /// <summary>
    /// 左侧树异步接口.
    /// </summary>
    public string treeInterfaceId { get; set; }

    /// <summary>
    /// 左侧树模板JSON.
    /// </summary>
    public List<object> treeTemplateJson { get; set; }

    /// <summary>
    /// 左侧树模板JSON.
    /// </summary>
    public List<object> templateJson { get; set; }
}