using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Apps.Entitys;

/// <summary>
/// App常用数据
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_APP_DATA")]
public class AppDataEntity : CLDSEntityBase
{
    /// <summary>
    /// 对象类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_OBJECT_TYPE")]
    public string ObjectType { get; set; }

    /// <summary>
    /// 对象主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_OBJECT_ID")]
    public string ObjectId { get; set; }

    /// <summary>
    /// 对象json.
    /// </summary>
    [SugarColumn(ColumnName = "F_OBJECT_DATA")]
    public string ObjectData { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }
}