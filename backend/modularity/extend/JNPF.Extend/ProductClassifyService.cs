using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extend.Entitys;
using JNPF.Extend.Entitys.Dto.ProductClassify;
using JNPF.FriendlyException;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Extend;

/// <summary>
/// 产品分类.
/// </summary>
[ApiDescriptionSettings(Tag = "Extend", Name = "Classify", Order = 600)]
[Route("api/extend/saleOrder/[controller]")]
public class ProductClassifyService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<ProductClassifyEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="ProductClassifyService"/>类型的新实例.
    /// </summary>
    public ProductClassifyService(
        ISqlSugarRepository<ProductClassifyEntity> repository,
        IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList()
    {
        var data = await _repository.AsQueryable().Where(t => t.DeleteMark == null).OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .ToListAsync();
        List<ProductClassifyTreeOutput>? treeList = data.Adapt<List<ProductClassifyTreeOutput>>();
        return new { list = treeList.ToTree("-1") };
    }

    /// <summary>
    /// 获取订单示例.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        return (await _repository.AsQueryable().FirstAsync(a => a.Id.Equals(id) && a.DeleteMark == null)).Adapt<ProductClassifyInfoOutput>();
    }

    #endregion

    #region POST

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] ProductClassifyCrInput input)
    {
        var entity = input.Adapt<ProductClassifyEntity>();
        entity.Id = SnowflakeIdHelper.NextId();
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 更新订单示例.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] ProductClassifyUpInput input)
    {
        var entity = input.Adapt<ProductClassifyEntity>();
        entity.LastModifyTime = DateTime.Now;
        entity.LastModifyUserId = _userManager.UserId;
        var isOk = await _repository.AsUpdateable(entity).UpdateColumns(it => new {
            it.ParentId,
            it.FullName,
            it.LastModifyTime,
            it.LastModifyUserId
        }).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 删除订单示例.
    /// </summary>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var isOk = await _repository.AsDeleteable().Where(it => it.Id.Equals(id)).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1002);
    }

    #endregion
}