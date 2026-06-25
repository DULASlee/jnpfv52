using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Job;
using JNPF.Extensions;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Models.Job;
using JNPF.Common.Models.WorkFlow;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.Message.Interfaces;
using JNPF.RemoteRequest.Extensions;
using JNPF.Schedule;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using JNPF.TaskQueue;
using JNPF.TaskScheduler.Entitys;
using JNPF.TaskScheduler.Entitys.Enum;
using JNPF.VisualDev.Interfaces;
using JNPF.WorkFlow.Entitys.Dto.FlowBefore;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Enum;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.Properties;
using JNPF.WorkFlow.Interfaces.Manager;
using JNPF.WorkFlow.Interfaces.Repository;
using Mapster;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SqlSugar;

namespace JNPF.WorkFlow.Manager;

public class FlowTaskManager : IFlowTaskManager, ITransient
{
    private readonly IFlowTaskRepository _flowTaskRepository;
    private readonly IUsersService _usersService;
    private readonly IRunService _runService;
    private readonly IUserManager _userManager;
    private readonly IJobManager _jobManager;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ITenant _db;
    private readonly IServer _server;
    private readonly ITaskQueue _taskQueue;
    private FlowTemplateUtil flowTemplateUtil;
    private FlowTaskUserUtil flowTaskUserUtil;
    private FlowTaskNodeUtil flowTaskNodeUtil;
    private FlowTaskMsgUtil flowTaskMsgUtil;
    private FlowTaskOtherUtil flowTaskOtherUtil;

    public FlowTaskManager(
        IFlowTaskRepository flowTaskRepository,
        IUsersService usersService,
        IOrganizeService organizeService,
        IDepartmentService departmentService,
        IUserRelationService userRelationService,
        IMessageManager messageManager,
        IDataInterfaceService dataInterfaceService,
        IRunService runService,
        IUserManager userManager,
        IJobManager jobManager,
        ISchedulerFactory schedulerFactory,
        ITaskQueue taskQueue,
        IDataBaseManager dataBaseManager,
        ICacheManager cacheManager,
        ISqlSugarClient context,
        IServer server)
    {
        _flowTaskRepository = flowTaskRepository;
        _server = server;
        _usersService = usersService;
        _runService = runService;
        _userManager = userManager;
        _jobManager = jobManager;
        _schedulerFactory = schedulerFactory;
        _taskQueue = taskQueue;
        flowTemplateUtil = new FlowTemplateUtil(dataBaseManager, userManager, flowTaskRepository, usersService);
        flowTaskUserUtil = new FlowTaskUserUtil(flowTaskRepository, usersService, organizeService, departmentService, userRelationService, dataInterfaceService, userManager, cacheManager);
        flowTaskNodeUtil = new FlowTaskNodeUtil(flowTaskRepository);
        flowTaskMsgUtil = new FlowTaskMsgUtil(messageManager, flowTaskRepository, userManager, usersService, dataInterfaceService);
        flowTaskOtherUtil = new FlowTaskOtherUtil(flowTaskRepository, usersService, runService, userManager, schedulerFactory);
        _db = context.AsTenant();
    }

    #region PublicMethod

    /// <summary>
    /// 获取任务详情.
    /// </summary>
    /// <param name="id">任务id.</param>
    /// <param name="flowId">流程id.</param>
    /// <param name="taskNodeId">节点id.</param>
    /// <param name="taskOperatorId">经办id.</param>
    /// <returns></returns>
    public async Task<FlowBeforeInfoOutput> GetFlowBeforeInfo(string id, string flowId, string taskNodeId, string taskOperatorId = null)
    {
        try
        {
            var output = new FlowBeforeInfoOutput();
            output.flowTemplateInfo = _flowTaskRepository.GetFlowTemplateInfo(flowId);
            var flowTaskParamter = taskNodeId.IsNotEmptyOrNull() ? await _flowTaskRepository.GetTaskParamterByNodeId(taskNodeId, null) : await _flowTaskRepository.GetTaskParamterByTaskId(id, null);
            if (flowTaskParamter.IsNotEmptyOrNull())
            {
                output.flowFormInfo = await _flowTaskRepository.GetFlowFromModel(flowTaskParamter.startProperties.formId);
                if (output.flowFormInfo == null) throw Oops.Oh(ErrorCode.WF0053);
                output.flowTaskInfo = flowTaskParamter.flowTaskEntity.Adapt<FlowTaskModel>();
                output.flowFormInfo.propertyJson = _runService.GetVisualDevModelDataConfig(output.flowFormInfo.propertyJson, output.flowFormInfo.tableJson, (int)output.flowFormInfo.formType);
                if (flowTaskParamter.flowTaskEntity.Suspend == 1)
                {
                    output.flowTaskInfo.status = FlowTaskStatusEnum.Suspend.ParseToInt();
                    output.flowTaskInfo.suspend = flowTaskParamter.flowTaskEntity.Restore.IsNotEmptyOrNull();
                }
                output.flowTaskOperatorRecordList = await _flowTaskRepository.GetTaskOperatorRecordModelList(id);
                if (flowTaskParamter.flowTaskNodeEntityList.IsNotEmptyOrNull())
                {
                    output.flowTaskNodeList = flowTaskParamter.flowTaskNodeEntityList.Adapt<List<FlowTaskNodeModel>>().OrderBy(x => x.sortCode).ToList();
                }
                var flowTaskOperatorList = await _flowTaskRepository.GetTaskOperatorList(x => x.TaskId == id && x.State == 0);
                if (flowTaskOperatorList.Any())
                {
                    output.flowTaskOperatorList = flowTaskOperatorList.Adapt<List<FlowTaskOperatorModel>>();
                }
                if (taskNodeId.IsNotEmptyOrNull())
                {
                    output.flowTaskInfo.thisStep = flowTaskParamter.approversProperties.title;
                    output.approversProperties = flowTaskParamter.approversProperties;
                    output.formOperates = await _flowTaskRepository.GetFlowFormAuthorize(id, flowTaskParamter.flowTaskNodeEntity.NodeCode);
                    output.flowFormInfo = await _flowTaskRepository.GetFlowFromModel(flowTaskParamter.flowTaskNodeEntity.FormId);
                }
                else
                {
                    output.formOperates = flowTaskParamter.startProperties.formOperates;
                    output.approversProperties = flowTaskParamter.startProperties;
                }
                output.formData = await _runService.GetFlowFormDataDetails(output.flowFormInfo.id, id);
                flowTaskParamter.formData = output.formData;
                // 复活初始数据
                if (flowTaskParamter.flowTaskEntity.ReviveNodeId.IsNotEmptyOrNull())
                {
                    // 复活取值规则
                    if (flowTaskParamter.startProperties.resurgenceDataRule == 1)
                    {
                        output.draftData = flowTaskParamter.flowTaskNodeEntityList.Find(x => x.Id == flowTaskParamter.flowTaskEntity.ReviveNodeId)?.DraftData?.ToObject();
                        if (output.formData.IsNotEmptyOrNull() && output.draftData.IsNotEmptyOrNull())
                        {
                            var draftDataObj = output.draftData?.ToObject<JObject>();
                            var formDataObj = output.formData?.ToObject<JObject>();
                            if (draftDataObj.ContainsKey("jnpf_resurgence") && draftDataObj.ContainsKey("version") && formDataObj.ContainsKey("version"))
                            {
                                draftDataObj["version"] = formDataObj["version"];
                                output.draftData = draftDataObj;
                            }
                        }
                    }
                    else
                    {
                        output.draftData = output.formData;
                    }
                }
                else
                {
                    // 草稿数据
                    var flowTaskOperator = output.flowTaskOperatorList.Find(x => x.id == taskOperatorId);
                    if (flowTaskOperator.IsNotEmptyOrNull() && flowTaskOperator.draftData.IsNotEmptyOrNull())
                    {
                        output.draftData = flowTaskOperator.draftData.ToObject();
                    }
                }
                // 流程图节点显示完成情况以及审批人员
                foreach (var item in output.flowTaskNodeList)
                {
                    if (item.completion == 1 || FlowTaskNodeTypeEnum.end.ParseToString().Equals(flowTaskParamter.flowTaskEntity.ThisStepId))
                    {
                        item.type = "0";
                    }
                    if (flowTaskParamter.flowTaskEntity.ThisStepId.Contains(item.nodeCode))
                    {
                        item.type = "1";
                    }
                    item.userName = await flowTaskUserUtil.GetApproverUserName(item, flowTaskParamter, output.flowTemplateInfo);
                }
                output.noOperateAuth = !_flowTaskRepository.GetOrgAdminAuthorize(flowTaskParamter.flowTaskEntity.CreatorUserId, 0);
            }
            else
            {
                var startProperties = output.flowTemplateInfo.flowTemplateJson.ToObject<FlowTemplateJsonModel>().properties.ToObject<StartProperties>();
                output.flowFormInfo = await _flowTaskRepository.GetFlowFromModel(startProperties.formId);
                output.flowFormInfo.propertyJson = _runService.GetVisualDevModelDataConfig(output.flowFormInfo.propertyJson, output.flowFormInfo.tableJson, (int)output.flowFormInfo.formType);
                output.formData = await _runService.GetFlowFormDataDetails(output.flowFormInfo.id, id);
                output.formOperates = startProperties.formOperates;
                output.approversProperties = startProperties;
            }
            return output;
        }
        catch (AppFriendlyException ex)
        {
            throw Oops.Oh(ex.ErrorCode);
        }
    }

