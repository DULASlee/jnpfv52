using JNPF.Apps.Entitys;
using JNPF.Apps.Entitys.Dto;
using JNPF.Apps.Interfaces;
using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Interfaces.Service;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Apps;

/// <summary>
/// App常用数据
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01 .
/// </summary>
[ApiDescriptionSettings(Tag = "App", Name = "Data", Order = 800)]
[Route("api/App/[controller]")]
public class AppDataService : IAppDataService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<AppDataEntity> _repository; // App常用数据

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 流程管理.
    /// </summary>
    private readonly IFlowTemplateService _flowTemplateService;

    /// <summary>
    /// 构造.
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="userManager"></param>
    /// <param name="flowTemplateService"></param>
    public AppDataService(
        ISqlSugarRepository<AppDataEntity> repository,
        IUserManager userManager,
        IFlowTemplateService flowTemplateService)
    {
        _repository = repository;
        _userManager = userManager;
        _flowTemplateService = flowTemplateService;
    }

    #region Get

    /// <summary>
    /// 常用数据.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] string type)
    {
        List<AppDataEntity>? list = await GetListByType(type);
        List<AppDataListOutput>? output = list.Adapt<List<AppDataListOutput>>();
        if (type.Equals("1"))
        {
            foreach (var item in output)
            {
                var flowJson = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().First(x => x.TemplateId == item.objectId && x.EnabledMark == 1 && x.DeleteMark == null);
                if (flowJson != null)
                {
                    item.objectId = flowJson.Id;
                }
            }
        }
        else if (type.Equals("2"))
        {
            var newList = new List<AppDataListOutput>();
            foreach (var item in output)
            {
                if (await _repository.AsSugarClient().Queryable<ModuleEntity>().AnyAsync(it => it.DeleteMark == null && it.EnabledMark == 1 && it.Id.Equals(item.objectId)))
                    newList.Add(item);
            }

            if (!_userManager.IsAdministrator)
            {
                var authorIds = (await GetAppMenuList(string.Empty)).Select(it => it.Id).ToList();
                newList = newList.Where(it => authorIds.Contains(it.objectId)).ToList();
            }

            return new { list = newList };
        }
        return new { list = output };
    }

    /// <summary>
    /// 所有流程.
    /// </summary>
    /// <returns></returns>
    [HttpGet("getFlowList")]
    public async Task<dynamic> GetFlowList([FromQuery] CommonInput input)
    {
        var list = await _repository.AsSugarClient().Queryable<FlowTemplateEntity>()
               .Where(a => a.DeleteMark == null && a.EnabledMark == 1 && a.Type == 0)
               .WhereIF(input.category.IsNotEmptyOrNull(), a => a.Category == input.category)
               .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
               .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
               .OrderBy(a => a.LastModifyTime, OrderByType.Desc)
               .Select(a => new AppFlowListAllOutput
               {
                   id = a.Id,
                   icon = a.Icon,
                   enCode = a.EnCode,
                   fullName = a.FullName,
                   iconBackground = a.IconBackground,
                   isData = SqlFunc.Subqueryable<AppDataEntity>().Where(x => x.ObjectType == "1" && x.CreatorUserId == _userManager.UserId && x.ObjectId == a.Id && x.DeleteMark == null).Any(),
               }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<AppFlowListAllOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 所有流程.
    /// </summary>
    /// <returns></returns>
    [HttpGet("getDataList")]
    public async Task<dynamic> GetDataList(string keyword)
    {
        List<AppDataListAllOutput>? list = (await GetAppMenuList(keyword)).Adapt<List<AppDataListAllOutput>>();
        foreach (AppDataListAllOutput? item in list)
        {
            item.isData = _repository.IsAny(x => x.ObjectType == "2" && x.CreatorUserId == _userManager.UserId && x.ObjectId == item.id && x.DeleteMark == null);
        }

        List<AppDataListAllOutput>? output = list.ToTree("-1");
        return new { list = output };
    }
    #endregion

    #region Post

    /// <summary>
    /// 新增.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] AppDataCrInput input)
    {
        AppDataEntity? entity = input.Adapt<AppDataEntity>();
        int isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="objectId"></param>
    /// <returns></returns>
    [HttpDelete("{objectId}")]
    public async Task Delete(string objectId)
    {
        AppDataEntity? entity = await _repository.GetSingleAsync(x => x.ObjectId == objectId && x.CreatorUserId == _userManager.UserId && x.DeleteMark == null);
        var isOk = await _repository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private async Task<List<AppDataEntity>> GetListByType(string type)
    {
        return await _repository.AsQueryable().Where(x => x.ObjectType == type && x.CreatorUserId == _userManager.UserId && x.DeleteMark == null).OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .ToListAsync();
    }

    /// <summary>
    /// 菜单列表.
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<List<ModuleEntity>> GetAppMenuList(string keyword)
    {
        List<ModuleEntity>? menuList = new List<ModuleEntity>();
        if (_userManager.IsAdministrator)
        {
            menuList = await _repository.AsSugarClient().Queryable<ModuleEntity>()
                .Where(x => x.EnabledMark == 1 && x.Category == "App" && x.DeleteMark == null && x.SystemId == _userManager.User.AppSystemId)
                .WhereIF(!string.IsNullOrEmpty(keyword), x => x.FullName.Contains(keyword) || x.ParentId == "-1")
                .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
                .OrderByIF(!string.IsNullOrEmpty(keyword), a => a.LastModifyTime, OrderByType.Desc)
                .ToListAsync();
        }
        else
        {
            if (_userManager.User.AppSystemId.IsNotEmptyOrNull())
            {
                // 分管 拥有所有菜单权限
                if (_userManager.DataScope.Any(x => x.organizeType.IsNotEmptyOrNull() && x.organizeId.Equals(_userManager.User.AppSystemId)))
                {
                    // 当前系统的有权限的菜单
                    menuList = await _repository.AsSugarClient().Queryable<ModuleEntity>()
                        .Where(a => (a.SystemId.Equals(_userManager.User.AppSystemId) && a.EnabledMark == 1 && a.Category.Equals("App") && a.DeleteMark == null))
                        .WhereIF(!string.IsNullOrEmpty(keyword), x => x.FullName.Contains(keyword) || x.ParentId == "-1")
                        .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
                        .OrderByIF(!string.IsNullOrEmpty(keyword), a => a.LastModifyTime, OrderByType.Desc).ToListAsync();
                }
                else
                {
                    var pIds = _userManager.PermissionGroup;
                    var mIdList = await _repository.AsSugarClient().Queryable<AuthorizeEntity>().Where(a => pIds.Contains(a.ObjectId)).Where(a => a.ItemType == "module").Select(a => a.ItemId).ToListAsync();

                    // 当前系统的有权限的菜单
                    menuList = await _repository.AsSugarClient().Queryable<ModuleEntity>()
                        .Where(a => (a.SystemId.Equals(_userManager.User.AppSystemId) && mIdList.Contains(a.Id) && a.EnabledMark == 1 && a.Category.Equals("App") && a.DeleteMark == null))
                        .WhereIF(!string.IsNullOrEmpty(keyword), x => x.FullName.Contains(keyword) || x.ParentId == "-1")
                        .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
                        .OrderByIF(!string.IsNullOrEmpty(keyword), a => a.LastModifyTime, OrderByType.Desc).ToListAsync();
                }
            }
        }

        return menuList;
    }

    #endregion
}