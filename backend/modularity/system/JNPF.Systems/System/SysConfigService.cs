using JNPF.Common.Dtos;
using JNPF.Common.Enums;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extras.Thirdparty.DingDing;
using JNPF.Extras.Thirdparty.Email;
using JNPF.Extras.Thirdparty.WeChat;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.SysConfig;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems.System;

/// <summary>
/// 系统配置
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "SysConfig", Order = 211)]
[Route("api/system/[controller]")]
public class SysConfigService : ISysConfigService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 系统配置仓储.
    /// </summary>
    private readonly ISqlSugarRepository<SysConfigEntity> _repository;

    /// <summary>
    /// 初始化一个<see cref="SysConfigService"/>类型的新实例.
    /// </summary>
    public SysConfigService(
        ISqlSugarRepository<SysConfigEntity> repository)
    {
        _repository = repository;
    }

    #region GET

    /// <summary>
    /// 获取系统配置.
    /// </summary>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<SysConfigOutput> GetInfo()
    {
        var array = new Dictionary<string, string>();
        var data = await _repository.AsQueryable().Where(x => x.Category.Equals("SysConfig")).ToListAsync();
        foreach (var item in data)
        {
            if (!array.ContainsKey(item.Key)) array.Add(item.Key, item.Value);
        }

        return array.ToObject<SysConfigOutput>();
    }

    /// <summary>
    /// 获取所有超级管理员.
    /// </summary>
    /// <returns></returns>
    [HttpGet("getAdminList")]
    public async Task<dynamic> GetAdminList()
    {
        return await _repository.AsSugarClient().Queryable<UserEntity>()
            .Where(x => x.IsAdministrator == 1 && x.DeleteMark == null)
            .Select(x => new AdminUserOutput()
            {
                id = x.Id,
                account = x.Account,
                realName = x.RealName
            }).ToListAsync();
    }

    #endregion

    #region Post

    /// <summary>
    /// 邮箱链接测试.
    /// </summary>
    /// <param name="input"></param>
    [HttpPost("Email/Test")]
    public void EmailTest([FromBody] MailParameterInfo input)
    {
        var result = MailUtil.CheckConnected(input);
        if (!result)
            throw Oops.Oh(ErrorCode.D9003);
    }

    /// <summary>
    /// 钉钉链接测试.
    /// </summary>
    /// <param name="input"></param>
    [HttpPost("testDingTalkConnect")]
    public void testDingTalkConnect([FromBody] DingParameterInfo input)
    {
        var dingUtil = new DingUtil(input.dingSynAppKey, input.dingSynAppSecret);
        if (string.IsNullOrEmpty(dingUtil.token))
            throw Oops.Oh(ErrorCode.D9003);
    }

    /// <summary>
    /// 企业微信链接测试.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="input"></param>
    [HttpPost("{type}/testQyWebChatConnect")]
    public void testQyWebChatConnect(int type, [FromBody] WeChatParameterInfo input)
    {
        var appSecret = type == 0 ? input.qyhAgentSecret : input.qyhCorpSecret;
        var weChatUtil = new WeChatUtil(input.qyhCorpId, appSecret);
        if (string.IsNullOrEmpty(weChatUtil.accessToken))
            throw Oops.Oh(ErrorCode.D9003);
    }

    /// <summary>
    /// 更新系统配置.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPut]
    [UnitOfWork]
    public async Task Update([FromBody] SysConfigOutput input)
    {
        var configDic = input.ToObject<Dictionary<string, object>>();
        var entitys = new List<SysConfigEntity>();
        foreach (var item in configDic.Keys)
        {
            if (configDic[item] != null)
            {
                if (item == "tokentimeout")
                {
                    long time = 0;
                    if (long.TryParse(configDic[item].ToString(), out time))
                    {
                        if (time > 8000000)
                        {
                            throw Oops.Oh(ErrorCode.D9008);
                        }
                    }
                }

                if (item == "verificationCodeNumber")
                {
                    int codeNum = 3;
                    if (int.TryParse(configDic[item].ToString(), out codeNum))
                    {
                        if (codeNum > 6 || codeNum < 3) throw Oops.Oh(ErrorCode.D9009);
                    }
                }

                SysConfigEntity sysConfigEntity = new SysConfigEntity();
                sysConfigEntity.Id = SnowflakeIdHelper.NextId();
                sysConfigEntity.Key = item;
                sysConfigEntity.Value = configDic[item].ToString();
                sysConfigEntity.Category = "SysConfig";
                entitys.Add(sysConfigEntity);
            }
        }

        await Save(entitys, "SysConfig");
    }

    /// <summary>
    /// 更新赋予超级管理员.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPut("setAdminList")]
    [UnitOfWork]
    public async Task SetAdminList([FromBody] SetAdminInput input)
    {
        await _repository.AsSugarClient().Updateable<UserEntity>().SetColumns(x => x.IsAdministrator == 0).Where(x => x.IsAdministrator == 1 && !x.Account.Equals("admin")).ExecuteCommandAsync();
        await _repository.AsSugarClient().Updateable<UserEntity>().SetColumns(x => x.IsAdministrator == 1).Where(x => input.adminIds.Contains(x.Id)).ExecuteCommandAsync();
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 系统配置信息.
    /// </summary>
    /// <param name="category">分类.</param>
    /// <param name="key">键.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<SysConfigEntity> GetInfo(string category, string key)
    {
        return await _repository.GetFirstAsync(s => s.Category == category && s.Key == key);
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 保存.
    /// </summary>
    /// <param name="entitys"></param>
    /// <param name="category"></param>
    /// <returns></returns>
    private async Task Save(List<SysConfigEntity> entitys, string category)
    {
        var oldEntitys = await _repository.AsQueryable().Where(it => it.Category.Equals(category)).ToListAsync();
        await _repository.DeleteAsync(oldEntitys);
        await _repository.AsInsertable(entitys).ExecuteCommandAsync();
    }

    #endregion
}