using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 第三方工具对象同步表
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_SYN_THIRD_INFO")]
public class SynThirdInfoEntity : CLDSEntityBase
{
    /// <summary>
    /// 第三方类型(1:企业微信;2:钉钉).
    /// </summary>
    [SugarColumn(ColumnName = "F_THIRD_TYPE")]
    public int? ThirdType { get; set; }

    /// <summary>
    /// 数据类型(1:公司;2:部门;3:用户).
    /// </summary>
    [SugarColumn(ColumnName = "F_DATA_TYPE")]
    public int? DataType { get; set; }

    /// <summary>
    /// 系统对象ID.
    /// </summary>
    [SugarColumn(ColumnName = "F_SYS_OBJ_ID")]
    public string SysObjId { get; set; }

    /// <summary>
    /// 第三对象ID.
    /// </summary>
    [SugarColumn(ColumnName = "F_THIRD_OBJ_ID")]
    public string ThirdObjId { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }
}