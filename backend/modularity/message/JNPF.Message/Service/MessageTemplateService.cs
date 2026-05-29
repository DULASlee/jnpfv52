using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Message.Entitys.Dto.MessageTemplate;
using JNPF.Message.Entitys.Entity;
using JNPF.Message.Entitys.Model.MessageTemplate;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Message.Service;

/// <summary>
/// 消息监控
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "Message", Name = "MessageTemplate", Order = 240)]
[Route("api/message/MessageTemplateConfig")]
public class MessageTemplateService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<MessageTemplateEntity> _repository;
    private readonly IUserManager _userManager;

    public MessageTemplateService(
        ISqlSugarRepository<MessageTemplateEntity> repository, IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
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
        var list = await _repository.AsSugarClient().Queryable<MessageTemplateEntity>()
            .Where(a => a.DeleteMark == null)
            .WhereIF(input.messageSource.IsNotEmptyOrNull(), a => a.MessageSource == input.messageSource)
            .WhereIF(input.messageType.IsNotEmptyOrNull(), a => a.MessageType == input.messageType)
            .WhereIF(input.templateType.IsNotEmptyOrNull(), a => a.TemplateType == input.templateType)
            .WhereIF(input.enabledMark.IsNotEmptyOrNull(), a => a.EnabledMark == input.enabledMark)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), a => a.LastModifyTime, OrderByType.Desc)
            .Select(a => new MessageTemplateListOutput
            {
                id = a.Id,
                fullName = a.FullName,
                enCode = a.EnCode,
                templateType = a.TemplateType,
                messageType = SqlFunc.Subqueryable<DictionaryDataEntity>().Where(u => u.DictionaryTypeId == msgSendType.Id && u.EnCode == a.MessageType).Select(u => u.FullName),
                messageSource = SqlFunc.Subqueryable<DictionaryDataEntity>().Where(u => u.DictionaryTypeId == msgSourceType.Id && u.EnCode == a.MessageSource).Select(u => u.FullName),
                creatorUser = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                creatorTime = a.CreatorTime,
                lastModifyTime = a.LastModifyTime,
                sortCode = a.SortCode,
                enabledMark = a.EnabledMark,
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<MessageTemplateListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 详情.
    /// </summary>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var output = (await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null)).Adapt<MessageTemplateListOutput>();
        output.smsFieldList = (await _repository.AsSugarClient().Queryable<MessageSmsFieldEntity>().Where(x => x.TemplateId == id && x.DeleteMark == null).ToListAsync()).ToObject<List<SmsFieldModel>>();
        output.templateParamList = (await _repository.AsSugarClient().Queryable<MessageTemplateParamEntity>().Where(x => x.TemplateId == id && x.DeleteMark == null).ToListAsync()).ToObject<List<TemplateParamModel>>();
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
    public async Task Create([FromBody] MessageTemplateListOutput input)
    {
        if (await _repository.IsAnyAsync(x => (x.EnCode == input.enCode || x.FullName == input.fullName) && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        if (input.enCode.Contains("MBXTLC"))
            throw Oops.Oh(ErrorCode.D7011);
        var entity = input.Adapt<MessageTemplateEntity>();
        var result = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteReturnEntityAsync();
        if (input.templateParamList.Any())
        {
            foreach (var item in input.templateParamList)
            {
                var paramEntity = item.Adapt<MessageTemplateParamEntity>();
                paramEntity.TemplateId = result.Id;
                await _repository.AsSugarClient().Insertable(paramEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }
        }
        if (input.smsFieldList.Any())
        {
            foreach (var item in input.smsFieldList)
            {
                var smsFieldEntity = item.Adapt<MessageSmsFieldEntity>();
                smsFieldEntity.TemplateId = result.Id;
                await _repository.AsSugarClient().Insertable(smsFieldEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
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
    public async Task Update(string id, [FromBody] MessageTemplateListOutput input)
    {
        if (await _repository.IsAnyAsync(x => x.Id != id && (x.EnCode == input.enCode || x.FullName == input.fullName) && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        if (input.enCode.Contains("MBXTLC"))
            throw Oops.Oh(ErrorCode.D7011);
        var entity = input.Adapt<MessageTemplateEntity>();
        if (input.templateParamList.Any())
        {
            await _repository.AsSugarClient().Deleteable<MessageTemplateParamEntity>(x => x.TemplateId == id).ExecuteCommandAsync();
            foreach (var item in input.templateParamList)
            {
                var paramEntity = item.Adapt<MessageTemplateParamEntity>();
                paramEntity.TemplateId = id;
                await _repository.AsSugarClient().Insertable(paramEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }
        }
        if (input.smsFieldList.Any())
        {
            await _repository.AsSugarClient().Deleteable<MessageSmsFieldEntity>(x => x.TemplateId == id).ExecuteCommandAsync();
            foreach (var item in input.smsFieldList)
            {
                var smsFieldEntity = item.Adapt<MessageSmsFieldEntity>();
                smsFieldEntity.TemplateId = id;
                await _repository.AsSugarClient().Insertable(smsFieldEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
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
        await _repository.AsSugarClient().Deleteable<MessageTemplateParamEntity>(x => x.TemplateId == id).ExecuteCommandAsync();
        await _repository.AsSugarClient().Deleteable<MessageSmsFieldEntity>(x => x.TemplateId == id).ExecuteCommandAsync();
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
        var templateParamList = await _repository.AsSugarClient().Queryable<MessageTemplateParamEntity>().Where(x => x.TemplateId == id && x.DeleteMark == null).ToListAsync();
        foreach (var item in templateParamList)
        {
            var paramEntity = item.Adapt<MessageTemplateParamEntity>();
            paramEntity.TemplateId = entity.Id;
            paramEntity.Id = SnowflakeIdHelper.NextId();
            await _repository.AsSugarClient().Insertable(paramEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
        }
        var smsFieldList = await _repository.AsSugarClient().Queryable<MessageSmsFieldEntity>().Where(x => x.TemplateId == id && x.DeleteMark == null).ToListAsync();
        foreach (var item in smsFieldList)
        {
            var smsFieldEntity = item.Adapt<MessageSmsFieldEntity>();
            smsFieldEntity.TemplateId = entity.Id;
            smsFieldEntity.Id = SnowflakeIdHelper.NextId();
            await _repository.AsSugarClient().Insertable(smsFieldEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1008);
    }
    #endregion
}
