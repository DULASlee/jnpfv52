using JNPF.Common.Core.Manager;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extend.Entitys;
using JNPF.Extend.Entitys.Dto.Email;
using JNPF.Message.Entitys;
using JNPF.Message.Entitys.Entity;
using JNPF.Systems.Entitys.Permission;
using JNPF.VisualDev.Entitys.Dto.Dashboard;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Interfaces.Repository;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.VisualDev;

/// <summary>
///  业务实现：主页显示.
/// </summary>
[ApiDescriptionSettings(Tag = "VisualDev", Name = "Dashboard", Order = 174)]
[Route("api/visualdev/[controller]")]
public class DashboardService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<EmailReceiveEntity> _emailReceiveRepository;

    /// <summary>
    /// 流程任务.
    /// </summary>
    private readonly IFlowTaskRepository _flowTaskRepository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="DashboardService"/>类型的新实例.
    /// </summary>
    public DashboardService(
        ISqlSugarRepository<EmailReceiveEntity> emailReceiveRepository,
        IFlowTaskRepository flowTaskRepository,
        IUserManager userManager)
    {
        _emailReceiveRepository = emailReceiveRepository;
        _flowTaskRepository = flowTaskRepository;
        _userManager = userManager;
    }

    #region Get

    /// <summary>
    /// 获取待办事项.
    /// </summary>
    [HttpGet("FlowTodo")]
    public async Task<dynamic> GetFlowTodo()
    {
        dynamic list = await _flowTaskRepository.GetPortalWaitList();
        return new { list = list };
    }

    /// <summary>
    /// 获取我的待办事项.
    /// </summary>
    [HttpGet("MyFlowTodo")]
    public async Task<dynamic> GetMyFlowTodo()
    {
        List<FlowTodoOutput> list = new List<FlowTodoOutput>();
        (await _flowTaskRepository.GetWaitList()).ForEach(l =>
        {
            list.Add(new FlowTodoOutput()
            {
                id = l.id,
                fullName = l.flowName,
                creatorTime = l.creatorTime
            });
        });
        return new { list = list };
    }

    /// <summary>
    /// 获取未读邮件.
    /// </summary>
    [HttpGet("Email")]
    public async Task<dynamic> GetEmail()
    {
        List<EmailHomeOutput>? res = (await _emailReceiveRepository.AsQueryable().Where(x => x.Read == 0 && x.CreatorUserId == _userManager.UserId && x.DeleteMark == null)
            .OrderBy(x => x.CreatorTime, OrderByType.Desc).ToListAsync()).Adapt<List<EmailHomeOutput>>();
        return new { list = res };
    }

    #endregion

    #region Post

    /// <summary>
    /// 获取我的待办.
    /// </summary>
    [HttpPost("FlowTodoCount")]
    public async Task<dynamic> GetFlowTodoCount([FromBody] FlowTodoCountInput input)
    {
        int flowCount = await _emailReceiveRepository.AsSugarClient().Queryable<FlowDelegateEntity>().Where(x => x.CreatorUserId == _userManager.UserId && x.DeleteMark == null).CountAsync();
        var waitList = await _flowTaskRepository.GetWaitList();
        if (input.toBeReviewedType.Any())
        {
            waitList = waitList.FindAll(x => input.toBeReviewedType.Contains(x.flowCategory));
        }
        List<FlowTaskEntity>? trialList = await _flowTaskRepository.GetTrialList();
        if (input.flowDoneType.Any())
        {
            trialList = trialList.FindAll(x => input.flowDoneType.Contains(x.FlowCategory));
        }
        int flowCirculateCount = await _emailReceiveRepository.AsSugarClient().Queryable<FlowTaskEntity, FlowTaskCirculateEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.Id == b.TaskId)).Where((a, b) => b.ObjectId == _userManager.UserId).WhereIF(input.flowCirculateType.Any(), a => input.flowCirculateType.Contains(a.FlowCategory)).CountAsync();
        return new FlowTodoCountOutput()
        {
            toBeReviewed = waitList.Count(),
            entrust = flowCount,
            flowDone = trialList.Count(),
            flowCirculate = flowCirculateCount
        };
    }

    /// <summary>
    /// 获取通知公告.
    /// </summary>
    [HttpPost("Notice")]
    public async Task<dynamic> GetNotice([FromBody] FlowTodoCountInput input)
    {
        var output = new List<NoticeOutput>();
        List<NoticeOutput> list = await _emailReceiveRepository.AsSugarClient().Queryable<MessageEntity>()
            .Where(a => a.Type == 1 && a.DeleteMark == null && a.UserId == _userManager.UserId)
            .OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .Select((a) => new NoticeOutput()
            {
                id = a.Id,
                fullName = a.Title,
                creatorTime = a.CreatorTime,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                excerpt = a.BodyText
            }).ToListAsync();
        foreach (var item in list)
        {
            if (item.excerpt.IsNotEmptyOrNull())
            {
                var noticeDic = item.excerpt.ToObject<Dictionary<string, object>>();
                var noticeId = string.Empty;
                if (noticeDic.ContainsKey("id"))
                {
                    noticeId = noticeDic["id"].ToString();
                }
                if (noticeDic.ContainsKey("Id"))
                {
                    noticeId = noticeDic["Id"].ToString();
                }
                var noticeEntity = _emailReceiveRepository.AsSugarClient().Queryable<NoticeEntity>().First(x => x.DeleteMark == null && x.Id == noticeId);
                if (noticeEntity.IsNotEmptyOrNull())
                {
                    item.category = noticeEntity.Category.Equals("1") ? "公告" : "通知";
                    item.coverImage = noticeEntity.CoverImage;
                    item.excerpt = noticeEntity.Description;
                    item.releaseTime = noticeEntity.LastModifyTime;
                    item.releaseUser = _userManager.GetUserName(noticeEntity.LastModifyUserId);
                    if (input.typeList.Any())
                    {
                        if (input.typeList.Contains(noticeEntity.Category))
                        {
                            output.Add(item);
                        }
                    }
                    else
                    {
                        output.Add(item);
                    }
                }
                if (output.Count == 50)
                    break;
            }
        }

        return new { list = output };
    }
    #endregion
}