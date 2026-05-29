using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extras.Thirdparty.DingDing;
using JNPF.Extras.Thirdparty.Email;
using JNPF.Extras.Thirdparty.WeChat;
using JNPF.FriendlyException;
using JNPF.Message.Entitys.Dto.MessageAccount;
using JNPF.Message.Entitys.Entity;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Web;

namespace JNPF.Message.Service;

/// <summary>
/// 消息账号
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "Message", Name = "AccountConfig", Order = 240)]
[Route("api/message/AccountConfig")]
public class MessageAccountService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<MessageAccountEntity> _repository;

    public MessageAccountService(ISqlSugarRepository<MessageAccountEntity> repository)
    {
        _repository = repository;
    }

    #region Get

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] MessageAccountQuery input)
    {
        var smsSendType = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "smsSendType" && x.DeleteMark == null);
        var msgWebHookSendType = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "msgWebHookSendType" && x.DeleteMark == null);
        var query =   _repository.AsSugarClient().Queryable<MessageAccountEntity>();

        //modify by harry 如果是公众号时需要小程序和公众号都显示，临时先这样
        if (input.type == "7")
        {
            query = query.Where(a => a.Category == "7" || a.Category == "22");
        }
        else
        {
            query = query.Where(a => a.Category == input.type);
        }

        var   list = await query.Where(a =>   a.DeleteMark == null)
            .WhereIF(input.enabledMark.IsNotEmptyOrNull(), a => a.EnabledMark == input.enabledMark)
            .WhereIF(input.channel.IsNotEmptyOrNull(), a => a.Channel.ToString() == input.channel)
            .WhereIF(input.webhookType.IsNotEmptyOrNull(), a => a.WebhookType.ToString() == input.webhookType)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword) ||
            a.AddressorName.Contains(input.keyword) || a.SmtpUser.Contains(input.keyword) ||
            a.SmsSignature.Contains(input.keyword))
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), a => a.LastModifyTime, OrderByType.Desc)
            .Select(a => new MessageAccountListOutput
            {
                id = a.Id,
                enCode = a.EnCode,
                fullName = a.FullName,
                type = a.Category,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                creatorTime = a.CreatorTime,
                lastModifyTime = a.LastModifyTime,
                sortCode = a.SortCode,
                enabledMark = a.EnabledMark,
                smsSignature = a.SmsSignature,
                channel = SqlFunc.Subqueryable<DictionaryDataEntity>().Where(u => u.DictionaryTypeId == smsSendType.Id && SqlFunc.ToInt64(u.EnCode) == a.Channel).Select(u => u.FullName),
                addressorName = a.AddressorName,
                smtpUser = a.SmtpUser,
                webhookType = SqlFunc.Subqueryable<DictionaryDataEntity>().Where(u => u.DictionaryTypeId == msgWebHookSendType.Id && SqlFunc.ToInt64(u.EnCode) == a.WebhookType).Select(u => u.FullName),
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<MessageAccountListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 详情.
    /// </summary>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        return (await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null)).Adapt<MessageAccountInfoOutput>();
    }
    #endregion

    #region Post

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] MessageAccountListOutput input)
    {
        if (await _repository.IsAnyAsync(x => (x.EnCode == input.enCode) && x.Category == input.type && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        var entity = input.Adapt<MessageAccountEntity>();
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] MessageAccountListOutput input)
    {
        if (await _repository.IsAnyAsync(x => x.Id != id && (x.EnCode == input.enCode) && x.Category == input.type && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        if ((await _repository.AsSugarClient().Queryable<MessageSendTemplateEntity>().AnyAsync(x => x.AccountConfigId == id && x.DeleteMark == null)) && input.enabledMark == 0)
            throw Oops.Oh(ErrorCode.D7013);
        var entity = input.Adapt<MessageAccountEntity>();
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
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
        if (await _repository.AsSugarClient().Queryable<MessageSendTemplateEntity>().AnyAsync(x => x.AccountConfigId == id && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D7012);
        var isOk = await _repository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 复制.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/Copy")]
    public async Task ActionsCopy(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        var random = RandomExtensions.NextLetterAndNumberString(new Random(), 5).ToLower();
        entity.FullName = string.Format("{0}.副本{1}", entity.FullName, random);
        entity.EnCode = string.Format("{0}{1}", entity.EnCode, random);
        entity.EnabledMark = 0;
        entity.LastModifyTime = null;
        entity.LastModifyUserId = null;
        if (entity.Category == "7")
        {
            entity.AppKey = string.Format("{0}{1}", entity.AppKey, random);
        }
        if (entity.FullName.Length >= 50 || entity.EnCode.Length >= 50)
            throw Oops.Oh(ErrorCode.COM1009);
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1008);
    }

    /// <summary>
    /// 钉钉链接测试.
    /// </summary>
    /// <param name="input"></param>
    [HttpPost("testDingTalkConnect")]
    public void testDingTalkConnect([FromBody] MessageAccountListOutput input)
    {
        var dingUtil = new DingUtil(input.appId, input.appSecret);
        if (string.IsNullOrEmpty(dingUtil.token))
            throw Oops.Oh(ErrorCode.D9003);
    }

    /// <summary>
    /// 邮箱链接测试.
    /// </summary>
    /// <param name="input"></param>
    [HttpPost("testSendMail")]
    public async Task EmailTest([FromBody] EmailSendTestQuery input)
    {
        MailParameterInfo mailParameterInfo = new MailParameterInfo()
        {
            SMTPHost = input.smtpServer,
            SMTPPort = input.smtpPort.ParseToInt(),
            Account = input.smtpUser,
            Password = input.smtpPassword,
            Ssl = input.sslLink == 1
        };
        var result = MailUtil.CheckConnected(mailParameterInfo);
        if (!result)
            throw Oops.Oh(ErrorCode.D7006);
        var emailList = new List<string>();
        foreach (var item in input.testSendEmail)
        {
            var receiverUser = await _repository.AsSugarClient().Queryable<UserEntity>().FirstAsync(x => x.Id == item && x.DeleteMark == null);
            if (receiverUser.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1005);
            if (receiverUser.Email.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.D7007, receiverUser.RealName);
            if (!receiverUser.Email.IsEmail()) throw Oops.Oh(ErrorCode.D7008, receiverUser.RealName);
            var mailModel = new MailInfo();
            mailModel.To = receiverUser.Email;
            mailModel.ToName = receiverUser.RealName;
            mailModel.Subject = input.testEmailTitle;
            mailModel.BodyText = HttpUtility.HtmlDecode(input.testEmailContent);
            try
            {
                MailUtil.Send(mailParameterInfo, mailModel);
            }
            catch (Exception ex)
            {
                throw Oops.Oh(ErrorCode.D7014, receiverUser.RealName, ex.Message);
            }
        }
    }

    /// <summary>
    /// 企业微信链接测试.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="input"></param>
    [HttpPost("testQyWebChatConnect")]
    public void testQyWebChatConnect([FromBody] MessageAccountListOutput input)
    {
        var weChatUtil = new WeChatUtil(input.enterpriseId, input.appSecret);
        if (string.IsNullOrEmpty(weChatUtil.accessToken))
            throw Oops.Oh(ErrorCode.D9003);
    }
    #endregion
}
