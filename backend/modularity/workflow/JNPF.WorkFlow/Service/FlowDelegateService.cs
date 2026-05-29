using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Message.Interfaces;
using JNPF.Systems.Entitys.Dto.User;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Interfaces.Permission;
using JNPF.WorkFlow.Entitys.Dto.FlowDelegete;
using JNPF.WorkFlow.Entitys.Dto.FlowTemplate;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Interfaces.Service;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.WorkFlow.Service;

/// <summary>
/// 流程委托.
/// </summary>
[ApiDescriptionSettings(Tag = "WorkflowEngine", Name = "FlowDelegate", Order = 300)]
[Route("api/workflow/Engine/[controller]")]
public class FlowDelegateService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<FlowDelegateEntity> _repository;
    private readonly IFlowTemplateService _flowTemplateService;
    private readonly IMessageManager _messageManager;
    private readonly IOrganizeService _organizeService;
    private readonly IUserManager _userManager;

    public FlowDelegateService(ISqlSugarRepository<FlowDelegateEntity> repository, IFlowTemplateService flowTemplateService, IMessageManager messageManager, IOrganizeService organizeService, IUserManager userManager)
    {
        _repository = repository;
        _flowTemplateService = flowTemplateService;
        _messageManager = messageManager;
        _organizeService = organizeService;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] FlowDelegateQuery input)
    {
        var output = new SqlSugarPagedList<FlowDelegateEntity>();
        if (input.myOrDelagateToMe.Equals("1"))
        {
            //var crUserList = new List<string>();
            //if (!_userManager.IsAdministrator)
            //{
            //    var orgIds = _userManager.DataScope.Select(x => x.organizeId).ToList();//分管组织id
            //    crUserList = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => orgIds.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();
            //    crUserList.Add(_userManager.UserId);
            //}
            //output = await _repository.AsQueryable().Where(x => x.DeleteMark == null).WhereIF(crUserList.Count > 0, x => crUserList.Contains(x.UserId))
            //.WhereIF(!input.keyword.IsNullOrEmpty(), m => m.FlowName.Contains(input.keyword) || m.ToUserName.Contains(input.keyword)).OrderBy(t => t.SortCode)
            //.OrderBy(x => x.CreatorTime, OrderByType.Desc).OrderByIF(!string.IsNullOrEmpty(input.keyword), t => t.LastModifyTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
            output = await _repository.AsQueryable().Where(x => x.DeleteMark == null).Where(x => x.UserId == _userManager.UserId)
            .WhereIF(!input.keyword.IsNullOrEmpty(), m => m.FlowName.Contains(input.keyword) || m.ToUserName.Contains(input.keyword)).OrderBy(t => t.SortCode)
            .OrderBy(x => x.CreatorTime, OrderByType.Desc).OrderByIF(!string.IsNullOrEmpty(input.keyword), t => t.LastModifyTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        }
        else
        {
            output = await _repository.AsQueryable().Where(x => x.ToUserId == _userManager.UserId && x.DeleteMark == null)
            .WhereIF(!input.keyword.IsNullOrEmpty(), m => m.FlowName.Contains(input.keyword) || m.UserName.Contains(input.keyword)).OrderBy(t => t.SortCode)
            .OrderBy(x => x.CreatorTime, OrderByType.Desc).OrderByIF(!string.IsNullOrEmpty(input.keyword), t => t.LastModifyTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        }
        var pageList = new SqlSugarPagedList<FlowDelegeteListOutput>()
        {
            list = output.list.Adapt<List<FlowDelegeteListOutput>>(),
            pagination = output.pagination
        };
        return PageResult<FlowDelegeteListOutput>.SqlSugarPageResult(pageList);
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo_Api(string id)
    {
        return (await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null)).Adapt<FlowDelegeteInfoOutput>();
    }

    /// <summary>
    /// 委托发起流程列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("getflow")]
    public async Task<dynamic> GetFlowList([FromQuery] FlowTemplateListQuery input)
    {
        var output = new List<FlowTemplateTreeOutput>();
        //委托给我的发起流程
        var flowDelegateList = await _repository.GetListAsync(x => x.ToUserId == _userManager.UserId && x.Type == 0 && x.EndTime > DateTime.Now && x.StartTime < DateTime.Now && x.DeleteMark == null);
        if (!flowDelegateList.Any())
        {
            var pageList = new SqlSugarPagedList<FlowTemplateTreeOutput>()
            {
                list = output,
                pagination = new Pagination()
                {
                    CurrentPage = input.currentPage,
                    PageSize = input.pageSize,
                    Total = output.Count
                }
            };
            return PageResult<FlowTemplateTreeOutput>.SqlSugarPageResult(pageList);
        }
        //foreach (var item in flowDelegateList)
        //{
        //    var flowList = await _flowTemplateService.GetFlowFormList(input.flowType.ParseToInt(), item.UserId);
        //    // 非全部流程
        //    if (item.FlowId.IsNotEmptyOrNull())
        //    {
        //        output = output.Union(flowList.FindAll(x => item.FlowId.Contains(x.templateId))).DistinctBy(x => x.id).ToList();
        //    }
        //    else
        //    {
        //        output = output.Union(flowList).DistinctBy(x => x.id).ToList();
        //    }
        //}
        //if (input.keyword.IsNotEmptyOrNull())
        //    output = output.FindAll(o => o.fullName.Contains(input.keyword) || o.enCode.Contains(input.keyword));
        //var pageList = new SqlSugarPagedList<FlowTemplateTreeOutput>()
        //{
        //    list = output.Skip((input.currentPage - 1) * input.pageSize).Take(input.pageSize).ToList(),
        //    pagination = new Pagination()
        //    {
        //        PageIndex = input.currentPage,
        //        PageSize = input.pageSize,
        //        Total = output.Count
        //    }
        //};
        //return PageResult<FlowTemplateTreeOutput>.SqlSugarPageResult(pageList);
        var flowIds = string.Empty;
        if (!flowDelegateList.Any(x => x.FlowId.IsNullOrEmpty()))
        {
            flowIds = string.Join(",", flowDelegateList.Select(x => x.FlowId).ToList());
        }
        var list = await _repository.AsSugarClient().Queryable<FlowTemplateEntity>()
              .Where(a => a.DeleteMark == null && a.EnabledMark == 1)
              .WhereIF(flowIds.IsNotEmptyOrNull(), x => flowIds.Contains(x.Id))
              .WhereIF(input.category.IsNotEmptyOrNull(), a => a.Category == input.category)
              .WhereIF(input.flowType.IsNotEmptyOrNull(), a => a.Type == input.flowType)
              .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
              .Select(a => new FlowTemplateTreeOutput
              {
                  id = a.Id,
                  category = a.Category,
                  enCode = a.EnCode,
                  fullName = a.FullName,
                  sortCode = a.SortCode,
                  creatorTime = a.CreatorTime,
                  lastModifyTime = a.LastModifyTime,
                  icon = a.Icon,
                  iconBackground = a.IconBackground,
                  type = a.Type,
              }).MergeTable().OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc)
              .OrderBy(a => a.lastModifyTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<FlowTemplateTreeOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 发起流程委托人.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("userList")]
    public async Task<dynamic> GetFlowList([FromQuery] string flowId)
    {
        var userList = _repository.AsSugarClient()
          .Queryable<FlowTemplateJsonEntity, FlowDelegateEntity, UserEntity>((a, b, c) => new JoinQueryInfos(JoinType.Left, b.FlowId.Contains(a.TemplateId) || b.FlowName == "全部流程", JoinType.Left, b.UserId == c.Id))
          .Where((a, b, c) => a.Id == flowId && b.Type == 0 && b.ToUserId == _userManager.UserId && b.EndTime > DateTime.Now && b.StartTime < DateTime.Now).Select((a, b, c) => new UserListOutput
          {
              id = c.Id,
              headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", c.HeadIcon),
              fullName = SqlFunc.MergeString(c.RealName, "/", c.Account),
              organizeId = c.OrganizeId
          }).Distinct().ToList();
        if (!userList.Any())
            throw Oops.Oh(ErrorCode.WF0049);
        var orgList = _organizeService.GetOrgListTreeName();
        foreach (var item in userList)
        {
            if (!item.id.Equals(_userManager.GetAdminUserId()))
            {
                item.organize = orgList.FirstOrDefault(x => x.Id == item.organizeId).Description;
            }
        }
        return new { list = userList };
    }
    #endregion

    #region POST

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键值.</param>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (entity == null)
            throw Oops.Oh(ErrorCode.COM1005);
        var isOk = await _repository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="jd">新建参数.</param>
    [HttpPost("")]
    public async Task Create([FromBody] FlowDelegeteCrInput input)
    {
        await Validation(input.Adapt<FlowDelegeteUpInput>());
        var entity = input.Adapt<FlowDelegateEntity>();
        var isOk = await _repository.AsInsertable(entity).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);

        #region 委托通知
        var enCode = entity.Type == 0 ? "MBXTLC012" : "MBXTLC013";
        var paramDic = new Dictionary<string, string>();
        paramDic.Add("@Mandator", _userManager.GetUserName(entity.UserId, false));
        var parameter = new { type = "2" };
        var bodyDic = new Dictionary<string, object>();
        bodyDic.Add(entity.ToUserId, parameter);
        var messageList = _messageManager.GetMessageList(enCode, new List<string>() { entity.ToUserId }, paramDic, 2, bodyDic, 2);
        await _messageManager.SendDefaultMsg(new List<string>() { entity.ToUserId }, messageList);
        #endregion
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="jd">修改参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] FlowDelegeteUpInput input)
    {
        await Validation(input.Adapt<FlowDelegeteUpInput>());
        var entity = input.Adapt<FlowDelegateEntity>();
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 结束委托.
    /// </summary>
    /// <param name="id">id.</param>
    [HttpPut("Stop/{id}")]
    public async Task Create(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (entity == null)
            throw Oops.Oh(ErrorCode.COM1005);
        entity.StartTime = DateTime.Now;
        entity.EndTime = DateTime.Now;
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);

        #region 委托通知
        var paramDic = new Dictionary<string, string>();
        paramDic.Add("@Mandator", _userManager.GetUserName(entity.UserId, false));
        var parameter = new { type = "2" };
        var bodyDic = new Dictionary<string, object>();
        bodyDic.Add(entity.ToUserId, parameter);
        var messageList = _messageManager.GetMessageList("MBXTLC014", new List<string>() { entity.ToUserId }, paramDic, 2, bodyDic, 2);
        await _messageManager.SendDefaultMsg(new List<string>() { entity.ToUserId }, messageList);
        #endregion
    }
    #endregion

    /// <summary>
    /// 委托验证.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private async Task Validation(FlowDelegeteUpInput input)
    {
        if (input.userId.Equals(input.toUserId)) throw Oops.Oh(ErrorCode.WF0001);
        //同委托人、被委托人、委托类型
        var list = await _repository.GetListAsync(x => x.UserId == input.userId && x.ToUserId == input.toUserId && x.Type.ToString() == input.type && x.Id != input.id && x.DeleteMark == null);
        if (list.Any())
        {
            //同一时间段内
            list = list.FindAll(x => !((x.StartTime > input.startTime && x.StartTime > input.endTime) || (x.EndTime < input.startTime && x.EndTime < input.endTime)));
            if (list.Any())
            {
                if (list.Any(x => x.FlowId.IsNullOrEmpty()) || input.flowId.IsNullOrEmpty())
                {
                    throw Oops.Oh(ErrorCode.WF0041);
                }
                else
                {
                    //非全部流程看存不存在相同流程
                    foreach (var item in input.flowId.Split(","))
                    {
                        if (list.Any(x => x.FlowId.Contains(item))) throw Oops.Oh(ErrorCode.WF0041);
                    }
                }
            }
        }
        var list1 = await _repository.GetListAsync(x => x.UserId == input.toUserId && x.ToUserId == input.userId && x.Type.ToString() == input.type && x.Id != input.id && x.DeleteMark == null);
        if (list1.Any())
        {
            //同一时间段内
            list1 = list1.FindAll(x => !((x.StartTime > input.startTime && x.StartTime > input.endTime) || (x.EndTime < input.startTime && x.EndTime < input.endTime)));
            if (list1.Any())
            {
                if (list1.Any(x => x.FlowId.IsNullOrEmpty()) || input.flowId.IsNullOrEmpty())
                {
                    throw Oops.Oh(ErrorCode.WF0042);
                }
                else
                {
                    //非全部流程看存不存在相同流程
                    foreach (var item in input.flowId.Split(","))
                    {
                        if (list1.Any(x => x.FlowId.Contains(item))) throw Oops.Oh(ErrorCode.WF0042);
                    }
                }
            }
        }
    }
}
