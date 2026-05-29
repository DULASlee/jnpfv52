using JNPF.Common.Contracts;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using Spire.Xls;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 字典分类
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_DICTIONARY_TYPE")]
public class DictionaryTypeEntity : FwCLDSEntityBase,IZxDataTypeFilter
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
    /// 树形.
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_TREE")]
    public int? IsTree { get; set; }

    /// <summary>
    /// 类型(1:系统,0:业务).
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int? Type { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }



    [SugarColumn(ColumnName = "F_Zx_DataType")]
    public int ZxDataType { get; set; }
}