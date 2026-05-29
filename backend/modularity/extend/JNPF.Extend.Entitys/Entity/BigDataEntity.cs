using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Extend.Entitys;

/// <summary>
/// 大数据测试
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("EXT_BIG_DATA")]
public class BigDataEntity : CLDEntityBase
{
    /// <summary>
    /// 编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string? EnCode { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string? FullName { get; set; }
}
