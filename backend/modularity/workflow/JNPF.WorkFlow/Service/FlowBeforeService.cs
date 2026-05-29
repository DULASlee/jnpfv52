using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.WorkFlow.Entitys.Dto.FlowBefore;
using JNPF.WorkFlow.Entitys.Enum;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.Properties;
using JNPF.WorkFlow.Interfaces.Manager;
using JNPF.WorkFlow.Interfaces.Repository;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.WorkFlow.Service;

/// <summary>
/// 流程审批.
/// </summary>
[ApiDescriptionSettings(Tag = "WorkflowEngine", Name = "FlowBefore", Order = 303)]
[Route("api/workflow/Engine/[controller]")]
public class FlowBeforeService : IDynamicApiController, ITransient
{
    private readonly IFlowTaskRepository _flowTaskRepository;
    private readonly IFlowTaskManager _flowTaskManager;
    private readonly IUserManager _userManager;

    public FlowBeforeService(IFlowTaskRepository flowTaskRepository, IFlowTaskManager flowTaskManager, IUserManager userManager)
    {
        _flowTaskRepository = flowTaskRepository;
        _flowTaskManager = flowTaskManager;
        _userManager = userManager;
    }

    #region Get

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <param name="category">分类.</param>
    /// <returns></returns>
    [HttpGet("List/{category}")]
    public async Task<dynamic> GetList([FromQuery] FlowBeforeListQuery input, string category)
    {
        try
        {
            switch (category)
            {
                case "1":
                    return await _flowTaskRepository.GetWaitList(input);
                case "2":
                    return await _flowTaskRepository.GetTrialList(input);
                case "3":
                    return await _flowTaskRepository.GetCirculateList(input);
                case "4":
                    return await _flowTaskRepository.GetBatchWaitList(input);
                default:
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
        catch (Exception ex)
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

    /// <summary>
    /// 获取任务详情.
    /// </summary>
    /// <param name="id">任务id.</param>
    /// <param name="flowId">流程id.</param>
    /// <param name="taskNodeId">节点id.</param>
    /// <param name="taskOperatorId">经办id.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    [UnifySerializerSetting("special")]
    public async Task<dynamic> GetInfo(string id, [FromQuery] string flowId, [FromQuery] string taskNodeId, [FromQuery] string taskOperatorId)
    {
        try
        {
            return await _flowTaskManager.GetFlowBeforeInfo(id, flowId, taskNodeId, taskOperatorId);
        }
        catch (AppFriendlyException ex)
        {
            throw Oops.Oh(ex.ErrorCode);
        }
    }

    /// <summary>
    /// 审批汇总.
    /// </summary>
    /// <param name="taskRecordId">主键值.</param>
    /// <param name="category">分类（1：部门，2：角色，3：岗位）.</param>
    /// <returns></returns>
    [HttpGet("RecordList/{taskRecordId}")]
    public async Task<dynamic> GetRecordList(string taskRecordId, [FromQuery] string category, [FromQuery] string type)
    {
        var recordList = await _flowTaskRepository.GetRecordListByCategory(taskRecordId, category, type);
        var categoryId = recordList.Select(x => x.category).Distinct().ToList();
        var list = new List<FlowBeforeRecordListOutput>();
        foreach (var item in categoryId)
        {
            var categoryList = recordList.FindAll(x => x.category == item).ToList();
            var output = new FlowBeforeRecordListOutput();
            output.fullName = categoryList.FirstOrDefault()?.categoryName;
            output.list = categoryList.OrderByDescending(x => x.handleTime).ToList();
            list.Add(output);
        }

        return list;
    }

    /// <summary>
    /// 获取候选人编码.
    /// </summary>
    /// <param name="taskOperatorId">经办id.</param>
    /// <param name="flowHandleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("Candidates/{taskOperatorId}")]
    public async Task<dynamic> Candidates(string taskOperatorId, [FromBody] FlowHandleModel flowHandleModel)
    {
        if (taskOperatorId != "0")
        {
            var flowTaskParamter = await _flowTaskManager.Validation(taskOperatorId, flowHandleModel);
            var flowEngine = _flowTaskRepository.GetFlowTemplateInfo(flowTaskParamter.flowTaskEntity.FlowId);
            if (flowTaskParamter.flowTaskEntity.RejectDataId.IsNotEmptyOrNull())
            {
                return new List<FlowTaskCandidateModel>();
            }
            await _flowTaskManager.AdjustNodeByCon(flowEngine, flowHandleModel.formData, flowTaskParamter.flowTaskOperatorEntity);
        }

        return await _flowTaskManager.GetCandidateModelList(taskOperatorId, flowHandleModel);
    }

    /// <summary>
    /// 获取候选人.
    /// </summary>
    /// <param name="taskOperatorId">经办id.</param>
    /// <param name="flowHandleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("CandidateUser/{taskOperatorId}")]
    public async Task<dynamic> CandidateUser(string taskOperatorId, [FromBody] FlowHandleModel flowHandleModel)
    {
        return await _flowTaskManager.GetCandidateModelList(taskOperatorId, flowHandleModel, 1);
    }

    /// <summary>
    /// 批量审批流程分类列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("BatchFlowSelector")]
    public async Task<dynamic> BatchFlowSelector()
    {
        return await _flowTaskRepository.BatchFlowSelector();
    }

    /// <summary>
    /// 批量审批流程列表.
    /// </summary>
    /// <param name="templateId"></param>
    /// <returns></returns>
    [HttpGet("BatchFlowJsonList/{templateId}")]
    public async Task<dynamic> BatchFlowJsonList(string templateId)
    {
        var list = (await _flowTaskRepository.GetWaitList()).FindAll(x => x.isBatch == 1 && x.templateId == templateId);
        var output = new List<object>();
        foreach (var item in list)
        {
            var flowJson = _flowTaskRepository.GetFlowTemplateJsonInfo(x => x.Id == item.flowId && x.DeleteMark == null);
            if (flowJson.IsNotEmptyOrNull())
            {
                output.Add(new { id = flowJson.Id, fullName = string.Format("{0}(v{1})", flowJson.FullName, flowJson.Version), flowTemplateJson = flowJson.FlowTemplateJson });
            }
        }
        return output.Distinct();
    }

    /// <summary>
    /// 批量审批节点列表.
    /// </summary>
    /// <param name="flowId">流程id.</param>
    /// <returns></returns>
    [HttpGet("NodeSelector/{flowId}")]
    public async Task<dynamic> NodeSelector(string flowId)
    {
        return await _flowTaskManager.NodeSelector(flowId);
    }

    /// <summary>
    /// 批量审批候选人.
    /// </summary>
    /// <param name="flowId">流程id.</param>
    /// <param name="taskOperatorId">经办id.</param>
    /// <returns></returns>
    [HttpGet("BatchCandidate")]
    public async Task<dynamic> GetBatchCandidate([FromQuery] string flowId, [FromQuery] string taskOperatorId)
    {
        await _flowTaskManager.Validation(taskOperatorId, new FlowHandleModel());
        return await _flowTaskManager.GetBatchCandidate(flowId, taskOperatorId);
    }

    /// <summary>
    /// 验证站内信详情是否有查看权限.
    /// </summary>
    /// <param name="taskOperatorId">经办id.</param>
    /// <returns></returns>
    [HttpGet("{taskOperatorId}/Info")]
    public async Task<dynamic> IsInfo(string taskOperatorId)
    {
        var flowTaskOperatorEntity = await _flowTaskRepository.GetTaskOperatorInfo(taskOperatorId);
        if (flowTaskOperatorEntity.IsNullOrEmpty())
            throw Oops.Oh(ErrorCode.WF0029);
        var flowTaskEntity = _flowTaskRepository.GetTaskFirstOrDefault(flowTaskOperatorEntity.TaskId);
        if (flowTaskOperatorEntity.HandleId == _userManager.UserId)
        {
            if (flowTaskOperatorEntity.State == -1 || flowTaskEntity.Status == 5)
                throw Oops.Oh(ErrorCode.WF0029);
        }
        else
        {
            var toUserId = _flowTaskRepository.GetToUserId(flowTaskOperatorEntity.HandleId, flowTaskEntity.FlowId);
            if (!toUserId.Contains(_userManager.UserId) || flowTaskOperatorEntity.State == -1 || flowTaskEntity.Status == 5)
                throw Oops.Oh(ErrorCode.WF0029);
        }
        // true 跳转抄送页面 false 审批页面
        return new { isCheck = flowTaskOperatorEntity.Completion != 0 };
    }

    /// <summary>
    /// 节点列表.
    /// </summary>
    /// <param name="taskId">任务id.</param>
    /// <returns></returns>
    [HttpGet("Selector/{taskId}")]
    public async Task<dynamic> Selector(string taskId)
    {
        var nodeList = await _flowTaskRepository.GetTaskNodeList(x => x.TaskId == taskId && x.State == 0 && x.NodeType == "approver");
        return nodeList.Select(x => new { id = x.Id, nodeName = x.NodeName }).ToList();
    }

    /// <summary>
    /// 驳回节点列表.
    /// </summary>
    /// <param name="taskOperatorId">经办id.</param>
    /// <returns></returns>
    [HttpGet("RejectList/{taskOperatorId}")]
    public async Task<dynamic> RejectNodeList(string taskOperatorId)
    {
        return await _flowTaskManager.RejectNodeList(taskOperatorId);
    }

    /// <summary>
    /// 子流程详情.
    /// </summary>
    /// <param name="taskNodeId">节点id.</param>
    /// <returns></returns>
    [HttpGet("SubFlowInfo/{taskNodeId}")]
    public async Task<dynamic> SubFlowInfo(string taskNodeId)
    {
        var output = new List<object>();
        var taskNodeEntity = await _flowTaskRepository.GetTaskNodeInfo(taskNodeId);
        if (FlowTaskNodeTypeEnum.subFlow.ParseToString().Equals(taskNodeEntity.NodeType))
        {
            var childProp = taskNodeEntity.NodePropertyJson.ToObject<ChildTaskProperties>();
            foreach (var item in childProp.childTaskId)
            {
                var childTaskInfo = await _flowTaskManager.GetFlowBeforeInfo(item, childProp.flowId, null, null);
                output.Add(childTaskInfo);
            }
        }
        return output;
    }

    /// <summary>
    /// 挂起任务是否存在异步子流程.
    /// </summary>
    /// <param name="taskId">任务id.</param>
    /// <returns></returns>
    [HttpGet("Suspend/{taskId}")]
    public async Task<dynamic> Suspend(string taskId)
    {
        return await _flowTaskRepository.AnyFlowTask(x => x.ParentId == taskId && x.IsAsync == 1 && x.DeleteMark == null);
    }
    #endregion

    #region POST

    /// <summary>
    /// 审核同意.
    /// </summary>
    /// <param name="taskOperatorId">经办id.</param>
    /// <param name="flowHandleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("Audit/{taskOperatorId}")]
    public async Task<dynamic> Audit(string taskOperatorId, [FromBody] FlowHandleModel flowHandleModel)
    {
        var flowTaskParamter = await _flowTaskManager.Validation(taskOperatorId, flowHandleModel);
        var flag = await _flowTaskManager.IsSwerve(flowTaskParamter);
        return flag ? await _flowTaskManager.Swerve(flowTaskParamter) : await _flowTaskManager.Audit(flowTaskParamter);
    }

    /// <summary>
    /// 审核拒绝.
    /// </summary>
    /// <param name="taskOperatorId">经办id.</param>
    /// <param name="flowHandleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("Reject/{taskOperatorId}")]
    public async Task<dynamic> Reject(string taskOperatorId, [FromBody] FlowHandleModel flowHandleModel)
    {
        var flowTaskParamter = await _flowTaskRepository.GetTaskParamterByOperatorId(taskOperatorId, flowHandleModel);
        if (flowTaskParamter.flowTaskEntity.Suspend == 1) throw Oops.Oh(ErrorCode.WF0046);
        if (_flowTaskManager.IsSubFlowUpNode(flowTaskParamter))
            throw Oops.Oh(ErrorCode.WF0019);
        if (flowHandleModel.rejectType.IsNotEmptyOrNull())
        {
            flowTaskParamter.approversProperties.rejectType = flowHandleModel.rejectType.ParseToInt();
        }
        return await _flowTaskManager.Reject(flowTaskParamter);
    }

    /// <summary>
    /// 审批撤回.
    /// 注意：在撤销流程时要保证你的下一节点没有处理这条记录；如已处理则无法撤销流程.
    /// </summary>
    /// <param name="taskRecordId">经办记录id.</param>
    /// <param name="flowHandleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("Recall/{taskRecordId}")]
    public async Task Recall(string taskRecordId, [FromBody] FlowHandleModel flowHandleModel)
    {
        var flowTaskOperatorRecord = await _flowTaskRepository.GetTaskOperatorRecordInfo(taskRecordId);
        if (await _flowTaskRepository.AnyFlowTask(x => x.ParentId == flowTaskOperatorRecord.TaskId && x.Status != FlowTaskStatusEnum.Cancel.ParseToInt() && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.WF0018);
        var flowTaskParamter = await _flowTaskRepository.GetTaskParamterByOperatorId(flowTaskOperatorRecord.TaskOperatorId, flowHandleModel);
        if (flowTaskParamter.flowTaskEntity.Suspend == 1) throw Oops.Oh(ErrorCode.WF0046);
        await _flowTaskManager.Recall(flowTaskParamter, flowTaskOperatorRecord);
    }

    /// <summary>
    /// 终止审核.
    /// </summary>
    /// <param name="taskId">任务id.</param>
    /// <param name="flowHandleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("Cancel/{taskId}")]
    public async Task Cancel(string taskId, [FromBody] FlowHandleModel flowHandleModel)
    {
        var flowTaskParamter = await _flowTaskRepository.GetTaskParamterByTaskId(taskId, flowHandleModel);
        if (flowTaskParamter.flowTaskEntity.Suspend == 1) throw Oops.Oh(ErrorCode.WF0046);
        if (flowTaskParamter.flowTaskEntity.FlowType == 1)
            throw Oops.Oh(ErrorCode.WF0016);
        await _flowTaskManager.Cancel(flowTaskParamter);
    }

    /// <summary>
    /// 转办.
    /// </summary>
    /// <param name="taskOperatorId">经办id.</param>
    /// <param name="flowHandleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("Transfer/{taskOperatorId}")]
    public async Task Transfer(string taskOperatorId, [FromBody] FlowHandleModel flowHandleModel)
    {
        var flowTaskParamter = await _flowTaskManager.Validation(taskOperatorId, flowHandleModel);
        await _flowTaskManager.Transfer(flowTaskParamter);
    }

    /// <summary>
    /// 指派.
    /// </summary>
    /// <param name="taskId">任务id.</param>
    /// <param name="flowHandleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("Assign/{taskId}")]
    public async Task Assigned(string taskId, [FromBody] FlowHandleModel flowHandleModel)
    {
        var nodeEntity = await _flowTaskRepository.GetTaskNodeList(x => x.TaskId == taskId && x.State == 0 && FlowTaskNodeTypeEnum.subFlow.ParseToString().Equals(x.NodeType) && x.NodeCode.Equals(flowHandleModel.nodeCode));
        if (nodeEntity.IsNotEmptyOrNull() && nodeEntity.Count > 0)
            throw Oops.Oh(ErrorCode.WF0014);
        var flowTaskParamter = await _flowTaskRepository.GetTaskParamterByTaskId(taskId, flowHandleModel);
        if (flowTaskParamter.flowTaskEntity.Suspend == 1) throw Oops.Oh(ErrorCode.WF0046);
        flowTaskParamter.thisFlowTaskOperatorEntityList = await _flowTaskRepository.GetTaskOperatorList(x => x.State == 0 && x.NodeCode == flowHandleModel.nodeCode && x.TaskId == taskId);
        await _flowTaskManager.Assigned(flowTaskParamter);
    }

    /// <summary>
    /// 保存审批草稿数据.
    /// </summary>
    /// <param name="taskOperatorId">经办id.</param>
    /// <param name="flowHandleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("SaveAudit/{taskOperatorId}")]
    [UnitOfWork]
    public async Task SaveAudit(string taskOperatorId, [FromBody] FlowHandleModel flowHandleModel)
    {
        var flowTaskParamter = await _flowTaskManager.Validation(taskOperatorId, flowHandleModel);
        if (flowTaskParamter.flowTaskEntity.Suspend == 1) throw Oops.Oh(ErrorCode.WF0046);
        flowTaskParamter.flowTaskOperatorEntity.DraftData = flowHandleModel.formData.ToJsonString();
        await _flowTaskRepository.UpdateTaskOperator(flowTaskParamter.flowTaskOperatorEntity);
    }

    /// <summary>
    /// 批量审批.
    /// </summary>
    /// <param name="flowHandleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("BatchOperation")]
    public async Task BatchOperation([FromBody] FlowHandleModel flowHandleModel)
    {
        foreach (var item in flowHandleModel.ids)
        {
            var flowTaskParamter = await _flowTaskRepository.GetTaskParamterByOperatorId(item, flowHandleModel);
            flowTaskParamter.formData = await _flowTaskManager.GetBatchOperationData(flowTaskParamter);
            flowHandleModel.formData = flowTaskParamter.formData;
            switch (flowHandleModel.batchType)
            {
                case 0:
                    if (flowTaskParamter.flowTaskOperatorEntity == null)
                        throw Oops.Oh(ErrorCode.COM1005);
                    if (flowTaskParamter.flowTaskOperatorEntity.Completion != 0)
                        throw Oops.Oh(ErrorCode.WF0006);
                    await _flowTaskManager.Audit(flowTaskParamter);
                    break;
                case 1:
                    await Reject(item, flowHandleModel);
                    break;
                case 2:
                    await Transfer(item, flowHandleModel);
                    break;
            }
        }
    }

    /// <summary>
    /// 任务(变更/复活).
    /// </summary>
    /// <param name="flowHandleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("Change")]
    public async Task<dynamic> Change([FromBody] FlowHandleModel flowHandleModel)
    {
        // 清除依次经办数据
        if (await _flowTaskRepository.AnyFlowTask(x => x.Id == flowHandleModel.taskId && x.Suspend == 1 && x.DeleteMark == null)) throw Oops.Oh(ErrorCode.WF0046);
        await _flowTaskRepository.DeleteTaskOperatorUser(x => x.TaskId == flowHandleModel.taskId);
        _flowTaskRepository.DeleteFlowCandidates(x => x.TaskId == flowHandleModel.taskId);
        var flowTaskParamter = await _flowTaskRepository.GetTaskParamterByTaskId(flowHandleModel.taskId, flowHandleModel);
        return await _flowTaskManager.Change(flowTaskParamter);
    }

    /// <summary>
    /// 加签.
    /// </summary>
    /// <param name="taskOperatorId">经办id.</param>
    /// <param name="flowHandleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("freeApprover/{taskOperatorId}")]
    public async Task<dynamic> FreeApprover(string taskOperatorId, [FromBody] FlowHandleModel flowHandleModel)
    {
        return await Audit(taskOperatorId, flowHandleModel);
    }

    /// <summary>
    /// 挂起.
    /// </summary>
    /// <param name="taskId">任务id.</param>
    /// <param name="flowHandleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("Suspend/{taskId}")]
    public async Task Suspend(string taskId, [FromBody] FlowHandleModel flowHandleModel)
    {
        var flowTaskParamter = await _flowTaskRepository.GetTaskParamterByTaskId(taskId, flowHandleModel);
        await _flowTaskManager.Suspend(flowTaskParamter);
    }

    /// <summary>
    /// 恢复.
    /// </summary>
    /// <param name="taskId">任务id.</param>
    /// <param name="flowHandleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("Restore/{taskId}")]
    public async Task Restore(string taskId, [FromBody] FlowHandleModel flowHandleModel)
    {
        var flowTaskParamter = await _flowTaskRepository.GetTaskParamterByTaskId(taskId, flowHandleModel);
        await _flowTaskManager.Restore(flowTaskParamter);
    }
    #endregion
}
