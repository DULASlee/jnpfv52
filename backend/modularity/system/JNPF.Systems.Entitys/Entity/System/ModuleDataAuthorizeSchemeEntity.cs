using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 数据权限方案.
/// </summary>
[SugarTable("BASE_MODULE_SCHEME")]
[Tenant(ClaimConst.TENANTID)]
public class ModuleDataAuthorizeSchemeEntity : CLDSEntityBase
{
    /// <summary>
    /// 方案编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 方案名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 条件规则Json.
    /// </summary>
    [SugarColumn(ColumnName = "F_CONDITION_JSON")]
    public string ConditionJson { get; set; }

    /// <summary>
    /// 条件规则描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_CONDITION_TEXT")]
    public string ConditionText { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 功能主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_MODULE_ID")]
    public string ModuleId { get; set; }

    /// <summary>
    /// 全部数据标识(1 标识全部).
    /// </summary>
    [SugarColumn(ColumnName = "F_ALL_DATA")]
    public int AllData { get; set; }

    /// <summary>
    /// 分组逻辑.
    /// </summary>
    [SugarColumn(ColumnName = "F_MATCH_LOGIC")]
    public string MatchLogic { get; set; }
}