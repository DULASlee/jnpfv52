using JNPF.Common.Const;
using JNPF.Common.Core.Handlers;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Dtos.OAuth;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models.User;
using JNPF.Common.Security;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Logging.Attributes;
using JNPF.Systems.Entitys.Dto.Tenant;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems.Common;

/// <summary>
/// 多租户功能.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "Tenant", Order = 307)]
[Route("api/system/[controller]")]
public class TenantService : IDynamicApiController, ITransient
{
    private readonly ITenantManager _tenantManager;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// SqlSugarClient客户端.
    /// </summary>
    private readonly SqlSugarScope _sqlSugarClient;

    /// <summary>
    /// IM中心处理程序.
    /// </summary>
    private IMHandler _imHandler;

    public TenantService(
        ITenantManager tenantManager,
        ICacheManager cacheManager,
        IMHandler imHandler,
        ISqlSugarClient sqlSugarClient)
    {
        _tenantManager = tenantManager;
        _imHandler = imHandler;
        _cacheManager = cacheManager;
        _sqlSugarClient = (SqlSugarScope)sqlSugarClient;
    }

    #region Post

    /// <summary>
    /// 获取授权菜单.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("menu")]
    [AllowAnonymous]
    [IgnoreLog]
    public async Task<dynamic> GetMenuTree([FromBody] TenantInterFaceOutput input)
    {
        VerificationHeaders();
        _tenantManager.ChangTenant(_sqlSugarClient, input);

        var systemList = await _sqlSugarClient.Queryable<SystemEntity>()
            .Where(it => it.EnabledMark == 1 && it.DeleteMark == null)
            .OrderBy(it => it.SortCode).OrderByDescending(it => it.CreatorTime)
            .Select(it => new TenantMenuOutput
            {
                id = it.Id,
                parentId = "-1",
                fullName = it.FullName,
                enCode = it.EnCode
            }).ToListAsync();

        var moduleList = new List<TenantMenuOutput>();
        moduleList.Add(new TenantMenuOutput
        {
            id = "-999",
            parentId = "-1",
            fullName = "协同办公",
            enCode = "-999"
        });
        var enCodeList = new List<string> { "workFlow", "workFlow.addFlow", "workFlow.flowLaunch", "workFlow.entrust", "workFlow.flowTodo", "workFlow.flowDone", "workFlow.flowCirculate" };
        foreach (var item in systemList)
        {
            // 系统下的菜单
            var sysModuleList = await _sqlSugarClient.Queryable<ModuleEntity>()
                .Where(it => it.EnabledMark == 1 && it.DeleteMark == null && it.SystemId.Equals(item.id) && !enCodeList.Contains(it.EnCode))
                .OrderBy(it => it.SortCode).OrderByDescending(it => it.CreatorTime)
                .Select(it => new TenantMenuOutput
                {
                    id = it.Id,
                    category = it.Category,
                    parentId = it.ParentId,
                    fullName = it.FullName,
                    urlAddress = it.UrlAddress,
                    enCode = it.EnCode
                })
                .ToListAsync();

            if (sysModuleList.Any(it => it.category.Equals("Web")))
            {
                var webId = string.Format("{0}1", item.id);
                moduleList.Add(new TenantMenuOutput
                {
                    id = webId,
                    enCode = webId,
                    parentId = item.id,
                    fullName = "WEB菜单"
                });

                foreach (var web in sysModuleList.Where(it => it.category.Equals("Web") && it.parentId.Equals("-1")))
                    web.parentId = webId;
            }
            if (sysModuleList.Any(it => it.category.Equals("App")))
            {
                var appId = string.Format("{0}2", item.id);
                moduleList.Add(new TenantMenuOutput
                {
                    id = appId,
                    enCode = appId,
                    parentId = item.id,
                    fullName = "APP菜单"
                });

                foreach (var app in sysModuleList.Where(it => it.category.Equals("App") && it.parentId.Equals("-1")))
                    app.parentId = appId;
            }
            moduleList.AddRange(sysModuleList);
        }

        moduleList.AddRange(systemList);
        var allIds = moduleList.Select(it => it.id).ToList();
        var ids = moduleList.Where(it => !input.moduleIdList.Contains(it.id) && (it.urlAddress.IsNullOrEmpty() || (input.urlAddressList.IsNotEmptyOrNull() && !input.urlAddressList.Contains(it.urlAddress)))).Select(it => it.id).ToList();
        return new { list = moduleList.ToTree("-1"), all = allIds, ids = ids };
    }

