using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.VisualDev.Entitys;

/// <summary>
/// 可视化开发功能实体.
/// </summary>
[SugarTable("BASE_VISUAL_DEV")]
public class VisualDevEntity : CLDSEntityBase
{
    /// <summary>
    /// 名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 状态(0-未发步，1-已发布，2-已修改).
    /// </summary>
    [SugarColumn(ColumnName = "F_STATE")]
    public int State { get; set; } = 0;

    /// <summary>
    /// 类型
    /// 1-Web设计,3-流程表单,4-Web表单.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int Type { get; set; }

    /// <summary>
    /// 关联的表.
    /// </summary>
    [SugarColumn(ColumnName = "F_TABLES_DATA")]
    public string Tables { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    [SugarColumn(ColumnName = "F_CATEGORY")]
    public string Category { get; set; }

    /// <summary>
    /// 表单配置JSON.
    /// </summary>
    [SugarColumn(ColumnName = "F_FORM_DATA")]
    public string FormData { get; set; }

    /// <summary>
    /// 列表配置JSON.
    /// </summary>
    [SugarColumn(ColumnName = "F_COLUMN_DATA")]
    public string ColumnData { get; set; }

    /// <summary>
    /// 描述或说明.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 关联数据连接id.
    /// </summary>
    [SugarColumn(ColumnName = "F_DB_LINK_ID")]
    public string DbLinkId { get; set; }

    /// <summary>
    /// 页面类型（1、纯表单，2、表单加列表，3、系统表单，4、数据视图）.
    /// </summary>
    [SugarColumn(ColumnName = "F_WEB_TYPE")]
    public int WebType { get; set; } = 2;

    /// <summary>
    /// 工作流引擎ID.
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public string FlowId { get; set; }

    /// <summary>
    /// App列表配置JSON.
    /// </summary>
    [SugarColumn(ColumnName = "F_APP_COLUMN_DATA")]
    public string AppColumnData { get; set; }

    /// <summary>
    /// 是否启用流程.
    /// </summary>
    [SugarColumn(ColumnName = "F_ENABLE_FLOW")]
    public int EnableFlow { get; set; }

    /// <summary>
    /// 接口id.
    /// </summary>
    [SugarColumn(ColumnName = "F_INTERFACE_ID")]
    public string InterfaceId { get; set; }

    /// <summary>
    /// 接口参数.
    /// </summary>
    [SugarColumn(ColumnName = "F_INTERFACE_PARAM")]
    public string InterfaceParam { get; set; }

    /// <summary>
    /// 发布选中平台.
    /// </summary>
    [SugarColumn(ColumnName = "F_PLATFORM_RELEASE")]
    public string PlatformRelease { get; set; }

    /// <summary>
    /// 是否外链(虚拟字段).
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public bool isShortLink { get; set; } = false;


    /// <summary>
    /// 关联历史版本，存储原始流程Id
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_ID")]
    public string ParentId { get; set; }
}