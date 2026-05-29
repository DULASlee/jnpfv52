using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extras.Thirdparty.DingDing;
using JNPF.Extras.Thirdparty.WeChat;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Dto.Organize;
using JNPF.Systems.Entitys.Dto.SynThirdInfo;
using JNPF.Systems.Entitys.Dto.SysConfig;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SqlSugar;
using System.Linq.Expressions;

namespace JNPF.Systems.System;

/// <summary>
/// 第三方同步
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "SynThirdInfo", Order = 210)]
[Route("api/system/[controller]")]
public class SynThirdInfoService : ISynThirdInfoService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<SynThirdInfoEntity> _repository;

    /// <summary>
    /// 系统配置服务.
    /// </summary>
    private readonly ISysConfigService _sysConfigService;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 租户管理.
    /// </summary>
    private readonly ITenantManager _tenantManager;

    /// <summary>
    /// 多租户配置选项.
    /// </summary>
    private readonly TenantOptions _tenant;

    /// <summary>
    /// 初始化一个<see cref="SynThirdInfoService"/>类型的新实例.
    /// </summary>
    public SynThirdInfoService(
        ISqlSugarRepository<SynThirdInfoEntity> repository,
        ISysConfigService sysConfigService,
        IUserManager userManager,
        IOptions<TenantOptions> tenantOptions,
        ITenantManager tenantManager)
    {
        _repository = repository;
        _sysConfigService = sysConfigService;
        _userManager = userManager;
        _tenant = tenantOptions.Value;
        _tenantManager = tenantManager;
    }

    #region Get

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="thirdType">请求参数.</param>
    /// <returns></returns>
    [HttpGet("getSynThirdTotal/{thirdType}")]
    public async Task<dynamic> GetList(int thirdType)
    {
        var whereLambda = LinqExpression.And<SynThirdInfoEntity>();
        whereLambda = whereLambda.And(x => x.ThirdType == thirdType);
        return await GetListByThirdType(whereLambda, "用户", "组织");
    }

    /// <summary>
    /// 钉钉同步组织.
    /// </summary>
    /// <returns></returns>
    [HttpGet("synAllOrganizeSysToDing")]
    public async Task<dynamic> synAllOrganizeSysToDing(string type)
    {
        var flag = "0".Equals(type) ? await SynData(2, 1) : await SynSysData(2, 1);
        var whereLambda = LinqExpression.And<SynThirdInfoEntity>();
        whereLambda = whereLambda.And(x => x.ThirdType == 2 && x.DataType < 3);
        return await GetListByThirdType(whereLambda, "组织", "组织");
    }

    /// <summary>
    /// 企业微信同步组织.
    /// </summary>
    /// <returns></returns>
    [HttpGet("synAllOrganizeSysToQy")]
    public async Task<dynamic> synAllOrganizeSysToQy(string type)
    {
        var flag = "0".Equals(type) ? await SynData(1, 1) : await SynSysData(1, 1);
        var whereLambda = LinqExpression.And<SynThirdInfoEntity>();
        whereLambda = whereLambda.And(x => x.ThirdType == 1 && x.DataType < 3);
        return await GetListByThirdType(whereLambda, "组织", "组织");
    }

    /// <summary>
    /// 钉钉同步用户.
    /// </summary>
    /// <returns></returns>
    [HttpGet("synAllUserSysToDing")]
    public async Task<dynamic> synAllUserSysToDing(string type)
    {
        var flag = "0".Equals(type) ? await SynData(2, 3) : await SynSysData(2, 3);
        var whereLambda = LinqExpression.And<SynThirdInfoEntity>();
        whereLambda = whereLambda.And(x => x.ThirdType == 2 && x.DataType == 3);
        return await GetListByThirdType(whereLambda, "用户", "用户");
    }

    /// <summary>
    /// 企业微信同步用户.
    /// </summary>
    /// <returns></returns>
    [HttpGet("synAllUserSysToQy")]
    public async Task<dynamic> synAllUserSysToQy(string type)
    {
        var flag = "0".Equals(type) ? await SynData(1, 3) : await SynSysData(1, 3);
        var whereLambda = LinqExpression.And<SynThirdInfoEntity>();
        whereLambda = whereLambda.And(x => x.ThirdType == 1 && x.DataType == 3);
        return await GetListByThirdType(whereLambda, "用户", "用户");
    }
    #endregion

    #region Method

    /// <summary>
    /// 获取同步数据.
    /// </summary>
    /// <param name="whereLambda">条件Lambda表达式.</param>
    /// <param name="synType1"></param>
    /// <param name="synType2"></param>
    /// <returns></returns>
    private async Task<dynamic> GetListByThirdType(Expression<Func<SynThirdInfoEntity, bool>> whereLambda, string synType1, string synType2)
    {
        var synThirdInfoList = await _repository.AsQueryable().Where(whereLambda).ToListAsync();
        var userList = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => x.DeleteMark == null).ToListAsync();
        var orgList = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null).ToListAsync();
        if (synType1.Equals(synType2))
        {
            return new SynThirdInfoOutput()
            {
                synType = synType1,
                recordTotal = synType1.Equals("组织") ? orgList.Count : userList.Count,
                synDate = synThirdInfoList.Select(x => x.LastModifyTime).ToList().Max().IsEmpty() ? synThirdInfoList.Select(x => x.CreatorTime).ToList().Max() : synThirdInfoList.Select(x => x.LastModifyTime).ToList().Max(),
                synFailCount = synThirdInfoList.FindAll(x => x.EnabledMark == 2).Count,
                synSuccessCount = synThirdInfoList.FindAll(x => x.EnabledMark == 1).Count,
                unSynCount = synThirdInfoList.FindAll(x => x.EnabledMark == 0).Count,
            };
        }
        else
        {
            var output = new List<SynThirdInfoOutput>();
            var synUserList = synThirdInfoList.FindAll(x => x.DataType == 3);
            var synOrgList = synThirdInfoList.FindAll(x => x.DataType < 3);
            output.Add(new SynThirdInfoOutput()
            {
                synType = synType2,
                recordTotal = synType2.Equals("组织") ? orgList.Count : userList.Count,
                synDate = synOrgList.Select(x => x.LastModifyTime).ToList().Max().IsEmpty() ? synOrgList.Select(x => x.CreatorTime).ToList().Max() : synOrgList.Select(x => x.LastModifyTime).ToList().Max(),
                synFailCount = synOrgList.FindAll(x => x.EnabledMark == 2).Count,
                synSuccessCount = synOrgList.FindAll(x => x.EnabledMark == 1).Count,
                unSynCount = synOrgList.FindAll(x => x.EnabledMark == 0).Count,
            });
            output.Add(new SynThirdInfoOutput()
            {
                synType = synType1,
                recordTotal = synType1.Equals("组织") ? orgList.Count : userList.Count,
                synDate = synUserList.Select(x => x.LastModifyTime).ToList().Max().IsEmpty() ? synUserList.Select(x => x.CreatorTime).ToList().Max() : synUserList.Select(x => x.LastModifyTime).ToList().Max(),
                synFailCount = synUserList.FindAll(x => x.EnabledMark == 2).Count,
                synSuccessCount = synUserList.FindAll(x => x.EnabledMark == 1).Count,
                unSynCount = synUserList.FindAll(x => x.EnabledMark == 0).Count,
            });
            return output;
        }
    }

    /// <summary>
    /// 同步数据(同步到第三方).
    /// </summary>
    /// <param name="thirdType"></param>
    /// <param name="dataType"></param>
    /// <returns></returns>
    private async Task<int> SynData(int thirdType, int dataType)
    {
        try
        {
            var sysConfig = await _sysConfigService.GetInfo();
            var synThirdInfo = await _repository.AsQueryable().Where(x => x.ThirdType == thirdType).ToListAsync();
            var orgList = (await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null).ToListAsync()).Adapt<List<OrganizeListOutput>>().ToTree("-1");
            var userList = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => x.DeleteMark == null).ToListAsync();
            if (dataType == 3)
                await SynUser(thirdType, dataType, sysConfig, userList);
            else
                await SynDep(thirdType, dataType, sysConfig, orgList);
            return 1;
        }
        catch (Exception ex)
        {
            return 0;
        }
    }

    /// <summary>
    /// 同步数据(同步到系统).
    /// </summary>
    /// <param name="thirdType"></param>
    /// <param name="dataType"></param>
    /// <returns></returns>
    private async Task<int> SynSysData(int thirdType, int dataType)
    {
        try
        {
            var sysConfig = await _sysConfigService.GetInfo();
            if (dataType == 3)
                await SynSysUser(thirdType, dataType, sysConfig);
            else
                await SynSysDep(thirdType, dataType, sysConfig);
            return 1;
        }
        catch (Exception ex)
        {
            return 0;
        }
    }

    /// <summary>
    /// 删除第三方数据.
    /// </summary>
    /// <param name="thirdType"></param>
    /// <param name="dataType"></param>
    /// <param name="sysConfig"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [NonAction]
    public async Task DelSynData(int thirdType, int dataType, SysConfigOutput sysConfig, string id)
    {
        string msg = string.Empty;
        try
        {
            var synInfo = await _repository.GetFirstAsync(x => x.ThirdType == thirdType && x.DataType == dataType && x.SysObjId == id);
            if (synInfo.IsNullOrEmpty() || synInfo.ThirdObjId.IsNullOrEmpty())
                throw Oops.Oh(ErrorCode.D9004);
            if (thirdType == 1)
            {
                var weChat = new WeChatUtil(sysConfig.qyhCorpId, sysConfig.qyhAgentSecret);
                if (dataType == 3)
                    weChat.DeleteMember(synInfo.ThirdObjId);
                else
                    weChat.DeleteDepartment(synInfo.ThirdObjId.ParseToInt(), ref msg);
            }
            else
            {
                var ding = new DingUtil(sysConfig.dingSynAppKey, sysConfig.dingSynAppSecret);
                if (dataType == 3)
                    ding.DeleteUser(new DingUserParameter() { Userid = synInfo.ThirdObjId }, ref msg);
                else
                    ding.DeleteDep(new DingDepartmentParameter() { DeptId = synInfo.ThirdObjId.ParseToInt() }, ref msg);
            }

            await _repository.DeleteAsync(synInfo);
        }
        catch (Exception ex)
        {
            throw Oops.Oh(msg);
        }
    }

    /// <summary>
    /// 判断是否存在同步成功数据.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="thirdType"></param>
    /// <param name="dataType"></param>
    /// <returns></returns>
    private async Task<bool> IsExistThirdObjId(string id, int thirdType, int dataType)
    {
        return !await _repository.IsAnyAsync(x => x.ThirdType == thirdType && x.DataType == dataType && x.SysObjId.Equals(id) && !SqlFunc.IsNullOrEmpty(x.ThirdObjId));
    }

    /// <summary>
    /// 保存同步数据.
    /// </summary>
    /// <param name="thirdType"></param>
    /// <param name="dataType"></param>
    /// <param name="sysObjId"></param>
    /// <param name="thirdObjId"></param>
    /// <param name="msg"></param>
    /// <returns></returns>
    private async Task Save(int thirdType, int dataType, string sysObjId, string thirdObjId, string msg)
    {
        var entity = await _repository.GetFirstAsync(x => x.SysObjId == sysObjId && x.ThirdType == thirdType);
        if (entity == null)
        {
            entity = new SynThirdInfoEntity();
            entity.Id = SnowflakeIdHelper.NextId();
            entity.CreatorTime = DateTime.Now;
            entity.CreatorUserId = _userManager.UserId;
            entity.ThirdType = thirdType;
            entity.DataType = dataType;
            entity.SysObjId = sysObjId;
            entity.ThirdObjId = thirdObjId;
            entity.EnabledMark = thirdObjId.IsNullOrEmpty() ? 2 : 1;
            entity.Description = msg;
            var newDic = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnEntityAsync();
            _ = newDic ?? throw Oops.Oh(ErrorCode.D9005);
        }
        else
        {
            entity.LastModifyTime = DateTime.Now;
            entity.LastModifyUserId = _userManager.UserId;
            entity.ThirdObjId = thirdObjId;
            entity.EnabledMark = thirdObjId.IsEmpty() ? 2 : 1;
            entity.Description = msg;
            var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();
            if (isOk < 0)
                throw Oops.Oh(ErrorCode.D9006);
        }
    }

    /// <summary>
    /// 获取第三方部门.
    /// </summary>
    /// <param name="organizeId"></param>
    /// <param name="thirdType"></param>
    /// <param name="thirdDepList"></param>
    private async Task GetThirdDep(string organizeId, int thirdType, List<int> thirdDepList)
    {
        var info = await _repository.GetFirstAsync(x => x.SysObjId == organizeId && x.ThirdType == thirdType);
        if (info.IsNotEmptyOrNull() && info.ThirdObjId.IsNotEmptyOrNull())
        {
            thirdDepList.Add(Convert.ToInt32(info.ThirdObjId));
        }
    }

    /// <summary>
    /// 根据系统主键获取第三方主键.
    /// </summary>
    /// <param name="ids">系统主键.</param>
    /// <param name="thirdType">第三方类型.</param>
    /// <param name="dataType">数据类型.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<string>> GetThirdIdList(List<string> ids, int thirdType, int dataType)
    {
        return await _repository.AsQueryable().Where(x => x.ThirdType == thirdType
        && x.DataType == dataType && !SqlFunc.IsNullOrEmpty(x.ThirdObjId)).
        In(x => x.SysObjId, ids.ToArray()).Select(x => x.ThirdObjId).ToListAsync();
    }

    #region 部门同步

    /// <summary>
    /// 同步部门.
    /// </summary>
    /// <param name="thirdType">第三方类型.</param>
    /// <param name="dataType">组织类型.</param>
    /// <param name="sysConfig">系统配置.</param>
    /// <param name="orgList">组织.</param>
    /// <returns></returns>
    [NonAction]
    public async Task SynDep(int thirdType, int dataType, SysConfigOutput sysConfig, List<OrganizeListOutput> orgList)
    {
        switch (thirdType)
        {
            case 1:
                var weChat = new WeChatUtil(sysConfig.qyhCorpId, sysConfig.qyhAgentSecret);
                foreach (var item in orgList)
                {
                    await WeChatDep(item, weChat, thirdType, dataType);
                }

                break;
            default:
                var ding = new DingUtil(sysConfig.dingSynAppKey, sysConfig.dingSynAppSecret);
                foreach (var item in orgList)
                {
                    await DingDep(item, ding, thirdType, dataType);
                }

                break;
        }
    }

    /// <summary>
    /// 同步部门.
    /// </summary>
    /// <param name="thirdType"></param>
    /// <param name="dataType"></param>
    /// <param name="sysConfig"></param>
    /// <returns></returns>
    [NonAction]
    public async Task SynSysDep(int thirdType, int dataType, SysConfigOutput sysConfig)
    {
        try
        {
            var depList = new List<OrganizeEntity>();
            switch (thirdType)
            {
                case 1:
                    var weChat = new WeChatUtil(sysConfig.qyhCorpId, sysConfig.qyhCorpSecret);
                    depList = weChat.GetDepartmentList().Adapt<List<OrganizeEntity>>();
                    break;
                default:
                    var ding = new DingUtil(sysConfig.dingSynAppKey, sysConfig.dingSynAppSecret);
                    depList = ding.GetDepList().Adapt<List<OrganizeEntity>>();
                    break;
            }
            foreach (var item in depList)
            {
                var syncEntity = await _repository.GetFirstAsync(x => x.ThirdObjId == item.Id && x.DataType == dataType);
                // 存在同步数据
                if (syncEntity.IsNotEmptyOrNull())
                {
                    await _repository.AsSugarClient().Updateable<OrganizeEntity>().SetColumns(it => new OrganizeEntity()
                    {
                        FullName = item.FullName,
                        LastModifyUserId = _userManager.UserId,
                        LastModifyTime = SqlFunc.GetDate()
                    }).Where(it => it.Id.Equals(syncEntity.SysObjId)).ExecuteCommandHasChangeAsync();
                }
                else
                {
                    var parentEntity = new OrganizeEntity();
                    if (item.ParentId == "-1")
                    {
                        parentEntity = await _repository.AsSugarClient().Queryable<OrganizeEntity>().FirstAsync(x => x.ParentId == "-1");
                        item.ParentId = parentEntity.Id;
                    }
                    else
                    {
                        item.ParentId = (await _repository.GetFirstAsync(x => x.ThirdObjId == item.ParentId && x.DataType == dataType))?.SysObjId;
                        parentEntity = await _repository.AsSugarClient().Queryable<OrganizeEntity>().FirstAsync(x => x.Id == item.ParentId);
                    }
                    var thirdObjId = item.Id;
                    item.Id = SnowflakeIdHelper.NextId();
                    item.EnCode = item.EnCode.IsNullOrEmpty() ? item.FullName : item.EnCode;
                    item.OrganizeIdTree = parentEntity.OrganizeIdTree + "," + item.Id;
                    await Save(thirdType, dataType, item.Id, thirdObjId, string.Empty);
                    await _repository.AsSugarClient().Insertable(item).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
                }
            }
        }
        catch (Exception e)
        {

            throw;
        }
    }

    private async Task WeChatDep(OrganizeListOutput org, WeChatUtil weChatQYHelper, int thirdType, int dataType)
    {
        long parentid = 1;
        var entity = await _repository.GetFirstAsync(x => x.SysObjId == org.parentId && x.ThirdType == thirdType && x.EnabledMark == 1);
        if (entity.IsNotEmptyOrNull())
        {
            parentid = Convert.ToInt32(entity.ThirdObjId);
        }

        var thirdObjId = string.Empty;
        var msg = string.Empty;
        if (await IsExistThirdObjId(org.id, thirdType, dataType))
        {
            // 顶级组织
            if (org.parentId.Equals("-1"))
            {
                thirdObjId = "1";
            }
            else
            {
                thirdObjId = weChatQYHelper.CreateDepartment(org.fullName, parentid, 1, ref msg).ToString();
            }
        }
        else
        {
            var synEntity = await _repository.GetFirstAsync(x => org.id == x.SysObjId && thirdType == x.ThirdType);
            if (synEntity.IsNotEmptyOrNull())
            {
                thirdObjId = synEntity.ThirdObjId;
                var id = Convert.ToInt32(thirdObjId);
                weChatQYHelper.UpdateDepartment(id, org.fullName, (int)parentid, 1, ref msg);
            }
        }

        await Save(thirdType, dataType, org.id, thirdObjId, msg);
        if (org.hasChildren)
        {
            foreach (var item in org.children)
            {
                var orgChild = item.Adapt<OrganizeListOutput>();
                await WeChatDep(orgChild, weChatQYHelper, thirdType, dataType);
            }
        }
    }

    private async Task DingDep(OrganizeListOutput org, DingUtil dingHelper, int thirdType, int dataType)
    {
        var dingDep = new DingDepartmentParameter();
        dingDep.Name = org.fullName;
        var entity = await _repository.GetFirstAsync(x => x.SysObjId == org.parentId && x.ThirdType == thirdType && x.EnabledMark == 1);
        if (entity != null)
        {
            dingDep.ParentId = Convert.ToInt32(entity.ThirdObjId);
        }

        var thirdObjId = string.Empty;
        var msg = string.Empty;
        if (await IsExistThirdObjId(org.id, thirdType, dataType))
        {
            if (org.parentId.Equals("-1"))
            {
                thirdObjId = "1";
            }
            else
            {
                thirdObjId = dingHelper.CreateDep(dingDep, ref msg);
            }
        }
        else
        {
            var synEntity = await _repository.GetFirstAsync(x => org.id == x.SysObjId && thirdType == x.ThirdType);
            if (synEntity.IsNotEmptyOrNull())
            {
                thirdObjId = synEntity.ThirdObjId;
                dingDep.DeptId = Convert.ToInt32(thirdObjId);
                var flag = dingHelper.UpdateDep(dingDep, ref msg);
                thirdObjId = flag ? thirdObjId : string.Empty;
            }

        }

        await Save(thirdType, dataType, org.id, thirdObjId, msg);
        if (org.hasChildren)
        {
            foreach (var item in org.children)
            {
                var orgChild = item.Adapt<OrganizeListOutput>();
                await DingDep(orgChild, dingHelper, thirdType, dataType);
            }
        }
    }

    #endregion

    #region 用户同步

    /// <summary>
    /// 同步用户.
    /// </summary>
    /// <param name="thirdType"></param>
    /// <param name="dataType"></param>
    /// <param name="sysConfig"></param>
    /// <param name="userList"></param>
    /// <returns></returns>
    [NonAction]
    public async Task SynUser(int thirdType, int dataType, SysConfigOutput sysConfig, List<UserEntity> userList)
    {
        switch (thirdType)
        {
            case 1:
                var weChat = new WeChatUtil(sysConfig.qyhCorpId, sysConfig.qyhAgentSecret);
                foreach (var item in userList)
                {
                    await WeChatUser(item, weChat, thirdType, dataType);
                }

                break;
            default:
                var ding = new DingUtil(sysConfig.dingSynAppKey, sysConfig.dingSynAppSecret);
                foreach (var item in userList)
                {
                    await DingUser(item, ding, thirdType, dataType);
                }

                break;
        }
    }

    /// <summary>
    /// 同步用户.
    /// </summary>
    /// <param name="thirdType"></param>
    /// <param name="dataType"></param>
    /// <param name="sysConfig"></param>
    /// <returns></returns>
    [NonAction]
    public async Task SynSysUser(int thirdType, int dataType, SysConfigOutput sysConfig)
    {
        try
        {
            var userList = new List<UserEntity>();
            switch (thirdType)
            {
                case 1:
                    var weChat = new WeChatUtil(sysConfig.qyhCorpId, sysConfig.qyhCorpSecret);
                    userList = weChat.GetDepartmentMember().Adapt<List<UserEntity>>();
                    break;
                default:
                    var ding = new DingUtil(sysConfig.dingSynAppKey, sysConfig.dingSynAppSecret);
                    userList = ding.GetUserList().Adapt<List<UserEntity>>();
                    break;
            }
            foreach (var item in userList)
            {
                var syncEntity = await _repository.GetFirstAsync(x => x.ThirdObjId == item.Account && x.DataType == dataType);
                // 存在同步数据
                if (syncEntity.IsNotEmptyOrNull())
                {
                    await _repository.AsSugarClient().Updateable<UserEntity>().SetColumns(it => new UserEntity()
                    {
                        RealName = item.RealName,
                        LastModifyUserId = _userManager.UserId,
                        LastModifyTime = SqlFunc.GetDate()
                    }).Where(it => it.Id.Equals(syncEntity.SysObjId)).ExecuteCommandHasChangeAsync();
                }
                else
                {
                    if (_tenant.MultiTenancy)
                    {
                        var tenatInfo = await _tenantManager.GetTenant(_userManager.TenantId);

                        // 是否超过租户账号额度
                        if (tenatInfo.accountNum != 0 && tenatInfo.accountNum <= await _repository.AsSugarClient().Queryable<UserEntity>().CountAsync(x => x.DeleteMark == null))
                            throw Oops.Oh(ErrorCode.D1041);
                    }

                    await Save(thirdType, dataType, item.Id, item.Account, "");
                    var syncOrgEntity = await _repository.GetFirstAsync(x => x.ThirdObjId == item.OrganizeId && x.DataType == 1);
                    if (syncOrgEntity.IsNotEmptyOrNull())
                    {
                        item.OrganizeId = syncOrgEntity.SysObjId;
                    }
                    await _repository.AsSugarClient().Insertable(item).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();

                    // 用户关系
                    var userRelation = new UserRelationEntity()
                    {
                        Id = SnowflakeIdHelper.NextId(),
                        UserId = item.Id,
                        ObjectId = item.OrganizeId,
                        ObjectType = "Organize",
                        SortCode = 0,
                        CreatorTime = DateTime.Now,
                        CreatorUserId = item.Id
                    };
                    await _repository.AsSugarClient().Insertable(userRelation).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
                }
            }
        }
        catch (Exception ex)
        {

            throw;
        }
    }

    private async Task WeChatUser(UserEntity user, WeChatUtil weChatQYHelper, int thirdType, int dataType)
    {
        var qyUser = new QYMember();
        List<int> depList = new List<int>();
        await GetThirdDep(user.OrganizeId, thirdType, depList);
        qyUser.userid = user.Id;
        qyUser.name = user.RealName;
        qyUser.mobile = user.MobilePhone;
        qyUser.email = user.Email;
        qyUser.department = depList.Select(x => (long)x).ToArray();
        var thirdObjId = string.Empty;
        var msg = string.Empty;
        if (await IsExistThirdObjId(user.Id, thirdType, dataType))
        {
            var flag = weChatQYHelper.CreateMember(qyUser, ref msg);
            thirdObjId = flag ? user.Id : weChatQYHelper.GetUserid(qyUser.mobile);
        }
        else
        {
            var flag = weChatQYHelper.UpdateMember(qyUser, ref msg);
            thirdObjId = flag ? user.Id : weChatQYHelper.GetUserid(qyUser.mobile);
        }

        await Save(thirdType, dataType, user.Id, thirdObjId, msg);
    }

    private async Task DingUser(UserEntity user, DingUtil dingHelper, int thirdType, int dataType)
    {
        var dingUser = new DingUserParameter();
        List<int> depList = new List<int>();
        await GetThirdDep(user.OrganizeId, thirdType, depList);
        dingUser.Name = user.RealName;
        dingUser.Mobile = user.MobilePhone;
        dingUser.Email = user.Email;
        dingUser.DeptIdList = string.Join(",", depList);
        var thirdObjId = string.Empty;
        var msg = string.Empty;
        if (await IsExistThirdObjId(user.Id, thirdType, dataType))
        {
            thirdObjId = dingHelper.CreateUser(dingUser, ref msg);
        }
        else
        {
            thirdObjId = (await _repository.GetFirstAsync(x => x.SysObjId == user.Id && x.ThirdType == thirdType)).ThirdObjId;
            dingUser.Userid = thirdObjId;
            var flag = dingHelper.UpdateUser(dingUser, ref msg);
            thirdObjId = flag ? thirdObjId : string.Empty;
        }

        await Save(thirdType, dataType, user.Id, thirdObjId, msg);
    }

    #endregion

    #endregion
}