using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.Group;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Permission;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 业务实现：分组管理
/// 版 本：V3.3.3
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2022.03.11.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "Group", Order = 162)]
[Route("api/Permission/[controller]")]
public class GroupService : IUserGroupService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<GroupEntity> _repository;

    /// <summary>
    /// 用户管理器.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="GroupService"/>类型的新实例.
    /// </summary>
    public GroupService(
        ISqlSugarRepository<GroupEntity> repository,
        IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 获取列表.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] CommonInput input)
    {
        var dictionaryTypeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "groupType" && x.DeleteMark == null);
        SqlSugarPagedList<GroupListOutput>? data = await _repository.AsSugarClient().Queryable<GroupEntity, DictionaryDataEntity>(
            (a, b) => new JoinQueryInfos(JoinType.Left, a.Category == b.Id && b.DictionaryTypeId == dictionaryTypeEntity.Id))

            // 关键字（名称、编码）
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
            .WhereIF(input.type.IsNotEmptyOrNull(), a => a.Category.Equals(input.type))
            .WhereIF(input.enabledMark.IsNotEmptyOrNull(), a => a.EnabledMark.Equals(input.enabledMark))
            .Where(a => a.DeleteMark == null).OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc).OrderByIF(!input.keyword.IsNullOrEmpty(), a => a.LastModifyTime, OrderByType.Desc)
            .Select((a, b) => new GroupListOutput
            {
                id = a.Id,
                fullName = a.FullName,
                enCode = a.EnCode,
                type = b.FullName,
                enabledMark = a.EnabledMark,
                creatorTime = a.CreatorTime,
                description = a.Description,
                sortCode = a.SortCode
            }).ToPagedListAsync(input.currentPage, input.pageSize);

        return PageResult<GroupListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 获取信息.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        GroupEntity? entity = await _repository.GetSingleAsync(p => p.Id == id);
        return entity.Adapt<GroupUpInput>();
    }

    /// <summary>
    /// 获取下拉框.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelector()
    {
        // 获取所有分组数据
        List<GroupEntity>? groupList = await _repository.AsQueryable()
            .Where(t => t.EnabledMark == 1 && t.DeleteMark == null)
            .OrderBy(o => o.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc).OrderBy(a => a.LastModifyTime, OrderByType.Desc).ToListAsync();
        var dictionaryTypeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "groupType" && x.DeleteMark == null);
        // 获取所有分组类型(字典)
        List<DictionaryDataEntity>? typeList = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>()
            .Where(x => x.DictionaryTypeId == dictionaryTypeEntity.Id && x.DeleteMark == null && x.EnabledMark == 1).ToListAsync();

        List<GroupSelectorOutput>? treeList = new List<GroupSelectorOutput>();
        typeList.ForEach(item =>
        {
            if (groupList.Count(x => x.Category == item.Id) > 0)
            {
                treeList.Add(new GroupSelectorOutput()
                {
                    id = item.Id,
                    parentId = "0",
                    num = groupList.Count(x => x.Category == item.Id),
                    fullName = item.FullName
                });
            }
        });

        groupList.ForEach(item =>
        {
            treeList.Add(
                new GroupSelectorOutput
                {
                    id = item.Id,
                    parentId = item.Category,
                    type = "group",
                    icon = "icon-ym icon-ym-generator-group1",
                    fullName = item.FullName,
                    sortCode = item.SortCode
                });
        });

        return treeList.OrderBy(x => x.sortCode).ToList().ToTree("0");
    }

    #endregion

    #region POST

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] GroupCrInput input)
    {
        if (await _repository.IsAnyAsync(p => p.FullName == input.fullName && p.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2402);
        if (await _repository.IsAnyAsync(p => p.EnCode == input.enCode && p.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2401);
        GroupEntity? entity = input.Adapt<GroupEntity>();
        int isOk = await _repository.AsInsertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D2400);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        // 岗位下有用户不能删
        if (await _repository.AsSugarClient().Queryable<UserRelationEntity>().AnyAsync(u => u.ObjectType == "Group" && u.ObjectId == id))
            throw Oops.Oh(ErrorCode.D2406);

        GroupEntity? entity = await _repository.GetSingleAsync
            (p => p.Id == id && p.DeleteMark == null);
        int isOk = await _repository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D2403);
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] GroupUpInput input)
    {
        GroupEntity oldEntity = await _repository.GetSingleAsync(it => it.Id == id);
        if (await _repository.IsAnyAsync(p => p.FullName == input.fullName && p.DeleteMark == null && p.Id != id))
            throw Oops.Oh(ErrorCode.D2402);
        if (await _repository.IsAnyAsync(p => p.EnCode == input.enCode && p.DeleteMark == null && p.Id != id))
            throw Oops.Oh(ErrorCode.D2401);

        if (oldEntity.EnabledMark.Equals(1) && input.enabledMark.Equals(0) && await _repository.AsSugarClient().Queryable<UserRelationEntity>().AnyAsync(x => x.ObjectId == id))
            throw Oops.Oh(ErrorCode.COM1030);
        GroupEntity entity = input.Adapt<GroupEntity>();
        int isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        if (!(isOk > 0))
            throw Oops.Oh(ErrorCode.D2404);
    }

    /// <summary>
    /// 更新状态.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/State")]
    public async Task UpdateState(string id)
    {
        if (!await _repository.IsAnyAsync(r => r.Id == id && r.DeleteMark == null)) throw Oops.Oh(ErrorCode.D2405);
        int isOk = await _repository.AsSugarClient().Updateable<GroupEntity>().UpdateColumns(it => new GroupEntity()
        {
            EnabledMark = SqlFunc.IIF(it.EnabledMark == 1, 0, 1),
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id == id).ExecuteCommandAsync();
        if (!(isOk > 0))
            throw Oops.Oh(ErrorCode.D6004);
    }

    /// <summary>
    /// 通过分组id获取分组列表.
    /// </summary>
    /// <returns></returns>
    [HttpPost("GroupCondition")]
    public async Task<dynamic> GroupCondition([FromBody] GroupConditionInput input)
    {
        // 获取所有分组数据
        List<GroupEntity>? groupList = await _repository.AsQueryable()
            .Where(it => it.EnabledMark == 1 && it.DeleteMark == null && input.ids.Contains(it.Id))
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
            .OrderBy(it => it.SortCode).OrderBy(it => it.CreatorTime, OrderByType.Desc).OrderBy(it => it.LastModifyTime, OrderByType.Desc).ToListAsync();

        var dictionaryTypeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "groupType" && x.DeleteMark == null);
        // 获取所有分组类型(字典)
        List<DictionaryDataEntity>? typeList = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>()
            .Where(x => x.DictionaryTypeId == dictionaryTypeEntity.Id && x.DeleteMark == null && x.EnabledMark == 1).ToListAsync();

        List<GroupConditionOutput>? treeList = new List<GroupConditionOutput>();
        typeList.ForEach(item =>
        {
            if (groupList.Count(x => x.Category == item.Id) > 0)
            {
                treeList.Add(new GroupConditionOutput()
                {
                    id = item.Id,
                    parentId = "0",
                    num = groupList.Count(x => x.Category == item.Id),
                    fullName = item.FullName
                });
            }
        });

        groupList.ForEach(item =>
        {
            treeList.Add(
                new GroupConditionOutput
                {
                    id = item.Id,
                    parentId = item.Category,
                    type = "group",
                    icon = "icon-ym icon-ym-generator-group1",
                    fullName = item.FullName
                });
        });

        return new { list = treeList.ToList().ToTree("0") };
    }

    #endregion
}