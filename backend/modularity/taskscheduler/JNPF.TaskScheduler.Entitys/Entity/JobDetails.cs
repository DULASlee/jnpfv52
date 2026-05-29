using JNPF.TaskScheduler.Entitys.Enum;
using SqlSugar;

namespace JNPF.TaskScheduler.Entitys;

/// <summary>
/// 作业信息表.
/// </summary>
[SugarTable("JobDetails", "作业信息表")]
[Tenant("JNPF-Job")]
public class JobDetails
{
    /// <summary>
    /// Id.
    /// </summary>
    [SugarColumn(ColumnDescription = "Id", IsPrimaryKey = true, IsIdentity = true)]
    public virtual long Id { get; set; }

    /// <summary>
    /// 作业 Id.
    /// </summary>
    [SugarColumn(ColumnDescription = "作业Id")]
    public virtual string JobId { get; set; }

    /// <summary>
    /// 组名称.
    /// </summary>
    [SugarColumn(ColumnDescription = "组名称")]
    public string? GroupName { get; set; }

    /// <summary>
    /// 作业类型 FullName.
    /// </summary>
    [SugarColumn(ColumnDescription = "作业类型")]
    public string? JobType { get; set; }

    /// <summary>
    /// 程序集 Name.
    /// </summary>
    [SugarColumn(ColumnDescription = "程序集")]
    public string? AssemblyName { get; set; }

    /// <summary>
    /// 描述信息.
    /// </summary>
    [SugarColumn(ColumnDescription = "描述信息")]
    public string? Description { get; set; }

    /// <summary>
    /// 是否并行执行.
    /// </summary>
    [SugarColumn(ColumnDescription = "是否并行执行")]
    public bool Concurrent { get; set; } = true;

    /// <summary>
    /// 是否扫描特性触发器.
    /// </summary>
    [SugarColumn(ColumnDescription = "是否扫描特性触发器")]
    public bool IncludeAnnotations { get; set; } = false;

    /// <summary>
    /// 额外数据.
    /// </summary>
    [SugarColumn(ColumnDescription = "额外数据", ColumnDataType = "longtext,text,clob")]
    public string? Properties { get; set; } = "{}";

    /// <summary>
    /// 更新时间.
    /// </summary>
    [SugarColumn(ColumnDescription = "更新时间")]
    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 作业创建类型.
    /// </summary>
    [SugarColumn(ColumnDescription = "作业创建类型")]
    public RequestTypeEnum CreateType { get; set; } = RequestTypeEnum.BuiltIn;

    /// <summary>
    /// 脚本代码.
    /// </summary>
    [SugarColumn(ColumnDescription = "脚本代码", ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string? ScriptCode { get; set; }

    /// <summary>
    /// 租户id.
    /// </summary>
    [SugarColumn(ColumnDescription = "租户ID")]
    public string? TenantId { get; set; }
}