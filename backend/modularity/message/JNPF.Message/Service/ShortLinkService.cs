using JNPF.Common.Configuration;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Net;
using JNPF.Common.Options;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Logging.Attributes;
using JNPF.Message.Entitys.Entity;
using JNPF.Message.Interfaces.Message;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Interfaces.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace JNPF.Message.Service;

/// <summary>
/// 公众号.
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "Message", Name = "ShortLink", Order = 240)]
[Route("api/message/[controller]")]
public class ShortLinkService : IShortLinkService, IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<MessageShortLinkEntity> _repository;
    private readonly ISysConfigService _sysConfigService;
    private readonly ITenantManager _tenantManager;
    private readonly SqlSugar.ConnectionStringsOptions _connectionStrings;
    private readonly MessageOptions _messageOptions = App.GetConfig<MessageOptions>("Message", true);
    private SqlSugarScope _sqlSugarClient;

    public ShortLinkService(
        ISqlSugarRepository<MessageShortLinkEntity> repository,
        ISysConfigService sysConfigService,
        ITenantManager tenantManager,
        IOptions<SqlSugar.ConnectionStringsOptions> connectionOptions,
        ISqlSugarClient sqlSugarClient
        )
    {
        _repository = repository;
        _sysConfigService = sysConfigService;
        _tenantManager = tenantManager;
        _connectionStrings = connectionOptions.Value;
        _sqlSugarClient = (SqlSugarScope)sqlSugarClient;
    }

    /// <summary>
    /// 新建.
    /// </summary>
    /// <returns></returns>
    [HttpGet("{shortLink}")]
    [HttpGet("{shortLink}/{tenantId}")]
    [AllowAnonymous]
    [LogPolicy(LogPolicy.IgnoreAll)]
    public async Task GetInfo(string shortLink, string tenantId)
    {
        UserAgent userAgent = new UserAgent(App.HttpContext);
        if (tenantId.IsNotEmptyOrNull())
        {
            await _tenantManager.ChangTenant(_sqlSugarClient, tenantId);
        }

        var entity = await _sqlSugarClient.Queryable<MessageShortLinkEntity>().SingleAsync(x => x.ShortLink == shortLink && x.DeleteMark == null);
        if (entity == null) throw Oops.Oh(ErrorCode.D7009);
        // 验证失效以及修改点击次数
        if (entity.IsUsed == 1)
        {
            if (entity.UnableTime < DateTime.Now || entity.ClickNum == entity.UnableNum) throw Oops.Oh(ErrorCode.D7010);
            ++entity.ClickNum;
            await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        }
        else
        {
            if (entity.UnableTime < DateTime.Now) throw Oops.Oh(ErrorCode.D7010);
        }
        string accessToken = await CreateToken(entity.UserId, tenantId);
        // 验证请求端
        var urlLink = userAgent.IsMobileDevice ? entity.RealAppLink : entity.RealPcLink;
        urlLink = string.Format("{0}&token={1}", urlLink, accessToken);
        App.HttpContext.Response.Redirect(urlLink);
    }

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <param name="bodyText"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<MessageShortLinkEntity> Create(string userId, string bodyText)
    {
        var sysconfig = await _sysConfigService.GetInfo();
        var entity = new MessageShortLinkEntity();
        entity.ShortLink = RandomExtensions.NextLetterAndNumberString(new Random(), 6);
        entity.UserId = userId;
        if (sysconfig.isClick == 1)
        {
            entity.IsUsed = 1;
            entity.ClickNum = 0;
            entity.UnableNum = sysconfig.unClickNum;
        }
        entity.UnableTime = DateTime.Now.AddHours(sysconfig.linkTime);
        entity.BodyText = bodyText;
        entity.RealAppLink = string.Format("{0}/pages/workFlow/flowBefore/index?config={1}", _messageOptions.DoMainApp, bodyText.ToBase64String());
        entity.RealPcLink = string.Format("{0}/workFlowDetail?config={1}", _messageOptions.DoMainPc, bodyText.ToBase64String());
        return await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
    }

    public async Task<string> CreateToken(string userId, string tenantId)
    {
        var defaultConnection = _connectionStrings.DefaultConnectionConfig;
        ConnectionConfigOptions options = JNPFTenantExtensions.GetLinkToOrdinary(defaultConnection.ConfigId.ToString(), defaultConnection.DBName);
        if (tenantId.IsNotEmptyOrNull() && KeyVariable.MultiTenancy && !tenantId.Equals("default"))
        {
            var output = await _tenantManager.ChangTenant(_sqlSugarClient, tenantId);
            options = output.options;
        }
        var userEntity = _sqlSugarClient.Queryable<UserEntity>().Single(u => u.Id == userId && u.DeleteMark == null);
        var token = NetHelper.GetToken(userEntity.Id, userEntity.Account, userEntity.RealName, userEntity.IsAdministrator, options.ConfigId, false);
        return token;
    }
}
