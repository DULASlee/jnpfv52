using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.Entity.System;

/// <summary>
/// 数据接口变量
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_DATA_INTERFACE_VARIATE")]
public class DataInterfaceVariateEntity : CLDEntityBase
{
    /// <summary>
    /// 数据接口id.
    /// </summary>
    [SugarColumn(ColumnName = "F_INTERFACE_ID")]
    public string InterfaceId { get; set; }

    /// <summary>
    /// 变量名.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 表达式.
    /// </summary>
    [SugarColumn(ColumnName = "F_EXPRESSION")]
    public string Expression { get; set; }

    /// <summary>
    /// 变量值.
    /// </summary>
    [SugarColumn(ColumnName = "F_VALUE")]
    public string Value { get; set; }
}
