using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 数据权限连接管理
/// 版 本：V3.0.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2017.09.20.
/// </summary>
[SugarTable("BASE_MODULE_LINK")]
[Tenant(ClaimConst.TENANTID)]
public class ModuleDataAuthorizeLinkEntity : CLDEntityBase
{
    /// <summary>
    /// 数据源连接主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_LINK_ID")]
    public string LinkId { get; set; }

    /// <summary>
    /// 表名.
    /// </summary>
    [SugarColumn(ColumnName = "F_LINK_TABLES")]
    public string LinkTables { get; set; }

    /// <summary>
    /// 权限类型(1:列表权限，2：数据权限，3：表单权限).
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int? Type { get; set; }

    /// <summary>
    /// 菜单主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_MODULE_ID")]
    public string ModuleId { get; set; }
}