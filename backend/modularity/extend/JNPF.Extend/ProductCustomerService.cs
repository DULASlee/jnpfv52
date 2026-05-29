using JNPF.Common.Core.Manager;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extend.Entitys.Dto.Customer;
using JNPF.Extend.Entitys.Entity;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Extend;

/// <summary>
/// 客户信息.
/// </summary>
[ApiDescriptionSettings(Tag = "Extend", Name = "Customer", Order = 600)]
[Route("api/extend/saleOrder/[controller]")]
public class ProductCustomerService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<ProductCustomerEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="ProductCustomerService"/>类型的新实例.
    /// </summary>
    public ProductCustomerService(
        ISqlSugarRepository<ProductCustomerEntity> repository,
        IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 客户列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] PageInputBase input)
    {
        var data = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), x => x.FullName.Contains(input.keyword))
             .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), a => a.LastModifyTime, OrderByType.Desc)
            .Select(x => new ProductCustomerListOutput
            {
                id = x.Id,
                code = x.EnCode,
                name = x.FullName,
                customerName = x.Customername,
                address = x.Address,
                contactTel = x.ContactTel
            })
            .ToListAsync();

        return new { list = data };
    }

    #endregion
}