using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.System.CommonWords;
using JNPF.Systems.Entitys.Entity.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems.System;

/// <summary>
/// 常用语.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "CommonWords", Order = 200)]
[Route("api/system/[controller]")]
public class CommonWordsService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<CommonWordsEntity> _repository;
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="ModuleService"/>类型的新实例.
    /// </summary>
    public CommonWordsService(
        ISqlSugarRepository<CommonWordsEntity> repository,
        IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region Get

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] CommonWordsListInput input)
    {
        var list = await _repository.AsSugarClient().Queryable<CommonWordsEntity>()
            .Where(a => a.CommonWordsType == 0 && a.DeleteMark == null)
            .Select(a => new CommonWordsOutput()
            {
                id = a.Id,
                commonWordsText = a.CommonWordsText,
                sortCode = a.SortCode,
                enabledMark = a.EnabledMark,
                creatorTime = a.CreatorTime,
                lastModifyTime = a.LastModifyTime,
            }).MergeTable()
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.commonWordsText.Contains(input.keyword))
            .WhereIF(input.enabledMark.IsNotEmptyOrNull(), a => a.enabledMark.Equals(input.enabledMark))
            .OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), a => a.lastModifyTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<CommonWordsOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 获取信息.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var data = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        return data.Adapt<CommonWordsOutput>();
    }

    /// <summary>
    /// 下拉列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelector()
    {
        var list = await _repository.AsSugarClient().Queryable<CommonWordsEntity>()
            .Where(a => (a.CommonWordsType == 0 || a.CreatorUserId == _userManager.UserId) && a.EnabledMark == 1 && a.DeleteMark == null)
            .OrderBy(a => a.CommonWordsType, OrderByType.Desc).OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .Select(a => new CommonWordsOutput()
            {
                id = a.Id,
                commonWordsText = a.CommonWordsText,
                sortCode = a.SortCode,
                enabledMark = a.EnabledMark,
                commonWordsType = a.CommonWordsType,
            }).ToListAsync();
        return new { list = list };
    }
    #endregion

    #region Post

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] CommonWordsInput input)
    {
        var entity = input.Adapt<CommonWordsEntity>();
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] CommonWordsInput input)
    {
        var entity = input.Adapt<CommonWordsEntity>();
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (entity == null)
            throw Oops.Oh(ErrorCode.COM1005);
        var isOk = await _repository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    #endregion
}
