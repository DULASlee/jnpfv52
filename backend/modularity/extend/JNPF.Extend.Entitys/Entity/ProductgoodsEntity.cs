using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Extend.Entitys;

/// <summary>
/// 产品商品.
/// </summary>
[SugarTable("EXT_PRODUCT_GOODS")]
[Tenant(ClaimConst.TENANTID)]
public class ProductGoodsEntity : CLDEntityBase
{
    /// <summary>
    /// 分类主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_CLASSIFY_ID")]
    public string ClassifyId { get; set; }

    /// <summary>
    /// 产品编号.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 产品名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 订货类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public string Type { get; set; }

    /// <summary>
    /// 产品规格.
    /// </summary>
    [SugarColumn(ColumnName = "F_PRODUCTSPECIFICATION")]
    public string ProductSpecification { get; set; }

    /// <summary>
    /// 单价.
    /// </summary>
    [SugarColumn(ColumnName = "F_MONEY")]
    public string Money { get; set; }

    /// <summary>
    /// 库存数.
    /// </summary>
    [SugarColumn(ColumnName = "F_QTY")]
    public int Qty { get; set; }

    /// <summary>
    /// 金额.
    /// </summary>
    [SugarColumn(ColumnName = "F_AMOUNT")]
    public string Amount { get; set; }
}