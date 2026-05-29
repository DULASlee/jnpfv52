using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Extend.Entitys;

/// <summary>
/// 职员信息
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01 .
/// </summary>
[SugarTable("EXT_EMPLOYEE")]
public class EmployeeEntity : CLDSEntityBase
{
    /// <summary>
    /// 工号.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string? EnCode { get; set; }

    /// <summary>
    /// 姓名.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string? FullName { get; set; }

    /// <summary>
    /// 性别.
    /// </summary>
    [SugarColumn(ColumnName = "F_GENDER")]
    public string? Gender { get; set; }

    /// <summary>
    /// 部门.
    /// </summary>
    [SugarColumn(ColumnName = "F_DEPARTMENT_NAME")]
    public string? DepartmentName { get; set; }

    /// <summary>
    /// 职位.
    /// </summary>
    [SugarColumn(ColumnName = "F_POSITION_NAME")]
    public string? PositionName { get; set; }

    /// <summary>
    /// 用工性质.
    /// </summary>
    [SugarColumn(ColumnName = "F_WORKING_NATURE")]
    public string? WorkingNature { get; set; }

    /// <summary>
    /// 身份证号.
    /// </summary>
    [SugarColumn(ColumnName = "F_ID_NUMBER")]
    public string? IdNumber { get; set; }

    /// <summary>
    /// 联系电话.
    /// </summary>
    [SugarColumn(ColumnName = "F_TELEPHONE")]
    public string? Telephone { get; set; }

    /// <summary>
    /// 参加工作.
    /// </summary>
    [SugarColumn(ColumnName = "F_ATTEND_WORK_TIME")]
    public DateTime? AttendWorkTime { get; set; }

    /// <summary>
    /// 出生年月.
    /// </summary>
    [SugarColumn(ColumnName = "F_BIRTHDAY")]
    public DateTime? Birthday { get; set; }

    /// <summary>
    /// 最高学历.
    /// </summary>
    [SugarColumn(ColumnName = "F_EDUCATION")]
    public string? Education { get; set; }

    /// <summary>
    /// 所学专业.
    /// </summary>
    [SugarColumn(ColumnName = "F_MAJOR")]
    public string? Major { get; set; }

    /// <summary>
    /// 毕业院校.
    /// </summary>
    [SugarColumn(ColumnName = "F_GRADUATION_ACADEMY")]
    public string? GraduationAcademy { get; set; }

    /// <summary>
    /// 毕业时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_GRADUATION_TIME")]
    public DateTime? GraduationTime { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }
}
