using JNPF.Common.Core.Manager;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extend.Entitys;
using JNPF.Extend.Entitys.Dto.ProductGoods;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Extend;

/// <summary>
/// 选择产品.
/// </summary>
[ApiDescriptionSettings(Tag = "Extend", Name = "Goods", Order = 600)]
[Route("api/extend/saleOrder/[controller]")]
public class ProductGoodsService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<ProductGoodsEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="ProductGoodsService"/>类型的新实例.
    /// </summary>
    public ProductGoodsService(
        ISqlSugarRepository<ProductGoodsEntity> repository,
        IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 产品列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] ProductGoodsListQueryInput input)
    {
        var data = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null)
            .WhereIF(!string.IsNullOrEmpty(input.classifyId), it => it.ClassifyId.Equals(input.classifyId))
            .WhereIF(!string.IsNullOrEmpty(input.code), it => it.EnCode.Contains(input.code))
            .WhereIF(!string.IsNullOrEmpty(input.fullName), it => it.FullName.Contains(input.fullName))
            .Select(it => new ProductGoodsListOutput
            {
                id = it.Id,
                code = it.EnCode,
                fullName = it.FullName,
                qty = it.Qty,
            }).MergeTable().OrderByIF(string.IsNullOrEmpty(input.sidx), it => it.id).OrderByIF(!string.IsNullOrEmpty(input.sidx), input.sidx + " " + input.sort).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<ProductGoodsListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 商品编码.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelectorList([FromQuery] PageInputBase input)
    {
        var data = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), x => x.EnCode.Contains(input.keyword))
            .Select(it => new ProductGoodsListOutput
            {
                id = it.Id,
                classifyId = it.ClassifyId,
                code = it.EnCode,
                fullName = it.FullName,
                qty = it.Qty,
                type = it.Type,
                amount = it.Amount,
                money = it.Money,
            }).ToListAsync();
        return new { list = data };
    }

    #endregion
}