    /// <summary>
    /// 获取授权菜单.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("menuList")]
    [AllowAnonymous]
    [IgnoreLog]
    public async Task<dynamic> GetMenuList([FromBody] TenantInterFaceOutput input)
    {
        VerificationHeaders();
        _tenantManager.ChangTenant(_sqlSugarClient, input);
        var systemList = await _sqlSugarClient.Queryable<SystemEntity>()
            .Where(it => it.EnabledMark == 1 && it.DeleteMark == null)
            .OrderBy(it => it.SortCode).OrderByDescending(it => it.CreatorTime)
            .Select(it => new TenantMenuOutput
            {
                id = it.Id,
                parentId = "-1",
                fullName = it.FullName,
                enCode = it.EnCode
            }).ToListAsync();

        var moduleList = new List<TenantMenuOutput>();
        moduleList.Add(new TenantMenuOutput
        {
            id = "-999",
            parentId = "-1",
            fullName = "协同办公",
            enCode = "-999"
        });
        var enCodeList = new List<string> { "workFlow", "workFlow.addFlow", "workFlow.flowLaunch", "workFlow.entrust", "workFlow.flowTodo", "workFlow.flowDone", "workFlow.flowCirculate" };
        foreach (var item in systemList)
        {
            // 系统下的菜单
            var sysModuleList = await _sqlSugarClient.Queryable<ModuleEntity>()
                .Where(it => it.EnabledMark == 1 && it.DeleteMark == null && it.SystemId.Equals(item.id) && !enCodeList.Contains(it.EnCode))
                .OrderBy(it => it.SortCode).OrderByDescending(it => it.CreatorTime)
                .Select(it => new TenantMenuOutput
                {
                    id = it.Id,
                    category = it.Category,
                    parentId = it.ParentId,
                    fullName = it.FullName,
                    urlAddress = it.UrlAddress,
                    enCode = it.EnCode
                })
                .ToListAsync();

            if (sysModuleList.Any(it => it.category.Equals("Web")))
            {
                var webId = string.Format("{0}1", item.id);
                moduleList.Add(new TenantMenuOutput
                {
                    id = webId,
                    enCode = webId,
                    parentId = item.id,
                    fullName = "WEB菜单"
                });

                foreach (var web in sysModuleList.Where(it => it.category.Equals("Web") && it.parentId.Equals("-1")))
                    web.parentId = webId;
            }
            if (sysModuleList.Any(it => it.category.Equals("App")))
            {
                var appId = string.Format("{0}2", item.id);
                moduleList.Add(new TenantMenuOutput
                {
                    id = appId,
                    enCode = appId,
                    parentId = item.id,
                    fullName = "APP菜单"
                });

                foreach (var app in sysModuleList.Where(it => it.category.Equals("App") && it.parentId.Equals("-1")))
                    app.parentId = appId;
            }
            moduleList.AddRange(sysModuleList);
        }

        moduleList.AddRange(systemList);
        //var enCodeList = new List<string> { "workFlow", "workFlow.addFlow", "workFlow.flowLaunch", "workFlow.entrust", "workFlow.flowTodo", "workFlow.flowDone", "workFlow.flowCirculate" };
        //var moduleList = await _sqlSugarClient.Queryable<ModuleEntity>().Where(x => x.DeleteMark == null && x.EnabledMark == 1 && !enCodeList.Contains(x.EnCode)).OrderBy(o => o.SortCode).ToListAsync();
        return moduleList;
    }

    /// <summary>
    /// 获取租户管理员信息.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("getAdminInfo")]
    [AllowAnonymous]
    [IgnoreLog]
    public async Task<dynamic> GetAdminInfo([FromBody] TenantInterFaceOutput input)
    {
        VerificationHeaders();
        _tenantManager.ChangTenant(_sqlSugarClient, input);

        var data = await _sqlSugarClient.Queryable<UserEntity>()
            .Where(it => it.EnabledMark == 1 && it.DeleteMark == null && it.Account.Equals("admin"))
            .Select(it => new {
                userId = it.Id,
                realName = it.RealName,
                account = it.Account,
                mobilePhone = it.MobilePhone,
                email = it.Email,
                gender = it.Gender,
            }).FirstAsync();
        if (data.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1018, "账号不存在");
        return data;
    }

    /// <summary>
    /// 重置租户库admin的密码.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("resetAdminPassword")]
    [AllowAnonymous]
    [IgnoreLog]
    public async Task<bool> resetAdminPassword([FromBody] TenantInterFaceOutput input)
    {
        VerificationHeaders();
        _tenantManager.ChangTenant(_sqlSugarClient, input);

        var user = await _sqlSugarClient.Queryable<UserEntity>().FirstAsync(u => u.Id == input.userId);
        user.Password = MD5Encryption.Encrypt(input.userPassword + user.Secretkey);
        var res = await _sqlSugarClient.Updateable(user).UpdateColumns(x => x.Password).ExecuteCommandAsync();
        return res > 0;
    }

