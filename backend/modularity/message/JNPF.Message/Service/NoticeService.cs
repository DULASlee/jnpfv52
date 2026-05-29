using JNPF.Common.Core.Manager;
using JNPF.Common.Dtos.Message;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Message.Entitys.Dto.Notice;
using JNPF.Message.Entitys.Entity;
using JNPF.Message.Interfaces;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Interfaces.Permission;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Message.Service;

/// <summary>
/// 系统公告
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "Message", Name = "Notice", Order = 240)]
[Route("api/message/[controller]")]
public class NoticeService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<NoticeEntity> _repository;
    private readonly IMessageManager _messageManager;
    private readonly IUserManager _userManager;
    private readonly IUsersService _usersService;
    private readonly IUserRelationService _userRelationService;

    public NoticeService(
        ISqlSugarRepository<NoticeEntity> repository,
        IMessageManager messageManager,
        IUserManager userManager,
        IUsersService usersService,
        IUserRelationService userRelationService)
    {
        _repository = repository;
        _messageManager = messageManager;
        _userManager = userManager;
        _usersService = usersService;
        _userRelationService = userRelationService;
    }

    /// <summary>
    /// 列表（通知公告）.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("List")]
    public async Task<dynamic> GetNoticeList([FromBody] NoticeQueryInput input)
    {
        #region 修改过期状态
        if (await _repository.IsAnyAsync(a => a.ExpirationTime != null && SqlFunc.GetDate() > a.ExpirationTime && a.EnabledMark == 1))
        {
            await _repository.AsUpdateable().SetColumns(it => new NoticeEntity()
            {
                EnabledMark = 2,
            }).Where(a => a.ExpirationTime != null && SqlFunc.GetDate() > a.ExpirationTime && a.EnabledMark == 1).ExecuteCommandAsync();
        }
        #endregion
        var whereLambda = LinqExpression.And<NoticeEntity>();
        if (input.releaseTime.Any())
        {
            whereLambda = whereLambda.And(a => SqlFunc.Between(a.LastModifyTime, input.releaseTime[0], input.releaseTime[1]));
        }
        if (input.expirationTime.Any())
        {
            whereLambda = whereLambda.And(a => SqlFunc.Between(a.ExpirationTime, input.expirationTime[0], input.expirationTime[1]));
        }
        if (input.creatorTime.Any())
        {
            whereLambda = whereLambda.And(a => SqlFunc.Between(a.CreatorTime, input.creatorTime[0], input.creatorTime[1]));
        }
        var list = await _repository.AsSugarClient().Queryable<NoticeEntity>()
            .Where(a => a.DeleteMark == null && a.EnabledMark != 3 && !SqlFunc.IsNullOrEmpty(a.Category))
            .Where(whereLambda)
            .WhereIF(!string.IsNullOrEmpty(input.keyword), a => a.Title.Contains(input.keyword))
            .WhereIF(input.type.Any(), a => input.type.Contains(a.Category))
            .WhereIF(input.enabledMark.Any(), a => input.enabledMark.Contains(SqlFunc.ToString(a.EnabledMark)))
            .WhereIF(input.releaseUser.Any(), a => input.releaseUser.Contains(a.LastModifyUserId))
            .WhereIF(input.creatorUser.Any(), a => input.creatorUser.Contains(a.CreatorUserId))
            .OrderBy(a => a.EnabledMark)
            .OrderBy(a => a.LastModifyTime, OrderByType.Desc)
            .OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .Select((a) => new NoticeOutput
            {
                id = a.Id,
                releaseTime = a.EnabledMark == 0 ? SqlFunc.GetDate() : a.LastModifyTime,
                enabledMark = a.EnabledMark,
                releaseUser = a.EnabledMark == 0 ? "" : SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.LastModifyUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                title = a.Title,
                type = a.Type,
                creatorTime = a.CreatorTime,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                category = a.Category.Equals("1") ? "公告" : "通知",
                excerpt = a.Description,
                expirationTime = a.ExpirationTime
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        // 兼容oracle 的null映射
        foreach (var item in list.list)
        {
            if (item.enabledMark == 0)
            {
                item.releaseTime = null;
            }
        }
        return PageResult<NoticeOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var data = await _repository.AsSugarClient().Queryable<NoticeEntity>()
            .Where(a => a.Id == id && a.DeleteMark == null)
            .Select((a) => new NoticeOutput
            {
                id = a.Id,
                releaseTime = a.LastModifyTime,
                releaseUser = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.LastModifyUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                creatorUser = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                title = a.Title,
                bodyText = a.BodyText,
                files = a.Files,
                toUserIds = a.ToUserIds,
                category = a.Category,
                coverImage = a.CoverImage,
                remindCategory = a.Type,
                sendConfigId = a.SendConfigId,
                excerpt = a.Description,
                expirationTime = a.ExpirationTime,
                sendConfigName = SqlFunc.Subqueryable<MessageSendEntity>().Where(u => u.Id == a.SendConfigId).Select(u => u.FullName),
            }).FirstAsync();
        data.releaseUser = data.releaseUser.IsNotEmptyOrNull() ? data.releaseUser : data.creatorUser;
        return data;
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        try
        {
            await _repository.AsUpdateable().SetColumns(it => new NoticeEntity()
            {
                DeleteMark = 1,
                DeleteUserId = _userManager.UserId,
                DeleteTime = SqlFunc.GetDate()
            }).Where(it => it.Id.Equals(id)).ExecuteCommandAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.COM1002);
        }
    }

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] NoticeInput input)
    {
        var entity = input.Adapt<NoticeEntity>();
        entity.EnabledMark = 0;
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="input">实体对象</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] NoticeInput input)
    {
        var entity = input.Adapt<NoticeEntity>();
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 发布公告.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/Release")]
    public async Task Release(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (entity != null)
        {
            var toUserIds = new List<string>();
            if (entity.ToUserIds.IsNullOrEmpty())
                toUserIds = (await _usersService.GetList()).Select(x => x.Id).ToList();
            else
                toUserIds = _userRelationService.GetUserId(entity.ToUserIds.Split(",").ToList());
            // 发送
            if (entity.Type == 1)
            {
                var paramsDic = new Dictionary<string, string>();
                paramsDic.Add("@Title", entity.Title);
                var messageList = _messageManager.GetMessageList("MBXTGG001", toUserIds, paramsDic, 1, entity.ToObject<Dictionary<string, object>>());
                await _messageManager.SendDefaultMsg(toUserIds, messageList);
            }
            if (entity.Type == 2)
            {
                var messageSendModelList = await _messageManager.GetMessageSendModels(entity.SendConfigId);
                foreach (var item in messageSendModelList)
                {
                    item.toUser = toUserIds;
                    item.paramJson.Clear();
                    item.paramJson.Add(new MessageSendParam
                    {
                        field = "@Title",
                        value = entity.Title
                    });
                    item.paramJson.Add(new MessageSendParam
                    {
                        field = "@CreatorUserName",
                        value = _userManager.GetUserName(entity.CreatorUserId)
                    });
                    item.paramJson.Add(new MessageSendParam
                    {
                        field = "@Content",
                        value = entity.BodyText
                    });
                    item.paramJson.Add(new MessageSendParam
                    {
                        field = "@Remark",
                        value = entity.Description
                    });
                    await _messageManager.SendDefinedMsg(item, entity.ToObject<Dictionary<string, object>>());
                }
            }

            entity.EnabledMark = 1;
            entity.LastModifyTime = DateTime.Now;
            entity.LastModifyUserId = _userManager.UserId;
            _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommand();
        }
    }
}
