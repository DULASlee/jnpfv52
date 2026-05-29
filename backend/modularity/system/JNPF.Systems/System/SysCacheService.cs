using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models.User;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Message.Interfaces;
using JNPF.Systems.Entitys.Dto.SysCache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.Systems;

/// <summary>
/// 缓存管理
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "CacheManage", Order = 100)]
[Route("api/system/[controller]")]
public class SysCacheService : IDynamicApiController, ITransient
{
    /// <summary>
    /// IM消息中心服务.
    /// </summary>
    private readonly IImReplyService _imReplyService;

    /// <summary>
    /// 缓存管理器.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 初始化一个<see cref="SysCacheService"/>类型的新实例.
    /// </summary>
    public SysCacheService(
        ICacheManager cacheManager,
        IHttpContextAccessor httpContextAccessor,
        IUserManager userManager,
        IImReplyService imReplyService)
    {
        _imReplyService = imReplyService;
        _cacheManager = cacheManager;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    #region GET

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] CacheListInput input)
    {
        var tenantId = _userManager.TenantId;
        var keys = _cacheManager.GetAllCacheKeys().FindAll(q => q.Contains(tenantId));
        var output = new List<CacheListOutput>();
        foreach (var key in keys)
        {
            var model = new CacheListOutput();
            model.name = key;
            model.overdueTime = _cacheManager.GetCacheOutTime(model.name).ToString().ParseToDateTime();
            model.cacheSize = await RedisHelper.StrLenAsync(key);
            output.Add(model);
        }

        if (!string.IsNullOrEmpty(input.keyword))
            output = output.FindAll(x => x.name.Contains(input.keyword));
        if (input.overdueStartTime.IsNotEmptyOrNull())
            output = output.FindAll(x => x.overdueTime >= input.overdueStartTime);
        if (input.overdueEndTime.IsNotEmptyOrNull())
            output = output.FindAll(x => x.overdueTime <= input.overdueEndTime);
        return new { list = output.OrderBy(o => o.overdueTime) };
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="name">缓存名称.</param>
    [HttpGet("{name}")]
    public async Task<dynamic> GetInfo(string name)
    {
        var strJson = await _cacheManager.GetAsync(name);
        var cacheInfoOutput = new CacheInfoOutput();
        cacheInfoOutput.name = name;
        cacheInfoOutput.value = strJson;
        return cacheInfoOutput;
    }

    #endregion

    #region POST

    /// <summary>
    /// 清空单个缓存.
    /// </summary>
    /// <param name="name">name.</param>
    /// <returns></returns>
    [HttpDelete("{name}")]
    public async Task Clear(string name)
    {
        if (!await _cacheManager.DelAsync(name))
            throw Oops.Oh(ErrorCode.D1700);
    }

    /// <summary>
    /// 清空所有缓存.
    /// </summary>
    /// <returns></returns>
    [HttpPost("Actions/ClearAll")]
    public async Task ClearAll()
    {
        var tenantId = _userManager.TenantId;

        var httpContext = _httpContextAccessor.HttpContext;
        httpContext.SignoutToSwagger();

        // 清除IM中的webSocket
        var list = await GetOnlineUserList(tenantId);

        var user = list?.FirstOrDefault(it => it.tenantId == tenantId && it.userId == _userManager.UserId);
        if (user != null)
        {
            _imReplyService.ForcedOffline(user.connectionId);
            await DelOnlineUser(tenantId, user.userId);
            await DelUserInfo(tenantId, user.userId);
        }

        var keys = _cacheManager.GetAllCacheKeys().FindAll(q => q.Contains(tenantId));
        var isOk = await _cacheManager.DelAsync(keys.ToArray());
        if (!isOk)
            throw Oops.Oh(ErrorCode.D1700);
    }

    #endregion

    /// <summary>
    /// 获取在线用户列表.
    /// </summary>
    /// <param name="tenantId">租户ID.</param>
    /// <returns></returns>
    private async Task<List<UserOnlineModel>> GetOnlineUserList(string tenantId)
    {
        var cacheKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, tenantId);
        return await _cacheManager.GetAsync<List<UserOnlineModel>>(cacheKey);
    }

    /// <summary>
    /// 删除在线用户ID.
    /// </summary>
    /// <param name="tenantId">租户ID.</param>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    private async Task<bool> DelOnlineUser(string tenantId, string userId)
    {
        var cacheKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, tenantId);
        var list = await _cacheManager.GetAsync<List<UserOnlineModel>>(cacheKey);
        var online = list.Find(it => it.userId == userId);
        list.RemoveAll((x) => x.connectionId == online.connectionId);
        return await _cacheManager.SetAsync(cacheKey, list);
    }

    /// <summary>
    /// 删除用户登录信息缓存.
    /// </summary>
    /// <param name="tenantId">租户ID.</param>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    private async Task<bool> DelUserInfo(string tenantId, string userId)
    {
        var cacheKey = string.Format("{0}:{1}:{2}", tenantId, CommonConst.CACHEKEYUSER, userId);
        return await _cacheManager.DelAsync(cacheKey);
    }
}
