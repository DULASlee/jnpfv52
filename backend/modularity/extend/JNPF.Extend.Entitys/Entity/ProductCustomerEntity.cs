using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Extend.Entitys.Entity;

/// <summary>
/// 客户信息.
/// </summary>
[SugarTable("EXT_CUSTOMER", TableDescription = "客户信息")]
public class ProductCustomerEntity : CLDEntityBase
{
    /// <summary>
    /// 编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 客户名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_CUSTOMER_NAME")]
    public string Customername { get; set; }

    /// <summary>
    /// 地址.
    /// </summary>
    [SugarColumn(ColumnName = "F_ADDRESS")]
    public string Address { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 联系方式.
    /// </summary>
    [SugarColumn(ColumnName = "F_CONTACT_TEL")]
    public string ContactTel { get; set; }
}