    /// <summary>
    /// 保存.
    /// </summary>
    /// <param name="flowTaskSubmitModel">提交参数.</param>
    /// <returns></returns>
    public async Task<FlowTaskEntity> Save(FlowTaskSubmitModel flowTaskSubmitModel)
    {
        try
        {
            var flowTaskEntity = new FlowTaskEntity();
            var flowJsonInfo = _flowTaskRepository.GetFlowTemplateInfo(flowTaskSubmitModel.flowId);
            flowTaskSubmitModel.isDev = _flowTaskRepository.IsDevFlow(flowJsonInfo.templateId);//是否为功能表单
            flowTaskSubmitModel.flowJsonModel = flowJsonInfo;
            await flowTaskOtherUtil.GetFlowTitle(flowTaskSubmitModel);
            // 表单数据处理
            var processId = await FlowDynamicDataManage(flowTaskSubmitModel);
            if (flowTaskSubmitModel.id.IsNullOrEmpty())
            {
                // 功能流程保存不做任务的编辑
                if (!(flowTaskSubmitModel.isDev && flowTaskSubmitModel.status == 1 && flowTaskSubmitModel.parentId.Equals("0")))
                {
                    flowTaskEntity.Id = processId;
                    flowTaskEntity.ProcessId = processId;
                    flowTaskEntity.FullName = flowTaskSubmitModel.parentId.Equals("0") ? flowTaskSubmitModel.flowTitle : string.Format("{0}(子流程)", flowTaskSubmitModel.flowTitle);
                    flowTaskEntity.FlowUrgent = flowTaskSubmitModel.flowUrgent;
                    flowTaskEntity.FlowId = flowJsonInfo.id;
                    flowTaskEntity.FlowCode = flowJsonInfo.enCode;
                    flowTaskEntity.FlowName = flowJsonInfo.flowName;
                    flowTaskEntity.FlowType = flowJsonInfo.type;
                    flowTaskEntity.FlowCategory = flowJsonInfo.category;
                    flowTaskEntity.FlowFormContentJson = flowTaskSubmitModel.formData.ToJsonString();
                    flowTaskEntity.FlowVersion = flowJsonInfo.version;
                    flowTaskEntity.StartTime = flowTaskSubmitModel.status == 0 ? DateTime.Now : null;
                    flowTaskEntity.ThisStep = "开始";
                    flowTaskEntity.Status = flowTaskSubmitModel.status == 0 ? FlowTaskStatusEnum.Handle.ParseToInt() : FlowTaskStatusEnum.Draft.ParseToInt();
                    flowTaskEntity.Completion = 0;
                    flowTaskEntity.EnabledMark = flowTaskSubmitModel.status == 0 ? 1 : 0;
                    flowTaskEntity.CreatorTime = DateTime.Now;
                    flowTaskEntity.CreatorUserId = flowTaskSubmitModel.crUser.IsEmpty() ? _userManager.UserId : flowTaskSubmitModel.crUser;
                    flowTaskEntity.ParentId = flowTaskSubmitModel.parentId;
                    flowTaskEntity.IsAsync = flowTaskSubmitModel.isAsync ? 1 : 0;
                    flowTaskEntity.TemplateId = flowJsonInfo.templateId;
                    flowTaskEntity.DelegateUserId = flowTaskSubmitModel.isDelegate ? _userManager.UserId : null;
                    // 保存发起人信息.
                    _flowTaskRepository.CreateFlowUser(flowTaskEntity.CreatorUserId, flowTaskEntity.Id);
                    await _flowTaskRepository.CreateTask(flowTaskEntity);
                }
            }
            else
            {
                if (!(flowTaskSubmitModel.isDev && flowTaskSubmitModel.status == 1 && flowTaskSubmitModel.parentId.Equals("0")))
                {
                    flowTaskEntity = _flowTaskRepository.GetTaskFirstOrDefault(flowTaskSubmitModel.id);
                    if (flowTaskEntity.Suspend == 1)
                        throw Oops.Oh(ErrorCode.WF0046);
                    if (flowTaskEntity.Status == FlowTaskStatusEnum.Handle.ParseToInt() && flowTaskSubmitModel.approvaUpType == 0)
                        throw Oops.Oh(ErrorCode.WF0031);
                    flowTaskEntity.FlowUrgent = flowTaskSubmitModel.flowUrgent;
                    flowTaskSubmitModel.crUser = flowTaskEntity.CreatorUserId;
                    await flowTaskOtherUtil.GetFlowTitle(flowTaskSubmitModel);
                    flowTaskEntity.FullName = flowTaskEntity.ParentId.Equals("0") ? flowTaskSubmitModel.flowTitle : string.Format("{0}(子流程)", flowTaskSubmitModel.flowTitle);
                    flowTaskEntity.FlowFormContentJson = flowTaskSubmitModel.formData.ToJsonString();
                    if (flowTaskSubmitModel.status == 0)
                    {
                        flowTaskEntity.Status = FlowTaskStatusEnum.Handle.ParseToInt();
                        flowTaskEntity.StartTime = DateTime.Now;
                        flowTaskEntity.LastModifyTime = DateTime.Now;
                        flowTaskEntity.LastModifyUserId = _userManager.UserId;
                        flowTaskEntity.EnabledMark = 1;
                    }
                    await _flowTaskRepository.UpdateTask(flowTaskEntity);
                }
            }
            return flowTaskEntity;
        }
        catch (AppFriendlyException ex)
        {
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 提交.
    /// </summary>
    /// <param name="flowTaskSubmitModel">提交参数.</param>
    /// <returns></returns>
    public async Task<dynamic> Submit(FlowTaskSubmitModel flowTaskSubmitModel)
    {
        try
        {
            if (!flowTaskSubmitModel.autoSubmit)
            {
                _db.BeginTran();
            }
            else
            {
                var flowHandModel = new FlowHandleModel { flowId = flowTaskSubmitModel.flowId, formData = flowTaskSubmitModel.formData };
                var output = await GetCandidateModelList("0", flowHandModel);
                if (output.type != 3) throw Oops.Oh(ErrorCode.WF0057);
            }
            var flowTaskParamter = new FlowTaskParamter();
            flowTaskParamter.candidateList = flowTaskSubmitModel.candidateList;
            flowTaskParamter.errorRuleUserList = flowTaskSubmitModel.errorRuleUserList;
            flowTaskParamter.branchList = flowTaskSubmitModel.branchList;
            flowTaskParamter.formData = flowTaskSubmitModel.formData;
            flowTaskParamter.copyIds = flowTaskSubmitModel.copyIds;

            // 流程任务
            flowTaskParamter.flowTaskEntity = await this.Save(flowTaskSubmitModel);
            flowTaskSubmitModel.childTask = !flowTaskSubmitModel.parentId.Equals("0") ? flowTaskParamter.flowTaskEntity.ToJsonString() : string.Empty;
            // 驳回提交
            if (flowTaskParamter.flowTaskEntity.RejectDataId.IsNotEmptyOrNull())
            {
                var rejectDataEntity = await _flowTaskRepository.GetRejectDataInfo(flowTaskParamter.flowTaskEntity.RejectDataId);
                await _flowTaskRepository.UpdateRejectData(rejectDataEntity);
                if (!flowTaskSubmitModel.autoSubmit) _db.CommitTran();
                return new List<FlowTaskCandidateModel>();
            }
            var formData = flowTaskSubmitModel.formData.ToObject<Dictionary<string, object>>();
            formData["id"] = flowTaskParamter.flowTaskEntity.Id;
            flowTaskParamter.formData = formData;
            // 流程引擎
            var flowEngineEntity = flowTaskSubmitModel.flowJsonModel.ToObject<FlowJsonModel>();

            // 流程节点
            flowTemplateUtil.Load(flowEngineEntity, flowTaskSubmitModel.formData.ToJsonString(), flowTaskParamter.flowTaskEntity.Id);
            flowTaskParamter.flowTaskNodeEntityList = flowTemplateUtil.flowTaskNodeEntityList;
            flowTaskParamter.flowTaskNodeEntity = flowTemplateUtil.startNode;
            flowTaskParamter.approversProperties = flowTemplateUtil.startPro.ToObject<ApproversProperties>();
            flowTaskParamter.startProperties = flowTemplateUtil.startPro;
            flowTaskParamter.flowTaskOperatorEntity = flowTemplateUtil.startNode.Adapt<FlowTaskOperatorEntity>();
            flowTaskParamter.flowTaskOperatorEntity.TaskId = flowTemplateUtil.startNode.TaskId;
            flowTaskParamter.flowTaskOperatorEntity.TaskNodeId = flowTemplateUtil.startNode.Id;
            flowTaskParamter.flowTaskOperatorEntity.Id = "0";

            // 选择分支变更节点
            await flowTaskNodeUtil.ChangeNodeListByBranch(flowTaskParamter);
            #region 保存候选人/异常节点处理
            flowTaskOtherUtil.SaveNodeCandidates(flowTaskParamter);
            #endregion
            // 保存表单权限
            await flowTaskOtherUtil.SaveFormOperates(flowTaskParamter.flowTaskNodeEntityList);
            await _flowTaskRepository.CreateTaskNode(flowTaskParamter.flowTaskNodeEntityList);

            flowTaskParamter.flowTaskEntity.IsBatch = flowTaskParamter.startProperties.isBatchApproval ? 1 : 0;

            #region 流程经办
            await CreateNextFlowTaskOperator(flowTaskParamter, 1, 2);
            if (flowTaskParamter.errorNodeList.Any())
            {
                if (!flowTaskSubmitModel.autoSubmit) _db.RollbackTran();
                return flowTaskParamter.errorNodeList;
            }
            #endregion

            #region 更新流程任务
            flowTaskParamter.flowTaskEntity.FlowFormContentJson = formData.ToJsonString();
            await _flowTaskRepository.UpdateTask(flowTaskParamter.flowTaskEntity);
            #endregion

            #region 更新当前抄送
            await flowTaskUserUtil.GetflowTaskCirculateEntityList(flowTaskParamter, 1);
            await _flowTaskRepository.CreateTaskCirculate(flowTaskParamter.flowTaskCirculateEntityList);
            #endregion

            #region 流程经办记录
            await flowTaskOtherUtil.CreateOperatorRecode(flowTaskParamter, 2);
            #endregion

            if (!flowTaskSubmitModel.autoSubmit) _db.CommitTran();

            #region 开始事件
            if (!await flowTaskMsgUtil.RequestEvents(flowTaskParamter.startProperties.initFuncConfig, flowTaskParamter, FuncConfigEnum.init) && flowTaskParamter.startProperties.funcConfigRule == 1)
            {
                await Cancel(flowTaskParamter);
            }
            #endregion

            #region 消息提醒
            // 审批消息
            var messageDic = flowTaskOtherUtil.GroupByOperator(flowTaskParamter.flowTaskOperatorEntityList);
            var bodyDic = new Dictionary<string, object>();

            //抄送
            var userIdList = flowTaskParamter.flowTaskCirculateEntityList.Select(x => x.ObjectId).ToList();
            if (userIdList.Any())
            {
                bodyDic = flowTaskMsgUtil.GetMesBodyText(flowTaskParamter, userIdList, null, 3, flowTaskParamter.flowTaskOperatorEntity.Id);
                await flowTaskMsgUtil.Alerts(flowTaskParamter.startProperties.copyMsgConfig, userIdList, flowTaskParamter, "MBXTLC007", bodyDic);
            }

            foreach (var item in messageDic.Keys)
            {
                var userList = messageDic[item].Select(x => x.HandleId).ToList();
                bodyDic = flowTaskMsgUtil.GetMesBodyText(flowTaskParamter, userList, messageDic[item], 2);
                await flowTaskMsgUtil.Alerts(flowTaskParamter.startProperties.waitMsgConfig, bodyDic.Keys.ToList(), flowTaskParamter, "MBXTLC001", bodyDic);
                // 超时提醒
                await TimeoutOrRemind(flowTaskParamter, item, messageDic[item]);
            }

            // 结束消息
            if (flowTaskParamter.flowTaskEntity.Status == FlowTaskStatusEnum.Adopt.ParseToInt())
            {
                #region 结束事件
                await flowTaskMsgUtil.RequestEvents(flowTaskParamter.startProperties.endFuncConfig, flowTaskParamter, FuncConfigEnum.end);
                #endregion

                bodyDic = flowTaskMsgUtil.GetMesBodyText(flowTaskParamter, new List<string>() { flowTaskParamter.flowTaskEntity.CreatorUserId }, null, 1);
                await flowTaskMsgUtil.Alerts(flowTaskParamter.startProperties.endMsgConfig, new List<string>() { flowTaskParamter.flowTaskEntity.CreatorUserId }, flowTaskParamter, "MBXTLC010", bodyDic);
            }

            //委托发起消息
            if (flowTaskSubmitModel.isDelegate)
            {
                await flowTaskMsgUtil.SendDelegateMsg("发起", flowTaskParamter.flowTaskEntity.CreatorUserId, flowTaskParamter.flowTaskEntity.FullName);
            }
            #endregion

            #region 自动审批
            await AutoAudit(flowTaskParamter);
            #endregion
            return new List<FlowTaskCandidateModel>();
        }
        catch (AppFriendlyException ex)
        {
            if (!flowTaskSubmitModel.autoSubmit) _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 审批(同意).
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <param name="isAuto">是否自动审批.</param>
    /// <returns></returns>
    public async Task<dynamic> Audit(FlowTaskParamter flowTaskParamter, bool isAuto = false)
    {
        flowTaskParamter.isAuto = isAuto;
        var candidates = flowTaskParamter.flowTaskEntity.RejectDataId.IsNotEmptyOrNull() ? new List<FlowCandidatesEntity>() : flowTaskOtherUtil.SaveNodeCandidates(flowTaskParamter);
        try
        {
            _db.BeginTran();
            if (!isAuto)
            {
                // 审批修改表单数据.
                if (flowTaskParamter.flowTaskOperatorEntity.Id.IsNotEmptyOrNull() && FlowTaskNodeTypeEnum.approver.ParseToString().Equals(flowTaskParamter.flowTaskNodeEntity.NodeType))
                {
                    var fEntity = _flowTaskRepository.GetFlowFromEntity(flowTaskParamter.flowTaskNodeEntity.FormId);
                    List<FormOperatesModel> formOperates = await _flowTaskRepository.GetFlowFormAuthorize(flowTaskParamter.flowTaskNodeEntity);
                    var systemControlList = formOperates.Where(x => !x.write && x.read).Select(x => x.id).ToList();
                    await _runService.SaveFlowFormData(fEntity, flowTaskParamter.formData.ToJsonString(), flowTaskParamter.flowTaskEntity.Id, flowTaskParamter.flowTaskEntity.FlowId, true, systemControlList);
                }
            }
            if (flowTaskParamter.flowTaskEntity.RejectDataId.IsNullOrEmpty())
            {
                await flowTaskNodeUtil.ChangeNodeListByBranch(flowTaskParamter);
            }
            // 依次审批当前节点所有审批人
            if (flowTaskParamter.approversProperties.counterSign == 2)
            {
                var operatorUserEntities = (await _flowTaskRepository.GetTaskOperatorUserList(x => x.TaskId == flowTaskParamter.flowTaskNodeEntity.TaskId && x.TaskNodeId == flowTaskParamter.flowTaskNodeEntity.Id && x.State == 0)).Adapt<List<FlowTaskOperatorEntity>>();
                // 取比当前节点审批人排序码大的与所有审批人员差集再加上当前节点审批人
                flowTaskParamter.thisFlowTaskOperatorEntityList = operatorUserEntities.Where(x => x.SortCode > flowTaskParamter.flowTaskOperatorEntity.SortCode).Union(flowTaskParamter.thisFlowTaskOperatorEntityList).ToList();
            }

            if (flowTaskParamter.flowTaskOperatorEntity.Id.IsNotEmptyOrNull())
            {
                #region 更新当前经办数据
                await flowTaskOtherUtil.UpdateFlowTaskOperator(flowTaskParamter, 1);
                #endregion

                #region 更新当前抄送
                await flowTaskUserUtil.GetflowTaskCirculateEntityList(flowTaskParamter, 1);
                await _flowTaskRepository.CreateTaskCirculate(flowTaskParamter.flowTaskCirculateEntityList);
                #endregion
            }

            #region 更新下一节点经办
            // 驳回审批
            if (flowTaskParamter.flowTaskEntity.RejectDataId.IsNotEmptyOrNull())
            {
                // 冻结驳回解冻必须是非前加签
                if (flowTaskParamter.freeApproverUserId.IsNullOrEmpty() && flowTaskParamter.flowTaskOperatorEntity.RollbackId.IsNullOrEmpty())
                {
                    if (flowTaskParamter.approversProperties.counterSign == 0 || flowTaskOtherUtil.IsAchievebilProportion(flowTaskParamter, 1))
                    {
                        var fEntity = _flowTaskRepository.GetFlowFromEntity(flowTaskParamter.flowTaskNodeEntity.FormId);
                        List<FormOperatesModel> formOperates = await _flowTaskRepository.GetFlowFormAuthorize(flowTaskParamter.flowTaskNodeEntity);
                        var systemControlList = formOperates.Where(x => !x.write && x.read).Select(x => x.id).ToList();
                        await _runService.SaveFlowFormData(fEntity, flowTaskParamter.formData.ToJsonString(), flowTaskParamter.flowTaskEntity.Id, flowTaskParamter.flowTaskEntity.FlowId, true, systemControlList);
                        var rejectDataEntity = await _flowTaskRepository.GetRejectDataInfo(flowTaskParamter.flowTaskEntity.RejectDataId);
                        await _flowTaskRepository.UpdateRejectData(rejectDataEntity);
                        _db.CommitTran();
                        return new List<FlowTaskCandidateModel>();
                    }
                }
            }
            if (flowTaskParamter.freeApproverUserId.IsNullOrEmpty() && flowTaskParamter.flowTaskOperatorEntity.RollbackId.IsNullOrEmpty())
            {
                await CreateNextFlowTaskOperator(flowTaskParamter, 1, 0);
                if (flowTaskParamter.errorNodeList.Count > 0)
                {
                    _db.RollbackTran();
                    return flowTaskParamter.errorNodeList;
                }
                foreach (var item in flowTaskParamter.flowTaskOperatorEntityList)
                {
                    var nextTaskNodeEntity = flowTaskParamter.flowTaskNodeEntityList.Find(m => m.Id.Equals(item.TaskNodeId));
                    var approverPropertiers = nextTaskNodeEntity.NodePropertyJson.ToObject<ApproversProperties>();
                    if (approverPropertiers.assigneeType == FlowTaskOperatorEnum.CandidateApprover.ParseToInt() && isAuto)
                    {
                        _db.RollbackTran();
                        return new List<FlowTaskCandidateModel>();
                    }
                }
            }
            #endregion

            #region 更新节点
            await _flowTaskRepository.UpdateTaskNode(flowTaskParamter.flowTaskNodeEntity);
            #endregion

            #region 更新任务
            if (flowTaskParamter.flowTaskEntity.ReviveNodeId.IsNotEmptyOrNull())
            {
                flowTaskParamter.flowTaskEntity.ReviveNodeId = null;
            }
            flowTaskParamter.flowTaskEntity.FlowFormContentJson = flowTaskParamter.formData.ToJsonString();
            await _flowTaskRepository.UpdateTask(flowTaskParamter.flowTaskEntity);
            #endregion
            _db.CommitTran();
            #region 消息与事件
            var bodyDic = new Dictionary<string, object>();
            flowTaskParamter.approversProperties = flowTaskOtherUtil.SyncApproProCofig(flowTaskParamter.approversProperties, flowTaskParamter.startProperties);
            //抄送
            var userIdList = flowTaskParamter.flowTaskCirculateEntityList.Select(x => x.ObjectId).ToList();
            if (userIdList.Any())
            {
                bodyDic = flowTaskMsgUtil.GetMesBodyText(flowTaskParamter, userIdList, null, 3, flowTaskParamter.flowTaskOperatorEntity.Id);
                await flowTaskMsgUtil.Alerts(flowTaskParamter.approversProperties.copyMsgConfig, userIdList, flowTaskParamter, "MBXTLC007", bodyDic);
            }
            //加签
            if (flowTaskParamter.freeApproverUserId.IsNotEmptyOrNull())
            {
                userIdList = flowTaskParamter.flowTaskOperatorEntityList.Select(x => x.HandleId).ToList();
                bodyDic = flowTaskMsgUtil.GetMesBodyText(flowTaskParamter, userIdList, flowTaskParamter.flowTaskOperatorEntityList, 2);
                await flowTaskMsgUtil.Alerts(flowTaskParamter.startProperties.waitMsgConfig, bodyDic.Keys.ToList(), flowTaskParamter, "MBXTLC001", bodyDic);
                _schedulerFactory.RemoveJob("Job_CS_" + flowTaskParamter.flowTaskNodeEntity.Id);
                _schedulerFactory.RemoveJob("Job_TX_" + flowTaskParamter.flowTaskNodeEntity.Id);
                await TimeoutOrRemind(flowTaskParamter, flowTaskParamter.flowTaskNodeEntity.Id, flowTaskParamter.flowTaskOperatorEntityList);
            }
            if (flowTaskParamter.flowTaskNodeEntity.Completion > 0)
            {
                // 关闭当前节点超时提醒任务
                _schedulerFactory.RemoveJob("Job_CS_" + flowTaskParamter.flowTaskNodeEntity.Id);
                _schedulerFactory.RemoveJob("Job_TX_" + flowTaskParamter.flowTaskNodeEntity.Id);

                #region 审批事件
                if (!await flowTaskMsgUtil.RequestEvents(flowTaskParamter.approversProperties.approveFuncConfig, flowTaskParamter, FuncConfigEnum.approve) && flowTaskParamter.startProperties.funcConfigRule == 1)
                {
                    await Cancel(flowTaskParamter);
                }
                #endregion

                #region 消息提醒
                var messageDic = flowTaskOtherUtil.GroupByOperator(flowTaskParamter.flowTaskOperatorEntityList);
                //审批
                foreach (var item in messageDic.Keys)
                {
                    var userList = messageDic[item].Select(x => x.HandleId).ToList();
                    bodyDic = flowTaskMsgUtil.GetMesBodyText(flowTaskParamter, userList, messageDic[item], 2);
                    await flowTaskMsgUtil.Alerts(flowTaskParamter.startProperties.waitMsgConfig, bodyDic.Keys.ToList(), flowTaskParamter, "MBXTLC001", bodyDic);
                    await flowTaskMsgUtil.Alerts(flowTaskParamter.approversProperties.approveMsgConfig, bodyDic.Keys.ToList(), flowTaskParamter, "MBXTLC002", bodyDic);
                    // 超时提醒
                    await TimeoutOrRemind(flowTaskParamter, item, messageDic[item]);
                }
                #endregion

                if (flowTaskParamter.flowTaskEntity.Status == FlowTaskStatusEnum.Adopt.ParseToInt())
                {
                    #region 结束事件
                    await flowTaskMsgUtil.RequestEvents(flowTaskParamter.startProperties.endFuncConfig, flowTaskParamter, FuncConfigEnum.end);
                    #endregion
                    //结束
                    bodyDic = flowTaskMsgUtil.GetMesBodyText(flowTaskParamter, new List<string>() { flowTaskParamter.flowTaskEntity.CreatorUserId }, null, 1);
                    await flowTaskMsgUtil.Alerts(flowTaskParamter.startProperties.endMsgConfig, new List<string>() { flowTaskParamter.flowTaskEntity.CreatorUserId }, flowTaskParamter, "MBXTLC010", bodyDic);
                }

                //委托审批消息
                if (flowTaskParamter.flowTaskOperatorEntity.HandleId.IsNotEmptyOrNull() && !_userManager.UserId.Equals(flowTaskParamter.flowTaskOperatorEntity.HandleId) && !isAuto)
                {
                    await flowTaskMsgUtil.SendDelegateMsg("审批", flowTaskParamter.flowTaskOperatorEntity.HandleId, flowTaskParamter.flowTaskEntity.FullName);
                }
            }
            #endregion

            #region 自动审批
            await AutoAudit(flowTaskParamter);
            #endregion
            return new List<FlowTaskCandidateModel>();
        }
        catch (AppFriendlyException ex)
        {
            var ids = candidates.Select(x => x.Id).ToArray();
            _flowTaskRepository.DeleteFlowCandidates(x => ids.Contains(x.Id));
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 审批(拒绝).
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    public async Task<dynamic> Reject(FlowTaskParamter flowTaskParamter)
    {
        try
        {
            _db.BeginTran();
            if (flowTaskParamter.flowTaskEntity.RejectDataId.IsNotEmptyOrNull()) throw Oops.Oh(ErrorCode.WF0045);
            //表单数据
            flowTaskParamter.formData = await _runService.GetFlowFormDataDetails(flowTaskParamter.flowTaskNodeEntity.FormId, flowTaskParamter.flowTaskEntity.Id);
            var flowEngineEntity = _flowTaskRepository.GetFlowTemplateInfo(flowTaskParamter.flowTaskEntity.FlowId);
            #region 更新当前经办数据
            await flowTaskOtherUtil.UpdateFlowTaskOperator(flowTaskParamter, 0);
            #endregion

            #region 自定义抄送
            await flowTaskUserUtil.GetflowTaskCirculateEntityList(flowTaskParamter, 1);
            await _flowTaskRepository.CreateTaskCirculate(flowTaskParamter.flowTaskCirculateEntityList);
            #endregion

            #region 更新驳回经办
            await CreateNextFlowTaskOperator(flowTaskParamter, 0, 0);
            if (flowTaskParamter.errorNodeList.Count > 0)
            {
                _db.RollbackTran();
                return flowTaskParamter.errorNodeList;
            }
            #endregion

            #region 更新流程任务
            flowTaskParamter.flowTaskEntity.ReviveNodeId = null;
            if (flowTaskParamter.flowTaskEntity.Status == FlowTaskStatusEnum.Reject.ParseToInt())
            {
                await _flowTaskRepository.UpdateTask(flowTaskParamter.flowTaskEntity);
                await _flowTaskRepository.DeleteFlowTaskAllData(flowTaskParamter.flowTaskEntity.Id, true, flowTaskParamter.approversProperties.rejectType == 1);
            }
            else
            {
                await _flowTaskRepository.UpdateTask(flowTaskParamter.flowTaskEntity);
                await _flowTaskRepository.CreateTaskOperator(flowTaskParamter.flowTaskOperatorEntityList);
                foreach (var item in flowTaskParamter.flowTaskOperatorEntityList)
                {
                    await AdjustNodeByCon(flowEngineEntity, flowTaskParamter.formData, item, true);
                }
            }
            #endregion
            _db.CommitTran();

            #region 消息与事件
            //退回事件
            if (!await flowTaskMsgUtil.RequestEvents(flowTaskParamter.approversProperties.rejectFuncConfig, flowTaskParamter, FuncConfigEnum.reject) && flowTaskParamter.startProperties.funcConfigRule == 1)
            {
                await Cancel(flowTaskParamter);
            }
            flowTaskParamter.approversProperties = flowTaskOtherUtil.SyncApproProCofig(flowTaskParamter.approversProperties, flowTaskParamter.startProperties);
            var bodyDic = new Dictionary<string, object>();
            //抄送
            var userIdList = flowTaskParamter.flowTaskCirculateEntityList.Select(x => x.ObjectId).ToList();
            if (userIdList.Any())
            {
                bodyDic = flowTaskMsgUtil.GetMesBodyText(flowTaskParamter, userIdList, null, 3, flowTaskParamter.flowTaskOperatorEntity.Id);
                await flowTaskMsgUtil.Alerts(flowTaskParamter.approversProperties.copyMsgConfig, userIdList, flowTaskParamter, "MBXTLC007", bodyDic);
            }

            if (flowTaskParamter.flowTaskOperatorEntityList.Any())
            {
                // 关闭当前节点超时提醒任务
                _schedulerFactory.RemoveJob("Job_CS_" + flowTaskParamter.flowTaskNodeEntity.Id);
                _schedulerFactory.RemoveJob("Job_TX_" + flowTaskParamter.flowTaskNodeEntity.Id);

                #region 审批事件
                if (!await flowTaskMsgUtil.RequestEvents(flowTaskParamter.approversProperties.approveFuncConfig, flowTaskParamter, FuncConfigEnum.approve) && flowTaskParamter.startProperties.funcConfigRule == 1)
                {
                    await Cancel(flowTaskParamter);
                }
                #endregion

                #region 消息提醒
                var messageDic = flowTaskOtherUtil.GroupByOperator(flowTaskParamter.flowTaskOperatorEntityList);
                //审批
                foreach (var item in messageDic.Keys)
                {
                    var userList = messageDic[item].Select(x => x.HandleId).ToList();
                    bodyDic = flowTaskMsgUtil.GetMesBodyText(flowTaskParamter, userList, messageDic[item], 2);
                    await flowTaskMsgUtil.Alerts(flowTaskParamter.startProperties.waitMsgConfig, bodyDic.Keys.ToList(), flowTaskParamter, "MBXTLC001", bodyDic);
                    await flowTaskMsgUtil.Alerts(flowTaskParamter.approversProperties.rejectMsgConfig, bodyDic.Keys.ToList(), flowTaskParamter, "MBXTLC003", bodyDic);
                    // 超时提醒
                    await TimeoutOrRemind(flowTaskParamter, item, messageDic[item]);
                }
                #endregion

            }

            //委托审批消息
            if (!_userManager.UserId.Equals(flowTaskParamter.flowTaskOperatorEntity.HandleId))
            {
                await flowTaskMsgUtil.SendDelegateMsg("审批", flowTaskParamter.flowTaskOperatorEntity.HandleId, flowTaskParamter.flowTaskEntity.FullName);
            }
            //退回到发起.
            if (flowTaskParamter.flowTaskEntity.Status == FlowTaskStatusEnum.Reject.ParseToInt())
            {
                bodyDic = flowTaskMsgUtil.GetMesBodyText(flowTaskParamter, new List<string>() { flowTaskParamter.flowTaskEntity.CreatorUserId }, null, 2);
                await flowTaskMsgUtil.Alerts(flowTaskParamter.approversProperties.rejectMsgConfig, new List<string>() { flowTaskParamter.flowTaskEntity.CreatorUserId }, flowTaskParamter, "MBXTLC003", bodyDic);
            }
            #endregion

            #region 默认审批
            if (flowTaskParamter.flowTaskOperatorEntityList.Count == 1 && flowTaskParamter.flowTaskOperatorEntityList.FirstOrDefault().HandleId == "jnpf")
            {
                var defaultAuditOperator = flowTaskParamter.flowTaskOperatorEntityList.FirstOrDefault();
                var handleModel = new FlowHandleModel();
                handleModel.handleOpinion = "默认审批通过";
                handleModel.candidateList = flowTaskParamter.candidateList;
                var formId = (await _flowTaskRepository.GetTaskNodeInfo(defaultAuditOperator.TaskNodeId)).FormId;
                handleModel.formData = await _runService.GetFlowFormDataDetails(formId, defaultAuditOperator.TaskId);
                flowTaskParamter = await _flowTaskRepository.GetTaskParamterByOperatorId(defaultAuditOperator.Id, handleModel);
                await this.Audit(flowTaskParamter, true);
            }
            #endregion
            return new List<FlowTaskCandidateModel>();
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 审批(撤回).
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <param name="flowTaskOperatorRecordEntity">经办记录.</param>
    public async Task Recall(FlowTaskParamter flowTaskParamter, FlowTaskOperatorRecordEntity flowTaskOperatorRecordEntity)
    {
        try
        {
            _db.BeginTran();
            if (flowTaskParamter.flowTaskEntity.RejectDataId.IsNotEmptyOrNull()) throw Oops.Oh(ErrorCode.WF0046);
            if (flowTaskOperatorRecordEntity.HandleStatus == 13)
                throw Oops.Oh(ErrorCode.WF0011);
            //拒绝不撤回
            if (flowTaskParamter.flowTaskOperatorEntity.HandleStatus == 0)
                throw Oops.Oh(ErrorCode.WF0010);
            //撤回经办记录
            if (flowTaskOperatorRecordEntity.Status == -1)
                throw Oops.Oh(ErrorCode.WF0010);
            //所有经办
            var flowTaskOperatorEntityList = await _flowTaskRepository.GetTaskOperatorList(x => x.TaskId == flowTaskOperatorRecordEntity.TaskId && x.State == 0);
            #region 撤回判断
            //任务待审状态才能撤回
            if (!(flowTaskParamter.flowTaskEntity.EnabledMark == 1 && flowTaskParamter.flowTaskEntity.Status == 1))
                throw Oops.Oh(ErrorCode.WF0010);
            //撤回节点下一节点已操作
            var recallNextOperatorList = flowTaskOperatorEntityList.FindAll(x => flowTaskParamter.flowTaskNodeEntity.NodeNext.Contains(x.NodeCode));
            if (recallNextOperatorList.FindAll(x => x.Completion == 1 && x.HandleStatus == 1).Count > 0 || flowTaskParamter.flowTaskNodeEntityList.Any(x => flowTaskParamter.flowTaskNodeEntity.NodeNext.Contains(x.NodeCode) && x.Completion == 1))
                throw Oops.Oh(ErrorCode.WF0010);
            #endregion

            #region 经办修改
            var delOperatorRecordIds = new List<string>();
            //加签人
            var upOperatorList = await flowTaskUserUtil.GetOperatorNew(flowTaskParamter.flowTaskOperatorEntity.Id, new List<FlowTaskOperatorEntity>());
            // 前加签回滚经办撤回
            if (!upOperatorList.Any() && flowTaskParamter.flowTaskOperatorEntity.ParentId.IsNotEmptyOrNull() && flowTaskParamter.flowTaskOperatorEntity.RollbackId.IsNotEmptyOrNull())
            {
                var rollBackOperator = await _flowTaskRepository.GetTaskOperatorInfo(flowTaskParamter.flowTaskOperatorEntity.RollbackId);
                rollBackOperator.State = 1;
                await _flowTaskRepository.UpdateTaskOperator(rollBackOperator);
            }
            flowTaskParamter.flowTaskOperatorEntity.HandleStatus = null;
            flowTaskParamter.flowTaskOperatorEntity.HandleTime = null;
            flowTaskParamter.flowTaskOperatorEntity.Completion = 0;
            flowTaskParamter.flowTaskOperatorEntity.State = 0;
            upOperatorList.Add(flowTaskParamter.flowTaskOperatorEntity);
            // 撤回节点是依次审批
            if (flowTaskParamter.approversProperties.counterSign == 2)
            {
                var operatorUserList = await _flowTaskRepository.GetTaskOperatorUserList(x => x.TaskId == flowTaskParamter.flowTaskOperatorEntity.TaskId && x.TaskNodeId == flowTaskParamter.flowTaskOperatorEntity.TaskNodeId && x.State == 0);
                var nextOperatorUser = operatorUserList.Find(x => x.SortCode == flowTaskParamter.flowTaskOperatorEntity.SortCode + 1);
                if (nextOperatorUser.IsNotEmptyOrNull())
                {
                    if (flowTaskOperatorEntityList.Any(x => x.Id == nextOperatorUser.Id && x.Completion == 1 && x.HandleStatus == 1))
                    {
                        throw Oops.Oh(ErrorCode.WF0010);
                    }
                    else
                    {
                        await _flowTaskRepository.DeleteTaskOperator(new List<string>() { nextOperatorUser.Id });
                    }
                }
            }

            foreach (var item in upOperatorList)
            {
                var operatorRecord = await _flowTaskRepository.GetTaskOperatorRecordList(x => x.TaskId == item.TaskId && x.TaskNodeId == item.TaskNodeId && x.TaskOperatorId == item.Id && x.Status != -1);
                if (operatorRecord.IsNotEmptyOrNull())
                {
                    delOperatorRecordIds = delOperatorRecordIds.Union(operatorRecord.Select(x => x.Id).ToList()).ToList();
                }
            }

            //撤回节点是否完成
            if (flowTaskParamter.flowTaskNodeEntity.Completion == 1)
            {
                //撤回节点下一节点经办删除
                await _flowTaskRepository.DeleteTaskOperator(recallNextOperatorList.Select(x => x.Id).ToList());
                //或签经办全部撤回，会签撤回未处理的经办
                //撤回节点未审批的经办
                var notHanleOperatorList = flowTaskOperatorEntityList.FindAll(x => x.TaskNodeId == flowTaskOperatorRecordEntity.TaskNodeId && x.HandleStatus == null
                 && x.HandleTime == null);
                foreach (var item in notHanleOperatorList)
                {
                    item.Completion = 0;
                }
                upOperatorList = upOperatorList.Union(notHanleOperatorList).ToList();

                #region 更新撤回节点
                flowTaskParamter.flowTaskNodeEntity.Completion = 0;
                await _flowTaskRepository.UpdateTaskNode(flowTaskParamter.flowTaskNodeEntity);
                #endregion

                #region 更新任务流程
                flowTaskParamter.flowTaskEntity.ThisStepId = flowTaskNodeUtil.GetRecallThisStepId(new List<FlowTaskNodeEntity>() { flowTaskParamter.flowTaskNodeEntity }, flowTaskParamter.flowTaskEntity.ThisStepId);
                flowTaskParamter.flowTaskEntity.ThisStep = flowTaskNodeUtil.GetThisStep(flowTaskParamter.flowTaskNodeEntityList, flowTaskParamter.flowTaskEntity.ThisStepId);
                flowTaskParamter.flowTaskEntity.Completion = flowTaskParamter.flowTaskNodeEntity.NodePropertyJson.ToObject<ApproversProperties>().progress.ParseToInt();
                flowTaskParamter.flowTaskEntity.Status = FlowTaskStatusEnum.Handle.ParseToInt();
                await _flowTaskRepository.UpdateTask(flowTaskParamter.flowTaskEntity);
                #endregion
            }
            var flowEngineEntity = _flowTaskRepository.GetFlowTemplateInfo(flowTaskParamter.flowTaskEntity.FlowId);
            var formData = await _runService.GetFlowFormDataDetails(flowTaskParamter.flowTaskNodeEntity.FormId, flowTaskParamter.flowTaskEntity.Id);
            if (flowTaskOperatorRecordEntity.Status == 0)
            {
                await AdjustNodeByCon(flowEngineEntity, formData, flowTaskParamter.flowTaskOperatorEntity, true);
            }

            var userList = upOperatorList.Select(x => x.HandleId).ToList();
            var idList = upOperatorList.Select(x => x.Id).ToList();
            foreach (var item in flowTaskParamter.flowTaskNodeEntityList)
            {
                if (flowTaskParamter.flowTaskNodeEntity.NodeNext.Contains(item.NodeCode))
                {
                    var nextNodeAppro = item.NodePropertyJson.ToObject<ApproversProperties>();
                    // 撤回节点是会签且下一节点是选择分支
                    if (flowTaskParamter.approversProperties.counterSign == 1 && nextNodeAppro.isBranchFlow)
                    {
                        //会签节点最后一个选择候选人，另一个人撤回清除最后一人的候选人
                        _flowTaskRepository.DeleteFlowCandidates(x => x.TaskNodeId == item.Id);
                    }
                    else
                    {
                        _flowTaskRepository.DeleteFlowCandidates(x => x.TaskNodeId == item.Id && userList.Contains(x.HandleId) && idList.Contains(x.TaskOperatorId));
                    }
                    _schedulerFactory.RemoveJob("Job_CS_" + item.Id);
                    _schedulerFactory.RemoveJob("Job_TX_" + item.Id);
                    // 超时提醒
                    await TimeoutOrRemind(flowTaskParamter, item.Id, upOperatorList.FindAll(x => x.TaskNodeId == item.Id));
                }
            }

            await _flowTaskRepository.UpdateTaskOperator(upOperatorList);
            #endregion

            #region 删除经办记录
            delOperatorRecordIds.Add(flowTaskOperatorRecordEntity.Id);
            await _flowTaskRepository.DeleteTaskOperatorRecord(delOperatorRecordIds);
            #endregion

            #region 撤回记录

            await flowTaskOtherUtil.CreateOperatorRecode(flowTaskParamter, 3);
            #endregion

            _db.CommitTran();
            #region 撤回事件
            if (!await flowTaskMsgUtil.RequestEvents(flowTaskParamter.approversProperties.recallFuncConfig, flowTaskParamter, FuncConfigEnum.recall) && flowTaskParamter.startProperties.funcConfigRule == 1)
            {
                await Cancel(flowTaskParamter);
            }
            #endregion
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 流程撤回.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    public async Task Revoke(FlowTaskParamter flowTaskParamter)
    {
        try
        {
            _db.BeginTran();
            #region 撤回数据
            await _flowTaskRepository.DeleteFlowTaskAllData(new List<string>() { flowTaskParamter.flowTaskEntity.Id }, false);
            foreach (var item in await _flowTaskRepository.GetTaskNodeList(x => x.TaskId == flowTaskParamter.flowTaskEntity.Id))
            {
                _schedulerFactory.RemoveJob("Job_CS_" + item.Id);
                _schedulerFactory.RemoveJob("Job_TX_" + item.Id);
            }
            #endregion

            #region 更新实例
            flowTaskParamter.flowTaskEntity.ThisStepId = string.Empty;
            flowTaskParamter.flowTaskEntity.ThisStep = "开始";
            flowTaskParamter.flowTaskEntity.Completion = 0;
            flowTaskParamter.flowTaskEntity.FlowUrgent = 0;
            if (flowTaskParamter.startProperties.revokeRule == 1 || !flowTaskParamter.flowTaskEntity.ParentId.Equals("0"))
            {
                flowTaskParamter.flowTaskEntity.Status = FlowTaskStatusEnum.Revoke.ParseToInt();
            }
            else
            {
                flowTaskParamter.flowTaskEntity.Status = FlowTaskStatusEnum.RevokeDraft.ParseToInt();
            }
            await _flowTaskRepository.UpdateTask(flowTaskParamter.flowTaskEntity);
            #endregion

            #region 撤回记录
            await flowTaskOtherUtil.CreateOperatorRecode(flowTaskParamter, 3);
            #endregion
            _db.CommitTran();

            #region 撤回子流程任务
            var childTask = await _flowTaskRepository.GetTaskList(x => flowTaskParamter.flowTaskEntity.Id == x.ParentId && x.DeleteMark == null);
            foreach (var item in childTask)
            {
                var childFlowTaskParamter = await _flowTaskRepository.GetTaskParamterByTaskId(item.Id, null);
                childFlowTaskParamter.handleOpinion = flowTaskParamter.handleOpinion;
                childFlowTaskParamter.signImg = flowTaskParamter.signImg;
                await this.Revoke(childFlowTaskParamter);
            }
            #endregion

            #region 撤回事件
            if (!await flowTaskMsgUtil.RequestEvents(flowTaskParamter.startProperties.flowRecallFuncConfig, flowTaskParamter, FuncConfigEnum.flowRecall) && flowTaskParamter.startProperties.funcConfigRule == 1)
            {
                await Cancel(flowTaskParamter);
            }
            #endregion
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 终止.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    public async Task Cancel(FlowTaskParamter flowTaskParamter)
    {
        try
        {
            _db.BeginTran();
            #region 更新实例
            await flowTaskOtherUtil.CancelTask(flowTaskParamter.flowTaskEntity);
            #endregion

            #region 作废记录
            await flowTaskOtherUtil.CreateOperatorRecode(flowTaskParamter, 4);
            #endregion
            _db.CommitTran();
            //结束
            var bodyDic = flowTaskMsgUtil.GetMesBodyText(flowTaskParamter, new List<string>() { flowTaskParamter.flowTaskEntity.CreatorUserId }, null, 1);
            await flowTaskMsgUtil.Alerts(flowTaskParamter.startProperties.endMsgConfig, new List<string>() { flowTaskParamter.flowTaskEntity.CreatorUserId }, flowTaskParamter, "MBXTLC010", bodyDic);
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 指派.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    public async Task Assigned(FlowTaskParamter flowTaskParamter)
    {
        try
        {
            _db.BeginTran();
            var OperatorIds = flowTaskParamter.thisFlowTaskOperatorEntityList.Select(x => x.Id).ToList();
            await _flowTaskRepository.DeleteTaskOperator(OperatorIds);
            await _flowTaskRepository.DeleteTaskOperatorUser(x => OperatorIds.Contains(x.Id));
            var OperatorRecordIds = (await _flowTaskRepository.GetTaskOperatorRecordList(x => x.TaskId == flowTaskParamter.flowTaskEntity.Id && OperatorIds.Contains(x.TaskOperatorId))).Select(x => x.Id).ToList();
            await _flowTaskRepository.DeleteTaskOperatorRecord(OperatorRecordIds);
            flowTaskParamter.flowTaskOperatorEntity = flowTaskParamter.thisFlowTaskOperatorEntityList.FirstOrDefault().Copy();
            flowTaskParamter.flowTaskOperatorEntity.Id = SnowflakeIdHelper.NextId();
            flowTaskParamter.flowTaskOperatorEntity.HandleId = flowTaskParamter.freeApproverUserId;
            flowTaskParamter.flowTaskOperatorEntity.Completion = 0;
            flowTaskParamter.flowTaskOperatorEntity.State = 0;

            var isOk = await _flowTaskRepository.CreateTaskOperator(flowTaskParamter.flowTaskOperatorEntity);
            if (!isOk)
                throw Oops.Oh(ErrorCode.WF0008);

            #region 流转记录
            await flowTaskOtherUtil.CreateOperatorRecode(flowTaskParamter, 5);
            #endregion
            _db.CommitTran();

            //_schedulerFactory.RemoveJob("Job_CS_" + flowTaskParamter.flowTaskOperatorEntity.TaskNodeId);
            //_schedulerFactory.RemoveJob("Job_TX_" + flowTaskParamter.flowTaskOperatorEntity.TaskNodeId);
            var userList = new List<string>() { flowTaskParamter.freeApproverUserId };
            var bodyDic = flowTaskMsgUtil.GetMesBodyText(flowTaskParamter, userList, new List<FlowTaskOperatorEntity>() { flowTaskParamter.flowTaskOperatorEntity }, 2);
            await flowTaskMsgUtil.Alerts(flowTaskParamter.startProperties.waitMsgConfig, bodyDic.Keys.ToList(), flowTaskParamter, "MBXTLC001", bodyDic);
            // 超时提醒
            //await TimeoutOrRemind(flowTaskParamter, flowTaskParamter.flowTaskOperatorEntity.TaskNodeId, new List<FlowTaskOperatorEntity>() { flowTaskParamter.flowTaskOperatorEntity });
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 转办.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    public async Task Transfer(FlowTaskParamter flowTaskParamter)
    {
        try
        {
            _db.BeginTran();
            if (flowTaskParamter.flowTaskOperatorEntity == null)
                throw Oops.Oh(ErrorCode.COM1005);
            flowTaskParamter.flowTaskOperatorEntity.HandleId = flowTaskParamter.freeApproverUserId;
            flowTaskParamter.flowTaskOperatorEntity.CreatorTime = DateTime.Now;
            var isOk = await _flowTaskRepository.UpdateTaskOperator(flowTaskParamter.flowTaskOperatorEntity);
            if (!isOk)
                throw Oops.Oh(ErrorCode.WF0007);

            #region 流转记录
            await flowTaskOtherUtil.CreateOperatorRecode(flowTaskParamter, 7);
            #endregion
            _db.CommitTran();

            var userList = new List<string>() { flowTaskParamter.freeApproverUserId };
            var bodyDic = flowTaskMsgUtil.GetMesBodyText(flowTaskParamter, userList, new List<FlowTaskOperatorEntity>() { flowTaskParamter.flowTaskOperatorEntity }, 2);
            await flowTaskMsgUtil.Alerts(flowTaskParamter.startProperties.waitMsgConfig, bodyDic.Keys.ToList(), flowTaskParamter, "MBXTLC001", bodyDic);
            // 超时提醒
            //await TimeoutOrRemind(flowTaskParamter, flowTaskParamter.flowTaskOperatorEntity.TaskNodeId, new List<FlowTaskOperatorEntity>() { flowTaskParamter.flowTaskOperatorEntity });

            #region 自动审批
            await AutoAudit(flowTaskParamter);
            #endregion
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 催办.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    public async Task Press(FlowTaskParamter flowTaskParamter)
    {
        try
        {
            _db.BeginTran();

            var flowTaskOperatorEntityList = await _flowTaskRepository.GetTaskOperatorList(x => x.TaskId == flowTaskParamter.flowTaskEntity.Id && x.Completion == 0 && x.State == 0);
            if (flowTaskOperatorEntityList.Any(x => x.HandleId.IsNullOrEmpty()))
                throw Oops.Oh(ErrorCode.WF0009);
            _db.CommitTran();

            var bodyDic = new Dictionary<string, object>();
            var messageDic = flowTaskOtherUtil.GroupByOperator(flowTaskOperatorEntityList);
            foreach (var item in messageDic.Keys)
            {
                var userList = messageDic[item].Select(x => x.HandleId).ToList();
                bodyDic = flowTaskMsgUtil.GetMesBodyText(flowTaskParamter, userList, messageDic[item], 2);
                await flowTaskMsgUtil.Alerts(flowTaskParamter.startProperties.waitMsgConfig, bodyDic.Keys.ToList(), flowTaskParamter, "MBXTLC001", bodyDic);
            }
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 获取候选人.
    /// </summary>
    /// <param name="id">经办id.</param>
    /// <param name="flowHandleModel">审批参数.</param>
    /// <param name="type">0:候选节点编码，1：候选人.</param>
    /// <returns></returns>
    public async Task<dynamic> GetCandidateModelList(string id, FlowHandleModel flowHandleModel, int type = 0)
    {
        var output = new List<FlowTaskCandidateModel>();
        //所有节点
        List<FlowTaskNodeEntity> flowTaskNodeEntityList = new List<FlowTaskNodeEntity>();
        //下个节点集合
        List<FlowTaskNodeEntity> nextNodeEntityList = new List<FlowTaskNodeEntity>();
        //指定下个节点
        FlowTaskNodeEntity nextNodeEntity = new FlowTaskNodeEntity();
        var flowEngineEntity = _flowTaskRepository.GetFlowTemplateInfo(flowHandleModel.flowId);
        var formData = flowHandleModel.formData;
        // 是否达到会签比例
        var isCom = true;
        var nodeNext = string.Empty;
        var thisNodeCode = id == "0" ? flowEngineEntity.flowTemplateJson.ToObject<FlowTemplateJsonModel>().nodeId : (await _flowTaskRepository.GetTaskOperatorInfo(id))?.NodeCode;
        flowTemplateUtil.Load(flowEngineEntity, formData.ToJsonString(), string.Empty, true, thisNodeCode);
        var isSwerve = false;
        if (id == "0")
        {
            //所有节点
            flowTaskNodeEntityList = flowTemplateUtil.flowTaskNodeEntityList;
            nodeNext = flowTemplateUtil.startPro.swerveNode.IsNotEmptyOrNull() ? flowTemplateUtil.startPro.swerveNode : flowTemplateUtil.startNode.NodeNext;
            nextNodeEntityList = flowTaskNodeEntityList.FindAll(m => nodeNext.Contains(m.NodeCode));
        }
        else
        {
            var flowTaskOperator = await _flowTaskRepository.GetTaskOperatorInfo(id);
            if (flowTaskOperator.ParentId.IsNotEmptyOrNull() && type == 0) return output;// 加签不弹窗
            // 选择分支：初始审批人前加签不算审批 后加签算审批
            var flowTaskOperatorList = await _flowTaskRepository.GetTaskOperatorList(x => x.TaskId == flowTaskOperator.TaskId && x.TaskNodeId == flowTaskOperator.TaskNodeId && x.State == 0 && (SqlFunc.IsNullOrEmpty(x.ParentId) || !SqlFunc.IsNullOrEmpty(x.RollbackId)));
            var flowTaskNodeEntity = await _flowTaskRepository.GetTaskNodeInfo(flowTaskOperator.TaskNodeId);
            flowTaskNodeEntityList = await _flowTaskRepository.GetTaskNodeList(x => x.State == 0 && x.TaskId == flowTaskOperator.TaskId);
            var newFlowTaskNodeEntity = flowTemplateUtil.flowTaskNodeEntityList.Find(x => x.NodeCode == flowTaskNodeEntity.NodeCode);
            var newNodePro = newFlowTaskNodeEntity.NodePropertyJson.ToObject<ApproversProperties>();
            isSwerve = newNodePro.swerveNode.IsNotEmptyOrNull();
            nodeNext = isSwerve ? newNodePro.swerveNode : flowTaskNodeEntity.NodeNext;

            #region 审批驳回撤回还原选择分支父节点下一节点数据
            if (flowTemplateUtil.flowTaskNodeEntityList.Any(m => flowTaskNodeEntity.NodeNext.Contains(m.NodeCode) && m.NodePropertyJson.ToObject<ApproversProperties>().isBranchFlow))
            {
                nodeNext = newFlowTaskNodeEntity.NodeNext;
            }
            #endregion
            nextNodeEntityList = flowTaskNodeEntityList.FindAll(m => nodeNext.Contains(m.NodeCode));
            if (flowTaskNodeEntity.NodePropertyJson.ToObject<ApproversProperties>().counterSign != 0)
            {
                var flowTaskParamter = new FlowTaskParamter()
                {
                    thisFlowTaskOperatorEntityList = flowTaskOperatorList,
                    flowTaskNodeEntity = flowTaskNodeEntity,
                    approversProperties = flowTaskNodeEntity.NodePropertyJson.ToObject<ApproversProperties>(),
                    flowTaskOperatorEntity = flowTaskOperator,
                };
                if (flowTaskNodeEntity.NodePropertyJson.ToObject<ApproversProperties>().counterSign == 2)
                {
                    var operatorUserEntities = (await _flowTaskRepository.GetTaskOperatorUserList(x => x.TaskId == flowTaskParamter.flowTaskNodeEntity.TaskId && x.TaskNodeId == flowTaskParamter.flowTaskNodeEntity.Id && x.State == 0)).Adapt<List<FlowTaskOperatorEntity>>();
                    // 取比当前节点审批人排序码大的与所有审批人员差集再加上当前节点审批人
                    flowTaskParamter.thisFlowTaskOperatorEntityList = operatorUserEntities.Where(x => x.SortCode > flowTaskParamter.flowTaskOperatorEntity.SortCode).Union(flowTaskParamter.thisFlowTaskOperatorEntityList).ToList();
                }
                isCom = flowTaskOtherUtil.IsAchievebilProportion(flowTaskParamter, 1);
            }
        }
        nextNodeEntity = flowTaskNodeEntityList.Find(x => x.NodeCode.Equals(flowHandleModel.nodeCode));
        if (type == 1)
        {
            return flowTaskUserUtil.GetCandidateItems(nextNodeEntity, flowHandleModel);
        }
        await flowTaskUserUtil.GetCandidates(output, nextNodeEntityList, flowTaskNodeEntityList, isSwerve);
        // 弹窗类型 1:条件分支弹窗(包含候选人) 2:候选人弹窗 3:无弹窗
        var branchType = output.Count > 0 ? (output.Any(x => x.isBranchFlow) ? 1 : 2) : 3;
        // 无弹窗：1.条件分支且未达到会签比例，2.任务冻结驳回
        if ((!isCom && branchType == 1) || await _flowTaskRepository.AnyFlowTask(x => x.Id == flowHandleModel.id && !SqlFunc.IsNullOrEmpty(x.RejectDataId)))
        {
            branchType = 3;
        }
        return new { list = output, type = branchType, countersignOver = isCom };

    }

    /// <summary>
    /// 批量审批节点列表.
    /// </summary>
    /// <param name="flowId">流程id.</param>
    /// <returns></returns>
    public async Task<dynamic> NodeSelector(string flowId)
    {
        var flowJsonModel = _flowTaskRepository.GetFlowTemplateInfo(flowId);
        flowTemplateUtil.Load(flowJsonModel, null, string.Empty, false);
        return flowTemplateUtil.taskNodeList.FindAll(x => FlowTaskNodeTypeEnum.approver.ParseToString().Equals(x.type)).Select(x => new { id = x.nodeId, fullName = x.propertyJson.title }).ToList();
    }

    /// <summary>
    /// 获取批量审批候选人.
    /// </summary>
    /// <param name="flowId">流程id.</param>
    /// <param name="flowTaskOperatorId">经办id.</param>
    /// <returns></returns>
    public async Task<dynamic> GetBatchCandidate(string flowId, string flowTaskOperatorId)
    {
        //所有节点
        var flowEngineEntity = _flowTaskRepository.GetFlowTemplateInfo(flowId);
        flowTemplateUtil.Load(flowEngineEntity, null, string.Empty, false);
        var taskNodeList = flowTemplateUtil.flowTaskNodeEntityList;
        var flowTaskOperatorEntity = await _flowTaskRepository.GetTaskOperatorInfo(flowTaskOperatorId);
        // 判断当前节点下节点是否属于条件之下
        var flag1 = flowTemplateUtil.taskNodeList.Any(x => x.upNodeId == flowTaskOperatorEntity.NodeCode && FlowTaskNodeTypeEnum.condition.ParseToString().Equals(x.type));
        // 判断当前节点下节点是否包含候选人节点
        var flag2 = flowTemplateUtil.taskNodeList.Any(x => x.upNodeId == flowTaskOperatorEntity.NodeCode && FlowTaskNodeTypeEnum.approver.ParseToString().Equals(x.type) && x.propertyJson.assigneeType == 7);
        // 判断当前节点下节点是否包含选择分支节点
        var flag3 = flowTemplateUtil.taskNodeList.Any(x => x.upNodeId == flowTaskOperatorEntity.NodeCode && x.isBranchFlow);
        if (flag1 || flag2 || flag3) throw Oops.Oh(ErrorCode.WF0022);
        var model = new FlowHandleModel
        {
            nodeCode = flowTaskOperatorEntity.NodeCode,
            flowId = flowId,
            formData = new { flowId = flowId, data = "{}", id = flowTaskOperatorEntity.TaskId }
        };
        return await GetCandidateModelList(flowTaskOperatorId, model);
    }

    /// <summary>
    /// 审批根据条件变更节点.
    /// </summary>
    /// <param name="flowEngineEntity">流程实例.</param>
    /// <param name="formData">表单数据.</param>
    /// <param name="flowTaskOperatorEntity">经办实例.</param>
    /// <returns></returns>
    public async Task AdjustNodeByCon(FlowJsonModel flowEngineEntity, object formData, FlowTaskOperatorEntity flowTaskOperatorEntity, bool isBranchFlow = false)
    {
        var data = formData.ToJsonString();
        flowTemplateUtil.Load(flowEngineEntity, data, flowTaskOperatorEntity.TaskId, false);
        var flag1 = flowTemplateUtil.taskNodeList.Any(x => x.upNodeId == flowTaskOperatorEntity.NodeCode && x.isBranchFlow);
        var flag2 = flowTemplateUtil.taskNodeList.Any(x => x.upNodeId == flowTaskOperatorEntity.NodeCode && FlowTaskNodeTypeEnum.condition.ParseToString().Equals(x.type));
        if (flag1 || flag2)
        {
            var oldNodeList = await _flowTaskRepository.GetTaskNodeList(x => x.TaskId == flowTaskOperatorEntity.TaskId && x.State != -2);
            var newNodeList = new List<FlowTaskNodeEntity>();
            // 获取新的节点数据
            flowTemplateUtil.Load(flowEngineEntity, data, flowTaskOperatorEntity.TaskId);
            // 替换原有id
            flowTemplateUtil.flowTaskNodeEntityList.Where(x => oldNodeList.Select(y => y.NodeCode).Contains(x.NodeCode)).ToList().ForEach(item =>
            {
                item.Id = oldNodeList.FirstOrDefault(x => x.NodeCode == item.NodeCode).Id;
            });

            var flowNodeList = flowTemplateUtil.flowTaskNodeEntityList;

            // 当前节点以下节点数据
            var nodeList = new List<FlowTaskNodeEntity>();
            if (flowTaskOperatorEntity.TaskNodeId.IsNotEmptyOrNull())
            {
                nodeList.Add(flowNodeList.Find(x => x.NodeCode == flowTaskOperatorEntity.NodeCode));
            }
            flowTaskNodeUtil.RecursiveNode(flowNodeList, flowTaskOperatorEntity.NodeCode, nodeList);


            foreach (var item in oldNodeList)
            {
                var node = nodeList.FirstOrDefault(x => x.Id == item.Id);
                if (node.IsNotEmptyOrNull())
                {
                    newNodeList.Add(node);
                }
                else
                {
                    newNodeList.Add(item);
                }
            }

            newNodeList.ForEach(x =>
            {
                x.SortCode = null;
                x.State = 1;
            });
            newNodeList.Remove(flowNodeList.FirstOrDefault(x => x.NodeCode == "end"));
            flowTemplateUtil.UpdateNodeSort(newNodeList);
            await flowTaskOtherUtil.SaveFormOperates(newNodeList, false);
            await _flowTaskRepository.UpdateTaskNode(newNodeList);
        }
    }

    /// <summary>
    /// 判断驳回节点是否存在子流程.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    public bool IsSubFlowUpNode(FlowTaskParamter flowTaskParamter)
    {
        var rejectNodeList = flowTaskNodeUtil.GetRejectNode(flowTaskParamter);
        return rejectNodeList.Any(x => FlowTaskNodeTypeEnum.subFlow.ParseToString().Equals(x.NodeType));
    }

    /// <summary>
    /// 获取批量任务的表单数据.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    public async Task<object> GetBatchOperationData(FlowTaskParamter flowTaskParamter)
    {
        return await _runService.GetFlowFormDataDetails(flowTaskParamter.flowTaskNodeEntity.FormId, flowTaskParamter.flowTaskEntity.Id);
    }

    /// <summary>
    /// 详情操作验证.
    /// </summary>
    /// <param name="taskOperatorId">经办id.</param>
    /// <param name="flowHandleModel">审批参数.</param>
    /// <returns></returns>
    public async Task<FlowTaskParamter> Validation(string taskOperatorId, FlowHandleModel flowHandleModel)
    {
        var flowTaskParamter = await _flowTaskRepository.GetTaskParamterByOperatorId(taskOperatorId, flowHandleModel);
        if (flowTaskParamter.flowTaskEntity.Suspend == 1) throw Oops.Oh(ErrorCode.WF0046);
        if (flowTaskParamter.flowTaskOperatorEntity.IsNullOrEmpty() || flowTaskParamter.flowTaskOperatorEntity.Completion != 0 || flowTaskParamter.flowTaskOperatorEntity.State == -1)
            throw Oops.Oh(ErrorCode.WF0030);
        if (flowTaskParamter.flowTaskEntity.IsNullOrEmpty() || flowTaskParamter.flowTaskEntity.Status != 1 || flowTaskParamter.flowTaskEntity.DeleteMark.IsNotEmptyOrNull())
            throw Oops.Oh(ErrorCode.WF0030);
        if (flowTaskParamter.flowTaskOperatorEntity.HandleId != _userManager.UserId)
        {
            var toUserId = _flowTaskRepository.GetToUserId(flowTaskParamter.flowTaskOperatorEntity.HandleId, flowTaskParamter.flowTaskEntity.TemplateId);
            if (!toUserId.Contains(_userManager.UserId))
                throw Oops.Oh(ErrorCode.WF0030);
        }
        flowTaskParamter.flowTaskEntity.FlowFormContentJson = flowHandleModel.formData.ToJsonStringOld();
        return flowTaskParamter;
    }

    /// <summary>
    /// 变更/复活.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    public async Task<dynamic> Change(FlowTaskParamter flowTaskParamter)
    {
        var flowEngineEntity = _flowTaskRepository.GetFlowTemplateInfo(flowTaskParamter.flowTaskEntity.FlowId);
        try
        {
            _db.BeginTran();
            // 当前待办节点是子流程不允许操作
            if (flowTaskParamter.flowTaskNodeEntityList.Any(x => flowTaskParamter.flowTaskEntity.ThisStepId.Contains(x.NodeCode) && FlowTaskNodeTypeEnum.subFlow.ParseToString().Equals(x.NodeType)))
                throw Oops.Oh(ErrorCode.WF0036);

            if (flowTaskParamter.flowTaskEntity.Status != FlowTaskStatusEnum.Adopt.ParseToInt() && flowTaskParamter.resurgence)
                throw Oops.Oh(ErrorCode.WF0034);
            // 变更节点
            var changeNode = flowTaskParamter.flowTaskNodeEntityList.Find(x => x.Id == flowTaskParamter.taskNodeId);
            if (changeNode.DraftData.IsNullOrEmpty() && flowTaskParamter.resurgence && flowTaskParamter.startProperties.resurgenceDataRule == 1)
                throw Oops.Oh(ErrorCode.WF0034);
            // 递归获取变更节点下级所有节点
            var changeNodeNextList = new List<FlowTaskNodeEntity>();
            await flowTaskNodeUtil.RecursiveNode(flowTaskParamter.flowTaskNodeEntityList, changeNode.NodeCode, changeNodeNextList);
            changeNodeNextList.Add(changeNode);
            var changeNodeNextIds = changeNodeNextList.Select(x => x.Id).ToList();
            // 将非变更节点以及其下级节点全部已完成
            foreach (var item in flowTaskParamter.flowTaskNodeEntityList)
            {
                item.Completion = changeNodeNextIds.Contains(item.Id) ? 0 : 1;
                if (item.Id == changeNode.Id && flowTaskParamter.resurgence && flowTaskParamter.startProperties.resurgenceDataRule == 1)
                {
                    var dataDic = changeNode.DraftData.ToObject<Dictionary<string, object>>();
                    dataDic["jnpf_resurgence"] = true;
                    item.DraftData = dataDic.ToJsonString();
                }
            }
            //变更节点经办数据
            var flowTaskOperatorEntityList = new List<FlowTaskOperatorEntity>();
            List<FlowTaskCandidateModel> flowTaskCandidateModels = new List<FlowTaskCandidateModel>();
            var errUser = new List<string>();
            if (flowTaskParamter.errorRuleUserList.IsNotEmptyOrNull() && flowTaskParamter.errorRuleUserList.ContainsKey(changeNode.NodeCode))
            {
                errUser = flowTaskParamter.errorRuleUserList[changeNode.NodeCode];
            }
            flowTaskParamter.flowTaskNodeEntity = changeNode;
            flowTaskParamter.approversProperties = changeNode.Adapt<ApproversProperties>();
            //最后经办节点数据
            var lastOperatorRecored = (await _flowTaskRepository.GetTaskOperatorRecordList(changeNode.TaskId)).Where(x => x.HandleStatus == 1 || x.HandleStatus == 2).LastOrDefault();
            if (lastOperatorRecored.IsNotEmptyOrNull() && !flowTaskParamter.resurgence)
            {
                var lastNode = await _flowTaskRepository.GetTaskNodeInfo(lastOperatorRecored.TaskNodeId);
                if (lastNode.IsNotEmptyOrNull() && lastNode.FormId == changeNode.FormId)
                {
                    flowTaskParamter.formData = await _runService.GetFlowFormDataDetails(lastNode.FormId, lastNode.TaskId);
                }
                else
                {
                    throw Oops.Oh(ErrorCode.WF0044);
                }
            }
            // 复活表单数据
            if (flowTaskParamter.resurgence)
            {
                flowTaskParamter.formData = await _runService.GetFlowFormDataDetails(changeNode.FormId, changeNode.TaskId);
            }
            await flowTaskUserUtil.AddFlowTaskOperatorEntityByAssigneeType(flowTaskParamter, changeNode, 3);
            if (flowTaskParamter.errorNodeList.Any())
            {
                _db.RollbackTran();
                return flowTaskParamter.errorNodeList;
            }

            await _flowTaskRepository.DeleteTaskOperatorRecord(x => x.TaskId == changeNode.TaskId && changeNodeNextIds.Contains(x.TaskNodeId));
            foreach (var item in await _flowTaskRepository.GetTaskNodeList(x => x.TaskId == flowTaskParamter.flowTaskEntity.Id))
            {
                _schedulerFactory.RemoveJob("Job_CS_" + item.Id);
                _schedulerFactory.RemoveJob("Job_TX_" + item.Id);
            }
            var oldOperatorList = await _flowTaskRepository.GetTaskOperatorList(x => x.TaskId == changeNode.TaskId);
            foreach (var item in oldOperatorList)
            {
                item.State = -1;
            }
            await _flowTaskRepository.UpdateTaskOperator(oldOperatorList);
            await _flowTaskRepository.UpdateTaskOperatorUser(oldOperatorList.Adapt<List<FlowTaskOperatorUserEntity>>());
            if (changeNode.NodePropertyJson.ToObject<ApproversProperties>().counterSign == 2)
            {
                await _flowTaskRepository.CreateTaskOperator(flowTaskParamter.flowTaskOperatorEntityList.FirstOrDefault());
                flowTaskOperatorEntityList = new List<FlowTaskOperatorEntity> { flowTaskParamter.flowTaskOperatorEntityList.FirstOrDefault() };
            }
            else
            {
                await _flowTaskRepository.CreateTaskOperator(flowTaskParamter.flowTaskOperatorEntityList);
                flowTaskOperatorEntityList = flowTaskParamter.flowTaskOperatorEntityList;
            }
            await _flowTaskRepository.UpdateTaskNode(flowTaskParamter.flowTaskNodeEntityList);
            //await AdjustNodeByCon(flowEngineEntity, flowTaskParamter.flowTaskEntity.FlowFormContentJson, new FlowTaskOperatorEntity { TaskId = changeNode.TaskId, NodeCode = changeNode.NodeCode, TaskNodeId = changeNode.Id }, true);
            await AdjustNodeByCon(flowEngineEntity, flowTaskParamter.formData, new FlowTaskOperatorEntity { TaskId = changeNode.TaskId, NodeCode = changeNode.NodeCode, TaskNodeId = changeNode.Id }, true);

            #region 更新流程任务
            flowTaskParamter.flowTaskEntity.ThisStepId = changeNode.NodeCode;
            flowTaskParamter.flowTaskEntity.ThisStep = changeNode.NodeName;
            if (flowTaskParamter.resurgence)
            {
                flowTaskParamter.flowTaskEntity.Completion = changeNode.NodePropertyJson.ToObject<ApproversProperties>().progress.ParseToInt();
                flowTaskParamter.flowTaskEntity.Status = FlowTaskStatusEnum.Handle.ParseToInt();
                flowTaskParamter.flowTaskEntity.ParentId = "0";
                flowTaskParamter.flowTaskEntity.IsAsync = 0;
                flowTaskParamter.flowTaskEntity.ReviveNodeId = changeNode.Id;
            }
            await _flowTaskRepository.UpdateTask(flowTaskParamter.flowTaskEntity);
            #endregion

            #region 流程经办记录
            var handleStatus = flowTaskParamter.resurgence ? 9 : 8;
            await flowTaskOtherUtil.CreateOperatorRecode(flowTaskParamter, handleStatus);
            #endregion
            _db.CommitTran();

            #region 消息提醒
            var messageDic = flowTaskOtherUtil.GroupByOperator(flowTaskOperatorEntityList);
            //审批
            foreach (var item in messageDic.Keys)
            {
                var userList = messageDic[item].Select(x => x.HandleId).ToList();
                var bodyDic = flowTaskMsgUtil.GetMesBodyText(flowTaskParamter, userList, flowTaskOperatorEntityList, 2);
                await flowTaskMsgUtil.Alerts(flowTaskParamter.startProperties.waitMsgConfig, bodyDic.Keys.ToList(), flowTaskParamter, "MBXTLC001", bodyDic);
                await TimeoutOrRemind(flowTaskParamter, item, messageDic[item]);// 超时提醒
            }
            #endregion
            return flowTaskCandidateModels;
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 驳回审批节点列表.
    /// </summary>
    /// <param name="taskOperatorId">经办id.</param>
    /// <returns></returns>
    public async Task<dynamic> RejectNodeList(string taskOperatorId)
    {
        var flowTaskParamter = await _flowTaskRepository.GetTaskParamterByOperatorId(taskOperatorId, null);
        switch (flowTaskParamter.approversProperties.rejectStep)
        {
            case "0":
                var upNodeCodeList0 = new List<object>() { new { id = "0", nodeCode = flowTaskParamter.flowTaskNodeEntityList.FirstOrDefault().NodeCode, nodeName = "流程发起" } };
                return new { list = upNodeCodeList0, isLastAppro = true };
            case "1":
                var upNodeCodeList1 = flowTaskParamter.flowTaskNodeEntityList.FindAll(x => !x.NodeCode.Equals("end") && x.NodeNext.Contains(flowTaskParamter.flowTaskNodeEntity.NodeCode)).Select(x => x.NodeCode).ToList();
                return new { list = new List<object>() { new { id = "0", nodeCode = string.Join(",", upNodeCodeList1), nodeName = "上级审批节点" } }, isLastAppro = true };
            case "2":
                var upNodeList = new List<FlowTaskNodeEntity>();
                await flowTaskNodeUtil.RecursiveNode(flowTaskParamter.flowTaskNodeEntityList, flowTaskParamter.flowTaskNodeEntity.NodeCode, upNodeList, true);
                var isLastAppro = flowTaskParamter.approversProperties.counterSign == 0 || flowTaskOtherUtil.IsAchievebilProportion(flowTaskParamter, 0);
                var upNodeCodeList2 = upNodeList.FindAll(x => !FlowTaskNodeTypeEnum.subFlow.ParseToString().Equals(x.NodeType)).Select(x => new { id = x.Id, nodeCode = x.NodeCode, nodeName = x.NodeName }).ToList();
                return new { list = upNodeCodeList2, isLastAppro = isLastAppro };
            default:
                var upNodeCode = await _flowTaskRepository.GetTaskNodeInfo(x => x.NodeCode == flowTaskParamter.approversProperties.rejectStep && x.TaskId == flowTaskParamter.flowTaskEntity.Id);
                if (upNodeCode.State != 0) throw Oops.Oh(ErrorCode.WF0054);
                var upNodeCodeList = new List<object>() { new { id = "0", nodeCode = flowTaskParamter.approversProperties.rejectStep, nodeName = upNodeCode.NodeName } };
                return new { list = upNodeCodeList, isLastAppro = true };
        }
    }

    /// <summary>
    /// 挂起.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    public async Task Suspend(FlowTaskParamter flowTaskParamter)
    {
        flowTaskParamter.flowTaskEntity.Suspend = 1;
        await _flowTaskRepository.UpdateTask(flowTaskParamter.flowTaskEntity);

        #region 流转记录
        await flowTaskOtherUtil.CreateOperatorRecode(flowTaskParamter, 11);
        #endregion

        if (!flowTaskParamter.suspend)
        {
            var childTask = await _flowTaskRepository.GetTaskList(x => flowTaskParamter.flowTaskEntity.Id == x.ParentId && x.DeleteMark == null);
            foreach (var item in childTask)
            {
                var childFlowTaskParamter = await _flowTaskRepository.GetTaskParamterByTaskId(item.Id, null);
                childFlowTaskParamter.flowTaskEntity.Restore = 1;
                childFlowTaskParamter.handleOpinion = flowTaskParamter.handleOpinion;
                childFlowTaskParamter.signImg = flowTaskParamter.signImg;
                childFlowTaskParamter.suspend = flowTaskParamter.suspend;
                childFlowTaskParamter.fileList = flowTaskParamter.fileList;
                await this.Suspend(childFlowTaskParamter);
            }
        }
    }

    /// <summary>
    /// 恢复.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    public async Task Restore(FlowTaskParamter flowTaskParamter)
    {
        flowTaskParamter.flowTaskEntity.Suspend = null;
        flowTaskParamter.flowTaskEntity.Restore = null;
        await _flowTaskRepository.UpdateTask(flowTaskParamter.flowTaskEntity);

        #region 流转记录
        await flowTaskOtherUtil.CreateOperatorRecode(flowTaskParamter, 12);
        #endregion

        var childTask = await _flowTaskRepository.GetTaskList(x => flowTaskParamter.flowTaskEntity.Id == x.ParentId && x.DeleteMark == null);
        foreach (var item in childTask)
        {
            if (item.Suspend == 1 && item.Restore == 1)
            {
                var childFlowTaskParamter = await _flowTaskRepository.GetTaskParamterByTaskId(item.Id, null);
                childFlowTaskParamter.handleOpinion = flowTaskParamter.handleOpinion;
                childFlowTaskParamter.signImg = flowTaskParamter.signImg;
                childFlowTaskParamter.fileList = flowTaskParamter.fileList;
                await this.Restore(childFlowTaskParamter);
            }
        }
    }

    /// <summary>
    /// 转向.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    public async Task<dynamic> Swerve(FlowTaskParamter flowTaskParamter)
    {
        // 清除转向节点候选人
        if (flowTaskParamter.candidateList.IsNotEmptyOrNull())
        {
            var nodeIds = flowTaskParamter.flowTaskNodeEntityList.FindAll(x => flowTaskParamter.candidateList.Keys.Contains(x.NodeCode)).Select(x => x.Id).ToList();
            _flowTaskRepository.DeleteFlowCandidates(x => nodeIds.Contains(x.TaskNodeId));
        }
        var candidates = flowTaskParamter.flowTaskEntity.RejectDataId.IsNotEmptyOrNull() ? new List<FlowCandidatesEntity>() : flowTaskOtherUtil.SaveNodeCandidates(flowTaskParamter);
        try
        {
            _db.BeginTran();
            if (flowTaskParamter.flowTaskEntity.RejectDataId.IsNotEmptyOrNull()) throw Oops.Oh(ErrorCode.WF0045);
            // 审批修改表单数据.
            if (flowTaskParamter.flowTaskOperatorEntity.Id.IsNotEmptyOrNull() && FlowTaskNodeTypeEnum.approver.ParseToString().Equals(flowTaskParamter.flowTaskNodeEntity.NodeType))
            {
                var fEntity = _flowTaskRepository.GetFlowFromEntity(flowTaskParamter.flowTaskNodeEntity.FormId);
                List<FormOperatesModel> formOperates = await _flowTaskRepository.GetFlowFormAuthorize(flowTaskParamter.flowTaskNodeEntity);
                var systemControlList = formOperates.Where(x => !x.write && x.read).Select(x => x.id).ToList();
                await _runService.SaveFlowFormData(fEntity, flowTaskParamter.formData.ToJsonString(), flowTaskParamter.flowTaskEntity.Id, flowTaskParamter.flowTaskEntity.FlowId, true, systemControlList);
            }
            var flowJsonEntity = _flowTaskRepository.GetFlowTemplateInfo(flowTaskParamter.flowTaskEntity.FlowId);

            #region 自定义抄送
            await flowTaskUserUtil.GetflowTaskCirculateEntityList(flowTaskParamter, 1);
            await _flowTaskRepository.CreateTaskCirculate(flowTaskParamter.flowTaskCirculateEntityList);
            #endregion

            #region 更新驳回经办
            var nextNodeEntityList = flowTaskParamter.flowTaskNodeEntityList.FindAll(x => flowTaskParamter.rejectStep.Contains(x.NodeCode));
            var isShuntNodeCompletion = flowTaskNodeUtil.IsShuntNodeCompletion(flowTaskParamter.flowTaskNodeEntityList, nextNodeEntityList.FirstOrDefault().NodeCode, flowTaskParamter.flowTaskNodeEntity);
            await flowTaskUserUtil.AddFlowTaskOperatorEntityByAssigneeType(flowTaskParamter, nextNodeEntityList.FirstOrDefault(), 1, isShuntNodeCompletion);
            // 判断是否插入下个节点数据
            await NextOperatorManager(flowTaskParamter, nextNodeEntityList, true, 0, 0);
            if (flowTaskParamter.errorNodeList.Count > 0)
            {
                _db.RollbackTran();
                return flowTaskParamter.errorNodeList;
            }
            #endregion

            #region 更新流程任务
            flowTaskParamter.flowTaskEntity.ReviveNodeId = null;
            if (flowTaskParamter.flowTaskEntity.Status == FlowTaskStatusEnum.Reject.ParseToInt())
            {
                flowTaskParamter.flowTaskEntity.Status = FlowTaskStatusEnum.Draft.ParseToInt();
                await _flowTaskRepository.UpdateTask(flowTaskParamter.flowTaskEntity);
                await _flowTaskRepository.DeleteFlowTaskAllData(flowTaskParamter.flowTaskEntity.Id, false, true);
            }
            else
            {
                await _flowTaskRepository.UpdateTask(flowTaskParamter.flowTaskEntity);
                await _flowTaskRepository.CreateTaskOperator(flowTaskParamter.flowTaskOperatorEntityList);
                foreach (var item in flowTaskParamter.flowTaskOperatorEntityList)
                {
                    await AdjustNodeByCon(flowJsonEntity, flowTaskParamter.formData, item, true);
                }
            }
            #endregion

            await flowTaskOtherUtil.CreateOperatorRecode(flowTaskParamter, 13);
            _db.CommitTran();

            #region 消息与事件
            //同意事件
            if (!await flowTaskMsgUtil.RequestEvents(flowTaskParamter.approversProperties.approveFuncConfig, flowTaskParamter, FuncConfigEnum.approve) && flowTaskParamter.startProperties.funcConfigRule == 1)
            {
                await Cancel(flowTaskParamter);
            }
            flowTaskParamter.approversProperties = flowTaskOtherUtil.SyncApproProCofig(flowTaskParamter.approversProperties, flowTaskParamter.startProperties);
            var bodyDic = new Dictionary<string, object>();
            //抄送
            var userIdList = flowTaskParamter.flowTaskCirculateEntityList.Select(x => x.ObjectId).ToList();
            if (userIdList.Any())
            {
                bodyDic = flowTaskMsgUtil.GetMesBodyText(flowTaskParamter, userIdList, null, 3, flowTaskParamter.flowTaskOperatorEntity.Id);
                await flowTaskMsgUtil.Alerts(flowTaskParamter.approversProperties.copyMsgConfig, userIdList, flowTaskParamter, "MBXTLC007", bodyDic);
            }

            if (flowTaskParamter.flowTaskOperatorEntityList.Any())
            {
                // 关闭当前节点超时提醒任务
                _schedulerFactory.RemoveJob("Job_CS_" + flowTaskParamter.flowTaskNodeEntity.Id);
                _schedulerFactory.RemoveJob("Job_TX_" + flowTaskParamter.flowTaskNodeEntity.Id);
                #region 消息提醒
                var messageDic = flowTaskOtherUtil.GroupByOperator(flowTaskParamter.flowTaskOperatorEntityList);
                //审批
                foreach (var item in messageDic.Keys)
                {
                    var userList = messageDic[item].Select(x => x.HandleId).ToList();
                    bodyDic = flowTaskMsgUtil.GetMesBodyText(flowTaskParamter, userList, messageDic[item], 2);
                    await flowTaskMsgUtil.Alerts(flowTaskParamter.startProperties.waitMsgConfig, bodyDic.Keys.ToList(), flowTaskParamter, "MBXTLC001", bodyDic);
                    await flowTaskMsgUtil.Alerts(flowTaskParamter.approversProperties.approveMsgConfig, bodyDic.Keys.ToList(), flowTaskParamter, "MBXTLC003", bodyDic);
                    // 超时提醒
                    await TimeoutOrRemind(flowTaskParamter, item, messageDic[item]);
                }
                #endregion

            }

            //委托审批消息
            if (!_userManager.UserId.Equals(flowTaskParamter.flowTaskOperatorEntity.HandleId))
            {
                await flowTaskMsgUtil.SendDelegateMsg("审批", flowTaskParamter.flowTaskOperatorEntity.HandleId, flowTaskParamter.flowTaskEntity.FullName);
            }
            //退回到发起.
            if (flowTaskParamter.flowTaskEntity.Status == FlowTaskStatusEnum.Reject.ParseToInt())
            {
                bodyDic = flowTaskMsgUtil.GetMesBodyText(flowTaskParamter, new List<string>() { flowTaskParamter.flowTaskEntity.CreatorUserId }, null, 2);
                await flowTaskMsgUtil.Alerts(flowTaskParamter.approversProperties.rejectMsgConfig, new List<string>() { flowTaskParamter.flowTaskEntity.CreatorUserId }, flowTaskParamter, "MBXTLC003", bodyDic);
            }
            #endregion
            return new List<FlowTaskCandidateModel>();
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 转向判断.
    /// </summary>
    /// <param name="flowTaskParamter"></param>
    /// <returns></returns>
    public async Task<bool> IsSwerve(FlowTaskParamter flowTaskParamter)
    {
        if (flowTaskParamter.flowTaskEntity.RejectDataId.IsNotEmptyOrNull())
            return false;
        // 会签是否完成
        if (!flowTaskParamter.countersignOver)
            return false;
        // 如果当前节点的下一节点是否转向
        if (flowTaskParamter.approversProperties.swerveNode.IsNotEmptyOrNull())
        {
            var operatorRecordList = await _flowTaskRepository.GetTaskOperatorRecordList(x => x.TaskId == flowTaskParamter.flowTaskEntity.Id && x.NodeCode == flowTaskParamter.approversProperties.swerveNode && x.Status == 0);
            if (!operatorRecordList.Any()) throw Oops.Oh(ErrorCode.WF0005);
            flowTaskParamter.rejectStep = flowTaskParamter.approversProperties.swerveNode;
            flowTaskParamter.rejectType = "1";
            return true;
        }
        return false;
    }
    #endregion

    #region PrivateMethod
    #region 经办处理
    /// <summary>
    /// 根据当前审批节点插入下一节点经办.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <param name="handleStatus">审批状态.</param>
    /// <param name="type">操作类型.</param>
    /// <returns></returns>
    private async Task CreateNextFlowTaskOperator(FlowTaskParamter flowTaskParamter, int handleStatus, int type)
    {
        try
        {
            var isInsert = false;// 当前节点是否完成.
            //下个节点集合
            List<FlowTaskNodeEntity> nextNodeEntityList = new List<FlowTaskNodeEntity>();
            if (handleStatus == 0)
            {
                nextNodeEntityList = flowTaskParamter.flowTaskNodeEntityList.FindAll(x => flowTaskParamter.rejectStep.Contains(x.NodeCode));
            }
            else
            {
                nextNodeEntityList = flowTaskParamter.flowTaskNodeEntityList.FindAll(m => flowTaskParamter.flowTaskNodeEntity.NodeNext.Contains(m.NodeCode));
            }

            foreach (var nextNodeEntity in nextNodeEntityList)
            {
                var isShuntNodeCompletion = flowTaskNodeUtil.IsShuntNodeCompletion(flowTaskParamter.flowTaskNodeEntityList, nextNodeEntity.NodeCode, flowTaskParamter.flowTaskNodeEntity);
                if (flowTaskParamter.approversProperties.counterSign == 0 || flowTaskOtherUtil.IsAchievebilProportion(flowTaskParamter, handleStatus))
                {
                    isInsert = true;
                    await flowTaskUserUtil.AddFlowTaskOperatorEntityByAssigneeType(flowTaskParamter, nextNodeEntity, 1, isShuntNodeCompletion);
                }
                else if (flowTaskParamter.approversProperties.counterSign == 2)
                {
                    // 当前所有经办(包含当前)的第二个为下一经办数据
                    flowTaskParamter.flowTaskOperatorEntityList.Add(flowTaskParamter.thisFlowTaskOperatorEntityList.Where(x => x.Completion == 0).OrderBy(x => x.SortCode).ToList()[1]);
                    await _flowTaskRepository.CreateTaskOperator(flowTaskParamter.flowTaskOperatorEntityList);
                    break;
                }

                // 如果下一节点是子流程，则要获取子流程下的审批节点的异常节点（审批同意）
                if (FlowTaskNodeTypeEnum.subFlow.ParseToString().Equals(nextNodeEntity.NodeType) && handleStatus == 1 && isShuntNodeCompletion)
                {
                    await flowTaskUserUtil.GetErrorNode(flowTaskParamter, flowTaskParamter.flowTaskNodeEntityList.FindAll(m => nextNodeEntity.NodeNext.Contains(m.NodeCode)));
                }
            }

            // 判断是否插入下个节点数据
            await NextOperatorManager(flowTaskParamter, nextNodeEntityList, isInsert, handleStatus, type);
        }
        catch (AppFriendlyException ex)
        {
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 下一节点经办以及相关数据处理.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <param name="nextNodeEntityList">下一节点.</param>
    /// <param name="handleStatus">审批状态.</param>
    /// <param name="type">操作类型.</param>
    /// <returns></returns>
    private async Task NextOperatorManager(FlowTaskParamter flowTaskParamter, List<FlowTaskNodeEntity> nextNodeEntityList, bool isInsert, int handleStatus, int type)
    {
        //下一节点编码
        var nextNodeCodeList = nextNodeEntityList.Select(x => x.NodeCode).ToList();
        //下一节点完成度
        var nextNodeCompletion = nextNodeEntityList.Where(x => x.NodePropertyJson.ToObject<ApproversProperties>().progress.ParseToInt() != 0).Select(x => x.NodePropertyJson.ToObject<ApproversProperties>().progress.ParseToInt()).ToList();
        if (handleStatus == 0)
        {
            if (isInsert)
            {
                var rejectsubFlowNodeList = await flowTaskNodeUtil.RejectManager(flowTaskParamter, nextNodeEntityList, nextNodeCodeList, nextNodeCompletion);
                if (flowTaskParamter.approversProperties.rejectType == 1)//重新审批
                {
                    // 终止驳回节点与当前节点中的子流程节点
                    foreach (var item in rejectsubFlowNodeList)
                    {
                        var childPro = item.NodePropertyJson.ToObject<ChildTaskProperties>();
                        var childTaskList = await _flowTaskRepository.GetTaskList(x => childPro.childTaskId.Contains(x.Id) && x.DeleteMark == null);
                        foreach (var childTask in childTaskList)
                        {
                            await flowTaskOtherUtil.CancelTask(childTask);
                        }
                    }
                }
            }
        }
        else
        {
            #region 判断是否插入下个节点数据
            if (isInsert)
            {
                flowTaskParamter.flowTaskNodeEntity.Completion = 1;
                if (flowTaskParamter.startProperties.resurgenceDataRule == 1)
                {
                    flowTaskParamter.flowTaskNodeEntity.DraftData = flowTaskParamter.formData.ToJsonString();
                }
                // 当前节点为分流的发起节点,下一节点必定都是审批节点
                if (nextNodeEntityList.Count > 1)
                {
                    flowTaskParamter.flowTaskEntity.ThisStepId = flowTaskNodeUtil.GetThisStepId(flowTaskParamter.flowTaskNodeEntityList, nextNodeCodeList, flowTaskParamter.flowTaskEntity.ThisStepId);
                    flowTaskParamter.flowTaskEntity.ThisStep = flowTaskNodeUtil.GetThisStep(flowTaskParamter.flowTaskNodeEntityList, flowTaskParamter.flowTaskEntity.ThisStepId);
                    flowTaskParamter.flowTaskEntity.Completion = nextNodeCompletion.Count == 0 ? flowTaskParamter.flowTaskEntity.Completion : nextNodeCompletion.Min();
                    await _flowTaskRepository.CreateTaskOperator(flowTaskParamter.flowTaskOperatorEntityList);
                    await flowTaskOtherUtil.GetNextFormData(flowTaskParamter, nextNodeEntityList, type);
                }
                else
                {
                    // 非分流的发起节点下一节点只有一个.
                    var nextNode = nextNodeEntityList.FirstOrDefault();
                    // 合流节点的上级节点是否都完成.
                    var isShuntNodeCompletion = flowTaskNodeUtil.IsShuntNodeCompletion(flowTaskParamter.flowTaskNodeEntityList, nextNode.NodeCode, flowTaskParamter.flowTaskNodeEntity);
                    if (isShuntNodeCompletion)
                    {
                        // 修改当前节点以及插入数据.
                        flowTaskParamter.flowTaskEntity.ThisStepId = flowTaskNodeUtil.GetThisStepId(flowTaskParamter.flowTaskNodeEntityList, nextNodeCodeList, flowTaskParamter.flowTaskEntity.ThisStepId);
                        flowTaskParamter.flowTaskEntity.ThisStep = flowTaskNodeUtil.GetThisStep(flowTaskParamter.flowTaskNodeEntityList, flowTaskParamter.flowTaskEntity.ThisStepId);
                        flowTaskParamter.flowTaskEntity.Completion = nextNodeCompletion.Count == 0 ? flowTaskParamter.flowTaskEntity.Completion : nextNodeCompletion.Min();
                        if (flowTaskParamter.flowTaskOperatorEntityList.Any())
                        {
                            await _flowTaskRepository.CreateTaskOperator(flowTaskParamter.flowTaskOperatorEntityList);
                            await flowTaskOtherUtil.GetNextFormData(flowTaskParamter, nextNodeEntityList, type);
                        }
                        // 下一节点是子流程节点.
                        if (FlowTaskNodeTypeEnum.subFlow.ParseToString().Equals(nextNode.NodeType))
                        {
                            var childTaskPro = nextNode.NodePropertyJson.ToObject<ChildTaskProperties>();
                            var childFLowEngine = _flowTaskRepository.GetFlowTemplateInfo(childTaskPro.flowId);//子流程引擎
                            if (childFLowEngine.IsNullOrEmpty())
                                throw Oops.Oh(ErrorCode.WF0026);
                            var childTaskCrUserList = await flowTaskUserUtil.GetSubFlowCrUser(childTaskPro, flowTaskParamter, nextNode);//子流程发起人
                            var childFormData = await flowTaskOtherUtil.GetSubFlowFormData(flowTaskParamter, childTaskPro);//子流程发起表单数据.
                            // 子流程任务id
                            childTaskPro.childTaskId = await CreateSubProcesses(childTaskPro, childFormData, flowTaskParamter.flowTaskEntity.Id, childTaskCrUserList);
                            childTaskPro.formData = flowTaskParamter.formData.ToJsonString();//子流程结束回到主流程表单数据.
                            nextNode.NodePropertyJson = childTaskPro.ToJsonString();
                            nextNode.Completion = childTaskPro.isAsync ? 1 : 0;
                            await _flowTaskRepository.UpdateTaskNode(nextNode);
                            // 异步子流程则跳过插入子流程下一节点经办
                            if (childTaskPro.isAsync)
                            {
                                // 异步子流程审批
                                var flowTaskParamterSubAsync = flowTaskParamter.Copy();
                                // 替换最新所有节点
                                flowTaskParamterSubAsync.flowTaskNodeEntityList.Remove(nextNode);
                                flowTaskParamterSubAsync.flowTaskNodeEntityList.Add(nextNode);
                                flowTaskParamterSubAsync.flowTaskNodeEntity = nextNode;
                                flowTaskParamterSubAsync.approversProperties = nextNode.NodePropertyJson.ToObject<ApproversProperties>();
                                await CreateNextFlowTaskOperator(flowTaskParamterSubAsync, handleStatus, type);
                                flowTaskParamter.flowTaskEntity = flowTaskParamterSubAsync.flowTaskEntity;//更新已完成的子流程节点任务信息
                            }
                        }
                        // 下一节点是结束节点.
                        if (FlowTaskNodeTypeEnum.end.ParseToString().Equals(nextNode.NodeCode))
                        {
                            flowTaskParamter.flowTaskEntity.Status = FlowTaskStatusEnum.Adopt.ParseToInt();
                            flowTaskParamter.flowTaskEntity.Completion = 100;
                            flowTaskParamter.flowTaskEntity.EndTime = DateTime.Now;
                            flowTaskParamter.flowTaskEntity.ThisStepId = "end";
                            flowTaskParamter.flowTaskEntity.ThisStep = "结束";

                            // 子流程结束回到主流程下一节点
                            if (flowTaskParamter.flowTaskEntity.ParentId != "0" && flowTaskParamter.flowTaskEntity.IsAsync == 0)
                            {
                                await InsertSubFlowNextNode(flowTaskParamter.flowTaskEntity);
                            }
                        }
                    }
                    else
                    {
                        if (FlowTaskNodeTypeEnum.end.ParseToString().Equals(nextNode.NodeCode) && isShuntNodeCompletion)
                        {
                            flowTaskParamter.flowTaskEntity.Status = FlowTaskStatusEnum.Adopt.ParseToInt();
                            flowTaskParamter.flowTaskEntity.Completion = 100;
                            flowTaskParamter.flowTaskEntity.EndTime = DateTime.Now;
                            flowTaskParamter.flowTaskEntity.ThisStepId = "end";
                            flowTaskParamter.flowTaskEntity.ThisStep = "结束";

                            // 子流程结束回到主流程下一节点
                            if (flowTaskParamter.flowTaskEntity.ParentId != "0" && flowTaskParamter.flowTaskEntity.IsAsync == 0)
                            {
                                await InsertSubFlowNextNode(flowTaskParamter.flowTaskEntity);
                            }
                        }
                        else
                        {
                            var thisStepIds = flowTaskParamter.flowTaskEntity.ThisStepId.Split(",").ToList();
                            thisStepIds.Remove(flowTaskParamter.flowTaskNodeEntity.NodeCode);
                            flowTaskParamter.flowTaskEntity.ThisStepId = string.Join(",", thisStepIds.ToArray());
                            flowTaskParamter.flowTaskEntity.ThisStep = flowTaskNodeUtil.GetThisStep(flowTaskParamter.flowTaskNodeEntityList, flowTaskParamter.flowTaskEntity.ThisStepId);
                        }
                    }
                }
            }
            #endregion
        }
    }
    #endregion

    #region 子流程处理

    /// <summary>
    /// 创建子流程任务.
    /// </summary>
    /// <param name="childTaskPro">子流程节点属性.</param>
    /// <param name="formData">表单数据.</param>
    /// <param name="parentId">子任务父id.</param>
    /// <param name="childTaskCrUsers">子任务创建人.</param>
    private async Task<List<string>> CreateSubProcesses(ChildTaskProperties childTaskPro, object formData, string parentId, List<string> childTaskCrUsers)
    {
        var childFLowEngine = _flowTaskRepository.GetFlowTemplateInfo(childTaskPro.flowId);
        var childTaskId = new List<string>();
        foreach (var item in childTaskCrUsers)
        {
            var title = string.Format("{0}的{1}", await _usersService.GetUserName(item, false), childFLowEngine.fullName);
            var flowTaskSubmitModel = new FlowTaskSubmitModel
            {
                flowId = childTaskPro.flowId,
                flowTitle = title,
                flowUrgent = 0,
                formData = formData,
                status = childTaskPro.autoSubmit ? 0 : 1,
                approvaUpType = 0,
                parentId = parentId,
                isDev = false,
                crUser = item,
                isAsync = childTaskPro.isAsync,
                autoSubmit = childTaskPro.autoSubmit,
            };
            var flowTaskParamter = new FlowTaskParamter();
            if (childTaskPro.autoSubmit)
            {
                List<FlowTaskCandidateModel> result = await this.Submit(flowTaskSubmitModel);
                if (result.Any()) throw Oops.Oh(ErrorCode.WF0056);
                flowTaskParamter.flowTaskEntity = flowTaskSubmitModel.childTask.ToObject<FlowTaskEntity>();
                childTaskId.Add(flowTaskParamter.flowTaskEntity.Id);
            }
            else
            {
                flowTaskParamter.flowTaskEntity = await this.Save(flowTaskSubmitModel);
                childTaskId.Add(flowTaskParamter.flowTaskEntity.Id);
            }

            #region 子流程发起通知
            var bodyDic = flowTaskMsgUtil.GetMesBodyText(flowTaskParamter, new List<string> { item }, null, 1);
            await flowTaskMsgUtil.Alerts(childTaskPro.launchMsgConfig, new List<string> { item }, flowTaskParamter, "MBXTLC011", bodyDic);
            #endregion
        }
        return childTaskId;
    }

    /// <summary>
    /// 插入子流程.
    /// </summary>
    /// <param name="childFlowTaskEntity">子流程.</param>
    /// <returns></returns>
    private async Task InsertSubFlowNextNode(FlowTaskEntity childFlowTaskEntity)
    {
        try
        {
            //所有子流程(不包括当前流程)
            var childFlowTaskAll = (await _flowTaskRepository.GetTaskList()).FindAll(x => x.ParentId == childFlowTaskEntity.ParentId && x.Id != childFlowTaskEntity.Id && x.IsAsync == 0);
            //父流程
            var parentFlowTask = _flowTaskRepository.GetTaskFirstOrDefault(childFlowTaskEntity.ParentId);
            if (parentFlowTask.RejectDataId.IsNotEmptyOrNull()) throw Oops.Oh(ErrorCode.WF0040);
            // 父流程所有节点
            var flowTaskNodeEntityList = await _flowTaskRepository.GetTaskNodeList(x => x.TaskId == parentFlowTask.Id && x.State == 0);
            //子流程所属父流程节点
            var parentSubFlowNode = (await _flowTaskRepository.GetTaskNodeList(x => x.TaskId == parentFlowTask.Id && FlowTaskNodeTypeEnum.subFlow.ParseToString().Equals(x.NodeType) && x.Completion == 0)).Find(x => x.NodePropertyJson.ToObject<ChildTaskProperties>().childTaskId.Contains(childFlowTaskEntity.Id));
            //判断该父流程子流程节点下的子流程是否完成
            var list = parentSubFlowNode.NodePropertyJson.ToObject<ChildTaskProperties>().childTaskId;
            if (!childFlowTaskAll.Any(x => x.Status != FlowTaskStatusEnum.Adopt.ParseToInt() && list.Contains(x.Id)))
            {
                parentSubFlowNode.Completion = 1;
                await _flowTaskRepository.UpdateTaskNode(parentSubFlowNode);
            }
            // 判断是否插入子流程节点下一节点数据
            var subFlowNextNode = flowTaskNodeEntityList.Find(m => parentSubFlowNode.NodeNext.Contains(m.NodeCode));
            var isShuntNodeCompletion = flowTaskNodeUtil.IsShuntNodeCompletion(flowTaskNodeEntityList, subFlowNextNode.NodeCode, parentSubFlowNode);
            if (parentSubFlowNode.Completion == 1)
            {
                // 分流合流完成则插入，反之则修改父流程当前节点
                if (isShuntNodeCompletion)
                {
                    var subFlowOperator = parentSubFlowNode.Adapt<FlowTaskOperatorEntity>();
                    subFlowOperator.Id = null;
                    subFlowOperator.TaskNodeId = parentSubFlowNode.Id;
                    var childData = parentSubFlowNode.NodePropertyJson.ToObject<ChildTaskProperties>().formData;
                    var handleModel = new FlowHandleModel();
                    handleModel.formData = childData.ToObject();
                    FlowTaskParamter flowTaskParamter = await _flowTaskRepository.GetTaskParamterByNodeId(parentSubFlowNode.Id, handleModel);
                    flowTaskParamter.flowTaskOperatorEntity = subFlowOperator;
                    await this.Audit(flowTaskParamter);
                }
                else
                {
                    var thisStepIds = parentFlowTask.ThisStepId.Split(",").ToList();
                    thisStepIds.Remove(parentSubFlowNode.NodeCode);
                    parentFlowTask.ThisStepId = string.Join(",", thisStepIds.ToArray());
                    parentFlowTask.ThisStep = flowTaskNodeUtil.GetThisStep(flowTaskNodeEntityList, parentFlowTask.ThisStepId);
                    await _flowTaskRepository.UpdateTask(parentFlowTask);
                }
            }
        }
        catch (AppFriendlyException ex)
        {
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }
    #endregion

    #region 其他
    /// <summary>
    /// 自动同意审批.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <param name="isTimeOut">是否超时审批.</param>
    /// <returns></returns>
    private async Task AutoAudit(FlowTaskParamter flowTaskParamter, bool isTimeOut = false)
    {
        try
        {
            var flowEngineEntity = _flowTaskRepository.GetFlowTemplateInfo(flowTaskParamter.flowTaskEntity.FlowId);
            var flowTaskOperatorEntityList = await _flowTaskRepository.GetTaskOperatorList(x => x.TaskId == flowTaskParamter.flowTaskEntity.Id && x.Completion == 0 && x.State != -1);
            foreach (var item in flowTaskOperatorEntityList)
            {
                var flowTaskCandidateModels = new List<FlowTaskCandidateModel>();
                var nextTaskNodeEntity = flowTaskParamter.flowTaskNodeEntityList.Find(m => m.Id.Equals(item.TaskNodeId));
                var approverPropertiers = nextTaskNodeEntity.NodePropertyJson.ToObject<ApproversProperties>();
                // 看下个审批节点是否是候选人或异常节点
                await flowTaskUserUtil.GetErrorNode(flowTaskParamter, flowTaskParamter.flowTaskNodeEntityList.FindAll(m => nextTaskNodeEntity.NodeNext.Contains(m.NodeCode)));
                await flowTaskUserUtil.GetCandidates(flowTaskCandidateModels, flowTaskParamter.flowTaskNodeEntityList.FindAll(m => nextTaskNodeEntity.NodeNext.Contains(m.NodeCode)), flowTaskParamter.flowTaskNodeEntityList, approverPropertiers.swerveNode.IsNotEmptyOrNull());
                var falag = flowTaskCandidateModels.Count == 0;
                // 如果自动审批下一节点是选择分支或候选人由超管审批
                if (!falag && item.HandleId.Equals("jnpf"))
                {
                    item.HandleId = _userManager.GetAdminUserId();
                    await _flowTaskRepository.UpdateTaskOperator(item);
                    await _flowTaskRepository.UpdateTaskOperatorUser(new List<FlowTaskOperatorUserEntity> { item.Adapt<FlowTaskOperatorUserEntity>() });
                }
                var isCom = (await _flowTaskRepository.GetTaskOperatorInfo(x => x.Id == item.Id && x.Completion == 0)).IsNotEmptyOrNull();
                if (isCom && falag)
                {
                    var isAuto = false;
                    var upNodeList = flowTaskParamter.flowTaskNodeEntityList.FindAll(x => !x.NodeCode.Equals("end") && x.NodeNext.Contains(item.NodeCode)).Select(x => x.Id).ToList();
                    if (approverPropertiers.hasAgreeRule)
                    {
                        foreach (var agreeRule in approverPropertiers.agreeRules)
                        {
                            if (agreeRule == "2")
                            {
                                isAuto = item.HandleId == flowTaskParamter.flowTaskEntity.CreatorUserId;
                                if (isAuto)
                                    break;
                            }
                            if (agreeRule == "3")
                            {
                                isAuto = (await _flowTaskRepository.GetTaskOperatorRecordInfo(x => x.TaskId == item.TaskId && upNodeList.Contains(x.TaskNodeId) && x.HandleId == item.HandleId && x.HandleStatus == 1 && x.Status >= 0)).IsNotEmptyOrNull();
                                if (isAuto)
                                    break;
                            }
                            if (agreeRule == "4")
                            {
                                isAuto = (await _flowTaskRepository.GetTaskOperatorRecordInfo(x => x.TaskId == item.TaskId && x.HandleId == item.HandleId && x.HandleStatus == 1 && x.Status >= 0)).IsNotEmptyOrNull();
                                if (isAuto)
                                    break;
                            }
                        }
                    }
                    if (isAuto || item.HandleId.Equals("jnpf") || isTimeOut)
                    {
                        var handleModel = new FlowHandleModel();
                        handleModel.handleOpinion = item.HandleId.Equals("jnpf") ? "默认审批通过" : "自动审批通过";
                        handleModel.handleOpinion = isTimeOut ? "超时审批通过" : handleModel.handleOpinion;
                        var formId = (await _flowTaskRepository.GetTaskNodeInfo(item.TaskNodeId)).FormId;
                        handleModel.formData = await _runService.GetFlowFormDataDetails(formId, item.TaskId);
                        await AdjustNodeByCon(flowEngineEntity, handleModel.formData, item);
                        var flowTaskParamterAuto = await _flowTaskRepository.GetTaskParamterByOperatorId(item.Id, handleModel);
                        await this.Audit(flowTaskParamterAuto, true);
                    }
                }
            }
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// 流程表单数据处理(新增/修改).
    /// </summary>
    /// <param name="flowTaskSubmitModel"></param>
    /// <returns></returns>
    private async Task<string> FlowDynamicDataManage(FlowTaskSubmitModel flowTaskSubmitModel)
    {
        try
        {
            var isUpdate = flowTaskSubmitModel.id.IsNotEmptyOrNull();
            var id = isUpdate ? flowTaskSubmitModel.id : SnowflakeIdHelper.NextId();
            var startProperties = flowTaskSubmitModel.flowJsonModel.flowTemplateJson.ToObject<FlowTemplateJsonModel>().properties.ToObject<StartProperties>();
            //  委托人系统控件数据.
            if (flowTaskSubmitModel.isDelegate)
            {
                var formDic = flowTaskSubmitModel.formData.ToObject<Dictionary<string, object>>();
                var delegateUser = _usersService.GetInfoByUserId(flowTaskSubmitModel.crUser);
                formDic["Jnpf_FlowDelegate_CurrPosition"] = delegateUser.PositionId;
                formDic["Jnpf_FlowDelegate_CurrOrganize"] = _flowTaskRepository.GetOrgTree(delegateUser.OrganizeId).ToJsonString();
                flowTaskSubmitModel.formData = formDic;
            }
            var fEntity = _flowTaskRepository.GetFlowFromEntity(startProperties.formId);
            var formOperates = startProperties.formOperates.ToObject<List<FormOperatesModel>>();
            var systemControlList = formOperates.Where(x => !x.write).Select(x => x.id).ToList();
            await _runService.SaveFlowFormData(fEntity, flowTaskSubmitModel.formData.ToJsonString(), id, flowTaskSubmitModel.flowId, isUpdate, systemControlList);

            // 功能流程提交
            if (flowTaskSubmitModel.isDev && flowTaskSubmitModel.id.IsNotEmptyOrNull() && flowTaskSubmitModel.status == 0)
            {
                if (!await _flowTaskRepository.AnyFlowTask(x => x.Id == flowTaskSubmitModel.id && x.DeleteMark == null))
                {
                    flowTaskSubmitModel.id = string.Empty;
                }
            }
            return id;
        }
        catch (AppFriendlyException ex)
        {
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }
    #endregion

    #region 超时处理

    /// <summary>
    /// 超时/提醒.
    /// </summary>
    /// <param name="flowTaskParamter"></param>
    /// <param name="nodeId"></param>
    /// <param name="flowTaskOperatorEntities"></param>
    /// <returns></returns>
    private async Task TimeoutOrRemind(FlowTaskParamter flowTaskParamter, string nodeId, List<FlowTaskOperatorEntity> flowTaskOperatorEntities)
    {
        var flowNode = await _flowTaskRepository.GetTaskNodeInfo(nodeId);
        flowTaskParamter.formData = await _runService.GetFlowFormDataDetails(flowNode.FormId, flowTaskParamter.flowTaskEntity.Id);
        var nodeProp = flowTaskOtherUtil.SyncApproProCofig(flowNode?.NodePropertyJson?.ToObject<ApproversProperties>(), flowTaskParamter.startProperties);
        if (nodeProp.timeLimitConfig.on > 0)
        {
            // 起始时间
            var startingTime = flowTaskOtherUtil.GetStartingTime(nodeProp.timeLimitConfig, (DateTime)flowTaskOperatorEntities.FirstOrDefault().CreatorTime, (DateTime)flowTaskParamter.flowTaskEntity.CreatorTime, flowTaskParamter.formData.ToJsonStringOld());
            Console.WriteLine(string.Format("节点{0}起始时间：{1}", nodeId, startingTime.ToString("yyyy-MM-dd HH:mm:ss")));
            // 创建限时提醒
            if (nodeProp.noticeConfig.on > 0)
            {
                var startTime = startingTime.AddHours(nodeProp.noticeConfig.firstOver); // 提醒开始时间
                var endTime = startingTime.AddHours(nodeProp.timeLimitConfig.duringDeal).AddSeconds(20); // 提醒结束时间
                // 分钟
                //var startTime = startingTime.AddMinutes(nodeProp.noticeConfig.firstOver); // 提醒开始时间
                //var endTime = startingTime.AddMinutes(nodeProp.timeLimitConfig.duringDeal).AddSeconds(20); // 提醒结束时间(加五秒误差时间)
                nodeProp.noticeMsgConfig = nodeProp.noticeMsgConfig.on == 2 ? flowTaskParamter.startProperties.noticeMsgConfig : nodeProp.noticeMsgConfig;

                if (startTime <= endTime && DateTime.Now <= endTime)
                {
                    var interval = 1; // 第一次执行间隔
                    var runCount = 0; // 已执行次数
                    var isAtOnce = false; // 是否立即执行事件和自动审批
                    if (DateTime.Now <= startTime) // 当前时间小于开始时间
                    {
                        interval = (startTime - DateTime.Now).TotalMilliseconds.ParseToInt();
                    }
                    else if (startTime < DateTime.Now && DateTime.Now < endTime && nodeProp.noticeConfig.overTimeDuring > 0) // 当前时间处于开始时间与结束时间区间内
                    {
                        runCount = (DateTime.Now - startTime).TotalHours.ParseToInt() / nodeProp.noticeConfig.overTimeDuring;
                        isAtOnce = true;
                    }
                    await _taskQueue.EnqueueAsync(
                        async (_, _) => { await OnceNotifyEvent(nodeProp, flowTaskParamter, nodeId, runCount + 1, false, isAtOnce); }, interval);
                    if (nodeProp.noticeConfig.overTimeDuring > 0 && startTime < endTime)
                    {
                        await MsgOrRequest(nodeProp, flowTaskParamter, startTime, endTime, nodeId, runCount + 1);
                    }
                }
            }

            // 创建超时提醒
            if (nodeProp.overTimeConfig.on > 0)
            {
                var startTime = startingTime.AddHours(nodeProp.timeLimitConfig.duringDeal + nodeProp.overTimeConfig.firstOver); // 超时开始时间
                // 分钟
                //var startTime = startingTime.AddMinutes(nodeProp.timeLimitConfig.duringDeal + nodeProp.overTimeConfig.firstOver); // 超时开始时间
                var interval = 1; // 第一次执行间隔
                var runCount = 0; // 已执行次数
                var isAtOnce = false; // 是否立即执行事件和自动审批
                if (DateTime.Now <= startTime)
                {
                    interval = (startTime - DateTime.Now).TotalMilliseconds.ParseToInt();
                }
                else
                {
                    if (nodeProp.overTimeConfig.overTimeDuring > 0)
                    {
                        runCount = (DateTime.Now - startTime).TotalHours.ParseToInt() / nodeProp.overTimeConfig.overTimeDuring;
                    }
                    isAtOnce = true;
                }
                await _taskQueue.EnqueueAsync(async (_, _) => { await OnceNotifyEvent(nodeProp, flowTaskParamter, nodeId, runCount + 1, true, isAtOnce); }, interval);
                if (nodeProp.overTimeConfig.overTimeDuring > 0)
                {
                    await MsgOrRequest(nodeProp, flowTaskParamter, startTime, null, nodeId, runCount + 1);
                }
            }
        }
    }

    /// <summary>
    /// 定时任务执行超时提醒.
    /// </summary>
    /// <param name="approPro"></param>
    /// <param name="flowTaskParamter"></param>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <param name="nodeId"></param>
    /// <param name="runCount"></param>
    /// <returns></returns>
    private async Task MsgOrRequest(ApproversProperties approPro, FlowTaskParamter flowTaskParamter, DateTime? startTime, DateTime? endTime, string nodeId, int runCount = 1)
    {
        // 提醒(false)/超时(true)
        var isTimeOut = endTime.IsNullOrEmpty();
        var workerName = isTimeOut ? "CS_" + nodeId : "TX_" + nodeId;
        var createType = RequestTypeEnum.Http;

        TriggerBuilder? triggerBuilder = _jobManager.CreateTriggerBuilder(new JobTriggerModel
        {
            interval = isTimeOut ? approPro.overTimeConfig.overTimeDuring : approPro.noticeConfig.overTimeDuring,
            intervalType = 3,
            TriggreId = string.Format("Trigger_{0}", workerName),
            Description = string.Format("节点:{0}调度触发器", workerName),
            StartTime = startTime,
            EndTime = endTime,
            RunOnStart = true,
        });
        triggerBuilder.SetResult(workerName + "任务创建");
        var scheduleTaskModel = new ScheduleTaskModel();
        scheduleTaskModel.taskParams.Add("approPro", approPro);
        scheduleTaskModel.taskParams.Add("flowTaskParamter", flowTaskParamter);
        scheduleTaskModel.taskParams.Add("nodeId", nodeId);
        scheduleTaskModel.taskParams.Add("count", runCount);
        scheduleTaskModel.taskParams.Add("isTimeOut", isTimeOut);
        scheduleTaskModel.taskParams.Add("isAtOnce", false);

        var jobDetail = _jobManager.ObtainJobHttpDetails(new JobDetailModel
        {
            JobId = string.Format("Job_{0}", workerName),
            Description = string.Format("租户`{0}`下名称为`{1}`的HTTP系统调度", _userManager.TenantId, workerName),
            GroupName = "FlowTask",
            RequestUri = "/ScheduleTask/flowtask",
            HttpMethod = HttpMethod.Post,
            Body = scheduleTaskModel.ToJsonString(),
            TaskId = nodeId,
            TenantId = _userManager.TenantId,
            UserId = _userManager.UserId,
        });
        var jobType = typeof(JNPFHttpJob);

        _schedulerFactory.AddJob(
                JobBuilder.Create(jobType).LoadFrom(jobDetail)
                .SetJobType(jobType),
                triggerBuilder);

        // 延迟一下等待持久化写入，再执行其他字段的更新
        await Task.Delay(500);

        var job = _db.GetConnection("JNPF-Job");

        await job.Updateable<JobDetails>().SetColumns(u => new JobDetails { CreateType = createType })
                     .Where(u => u.JobId.Equals(jobDetail.JobId)).ExecuteCommandAsync();
    }

    /// <summary>
    /// 执行超时提醒配置.
    /// </summary>
    /// <param name="approPro"></param>
    /// <param name="flowTaskParamter"></param>
    /// <param name="nodeId"></param>
    /// <param name="count"></param>
    /// <param name="isTimeOut"></param>
    /// <param name="isAtOnce"></param>
    /// <returns></returns>
    public async Task NotifyEvent(ApproversProperties approPro, FlowTaskParamter flowTaskParamter, string nodeId, int count, bool isTimeOut, bool isAtOnce = false)
    {
        var flowTask = _flowTaskRepository.GetTaskFirstOrDefault(flowTaskParamter.flowTaskEntity.Id);
        if (flowTask.IsNotEmptyOrNull() && flowTask.Suspend != 1)
        {
            var workerName = isTimeOut ? "CS_" + nodeId : "TX_" + nodeId;
            if (_schedulerFactory.ContainsJob("Job_" + workerName))
            {
                var job = _schedulerFactory.GetJob("Job_" + workerName);
                count = job.GetTriggerBuilder("Trigger_" + workerName).NumberOfRuns.ParseToInt() + count;
            }
            var msgEncode = isTimeOut ? "MBXTLC009" : "MBXTLC008";
            var msgReMark = isTimeOut ? "超时" : "提醒";
            var msgConfig = isTimeOut ? approPro.overTimeMsgConfig : approPro.noticeMsgConfig;
            var funcConfig = isTimeOut ? approPro.overTimeFuncConfig : approPro.noticeFuncConfig;
            var funcConfigEnum = isTimeOut ? FuncConfigEnum.overTime : FuncConfigEnum.notice;
            var timeOutConfig = isTimeOut ? approPro.overTimeConfig : approPro.noticeConfig;
            // 通知
            if (timeOutConfig.overNotice)
            {
                var operatorList = await _flowTaskRepository.GetTaskOperatorList(x => x.TaskId == flowTaskParamter.flowTaskEntity.Id && x.TaskNodeId == nodeId && x.Completion == 0 && x.State != -1);
                var userList = operatorList.Select(x => x.HandleId).ToList();
                var remark = string.Format("现在时间：{3},节点{0}：第{1}次{2}通知", nodeId, count, msgReMark, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                Console.WriteLine(remark);
                var bodyDic = flowTaskMsgUtil.GetMesBodyText(flowTaskParamter, userList, operatorList, 2, remark);
                if (msgConfig.on > 0)
                {
                    await flowTaskMsgUtil.Alerts(msgConfig, userList, flowTaskParamter, msgEncode, bodyDic);
                }
            }
            // 事件
            var eventFlag = isAtOnce ? count >= timeOutConfig.overEventTime : timeOutConfig.overEventTime == count;
            if (funcConfig.on && timeOutConfig.overEvent && eventFlag)
            {
                Console.WriteLine(string.Format("开始执行{0}事件，现在时间：{1}", msgReMark, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                await flowTaskMsgUtil.RequestEvents(funcConfig, flowTaskParamter, funcConfigEnum);
            }
            // 自动审批
            var autoFlag = isAtOnce ? count >= timeOutConfig.overAutoApproveTime : timeOutConfig.overAutoApproveTime == count;
            if (isTimeOut && autoFlag && timeOutConfig.overAutoApprove)
            {
                Console.WriteLine("开始自动审批，现在时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                await AutoAudit(flowTaskParamter, true);
            }
        }
    }

    public async Task OnceNotifyEvent(ApproversProperties approPro, FlowTaskParamter flowTaskParamter, string nodeId, int count, bool isTimeOut, bool isAtOnce = false)
    {
        var userEntity = _userManager.User;
        var tenantId = _userManager.TenantId;
        var addressesFeature = _server.Features.Get<IServerAddressesFeature>();
        var addresses = addressesFeature?.Addresses;
        var localAddress = addresses.FirstOrDefault().Replace("[::]", "localhost");
        var token = NetHelper.GetToken(userEntity.Id, userEntity.Account, userEntity.RealName, userEntity.IsAdministrator, tenantId);
        var heraderDic = new Dictionary<string, object>();
        heraderDic.Add("Authorization", token);
        var scheduleTaskModel = new ScheduleTaskModel();
        scheduleTaskModel.taskParams.Add("approPro", approPro);
        scheduleTaskModel.taskParams.Add("flowTaskParamter", flowTaskParamter);
        scheduleTaskModel.taskParams.Add("nodeId", nodeId);
        scheduleTaskModel.taskParams.Add("count", count);
        scheduleTaskModel.taskParams.Add("isTimeOut", isTimeOut);
        scheduleTaskModel.taskParams.Add("isAtOnce", isAtOnce);
        var path = string.Format("{0}/ScheduleTask/flowtask", localAddress);
        var result = await path.SetHeaders(heraderDic).SetBody(scheduleTaskModel).PostAsStringAsync();
    }
    #endregion

    #endregion
}
