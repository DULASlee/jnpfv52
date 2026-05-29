using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.Province;
using JNPF.Systems.Entitys.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems.Core.Province;

/// <summary>
/// 行政区划
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "Area", Order = 206)]
[Route("api/system/[controller]")]
public class ProvinceService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<ProvinceEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="ProvinceService"/>类型的新实例.
    /// </summary>
    public ProvinceService(
        ISqlSugarRepository<ProvinceEntity> repository,
        IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 获取行政区划列表.
    /// </summary>
    /// <param name="nodeid">节点Id.</param>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("{nodeId}")]
    public async Task<dynamic> GetList(string nodeid, [FromQuery] ProvinceListInput input)
    {
        var data = await _repository.AsQueryable().Where(m => m.ParentId == nodeid && m.DeleteMark == null)
            .WhereIF(!string.IsNullOrEmpty(input.keyword), t => t.EnCode.Contains(input.keyword) || t.FullName.Contains(input.keyword))
            .WhereIF(input.enabledMark.IsNotEmptyOrNull(), t => t.EnabledMark.Equals(input.enabledMark))
            .OrderBy(o => o.SortCode).OrderBy(t => t.CreatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), t => t.LastModifyTime, OrderByType.Desc).ToListAsync();
        var output = data.Adapt<List<ProvinceListOutput>>();
        foreach (var item in output)
        {
            var flag = await _repository.IsAnyAsync(m => m.ParentId == item.id && m.DeleteMark == null);
            item.isLeaf = !flag;
            item.hasChildren = flag;
        }

        return new { list = output };
    }

    /// <summary>
    /// 获取行政区划下拉框数据(异步).
    /// </summary>
    /// <param name="id">当前Id.</param>
    /// <param name="areaId">区域ID.</param>
    /// <returns></returns>
    [HttpGet("{id}/Selector/{areaId}")]
    public async Task<dynamic> GetSelector(string id, string areaId)
    {
        var data = await _repository.AsQueryable().Where(m => m.ParentId == id && m.DeleteMark == null && m.EnabledMark == 1).OrderBy(o => o.SortCode).ToListAsync();
        if (!areaId.Equals("0"))
            data.RemoveAll(x => x.Id == areaId);
        var output = data.Adapt<List<ProvinceSelectorOutput>>();
        foreach (var item in output)
        {
            item.isLeaf = !await _repository.IsAnyAsync(m => m.ParentId == item.id && m.DeleteMark == null);
        }

        return new { list = output };
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值</param>
    /// <returns></returns>
    [HttpGet("{id}/Info")]
    public async Task<dynamic> GetInfo(string id)
    {
        return (await _repository.GetFirstAsync(m => m.Id == id && m.DeleteMark == null)).Adapt<ProvinceInfoOutput>();
    }

    #endregion

    #region POST

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        if (!await _repository.IsAnyAsync(m => m.Id == id && m.DeleteMark == null) || await _repository.IsAnyAsync(m => m.ParentId == id && m.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D1007);
        var isOk = await _repository.AsUpdateable().SetColumns(it => new ProvinceEntity()
        {
            DeleteMark = 1,
            DeleteUserId = _userManager.UserId,
            DeleteTime = SqlFunc.GetDate()
        }).Where(it => it.Id.Equals(id)).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] ProvinceCrInput input)
    {
        if (await _repository.IsAnyAsync(x => x.EnCode == input.enCode && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        var entity = input.Adapt<ProvinceEntity>();
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 编辑.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] ProvinceUpInput input)
    {
        if (await _repository.IsAnyAsync(x => x.Id != id && x.EnCode == input.enCode && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        var entity = input.Adapt<ProvinceEntity>();
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 更新行政区划状态.
    /// </summary>
    /// <param name="id">主键值</param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/State")]
    public async Task ActionsState(string id)
    {
        var isOk = await _repository.AsUpdateable().SetColumns(it => new ProvinceEntity()
        {
            EnabledMark = SqlFunc.IIF(it.EnabledMark == 1, 0, 1),
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id.Equals(id)).ExecuteCommandAsync();
        if (isOk < 0)
            throw Oops.Oh(ErrorCode.COM1003);
    }

    /// <summary>
    /// 获取省市区 根据 二维数组 id .
    /// </summary>
    /// <param name="input">省市区 二维 数组.</param>
    /// <returns></returns>
    [HttpPost("GetAreaByIds")]
    public async Task<dynamic> GetAreaByIds([FromBody] ProvinceGetDataInput input)
    {
        var allIds = new List<string>();
        var res = new List<List<string>>();

        foreach (var item in input.idsList)
        {
            foreach (var it in item)
            {
                allIds.Add(it);
            }
        }

        var data = await _repository.AsQueryable().Where(m => allIds.Contains(m.Id) && m.DeleteMark == null).
            Select(m => new ProvinceEntity() { Id = m.Id, FullName = m.FullName }).ToListAsync();

        foreach (var item in input.idsList)
        {
            var itemValueList = data.FindAll(x => item.Contains(x.Id));
            var valueList = new List<string>();
            itemValueList.ForEach(it =>
            {
                valueList.Add(it.FullName);
            });

            res.Add(valueList);
        }

        return res;
    }

    #endregion
}