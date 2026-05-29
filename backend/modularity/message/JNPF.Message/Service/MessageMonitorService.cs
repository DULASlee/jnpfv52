using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.LinqBuilder;
using JNPF.Message.Entitys.Dto.MessageMonitor;
using JNPF.Message.Entitys.Entity;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
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
[ApiDescriptionSettings(Tag = "Message", Name = "MessageMonitor", Order = 240)]
[Route("api/message/[controller]")]
public class MessageMonitorService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<MessageMonitorEntity> _repository;

    public MessageMonitorService(ISqlSugarRepository<MessageMonitorEntity> repository)
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
    public async Task<dynamic> GetList([FromQuery] MessageMonitorQuery input)
    {
        var msgSourceType = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "msgSourceType" && x.DeleteMark == null);
        var msgSendType = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "msgSendType" && x.DeleteMark == null);
        var whereLambda = LinqExpression.And<MessageMonitorEntity>();
        whereLambda = whereLambda.And(a => a.DeleteMark == null);
        if (input.endTime != null && input.startTime != null)
        {
            whereLambda = whereLambda.And(a => SqlFunc.Between(a.SendTime, input.startTime, input.endTime));
        }
        // 关键字（用户、IP地址、功能名称）
        if (!string.IsNullOrEmpty(input.keyword))
            whereLambda = whereLambda.And(a => a.Title.Contains(input.keyword));
        if (input.messageSource.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(a => a.MessageSource.Contains(input.messageSource));
        if (input.messageType.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(m => m.MessageType.Contains(input.messageType));
        var list = await _repository.AsSugarClient().Queryable<MessageMonitorEntity>()
            .Where(whereLambda)
            .OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), a => a.LastModifyTime, OrderByType.Desc)
            .Select(a => new MessageMonitorListOutput
            {
                id = a.Id,
                messageType = SqlFunc.Subqueryable<DictionaryDataEntity>().Where(u => u.DictionaryTypeId == msgSendType.Id && u.EnCode == a.MessageType).Select(u => u.FullName),
                messageSource = SqlFunc.Subqueryable<DictionaryDataEntity>().Where(u => u.DictionaryTypeId == msgSourceType.Id && u.EnCode == a.MessageSource).Select(u => u.FullName),
                title = a.Title,
                sendTime = a.SendTime,
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<MessageMonitorListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 详情.
    /// </summary>
    /// <returns></returns>
    [HttpGet("detail/{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var msgSourceType = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "msgSourceType" && x.DeleteMark == null);
        var msgSendType = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "msgSendType" && x.DeleteMark == null);
        var output = await _repository.AsSugarClient().Queryable<MessageMonitorEntity>().Where(a => a.Id == id && a.DeleteMark == null).Select(a => new MessageMonitorListOutput
        {
            id = a.Id,
            messageType = SqlFunc.Subqueryable<DictionaryDataEntity>().Where(u => u.DictionaryTypeId == msgSendType.Id && u.EnCode == a.MessageType).Select(u => u.FullName),
            messageSource = SqlFunc.Subqueryable<DictionaryDataEntity>().Where(u => u.DictionaryTypeId == msgSourceType.Id && u.EnCode == a.MessageSource).Select(u => u.FullName),
            title = a.Title,
            sendTime = a.SendTime,
            receiveUser = a.ReceiveUser,
            content = a.Content
        }).FirstAsync();
        var userIds = output.receiveUser.ToList<string>();
        var userList = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => userIds.Contains(x.Id)).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)).ToListAsync();
        output.receiveUser = string.Join(",", userList);
        return output;
    }
    #endregion

    #region POST

    /// <summary>
    /// 批量删除.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpDelete("batchRemove")]
    public async Task Delete([FromBody] MessageMonitorDelInput input)
    {
        await _repository.AsDeleteable().In(it => it.Id, input.ids).ExecuteCommandAsync();
    }

    /// <summary>
    /// 一键删除.
    /// </summary>
    /// <returns></returns>
    [HttpDelete("empty")]
    public async Task Delete()
    {
        await _repository.DeleteAsync(x => x.DeleteMark == null);
    }
    #endregion
}
