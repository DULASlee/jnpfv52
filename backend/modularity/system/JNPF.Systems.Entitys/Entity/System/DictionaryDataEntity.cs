using JNPF.Common.Contracts;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 字典数据
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_DICTIONARY_DATA")]
public class DictionaryDataEntity : FwCLDSEntityBase,IZxDataTypeFilter
{
    /// <summary>
    /// 上级.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_ID")]
    public string ParentId { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 拼音.
    /// </summary>
    [SugarColumn(ColumnName = "F_SIMPLE_SPELLING")]
    public string SimpleSpelling { get; set; }

    /// <summary>
    /// 默认.
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_DEFAULT")]
    public int? IsDefault { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 类别主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_DICTIONARY_TYPE_ID")]
    public string DictionaryTypeId { get; set; }

    [SugarColumn(ColumnName = "F_Zx_DataType")]
    public int ZxDataType { get; set; }
}