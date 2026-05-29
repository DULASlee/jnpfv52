using JNPF.Common.Core.Manager;
using JNPF.Common.Dtos.Message;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Message.Entitys.Dto.MessageTemplate;
using JNPF.Message.Entitys.Dto.SendMessage;
using JNPF.Message.Entitys.Entity;
using JNPF.Message.Entitys.Model.MessageTemplate;
using JNPF.Message.Interfaces;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using JNPF.WorkFlow.Entitys.Entity;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Message.Service;

/// <summary>
/// 发送配置
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "Message", Name = "SendMessage", Order = 240)]
[Route("api/message/SendMessageConfig")]
public class SendMessageService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<MessageSendEntity> _repository;
    private readonly IMessageManager _messageManager;
    private readonly IUserManager _userManager;
    private readonly ISysConfigService _sysConfigService;

    public SendMessageService(
        ISqlSugarRepository<MessageSendEntity> repository,
        IMessageManager messageManager,
        IUserManager userManager,
        ISysConfigService sysConfigService)
    {
        _repository = repository;
        _messageManager = messageManager;
        _userManager = userManager;
        _sysConfigService = sysConfigService;
    }

    #region Get
    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] MessageTemplateQuery input)
    {
        var msgSourceType = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "msgSourceType" && x.DeleteMark == null);
        var msgSendType = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "msgSendType" && x.DeleteMark == null);
        var list = await _repository.AsSugarClient().Queryable<MessageSendEntity>()
            .Where(a => a.DeleteMark == null)
            .WhereIF(input.messageSource.IsNotEmptyOrNull(), a => a.MessageSource == input.messageSource)
            .WhereIF(input.templateType.IsNotEmptyOrNull(), a => a.TemplateType == input.templateType)
            .WhereIF(input.enabledMark.IsNotEmptyOrNull(), a => a.EnabledMark == input.enabledMark)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), a => a.LastModifyTime, OrderByType.Desc)
            .Select(a => new SendMessageListOutput
            {
                id = a.Id,
                fullName = a.FullName,
                enCode = a.EnCode,
                templateType = a.TemplateType,
                messageSource = SqlFunc.Subqueryable<DictionaryDataEntity>().Where(u => u.DictionaryTypeId == msgSourceType.Id && u.EnCode == a.MessageSource).Select(u => u.FullName),
                creatorUser = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                creatorTime = a.CreatorTime,
                lastModifyTime = a.LastModifyTime,
                sortCode = a.SortCode,
                enabledMark = a.EnabledMark,
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        foreach (var item in list.list)
        {
            item.messageType = await _repository.AsSugarClient()
                .Queryable<MessageSendTemplateEntity, DictionaryDataEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.MessageType == b.EnCode && b.DictionaryTypeId == msgSendType.Id))
                .Where((a, b) => a.SendConfigId == item.id && a.DeleteMark == null)
                .Select((a, b) => new MessageTypeModel
                {
                    fullName = b.FullName,
                    type = a.MessageType,
                }).Distinct().ToListAsync();
        }
        return PageResult<SendMessageListOutput>.SqlSugarPageResult(list);
    }

    [HttpGet("getSendConfigList")]
    public async Task<dynamic> GetSendList([FromQuery] MessageTemplateQuery input)
    {
        var list = await _repository.AsSugarClient().Queryable<MessageSendEntity>()
             .Where(a => a.DeleteMark == null && a.EnabledMark == 1 && a.TemplateType == "0")
             .WhereIF(input.messageSource.IsNotEmptyOrNull(), a => a.MessageSource == input.messageSource)
             .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
             .OrderBy(a => a.SortCode)
             .OrderBy(a => a.CreatorTime, OrderByType.Desc)
             .OrderBy(a => a.LastModifyTime, OrderByType.Desc)
             .Select(a => new SendMessageListOutput
             {
                 id = a.Id,
                 fullName = a.FullName,
                 enCode = a.EnCode,
             }).ToPagedListAsync(input.currentPage, input.pageSize);
        foreach (var item in list.list)
        {
            item.templateJson = await SendTest(item.id);
        }
        return PageResult<SendMessageListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 详情.
    /// </summary>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var output = (await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null)).Adapt<SendMessageInfoOutput>();
        output.sendConfigTemplateList = await _repository.AsSugarClient().Queryable<MessageSendTemplateEntity, MessageTemplateEntity, MessageAccountEntity>((a, b, c) => new JoinQueryInfos(JoinType.Left, a.TemplateId == b.Id, JoinType.Left, a.AccountConfigId == c.Id))
            .Where(a => a.SendConfigId == id && a.DeleteMark == null)
            .Select((a, b, c) => new SendTemplateModel
            {
                id = a.Id,
                messageType = a.MessageType,
                sendConfigId = a.SendConfigId,
                templateId = a.TemplateId,
                accountConfigId = a.AccountConfigId,
                templateCode = b.EnCode,
                templateName = b.FullName,
                accountCode = c.EnCode,
                accountName = c.FullName,
                enabledMark = a.EnabledMark,
                sortCode = a.SortCode,
                description = a.Description
            }).ToListAsync();
        return output;
    }
    #endregion

    #region POST

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] SendMessageInfoOutput input)
    {
        if (await _repository.IsAnyAsync(x => (x.EnCode == input.enCode || x.FullName == input.fullName) && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        var sysconfig = await _sysConfigService.GetInfo();
        if (input.sendConfigTemplateList.Any(x => "4".Equals(x.messageType)) && (sysconfig.dingSynAppKey.IsNullOrEmpty() || sysconfig.dingSynAppSecret.IsNullOrEmpty() || sysconfig.dingAgentId.IsNullOrEmpty()))
            throw Oops.Oh(ErrorCode.D7018);
        if (input.sendConfigTemplateList.Any(x => "5".Equals(x.messageType)) && (sysconfig.qyhCorpId.IsNullOrEmpty() || sysconfig.qyhAgentSecret.IsNullOrEmpty() || sysconfig.qyhAgentId.IsNullOrEmpty()))
            throw Oops.Oh(ErrorCode.D7019);
        var entity = input.Adapt<MessageSendEntity>();
        var result = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteReturnEntityAsync();
        if (input.sendConfigTemplateList.Any())
        {
            foreach (var item in input.sendConfigTemplateList)
            {
                var sendTemplateEntity = item.Adapt<MessageSendTemplateEntity>();
                sendTemplateEntity.SendConfigId = result.Id;
                await _repository.AsSugarClient().Insertable(sendTemplateEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }
        }
        if (result.IsNullOrEmpty())
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] SendMessageInfoOutput input)
    {
        if (await _repository.IsAnyAsync(x => x.Id != id && (x.EnCode == input.enCode || x.FullName == input.fullName) && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        var sysconfig = await _sysConfigService.GetInfo();
        if (input.sendConfigTemplateList.Any(x => "4".Equals(x.messageType)) && (sysconfig.dingSynAppKey.IsNullOrEmpty() || sysconfig.dingSynAppSecret.IsNullOrEmpty() || sysconfig.dingAgentId.IsNullOrEmpty()))
            throw Oops.Oh(ErrorCode.D7018);
        if (input.sendConfigTemplateList.Any(x => "5".Equals(x.messageType)) && (sysconfig.qyhCorpId.IsNullOrEmpty() || sysconfig.qyhAgentSecret.IsNullOrEmpty() || sysconfig.qyhAgentId.IsNullOrEmpty()))
            throw Oops.Oh(ErrorCode.D7019);
        var entity = input.Adapt<MessageSendEntity>();
        await _repository.AsSugarClient().Deleteable<MessageSendTemplateEntity>(x => x.SendConfigId == id).ExecuteCommandAsync();
        if (input.sendConfigTemplateList.Any())
        {
            foreach (var item in input.sendConfigTemplateList)
            {
                var sendTemplateEntity = item.Adapt<MessageSendTemplateEntity>();
                sendTemplateEntity.SendConfigId = id;
                await _repository.AsSugarClient().Insertable(sendTemplateEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }
        }
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
        if (await _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().AnyAsync(x => x.SendConfigIds.Contains(id)))
            throw Oops.Oh(ErrorCode.D1007);
        await _repository.AsSugarClient().Deleteable<MessageSendTemplateEntity>(x => x.SendConfigId == id).ExecuteCommandAsync();
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
        entity.Id = SnowflakeIdHelper.NextId();
        entity.EnabledMark = 0;
        entity.TemplateType = "0";
        entity.LastModifyTime = null;
        entity.LastModifyUserId = null;
        entity.CreatorUserId = _userManager.UserId;
        if (entity.FullName.Length >= 50 || entity.EnCode.Length >= 50)
            throw Oops.Oh(ErrorCode.COM1009);
        var sendTemplateList = await _repository.AsSugarClient().Queryable<MessageSendTemplateEntity>().Where(x => x.SendConfigId == id && x.DeleteMark == null).ToListAsync();
        foreach (var item in sendTemplateList)
        {
            var sendTemplateEntity = item.Adapt<MessageSendTemplateEntity>();
            sendTemplateEntity.SendConfigId = entity.Id;
            await _repository.AsSugarClient().Insertable(sendTemplateEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1008);
    }

    /// <summary>
    /// 测试发送.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPost("getTestConfig/{id}")]
    public async Task<List<MessageSendModel>> SendTest(string id)
    {
        return await _messageManager.GetMessageSendModels(id);
    }

    /// <summary>
    /// 测试发送.
    /// </summary>
    /// <param name="input">主键值.</param>
    /// <returns></returns>
    [HttpPost("testSendConfig")]
    public async Task<dynamic> SendTest([FromBody] List<MessageSendModel> input)
    {
        var resultList = new List<object>();
        foreach (var item in input)
        {
            var result = await _messageManager.SendDefinedMsg(item, new Dictionary<string, object>());
            if (result.IsNullOrEmpty())
            {
                resultList.Add(new { isSuccess = "1", messageType = string.Format("消息类型:{0}", item.messageType) });
            }
            else
            {
                resultList.Add(new { isSuccess = "0", messageType = string.Format("消息类型:{0}", item.messageType), result = string.Format("发送{0}失败,失败原因:{1}", item.messageType, result) });
            }
        }
        return resultList;
    }
    #endregion
}
