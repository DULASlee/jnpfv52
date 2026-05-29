using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extend.Entitys;
using JNPF.Extend.Entitys.Dto.Product;
using JNPF.Extend.Entitys.Dto.ProductEntry;
using JNPF.Extend.Entitys.Model;
using JNPF.FriendlyException;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Extend;

/// <summary>
/// 业务实现：订单示例.
/// </summary>
[ApiDescriptionSettings(Tag = "Extend", Name = "Product", Order = 200)]
[Route("api/extend/saleOrder/[controller]")]
public class ProductService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<ProductEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 单据规则服务.
    /// </summary>
    private readonly IBillRullService _billRullService;

    /// <summary>
    /// 初始化一个<see cref="ProductService"/>类型的新实例.
    /// </summary>
    public ProductService(
        ISqlSugarRepository<ProductEntity> repository,
        IBillRullService billRullService,
        IUserManager userManager)
    {
        _repository = repository;
        _billRullService = billRullService;
        _userManager = userManager;
    }

    #region Get

    /// <summary>
    /// 获取订单示例.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        return (await _repository.AsQueryable()
            .Includes(x => x.productEntryList.Where(it => it.DeleteMark == null).Select(it => new ProductEntryEntity
            {
                ProductCode = it.ProductCode,
                ProductName = it.ProductName,
                ProductSpecification = it.ProductSpecification,
                Qty = it.Qty,
                Type = it.Type,
                Money = it.Money,
                Price = it.Price,
                Amount = it.Amount,
                Description = it.Description
            }).ToList())
            .Where(it => it.Id.Equals(id) && it.DeleteMark == null)
            .ToListAsync(it => new ProductInfoOutput
            {
                id = it.Id,
                code = it.EnCode,
                customerName = it.CustomerName,
                customerId = it.CustomerId,
                auditName = it.AuditName,
                auditDate = it.AuditDate,
                goodsWarehouse = it.GoodsWarehouse,
                goodsDate = it.GoodsDate,
                gatheringType = it.GatheringType,
                business = it.Business,
                address = it.Address,
                contactTel = it.ContactTel,
                harvestMsg = it.HarvestMsg,
                harvestWarehouse = it.HarvestWarehouse,
                issuingName = it.IssuingName,
                partPrice = it.PartPrice,
                reducedPrice = it.ReducedPrice,
                discountPrice = it.DiscountPrice,
                description = it.Description,
                productEntryList = it.productEntryList.Adapt<List<ProductEntryInfoOutput>>()
            }))?.FirstOrDefault();
    }

    /// <summary>
    /// 获取全订单示例.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("Goods")]
    public async Task<dynamic> GetAllProductEntryList([FromQuery] ProductListQueryInput input)
    {
        var data = await _repository.AsQueryable()
            .Includes(x => x.productEntryList.Where(it => it.DeleteMark == null).Select(it => new ProductEntryEntity
            {
                ProductCode = it.ProductCode,
                ProductName = it.ProductName,
                ProductSpecification = it.ProductSpecification,
                Qty = it.Qty,
                Type = it.Type,
                Money = it.Money,
                Price = it.Price,
                Amount = it.Amount,
                Description = it.Description,
                Activity = it.Activity
            }).ToList())
            .Where(it => it.DeleteMark == null)
            .WhereIF(!string.IsNullOrEmpty(input.code), it => it.EnCode.Contains(input.code))
            .WhereIF(!string.IsNullOrEmpty(input.customerName), it => it.CustomerName.Contains(input.customerName))
            .OrderByIF(string.IsNullOrEmpty(input.sidx), it => it.Id)
            .OrderByIF(!string.IsNullOrEmpty(input.sidx), input.sidx + " " + input.sort)
            .ToPagedListAsync(input.currentPage, input.pageSize, it => new ProductListOutput
            {
                id = it.Id,
                code = it.EnCode,
                customerName = it.CustomerName,
                business = it.Business,
                address = it.Address,
                contactTel = it.ContactTel,
                salesmanName = it.SalesmanName,
                auditState = it.AuditState,
                goodsState = it.GoodsState,
                closeState = it.CloseState,
                closeDate = it.CloseDate,
                contactName = it.ContactName,
                productEntryList = it.productEntryList.Adapt<List<ProductEntryEntity>>()
            });
        return PageResult<ProductListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 获取订单示例列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] ProductListQueryInput input)
    {
        if (input.auditState == "0")
            input.auditState = null;
        if (input.closeState == "0")
            input.closeState = null;
        var data = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null)
            .WhereIF(!string.IsNullOrEmpty(input.code), it => it.EnCode.Contains(input.code))
            .WhereIF(!string.IsNullOrEmpty(input.customerName), it => it.CustomerName.Contains(input.customerName))
            .WhereIF(!string.IsNullOrEmpty(input.contactTel), it => it.ContactTel.Contains(input.contactTel))
            .WhereIF(!string.IsNullOrEmpty(input.auditState), it => it.AuditState.Equals(input.auditState))
            .WhereIF(!string.IsNullOrEmpty(input.closeState), it => it.CloseState.Equals(input.closeState))
            .OrderByIF(string.IsNullOrEmpty(input.sidx), it => it.Id).OrderByIF(!string.IsNullOrEmpty(input.sidx), input.sidx + " " + input.sort)
            .Select(it => new ProductListOutput
            {
                id = it.Id,
                code = it.EnCode,
                customerName = it.CustomerName,
                business = it.Business,
                address = it.Address,
                contactTel = it.ContactTel,
                salesmanName = it.SalesmanName,
                auditState = it.AuditState,
                goodsState = it.GoodsState,
                closeState = it.CloseState,
                closeDate = it.CloseDate,
                contactName = it.ContactName
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<ProductListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 获取订单示例列表.
    /// </summary>
    /// <param name="id">请求参数.</param>
    /// <returns></returns>
    [HttpGet("ProductEntry/{id}")]
    public async Task<dynamic> GetProductEntryList(string id)
    {
        string data = "[{\"id\":\"37c995b4044541009fb7e285bcf9845d\",\"productSpecification\":\"120ml\",\"qty\":16,\"money\":510,\"price\":120,\"commandType\":\"唯一码\",\"util\":\"盒\"},{\"id\":\"2dbb11d3cde04c299985ac944d130ba0\",\"productSpecification\":\"150ml\",\"qty\":15,\"money\":520,\"price\":310,\"commandType\":\"唯一码\",\"util\":\"盒\"},{\"id\":\"f8ec261ccdf045e5a2e1f0e5485cda76\",\"productSpecification\":\"40ml\",\"qty\":13,\"money\":530,\"price\":140,\"commandType\":\"唯一码\",\"util\":\"盒\"},{\"id\":\"6c110b57ae56445faa8ce9be501c8997\",\"productSpecification\":\"103ml\",\"qty\":2,\"money\":504,\"price\":150,\"commandType\":\"唯一码\",\"util\":\"盒\"},{\"id\":\"f2ee981aaf934147a4d090a0eed2203f\",\"productSpecification\":\"120ml\",\"qty\":21,\"money\":550,\"price\":160,\"commandType\":\"唯一码\",\"util\":\"盒\"}]";
        List<ProductEntryMdoel> dataAll = data.ToObject<List<ProductEntryMdoel>>();
        List<ProductEntryListOutput> productEntryList = await _repository.AsSugarClient().Queryable<ProductEntryEntity>().Where(it => it.ProductId.Equals(id) && it.DeleteMark == null).Select(it => new ProductEntryListOutput
        {
            productCode = it.ProductCode,
            productName = it.ProductName,
            qty = it.Qty,
            type = it.Type,
            activity = it.Activity,
        }).ToListAsync();

        productEntryList.ForEach(item =>
        {
            item.dataList = new List<ProductEntryMdoel>();
            List<ProductEntryMdoel> dataList = new List<ProductEntryMdoel>();
            var randomData = new Random();
            int num = randomData.Next(1, dataAll.Count);
            for (int i = 0; i < num; i++)
            {
                dataList.Add(dataAll[i]);
            }

            item.dataList = dataList;
        });

        return new { list = productEntryList };
    }

    #endregion

    #region POST

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    [UnitOfWork]
    public async Task Create([FromBody] ProductCrInput input)
    {
        var entity = input.Adapt<ProductEntity>();
        entity.EnCode = await _billRullService.GetBillNumber("OrderNumber", false);
        entity.Id = SnowflakeIdHelper.NextId();
        entity.CreatorTime = DateTime.Now;
        entity.CreatorUserId = _userManager.UserId;

        var productEntryList = input.productEntryList.Adapt<List<ProductEntryEntity>>();
        if (productEntryList != null)
        {
            productEntryList.ForEach(item =>
            {
                item.Id = SnowflakeIdHelper.NextId();
                item.ProductId = entity.Id;
                item.CreatorTime = DateTime.Now;
                item.CreatorUserId = _userManager.UserId;
            });
            entity.productEntryList = productEntryList;
        }

        var isOk = await _repository.AsSugarClient().InsertNav(entity)
            .Include(it => it.productEntryList).ExecuteCommandAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 更新订单示例.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    [UnitOfWork]
    public async Task Update(string id, [FromBody] ProductUpInput input)
    {
        var entity = input.Adapt<ProductEntity>();
        entity.LastModifyTime = DateTime.Now;
        entity.LastModifyUserId = _userManager.UserId;

        await _repository.AsSugarClient().Updateable<ProductEntryEntity>()
            .Where(it => it.ProductId.Equals(entity.Id) && !input.productEntryList.Select(a => a.id).ToList().Contains(it.Id))
            .SetColumns(it => new ProductEntryEntity()
            {
                DeleteMark = 1,
                DeleteUserId = _userManager.UserId,
                DeleteTime = SqlFunc.GetDate()
            }).ExecuteCommandAsync();

        var productEntryList = input.productEntryList.Adapt<List<ProductEntryEntity>>();
        productEntryList.ForEach(item =>
        {
            item.Id = item.Id == null ? SnowflakeIdHelper.NextId() : item.Id;
            item.ProductId = entity.Id;
        });

        await _repository.AsSugarClient().Storageable(productEntryList).ExecuteCommandAsync();

        var isOk = await _repository.AsUpdateable(entity).UpdateColumns(it => new {
            it.EnCode,
            it.CustomerName,
            it.ContactTel,
            it.Address,
            it.GoodsWarehouse,
            it.Business,
            it.GatheringType,
            it.PartPrice,
            it.ReducedPrice,
            it.DiscountPrice,
            it.Description,
            it.LastModifyUserId,
            it.LastModifyTime
        }).ExecuteCommandAsync();
        if (!(isOk > 0))
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 删除订单示例.
    /// </summary>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [UnitOfWork]
    public async Task Delete(string id)
    {
        await _repository.AsSugarClient().Updateable<ProductEntryEntity>()
           .Where(it => it.ProductId.Equals(id))
           .SetColumns(it => new ProductEntryEntity()
           {
               DeleteMark = 1,
               DeleteUserId = _userManager.UserId,
               DeleteTime = SqlFunc.GetDate()
           }).ExecuteCommandAsync();

        var isOk = await _repository.AsUpdateable()
            .Where(it => it.Id.Equals(id))
            .SetColumns(it => new ProductEntity()
            {
                DeleteMark = 1,
                DeleteUserId = _userManager.UserId,
                DeleteTime = SqlFunc.GetDate()
            }).ExecuteCommandAsync();
        if (!(isOk > 0))
            throw Oops.Oh(ErrorCode.COM1002);
    }

    #endregion

}