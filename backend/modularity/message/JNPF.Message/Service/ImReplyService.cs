using JNPF.Common.Core.Handlers;
using JNPF.Common.Core.Manager;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Message.Entitys;
using JNPF.Message.Entitys.Dto.ImReply;
using JNPF.Message.Interfaces;
using JNPF.Systems.Entitys.Permission;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Message;

/// <summary>
/// 业务实现：消息会话.
/// </summary>
[ApiDescriptionSettings(Tag = "Message", Name = "imreply", Order = 163)]
[Route("api/message/[controller]")]
public class ImReplyService : IImReplyService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<ImReplyEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// IM中心处理程序.
    /// </summary>
    private IMHandler _imHandler;

    /// <summary>
    /// 初始化一个<see cref="ImReplyService"/>类型的新实例.
    /// </summary>
    public ImReplyService(
        ISqlSugarRepository<ImReplyEntity> repository,
        IUserManager userManager,
        IMHandler imHandler)
    {
        _repository = repository;
        _userManager = userManager;
        _imHandler = imHandler;
    }

    /// <summary>
    /// 获取消息会话列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList()
    {
        var newObjectUserList = new List<ImReplyListOutput>();

        // 获取全部聊天对象列表
        var objectList = _repository.AsSugarClient().UnionAll(
            _repository.AsQueryable().Where(i => i.ReceiveUserId == _userManager.UserId && (SqlFunc.IsNullOrEmpty(i.DeleteUserId) || i.DeleteUserId != _userManager.UserId)).Select(it => new ImReplyObjectIdOutput { userId = it.UserId, latestDate = it.ReceiveTime }),
            _repository.AsQueryable().Where(i => i.UserId == _userManager.UserId && (SqlFunc.IsNullOrEmpty(i.DeleteUserId) || i.DeleteUserId != _userManager.UserId)).Select(it => new ImReplyObjectIdOutput { userId = it.ReceiveUserId, latestDate = it.ReceiveTime })).MergeTable().GroupBy(it => new { it.userId }).Select(it => new { it.userId, latestDate = SqlFunc.AggregateMax(it.latestDate) }).ToList();
        var objectUserList = objectList.Adapt<List<ImReplyListOutput>>();
        if (objectUserList.Count > 0)
        {
            var userList = await _repository.AsSugarClient().Queryable<UserEntity>().In(it => it.Id, objectUserList.Select(it => it.userId).ToArray()).ToListAsync();

            // 将用户信息补齐
            userList.ForEach(item =>
            {
                objectUserList.ForEach(it =>
                {
                    if (it.userId == item.Id)
                    {
                        it.account = item.Account;
                        it.id = it.userId;
                        it.realName = item.RealName;
                        it.headIcon = "/api/File/Image/userAvatar/" + item.HeadIcon;

                        var imContent = _repository.AsSugarClient().Queryable<IMContentEntity>().Where(i => (i.SendUserId == _userManager.UserId && i.ReceiveUserId == it.userId) || (i.SendUserId == it.userId && i.ReceiveUserId == _userManager.UserId)).Where(i => i.SendTime.Equals(it.latestDate) && (SqlFunc.IsNullOrEmpty(i.DeleteUserId) || i.DeleteUserId != _userManager.UserId)).ToList().FirstOrDefault();

                        // 获取最信息
                        if (imContent != null)
                        {
                            it.latestMessage = imContent.Content;
                            it.messageType = imContent.ContentType;
                        }

                        it.unreadMessage = _repository.AsSugarClient().Queryable<IMContentEntity>().Where(i => i.SendUserId == it.userId && i.ReceiveUserId == _userManager.UserId).Where(i => i.EnabledMark == 0 && (SqlFunc.IsNullOrEmpty(i.DeleteUserId) || i.DeleteUserId != _userManager.UserId)).Count();
                    }
                });
            });
        }

        return new { list = objectUserList.OrderByDescending(x => x.latestDate).ToList() };
    }

    /// <summary>
    /// 删除聊天会话.
    /// </summary>
    /// <param name="id">聊天人员UserId</param>
    /// <returns></returns>
    [HttpDelete("relocation/{id}")]
    public async Task DelMsgSession(string id)
    {
        var list = _repository.AsQueryable().Where(i => (i.UserId == _userManager.UserId && i.ReceiveUserId == id) || (i.UserId == id && i.ReceiveUserId == _userManager.UserId)).ToList();
        if (list.Any(x => x.DeleteUserId == id))
        {
            await _repository.AsSugarClient().Deleteable(list).ExecuteCommandAsync();
        }
        else
        {
            await _repository.AsSugarClient().Updateable(list).ReSetValue(it => { it.DeleteUserId = _userManager.UserId; }).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 删除聊天记录.
    /// </summary>
    /// <param name="id">聊天人员UserId</param>
    /// <returns></returns>
    [HttpDelete("deleteChatRecord/{id}")]
    public async Task DelMsgContent(string id)
    {
        var list = _repository.AsSugarClient().Queryable<IMContentEntity>().Where(i => (i.SendUserId == _userManager.UserId && i.ReceiveUserId == id) || (i.SendUserId == id && i.ReceiveUserId == _userManager.UserId)).ToList();
        if (list.Any(x => x.DeleteUserId == id))
        {
            await _repository.AsSugarClient().Deleteable(list).ExecuteCommandAsync();
        }
        else
        {
            await _repository.AsSugarClient().Updateable(list).ReSetValue(it => { it.DeleteUserId = _userManager.UserId; }).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 强制下线.
    /// </summary>
    /// <param name="connectionId"></param>
    [NonAction]
    public async void ForcedOffline(string connectionId)
    {
        await _imHandler.SendMessageAsync(connectionId, new { method = "logout", msg = "此账号已在其他地方登录" }.ToJsonString());
    }
}