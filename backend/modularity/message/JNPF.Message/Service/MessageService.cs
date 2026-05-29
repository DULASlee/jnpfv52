using JNPF.Common.Core.Handlers;
using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Message.Entitys;
using JNPF.Message.Entitys.Dto.Message;
using JNPF.Message.Entitys.Entity;
using JNPF.Message.Interfaces;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Interfaces.Permission;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Message;

/// <summary>
/// 系统消息
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "Message", Name = "message", Order = 240)]
[Route("api/[controller]")]
public class MessageService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<MessageEntity> _repository;
    private readonly IMHandler _imHandler;
    private readonly IMessageManager _messageManager;

    /// <summary>
    /// 用户服务.
    /// </summary>
    private readonly IUsersService _usersService;

    /// <summary>
    /// 用户服务.
    /// </summary>
    private readonly IUserRelationService _userRelationService;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="MessageService"/>类型的新实例.
    /// </summary>
    public MessageService(
        ISqlSugarRepository<MessageEntity> repository,
        IUsersService usersService,
        IUserRelationService userRelationService,
        IMessageManager messageManager,
        IUserManager userManager,
        IMHandler imHandler)
    {
        _repository = repository;
        _usersService = usersService;
        _userRelationService = userRelationService;
        _messageManager = messageManager;
        _userManager = userManager;
        _imHandler = imHandler;
    }

    #region Get

    /// <summary>
    /// 列表（通知公告/系统消息/私信消息）.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetMessageList([FromQuery] MessageListQueryInput input)
    {
        var list = await _repository.AsSugarClient().Queryable<MessageEntity>()
            .Where(a => a.UserId == _userManager.UserId && a.DeleteMark == null)
            .WhereIF(input.type.IsNotEmptyOrNull(), a => a.Type == input.type)
            .WhereIF(input.isRead.IsNotEmptyOrNull(), a => a.IsRead == SqlFunc.ToInt32(input.isRead))
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.Title.Contains(input.keyword))
            .OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), a => a.LastModifyTime, OrderByType.Desc)
            .Select(a => new MessageListOutput
            {
                id = a.Id,
                releaseTime = a.CreatorTime,
                releaseUser = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                title = a.Title,
                type = a.Type,
                isRead = a.IsRead,
                flowType = a.FlowType
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<MessageListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 读取消息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("ReadInfo/{id}")]
    public async Task<dynamic> ReadInfo(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null && x.UserId == _userManager.UserId);
        var output = new MessageReadInfoOutput();
        if (entity.IsNotEmptyOrNull())
        {
            output.id = entity.Id;
            output.releaseTime = entity.CreatorTime;
            output.title = entity.Title;
            output.releaseUser = await _usersService.GetUserName(entity.CreatorUserId);
            if (entity.Type == 1)
            {
                var noticeEntity = entity.BodyText.ToObject<NoticeEntity>();
                output.bodyText = noticeEntity.BodyText;
                output.files = noticeEntity.Files;
                output.excerpt = noticeEntity.Description;
            }
            else
            {
                output.bodyText = entity.BodyText;
            }
            await MessageRead(id, null, null, null);
        }
        return output;
    }

    /// <summary>
    /// 读取消息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("getUnReadMsgNum")]
    public async Task<dynamic> GetUnReadMsgNum()
    {
        var unreadNoticeCount = await _repository.CountAsync(x => x.Type == 1 && x.DeleteMark == null && x.UserId == _userManager.UserId && x.IsRead == 0);
        var unreadMessageCount = await _repository.CountAsync(x => x.Type == 2 && x.DeleteMark == null && x.UserId == _userManager.UserId && x.IsRead == 0);
        var unreadSystemMessageCount = await _repository.CountAsync(x => x.Type == 3 && x.DeleteMark == null && x.UserId == _userManager.UserId && x.IsRead == 0);
        var unreadScheduleCount = await _repository.CountAsync(x => x.Type == 4 && x.DeleteMark == null && x.UserId == _userManager.UserId && x.IsRead == 0);
        var unreadTotalCount = await _repository.CountAsync(x => x.DeleteMark == null && x.UserId == _userManager.UserId && x.IsRead == 0);
        return new { unReadMsg = unreadMessageCount, unReadNotice = unreadNoticeCount, unReadSystemMsg = unreadSystemMessageCount, unReadSchedule = unreadScheduleCount, unReadNum = unreadTotalCount };
    }

    #endregion

    #region Post

    /// <summary>
    /// 全部已读.
    /// </summary>
    /// <returns></returns>
    [HttpPost("Actions/ReadAll")]
    public async Task AllRead([FromQuery] string isRead, [FromQuery] string keyword, [FromQuery] string type)
    {
        await MessageRead(string.Empty, isRead, keyword, type);
    }

    /// <summary>
    /// 删除记录.
    /// </summary>
    /// <param name="postParam">请求参数.</param>
    /// <returns></returns>
    [HttpDelete("Record")]
    public async Task DeleteRecord([FromBody] dynamic postParam)
    {
        string[] ids = postParam.ids.ToString().Split(',');
        var isOk = await _repository.AsSugarClient().Deleteable<MessageEntity>().Where(m => m.UserId == _userManager.UserId && ids.Contains(m.Id)).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }
    #endregion

    #region PublicMethod

    /// <summary>
    /// 消息已读（全部）.
    /// </summary>
    /// <param name="id">id.</param>
    [NonAction]
    private async Task MessageRead(string id, string isRead, string keyword, string type)
    {
        var ids = await _repository.AsSugarClient().Queryable<MessageEntity>()
            .Where(a => a.UserId == _userManager.UserId && a.DeleteMark == null)
            .WhereIF(id.IsNotEmptyOrNull(), a => a.Id == id)
            .WhereIF(type.IsNotEmptyOrNull(), a => a.Type == SqlFunc.ToInt32(type))
            .WhereIF(!string.IsNullOrEmpty(keyword), a => a.Title.Contains(keyword))
            .Select(a => a.Id).ToListAsync();
        if (!_repository.AsSugarClient().Queryable<MessageEntity>().Any(x => x.IsRead == 0 && x.UserId == _userManager.UserId) && id.IsNullOrEmpty())
        {
            throw Oops.Oh(ErrorCode.D7017);
        }
        await _repository.AsSugarClient().Updateable<MessageEntity>().SetColumns(it => it.ReadCount == it.ReadCount + 1).SetColumns(x => new MessageEntity()
        {
            IsRead = 1,
            ReadTime = DateTime.Now
        }).Where(x => ids.Contains(x.Id)).ExecuteCommandAsync();
    }
    #endregion
}