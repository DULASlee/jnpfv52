using JNPF.Common.Core.Manager;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Models.User;
using JNPF.Common.Models.WorkFlow;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.VisualDev.Entitys;
using JNPF.WorkFlow.Entitys;
using JNPF.WorkFlow.Entitys.Dto.FlowBefore;
using JNPF.WorkFlow.Entitys.Dto.FlowLaunch;
using JNPF.WorkFlow.Entitys.Dto.FlowMonitor;
using JNPF.WorkFlow.Entitys.Dto.WorkFlowForm.LeaveApply;
using JNPF.WorkFlow.Entitys.Dto.WorkFlowForm.SalesOrder;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Enum;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.Properties;
using JNPF.WorkFlow.Interfaces.Repository;
using Mapster;
using SqlSugar;
using System.Linq.Expressions;

namespace JNPF.WorkFlow.Repository;

/// <summary>
/// 流程任务数据处理.
/// </summary>
public class FlowTaskRepository : IFlowTaskRepository, ITransient
{
    private readonly ISqlSugarRepository<FlowTaskEntity> _repository;
    private readonly IUserManager _userManager;
    private readonly ITenant _db;

    /// <summary>
    /// 构造.
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="userManager"></param>
    /// <param name="context"></param>
    public FlowTaskRepository(
        ISqlSugarRepository<FlowTaskEntity> repository,
        IUserManager userManager,
        ISqlSugarClient context)
    {
        _repository = repository;
        _userManager = userManager;
        _db = context.AsTenant();
    }

    #region 流程列表