    /// <summary>
    /// 修改租户库admin的基础信息.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("modifyAdminInfo")]
    [AllowAnonymous]
    [IgnoreLog]
    public async Task<bool> modifyAdminInfo([FromBody] TenantInterFaceOutput input)
    {
        VerificationHeaders();
        _tenantManager.ChangTenant(_sqlSugarClient, input);

        var user = await _sqlSugarClient.Queryable<UserEntity>().FirstAsync(u => u.Id == input.userId);
        user.RealName = input.realName;
        user.MobilePhone = input.mobilePhone;
        user.Email = input.email;
        user.Gender = input.gender;
        var res = await _sqlSugarClient.Updateable(user).UpdateColumns(x => new { x.RealName, x.MobilePhone, x.Email, x.Gender }).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();
        return res > 0;
    }

    /// <summary>
    /// 修改租户缓存.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("updateTenantCache")]
    [AllowAnonymous]
    [IgnoreLog]
    public async Task<bool> UpdateTenantCache([FromBody] TenantInterFaceOutput input)
    {
        VerificationHeaders();
        _tenantManager.ChangTenant(_sqlSugarClient, input);

        string cacheKey = string.Format("{0}", CommonConst.GLOBALTENANT);
        var list = await _cacheManager.GetAsync<List<GlobalTenantCacheModel>>(cacheKey);

        if (list != null && list.Any(it => it.TenantId.Equals(input.tenantId)))
        {
            list.FindAll(it => it.TenantId.Equals(input.tenantId)).ForEach(item =>
            {
                item.tenantName = input.tenantName;
                item.validTime = input.validTime;
                item.domain = input.domain;
                item.accountNum = input.accountNum;
                item.moduleIdList = input.moduleIdList;
                item.urlAddressList = input.urlAddressList;
                item.unitInfoJson = input.unitInfoJson;
                item.userInfoJson = input.userInfoJson;
            });

            return await _cacheManager.SetAsync(string.Format("{0}", CommonConst.GLOBALTENANT), list);
        }

        return false;
    }

    /// <summary>
    /// 修改租户缓存.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("{type}/ClearTenantOnlineUser")]
    [AllowAnonymous]
    [IgnoreLog]
    public async Task<bool> ClearTenantOnlineUser(string type, [FromBody] TenantInterFaceOutput input)
    {
        VerificationHeaders();
        _tenantManager.ChangTenant(_sqlSugarClient, input);

        var onlineCacheKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, input.tenantId);
        var list = await _cacheManager.GetAsync<List<UserOnlineModel>>(onlineCacheKey);
        var onlineUserList = list.FindAll(it => it.tenantId == input.tenantId);
        var msg = string.Empty;
        switch (type)
        {
            case "1":
                msg = "租户被禁用";
                break;
            case "2":
                msg = "租户未到有效期";
                break;
            case "3":
                msg = "租户已超过有效期";
                break;
            case "4":
                msg = "租户不存在";
                break;
            case "5":
                msg = "权限已变更，请重新登录";
                break;
            default:
                break;
        }
        foreach (var onlineUser in onlineUserList)
        {
            if (onlineUser != null)
            {
                if (type == "6" && onlineUser.account != "admin") continue;
                await _imHandler.SendMessageAsync(onlineUser.connectionId, new { method = "logout", msg = msg }.ToJsonString());
                // 删除在线用户ID
                list.RemoveAll((x) => x.connectionId == onlineUser.connectionId);
                await _cacheManager.SetAsync(onlineCacheKey, list);

                // 删除用户登录信息缓存
                var cacheKey = string.Format("{0}:{1}:{2}", input.tenantId, CommonConst.CACHEKEYUSER, onlineUser.userId);
                await _cacheManager.DelAsync(cacheKey);
            }
        }
        return false;
    }

    #endregion

    #region public

    /// <summary>
    /// 验证头部JNPF_TENANT 如果头部不包含则报错无权限访问.
    /// </summary>
    public void VerificationHeaders()
    {
        var httpContext = App.HttpContext;
        var headers = httpContext.Request.Headers;
        if (!headers.ContainsKey("JNPF_TENANT"))
            throw Oops.Oh(ErrorCode.Zh10003);
    }
    #endregion
}
