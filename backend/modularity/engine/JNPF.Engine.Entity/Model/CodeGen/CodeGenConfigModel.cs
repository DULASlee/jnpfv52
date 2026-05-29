using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.CodeGen;

/// <summary>
/// 代码生成详细配置参数.
/// </summary>
[SuppressSniffer]
public class CodeGenConfigModel
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// 业务名.
    /// </summary>
    public string BusName { get; set; }

    /// <summary>
    /// 命名空间.
    /// </summary>
    public string NameSpace { get; set; }

    /// <summary>
    /// 类型名称.
    /// </summary>
    public string ClassName { get; set; }

    /// <summary>
    /// 主键.
    /// </summary>
    public string PrimaryKey { get; set; }

    /// <summary>
    /// 主键(首字母小写).
    /// </summary>
    public string LowerPrimaryKey => string.IsNullOrWhiteSpace(PrimaryKey) ? null : PrimaryKey.Substring(0, 1).ToLower() + PrimaryKey[1..];

    /// <summary>
    /// 原始主键.
    /// </summary>
    public string OriginalPrimaryKey { get; set; }

    /// <summary>
    /// 主键策略.
    /// </summary>
    public int PrimaryKeyPolicy { get; set; }

    /// <summary>
    /// 主表.
    /// </summary>
    public string MainTable { get; set; }

    /// <summary>
    /// 原本名称.
    /// </summary>
    public string OriginalMainTableName { get; set; }

    /// <summary>
    /// 主表(首字母小写).
    /// </summary>
    public string LowerMainTable => string.IsNullOrWhiteSpace(MainTable) ? null : MainTable.Substring(0, 1).ToLower() + MainTable[1..];

    /// <summary>
    /// 服务列表.
    /// </summary>
    public List<string> ServiceList { get; set; }

    /// <summary>
    /// 列表分页.
    /// </summary>
    public bool hasPage { get; set; }

    /// <summary>
    /// 功能列表.
    /// </summary>
    public List<CodeGenFunctionModel> Function { get; set; }

    /// <summary>
    /// 表字段.
    /// </summary>
    public List<TableColumnConfigModel> TableField { get; set; }

    /// <summary>
    /// 关联字段.
    /// </summary>
    public List<TableColumnConfigModel> RelationsField { get; set; }

    /// <summary>
    /// 表关系.
    /// </summary>
    public List<CodeGenTableRelationsModel> TableRelations { get; set; }

    /// <summary>
    /// 默认排序.
    /// </summary>
    public string DefaultSidx { get; set; }

    /// <summary>
    /// 是否存在单据规则控件.
    /// </summary>
    public bool IsBillRule { get; set; }

    /// <summary>
    /// 是否导出.
    /// </summary>
    public bool IsExport { get; set; }

    /// <summary>
    /// 是否批量删除.
    /// </summary>
    public bool IsBatchRemove { get; set; }

    /// <summary>
    /// 是否有上传控件.
    /// </summary>
    public bool IsUpload { get; set; }

    /// <summary>
    /// 是否存在关系.
    /// </summary>
    public bool IsTableRelations { get; set; }

    /// <summary>
    /// 是否要生成对象映射.
    /// </summary>
    public bool IsMapper { get; set; }

    /// <summary>
    /// 是否主表.
    /// </summary>
    public bool IsMainTable { get; set; }

    /// <summary>
    /// 是否副表.
    /// </summary>
    public bool IsAuxiliaryTable { get; set; }

    /// <summary>
    /// 数据库连接ID.
    /// </summary>
    public string DbLinkId { get; set; }

    /// <summary>
    /// 生成表单ID.
    /// </summary>
    public string FormId { get; set; }

    /// <summary>
    /// 页面类型（1、纯表单，2、表单加列表，3、表单列表工作流）.
    /// </summary>
    public int WebType { get; set; }

    /// <summary>
    /// 页面类型（1-Web设计,2-App设计,3-流程表单,4-Web表单,5-App表单）.
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// 是否开启流程.
    /// </summary>
    public bool EnableFlow { get; set; }

    /// <summary>
    /// 模板编码.
    /// </summary>
    public string EnCode { get; set; }

    /// <summary>
    /// 是否开启数据权限.
    /// </summary>
    public bool UseDataPermission { get; set; }

    /// <summary>
    /// 查询类型为等于的控件数量.
    /// </summary>
    public int SearchControlNum { get; set; }

    /// <summary>
    /// 表关系模型.
    /// </summary>
    public List<CodeGenTableRelationsModel> AuxiliaryTable { get; set; }

    /// <summary>
    /// 导出字段.
    /// </summary>
    public string ExportField { get; set; }

    /// <summary>
    /// 联表数量.
    /// </summary>
    public int LeagueTableCount { get; set; }

    /// <summary>
    /// 是否数据转换.
    /// </summary>
    public bool IsConversion { get; set; }

    /// <summary>
    /// 是否数据转换.
    /// </summary>
    public bool IsDetailConversion { get; set; }

    /// <summary>
    /// 是否更新.
    /// </summary>
    public bool IsUpdate { get; set; }

    /// <summary>
    /// 是否存在子表数据转换.
    /// </summary>
    public bool IsChildConversion { get; set; }

    /// <summary>
    /// 开启高级查询.
    /// </summary>
    public bool HasSuperQuery { get; set; }

    /// <summary>
    /// 是否唯一.
    /// </summary>
    public bool IsUnique { get; set; }

    /// <summary>
    /// 并发锁.
    /// </summary>
    public bool ConcurrencyLock { get; set; }

    /// <summary>
    /// 是否展示子表字段.
    /// </summary>
    public bool IsShowSubTableField { get; set; }

    /// <summary>
    /// 分组字段名..
    /// </summary>
    public string GroupField { get; set; }

    /// <summary>
    /// 分组显示字段名..
    /// </summary>
    public string GroupShowField { get; set; }

    /// <summary>
    /// 是否导入数据.
    /// </summary>
    public bool IsImportData { get; set; }

    /// <summary>
    /// 导入数据类型.
    /// </summary>
    public string ImportDataType { get; set; }

    /// <summary>
    /// 是否存在系统控件.
    /// </summary>
    public bool IsSystemControl { get; set; }

    /// <summary>
    /// 是否查询条件多选.
    /// </summary>
    public bool IsSearchMultiple { get; set; }

    /// <summary>
    /// 需解析的控件类型 JnpfKeyConst @@ 需解析的字段集合（以,隔开）.
    /// </summary>
    public List<string[]> ParsJnpfKeyConstList { get; set; }

    /// <summary>
    /// 需解析的控件类型 JnpfKeyConst @@ 需解析的字段集合（以,隔开）详情页 （行内编辑的时候特殊处理）.
    /// </summary>
    public List<string[]> ParsJnpfKeyConstListDetails { get; set; }

    /// <summary>
    /// 是否树形表格.
    /// </summary>
    public bool IsTreeTable { get; set; }

    /// <summary>
    /// 树形表格-父级字段.
    /// </summary>
    public string ParentField { get; set; }

    /// <summary>
    /// 树形表格-显示字段.
    /// </summary>
    public string TreeShowField { get; set; }

    /// <summary>
    /// 是否开启逻辑删除.
    /// </summary>
    public bool IsLogicalDelete { get; set; }

    /// <summary>
    /// 表格类型
    /// 1-普通,2-左侧树,3-分组,4-行内编辑,5-树形.
    /// </summary>
    public int TableType { get; set; }
}