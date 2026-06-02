using System.Diagnostics;
using JNPF.Common.Captcha.General;
using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Dtos.OAuth;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Models.User;
using JNPF.Common.Net;
using JNPF.Common.Options;
using JNPF.Common.Security;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.EventBus;
using JNPF.EventHandler;
using JNPF.Extras.CollectiveOAuth.Models;
using JNPF.FriendlyException;
using JNPF.Logging.Attributes;
using JNPF.Message.Interfaces;
using JNPF.OAuth.Dto;
using JNPF.OAuth.Model;
using JNPF.RemoteRequest.Extensions;
using JNPF.Systems.Entitys.Dto.Module;
using JNPF.Systems.Entitys.Enum;
using JNPF.Systems.Entitys.Model.Permission.SocialsUser;
using JNPF.Systems.Entitys.Model.Permission.User;
using JNPF.Systems.Entitys.Model.SysConfig;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using JNPF.UnifyResult;
using JNPF.VisualDev.Entitys;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SqlSugar;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;

namespace JNPF.OAuth;

/// <summary>
/// 业务实现：身份认证模块 .
/// </summary>
[ApiDescriptionSettings(Tag = "OAuth", Name = "OAuth", Order = 160)]
[Route("api/[controller]")]
public class OAuthService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 配置文档.
    /// </summary>
    private readonly OauthOptions _oauthOptions = App.GetConfig<OauthOptions>("OAuth", true);

    /// <summary>
    /// 用户仓储.
    /// </summary>
    private readonly ISqlSugarRepository<UserEntity> _userRepository;

    /// <summary>
    /// 功能模块.
    /// </summary>
    private readonly IModuleService _moduleService;

    /// <summary>
    /// 功能按钮.
    /// </summary>
    private readonly IModuleButtonService _moduleButtonService;

    /// <summary>
    /// 功能列.
    /// </summary>
    private readonly IModuleColumnService _columnService;

    /// <summary>
    /// 功能数据权限计划.
    /// </summary>
    private readonly IModuleDataAuthorizeSchemeService _moduleDataAuthorizeSchemeService;

    /// <summary>
    /// 功能表单.
    /// </summary>
    private readonly IModuleFormService _formService;

    /// <summary>
    /// 系统配置.
    /// </summary>
    private readonly ISysConfigService _sysConfigService;

    /// <summary>
    /// 验证码处理程序.
    /// </summary>
    private readonly IGeneralCaptcha _captchaHandler;

    /// <summary>
    /// 第三方登录.
    /// </summary>
    private readonly ISocialsUserService _socialsUserService;

    /// <summary>
    /// 数据库配置选项.
    /// </summary>
    private readonly SqlSugar.ConnectionStringsOptions _connectionStrings;

    /// <summary>
    /// 多租户配置选项.
    /// </summary>
    private readonly TenantOptions _tenant;

    /// <summary>
    /// Http上下文.
    /// </summary>
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 事件总线.
    /// </summary>
    private readonly IEventPublisher _eventPublisher;

    /// <summary>
    /// 解析服务作用域工厂服务.
    /// </summary>
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    /// SqlSugarClient客户端.
    /// </summary>
    private SqlSugarScope _sqlSugarClient;

    private readonly IMessageManager _messageManager;

    private readonly ITenantManager _tenantManager;

    /// <summary>
    /// 初始化一个<see cref="OAuthService"/>类型的新实例.
    /// </summary>
    public OAuthService(
        IServiceScopeFactory serviceScopeFactory,
        IGeneralCaptcha captchaHandler,
        ISqlSugarRepository<UserEntity> userRepository,
        IModuleService moduleService,
        IModuleButtonService moduleButtonService,
        IModuleColumnService columnService,
        IModuleDataAuthorizeSchemeService moduleDataAuthorizeSchemeService,
        IModuleFormService formService,
        ISysConfigService sysConfigService,
        ISocialsUserService socialsUserService,
        IMessageManager messageManager,
        IOptions<SqlSugar.ConnectionStringsOptions> connectionOptions,
        IOptions<TenantOptions> tenantOptions,
        ISqlSugarClient sqlSugarClient,
        IHttpContextAccessor httpContextAccessor,
        ICacheManager cacheManager,
        IUserManager userManager,
        ITenantManager tenantManager,
        IEventPublisher eventPublisher)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _captchaHandler = captchaHandler;
        _userRepository = userRepository;
        _moduleService = moduleService;
        _moduleButtonService = moduleButtonService;
        _columnService = columnService;
        _moduleDataAuthorizeSchemeService = moduleDataAuthorizeSchemeService;
        _formService = formService;
        _sysConfigService = sysConfigService;
        _socialsUserService = socialsUserService;
        _messageManager = messageManager;
        _connectionStrings = connectionOptions.Value;
        _tenant = tenantOptions.Value;
        _sqlSugarClient = (SqlSugarScope)sqlSugarClient;
        _httpContextAccessor = httpContextAccessor;
        _cacheManager = cacheManager;
        _userManager = userManager;
        _tenantManager = tenantManager;
        _eventPublisher = eventPublisher;
    }

    #region Get

    /// <summary>
    /// 获取图形验证码.
    /// </summary>
    /// <param name="codeLength">验证码长度.</param>
    /// <param name="timestamp">时间戳.</param>
    /// <returns></returns>
    [HttpGet("ImageCode/{codeLength}/{timestamp}")]
    [AllowAnonymous]
    [LogPolicy(LogPolicy.IgnoreAll)]
    [NonUnify]
    public async Task<IActionResult> GetCode(int codeLength, string timestamp)
    {
        return new FileContentResult(await _captchaHandler.CreateCaptchaImage(timestamp, 120, 40, codeLength > 0 ? codeLength : 4), "image/jpeg");
    }

    /// <summary>
    /// 首次登录 根据账号获取数据库配置.
    /// </summary>
    /// <param name="account">账号.</param>
    [HttpGet("getConfig/{account}")]
    [AllowAnonymous]
    [LogPolicy(LogPolicy.IgnoreAll)]
    public async Task<dynamic> GetConfigCode(string account)
    {
        ConnectionConfigOptions options = new ConnectionConfigOptions();
        var defaultConnection = _connectionStrings.DefaultConnectionConfig;
        options = JNPFTenantExtensions.GetLinkToOrdinary(defaultConnection.ConfigId.ToString(), defaultConnection.DBName);
        string tenantId = defaultConnection.ConfigId.ToString();
        var tenantInterFaceOutput = new TenantInterFaceOutput();

        // 域名租户
        if (_tenant.MultiTenancy && tenantId.Equals("default") && App.HttpContext.Request.Headers["origin"].IsNotEmptyOrNull())
        {
            // 本地url地址
            var address = _userManager.UserOrigin.Equals("pc") ? App.Configuration["Message:DoMainPc"] : App.Configuration["Message:DoMainApp"];
            address = address.Replace("https://", string.Empty).Replace("http://", string.Empty).Replace("www.", string.Empty);
            var host = App.HttpContext.Request.Headers["origin"].ToString().Replace("https://", string.Empty).Replace("http://", string.Empty).Replace("www.", string.Empty);

            if (host.Contains(address) && !host.Equals(address))
            {
                tenantId = host.Split(".").FirstOrDefault();
                account = string.Format("{0}@{1}", tenantId, account);
            }
        }

        if (_tenant.MultiTenancy)
        {
            string tenantAccout = string.Empty;

            // 分割账号
            var tenantAccount = account.Split('@');
            tenantId = tenantAccount.FirstOrDefault();

            if (tenantAccount.Length == 1) account = "admin";
            else account = tenantAccount[1];

            tenantAccout = account;
            tenantInterFaceOutput = await _tenantManager.ChangTenant(_sqlSugarClient, tenantId, false);
            options = tenantInterFaceOutput.options;
        }

        // 验证连接是否成功
        if (!_sqlSugarClient.Ado.IsValidConnection()) throw Oops.Oh(ErrorCode.D1032);

        // 读取配置文件
        var array = new Dictionary<string, object>();
        var sysConfigData = await _sqlSugarClient.Queryable<SysConfigEntity>()
            .Where(x => x.Category.Equals("SysConfig") && (SqlFunc.ToLower(x.Key).Equals("singlelogin") || SqlFunc.ToLower(x.Key).Equals("enableverificationcode") || SqlFunc.ToLower(x.Key).Equals("verificationcodenumber"))).ToListAsync();
        foreach (var item in sysConfigData)
        {
            if (!array.ContainsKey(item.Key)) array.Add(item.Key, item.Value);
        }

        var sysConfig = array.ToObject<SysConfigModel>();

        /*
        * 登录完成后添加全局租户缓存
        * 判断当前租户是否存在缓存
        * 不存在添加缓存
        * 存在更新缓存
        */
        if (!await IsAnyByTenantIdAsync(tenantId))
        {
            List<GlobalTenantCacheModel>? list = _userManager.GetGlobalTenantCache();
            list.Add(new GlobalTenantCacheModel
            {
                TenantId = tenantId,
                SingleLogin = (int)sysConfig.singleLogin,
                connectionConfig = options,
                type = tenantInterFaceOutput.type,
                tenantName = tenantInterFaceOutput.tenantName,
                validTime = tenantInterFaceOutput.validTime,
                domain = tenantInterFaceOutput.domain,
                accountNum = tenantInterFaceOutput.accountNum,
                moduleIdList = tenantInterFaceOutput.moduleIdList,
                urlAddressList = tenantInterFaceOutput.urlAddressList,
                unitInfoJson = tenantInterFaceOutput.unitInfoJson,
                userInfoJson = tenantInterFaceOutput.userInfoJson,

            });
            await SetGlobalTenantCache(list);
        }
        else
        {
            List<GlobalTenantCacheModel>? list = _userManager.GetGlobalTenantCache();
            list.FindAll(it => it.TenantId.Equals(tenantId)).ForEach(item =>
            {
                item.TenantId = tenantId;
                item.SingleLogin = (int)sysConfig.singleLogin;
                item.connectionConfig = options;
            });
            await SetGlobalTenantCache(list);
        }

        // 返回给前端 是否开启验证码 和 验证码长度
        return new { enableVerificationCode = sysConfig.enableVerificationCode, verificationCodeNumber = sysConfig.verificationCodeNumber > 0 ? sysConfig.verificationCodeNumber : 4 };
    }

    /// <summary>
    /// 获取当前登录用户信息.
    /// </summary>
    /// <param name="type">Web和App</param>
    /// <param name="systemCode">系统编码（应用独立URL）.</param>
    /// <returns></returns>
    [HttpGet("CurrentUser")]
    public async Task<dynamic> GetCurrentUser(string type, string systemCode)
    {
        var __sw = Stopwatch.StartNew();
        if (type.IsNullOrEmpty()) type = "Web"; // 默认为Web端菜单目录
        if (type.ToLower().Equals("app")) type = "App";
        if (type.ToLower().Equals("web") || type.ToLower().Equals("pc")) type = "Web";

        var userId = _userManager.UserId;
        var tenantId = _userManager.TenantId ?? "default";
        var currentUserCacheKey = $"CurrentUser:{tenantId}:{userId}:{type}";

        // P0-2: 优先读取缓存
        var cachedResult = await _cacheManager.GetAsync<CurrentUserOutput>(currentUserCacheKey);
        if (cachedResult != null)
        {
            __sw.Stop();
            Console.WriteLine($"[P0-2-TIMING] GetCurrentUser cache HIT: {__sw.ElapsedMilliseconds}ms");
            return cachedResult;
        }

        var loginOutput = new CurrentUserOutput();
        loginOutput.userInfo = await _userManager.GetUserInfo();
        var mainSysId = loginOutput.userInfo.systemId;
        var dataScope = loginOutput.userInfo.dataScope.Select(x => x.organizeId).Distinct().ToList();

        var sysId = string.Empty;
        if (systemCode.IsNotEmptyOrNull())
        {
            var entity = await _userRepository.AsSugarClient().Queryable<SystemEntity>().FirstAsync(it => it.DeleteMark == null && it.EnCode.Equals(systemCode));
            if (entity.IsNullOrEmpty())
                throw Oops.Oh(ErrorCode.D1042);
            if (!entity.EnabledMark.Equals(1))
                throw Oops.Oh(ErrorCode.D1043);
            sysId = entity.Id;
            loginOutput.userInfo.systemId = entity.Id;
            _userManager.User.SystemId = entity.Id;
        }

        // 根据多租户返回结果moduleIdList :[菜单id] 过滤菜单
        var noContainsMIdList = _userManager.TenantIgnoreModuleIdList;
        var noContainsMUrlList = _userManager.TenantIgnoreUrlAddressList;

        // 多租户菜单的协同办公
        if (noContainsMIdList.IsNotEmptyOrNull())
        {
            var wfId = await _userRepository.AsSugarClient().Queryable<ModuleEntity>().Where(it => it.DeleteMark == null && it.EnCode.Equals("workFlow")).Select(it => it.Id).FirstAsync();
            var allWf = await _userRepository.AsSugarClient().Queryable<ModuleEntity>().Where(it => it.DeleteMark == null && (it.ParentId.Equals(wfId) || it.Id.Equals(wfId))).ToListAsync();
            if (noContainsMIdList.Contains("-999"))
            {
                noContainsMIdList.AddRange(allWf.Select(it => it.Id));
                noContainsMUrlList.AddRange(allWf.Where(it => it.UrlAddress.IsNotEmptyOrNull()).Select(it => it.UrlAddress));
            }
            else if (noContainsMIdList.Intersect(allWf.Select(it => it.Id)).Any())
            {
                noContainsMIdList.RemoveAll(it => allWf.Select(it => it.Id).Contains(it));
                noContainsMUrlList.RemoveAll(it => allWf.Where(it => it.UrlAddress.IsNotEmptyOrNull()).Select(it => it.UrlAddress).Contains(it));
            }
        }

        // 应用系统
        loginOutput.userInfo.systemIds = await _userRepository.AsSugarClient().Queryable<SystemEntity>()
            .Where(x => x.DeleteMark == null && x.EnabledMark.Equals(1))
            .WhereIF(noContainsMIdList != null, x => !noContainsMIdList.Contains(x.Id))
            .WhereIF(type.Equals("App"), x => x.EnCode != "mainSystem")
            .OrderBy(x => x.SortCode).OrderByDescending(x => x.CreatorTime)
            .Select(x => new UserSystemModel()
            {
                id = x.Id,
                enCode = x.EnCode,
                name = x.FullName,
                icon = x.Icon,
                sortCode = x.SortCode,
                currentSystem = SqlFunc.Equals(mainSysId, x.Id),
                devSystem = SqlFunc.Equals(_userManager.BizSystemId,x.Id)
            }).ToListAsync();

        // 菜单
        __sw.Restart();
        loginOutput.menuList = (await _moduleService.GetUserModuleListByIds(type, sysId, noContainsMIdList, noContainsMUrlList)).ToTree("-1");
        Console.WriteLine($"[P0-1-TIMING] GetUserModuleListByIds (menu): {__sw.ElapsedMilliseconds}ms");

        var portalManageList = new List<PortalManageEntity>();
        var currSysId = _userManager.UserOrigin.Equals("pc") ? loginOutput.userInfo.systemId : loginOutput.userInfo.appSystemId;

        // 预取授权菜单 ID，避免循环内重复查询 AuthorizeEntity
        List<string> cachedModuleAuthorizeIds = null;
        if (!_userManager.IsAdministrator)
        {
            var pIds = _userManager.GetPermissionByUserId(_userManager.UserId);
            var sId = await _userRepository.AsSugarClient().Queryable<AuthorizeEntity>().Where(a => pIds.Contains(a.ObjectId)).Where(a => a.ItemType == "system").Select(a => a.ItemId).ToListAsync();

            if (pIds.Any())
            {
                cachedModuleAuthorizeIds = await _userRepository.AsSugarClient().Queryable<AuthorizeEntity>()
                    .Where(a => pIds.Contains(a.ObjectId)).Where(a => a.ItemType == "module")
                    .Select(a => a.ItemId).ToListAsync();
            }

            // 分管只捞取分管应用 (权限组授权的权限失效)
            if (loginOutput.userInfo.dataScope.Any(x => x.organizeType.IsNotEmptyOrNull()))
                sId = loginOutput.userInfo.dataScope.Where(x => x.organizeType.IsNotEmptyOrNull()).Select(x => x.organizeId).ToList();
            else if (_userRepository.AsSugarClient().Queryable<OrganizeAdministratorEntity>().Any(x => x.UserId.Equals(_userManager.UserId)))
                throw Oops.Oh(ErrorCode.D1038);

            loginOutput.userInfo.systemIds = loginOutput.userInfo.systemIds.Where(x => sId.Contains(x.id)).ToList();
            if (!pIds.Any() && !loginOutput.userInfo.systemIds.Any()) throw Oops.Oh(ErrorCode.D1038);
            if (systemCode.IsNotEmptyOrNull() && (!loginOutput.userInfo.systemIds.Any(x => x.id.Equals(sysId)) || !loginOutput.menuList.Any())) throw Oops.Oh(ErrorCode.D1038);

            if ((loginOutput.userInfo.systemIds.Any() && !loginOutput.userInfo.systemIds.Any(x => x.id.Equals(currSysId))) || currSysId.IsNullOrEmpty() || !loginOutput.menuList.Any())
            {
                if (loginOutput.userInfo.systemIds.Any())
                {
                    var defaultItem = loginOutput.userInfo.systemIds.FirstOrDefault();
                    if (_userManager.UserOrigin.Equals("pc"))
                    {
                        if (loginOutput.userInfo.systemIds.Any(x => x.currentSystem))
                            loginOutput.userInfo.systemIds.Find(x => x.currentSystem).currentSystem = false;
                        loginOutput.userInfo.systemId = defaultItem.id;
                        defaultItem.currentSystem = true;
                        await _userRepository.AsUpdateable().SetColumns(x => x.SystemId == loginOutput.userInfo.systemId).Where(x => x.Id.Equals(userId)).ExecuteCommandAsync();
                        _userManager.User.SystemId = loginOutput.userInfo.systemId;
                    }
                    else
                    {
                        if (loginOutput.userInfo.systemIds.Any(x => x.currentSystem))
                            loginOutput.userInfo.systemIds.Find(x => x.currentSystem).currentSystem = false;
                        loginOutput.userInfo.appSystemId = defaultItem.id;
                        defaultItem.currentSystem = true;
                        await _userRepository.AsUpdateable().SetColumns(x => x.AppSystemId == loginOutput.userInfo.appSystemId).Where(x => x.Id.Equals(userId)).ExecuteCommandAsync();
                        _userManager.User.AppSystemId = loginOutput.userInfo.systemId;
                    }

                    loginOutput.menuList = (await _moduleService.GetUserModuleListWithAuthIds(type, cachedModuleAuthorizeIds, noContainsMIdList, noContainsMUrlList)).ToTree("-1");
                    if (!loginOutput.menuList.Any())
                    {
                        for (var i = 1; i < loginOutput.userInfo.systemIds.Count; i++)
                        {
                            defaultItem = loginOutput.userInfo.systemIds[i];
                            if (_userManager.UserOrigin.Equals("pc"))
                            {
                                if (loginOutput.userInfo.systemIds.Any(x => x.currentSystem))
                                    loginOutput.userInfo.systemIds.Find(x => x.currentSystem).currentSystem = false;
                                loginOutput.userInfo.systemId = defaultItem.id;
                                defaultItem.currentSystem = true;
                                await _userRepository.AsUpdateable().SetColumns(x => x.SystemId == loginOutput.userInfo.systemId).Where(x => x.Id.Equals(userId)).ExecuteCommandAsync();
                                _userManager.User.SystemId = loginOutput.userInfo.systemId;
                            }
                            else
                            {
                                if (loginOutput.userInfo.systemIds.Any(x => x.currentSystem))
                                    loginOutput.userInfo.systemIds.Find(x => x.currentSystem).currentSystem = false;
                                loginOutput.userInfo.appSystemId = defaultItem.id;
                                defaultItem.currentSystem = true;
                                await _userRepository.AsUpdateable().SetColumns(x => x.AppSystemId == loginOutput.userInfo.appSystemId).Where(x => x.Id.Equals(userId)).ExecuteCommandAsync();
                                _userManager.User.AppSystemId = loginOutput.userInfo.systemId;
                            }

                            loginOutput.menuList = (await _moduleService.GetUserModuleListWithAuthIds(type, cachedModuleAuthorizeIds, noContainsMIdList, noContainsMUrlList)).ToTree("-1");
                            if (loginOutput.menuList.Any()) break;
                        }
                    }
                }
            }

            if (!loginOutput.userInfo.systemIds.Any()) loginOutput.menuList.Clear();

            // 授权的所有门户管理
            var portalManageIds = await _userRepository.AsSugarClient().Queryable<AuthorizeEntity>().In(a => a.ObjectId, pIds).Where(a => a.ItemType == "portalManage").GroupBy(it => it.ItemId).Select(it => it.ItemId).ToListAsync();

            // 授权的当前系统的所有门户管理
            portalManageList = await _userRepository.AsSugarClient().Queryable<PortalManageEntity, PortalEntity>((pm, p) => new JoinQueryInfos(JoinType.Left, pm.PortalId == p.Id))
                .Where((pm, p) => pm.EnabledMark == 1 && pm.DeleteMark == null && p.EnabledMark == 1 && p.DeleteMark == null)
                .Where(pm => portalManageIds.Contains(pm.Id))
                .Select<PortalManageEntity>()
                .ToListAsync();
        }
        else
        {
            if (!loginOutput.userInfo.systemIds.Any()) throw Oops.Oh(ErrorCode.D1038);
            if (systemCode.IsNotEmptyOrNull() && !loginOutput.menuList.Any()) throw Oops.Oh(ErrorCode.D1038);

            if ((loginOutput.userInfo.systemIds.Any() && !loginOutput.userInfo.systemIds.Any(x => x.id.Equals(currSysId))) || currSysId.IsNullOrEmpty() || !loginOutput.menuList.Any() || !await _userRepository.AsSugarClient().Queryable<SystemEntity>().AnyAsync(it => it.Id.Equals(currSysId) && it.DeleteMark == null && it.EnabledMark.Equals(1)))
            {
                switch (_userManager.UserOrigin)
                {
                    case "pc":
                        if (loginOutput.userInfo.systemIds.Any(x => x.currentSystem))
                            loginOutput.userInfo.systemIds.Find(x => x.currentSystem).currentSystem = false;
                        var defaultItem = loginOutput.userInfo.systemIds.Any(x => x.enCode.Equals("mainSystem")) ? loginOutput.userInfo.systemIds.FirstOrDefault(x => x.enCode.Equals("mainSystem")) : loginOutput.userInfo.systemIds.FirstOrDefault();
                        loginOutput.userInfo.systemId = defaultItem.id;
                        defaultItem.currentSystem = true;
                        await _userRepository.AsSugarClient().Updateable<UserEntity>().SetColumns(it => new UserEntity() { SystemId = loginOutput.userInfo.systemId }).Where(it => it.Id == _userManager.User.Id).ExecuteCommandAsync();
                        _userManager.User.SystemId = loginOutput.userInfo.systemId;
                        break;
                    case "app":
                        if (loginOutput.userInfo.systemIds.Any(x => x.currentSystem))
                            loginOutput.userInfo.systemIds.Find(x => x.currentSystem).currentSystem = false;
                        defaultItem = loginOutput.userInfo.systemIds.FirstOrDefault();
                        loginOutput.userInfo.appSystemId = defaultItem.id;
                        defaultItem.currentSystem = true;
                        await _userRepository.AsSugarClient().Updateable<UserEntity>().SetColumns(it => new UserEntity() { AppSystemId = loginOutput.userInfo.appSystemId }).Where(it => it.Id == _userManager.User.Id).ExecuteCommandAsync();
                        _userManager.User.AppSystemId = loginOutput.userInfo.appSystemId;
                        break;
                }

                loginOutput.menuList = (await _moduleService.GetUserModuleListByIds(type, string.Empty, noContainsMIdList, noContainsMUrlList)).ToTree("-1");
            }

            // 当前系统的所有门户管理
            portalManageList = await _userRepository.AsSugarClient().Queryable<PortalManageEntity, PortalEntity>((pm, p) => new JoinQueryInfos(JoinType.Left, pm.PortalId == p.Id))
                .Where((pm, p) => pm.EnabledMark == 1 && pm.DeleteMark == null && p.EnabledMark == 1 && p.DeleteMark == null)
                .Select<PortalManageEntity>()
                .ToListAsync();
        }
        var webPortalManageList = portalManageList.Where(it => it.SystemId.Equals(loginOutput.userInfo.systemId)).ToList();
        var appPortalManageList = portalManageList.Where(it => it.SystemId.Equals(loginOutput.userInfo.appSystemId)).ToList();
        var portalId = loginOutput.userInfo.portalId;
        loginOutput.userInfo.portalId = await GetPortalId(webPortalManageList, portalId, loginOutput.userInfo.systemId, loginOutput.userInfo.userId, "Web");
        loginOutput.userInfo.appPortalId = await GetPortalId(appPortalManageList, portalId, loginOutput.userInfo.appSystemId, loginOutput.userInfo.userId, "App");

        var currentUserModel = new CurrentUserModelOutput();
        var __sw2 = Stopwatch.StartNew();
        currentUserModel.moduleList = (await _moduleService.GetUserModuleListByIds(type, sysId, noContainsMIdList, noContainsMUrlList)).Adapt<List<ModuleOutput>>();
        Console.WriteLine($"[P0-1-TIMING] GetUserModuleListByIds (permission): {__sw2.ElapsedMilliseconds}ms");
        __sw2.Restart();
        var dataScopeModuleIds = await _userRepository.AsSugarClient().Queryable<ModuleEntity>().Where(x => dataScope.Contains(x.SystemId)).Select(x => x.Id).ToListAsync();
        currentUserModel.buttonList = await _moduleButtonService.GetUserModuleButtonList(dataScopeModuleIds);
        currentUserModel.columnList = await _columnService.GetUserModuleColumnList(dataScopeModuleIds);
        currentUserModel.resourceList = await _moduleDataAuthorizeSchemeService.GetResourceList(dataScopeModuleIds);
        currentUserModel.formList = await _formService.GetUserModuleFormList(dataScopeModuleIds);
        Console.WriteLine($"[P0-1-TIMING] Permission queries (button+column+resource+form): {__sw2.ElapsedMilliseconds}ms");

        // 权限信息
        var permissionList = new List<PermissionModel>();
        currentUserModel.moduleList.ForEach(menu =>
        {
            var permissionModel = new PermissionModel();
            permissionModel.modelId = menu.id;
            permissionModel.moduleName = menu.fullName;
            permissionModel.button = currentUserModel.buttonList.FindAll(t => t.moduleId.Equals(menu.id)).Adapt<List<FunctionalButtonAuthorizeModel>>();
            permissionModel.column = currentUserModel.columnList.FindAll(t => t.moduleId.Equals(menu.id)).Adapt<List<FunctionalColumnAuthorizeModel>>();
            permissionModel.form = currentUserModel.formList.FindAll(t => t.moduleId.Equals(menu.id)).Adapt<List<FunctionalFormAuthorizeModel>>();
            permissionModel.resource = currentUserModel.resourceList.FindAll(t => t.moduleId.Equals(menu.id)).Adapt<List<FunctionalResourceAuthorizeModel>>();
            permissionList.Add(permissionModel);
        });

        loginOutput.permissionList = permissionList;

        // 系统配置信息
        if (_userManager.User.SystemId.IsNotEmptyOrNull())
        {
            var sysInfo = await _sysConfigService.GetInfo();

            var icon = await _userRepository.AsSugarClient().Queryable<SystemEntity>()
                .Where(it => it.DeleteMark == null && it.Id.Equals(_userManager.User.SystemId))
                .Select(it => new {
                    it.NavigationIcon,
                    it.WorkLogoIcon
                })
                .FirstAsync();
            if (icon.IsNotEmptyOrNull())
            {
                sysInfo.navigationIcon = icon.NavigationIcon;
                sysInfo.workLogoIcon = icon.WorkLogoIcon;
            }

            loginOutput.sysConfigInfo = sysInfo.Adapt<SysConfigInfo>();
        }

        loginOutput.sysConfigInfo.jnpfDomain = GetLocalAddress();

        __sw.Stop();
        Console.WriteLine($"[P0-1-TIMING] GetCurrentUser elapsed: {__sw.ElapsedMilliseconds}ms");

        // P0-2: 写入缓存（30分钟过期）
        await _cacheManager.SetAsync(currentUserCacheKey, loginOutput, TimeSpan.FromMinutes(30));

        return loginOutput;
    }

    /// <summary>
    /// 退出.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Logout")]
    public async Task Logout([FromQuery] string ticket)
    {
        var tenantId = _userManager.TenantId ?? "default";
        var userId = _userManager.UserId ?? "admim";

        // 日志参数
        Stopwatch sw = new Stopwatch();
        sw.Start();
        var logUserName = _userManager.RealName.IsNotEmptyOrNull() && _userManager.Account.IsNotEmptyOrNull() ? string.Format("{0}/{1}", _userManager.RealName, _userManager.Account) : string.Empty;
        UserAgent userAgent = new UserAgent(App.HttpContext);

        var httpContext = _httpContextAccessor.HttpContext;
        httpContext.SignoutToSwagger();

        // 增加退出日志
        sw.Stop();
        await AddLoginLog(tenantId, _userManager.User, logUserName, "password", userAgent, (int)sw.ElapsedMilliseconds, 1, 1, "退出成功");

        // 清除IM中的webSocket
        var list = await GetOnlineUserList(tenantId);
        if (list != null)
        {
            var isApp = _userManager.UserOrigin.Equals("app");
            var onlineUser = list.Find(it => it.tenantId == tenantId && it.userId == userId && it.isMobileDevice.Equals(isApp));
            if (onlineUser != null)
            {
                list.RemoveAll((x) => x.connectionId == onlineUser.connectionId);
                await SetOnlineUserList(list, tenantId);
            }
        }

        await DelUserInfo(tenantId, userId);
    }

    /// <summary>
    /// 密码重置.
    /// </summary>
    /// <param name="mobile"></param>
    /// <param name="smsCode"></param>
    /// <returns></returns>
    [HttpGet("resetOfficialPassword/{mobile}/{smsCode}")]
    [AllowAnonymous]
    [LogPolicy(LogPolicy.IgnoreAll)]
    public async Task ResetOfficialPassword(string mobile, string smsCode)
    {
        var apiUrl = string.Format("{0}/Tenant/ResetPasswordSmsCodeCheck/{1}/{2}", _tenant.MultiTenancyDBInterFace.Split("/Tenant").First(), mobile, smsCode);

        // 请求接口 验证
        var apiResponse = await apiUrl.SetHeaders(new Dictionary<string, object> {
                { "X-Forwarded-For", NetHelper.Ip}
            }).GetAsStringAsync();
        var resObj = apiResponse.ToObject<RESTfulResult<TenantInterFaceOutput>>();
        if (resObj == null || resObj.code != 200) throw Oops.Oh(resObj?.msg);

        var defaultConnection = _connectionStrings.DefaultConnectionConfig;
        ConnectionConfigOptions options = JNPFTenantExtensions.GetLinkToOrdinary(defaultConnection.ConfigId.ToString(), defaultConnection.DBName);
        UserAgent userAgent = new UserAgent(App.HttpContext);
        string tenantAccout = string.Empty;
        string tenantId = defaultConnection.ConfigId.ToString();
        if (_tenant.MultiTenancy)
        {
            // 分割账号
            var tenantAccount = mobile.Split('@');
            tenantId = tenantAccount.FirstOrDefault();
            if (tenantAccount.Length == 1) mobile = "admin";
            else mobile = tenantAccount[1];
            tenantAccout = mobile;
            await _tenantManager.ChangTenant(_sqlSugarClient, tenantId, false);
        }

        // 验证连接是否成功
        if (!_sqlSugarClient.Ado.IsValidConnection()) throw Oops.Oh(ErrorCode.D1032);

        var user = await _sqlSugarClient.Queryable<UserEntity>().FirstAsync(it => it.Account.Equals(mobile) && it.DeleteMark == null);
        _ = user ?? throw Oops.Oh(ErrorCode.D1000);
        user.Password = MD5Encryption.Encrypt(MD5Encryption.Encrypt("123456") + user.Secretkey);

        await _sqlSugarClient.Updateable(user).UpdateColumns(it => new { it.Password }).ExecuteCommandAsync();
    }


    [HttpGet("GetTenantInfo")]
    [AllowAnonymous]
    [LogPolicy(LogPolicy.IgnoreRequest)]
    public async Task<dynamic> GetTenantInfo()
    {
        string domain = _httpContextAccessor.HttpContext.Request.Host.Value.ToString();

        var apiUrl = string.Format("{0}/api/tenant/gettenantinfo", "https://tenant.zhixuan.cloud");

        // 请求接口 验证
        var apiResponse = await apiUrl.SetHeaders(new Dictionary<string, object> {
                { "X-Forwarded-For", NetHelper.Ip}
        }).SetQueries(new Dictionary<string, string> {
                { "domain",domain },{"encode","" }
            }
            ).GetAsStringAsync();

        var resObj = apiResponse.ToObject<RESTfulResult<object>>();
        if (resObj == null || resObj.code != 200) throw Oops.Oh(resObj?.msg);



        return resObj.data;

    }

    #endregion

    #region POST

    /// <summary>
    /// 用户登录.
    /// </summary>
    /// <param name="input">登录输入参数.</param>
    /// <returns></returns>
    [HttpPost("Login")]
    [Consumes("application/x-www-form-urlencoded")]
    [AllowAnonymous]
    [LogPolicy(LogPolicy.IgnoreRequest)]
    public async Task<dynamic> Login([FromForm] LoginInput input)
    {
        // 普通登录 密码 AES 解密.
        if (!input.isSocialsLoginCallBack && (input.grant_type.IsNullOrEmpty() || !input.grant_type.Equals("official")))
            input.password = AESEncryption.AesDecrypt(input.password, App.GetConfig<AppOptions>("JNPF_App", true).AesKey);

        var defaultConnection = _connectionStrings.DefaultConnectionConfig;
        ConnectionConfigOptions options = JNPFTenantExtensions.GetLinkToOrdinary(defaultConnection.ConfigId.ToString(), defaultConnection.DBName);
        UserAgent userAgent = new UserAgent(App.HttpContext);
        string tenantAccout = string.Empty;
        string tenantId = defaultConnection.ConfigId.ToString();
        string oldAccount = input.account;
        var tenantInterFaceOutput = new TenantInterFaceOutput();
        var loginType = 0;

        // 域名租户
        if (_tenant.MultiTenancy && tenantId.Equals("default") && App.HttpContext.Request.Headers["origin"].IsNotEmptyOrNull())
        {
            // 本地url地址
            var address = _userManager.UserOrigin.Equals("pc") ? App.Configuration["Message:DoMainPc"] : App.Configuration["Message:DoMainApp"];
            address = address.Replace("https://", string.Empty).Replace("http://", string.Empty).Replace("www.", string.Empty);
            var host = App.HttpContext.Request.Headers["origin"].ToString().Replace("https://", string.Empty).Replace("http://", string.Empty).Replace("www.", string.Empty);

            if (host.Contains(address) && !host.Equals(address))
            {
                tenantId = host.Split(".").FirstOrDefault();
                input.account = string.Format("{0}@{1}", tenantId, input.account);
            }
        }

        if (_tenant.MultiTenancy)
        {
            // 分割账号
            var tenantAccount = input.account.Split('@');
            tenantId = tenantAccount.FirstOrDefault();
            if (tenantAccount.Length == 1)
                input.account = "admin";
            else
                input.account = tenantAccount[1];
            tenantAccout = input.account;

            if (input.socialsOptions == null)
            {
                tenantInterFaceOutput = await _tenantManager.ChangTenant(_sqlSugarClient, tenantId, false);
                options = tenantInterFaceOutput.options;
            }
            else
            {
                options = input.socialsOptions;
            }
        }

        // 验证连接是否成功
        if (!_sqlSugarClient.Ado.IsValidConnection()) throw Oops.Oh(ErrorCode.D1032);

        Stopwatch sw = new Stopwatch();
        sw.Start();

        // 读取配置文件
        var array = new Dictionary<string, string>();
        var sysConfigData = await _sqlSugarClient.Queryable<SysConfigEntity>().Where(x => x.Category.Equals("SysConfig")).ToListAsync();
        foreach (var item in sysConfigData)
        {
            if (!array.ContainsKey(item.Key)) array.Add(item.Key, item.Value);
        }

        var sysConfig = array.ToObject<SysConfigByOAuthModel>();

        // 根据用户账号获取用户秘钥
        var user = await _sqlSugarClient.Queryable<UserEntity>().FirstAsync(it => it.Account.Equals(input.account) && it.DeleteMark == null);
        var logUserName = user.IsNotEmptyOrNull() ? string.Format("{0}/{1}", user.RealName, user.Account) : input.account;

        try
        {
            var ip = NetHelper.Ip;

            // 官网授权验证(手机短信等方式)
            if (input.grant_type.IsNotEmptyOrNull() && input.grant_type.Equals("official"))
            {
                var apiUrl = string.Format("{0}/Tenant/LoginSmsCodeCheck/{1}/{2}", _tenant.MultiTenancyDBInterFace.Split("/Tenant").First(), oldAccount, input.code);

                // 请求接口 验证
                var resStr = await apiUrl.SetHeaders(new Dictionary<string, object> {
                { "X-Forwarded-For", ip}
            }).GetAsStringAsync();
                var resObj = resStr.ToObject<RESTfulResult<TenantInterFaceOutput>>();
                if (resObj == null || resObj.code != 200)
                {
                    await AddLoginLog(apiUrl, user, logUserName, input.grant_type, userAgent, (int)sw.ElapsedMilliseconds, loginType, 0, resObj.ToJsonString());
                    throw Oops.Oh(resObj?.msg);
                }
                input.password = await _sqlSugarClient.Queryable<UserEntity>().Where(it => it.Account.Equals(input.account) && it.DeleteMark == null).Select(x => x.Password).FirstAsync();
            }
            else
            {
                // 开启验证码验证
                if (sysConfig.enableVerificationCode)
                {
                    if (string.IsNullOrWhiteSpace(input.timestamp) || string.IsNullOrWhiteSpace(input.code))
                        throw Oops.Oh(ErrorCode.D1029);
                    string imageCode = await GetCode(input.timestamp);
                    if (imageCode.IsNullOrEmpty())
                        throw Oops.Oh(ErrorCode.D1030);
                    if (!input.code.ToLower().Equals(imageCode.ToLower()))
                        throw Oops.Oh(ErrorCode.D1029);
                }
            }

            if (user.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.D1000);

            // 验证账号是否未被激活
            if (user.EnabledMark == null) throw Oops.Oh(ErrorCode.D1018);

            // 验证账号是否被禁用
            if (user.EnabledMark == 0) throw Oops.Oh(ErrorCode.D1019);

            // 是否延迟登录
            if (sysConfig.lockType.Equals(ErrorStrategy.Delay) && user.UnLockTime.IsNullOrEmpty())
            {
                if (user.UnLockTime > DateTime.Now)
                {
                    int unlockTime = ((user.UnLockTime - DateTime.Now)?.TotalMinutes).ParseToInt();
                    if (unlockTime < 1) unlockTime = 1;
                    throw Oops.Oh(ErrorCode.D1027, unlockTime);
                }
                else if (user.UnLockTime <= DateTime.Now)
                {
                    user.EnabledMark = 1;
                    user.LogErrorCount = 0;
                    await _sqlSugarClient.Updateable(user).UpdateColumns(it => new { it.LogErrorCount, it.EnabledMark }).ExecuteCommandAsync();
                }
            }

            // 是否 延迟登录
            if (sysConfig.lockType.Equals(ErrorStrategy.Delay) && user.UnLockTime.IsNotEmptyOrNull() && user.UnLockTime > DateTime.Now)
            {
                int? t3 = (user.UnLockTime - DateTime.Now)?.TotalMinutes.ParseToInt();
                if (t3 < 1) t3 = 1;
                throw Oops.Oh(ErrorCode.D1027, t3?.ToString());
            }

            if (sysConfig.lockType.Equals(ErrorStrategy.Delay) && user.UnLockTime.IsNotEmptyOrNull() && user.UnLockTime <= DateTime.Now)
            {
                user.EnabledMark = 1;
                user.LogErrorCount = 0;
                await _sqlSugarClient.Updateable(user).UpdateColumns(it => new { it.LogErrorCount, it.EnabledMark }).ExecuteCommandAsync();
            }

            // 是否锁定
            if (user.EnabledMark == 2) throw Oops.Oh(ErrorCode.D1031);

            // 获取加密后的密码
            var encryptPasswod = MD5Encryption.Encrypt(input.password + user.Secretkey);
            if (input.isSocialsLoginCallBack || (input.grant_type.IsNotEmptyOrNull() && input.grant_type.Equals("official"))) encryptPasswod = input.password;

            // 账户密码是否匹配
            var userAnyPwd = await _sqlSugarClient.Queryable<UserEntity>().FirstAsync(u => u.Account == input.account && u.Password == encryptPasswod && u.DeleteMark == null);
            if (userAnyPwd.IsNullOrEmpty())
            {
                // 如果是密码错误 记录账号的密码错误次数
                await UpdateErrorLog(user, sysConfig);
            }
            else
            {
                // 清空记录错误次数
                userAnyPwd.LogErrorCount = 0;

                // 解除锁定
                userAnyPwd.EnabledMark = 1;
                await _sqlSugarClient.Updateable(userAnyPwd).UpdateColumns(it => new { it.LogErrorCount, it.EnabledMark }).ExecuteCommandAsync();
            }

            if (userAnyPwd.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.D1000);

            // 登录成功时 判断单点登录信息
            int whitelistSwitch = Convert.ToInt32(sysConfig.whitelistSwitch);
            string whiteListIp = sysConfig.whiteListIp;
            if (whitelistSwitch.Equals(1) && user.IsAdministrator.Equals(0) && !whiteListIp.Split(",").Contains(ip))
                throw Oops.Oh(ErrorCode.D9002);

            // token过期时间
            long tokenTimeout = sysConfig.tokenTimeout;

            // 生成Token令牌
            string accessToken = JWTEncryption.Encrypt(
                    new Dictionary<string, object>
                    {
                    { ClaimConst.CLAINMUSERID, userAnyPwd.Id },
                    { ClaimConst.CLAINMACCOUNT, userAnyPwd.Account },
                    { ClaimConst.CLAINMREALNAME, userAnyPwd.RealName },
                    { ClaimConst.CLAINMADMINISTRATOR, userAnyPwd.IsAdministrator },
                    { ClaimConst.TENANTID, tenantId},
                    { ClaimConst.OnlineTicket, input.online_ticket },
                    { ClaimConst.ZXSYSTEMID, userAnyPwd.BizSystemId }
                    }, tokenTimeout);

            string configId = _sqlSugarClient.CurrentConnectionConfig.ConfigId.ToString();
            _cacheManager.Set(configId + _userManager.UserId + "_devSystemId", userAnyPwd.BizSystemId);

            // 单点登录标识缓存
            if (_oauthOptions.Enabled) _cacheManager.Set("OnlineTicket_" + input.online_ticket, options.ConfigId);

            // 设置Swagger自动登录
            _httpContextAccessor.HttpContext.SigninToSwagger(accessToken);

            // 设置刷新Token令牌
            _httpContextAccessor.HttpContext.Response.Headers["x-access-token"] = JWTEncryption.GenerateRefreshToken(accessToken, 30); // 生成刷新Token令牌

            /*
            * 登录完成后添加全局租户缓存
            * 判断当前租户是否存在缓存
            * 不存在添加缓存
            * 存在更新缓存
            */
            if (!await IsAnyByTenantIdAsync(tenantId))
            {
                List<GlobalTenantCacheModel>? list = _userManager.GetGlobalTenantCache();
                list.Add(new GlobalTenantCacheModel
                {
                    TenantId = tenantId,
                    SingleLogin = (int)sysConfig.singleLogin,
                    connectionConfig = options,
                    type = tenantInterFaceOutput.type,
                    tenantName = tenantInterFaceOutput.tenantName,
                    validTime = tenantInterFaceOutput.validTime,
                    domain = tenantInterFaceOutput.domain,
                    accountNum = tenantInterFaceOutput.accountNum,
                    moduleIdList = tenantInterFaceOutput.moduleIdList,
                    urlAddressList = tenantInterFaceOutput.urlAddressList,
                    unitInfoJson = tenantInterFaceOutput.unitInfoJson,
                    userInfoJson = tenantInterFaceOutput.userInfoJson,
                });
                await SetGlobalTenantCache(list);
            }
            else
            {
                List<GlobalTenantCacheModel>? list = _userManager.GetGlobalTenantCache();
                list.FindAll(it => it.TenantId.Equals(tenantId)).ForEach(item =>
                {
                    item.TenantId = tenantId;
                    item.SingleLogin = (int)sysConfig.singleLogin;
                    item.connectionConfig = options;
                    item.type = tenantInterFaceOutput.type;
                    item.tenantName = tenantInterFaceOutput.tenantName;
                    item.validTime = tenantInterFaceOutput.validTime;
                    item.domain = tenantInterFaceOutput.domain;
                    item.accountNum = tenantInterFaceOutput.accountNum;
                    item.moduleIdList = tenantInterFaceOutput.moduleIdList;
                    item.urlAddressList = tenantInterFaceOutput.urlAddressList;
                    item.unitInfoJson = tenantInterFaceOutput.unitInfoJson;
                    item.userInfoJson = tenantInterFaceOutput.userInfoJson;
                });
                await SetGlobalTenantCache(list);
            }

            // 修改用户登录信息
            await _eventPublisher.PublishAsync(new UserEventSource("User:UpdateUserLogin", tenantId, new UserEntity
            {
                Id = user.Id,
                FirstLogIP = user.FirstLogIP ?? ip,
                FirstLogTime = user.FirstLogTime ?? DateTime.Now,
                PrevLogTime = user.LastLogTime,
                PrevLogIP = user.LastLogIP,
                LastLogTime = DateTime.Now,
                LastLogIP = ip,
                LogSuccessCount = user.LogSuccessCount + 1
            }));

            // 增加登录日志
            sw.Stop();
            await AddLoginLog(tenantId, user, logUserName, input.grant_type, userAgent, (int)sw.ElapsedMilliseconds, loginType, 1, "登录成功");

            if (input.jnpf_ticket.IsNotEmptyOrNull())
            {
                var ticket = await _cacheManager.GetAsync<SocialsLoginTicketModel>(input.jnpf_ticket);
                if (ticket.IsNotEmptyOrNull())
                {
                    var socialsEntity = ticket.value.ToObject<SocialsUsersEntity>();
                    var sInfo = await _userRepository.AsSugarClient().Queryable<SocialsUsersEntity>().Where(x => (x.SocialId.Equals(socialsEntity.SocialId) || x.UserId.Equals(user.Id)) && x.SocialType.Equals(socialsEntity.SocialType) && x.DeleteMark == null).FirstAsync();
                    if (sInfo == null)
                    {
                        var socialsUserEntity = new SocialsUsersEntity();
                        socialsUserEntity.UserId = user.Id;
                        socialsUserEntity.SocialType = socialsEntity.SocialType.ToLower();
                        socialsUserEntity.SocialName = socialsEntity.SocialName;
                        socialsUserEntity.SocialId = socialsEntity.SocialId;
                        await _userRepository.AsSugarClient().Insertable(socialsUserEntity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();

                        // 租户开启时-添加租户库绑定数据
                        if (_tenant.MultiTenancy)
                        {
                            var info = await _userRepository.AsSugarClient().Queryable<UserEntity>().FirstAsync(x => x.Id.Equals(user.Id));
                            var param = socialsUserEntity.ToObject<Dictionary<string, string>>();
                            if (param.ContainsKey("SocialType")) param["SocialType"] = param["SocialType"].ToLower();
                            param.Add("tenantId", tenantId);
                            param.Add("account", info.Account);
                            param.Add("accountName", info.RealName + "/" + info.Account);

                            var postUrl = _tenant.MultiTenancyDBInterFace + "socials";
                            var result = (await postUrl.SetHeaders(new Dictionary<string, object> {
                            { "X-Forwarded-For", NetHelper.Ip}
                        }).SetBody(param).PostAsStringAsync()).ToObject<Dictionary<string, string>>();

                            if (result == null || "500".Equals(result["code"]) || "400".Equals(result["code"]))
                            {
                                return new { code = 201, message = "用户租户绑定错误!" }.ToJsonString();
                            }
                        }
                    }
                }
            }

            return new {
                theme = user.Theme == null ? "classic" : user.Theme,
                token = string.Format("Bearer {0}", accessToken),
                wl_qrcode = tenantInterFaceOutput.wl_qrcode
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            await AddLoginLog(tenantId, user, logUserName, input.grant_type, userAgent, (int)sw.ElapsedMilliseconds, loginType, 0, ex.Message);
            throw Oops.Bah(ex.Message);
        }
    }

    /// <summary>
    /// 锁屏解锁登录.
    /// </summary>
    /// <param name="input">登录输入参数.</param>
    /// <returns></returns>
    [HttpPost("LockScreen")]
    public async Task LockScreen([FromBody] LockScreenInput input)
    {
        // 根据用户账号获取用户秘钥
        var secretkey = (await _userRepository.GetFirstAsync(u => u.Account == input.account && u.DeleteMark == null)).Secretkey;

        // 获取加密后的密码
        var encryptPasswod = MD5Encryption.Encrypt(input.password + secretkey);

        var user = await _userRepository.GetFirstAsync(u => u.Account == input.account && u.Password == encryptPasswod && u.DeleteMark == null);
        _ = user ?? throw Oops.Oh(ErrorCode.D1000);
    }

    /// <summary>
    /// 注销用户.
    /// </summary>
    /// <returns></returns>
    [HttpPost("logoutCurrentUser")]
    [NonUnify]
    public async Task<dynamic> LogoutCurrentUser()
    {
        var userInfo = _userManager.User;
        if (userInfo.IsAdministrator.Equals(1)) throw Oops.Oh(ErrorCode.D1034);
        userInfo.DeleteMark = 1;
        userInfo.DeleteTime = DateTime.Now;
        userInfo.DeleteUserId = userInfo.Id;
        await _userRepository.AsUpdateable(userInfo).ExecuteCommandAsync();
        return new { code = 200, msg = "注销成功" };
    }

    /// <summary>
    /// 单点登录退出.
    /// </summary>
    /// <returns></returns>
    [HttpPost("Logout/auth2")]
    [AllowAnonymous]
    public async Task OnlineLogout()
    {
        var ticket = _httpContextAccessor.HttpContext.Request.Form["ticket"];
        var tenantId = await _cacheManager.GetAsync("OnlineTicket_" + ticket);
        if (ticket.IsNotEmptyOrNull())
        {
            await _cacheManager.DelAsync("OnlineTicket_" + ticket);
            var userId = _userManager.GetAdminUserId();
            var userOnlineList = new List<UserOnlineModel>();
            userOnlineList = await GetOnlineUserList(tenantId);
            var userOnline = userOnlineList.Find(x => x.onlineTicket.Equals(ticket));
            if (userOnline != null)
            {
                userId = userOnline.userId;
                await _messageManager.ForcedOffline(userOnline.connectionId);
            }

            // 清除IM中的webSocket
            if (userOnlineList != null)
            {
                var onlineUser = userOnlineList.Find(it => it.tenantId == tenantId && it.userId == userId);
                if (onlineUser != null)
                {
                    userOnlineList.RemoveAll((x) => x.connectionId == onlineUser.connectionId);
                    await SetOnlineUserList(userOnlineList, tenantId);
                }
            }

            await DelUserInfo(tenantId, userId);
        }
    }

    /// <summary>
    /// 密码过期提醒.
    /// </summary>
    /// <returns></returns>
    [HttpPost("updatePasswordMessage")]
    public async Task PwdMessage()
    {
        var sysConfigInfo = await _sysConfigService.GetInfo();
        // 密码修改时间.
        var changePasswordDate = _userManager.User.ChangePasswordDate.IsNullOrEmpty() ? _userManager.User.CreatorTime : _userManager.User.ChangePasswordDate;
        // 提醒时间
        var remindDate = changePasswordDate.ParseToDateTime().AddDays(sysConfigInfo.updateCycle - sysConfigInfo.updateInAdvance);
        if (sysConfigInfo.passwordIsUpdatedRegularly == 1 && remindDate < DateTime.Now)
        {
            var paramsDic = new Dictionary<string, string>();
            paramsDic.Add("@Title", "");
            paramsDic.Add("@CreatorUserName", _userManager.GetUserName(_userManager.UserId));
            var messageList = _messageManager.GetMessageList("XTXXTX001", new List<string> { _userManager.UserId }, paramsDic, 3);
            await _messageManager.SendDefaultMsg(new List<string> { _userManager.UserId }, messageList);
        }
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 获取验证码.
    /// </summary>
    /// <param name="timestamp">时间戳.</param>
    /// <returns></returns>
    private async Task<string> GetCode(string timestamp)
    {
        string cacheKey = string.Format("{0}{1}", CommonConst.CACHEKEYCODE, timestamp);
        return await _cacheManager.GetAsync<string>(cacheKey);
    }

    /// <summary>
    /// 判断app用户角色是否存在且有效.
    /// </summary>
    /// <param name="roleIds"></param>
    /// <returns></returns>
    private bool ExistRoleByApp(string roleIds)
    {
        if (roleIds.IsEmpty())
            return false;
        var roleIdList1 = roleIds.Split(",").ToList();
        var roleIdList2 = _sqlSugarClient.Queryable<RoleEntity>().Where(x => x.DeleteMark == null && x.EnabledMark == 1).Select(x => x.Id).ToList();
        return roleIdList1.Intersect(roleIdList2).ToList().Count > 0;
    }

    /// <summary>
    /// 记录密码错误次数.
    /// </summary>
    /// <param name="entity">用户实体.</param>
    /// <param name="sysConfigOutput">系统配置输出.</param>
    /// <returns></returns>
    private async Task UpdateErrorLog(UserEntity entity, SysConfigByOAuthModel sysConfigOutput)
    {
        if (entity != null)
        {
            if (entity.EnabledMark.Equals(1) && !entity.Account.ToLower().Equals("admin") && sysConfigOutput.lockType > 0 && sysConfigOutput.passwordErrorsNumber > 2)
            {

                switch (sysConfigOutput.lockType)
                {
                    case ErrorStrategy.Lock:
                        entity.EnabledMark = entity.LogErrorCount >= sysConfigOutput.passwordErrorsNumber - 1 ? 2 : 1;
                        break;
                    case ErrorStrategy.Delay:
                        entity.UnLockTime = entity.LogErrorCount >= sysConfigOutput.passwordErrorsNumber - 1 ? DateTime.Now.AddMinutes(sysConfigOutput.lockTime) : null;
                        entity.EnabledMark = entity.LogErrorCount >= sysConfigOutput.passwordErrorsNumber - 1 ? 2 : 1;
                        break;
                }

                entity.LogErrorCount++;

                await _sqlSugarClient.Updateable(entity).UpdateColumns(it => new { it.EnabledMark, it.UnLockTime, it.LogErrorCount }).ExecuteCommandAsync();
            }
        }
    }

    /// <summary>
    /// 获取在线用户列表.
    /// </summary>
    /// <returns></returns>
    private async Task<List<UserOnlineModel>> GetOnlineUserList(string tenantId)
    {
        string cacheKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, tenantId);
        return await _cacheManager.GetAsync<List<UserOnlineModel>>(cacheKey);
    }

    /// <summary>
    /// 保存在线用户列表.
    /// </summary>
    /// <param name="onlineList">在线用户列表.</param>
    /// <returns></returns>
    private async Task<bool> SetOnlineUserList(List<UserOnlineModel> onlineList, string tenantId)
    {
        string cacheKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, tenantId);
        return await _cacheManager.SetAsync(cacheKey, onlineList);
    }

    /// <summary>
    /// 删除用户登录信息缓存.
    /// </summary>
    private async Task<bool> DelUserInfo(string tenantId, string userId)
    {
        string cacheKey = string.Format("{0}:{1}:{2}", tenantId, CommonConst.CACHEKEYUSER, userId);
        // P0-2: 同步清除 CurrentUser 缓存（Web + App）
        await _cacheManager.DelAsync($"CurrentUser:{tenantId}:{userId}:Web");
        await _cacheManager.DelAsync($"CurrentUser:{tenantId}:{userId}:App");
        return await _cacheManager.DelAsync(cacheKey);
    }

    /// <summary>
    /// 是否存在租户缓存.
    /// </summary>
    /// <param name="tenantId">租户id.</param>
    /// <returns></returns>
    private async Task<bool> IsAnyByTenantIdAsync(string tenantId)
    {
        string cacheKey = string.Format("{0}", CommonConst.GLOBALTENANT);
        var list = await _cacheManager.GetAsync<List<GlobalTenantCacheModel>>(cacheKey);
        return list != null ? list.Any(it => it.TenantId.Equals(tenantId)) : false;
    }

    /// <summary>
    /// 保存全局租户缓存.
    /// </summary>
    /// <returns></returns>
    private async Task<bool> SetGlobalTenantCache(List<GlobalTenantCacheModel> list)
    {
        string cacheKey = string.Format("{0}", CommonConst.GLOBALTENANT);
        return await _cacheManager.SetAsync(cacheKey, list);
    }

    /// <summary>
    /// 组装用户所有菜单.
    /// </summary>
    /// <param name="menuList"></param>
    /// <returns></returns>
    private List<UserAllMenu> GetUserAllMenu(List<ModuleNodeOutput> menuList)
    {
        var result = new List<UserAllMenu>();
        menuList.ForEach(item =>
        {
            var menu = item.Adapt<UserAllMenu>();
            if (menu.children != null && menu.children.Any())
            {
                menu.hasChildren = true;
                menu.children = GetUserAllMenu(menu.children.Adapt<List<ModuleNodeOutput>>());
            }

            result.Add(menu);
        });

        return result;
    }

    /// <summary>
    /// 获取门户id.
    /// </summary>
    /// <param name="entityList">当前系统的所有门户管理.</param>
    /// <param name="portalId">用户表门户ID.</param>
    /// <param name="systemId">当前系统ID.</param>
    /// <param name="userId">当前用户ID.</param>
    /// <param name="type">门户类型（web、app）.</param>
    /// <returns></returns>
    private async Task<string> GetPortalId(List<PortalManageEntity> entityList, string portalId, string systemId, string userId, string type)
    {
        var portalDic = new Dictionary<string, string>();

        if (type.Equals("App"))
        {
            portalId = await _userRepository.AsQueryable()
                .Where(it => it.Id.Equals(userId))
                .Select(it => it.PortalId)
                .FirstAsync();
        }

        if (portalId.IsNotEmptyOrNull())
        {
            // 目前系统的门户
            if (portalId.Contains("{"))
            {
                portalDic = portalId.ToObject<Dictionary<string, string>>();
            }
        }

        var portalIds = entityList
            .Where(it => it.Platform != null && it.Platform.Equals(type))
            .Select(it => it.PortalId)
            .ToList();
        if (portalIds.Count > 0)
        {
            // 侧边栏第一个门户
            var firstPortalId = await _userRepository.AsSugarClient().Queryable<PortalEntity, DictionaryDataEntity>((p, d) => new JoinQueryInfos(JoinType.Left, p.Category == d.Id))
                .Where(p => portalIds.Contains(p.Id))
                .OrderBy((p, d) => d.SortCode)
                .OrderBy((p, d) => d.CreatorTime, OrderByType.Desc)
                .OrderBy(p => p.SortCode)
                .OrderBy(p => p.CreatorTime, OrderByType.Desc)
                .Select(p => p.Id)
                .FirstAsync();

            var key = string.Format("{0}:{1}", type, systemId);
            if (portalDic.ContainsKey(key))
            {
                var portalData = portalDic[key];
                if (portalData.IsNotEmptyOrNull() && portalIds.Contains(portalData))
                {
                    return portalData;
                }
                else
                {
                    portalDic[key] = firstPortalId;

                    var portal = portalDic.ToJsonString();
                    await _userRepository.AsSugarClient().Updateable<UserEntity>()
                        .Where(it => it.Id.Equals(userId))
                        .SetColumns(it => new UserEntity()
                        {
                            PortalId = portal,
                            LastModifyTime = SqlFunc.GetDate(),
                            LastModifyUserId = userId
                        })
                        .ExecuteCommandAsync();

                    return firstPortalId;
                }
            }
            else
            {
                portalDic.Add(key, firstPortalId);

                var portal = portalDic.ToJsonString();
                await _userRepository.AsSugarClient().Updateable<UserEntity>()
                    .Where(it => it.Id.Equals(userId))
                    .SetColumns(it => new UserEntity()
                    {
                        PortalId = portal,
                        LastModifyTime = SqlFunc.GetDate(),
                        LastModifyUserId = userId
                    })
                    .ExecuteCommandAsync();

                return firstPortalId;
            }
        }
        else
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 添加登录日志.
    /// </summary>
    /// <param name="tenantId">租户id.</param>
    /// <param name="user">用户.</param>
    /// <param name="userName">用户名称.</param>
    /// <param name="grant_type">授权类型 : official 官网专用.</param>
    /// <param name="userAgent">请求上下文.</param>
    /// <param name="requestDuration">耗时.</param>
    /// <param name="loginType">登录类型（0：登录；1：退出）.</param>
    /// <param name="loginMark">是否登录成功（0：失败；1：成功）.</param>
    /// <param name="description"></param>
    /// <returns></returns>
    private async Task AddLoginLog(string tenantId, UserEntity user, string userName, string grant_type, UserAgent userAgent, int requestDuration, int loginType, int loginMark, string description)
    {
        var ipAddress = NetHelper.Ip;
        var ipAddressName = await NetHelper.GetLocation(ipAddress);
        var traceId = App.HttpContext?.Items["TraceId"]?.ToString();

        var _OS = string.Empty;
        var _Browser = string.Empty;

        // 非常规请求(Saas) ， 浏览器表头为空处理.
        if (grant_type != null && grant_type.Equals("official"))
        {
            _OS = "Saas";
            _Browser = "Saas";
        }
        else
        {
            _OS = userAgent.OS.ToString();
            _Browser = userAgent.userAgent?.ToString();
        }

        await _eventPublisher.PublishAsync(new LogEventSource("Log:CreateVisLog", tenantId, new SysLogEntity
        {
            Id = SnowflakeIdHelper.NextId(),
            UserId = user.IsNotEmptyOrNull() ? user.Id : null,
            UserName = userName,
            Type = 1,
            IPAddress = ipAddress,
            IPAddressName = ipAddressName,
            PlatForm = _OS,
            Browser = _Browser,
            RequestDuration = requestDuration,
            CreatorTime = DateTime.Now,
            LoginType = loginType,
            LoginMark = loginMark,
            Description = description.Contains(']') ? description.Split("]").Last().TrimStart() : description,
            TraceId = traceId,
            TenantId = tenantId
        }));
    }

    /// <summary>
    /// 项目本地地址.
    /// </summary>
    /// <returns></returns>
    private string GetLocalAddress()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var server = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>();
        var addressesFeature = server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
        var addresses = addressesFeature?.Addresses;
        return addresses.FirstOrDefault().Replace("[::]", "localhost");
    }

    #endregion

    #region 第三方登录回调

    /// <summary>
    /// 第三方登录回调.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Login/socials")]
    [AllowAnonymous]
    [LogPolicy(LogPolicy.IgnoreRequest)]
    [NonUnify]
    public async Task<dynamic> SocialsLoginCallBack([FromQuery] SocialsUserInputModel req)
    {
        ConnectionConfigOptions options = null;
        var defaultConnection = _connectionStrings.DefaultConnectionConfig;

        if (req.tenantLogin && req.tenantId.IsNotEmptyOrNull() && req.userId.IsNotEmptyOrNull())
        {
            var tenantInterFaceOutput = await _tenantManager.ChangTenant(_sqlSugarClient, req.tenantId);
            options = tenantInterFaceOutput.options;
            var userEntity = await _userRepository.AsQueryable().FirstAsync(x => x.Id.Equals(req.userId));
            if (_tenant.MultiTenancy) userEntity.Account = req.tenantId + "@" + userEntity.Account;
            var result = await Login(new LoginInput() { account = userEntity.Account, password = userEntity.Password, isSocialsLoginCallBack = true, socialsOptions = options });
            return new { code = 200, data = result };
        }

        if (_tenant.MultiTenancy && req.tenantId.IsNullOrEmpty())
        {
            var result = new AuthResponse(5001, string.Empty);
            var resStr = string.Empty;

            // 微信小程序唤醒登录.
            if (req.uuid.IsNotEmptyOrNull())
            {
                AuthUser user = new AuthUser();
                user.uuid = req.uuid;
                user.source = req.source;
                user.username = req.socialName;
                result = new AuthResponse(2000, null, user);
            }
            else
            {
                if (req.code.IsNullOrWhiteSpace()) req.code = req.authCode != null ? req.authCode : req.auth_code;

                // 获取第三方请求
                AuthCallbackNew callback = _socialsUserService.SetAuthCallback(req.code, req.state);

                // 获取第三方请求
                var authRequest = _socialsUserService.GetAuthRequest(req.source, null, false, null, null);
                result = authRequest.login(callback);
            }

            if (result.ok())
            {
                var resData = result.data.ToObject<AuthUser>();
                var uuid = _socialsUserService.GetSocialUuid(result);

                options = JNPFTenantExtensions.GetLinkToOrdinary(defaultConnection.ConfigId.ToString(), defaultConnection.DBName);
                var interFace = string.Format("{0}socials/list?socialsId={1}", _tenant.MultiTenancyDBInterFace, uuid);
                var response = await interFace.SetHeaders(new Dictionary<string, object> {
                    { "X-Forwarded-For", NetHelper.Ip}
                }).GetAsStringAsync();
                var resultObj = response.ToObject<Dictionary<string, object>>();
                if (resultObj["code"].ToString() != "200")
                {
                    throw Oops.Oh(resultObj["msg"].ToString());
                }

                var ticket = _cacheManager.Get<SocialsLoginTicketModel>(req.jnpf_ticket);
                if (ticket == null && req.code.IsNullOrWhiteSpace()) throw Oops.Oh(ErrorCode.D1035);
                if (ticket.IsNotEmptyOrNull())
                {
                    // 修改 缓存 状态
                    ticket.status = (int)SocialsLoginTicketStatus.Multitenancy;
                    if (resultObj["data"] != null && resultObj["data"].ToJsonString().Equals("[]"))
                    {
                        if (result.ok())
                        {
                            var socialsUserEntity = new SocialsUsersEntity();
                            socialsUserEntity.SocialType = resData.source;
                            socialsUserEntity.SocialName = resData.username;
                            socialsUserEntity.SocialId = uuid;
                            ticket.status = (int)SocialsLoginTicketStatus.UnBind;
                            ticket.value = socialsUserEntity.ToJsonString();
                            _cacheManager.Set(req.jnpf_ticket, ticket, TimeSpan.FromMinutes(5));
                            resStr = new { code = 400, msg = "等待登录自动绑定!", message = "等待登录自动绑定!" }.ToJsonString();
                        }
                        else
                        {
                            resStr = new { code = 400, msg = "第三方回调失败!", message = "第三方回调失败!" }.ToJsonString();
                        }
                    }
                    else
                    {
                        var tList = resultObj["data"].ToObject<List<Dictionary<string, object>>>();
                        if (tList.Count == 1)
                        {
                            var tInfo = tList.FirstOrDefault();
                            await _tenantManager.ChangTenant(_sqlSugarClient, tInfo["tenantId"].ToString());
                            var userEntity = await _userRepository.AsQueryable().FirstAsync(x => x.Id.Equals(tInfo["userId"].ToString()));
                            if (_tenant.MultiTenancy) userEntity.Account = tInfo["tenantId"].ToString() + "@" + userEntity.Account;
                            var loginRes = await Login(new LoginInput() { account = userEntity.Account, password = userEntity.Password, isSocialsLoginCallBack = true, socialsOptions = options });

                            // 修改 缓存 状态
                            ticket.status = (int)SocialsLoginTicketStatus.Success;
                            ticket.value = loginRes.token;
                            _cacheManager.Set(req.jnpf_ticket, ticket.ToJsonString(), TimeSpan.FromMinutes(5));
                            return new { code = 200, data = ticket };
                        }
                        ticket.value = resultObj["data"].ToJsonString();
                        _cacheManager.Set(req.jnpf_ticket, ticket.ToJsonString(), TimeSpan.FromMinutes(5));
                        resStr = new { code = 200, data = ticket.value }.ToJsonString();
                    }
                }
            }
            else
            {
                resStr = new { code = 400, msg = "第三方回调失败!", message = "第三方回调失败!" }.ToJsonString();
            }

            if (req.jnpf_ticket.IsNullOrEmpty())
            {
                return new ContentResult()
                {
                    Content = string.Format("<script>window.opener.postMessage('{0}', '*');window.open('','_self','');window.close();</script>", resStr),
                    StatusCode = 200,
                    ContentType = "text/html;charset=utf-8"
                };
            }

            return resStr.ToObject<Dictionary<string, object>>();
        }
        if (_tenant.MultiTenancy && req.tenantId.IsNotEmptyOrNull())
        {
            await _tenantManager.ChangTenant(_sqlSugarClient, req.tenantId);
        }

        if (req.code.IsNullOrWhiteSpace()) req.code = req.authCode != null ? req.authCode : req.auth_code;

        var res = await _socialsUserService.Binding(req);

        if (req.jnpf_ticket.IsNotEmptyOrNull())
        {
            var ticket = _cacheManager.Get<SocialsLoginTicketModel>(req.jnpf_ticket);
            if (ticket == null && req.code.IsNullOrWhiteSpace()) throw Oops.Oh(ErrorCode.D1035);

            var data = res.ToObject<Dictionary<string, object>>();
            if (data.ContainsKey("data"))
            {
                var socialsEntity = data["data"].ToObject<SocialsUsersEntity>();

                // 接受CODE 进行登录
                var sEntity = await _userRepository.AsSugarClient().Queryable<SocialsUsersEntity>().FirstAsync(x => x.SocialType.Equals(socialsEntity.SocialType) && x.SocialId.Equals(socialsEntity.SocialId) && x.DeleteMark == null);
                if (sEntity != null)
                {
                    var userEntity = await _userRepository.AsQueryable().FirstAsync(x => x.Id.Equals(sEntity.UserId));
                    if (_tenant.MultiTenancy) userEntity.Account = req.tenantId + "@" + userEntity.Account;
                    var loginRes = await Login(new LoginInput() { account = userEntity.Account, password = userEntity.Password, isSocialsLoginCallBack = true, socialsOptions = options });

                    // 修改 缓存 状态
                    ticket.status = (int)SocialsLoginTicketStatus.Success;
                    ticket.value = loginRes.token;
                    _cacheManager.Set(req.jnpf_ticket, ticket.ToJsonString(), TimeSpan.FromMinutes(5));
                    return new { code = 200, data = ticket };
                }
                else
                {
                    var ticketValue = _cacheManager.Get(req.jnpf_ticket);
                    if (ticketValue.IsNotEmptyOrNull())
                    {
                        ticket.status = (int)SocialsLoginTicketStatus.UnBind;
                        ticket.value = socialsEntity.ToJsonString();
                        _cacheManager.Set(req.jnpf_ticket, ticket, TimeSpan.FromMinutes(5));
                        res = new { code = 400, msg = "等待登录自动绑定!", message = "等待登录自动绑定!" }.ToJsonString();
                    }
                    else
                    {
                        res = new { code = 400, msg = "第三方回调失败!", message = "第三方回调失败!" }.ToJsonString();
                    }
                }
            }
            else
            {
                res = new { code = 400, msg = "第三方回调失败!", message = "第三方回调失败!" }.ToJsonString();
            }
        }

        if (req.jnpf_ticket.IsNullOrEmpty())
        {
            var result = res.ToObject<Dictionary<string, object>>();
            if (result.ContainsKey("data")) result.Remove("data");
            return new ContentResult()
            {
                Content = string.Format("<script>window.opener.postMessage('{0}', '*');window.open('','_self','');window.close();</script>", result.ToJsonString()),
                StatusCode = 200,
                ContentType = "text/html;charset=utf-8"
            };
        }

        return res.ToObject<Dictionary<string, object>>();
    }

    /// <summary>
    /// 多租户第三方登录回调.
    /// </summary>
    /// <returns></returns>
    [HttpPost("Login/socials")]
    [Consumes("application/x-www-form-urlencoded")]
    [AllowAnonymous]
    [LogPolicy(LogPolicy.IgnoreRequest)]
    public async Task<dynamic> SocialsLogin([FromForm] SocialsUserCallBackModel req)
    {
        if (req.tenantLogin)
        {
            var tenantInterFaceOutput = await _tenantManager.ChangTenant(_sqlSugarClient, req.tenantId);
            var userEntity = await _userRepository.AsQueryable().FirstAsync(x => x.Id.Equals(req.userId));
            if (_tenant.MultiTenancy) userEntity.Account = req.tenantId + "@" + userEntity.Account;
            return await Login(new LoginInput() { account = userEntity.Account, password = userEntity.Password, isSocialsLoginCallBack = true, socialsOptions = tenantInterFaceOutput.options });
        }
        return null;
    }

    /// <summary>
    /// 获取登录配置, 是否需要跳转、第三方登录信息.
    /// </summary>
    [HttpGet("GetLoginConfig")]
    [AllowAnonymous]
    [LogPolicy(LogPolicy.IgnoreAll)]
    public dynamic GetSocialsLoginConfig()
    {
        var loginConfigModel = new SocialsLoginConfigModel();

        if (_oauthOptions.Enabled)
        {
            var url = _oauthOptions.LoginPath + "/" + _oauthOptions.DefaultSSO;
            loginConfigModel.redirect = true;
            loginConfigModel.url = url;
            loginConfigModel.ticketParams = CommonConst.PARAMS_JNPF_TICKET;
        }
        else
        {
            // 追加第三方登录配置
            var loginList = _socialsUserService.GetLoginList(CommonConst.PARAMS_JNPF_TICKET.ToUpper());
            if (loginList == null) return loginConfigModel;
            if (loginList.Any())
            {
                loginConfigModel.socialsList = loginList.ToObject<List<object>>();
                loginConfigModel.redirect = false;
                loginConfigModel.ticketParams = CommonConst.PARAMS_JNPF_TICKET;
            }
        }

        return loginConfigModel;
    }

    /// <summary>
    /// 获取登录票据.
    /// </summary>
    /// <returns>return {msg:有效期, data:票据}.</returns>
    [HttpGet("getTicket")]
    [AllowAnonymous]
    [LogPolicy(LogPolicy.IgnoreAll)]
    public dynamic GetTicket()
    {
        SocialsLoginTicketModel ticketModel = new SocialsLoginTicketModel();
        var curDate = DateTime.Now.AddMinutes(_oauthOptions.TicketTimeout); // 默认过期5分钟.
        ticketModel.ticketTimeout = curDate.ParseToUnixTime();
        var key = "SocialsLogin_" + SnowflakeIdHelper.NextId();
        _cacheManager.Set(key, ticketModel.ToJsonString(), TimeSpan.FromMinutes(_oauthOptions.TicketTimeout));
        return key;
    }

    /// <summary>
    /// 检测票据登录状态.
    /// </summary>
    /// <returns></returns>
    [HttpGet("getTicketStatus/{ticket}")]
    [AllowAnonymous]
    [LogPolicy(LogPolicy.IgnoreAll)]
    public dynamic GetTicketStatus(string ticket)
    {
        var ticketModel = _cacheManager.Get<SocialsLoginTicketModel>(ticket);
        if (ticketModel == null)
        {
            ticketModel = new SocialsLoginTicketModel() { status = (int)SocialsLoginTicketStatus.Invalid };
        }

        return ticketModel;
    }

    #endregion

    #region 单点登录.

    /// <summary>
    /// 单点登录接口.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("Login/{type}")]
    [AllowAnonymous]
    [LogPolicy(LogPolicy.IgnoreRequest)]
    [NonUnify]
    public async Task<dynamic> LoginByType(string type, [FromQuery] Dictionary<string, string> input)
    {
        #region Cas
        //if (type.ToLower().Equals("cas"))
        //{
        //    var ticket = input.ContainsKey(CommonConst.PARAMS_JNPF_TICKET) ? input[CommonConst.PARAMS_JNPF_TICKET].ToString() : string.Empty;
        //    var ticketModel = _cacheManager.Get<SocialsLoginTicketModel>(ticket);
        //    if (ticketModel == null) return "登录票据已失效";

        //    var casTicket = input.ContainsKey(CommonConst.CAS_Ticket) ? input[CommonConst.CAS_Ticket].ToString() : string.Empty;
        //    if (casTicket.IsNotEmptyOrNull())
        //    {

        //    }
        //    else
        //    {
        //        var loginUrl = _oauthOptions.SSO.Cas.ServerLoginUrl;
        //        //http://sso.maxkey.top:8527/sign/authz/cas/?service=http://sa-oauth-client.demo.maxkey.top:8002

        //        loginUrl = Extras.CollectiveOAuth.Utils.UrlBuilder.fromBaseUrl(loginUrl)
        //            .queryParam("service", _oauthOptions.LoginPath + "/cas")
        //            .queryParam(CommonConst.PARAMS_JNPF_TICKET, ticket)
        //            .build();
        //        _httpContextAccessor.HttpContext.Response.Redirect(loginUrl);
        //    }
        //}
        #endregion

        if (type.ToLower().Equals("auth2"))
        {
            var ticket = string.Empty;
            if (input.ContainsKey(CommonConst.PARAMS_JNPF_TICKET) && input[CommonConst.PARAMS_JNPF_TICKET].IsNotEmptyOrNull())
            {
                ticket = input[CommonConst.PARAMS_JNPF_TICKET];
                var ticketModel = _cacheManager.Get<SocialsLoginTicketModel>(ticket);
                if (ticketModel == null) return "登录票据已失效";
            }

            var code = input.ContainsKey(CommonConst.Code) ? input[CommonConst.Code] : string.Empty;

            // 接受CODE 进行登录
            if (code.IsNotEmptyOrNull())
            {
                try
                {
                    await loginByCode(code, ticket);
                }
                catch (Exception e)
                {
                    // 更新登录结果
                    return e.Message;
                }
            }
            else
            {
                redirectLogin(ticket);
            }
        }

        return null;
    }

    /// <summary>
    /// 跳转单点登录页面.
    /// </summary>
    protected void redirectLogin(string ticket)
    {
        var loginUrl = _oauthOptions.SSO.Auth2.AuthorizeUrl;
        var tmpAuthCallbackUrl = _oauthOptions.LoginPath + "/auth2";
        //http://sso.maxkey.top:8527/sign/authz/oauth/v20/authorize?response_type=code&client_id=745057899234983936&redirect_uri=http://sa-oauth-client.demo.maxkey.top:8002/&scope=all

        if (ticket.IsNotEmptyOrNull())
        {
            tmpAuthCallbackUrl = Extras.CollectiveOAuth.Utils.UrlBuilder.fromBaseUrl(tmpAuthCallbackUrl)
                    .queryParam(CommonConst.PARAMS_JNPF_TICKET, ticket)
                    .build();
        }

        loginUrl = Extras.CollectiveOAuth.Utils.UrlBuilder.fromBaseUrl(loginUrl)
            .queryParam("response_type", CommonConst.Code)
            .queryParam("client_id", _oauthOptions.SSO.Auth2.ClientId)
            .queryParam("scope", "read")
            .queryParam("redirect_uri", tmpAuthCallbackUrl)
            .build();

        _httpContextAccessor.HttpContext.Response.Redirect(loginUrl);
    }

    /// <summary>
    /// Oauth2登录.
    /// </summary>
    /// <param name="code"></param>
    /// <param name="ticket"></param>
    protected async Task loginByCode(string code, string ticket)
    {
        var token = await getAccessToken(code);
        var remoteUserInfo = await getRemoteInfo(token);
        //var userId = remoteUserInfo.getOrDefault("accounts.username", remoteUserInfo["username"]).ToString();
        var userId = remoteUserInfo.ContainsKey("accounts.username") ? remoteUserInfo["accounts.username"].ToString() : remoteUserInfo["username"].ToString();
        var userAccount = string.Empty;
        if (_tenant.MultiTenancy)
        {
            var instId = remoteUserInfo["institution"].ToString();
            userAccount = instId + "@" + userId;
        }
        else
        {
            userAccount = userId;
        }

        // 登录账号
        var loginInput = await GetUserInfoByUserAccount(userAccount);
        loginInput.online_ticket = remoteUserInfo["online_ticket"].ToString();
        var loginRes = await Login(loginInput);

        var jnpfTicket = _cacheManager.Get<SocialsLoginTicketModel>(ticket);
        if (jnpfTicket.IsNotEmptyOrNull())
        {
            // 修改 缓存 状态
            jnpfTicket.status = (int)SocialsLoginTicketStatus.Success;
            jnpfTicket.value = loginRes.token;
            _cacheManager.Set(ticket, jnpfTicket.ToJsonString(), TimeSpan.FromMinutes(_oauthOptions.TicketTimeout));
        }
        else
        {
            var url = string.Format("{0}?token={1}&theme={2}", _oauthOptions.SucessFrontUrl, loginRes.token, loginRes.theme);
            _httpContextAccessor.HttpContext.Response.Redirect(url);
        }
    }

    /// <summary>
    /// 获取OAUTH2 AccessToken.
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    private async Task<string> getAccessToken(string code)
    {
        var reqUrl = _oauthOptions.SSO.Auth2.AccessTokenUrl
                .AddUrlQuery(string.Format("grant_type={0}", "authorization_code"))
                .AddUrlQuery(string.Format("client_id={0}", _oauthOptions.SSO.Auth2.ClientId))
                .AddUrlQuery(string.Format("client_secret={0}", _oauthOptions.SSO.Auth2.ClientSecret))
                .AddUrlQuery(string.Format("redirect_uri={0}", _oauthOptions.LoginPath + "/auth2"))
                .AddUrlQuery(string.Format("code={0}", code));

        var response = await reqUrl.GetAsStringAsync();
        Dictionary<string, object> result = null;
        try
        {
            result = response.ToObject<Dictionary<string, object>>();
        }
        catch (Exception e)
        {
            // log.error("解析Auth2 access_token失败", e);
        }

        if (result == null || !result.ContainsKey("access_token"))
        {
            throw new Exception("Auth2: 获取access_token失败");
        }

        var access_token = result["access_token"].ToString();

        // log.debug("Auth2 Token: {}", access_token);
        return access_token;
    }

    /// <summary>
    /// 获取用户信息.
    /// </summary>
    /// <param name="access_token"></param>
    /// <returns></returns>
    private async Task<Dictionary<string, object>> getRemoteInfo(string access_token)
    {
        var reqUrl = _oauthOptions.SSO.Auth2.UserInfoUrl
                .AddUrlQuery(string.Format("access_token={0}", access_token));
        var response = await reqUrl.GetAsStringAsync();

        Dictionary<string, object> result = null;
        try
        {
            // log.debug("Auth2 User: {}", response);
            result = response.ToObject<Dictionary<string, object>>();
        }
        catch (Exception e)
        {
            // log.error("解析Auth2 用户信息失败", e);
        }

        if (result == null || !result.ContainsKey("username"))
        {
            // log.error(response);
            throw new Exception("Auth2: 获取远程用户信息失败");
        }

        return result;
    }

    private async Task<LoginInput> GetUserInfoByUserAccount(string account)
    {
        var defaultConnection = _connectionStrings.DefaultConnectionConfig;
        ConnectionConfigOptions options = JNPFTenantExtensions.GetLinkToOrdinary(defaultConnection.ConfigId.ToString(), defaultConnection.DBName);
        UserAgent userAgent = new UserAgent(App.HttpContext);
        if (_tenant.MultiTenancy)
        {
            // 分割账号
            var tenantAccount = account.Split('@');
            var tenantId = tenantAccount.FirstOrDefault();
            if (tenantAccount.Length == 1)
                account = "admin";
            else
                account = tenantAccount[1];

            await _tenantManager.ChangTenant(_sqlSugarClient, tenantId);
            var userEntity = _sqlSugarClient.Queryable<UserEntity>().Single(u => u.Account == account && u.DeleteMark == null);
            return new LoginInput()
            {
                account = string.Join("@", tenantAccount),
                password = userEntity.Password,
                isSocialsLoginCallBack = true
            };
        }
        else
        {
            var userEntity = _sqlSugarClient.Queryable<UserEntity>().Single(u => u.Account == account && u.DeleteMark == null);
            return new LoginInput()
            {
                account = userEntity.Account,
                password = userEntity.Password,
                isSocialsLoginCallBack = true
            };
        }
    }

    #endregion

    #region 扫码登录

    /// <summary>
    /// 生成扫码凭证.
    /// </summary>
    /// <returns></returns>
    [HttpGet("CodeCertificate")]
    [AllowAnonymous]
    [LogPolicy(LogPolicy.IgnoreAll)]
    public dynamic GetCodeCertificate()
    {
        ScanCodeLoginConfigModel ticketModel = new ScanCodeLoginConfigModel();
        var timeOut = App.GetConfig<AppOptions>("JNPF_App", true).CodeCertificateTimeout;
        var curDate = DateTime.Now.AddMinutes(timeOut); // 默认过期3分钟.
        ticketModel.ticketTimeout = curDate.ParseToUnixTime();
        var key = "ScanCode_" + SnowflakeIdHelper.NextId();
        _cacheManager.Set(key, ticketModel.ToJsonString(), ticketModel.ticketTimeout.TimeStampToDateTime() - DateTime.Now);
        return key;
    }

    /// <summary>
    /// 获取扫码凭证状态.
    /// </summary>
    /// <returns></returns>
    [HttpGet("CodeCertificateStatus/{ticket}")]
    [AllowAnonymous]
    [LogPolicy(LogPolicy.IgnoreAll)]
    public dynamic GetCodeCertificateStatus(string ticket)
    {
        var ticketModel = _cacheManager.Get<ScanCodeLoginConfigModel>(ticket);
        if (ticketModel == null)
        {
            ticketModel = new ScanCodeLoginConfigModel() { status = (int)ScanCodeLoginTicketStatus.Invalid };
        }

        return ticketModel;
    }

    /// <summary>
    /// 更改扫码凭证状态.
    /// </summary>
    /// <returns></returns>
    [HttpGet("SetCodeCertificateStatus/{ticket}/{status}")]
    [AllowAnonymous]
    [LogPolicy(LogPolicy.Minimal)]
    public dynamic SetCodeCertificateStatus(string ticket, int status)
    {
        var ticketModel = _cacheManager.Get<ScanCodeLoginConfigModel>(ticket);
        if (ticketModel == null)
        {
            ticketModel = new ScanCodeLoginConfigModel() { status = (int)ScanCodeLoginTicketStatus.Invalid };
        }
        else
        {
            ticketModel = new ScanCodeLoginConfigModel() { status = status };
            var timeOut = App.GetConfig<AppOptions>("JNPF_App", true).CodeCertificateTimeout;
            var curDate = DateTime.Now.AddMinutes(timeOut); // 默认过期3分钟.
            ticketModel.ticketTimeout = curDate.ParseToUnixTime();
            _cacheManager.Set(ticket, ticketModel.ToJsonString(), ticketModel.ticketTimeout.TimeStampToDateTime() - DateTime.Now);
        }

        return ticketModel;
    }

    /// <summary>
    /// 确认登录.
    /// </summary>
    /// <param name="ticket"></param>
    /// <returns></returns>
    [HttpGet("ConfirmLogin/{ticket}")]
    public async Task<dynamic> ConfirmLogin(string ticket)
    {
        var ticketModel = _cacheManager.Get<ScanCodeLoginConfigModel>(ticket);
        if (ticketModel == null || !ticketModel.status.Equals((int)ScanCodeLoginTicketStatus.ScanCode)) return new ScanCodeLoginConfigModel() { status = (int)ScanCodeLoginTicketStatus.Invalid };

        var account = _tenant.MultiTenancy ? _userManager.TenantId + "@" + _userManager.User.Account : _userManager.User.Account;
        var loginRes = await Login(new LoginInput() { account = account, password = _userManager.User.Password, isSocialsLoginCallBack = true });

        // 修改 缓存 状态
        ticketModel.status = (int)ScanCodeLoginTicketStatus.Success;
        ticketModel.value = loginRes.token;
        _cacheManager.Set(ticket, ticketModel.ToJsonString(), TimeSpan.FromMinutes(2));
        return ticketModel;
    }

    #endregion
}