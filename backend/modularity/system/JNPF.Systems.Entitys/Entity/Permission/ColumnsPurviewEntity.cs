using JNPF.Common.Contracts;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using SqlSugar;

namespace JNPF.Systems.Entitys.Permission;

/// <summary>
/// 模块列表权限
/// 版 本：V3.3
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2022-03-15.
/// </summary>
[SugarTable("BASE_COLUMNS_PURVIEW")]
public class ColumnsPurviewEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 模块ID.
    /// </summary>
    [SugarColumn(ColumnName = "F_MODULE_ID")]
    public string ModuleId { get; set; }

    /// <summary>
    /// 列表字段数组.
    /// </summary>
    [SugarColumn(ColumnName = "F_FIELD_LIST")]
    public string FieldList { get; set; }
}