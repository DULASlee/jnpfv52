using JNPF.Common.CodeGenUpload;
using SqlSugar;

namespace JNPF.ZxDev.Entitys;



public class SystemAppCheckModel
{
    /// <summary>
    /// 主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_Id", IsPrimaryKey = true)]
    public string? Id { get; set; }

    /// <summary>
    /// 租户Id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TenantId")]
    public string? TenantId { get; set; }

    /// <summary>
    /// 用户Id.
    /// </summary>
    [SugarColumn(ColumnName = "F_AccountId")]
    public string? AccountId { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_CreatorTime")]
    public DateTime? CreatorTime { get; set; }

    /// <summary>
    /// 系统应用Id.
    /// </summary>
    [SugarColumn(ColumnName = "F_SystemId")]
    public string? SystemId { get; set; }

    /// <summary>
    /// 系统应用名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_SystemName")]
    public string? SystemName { get; set; }

    /// <summary>
    /// 申请说明.
    /// </summary>
    [SugarColumn(ColumnName = "F_Comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// 接口地址.
    /// </summary>
    [SugarColumn(ColumnName = "F_InterfaceId")]
    public string? InterfaceId { get; set; }

    /// <summary>
    /// 框架版本.
    /// </summary>
    [SugarColumn(ColumnName = "F_FrameworkId")]
    public string? FrameworkId { get; set; }

    /// <summary>
    /// 业务数据库.
    /// </summary>
    [SugarColumn(ColumnName = "F_AppDbConfig")]
    public string? AppDbConfig { get; set; }

    /// <summary>
    /// 框架数据库.
    /// </summary>
    [SugarColumn(ColumnName = "F_FwDbConfig")]
    public string? FwDbConfig { get; set; }

    /// <summary>
    /// 审核状态.
    /// </summary>
    [SugarColumn(ColumnName = "F_Status")]
    public int? Status { get; set; }


    /// <summary>
    /// 集成助手数据标识.
    /// </summary>
    [SugarColumn(ColumnName = "f_inte_assistant")]
    public int? InteAssistant { get; set; }

}