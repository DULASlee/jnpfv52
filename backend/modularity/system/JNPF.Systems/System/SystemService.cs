using JNPF.Common.Const;
using JNPF.Common.Core.Handlers;
using JNPF.Extensions;
using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models.User;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Dto.System.System;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems.System;

/// <summary>
/// 系统功能.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "System", Order = 200)]
[Route("api/system/[controller]")]
public class SystemService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 系统功能表仓储.
    /// </summary>
    private readonly ISqlSugarRepository<SystemEntity> _repository;

    /// <summary>
    /// 缓存管理器.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// IM中心处理程序.
    /// </summary>
    private IMHandler _imHandler;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="SystemService"/>类型的新实例.
    /// </summary>
    public SystemService(
        ISqlSugarRepository<SystemEntity> repository,
        ICacheManager cacheManager,
        IMHandler imHandler,
        IUserManager userManager)
    {
        _repository = repository;
        _cacheManager = cacheManager;
        _imHandler = imHandler;
        _userManager = userManager;
    }

    #region Get

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] SystemQuery input)
    {
        // 根据多租户返回结果moduleIdList :[菜单id] 过滤应用菜单
        var ignoreIds = _userManager.TenantIgnoreModuleIdList;

        var authorIds = new List<string>();
        if (_repository.AsSugarClient().Queryable<OrganizeAdministratorEntity>().Any(x => x.UserId.Equals(_userManager.UserId)) && _userManager.UserOrigin.Equals("pc") && _userManager.DataScope.Any(x => x.organizeType.IsNotEmptyOrNull()))
            authorIds = _userManager.DataScope.Where(x => x.organizeType.IsNotEmptyOrNull()).Select(x => x.organizeId).ToList();
        else
            authorIds = await _repository.AsSugarClient().Queryable<AuthorizeEntity>().Where(x => x.ItemType.Equals("system") && x.ObjectType.Equals("Role") && _userManager.PermissionGroup.Contains(x.ObjectId)).Select(x => x.ItemId).ToListAsync();

        var whereLambda = LinqExpression.And<SystemEntity>();
        whereLambda = whereLambda.And(x => x.DeleteMark == null);
        if (!_userManager.IsAdministrator)
            whereLambda = whereLambda.And(x => authorIds.Contains(x.Id));
        if (input.keyword.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(x => x.FullName.Contains(input.keyword) || x.EnCode.Contains(input.keyword));
        if (input.enabledMark.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(x => x.EnabledMark == input.enabledMark);
        if (ignoreIds.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(x => !ignoreIds.Contains(x.Id));
        var output = (await _repository.AsQueryable().Where(whereLambda).OrderBy(a => a.SortCode).OrderByDescending(a => a.CreatorTime).ToListAsync()).Adapt<List<SystemListOutput>>();

        if (output.Any(it => it.enCode.Equals("mainSystem")))
            output.Find(it => it.enCode.Equals("mainSystem")).mainSystem = true;

        return new { list = output };
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
        return data.Adapt<SystemCrInput>();
    }
    #endregion

    #region Post

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] SystemCrInput input)
    {
        if (await _repository.IsAnyAsync(x => (x.EnCode == input.enCode || x.FullName == input.fullName) && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        var entity = input.Adapt<SystemEntity>();
        entity.Id = SnowflakeIdHelper.NextId();
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
        if (isOk < 1) throw Oops.Oh(ErrorCode.COM1000);

        // 默认拥有该应用的权限(分管权限)
        var orgAdmin = new OrganizeAdministratorEntity()
        {
            OrganizeId = entity.Id,
            OrganizeType = "System",
            UserId = _userManager.UserId,
            EnabledMark = 1,
            ThisLayerAdd = 1,
            ThisLayerDelete = 1,
            ThisLayerEdit = 1,
            ThisLayerSelect = 1
        };

        await _repository.AsSugarClient().Insertable(orgAdmin).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] SystemCrInput input)
    {
        if (await _repository.IsAnyAsync(x => x.Id != id && (x.EnCode == input.enCode || x.FullName == input.fullName) && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);

        // 主系统（开发平台、业务平台）
        var mainSystem = await _repository.GetFirstAsync(it => it.DeleteMark == null && it.IsMain.Equals(1) && it.EnCode.Equals("mainSystem"));
        var workSystem = await _repository.GetFirstAsync(it => it.DeleteMark == null && it.IsMain.Equals(1) && it.EnCode.Equals("workSystem"));

        // 判断主系统是否被禁用.
        if ((input.id.Equals(mainSystem.Id) || input.id.Equals(workSystem.Id)) && input.enabledMark.Equals(0))
            throw Oops.Oh(ErrorCode.D1036);

        // 判断主系统是否有修改系统编码.
        if ((input.id.Equals(mainSystem.Id) && !input.enCode.Equals(mainSystem.EnCode)) || (input.id.Equals(workSystem.Id) && !input.enCode.Equals(workSystem.EnCode)))
            throw Oops.Oh(ErrorCode.D1037);

        // 当用户的子系统被禁用时，提醒在线用户.
        if (input.enabledMark.Equals(0)) await ForcedOffline(id);

        var isOk = await _repository.AsUpdateable()
            .SetColumns(it => new SystemEntity()
            {
                FullName = input.fullName,
                EnCode = input.enCode,
                Icon = input.icon,
                PropertyJson = input.propertyJson,
                NavigationIcon = input.navigationIcon,
                WorkLogoIcon = input.workLogoIcon,
                SortCode = input.sortCode,
                Description = input.description,
                EnabledMark = input.enabledMark,
                WorkflowEnabled = input.workflowEnabled,
                LastModifyUserId = _userManager.UserId,
                LastModifyTime = SqlFunc.GetDate(),
            })
            .Where(it => it.Id.Equals(id))
            .ExecuteCommandAsync();
        if (!(isOk > 0))
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

        await ForcedOffline(id);
        var isOk = await _repository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    #endregion

    #region Private

    /// <summary>
    /// 强制当前系统下的所有用户下线或切换应用.
    /// </summary>
    /// <param name="systemId"></param>
    private async Task ForcedOffline(string systemId)
    {
        var tenantId = _userManager.TenantId;
        var cacheKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, tenantId);
        var allUserOnlineList = await _cacheManager.GetAsync<List<UserOnlineModel>>(cacheKey);

        var saveUserOnlineList = allUserOnlineList.Copy();
        foreach (var item in allUserOnlineList)
        {
            if (item.systemId.IsNotEmptyOrNull() && item.systemId.Equals(systemId))
            {
                await _imHandler.SendMessageAsync(item.connectionId, new { method = "logout", msg = "应用已被禁用或删除" }.ToJsonString());

                // 删除在线用户ID
                saveUserOnlineList.RemoveAll((x) => x.connectionId == item.connectionId);
                await _cacheManager.SetAsync(cacheKey, saveUserOnlineList);
            }
        }
    }

    #endregion
}
