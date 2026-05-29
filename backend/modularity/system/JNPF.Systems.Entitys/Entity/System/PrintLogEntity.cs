using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.Entity.System;

/// <summary>
/// 打印模板日志
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_PRINT_LOG")]
[Tenant(ClaimConst.TENANTID)]
public class PrintLogEntity : CLDEntityBase
{
    /// <summary>
    /// 打印条数.
    /// </summary>
    [SugarColumn(ColumnName = "F_PRINT_NUM")]
    public int? PrintNum { get; set; }

    /// <summary>
    /// 打印功能名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_PRINT_TITLE")]
    public string PrintTitle { get; set; }

    /// <summary>
    /// 打印模板id.
    /// </summary>
    [SugarColumn(ColumnName = "F_PRINT_ID")]
    public string PrintId { get; set; }
}
