using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 高级查询方案
/// 版 本：V3.4
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2022-06-06.
/// </summary>
[SugarTable("BASE_ADVANCED_QUERY_SCHEME")]
public class AdvancedQuerySchemeEntity : CLDEntityBase
{
    /// <summary>
    /// 方案名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 匹配逻辑.
    /// </summary>
    [SugarColumn(ColumnName = "F_MATCH_LOGIC")]
    public string MatchLogic { get; set; }

    /// <summary>
    /// 条件规则Json.
    /// </summary>
    [SugarColumn(ColumnName = "F_CONDITION_JSON")]
    public string ConditionJson { get; set; }

    /// <summary>
    /// 菜单主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_MODULE_ID")]
    public string ModuleId { get; set; }
}