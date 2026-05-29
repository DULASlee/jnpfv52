using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.CodeGen;

/// <summary>
/// 代码生成前端配置模型.
/// </summary>
[SuppressSniffer]
public class CodeGenFrontEndConfigModel
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
    /// 1、纯表单，2、表单加列表.
    /// </summary>
    public int WebType { get; set; }

    /// <summary>
    /// 列表布局
    /// 1-普通列表,2-左侧树形+普通表格,3-分组表格,4-行内编辑,5-树形表格.
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// 纯表单标题.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 头部按钮设计.
    /// </summary>
    public List<IndexButtonDesign> TopButtonDesign { get; set; }

    /// <summary>
    /// 头部按钮设计.
    /// </summary>
    public List<IndexButtonDesign> ColumnButtonDesign { get; set; }

    /// <summary>
    /// 复杂表头配置.
    /// </summary>
    public string ComplexColumns { get; set; }

    /// <summary>
    /// 是否开启流程.
    /// </summary>
    public bool HasFlow { get; set; }

    /// <summary>
    /// 是否开启查询.
    /// </summary>
    public bool HasSearch { get; set; }

    /// <summary>
    /// 是否存在子表.
    /// </summary>
    public bool HasChildTable { get; set; }

    /// <summary>
    /// 左侧配置.
    /// </summary>
    public CodeGenFrontEndLeftTreeFieldDesignModel LeftTree { get; set; }

    /// <summary>
    /// 表格配置.
    /// </summary>
    public CodeGenFrontEndTableConfigModel TableConfig { get; set; }

    /// <summary>
    /// 是否开启`新增`.
    /// </summary>
    public bool HasAdd { get; set; }

    /// <summary>
    /// 是否开启`导出`.
    /// </summary>
    public bool HasDownload { get; set; }

    /// <summary>
    /// 是否开启`导入`.
    /// </summary>
    public bool HasUpload { get; set; }

    /// <summary>
    /// 是否开启`批量删除`.
    /// </summary>
    public bool HasBatchRemove { get; set; }

    /// <summary>
    /// 是否开启`批量打印`.
    /// </summary>
    public bool HasBatchPrint { get; set; }

    /// <summary>
    /// 批量打印ID.
    /// </summary>
    public string BatchPrints { get; set; }

    /// <summary>
    /// 是否开启`编辑`.
    /// </summary>
    public bool HasEdit { get; set; }

    /// <summary>
    /// 是否开启`删除`.
    /// </summary>
    public bool HasRemove { get; set; }

    /// <summary>
    /// 是否开启`详情`.
    /// </summary>
    public bool HasDetail { get; set; }

    /// <summary>
    /// 是否开启`关联表单详情`.
    /// </summary>
    public bool HasRelationDetail { get; set; }

    /// <summary>
    /// 是否开启子表`关联表单详情`.
    /// </summary>
    public bool HasSubTableRelationDetail { get; set; }

    /// <summary>
    /// 是否开启`数组输入千位符`.
    /// </summary>
    public bool HasThousands { get; set; }

    /// <summary>
    /// 是否开启子表`数组输入千位符`.
    /// </summary>
    public bool HasSubTableThousands { get; set; }

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
    /// 表单属性.
    /// </summary>
    public CodeGenFrontEndFormAttributeModel FormAttribute { get; set; }

    /// <summary>
    /// 基本信息.
    /// </summary>
    public CodeGenBasicInfoAttributeModel BasicInfo { get; set; }

    /// <summary>
    /// 主键字段.
    /// </summary>
    public string PrimaryKeyField { get; set; }

    /// <summary>
    /// 显示列字符串.
    /// </summary>
    public string? ColumnList { get; set; }

    /// <summary>
    /// 查询条件字符串.
    /// </summary>
    public string? SearchList { get; set; }

    /// <summary>
    /// 高级查询字符串.
    /// </summary>
    public string? SuperQueryJson { get; set; }

    /// <summary>
    /// 表单JavaScript集合.
    /// </summary>
    public CodeGenFrontEndFormScriptModel FormScript { get; set; }

    /// <summary>
    /// 查询条件查询差异列表.
    /// </summary>
    public List<string> QueryCriteriaQueryVarianceList { get; set; }

    /// <summary>
    /// 是否 确定并继续操作.
    /// </summary>
    public bool HasConfirmAndAddBtn { get; set; }

    /// <summary>
    /// 确定并继续操作 文本.
    /// </summary>
    public string ConfirmAndAddText { get; set; }
}