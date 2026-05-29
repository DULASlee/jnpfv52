using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.Signature;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems.System;

/// <summary>
/// 签章管理.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "Signature", Order = 207)]
[Route("api/system/[controller]")]
public class SignatureService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 系统功能表仓储.
    /// </summary>
    private readonly ISqlSugarRepository<SignatureEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="SignatureService"/>类型的新实例.
    /// </summary>
    public SignatureService(
        ISqlSugarRepository<SignatureEntity> repository,
        IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region Get

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] SignatureListInput input)
    {
        var list = await _repository.AsQueryable()
            .Includes(it => it.SignatureUser)
            .Where(it => it.DeleteMark == null)
            .WhereIF(input.userId.IsNotEmptyOrNull(), it => it.SignatureUser.Any(s => s.DeleteMark == null && input.userId.Equals(s.UserId)))
            .WhereIF(input.keyword.IsNotEmptyOrNull(), it => it.FullName.Contains(input.keyword) || it.EnCode.Contains(input.keyword))
            .OrderByDescending(it => it.CreatorTime)
            .Select(it => new SignatureListOutput
            {
                id = it.Id,
                fullName = it.FullName,
                enCode = it.EnCode,
                userIdList = it.SignatureUser.Where(it => it.DeleteMark == null).Select(it => it.UserId).ToList(),
                creatorTime = it.CreatorTime,
            })
            .ToPagedListAsync(input.currentPage, input.pageSize);

        foreach (var item in list.list)
        {
            item.userIds = string.Join("；", await _repository.AsSugarClient().Queryable<UserEntity>().Where(it => item.userIdList.Contains(it.Id)).Select(it => SqlFunc.MergeString(it.RealName, "/", it.Account)).ToListAsync());
        }

        return PageResult<SignatureListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 获取签章下拉框列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelector()
    {
        var list = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null)
            .OrderByDescending(it => it.CreatorTime)
            .Select(it => new SignatureSelectorOutput
            {
                id = it.Id,
                fullName = it.FullName,
                enCode = it.EnCode,
                icon = it.Icon
            })
            .ToListAsync();
        return new { list = list };
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        return await _repository.AsQueryable()
            .Includes(it => it.SignatureUser)
            .Where(it => it.DeleteMark == null && it.Id.Equals(id))
            .Select(it => new SignatureInfoOutput
            {
                id = it.Id,
                fullName = it.FullName,
                enCode = it.EnCode,
                userIds = it.SignatureUser.Where(it => it.DeleteMark == null).Select(it => it.UserId).ToList(),
                icon = it.Icon
            })
            .FirstAsync();
    }

    #endregion

    #region Post

    /// <summary>
    /// 获取签章下拉框列表.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("ListByIds")]
    public async Task<dynamic> GetListByIds([FromBody] SignatureListByIdsInput input)
    {
        var list = await _repository.AsQueryable()
            .Includes(it => it.SignatureUser)
            .Where(it => it.DeleteMark == null && input.ids.Contains(it.Id))
            .Where(it => it.SignatureUser.Any(s => s.DeleteMark == null && s.UserId.Equals(_userManager.UserId)))
            .OrderByDescending(it => it.CreatorTime)
            .Select(it => new SignatureSelectorOutput
            {
                id = it.Id,
                fullName = it.FullName,
                enCode = it.EnCode,
                icon = it.Icon
            })
            .ToListAsync();
        return new { list = list };
    }

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    [UnitOfWork]
    public async Task Create([FromBody] SignatureCrInput input)
    {
        if (await _repository.IsAnyAsync(x => x.DeleteMark == null && (x.FullName == input.fullName || x.EnCode == input.enCode)))
            throw Oops.Oh(ErrorCode.COM1004);
        var entity = input.Adapt<SignatureEntity>();
        entity.Id = SnowflakeIdHelper.NextId();
        entity.CreatorTime = DateTime.Now;
        entity.CreatorUserId = _userManager.UserId;

        var userList = new List<SignatureUserEntity>();
        foreach (var item in input.userIds)
        {
            userList.Add(new SignatureUserEntity
            {
                Id = SnowflakeIdHelper.NextId(),
                SignatureId = entity.Id,
                UserId = item,
                CreatorTime = DateTime.Now,
                CreatorUserId = _userManager.UserId
            });
        }

        try
        {
            await _repository.AsInsertable(entity).ExecuteCommandAsync();
            await _repository.AsSugarClient().Insertable(userList).ExecuteCommandAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.COM1000);
        }
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    [UnitOfWork]
    public async Task Update(string id, [FromBody] SignatureUpInput input)
    {
        if (await _repository.IsAnyAsync(x => x.Id != id && x.DeleteMark == null && (x.FullName == input.fullName || x.EnCode == input.enCode)))
            throw Oops.Oh(ErrorCode.COM1004);
        var entity = input.Adapt<SignatureEntity>();

        var userList = new List<SignatureUserEntity>();
        foreach (var item in input.userIds)
        {
            userList.Add(new SignatureUserEntity
            {
                Id = SnowflakeIdHelper.NextId(),
                SignatureId = id,
                UserId = item,
                CreatorTime = DateTime.Now,
                CreatorUserId = _userManager.UserId
            });
        }

        try
        {
            await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
            await _repository.AsSugarClient().Deleteable<SignatureUserEntity>().Where(it => it.SignatureId.Equals(id)).ExecuteCommandAsync();
            await _repository.AsSugarClient().Insertable(userList).ExecuteCommandAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.COM1000);
        }
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键值.</param>
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