    /// <summary>
    /// 列表（流程监控）.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    public async Task<dynamic> GetMonitorList(FlowMonitorListQuery input)
    {
        var objList = new List<string>();
        if (!_userManager.IsAdministrator)
        {
            var orgIds = _userManager.DataScope.Where(x => x.Select).Select(x => x.organizeId).ToList();//分管组织id
            objList = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => orgIds.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();
            if (!objList.Any())
            {
                var pageList = new SqlSugarPagedList<FlowBeforeListOutput>()
                {
                    list = new List<FlowBeforeListOutput>(),
                    pagination = new Pagination()
                    {
                        CurrentPage = input.currentPage,
                        PageSize = input.pageSize,
                        Total = 0
                    }
                };
                return PageResult<FlowBeforeListOutput>.SqlSugarPageResult(pageList);
            }
        }
        var whereLambda = LinqExpression.And<FlowMonitorListOutput>();
        if (!input.startTime.IsNullOrEmpty() && !input.endTime.IsNullOrEmpty())
        {
            whereLambda = whereLambda.And(a => SqlFunc.Between(a.startTime, input.startTime, input.endTime));
        }
        if (input.creatorUserId.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(x => x.creatorUserId == input.creatorUserId);
        if (!input.flowCategory.IsNullOrEmpty())
            whereLambda = whereLambda.And(x => input.flowCategory.Contains(x.flowCategory));
        if (!input.templateId.IsNullOrEmpty())
            whereLambda = whereLambda.And(x => x.templateId == input.templateId);
        var flowJosnEntity = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().First(x => x.Id == input.flowId && x.DeleteMark == null);
        if (flowJosnEntity.IsNotEmptyOrNull())
        {
            var flowIds = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().Where(x => x.GroupId == flowJosnEntity.GroupId).Select(x => x.Id).ToList();
            whereLambda = whereLambda.And(x => flowIds.Contains(x.flowId));
        }
        if (!input.status.IsNullOrEmpty())
        {
            if (input.status == 4)
            {
                whereLambda = whereLambda.And(x => x.status == input.status || x.status == 7);
            }
            else
            {
                whereLambda = whereLambda.And(x => x.status == input.status);
            }
        }
        if (!input.flowUrgent.IsNullOrEmpty())
        {
            if (input.flowUrgent == 1)
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent || x.flowUrgent == 0);
            }
            else
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent);
            }
        }
        if (!input.keyword.IsNullOrEmpty())
            whereLambda = whereLambda.And(m => m.enCode.Contains(input.keyword) || m.fullName.Contains(input.keyword));
        var list = await _repository.AsSugarClient().Queryable<FlowTaskEntity, FlowTemplateEntity>(
            (a, b) => new JoinQueryInfos(JoinType.Left, a.TemplateId == b.Id))
            .WhereIF(!_userManager.IsAdministrator, (a, b) => objList.Contains(a.CreatorUserId))
            .Where((a, b) => a.Status > 0 && a.DeleteMark == null)
            .Select((a) => new FlowMonitorListOutput()
            {
                completion = a.Completion,
                creatorTime = a.CreatorTime,
                creatorUserId = a.CreatorUserId,
                description = a.Description,
                enCode = a.EnCode,
                flowCategory = a.FlowCategory,
                flowCode = a.FlowCode,
                flowId = a.FlowId,
                flowName = a.FlowName,
                flowUrgent = a.FlowUrgent,
                fullName = a.FullName,
                id = a.Id,
                processId = a.ProcessId,
                startTime = a.StartTime,
                thisStep = a.ThisStep,
                userName = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                status = SqlFunc.IIF(a.Suspend == 1, 6, a.Status),
                sortCode = a.SortCode,
                templateId = a.TemplateId,
                suspend = a.Suspend,
                flowVersion = a.FlowVersion,
            }).MergeTable().Where(whereLambda)
           .OrderBy(a => a.creatorTime, OrderByType.Desc)
           .ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<FlowMonitorListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 列表（我发起的）.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    public async Task<dynamic> GetLaunchList(FlowLaunchListQuery input)
    {
        var whereLambda = LinqExpression.And<FlowLaunchListOutput>();
        if ("true".Equals(input.delegateType))
        {
            whereLambda = whereLambda.And(x => x.delegateUser == _userManager.UserId);
        }
        else
        {
            whereLambda = whereLambda.And(x => x.creatorUserId == _userManager.UserId);
        }
        if (!input.startTime.IsNullOrEmpty() && !input.endTime.IsNullOrEmpty())
        {
            whereLambda = whereLambda.And(a => SqlFunc.Between(a.startTime, input.startTime, input.endTime));
        }
        if (!input.flowCategory.IsNullOrEmpty())
            whereLambda = whereLambda.And(x => input.flowCategory.Contains(x.flowCategory));
        if (!input.templateId.IsNullOrEmpty())
            whereLambda = whereLambda.And(x => x.templateId == input.templateId);
        var flowJosnEntity = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().First(x => x.Id == input.flowId && x.DeleteMark == null);
        if (flowJosnEntity.IsNotEmptyOrNull())
        {
            var flowIds = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().Where(x => x.GroupId == flowJosnEntity.GroupId).Select(x => x.Id).ToList();
            whereLambda = whereLambda.And(x => flowIds.Contains(x.flowId));
        }
        if (!input.status.IsNullOrEmpty())
        {
            if (input.status == 4)
            {
                whereLambda = whereLambda.And(x => x.status == input.status || x.status == 7);
            }
            else
            {
                whereLambda = whereLambda.And(x => x.status == input.status);
            }
        }
        if (!input.flowUrgent.IsNullOrEmpty())
        {
            if (input.flowUrgent == 1)
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent || x.flowUrgent == 0);
            }
            else
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent);
            }
        }
        if (!input.keyword.IsNullOrEmpty())
            whereLambda = whereLambda.And(m => m.enCode.Contains(input.keyword) || m.fullName.Contains(input.keyword));
        var list = await _repository.AsSugarClient().Queryable<FlowTaskEntity>().Where((a) => a.DeleteMark == null).Select(a => new FlowLaunchListOutput()
        {
            id = a.Id,
            fullName = a.FullName,
            flowName = a.FlowName,
            startTime = a.StartTime,
            creatorTime = a.CreatorTime,
            thisStep = a.ThisStep,
            flowUrgent = a.FlowUrgent,
            enCode = a.EnCode,
            status = SqlFunc.IIF(a.Suspend == 1, 6, a.Status),
            flowCategory = a.FlowCategory,
            flowCode = a.FlowCode,
            completion = a.Completion,
            creatorUserId = a.CreatorUserId,
            endTime = a.EndTime,
            flowId = a.FlowId,
            templateId = a.TemplateId,
            sortCode = a.SortCode,
            delegateUser = a.DelegateUserId,
            suspend = a.Suspend,
        }).MergeTable().Where(whereLambda).OrderBy(a => a.status).OrderBy(a => a.startTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<FlowLaunchListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 列表（待我审批）.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    public async Task<dynamic> GetWaitList(FlowBeforeListQuery input)
    {
        var whereLambda = LinqExpression.And<FlowBeforeListOutput>();
        if (!input.startTime.IsNullOrEmpty() && !input.endTime.IsNullOrEmpty())
        {
            whereLambda = whereLambda.And(a => SqlFunc.Between(a.startTime, input.startTime, input.endTime));
        }
        if (input.flowCategory.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(x => input.flowCategory.Contains(x.flowCategory));
        if (input.templateId.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(x => x.templateId == input.templateId);
        var flowJosnEntity = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().First(x => x.Id == input.flowId && x.DeleteMark == null);
        if (flowJosnEntity.IsNotEmptyOrNull())
        {
            var flowIds = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().Where(x => x.GroupId == flowJosnEntity.GroupId).Select(x => x.Id).ToList();
            whereLambda = whereLambda.And(x => flowIds.Contains(x.flowId));
        }
        if (input.keyword.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(m => m.enCode.Contains(input.keyword) || m.fullName.Contains(input.keyword));
        if (input.creatorUserId.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(m => m.creatorUserId.Contains(input.creatorUserId));
        if (!input.flowUrgent.IsNullOrEmpty())
        {
            if (input.flowUrgent == 1)
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent || x.flowUrgent == 0);
            }
            else
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent);
            }
        }

        // 经办审核
        var list1 = _repository.AsSugarClient().Queryable<FlowTaskEntity, FlowTaskOperatorEntity>(
            (a, b) => new JoinQueryInfos(JoinType.Left, a.Id == b.TaskId))
            .Where((a, b) => a.Status == 1 && a.DeleteMark == null && b.Completion == 0 && b.State == 0 && a.Suspend == null
            && b.HandleId == _userManager.UserId && b.CreatorTime < SqlFunc.GetDate())
            .Select((a, b) => new FlowBeforeListOutput()
            {
                enCode = a.EnCode,
                creatorUserId = a.CreatorUserId,
                creatorTime = b.CreatorTime,
                thisStep = b.NodeName,
                thisStepId = b.TaskNodeId,
                flowCategory = a.FlowCategory,
                fullName = a.FullName,
                flowName = a.FlowName,
                status = a.Status,
                id = b.Id,
                userName = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                description = b.Description,
                flowCode = a.FlowCode,
                flowId = a.FlowId,
                processId = a.ProcessId,
                flowUrgent = a.FlowUrgent,
                startTime = a.CreatorTime,
                completion = a.Completion,
                nodeName = b.NodeName,
                delegateUser = "",//适配oracle
                suspend = a.Suspend,
                templateId = a.TemplateId,
            });

        // 委托审核
        var list2 = _repository.AsSugarClient().Queryable<FlowTaskEntity, FlowTaskOperatorEntity, FlowDelegateEntity>(
            (a, b, c) => new JoinQueryInfos(JoinType.Left, a.Id == b.TaskId, JoinType.Left, (c.FlowId.Contains(a.TemplateId) || c.FlowName == "全部流程")
        && c.EndTime > DateTime.Now))
            .Where((a, b, c) => a.Status == 1 && a.DeleteMark == null && b.Completion == 0 && b.State == 0 && a.Suspend == null
            && b.HandleId == c.UserId && c.Type == 1 && c.ToUserId == _userManager.UserId && c.DeleteMark == null
            && c.EnabledMark == 1 && c.EndTime > DateTime.Now && c.StartTime < DateTime.Now
            && b.CreatorTime < SqlFunc.GetDate())
            .Select((a, b, c) => new FlowBeforeListOutput()
            {
                enCode = a.EnCode,
                creatorUserId = a.CreatorUserId,
                creatorTime = b.CreatorTime,
                thisStep = b.NodeName,
                thisStepId = b.TaskNodeId,
                flowCategory = a.FlowCategory,
                fullName = a.FullName,
                flowName = a.FlowName,
                status = a.Status,
                id = b.Id,
                userName = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                description = b.Description,
                flowCode = a.FlowCode,
                flowId = a.FlowId,
                processId = a.ProcessId,
                flowUrgent = a.FlowUrgent,
                startTime = a.CreatorTime,
                completion = a.Completion,
                nodeName = b.NodeName,
                delegateUser = c.ToUserId,
                suspend = a.Suspend,
                templateId = a.TemplateId,
            });
        var output = await _repository.AsSugarClient().UnionAll(list1, list2).Where(whereLambda).MergeTable().OrderBy(x => x.creatorTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<FlowBeforeListOutput>.SqlSugarPageResult(output);
    }

    /// <summary>
    /// 列表（批量审批）.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    public async Task<dynamic> GetBatchWaitList(FlowBeforeListQuery input)
    {
        var whereLambda = LinqExpression.And<FlowBeforeListOutput>();
        if (!input.startTime.IsNullOrEmpty() && !input.endTime.IsNullOrEmpty())
        {
            whereLambda = whereLambda.And(a => SqlFunc.Between(a.startTime, input.startTime, input.endTime));
        }
        if (!input.flowCategory.IsNullOrEmpty())
            whereLambda = whereLambda.And(x => input.flowCategory.Contains(x.flowCategory));
        if (!input.templateId.IsNullOrEmpty())
            whereLambda = whereLambda.And(x => x.templateId == input.templateId);
        if (!input.flowId.IsNullOrEmpty())
            whereLambda = whereLambda.And(x => x.flowId == input.flowId);
        if (!input.keyword.IsNullOrEmpty())
            whereLambda = whereLambda.And(m => m.enCode.Contains(input.keyword) || m.fullName.Contains(input.keyword));
        if (!input.creatorUserId.IsNullOrEmpty())
            whereLambda = whereLambda.And(m => m.creatorUserId.Contains(input.creatorUserId));
        if (!input.flowUrgent.IsNullOrEmpty())
        {
            if (input.flowUrgent == 1)
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent || x.flowUrgent == 0);
            }
            else
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent);
            }
        }
        if (!input.nodeCode.IsNullOrEmpty())
            whereLambda = whereLambda.And(m => m.nodeCode.Contains(input.nodeCode));
        //经办审核
        var list1 = _repository.AsSugarClient().Queryable<FlowTaskEntity, FlowTaskOperatorEntity, FlowTaskNodeEntity>((a, b, c) =>
         new JoinQueryInfos(JoinType.Left, a.Id == b.TaskId, JoinType.Left, b.TaskNodeId == c.Id))
            .Where((a, b, c) => a.Status == 1 && a.DeleteMark == null && b.Completion == 0 && b.State == 0 && a.Suspend == null
            && b.CreatorTime < SqlFunc.GetDate()
            && b.HandleId == _userManager.UserId && a.IsBatch == 1)
            .Select((a, b, c) => new FlowBeforeListOutput()
            {
                enCode = a.EnCode,
                creatorUserId = a.CreatorUserId,
                creatorTime = b.CreatorTime,
                thisStep = b.NodeName,
                thisStepId = b.TaskNodeId,
                flowCategory = a.FlowCategory,
                fullName = a.FullName,
                flowName = a.FlowName,
                status = a.Status,
                id = b.Id,
                userName = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                description = a.Description,
                flowCode = a.FlowCode,
                flowId = a.FlowId,
                processId = a.ProcessId,
                flowUrgent = a.FlowUrgent,
                startTime = a.CreatorTime,
                completion = a.Completion,
                nodeName = b.NodeName,
                approversProperties = c.NodePropertyJson,
                flowVersion = a.FlowVersion,
                nodeCode = b.NodeCode,
                templateId = a.TemplateId,
                delegateUser = "",//适配oracle
                suspend = a.Suspend,
            });
        //委托审核
        var list2 = _repository.AsSugarClient().Queryable<FlowTaskEntity, FlowTaskOperatorEntity, FlowDelegateEntity, FlowTaskNodeEntity>((a, b, c, d) =>
         new JoinQueryInfos(JoinType.Left, a.Id == b.TaskId, JoinType.Left, (c.FlowId.Contains(a.TemplateId) || c.FlowName == "全部流程")
         && c.EndTime > DateTime.Now, JoinType.Left, b.TaskNodeId == d.Id))
            .Where((a, b, c) => a.Status == 1 && a.DeleteMark == null && b.Completion == 0 && b.State == 0 && a.IsBatch == 1 && a.Suspend == null
            && b.HandleId == c.UserId && c.Type == 1 && b.CreatorTime < SqlFunc.GetDate()
            && c.ToUserId == _userManager.UserId && c.DeleteMark == null && c.EnabledMark == 1 && c.EndTime > DateTime.Now && c.StartTime < DateTime.Now)
            .Select((a, b, c, d) => new FlowBeforeListOutput()
            {
                enCode = a.EnCode,
                creatorUserId = a.CreatorUserId,
                creatorTime = b.CreatorTime,
                thisStep = b.NodeName,
                thisStepId = b.TaskNodeId,
                flowCategory = a.FlowCategory,
                fullName = a.FullName,
                flowName = a.FlowName,
                status = a.Status,
                id = b.Id,
                userName = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                description = a.Description,
                flowCode = a.FlowCode,
                flowId = a.FlowId,
                processId = a.ProcessId,
                flowUrgent = a.FlowUrgent,
                startTime = a.CreatorTime,
                completion = a.Completion,
                nodeName = b.NodeName,
                approversProperties = d.NodePropertyJson,
                flowVersion = a.FlowVersion,
                nodeCode = b.NodeCode,
                templateId = a.TemplateId,
                delegateUser = c.ToUserId,
                suspend = a.Suspend,
            });
        var output = await _repository.AsSugarClient().UnionAll(list1, list2).Where(whereLambda).MergeTable().OrderBy(x => x.creatorTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<FlowBeforeListOutput>.SqlSugarPageResult(output);
    }

    /// <summary>
    /// 列表（我已审批）.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    public async Task<dynamic> GetTrialList(FlowBeforeListQuery input)
    {
        var statusList = new List<int>() { 0, 1, 10, 13 };// 同意、拒绝、前签
        var whereLambda = LinqExpression.And<FlowBeforeListOutput>();
        if (!input.startTime.IsNullOrEmpty() && !input.endTime.IsNullOrEmpty())
        {
            whereLambda = whereLambda.And(a => SqlFunc.Between(a.startTime, input.startTime, input.endTime));
        }
        if (!input.flowCategory.IsNullOrEmpty())
            whereLambda = whereLambda.And(x => input.flowCategory.Contains(x.flowCategory));
        if (input.templateId.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(x => x.templateId == input.templateId);
        var flowJosnEntity = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().First(x => x.Id == input.flowId && x.DeleteMark == null);
        if (flowJosnEntity.IsNotEmptyOrNull())
        {
            var flowIds = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().Where(x => x.GroupId == flowJosnEntity.GroupId).Select(x => x.Id).ToList();
            whereLambda = whereLambda.And(x => flowIds.Contains(x.flowId));
        }
        if (!input.creatorUserId.IsNullOrEmpty())
            whereLambda = whereLambda.And(m => m.creatorUserId.Contains(input.creatorUserId));
        if (!input.keyword.IsNullOrEmpty())
            whereLambda = whereLambda.And(m => m.enCode.Contains(input.keyword) || m.fullName.Contains(input.keyword));
        if (!input.flowUrgent.IsNullOrEmpty())
        {
            if (input.flowUrgent == 1)
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent || x.flowUrgent == 0);
            }
            else
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent);
            }
        }
        var list = await _repository.AsSugarClient().Queryable<FlowTaskOperatorRecordEntity>()
            .GroupBy(it => new { it.TaskId, it.TaskNodeId, it.HandleId }).Where(a => statusList.Contains(a.HandleStatus) && a.HandleId == _userManager.UserId && a.TaskOperatorId != null)
            .Select(a => new { TaskId = a.TaskId, TaskNodeId = a.TaskNodeId, HandleId = a.HandleId, HandleTime = SqlFunc.AggregateMax(a.HandleTime) })
            .MergeTable().LeftJoin<FlowTaskOperatorRecordEntity>((a, b) => a.TaskId == b.TaskId && a.TaskNodeId == b.TaskNodeId && a.HandleId == b.HandleId)
            .LeftJoin<FlowTaskEntity>((a, b, c) => b.TaskId == c.Id).LeftJoin<FlowTaskOperatorEntity>((a, b, c, d) => b.TaskOperatorId == d.Id)
            .Where((a, b, c) => a.HandleTime == b.HandleTime && statusList.Contains(b.HandleStatus) && b.TaskOperatorId != null && b.HandleId == _userManager.UserId)
            .Select((a, b, c, d) => new FlowBeforeListOutput()
            {
                enCode = c.EnCode,
                creatorUserId = c.CreatorUserId,
                creatorTime = b.HandleTime,
                thisStep = b.NodeName,
                thisStepId = b.TaskNodeId,
                flowCategory = c.FlowCategory,
                fullName = c.FullName,
                flowName = c.FlowName,
                status = b.HandleStatus,
                id = b.Id,
                userName = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == c.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                description = c.Description,
                flowCode = c.FlowCode,
                flowId = c.FlowId,
                processId = c.ProcessId,
                flowUrgent = c.FlowUrgent,
                startTime = c.CreatorTime,
                templateId = c.TemplateId,
                delegateUser = b.HandleId == null || b.HandleId == d.HandleId || c.Id == null ? null : d.HandleId,
                suspend = c.Suspend,
            }).MergeTable().Where(whereLambda).OrderBy(a => a.creatorTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<FlowBeforeListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 列表（抄送我的）.
    /// </summary>
    /// <param name="input">请求参数</param>
    /// <returns></returns>
    public async Task<dynamic> GetCirculateList(FlowBeforeListQuery input)
    {
        var whereLambda = LinqExpression.And<FlowBeforeListOutput>();
        if (!input.startTime.IsNullOrEmpty() && !input.endTime.IsNullOrEmpty())
        {
            whereLambda = whereLambda.And(a => SqlFunc.Between(a.startTime, input.startTime, input.endTime));
        }
        if (!input.flowCategory.IsNullOrEmpty())
            whereLambda = whereLambda.And(x => input.flowCategory.Contains(x.flowCategory));
        if (!input.templateId.IsNullOrEmpty())
            whereLambda = whereLambda.And(x => x.templateId == input.templateId);
        var flowJosnEntity = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().First(x => x.Id == input.flowId && x.DeleteMark == null);
        if (flowJosnEntity.IsNotEmptyOrNull())
        {
            var flowIds = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().Where(x => x.GroupId == flowJosnEntity.GroupId).Select(x => x.Id).ToList();
            whereLambda = whereLambda.And(x => flowIds.Contains(x.flowId));
        }
        if (!input.creatorUserId.IsNullOrEmpty())
            whereLambda = whereLambda.And(m => m.creatorUserId.Contains(input.creatorUserId));
        if (!input.keyword.IsNullOrEmpty())
            whereLambda = whereLambda.And(m => m.enCode.Contains(input.keyword) || m.fullName.Contains(input.keyword));
        if (!input.flowUrgent.IsNullOrEmpty())
        {
            if (input.flowUrgent == 1)
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent || x.flowUrgent == 0);
            }
            else
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent);
            }
        }
        var list = await _repository.AsSugarClient().Queryable<FlowTaskEntity, FlowTaskCirculateEntity, UserEntity>((a, b, c) => new JoinQueryInfos(JoinType.Left, a.Id == b.TaskId, JoinType.Left, a.CreatorUserId == c.Id)).Where((a, b) => b.ObjectId == _userManager.UserId).Select((a, b, c) => new FlowBeforeListOutput()
        {
            enCode = a.EnCode,
            creatorUserId = a.CreatorUserId,
            creatorTime = b.CreatorTime,
            thisStep = b.NodeName,
            thisStepId = b.TaskNodeId,
            flowCategory = a.FlowCategory,
            fullName = a.FullName,
            flowName = a.FlowName,
            status = a.Status,
            id = b.Id,
            userName = SqlFunc.MergeString(c.RealName, "/", c.Account),
            description = a.Description,
            flowCode = a.FlowCode,
            flowId = a.FlowId,
            processId = a.ProcessId,
            flowUrgent = a.FlowUrgent,
            startTime = a.CreatorTime,
            suspend = a.Suspend,
            templateId = a.TemplateId,
        }).MergeTable().Where(whereLambda).OrderBy(x => x.creatorTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<FlowBeforeListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 批量流程列表.
    /// </summary>
    /// <returns></returns>
    public async Task<dynamic> BatchFlowSelector()
    {
        var list = (await GetWaitList()).FindAll(x => x.isBatch == 1);
        var output = new List<object>();
        foreach (var item in list.GroupBy(x => x.templateId))
        {
            output.Add(new {
                id = item.Key,
                fullName = string.Format("{0}({1})", item.FirstOrDefault().flowName, item.Count()),
                count = item.Count()
            });
        };
        return output;
    }

    /// <summary>
    /// 根据分类获取审批意见.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="category"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public async Task<List<FlowBeforeRecordListModel>> GetRecordListByCategory(string taskId, string category, string type = "0")
    {
        var recordList = new List<FlowBeforeRecordListModel>();
        switch (category)
        {
            case "1":
                return await _repository.AsSugarClient().Queryable<FlowTaskOperatorRecordEntity, UserEntity, OrganizeEntity>((a, b, c) =>
         new JoinQueryInfos(JoinType.Left, a.HandleId == b.Id, JoinType.Left, b.OrganizeId == c.Id)).Where(a => a.TaskId == taskId)
         .WhereIF(type == "1", (a) => a.HandleStatus == 0 || a.HandleStatus == 1).Select((a, b, c) =>
                       new FlowBeforeRecordListModel()
                       {
                           id = a.Id,
                           handleId = a.Id,
                           handleOpinion = a.HandleOpinion,
                           handleStatus = a.HandleStatus,
                           handleTime = a.HandleTime,
                           userName = SqlFunc.MergeString(b.RealName, "/", b.Account),
                           category = c.Id,
                           categoryName = c.FullName,
                           operatorId = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.OperatorId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                           fileList = a.FileList,
                           signImg = a.SignImg,
                           headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", b.HeadIcon),
                       }).ToListAsync();
            case "2":
                return await _repository.AsSugarClient().Queryable<FlowTaskOperatorRecordEntity, UserEntity, UserRelationEntity, RoleEntity>((a, b, c, d) =>
          new JoinQueryInfos(JoinType.Left, a.HandleId == b.Id, JoinType.Left, b.Id == c.UserId, JoinType.Left, c.ObjectId == d.Id))
            .Where((a, b, c) => a.TaskId == taskId && c.ObjectType == "Role").WhereIF(type == "1", (a) => a.HandleStatus == 0 || a.HandleStatus == 1)
            .Select((a, b, c, d) => new FlowBeforeRecordListModel()
            {
                id = a.Id,
                handleId = a.Id,
                handleOpinion = a.HandleOpinion,
                handleStatus = a.HandleStatus,
                handleTime = a.HandleTime,
                userName = SqlFunc.MergeString(b.RealName, "/", b.Account),
                category = d.Id,
                categoryName = d.FullName,
                operatorId = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.OperatorId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                fileList = a.FileList,
                signImg = a.SignImg,
                headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", b.HeadIcon),
            }).ToListAsync();
            case "3":
                return await _repository.AsSugarClient().Queryable<FlowTaskOperatorRecordEntity, UserEntity, UserRelationEntity, PositionEntity>((a, b, c, d) =>
          new JoinQueryInfos(JoinType.Left, a.HandleId == b.Id, JoinType.Left, b.Id == c.UserId, JoinType.Left, c.ObjectId == d.Id))
             .Where((a, b, c) => a.TaskId == taskId && c.ObjectType == "Position").WhereIF(type == "1", (a) => a.HandleStatus == 0 || a.HandleStatus == 1)
             .Select((a, b, c, d) => new FlowBeforeRecordListModel()
             {
                 id = a.Id,
                 handleId = a.Id,
                 handleOpinion = a.HandleOpinion,
                 handleStatus = a.HandleStatus,
                 handleTime = a.HandleTime,
                 userName = SqlFunc.MergeString(b.RealName, "/", b.Account),
                 category = d.Id,
                 categoryName = d.FullName,
                 operatorId = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.OperatorId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                 fileList = a.FileList,
                 signImg = a.SignImg,
                 headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", b.HeadIcon),
             }).ToListAsync();
        }

        return recordList;
    }

    /// <summary>
    /// 事件日志列表.
    /// </summary>
    /// <param name="taskNodeId"></param>
    /// <returns></returns>
    public async Task<dynamic> GetEventLog(string taskNodeId)
    {
        var list = await _repository.AsSugarClient().Queryable<FlowEventLogEntity>()
            .Where(a => a.TaskNodeId == taskNodeId)
            .Select(a => new {
                id = a.Id,
                fullName = a.FullName,
                interfaceName = SqlFunc.Subqueryable<DataInterfaceEntity>().Where(u => u.Id == a.InterfaceId && u.DeleteMark == null).Select(u => u.FullName),
                interfaceCode = SqlFunc.Subqueryable<DataInterfaceEntity>().Where(u => u.Id == a.InterfaceId && u.DeleteMark == null).Select(u => u.EnCode),
                creatorTime = a.CreatorTime,
                result = SqlFunc.ToString(a.Result)
            }).ToListAsync();
        return new { list = list };
    }
    #endregion

    #region 其他模块流程列表

    /// <summary>
    /// 门户列表（待我审批）.
    /// </summary>
    /// <returns></returns>
    public async Task<List<FlowBeforeListOutput>> GetWaitList()
    {
        // 经办审核
        var list1 = _repository.AsSugarClient().Queryable<FlowTaskEntity, FlowTaskOperatorEntity>(
            (a, b) => new JoinQueryInfos(JoinType.Left, a.Id == b.TaskId))
            .Where((a, b) => a.Status == 1 && a.DeleteMark == null && b.Completion == 0 && b.State == 0 && a.Suspend == null
            && b.HandleId == _userManager.UserId && b.CreatorTime < SqlFunc.GetDate())
            .Select((a, b) => new FlowBeforeListOutput()
            {
                enCode = a.EnCode,
                creatorUserId = a.CreatorUserId,
                creatorTime = b.CreatorTime,
                thisStep = b.NodeName,
                thisStepId = b.TaskNodeId,
                flowCategory = a.FlowCategory,
                fullName = a.FullName,
                flowName = a.FlowName,
                status = a.Status,
                id = b.Id,
                userName = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                description = b.Description,
                flowCode = a.FlowCode,
                flowId = a.FlowId,
                processId = a.ProcessId,
                flowUrgent = a.FlowUrgent,
                startTime = a.CreatorTime,
                completion = a.Completion,
                nodeName = b.NodeName,
                delegateUser = "",//适配oracle
                suspend = a.Suspend,
                isBatch = a.IsBatch,
                templateId = a.TemplateId,
                tenantId = a.TenantId,
            });

        // 委托审核
        var list2 = _repository.AsSugarClient().Queryable<FlowTaskEntity, FlowTaskOperatorEntity, FlowDelegateEntity>(
            (a, b, c) => new JoinQueryInfos(JoinType.Left, a.Id == b.TaskId, JoinType.Left, (c.FlowId.Contains(a.TemplateId) || c.FlowName == "全部流程")
        && c.EndTime > DateTime.Now))
            .Where((a, b, c) => a.Status == 1 && a.DeleteMark == null && b.Completion == 0 && b.State == 0 && a.Suspend == null
            && b.HandleId == c.UserId && c.Type == 1 && c.ToUserId == _userManager.UserId && c.DeleteMark == null
            && c.EnabledMark == 1 && c.EndTime > DateTime.Now && c.StartTime < DateTime.Now
            && b.CreatorTime < SqlFunc.GetDate())
            .Select((a, b, c) => new FlowBeforeListOutput()
            {
                enCode = a.EnCode,
                creatorUserId = a.CreatorUserId,
                creatorTime = b.CreatorTime,
                thisStep = b.NodeName,
                thisStepId = b.TaskNodeId,
                flowCategory = a.FlowCategory,
                fullName = a.FullName,
                flowName = a.FlowName,
                status = a.Status,
                id = b.Id,
                userName = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                description = b.Description,
                flowCode = a.FlowCode,
                flowId = a.FlowId,
                processId = a.ProcessId,
                flowUrgent = a.FlowUrgent,
                startTime = a.CreatorTime,
                completion = a.Completion,
                nodeName = b.NodeName,
                delegateUser = c.ToUserId,
                suspend = a.Suspend,
                isBatch = a.IsBatch,
                templateId = a.TemplateId,
                tenantId = a.TenantId,
            });
        return await _repository.AsSugarClient().UnionAll(list1, list2).MergeTable().ToListAsync();
    }

    /// <summary>
    /// 门户列表（待我审批）.
    /// </summary>
    /// <returns></returns>
    public async Task<dynamic> GetPortalWaitList()
    {
        // 经办审核
        var list1 = _repository.AsSugarClient().Queryable<FlowTaskEntity, FlowTaskOperatorEntity>((a, b) =>
            new JoinQueryInfos(JoinType.Left, a.Id == b.TaskId)).Where((a, b) => a.Status == 1 && a.DeleteMark == null && a.Suspend == null
            && b.Completion == 0 && b.State == 0 && b.CreatorTime < SqlFunc.GetDate() && b.HandleId == _userManager.UserId)
            .Select((a, b) => new PortalWaitListModel()
            {
                id = b.Id,
                fullName = a.FullName,
                enCode = a.FlowCode,
                flowId = a.FlowId,
                status = a.Status,
                processId = a.Id,
                taskNodeId = b.TaskNodeId,
                taskOperatorId = b.Id,
                creatorTime = b.CreatorTime,
                type = 2,
                delegateUser = "",//适配oracle
            });

        // 委托审核
        var list2 = _repository.AsSugarClient().Queryable<FlowTaskEntity, FlowTaskOperatorEntity, FlowDelegateEntity, UserEntity>((a, b, c, d) =>
            new JoinQueryInfos(JoinType.Left, a.Id == b.TaskId, JoinType.Left, (c.FlowId.Contains(a.TemplateId) || c.FlowName == "全部流程"),
            JoinType.Left, c.UserId == d.Id))
            .Where((a, b, c) => a.Status == 1 && a.DeleteMark == null && b.Completion == 0 && b.State == 0 && a.Suspend == null
             && b.CreatorTime < SqlFunc.GetDate()
             && b.HandleId == c.UserId && c.Type == 1 && c.ToUserId == _userManager.UserId && c.DeleteMark == null && c.EndTime > DateTime.Now && c.StartTime < DateTime.Now)
            .Select((a, b, c, d) => new PortalWaitListModel()
            {
                id = b.Id,
                fullName = a.FullName,
                enCode = a.EnCode,
                flowId = a.FlowId,
                status = a.Status,
                processId = a.Id,
                taskNodeId = b.TaskNodeId,
                taskOperatorId = b.Id,
                creatorTime = b.CreatorTime,
                type = 2,
                delegateUser = c.ToUserId,
            });
        return await _repository.AsSugarClient().UnionAll(list1, list2).MergeTable().OrderByDescending(x => x.creatorTime).ToListAsync();
    }

    /// <summary>
    /// 列表（我已审批）.
    /// </summary>
    /// <returns></returns>
    public async Task<List<FlowTaskEntity>> GetTrialList()
    {
        return await _repository.AsSugarClient().Queryable<FlowTaskEntity, FlowTaskOperatorRecordEntity, FlowTaskOperatorEntity, UserEntity>(
            (a, b, c, d) => new JoinQueryInfos(JoinType.Left, a.Id == b.TaskId, JoinType.Left, b.TaskOperatorId == c.Id, JoinType.Left, c.HandleId == d.Id))
            .Where((a, b, c) => b.HandleStatus < 2 && b.TaskOperatorId != null && b.HandleId == _userManager.UserId)
            .Select((a, b, c, d) => new FlowTaskEntity()
            {
                Id = b.Id,
                ParentId = a.ParentId,
                ProcessId = a.ProcessId,
                EnCode = a.EnCode,
                FullName = a.FullName,
                FlowUrgent = a.FlowUrgent,
                FlowId = a.FlowId,
                FlowCode = a.FlowCode,
                FlowName = a.FlowName,
                FlowCategory = a.FlowCategory,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                ThisStep = b.NodeName,
                ThisStepId = c.TaskNodeId,
                Status = b.HandleStatus,
                Completion = a.Completion,
                CreatorTime = b.HandleTime,
                CreatorUserId = a.CreatorUserId,
                LastModifyTime = a.LastModifyTime,
                LastModifyUserId = a.LastModifyUserId
            }).ToListAsync();
    }

    /// <summary>
    /// 获取工作交接列表.
    /// </summary>
    /// <param name="userId">离职人.</param>
    /// <param name="type">1-待办事宜 2-负责流程.</param>
    /// <returns></returns>
    public async Task<List<FlowWorkModel>> GetWorkHandover(string userId, int type)
    {
        var workHandoverList = new List<FlowWorkModel>();
        if (type == 1)//待办
        {
            workHandoverList = _repository.AsSugarClient().Queryable<FlowTaskEntity, FlowTaskOperatorEntity>((a, b) =>
            new JoinQueryInfos(JoinType.Left, a.Id == b.TaskId)).Where((a, b) => a.DeleteMark == null && b.HandleId == userId)
            .OrderByDescending(a => a.Id)
            .Select((a, b) => new FlowWorkModel()
            {
                id = a.Id,
                fullName = a.FullName,
                icon = SqlFunc.Subqueryable<FlowTemplateEntity>().Where(u => u.Id == a.TemplateId).Select(u => u.Icon),
            }).Distinct().ToList();
        }
        else if (type == 2)
        {
            workHandoverList = _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity, FlowTemplateEntity>((a, b) =>
            new JoinQueryInfos(JoinType.Left, a.TemplateId == b.Id)).Where((a, b) => a.DeleteMark == null && b.DeleteMark == null && a.FlowTemplateJson.Contains(userId))
            .OrderByDescending(a => a.CreatorTime)
            .Select((a, b) => new FlowWorkModel()
            {
                id = a.Id,
                fullName = SqlFunc.MergeString(b.FullName, "-", a.FullName, "(V", a.Version, ")"),
                icon = b.Icon,
            }).ToList();
        }
        return workHandoverList;
    }

    /// <summary>
    /// 保存工作交接数据.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="ids"></param>
    /// <param name="type"></param>
    public void SaveWorkHandover(string userId, List<string> ids, int type, string handOverUserId = "")
    {
        switch (type)
        {
            case 1:
                _repository.AsSugarClient().Updateable<FlowTaskOperatorEntity>().SetColumns(x => x.HandleId == userId).Where(x => ids.Contains(x.TaskId)).ExecuteCommand();
                _repository.AsSugarClient().Updateable<FlowTaskOperatorUserEntity>().SetColumns(x => x.HandleId == userId).Where(x => ids.Contains(x.TaskId)).ExecuteCommand();
                _repository.AsSugarClient().Updateable<FlowCandidatesEntity>().SetColumns(x => x.Candidates == SqlFunc.Replace(x.Candidates, handOverUserId, userId)).Where(x => ids.Contains(x.TaskId)).ExecuteCommand();
                break;
            default:
                var userName = _userManager.GetUserName(userId);
                var handOverUserName = _userManager.GetUserName(handOverUserId);
                _repository.AsSugarClient().Updateable<FlowTemplateJsonEntity>().SetColumns(x => x.FlowTemplateJson == SqlFunc.Replace(SqlFunc.Replace(x.FlowTemplateJson, handOverUserId, userId), handOverUserName, userName)).Where(x => ids.Contains(x.Id)).ExecuteCommand();
                _repository.AsSugarClient().Updateable<FlowVisibleEntity>().SetColumns(x => x.OperatorId == userId).Where(x => x.OperatorId == handOverUserId && ids.Contains(x.FlowId)).ExecuteCommand();
                var taskIds = _repository.GetList(x => ids.Contains(x.FlowId)).Select(x => x.Id).ToList();
                _repository.AsSugarClient().Updateable<FlowTaskNodeEntity>().SetColumns(x => x.NodePropertyJson == SqlFunc.Replace(SqlFunc.Replace(x.NodePropertyJson, handOverUserId, userId), handOverUserName, userName)).Where(x => taskIds.Contains(x.TaskId)).SetColumns(x => x.NodePropertyJson == SqlFunc.Replace(x.NodePropertyJson, handOverUserName, userName)).ExecuteCommand();
                break;
        }
    }
    #endregion

    #region other

    /// <summary>
    /// 流程信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public FlowJsonModel GetFlowTemplateInfo(string id)
    {
        return _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity, FlowTemplateEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.TemplateId == b.Id))
             .Where((a, b) => a.Id == id && a.DeleteMark == null && b.DeleteMark == null).Select((a, b) => new FlowJsonModel()
             {
                 id = a.Id,
                 templateId = a.TemplateId,
                 visibleType = a.VisibleType,
                 version = a.Version,
                 flowTemplateJson = a.FlowTemplateJson,
                 type = b.Type,
                 enCode = b.EnCode,
                 fullName = a.FullName,
                 category = b.Category,
                 flowName = b.FullName
             }).First();
    }

    /// <summary>
    /// 流程json信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public FlowTemplateJsonEntity GetFlowTemplateJsonInfo(Expression<Func<FlowTemplateJsonEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<FlowTemplateJsonEntity>().First(expression);
    }

    /// <summary>
    /// 表单信息.
    /// </summary>
    /// <param name="formId"></param>
    /// <returns></returns>
    public async Task<FlowFormModel> GetFlowFromModel(string formId)
    {
        return (_repository.AsSugarClient().Queryable<FlowFormEntity>().First(x => x.Id == formId && x.DeleteMark == null)).Adapt<FlowFormModel>();
    }

    /// <summary>
    /// 表单信息.
    /// </summary>
    /// <param name="formId"></param>
    /// <returns></returns>
    public FlowFormEntity GetFlowFromEntity(string formId)
    {
        return _repository.AsSugarClient().Queryable<FlowFormEntity>().First(x => x.Id == formId && x.DeleteMark == null);
    }

    /// <summary>
    /// 获取指定用户被委托人.
    /// </summary>
    /// <param name="userIds">指定用户.</param>
    /// <param name="flowId">流程主表id.</param>
    /// <returns></returns>
    public async Task<List<string>> GetDelegateUserIds(List<string> userIds, string flowId)
    {
        return await _repository.AsSugarClient().Queryable<FlowDelegateEntity>().Where(a => userIds.Contains(a.UserId) && a.Type == 1 && (a.FlowId.Contains(flowId) || a.FlowName == "全部流程") && a.EndTime > DateTime.Now && a.DeleteMark == null).Select(a => a.ToUserId).ToListAsync();
    }

    /// <summary>
    /// 获取指定用户被委托人.
    /// </summary>
    /// <param name="userId">指定用户.</param>
    /// <param name="flowId">流程id.</param>
    /// <returns></returns>
    public List<string> GetToUserId(string userId, string flowId)
    {
        return _repository.AsSugarClient().Queryable<FlowDelegateEntity>().Where(a => a.UserId == userId && a.Type == 1 && (a.FlowId.Contains(flowId) || a.FlowName == "全部流程") && a.EndTime > DateTime.Now && a.DeleteMark == null).Select(a => a.ToUserId).ToList();
    }

    /// <summary>
    /// 获取功能开发.
    /// </summary>
    /// <param name="flowId">流程id.</param>
    /// <returns></returns>
    public async Task<VisualDevEntity> GetVisualDevInfo(string flowId)
    {
        return await _repository.AsSugarClient().Queryable<VisualDevEntity>().FirstAsync(a => a.Id == flowId && a.DeleteMark == null);
    }

    /// <summary>
    /// 获取数据连接.
    /// </summary>
    /// <param name="id">id.</param>
    /// <returns></returns>
    public async Task<DbLinkEntity> GetLinkInfo(string id)
    {
        return await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(a => a.Id == id && a.DeleteMark == null);
    }

    /// <summary>
    /// 获取任务发起人信息.
    /// </summary>
    /// <param name="id">id.</param>
    /// <returns></returns>
    public FlowUserEntity GetFlowUserEntity(string id)
    {
        return _repository.AsSugarClient().Queryable<FlowUserEntity>().First(a => a.TaskId == id);
    }

    /// <summary>
    /// 新增任务发起人信息.
    /// </summary>
    /// <param name="userId">用户id.</param>
    /// <param name="taskId">任务id.</param>
    public void CreateFlowUser(string userId, string taskId)
    {
        var flowUserEntity = _repository.AsSugarClient().Queryable<UserEntity>().First(a => a.Id == userId && a.DeleteMark == null && a.EnabledMark == 1).Adapt<FlowUserEntity>();
        flowUserEntity.Id = SnowflakeIdHelper.NextId();
        flowUserEntity.TaskId = taskId;
        flowUserEntity.Subordinate = _repository.AsSugarClient().Queryable<UserEntity>()
                .Where(u => u.EnabledMark == 1 && u.DeleteMark == null && u.ManagerId == userId)
                .Select(u => u.Id).ToList().ToJsonString();
        _repository.AsSugarClient().Insertable(flowUserEntity).ExecuteCommand();
    }

    /// <summary>
    /// 获取当前用户关系id.
    /// </summary>
    /// <returns></returns>
    public List<string> GetCurrentUserObjId()
    {
        var rIdList = _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.UserId.Equals(_userManager.UserId)).Select(x => new { x.ObjectId, x.ObjectType }).ToList();
        var objIdList = new List<string>() { _userManager.UserId + "--user" };
        rIdList.ForEach(x =>
        {
            if (x.ObjectType.Equals("Organize"))
            {
                objIdList.Add(x.ObjectId + "--company");
                objIdList.Add(x.ObjectId + "--department");
            }
            else
            {
                objIdList.Add(x.ObjectId + "--" + x.ObjectType.ToLower());
            }
        });
        return objIdList;
    }

    /// <summary>
    /// 是否为功能流程.
    /// </summary>
    /// <param name="flowId"></param>
    /// <returns></returns>
    public bool IsDevFlow(string flowId)
    {
        return _repository.AsSugarClient().Queryable<FlowFormEntity>().Any(x => x.FlowId == flowId && x.FlowType == 1 && x.DeleteMark == null);
    }

    /// <summary>
    /// 当前用户分级管理员权限.
    /// </summary>
    /// <param name="creatorUserId"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public bool GetOrgAdminAuthorize(string creatorUserId, int type)
    {
        if (!_userManager.IsAdministrator)
        {
            var objList = new List<string>();
            var orgIds = _userManager.DataScope.Where(x => x.Edit).Select(x => x.organizeId).ToList(); // 分管组织id
            if (type == 1)
            {
                orgIds = _userManager.DataScope.Where(x => x.Delete).Select(x => x.organizeId).ToList();
            }

            objList = _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => orgIds.Contains(x.ObjectId)).Select(x => x.UserId).ToList();
            return objList.Contains(creatorUserId);
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// 获取组织树.
    /// </summary>
    /// <param name="orgId"></param>
    /// <returns></returns>
    public List<string> GetOrgTree(string orgId)
    {
        return _repository.AsSugarClient().Queryable<OrganizeEntity>().First(x => x.Id == orgId && x.DeleteMark == null).OrganizeIdTree.Split(",").ToList();
    }
    #endregion

    #region FlowTask

    /// <summary>
    /// 任务列表.
    /// </summary>
    /// <returns></returns>
    public async Task<List<FlowTaskEntity>> GetTaskList()
    {
        return _repository.GetList();
    }

    /// <summary>
    /// 任务列表.
    /// </summary>
    /// <param name="flowId">引擎id.</param>
    /// <returns></returns>
    public async Task<List<FlowTaskEntity>> GetTaskList(string flowId)
    {
        return _repository.GetList(x => x.DeleteMark == null && x.FlowId == flowId);
    }

    /// <summary>
    /// 任务列表.
    /// </summary>
    /// <param name="expression">条件.</param>
    /// <returns></returns>
    public async Task<List<FlowTaskEntity>> GetTaskList(Expression<Func<FlowTaskEntity, bool>> expression)
    {
        return _repository.GetList(expression);
    }

    /// <summary>
    /// 任务信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<FlowTaskEntity> GetTaskInfo(string id)
    {
        return await _repository.GetFirstAsync(x => x.DeleteMark == null && x.Id == id);
    }

    /// <summary>
    /// 任务信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public FlowTaskEntity GetTaskFirstOrDefault(string id)
    {
        return _repository.GetFirst(x => x.DeleteMark == null && x.Id == id);
    }

    /// <summary>
    /// 是否存在任务.
    /// </summary>
    /// <param name="expression">条件.</param>
    /// <returns></returns>
    public async Task<bool> AnyFlowTask(Expression<Func<FlowTaskEntity, bool>> expression)
    {
        return _repository.IsAny(expression);
    }

    /// <summary>
    /// 任务删除.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public async Task<int> DeleteTask(FlowTaskEntity entity)
    {
        entity.DeleteTime = DateTime.Now;
        entity.DeleteMark = 1;
        entity.DeleteUserId = _userManager.UserId;
        await _repository.AsSugarClient().Deleteable<FlowTaskNodeEntity>(x => entity.Id == x.TaskId).ExecuteCommandAsync();
        await _repository.AsSugarClient().Deleteable<FlowTaskOperatorEntity>(x => entity.Id == x.TaskId).ExecuteCommandAsync();
        await _repository.AsSugarClient().Deleteable<FlowTaskOperatorRecordEntity>(x => entity.Id == x.TaskId).ExecuteCommandAsync();
        await _repository.AsSugarClient().Deleteable<FlowTaskCirculateEntity>(x => entity.Id == x.TaskId).ExecuteCommandAsync();
        await _repository.AsSugarClient().Deleteable<FlowCandidatesEntity>(x => entity.Id == x.TaskId).ExecuteCommandAsync();
        await _repository.AsSugarClient().Deleteable<FlowCommentEntity>(x => entity.Id == x.TaskId).ExecuteCommandAsync();
        return _repository.AsSugarClient().Updateable(entity).UpdateColumns(it => new { it.DeleteTime, it.DeleteMark, it.DeleteUserId }).ExecuteCommand();
    }

    /// <summary>
    /// 任务删除, 非异步.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public int DeleteTaskNoAwait(FlowTaskEntity entity, bool isDel = true)
    {
        entity.DeleteTime = DateTime.Now;
        entity.DeleteMark = 1;
        entity.DeleteUserId = _userManager.UserId;
        _repository.AsSugarClient().Deleteable<FlowTaskNodeEntity>(x => entity.Id == x.TaskId).ExecuteCommand();
        _repository.AsSugarClient().Deleteable<FlowTaskOperatorEntity>(x => entity.Id == x.TaskId).ExecuteCommand();
        _repository.AsSugarClient().Deleteable<FlowTaskOperatorRecordEntity>(x => entity.Id == x.TaskId).ExecuteCommand();
        _repository.AsSugarClient().Deleteable<FlowTaskCirculateEntity>(x => entity.Id == x.TaskId).ExecuteCommand();
        _repository.AsSugarClient().Deleteable<FlowCandidatesEntity>(x => entity.Id == x.TaskId).ExecuteCommand();
        _repository.AsSugarClient().Deleteable<FlowCommentEntity>(x => entity.Id == x.TaskId).ExecuteCommand();
        if (isDel)
        {
            return _repository.AsSugarClient().Updateable(entity).UpdateColumns(it => new { it.DeleteTime, it.DeleteMark, it.DeleteUserId }).ExecuteCommand();
        }
        else
        {
            return 1;
        }
    }

    /// <summary>
    /// 任务创建.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public async Task<FlowTaskEntity> CreateTask(FlowTaskEntity entity)
    {
        return _repository.AsSugarClient().GetSimpleClient<FlowTaskEntity>().AsInsertable(entity).CallEntityMethod(m => m.Create()).ExecuteReturnEntity();
    }

    /// <summary>
    /// 任务更新.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public async Task<bool> UpdateTask(FlowTaskEntity entity)
    {
        return _repository.AsSugarClient().GetSimpleClient<FlowTaskEntity>().AsUpdateable(entity).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChange();
    }

    /// <summary>
    /// 任务更新.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public async Task<bool> UpdateTask(FlowTaskEntity entity, Expression<Func<FlowTaskEntity, object>> Expression = null)
    {
        return _repository.AsSugarClient().Updateable(entity).UpdateColumns(Expression).ExecuteCommandHasChange();
    }

    /// <summary>
    /// 打回流程删除所有相关数据.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="isClearRecord">是否清除记录.</param>
    /// <returns></returns>
    public async Task DeleteFlowTaskAllData(string taskId, bool isClearRecord = true, bool isClearCandidates = true)
    {
        await _repository.AsSugarClient().Updateable<FlowTaskNodeEntity>().SetColumns(x => x.State == -2).Where(x => x.TaskId == taskId).ExecuteCommandAsync();
        await _repository.AsSugarClient().Updateable<FlowTaskOperatorEntity>().SetColumns(x => x.State == -1).Where(x => x.TaskId == taskId).ExecuteCommandAsync();
        if (isClearRecord)
            await _repository.AsSugarClient().Updateable<FlowTaskOperatorRecordEntity>().SetColumns(x => x.Status == -1).Where(x => x.TaskId == taskId).ExecuteCommandAsync();
        if (isClearCandidates)
            await _repository.AsSugarClient().Deleteable<FlowCandidatesEntity>(x => x.TaskId == taskId).ExecuteCommandAsync();
    }

    /// <summary>
    /// 打回流程删除所有相关数据.
    /// </summary>
    /// <param name="taskIds">任务di数组.</param>
    /// <param name="isClearRecord">是否清除记录.</param>
    /// <returns></returns>
    public async Task DeleteFlowTaskAllData(List<string> taskIds, bool isClearRecord = true)
    {
        await _repository.AsSugarClient().Updateable<FlowTaskNodeEntity>().SetColumns(x => x.State == -2).Where(x => taskIds.Contains(x.TaskId)).ExecuteCommandAsync();
        await _repository.AsSugarClient().Updateable<FlowTaskOperatorEntity>().SetColumns(x => x.State == -1).Where(x => taskIds.Contains(x.TaskId)).ExecuteCommandAsync();
        if (isClearRecord)
            await _repository.AsSugarClient().Updateable<FlowTaskOperatorRecordEntity>().SetColumns(x => x.Status == -1).Where(x => taskIds.Contains(x.TaskId)).ExecuteCommandAsync();
        await _repository.AsSugarClient().Deleteable<FlowCandidatesEntity>(x => taskIds.Contains(x.TaskId)).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除子流程.
    /// </summary>
    /// <param name="flowTaskEntity"></param>
    /// <returns></returns>
    public async Task DeleteSubTask(FlowTaskEntity flowTaskEntity)
    {
        var entityList = await GetTaskList(x => x.ParentId == flowTaskEntity.Id);
        if (entityList.Any())
        {
            foreach (var item in entityList)
            {
                await DeleteTask(item);
                await DeleteSubTask(item);
            }
        }
    }
    #endregion

    #region FlowTaskNode

    /// <summary>
    /// 节点列表.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    public async Task<List<FlowTaskNodeEntity>> GetTaskNodeList(string taskId)
    {
        return await _repository.AsSugarClient().Queryable<FlowTaskNodeEntity>().Where(x => x.TaskId == taskId).OrderBy(x => x.SortCode).ToListAsync();
    }

    /// <summary>
    /// 节点列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="orderByExpression"></param>
    /// <param name="orderByType"></param>
    /// <returns></returns>
    public async Task<List<FlowTaskNodeEntity>> GetTaskNodeList(Expression<Func<FlowTaskNodeEntity, bool>> expression, Expression<Func<FlowTaskNodeEntity, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc)
    {
        return await _repository.AsSugarClient().Queryable<FlowTaskNodeEntity>().Where(expression).OrderByIF(orderByExpression.IsNotEmptyOrNull(), orderByExpression, orderByType).ToListAsync();
    }

    /// <summary>
    /// 节点信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<FlowTaskNodeEntity> GetTaskNodeInfo(string id)
    {
        return await _repository.AsSugarClient().Queryable<FlowTaskNodeEntity>().FirstAsync(x => x.Id == id);
    }

    /// <summary>
    /// 节点信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool AnyTaskNode(Expression<Func<FlowTaskNodeEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<FlowTaskNodeEntity>().Any(expression);
    }

    /// <summary>
    /// 节点信息.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public async Task<FlowTaskNodeEntity> GetTaskNodeInfo(Expression<Func<FlowTaskNodeEntity, bool>> expression)
    {
        return await _repository.AsSugarClient().Queryable<FlowTaskNodeEntity>().FirstAsync(expression);
    }

    /// <summary>
    /// 节点创建.
    /// </summary>
    /// <param name="entitys"></param>
    /// <returns></returns>
    public async Task<bool> CreateTaskNode(List<FlowTaskNodeEntity> entitys)
    {
        return await _repository.AsSugarClient().GetSimpleClient<FlowTaskNodeEntity>().InsertRangeAsync(entitys);
    }

    /// <summary>
    /// 节点更新.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public async Task<bool> UpdateTaskNode(FlowTaskNodeEntity entity)
    {
        return await _repository.AsSugarClient().GetSimpleClient<FlowTaskNodeEntity>().UpdateAsync(entity);
    }

    /// <summary>
    /// 节点更新.
    /// </summary>
    /// <param name="entitys"></param>
    /// <returns></returns>
    public async Task<bool> UpdateTaskNode(List<FlowTaskNodeEntity> entitys)
    {
        return await _repository.AsSugarClient().GetSimpleClient<FlowTaskNodeEntity>().UpdateRangeAsync(entitys);
    }
    #endregion

    #region FlowTaskOperator

    /// <summary>
    /// 经办列表.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    public async Task<List<FlowTaskOperatorEntity>> GetTaskOperatorList(string taskId)
    {
        return await _repository.AsSugarClient().Queryable<FlowTaskOperatorEntity>().Where(x => x.TaskId == taskId).OrderBy(x => x.CreatorTime).ToListAsync();
    }

    /// <summary>
    /// 经办列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="orderByExpression"></param>
    /// <param name="orderByType"></param>
    /// <returns></returns>
    public async Task<List<FlowTaskOperatorEntity>> GetTaskOperatorList(Expression<Func<FlowTaskOperatorEntity, bool>> expression, Expression<Func<FlowTaskOperatorEntity, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc)
    {
        return await _repository.AsSugarClient().Queryable<FlowTaskOperatorEntity>().Where(expression).OrderByIF(orderByExpression.IsNotEmptyOrNull(), orderByExpression, orderByType).ToListAsync();
    }

    /// <summary>
    /// 依次审批经办列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="orderByExpression"></param>
    /// <param name="orderByType"></param>
    /// <returns></returns>
    public async Task<List<FlowTaskOperatorUserEntity>> GetTaskOperatorUserList(Expression<Func<FlowTaskOperatorUserEntity, bool>> expression, Expression<Func<FlowTaskOperatorUserEntity, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc)
    {
        return await _repository.AsSugarClient().Queryable<FlowTaskOperatorUserEntity>().Where(expression).OrderByIF(orderByExpression.IsNotEmptyOrNull(), orderByExpression, orderByType).ToListAsync();
    }

    /// <summary>
    /// 经办信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<FlowTaskOperatorEntity> GetTaskOperatorInfo(string id)
    {
        return await _repository.AsSugarClient().Queryable<FlowTaskOperatorEntity>().FirstAsync(x => x.Id == id);
    }

    /// <summary>
    /// 经办信息.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public async Task<FlowTaskOperatorEntity> GetTaskOperatorInfo(Expression<Func<FlowTaskOperatorEntity, bool>> expression)
    {
        return await _repository.AsSugarClient().Queryable<FlowTaskOperatorEntity>().FirstAsync(expression);
    }

    /// <summary>
    /// 经办删除.
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    public async Task<int> DeleteTaskOperator(List<string> ids)
    {
        return await _repository.AsSugarClient().Updateable<FlowTaskOperatorEntity>().SetColumns(x => x.State == -1).Where(x => ids.Contains(x.Id)).ExecuteCommandAsync();
    }

    /// <summary>
    /// 依次经办删除.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    public async Task<int> DeleteTaskOperatorUser(Expression<Func<FlowTaskOperatorUserEntity, bool>> expression)
    {
        return await _repository.AsSugarClient().Updateable<FlowTaskOperatorUserEntity>().SetColumns(x => x.State == -1).Where(expression).ExecuteCommandAsync();
    }

    /// <summary>
    /// 经办创建.
    /// </summary>
    /// <param name="entitys"></param>
    /// <returns></returns>
    public async Task<bool> CreateTaskOperator(List<FlowTaskOperatorEntity> entitys)
    {
        return await _repository.AsSugarClient().Storageable(entitys).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 依次经办创建.
    /// </summary>
    /// <param name="entitys"></param>
    /// <returns></returns>
    public async Task<bool> CreateTaskOperatorUser(List<FlowTaskOperatorUserEntity> entitys)
    {
        return await _repository.AsSugarClient().Storageable(entitys).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 经办创建.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public async Task<bool> CreateTaskOperator(FlowTaskOperatorEntity entity)
    {
        return await _repository.AsSugarClient().GetSimpleClient<FlowTaskOperatorEntity>().InsertAsync(entity);
    }

    /// <summary>
    /// 经办更新.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public async Task<bool> UpdateTaskOperator(FlowTaskOperatorEntity entity)
    {
        return await _repository.AsSugarClient().GetSimpleClient<FlowTaskOperatorEntity>().UpdateAsync(entity);
    }

    /// <summary>
    /// 经办更新.
    /// </summary>
    /// <param name="entitys"></param>
    /// <returns></returns>
    public async Task<bool> UpdateTaskOperator(List<FlowTaskOperatorEntity> entitys)
    {
        return await _repository.AsSugarClient().GetSimpleClient<FlowTaskOperatorEntity>().UpdateRangeAsync(entitys);
    }

    /// <summary>
    /// 是否存在依次审批经办.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public bool AnyTaskOperatorUser(Expression<Func<FlowTaskOperatorUserEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<FlowTaskOperatorUserEntity>().Any(expression);
    }

    /// <summary>
    /// 经办更新.
    /// </summary>
    /// <param name="entitys"></param>
    /// <returns></returns>
    public async Task<bool> UpdateTaskOperatorUser(List<FlowTaskOperatorUserEntity> entitys)
    {
        return await _repository.AsSugarClient().GetSimpleClient<FlowTaskOperatorUserEntity>().UpdateRangeAsync(entitys);
    }
    #endregion

    #region FlowTaskOperatorRecord

    /// <summary>
    /// 经办记录列表.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    public async Task<List<FlowTaskOperatorRecordEntity>> GetTaskOperatorRecordList(string taskId)
    {
        return await _repository.AsSugarClient().Queryable<FlowTaskOperatorRecordEntity>().Where(x => x.TaskId == taskId).OrderBy(o => o.HandleTime).ToListAsync();
    }

    /// <summary>
    /// 经办记录列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="orderByExpression"></param>
    /// <param name="orderByType"></param>
    /// <returns></returns>
    public async Task<List<FlowTaskOperatorRecordEntity>> GetTaskOperatorRecordList(Expression<Func<FlowTaskOperatorRecordEntity, bool>> expression, Expression<Func<FlowTaskOperatorRecordEntity, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc)
    {
        return await _repository.AsSugarClient().Queryable<FlowTaskOperatorRecordEntity>().Where(expression).OrderByIF(orderByExpression.IsNotEmptyOrNull(), orderByExpression, orderByType).ToListAsync();
    }

    /// <summary>
    /// 经办记录列表.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    public async Task<List<FlowTaskOperatorRecordModel>> GetTaskOperatorRecordModelList(string taskId)
    {
        return await _repository.AsSugarClient().Queryable<FlowTaskOperatorRecordEntity, FlowTaskOperatorEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.TaskOperatorId == b.Id)).Where(a => a.TaskId == taskId).OrderBy(a => a.HandleTime).Select((a, b) => new FlowTaskOperatorRecordModel
        {
            id = a.Id,
            nodeCode = a.NodeCode,
            nodeName = a.NodeName,
            handleStatus = a.HandleStatus,
            handleId = a.HandleId,
            handleOpinion = a.HandleOpinion,
            handleTime = a.HandleTime,
            taskId = a.TaskId,
            taskNodeId = a.TaskNodeId,
            taskOperatorId = a.TaskOperatorId,
            signImg = a.SignImg,
            status = a.Status,
            userName = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.HandleId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
            operatorId = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id == a.OperatorId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
            creatorTime = b.CreatorTime == null ? a.HandleTime : b.CreatorTime,
            fileList = a.FileList,
            isLog = SqlFunc.Subqueryable<FlowEventLogEntity>().Where(u => u.TaskNodeId == a.TaskNodeId).Any()
        }).ToListAsync();
    }

    /// <summary>
    /// 经办记录信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<FlowTaskOperatorRecordEntity> GetTaskOperatorRecordInfo(string id)
    {
        return await _repository.AsSugarClient().Queryable<FlowTaskOperatorRecordEntity>().FirstAsync(x => x.Id == id);
    }

    /// <summary>
    /// 经办记录信息.
    /// </summary>
    /// <param name="expression">条件.</param>
    /// <returns></returns>
    public async Task<FlowTaskOperatorRecordEntity> GetTaskOperatorRecordInfo(Expression<Func<FlowTaskOperatorRecordEntity, bool>> expression)
    {
        return await _repository.AsSugarClient().Queryable<FlowTaskOperatorRecordEntity>().FirstAsync(expression);
    }

    /// <summary>
    /// 经办记录创建.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public async Task<bool> CreateTaskOperatorRecord(FlowTaskOperatorRecordEntity entity)
    {
        entity.Id = SnowflakeIdHelper.NextId();
        return await _repository.AsSugarClient().GetSimpleClient<FlowTaskOperatorRecordEntity>().InsertAsync(entity);
    }

    /// <summary>
    /// 经办记录作废.
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    public async Task DeleteTaskOperatorRecord(List<string> ids)
    {
        await _repository.AsSugarClient().Updateable<FlowTaskOperatorRecordEntity>().SetColumns(it => it.Status == -1).Where(x => ids.Contains(x.Id)).ExecuteCommandAsync();
    }

    /// <summary>
    /// 经办记录作废.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public async Task DeleteTaskOperatorRecord(Expression<Func<FlowTaskOperatorRecordEntity, bool>> expression)
    {
        await _repository.AsSugarClient().Updateable<FlowTaskOperatorRecordEntity>().SetColumns(it => it.Status == -1).Where(expression).ExecuteCommandAsync();
    }
    #endregion

    #region FlowTaskCirculate

    /// <summary>
    /// 传阅创建.
    /// </summary>
    /// <param name="entitys"></param>
    /// <returns></returns>
    public async Task<bool> CreateTaskCirculate(List<FlowTaskCirculateEntity> entitys)
    {
        return await _repository.AsSugarClient().GetSimpleClient<FlowTaskCirculateEntity>().InsertRangeAsync(entitys);
    }
    #endregion

    #region FlowTaskCandidates

    /// <summary>
    /// 候选人创建.
    /// </summary>
    /// <param name="entitys"></param>
    public void CreateFlowCandidates(List<FlowCandidatesEntity> entitys)
    {
        _repository.AsSugarClient().GetSimpleClient<FlowCandidatesEntity>().InsertRange(entitys);
    }

    /// <summary>
    /// 候选人删除.
    /// </summary>
    /// <param name="expression"></param>
    public void DeleteFlowCandidates(Expression<Func<FlowCandidatesEntity, bool>> expression)
    {
        _repository.AsSugarClient().Deleteable(expression).ExecuteCommand();
    }

    /// <summary>
    /// 候选人获取.
    /// </summary>
    /// <param name="nodeId"></param>
    public List<string> GetFlowCandidates(string nodeId)
    {
        var flowCandidates = new List<string>();
        var candidateUserIdList = _repository.AsSugarClient().GetSimpleClient<FlowCandidatesEntity>().GetList(x => x.TaskNodeId == nodeId).Select(x => x.Candidates).ToList();
        foreach (var item in candidateUserIdList)
        {
            flowCandidates = flowCandidates.Union(item.Split(",").ToList()).Distinct().ToList();
        }

        return flowCandidates;
    }

    /// <summary>
    /// 系统表单.
    /// </summary>
    /// <param name="enCode"></param>
    /// <param name="data"></param>
    /// <param name="id"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public async Task GetSysTableFromService(string enCode, object data, string id, int type)
    {
        switch (enCode.ToLower())
        {
            case "leaveapply":
                var leaveapplyentity = data.ToObject<LeaveApplyInput>().Adapt<LeaveApplyEntity>();
                if (type == 0)
                {
                    if (string.IsNullOrEmpty(id))
                    {
                        leaveapplyentity.Id = SnowflakeIdHelper.NextId();
                        await _repository.AsSugarClient().Insertable(leaveapplyentity).ExecuteCommandAsync();
                    }
                    else
                    {
                        leaveapplyentity.Id = id;
                        await _repository.AsSugarClient().Updateable(leaveapplyentity).ExecuteCommandAsync();
                    }
                }
                else
                {
                    leaveapplyentity.Id = id;
                    await _repository.AsSugarClient().Insertable(leaveapplyentity).ExecuteCommandAsync();
                }
                break;
            case "salesorder":
                var input = data.ToObject<SalesOrderInput>();
                var salesorderentity = input.Adapt<SalesOrderEntity>();
                var entityList = input.entryList.Adapt<List<SalesOrderEntryEntity>>();
                if (type == 0)
                {
                    if (string.IsNullOrEmpty(id))
                    {
                        salesorderentity.Id = SnowflakeIdHelper.NextId();
                        foreach (var item in entityList)
                        {
                            item.Id = SnowflakeIdHelper.NextId();
                            item.SalesOrderId = salesorderentity.Id;
                            item.SortCode = entityList.IndexOf(item);
                        }
                        await _repository.AsSugarClient().Insertable(entityList).ExecuteCommandAsync();
                        await _repository.AsSugarClient().Insertable(salesorderentity).ExecuteCommandAsync();
                    }
                    else
                    {
                        salesorderentity.Id = id;
                        foreach (var item in entityList)
                        {
                            item.Id = SnowflakeIdHelper.NextId();
                            item.SalesOrderId = salesorderentity.Id;
                            item.SortCode = entityList.IndexOf(item);
                        }

                        await _repository.AsSugarClient().Deleteable<SalesOrderEntryEntity>(x => x.SalesOrderId == id).ExecuteCommandAsync();
                        await _repository.AsSugarClient().Insertable(entityList).ExecuteCommandAsync();
                        await _repository.AsSugarClient().Updateable(salesorderentity).ExecuteCommandAsync();
                    }
                }
                else
                {
                    salesorderentity.Id = id;
                    foreach (var item in entityList)
                    {
                        item.Id = SnowflakeIdHelper.NextId();
                        item.SalesOrderId = salesorderentity.Id;
                        item.SortCode = entityList.IndexOf(item);
                    }
                    await _repository.AsSugarClient().Insertable(entityList).ExecuteCommandAsync();
                    await _repository.AsSugarClient().Insertable(salesorderentity).ExecuteCommandAsync();
                }
                break;
        }
    }
    #endregion

    #region FlowTaskParamter
    /// <summary>
    /// 根据任务id获取任务引擎参数.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="flowHandleModel"></param>
    /// <returns></returns>
    public async Task<FlowTaskParamter> GetTaskParamterByTaskId(string taskId, FlowHandleModel flowHandleModel)
    {
        var entity = GetTaskFirstOrDefault(taskId);
        if (entity == null) return null;
        var flowTaskParamter = flowHandleModel == null ? new FlowTaskParamter() : flowHandleModel.ToObject<FlowTaskParamter>();
        flowTaskParamter.flowTaskEntity = entity;
        if (AnyTaskNode(x => x.State == 0 && x.TaskId == flowTaskParamter.flowTaskEntity.Id))
        {
            flowTaskParamter.flowTaskNodeEntityList = await GetTaskNodeList(x => x.State == 0 && x.TaskId == flowTaskParamter.flowTaskEntity.Id, x => x.SortCode, OrderByType.Asc);
            var startTaskNodeEntity = flowTaskParamter.flowTaskNodeEntityList.Find(x => FlowTaskNodeTypeEnum.start.ParseToString().Equals(x.NodeType));
            flowTaskParamter.startProperties = startTaskNodeEntity.NodePropertyJson.ToObject<StartProperties>();
            flowTaskParamter.startProperties.formOperates = await GetFlowFormAuthorize(flowTaskParamter.flowTaskEntity.Id, startTaskNodeEntity.NodeCode);
        }
        else
        {
            var flowTemplateJson = GetFlowTemplateJsonInfo(x => x.Id == flowTaskParamter.flowTaskEntity.FlowId);
            flowTaskParamter.startProperties = flowTemplateJson.FlowTemplateJson.ToObject<FlowTemplateJsonModel>().properties.ToObject<StartProperties>();
        }
        return flowTaskParamter;
    }

    /// <summary>
    /// 根据节点id获取任务引擎参数.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="flowHandleModel"></param>
    /// <returns></returns>
    public async Task<FlowTaskParamter> GetTaskParamterByNodeId(string nodeId, FlowHandleModel flowHandleModel)
    {
        var entity = await GetTaskNodeInfo(nodeId);
        if (entity == null) return null;
        var flowTaskParamter = await GetTaskParamterByTaskId(entity.TaskId, flowHandleModel);
        flowTaskParamter.flowTaskNodeEntity = entity;
        flowTaskParamter.approversProperties = entity.NodePropertyJson.ToObject<ApproversProperties>();
        flowTaskParamter.thisFlowTaskOperatorEntityList = await GetTaskOperatorList(x => x.TaskId == entity.TaskId && x.TaskNodeId == entity.Id && x.State == 0);
        return flowTaskParamter;
    }

    /// <summary>
    /// 根据经办id获取任务引擎参数.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="flowHandleModel"></param>
    /// <returns></returns>
    public async Task<FlowTaskParamter> GetTaskParamterByOperatorId(string operatorId, FlowHandleModel flowHandleModel)
    {
        var entity = await GetTaskOperatorInfo(operatorId);
        if (entity == null) return null;
        var flowTaskParamter = await GetTaskParamterByNodeId(entity.TaskNodeId, flowHandleModel);
        flowTaskParamter.flowTaskOperatorEntity = entity;
        return flowTaskParamter;
    }
    #endregion

    #region FlowRejectData
    /// <summary>
    /// 驳回数据信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<FlowRejectDataEntity> GetRejectDataInfo(string id)
    {
        return await _repository.AsSugarClient().Queryable<FlowRejectDataEntity>().FirstAsync(x => x.Id == id);
    }

    /// <summary>
    /// 驳回数据创建.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="taskNodeId"></param>
    /// <returns></returns>
    public async Task<string> CreateRejectData(string taskId, string taskNodeId)
    {
        var entity = new FlowRejectDataEntity();
        entity.Id = SnowflakeIdHelper.NextId();
        #region 解决mysql字段长度太小
        var taskEntity = GetTaskFirstOrDefault(taskId);
        taskEntity.FlowFormContentJson = null;
        #endregion
        entity.TaskJson = taskEntity.ToJsonString();
        entity.TaskNodeJson = (await GetTaskNodeList(taskId)).ToJsonString();
        //entity.TaskOperatorJson = (await GetTaskOperatorList(x => x.TaskId == taskId && x.State != "-1" && SqlFunc.IsNullOrEmpty(x.ParentId) && x.TaskNodeId == taskNodeId)).ToJsonString();
        // 分流合流节点未审待办
        entity.TaskOperatorJson = (await GetTaskOperatorList(x => x.TaskId == taskId && x.State != -1 && SqlFunc.IsNullOrEmpty(x.ParentId) && taskNodeId.Contains(x.NodeCode))).ToJsonString();
        await _repository.AsSugarClient().GetSimpleClient<FlowRejectDataEntity>().InsertAsync(entity);
        return entity.Id;
    }

    /// <summary>
    /// 驳回数据重启.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public async Task UpdateRejectData(FlowRejectDataEntity entity)
    {
        var taskEntity = entity.TaskJson.ToObject<FlowTaskEntity>();
        #region 解决mysql字段长度太小
        var thisTaskEntity = GetTaskFirstOrDefault(taskEntity.Id);
        taskEntity.FlowFormContentJson = thisTaskEntity.FlowFormContentJson;
        #endregion
        var taskNodeEntityList = entity.TaskNodeJson.ToObject<List<FlowTaskNodeEntity>>();
        var taskOperatorEntityList = entity.TaskOperatorJson.ToObject<List<FlowTaskOperatorEntity>>();
        foreach (var item in taskOperatorEntityList)
        {
            item.Id = SnowflakeIdHelper.NextId();
            item.HandleStatus = null;
            item.HandleTime = null;
            item.Completion = 0;
            item.State = 0;
        }
        await UpdateTask(taskEntity);
        await UpdateTaskNode(taskNodeEntityList);
        await _repository.AsSugarClient().GetSimpleClient<FlowTaskOperatorEntity>().InsertRangeAsync(taskOperatorEntityList);
    }
    #endregion

    #region FlowEventLog
    /// <summary>
    /// 插入事件日志.
    /// </summary>
    /// <param name="flowEventLogEntity"></param>
    /// <returns></returns>
    public async Task CreateEventLog(FlowEventLogEntity flowEventLogEntity)
    {
        flowEventLogEntity.Id = SnowflakeIdHelper.NextId();
        await _repository.AsSugarClient().GetSimpleClient<FlowEventLogEntity>().InsertAsync(flowEventLogEntity);
    }
    #endregion

    #region FlowFormAuthorize

    public async Task CreateFlowFormAuthorize(List<FlowFormAuthorizeEntity> flowFormAuthorizeEntitys)
    {
        if (flowFormAuthorizeEntitys.Any())
        {
            var taskId = flowFormAuthorizeEntitys.FirstOrDefault().TaskId;
            var nodeCodeList = flowFormAuthorizeEntitys.Select(x => x.NodeCode).ToList();
            await _repository.AsSugarClient().Deleteable<FlowFormAuthorizeEntity>(x => x.TaskId == taskId && nodeCodeList.Contains(x.NodeCode)).ExecuteCommandAsync();
        }
        await _repository.AsSugarClient().GetSimpleClient<FlowFormAuthorizeEntity>().InsertRangeAsync(flowFormAuthorizeEntitys);
    }

    public async Task<dynamic> GetFlowFormAuthorize(string taskId, string nodeCode)
    {
        var flowFormAuthorizeEntity = await _repository.AsSugarClient().Queryable<FlowFormAuthorizeEntity>().Where(x => x.TaskId == taskId && x.NodeCode == nodeCode && x.DeleteMark == null).FirstAsync();
        if (flowFormAuthorizeEntity.IsNullOrEmpty() && flowFormAuthorizeEntity.FormOperate.IsNullOrEmpty()) return new List<object>();
        return flowFormAuthorizeEntity.FormOperate.ToObject<List<object>>();
    }

    public async Task<dynamic> GetFlowFormAuthorize(FlowTaskNodeEntity flowTaskNodeEntity)
    {
        var flowFormAuthorizeEntity = await _repository.AsSugarClient().Queryable<FlowFormAuthorizeEntity>().Where(x => x.TaskId == flowTaskNodeEntity.TaskId && x.NodeCode == flowTaskNodeEntity.NodeCode && x.DeleteMark == null).FirstAsync();
        if (flowFormAuthorizeEntity.IsNullOrEmpty() || flowFormAuthorizeEntity.FormOperate.IsNullOrEmpty()) return new List<FormOperatesModel>();
        return flowFormAuthorizeEntity.FormOperate.ToObject<List<FormOperatesModel>>();
    }
    #endregion
}

