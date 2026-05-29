using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Models.WorkFlow;
using JNPF.Common.Security;
using JNPF.FriendlyException;
using JNPF.Schedule;
using JNPF.Systems.Interfaces.Permission;
using JNPF.VisualDev.Interfaces;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Enum;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.Conifg;
using JNPF.WorkFlow.Entitys.Model.Item;
using JNPF.WorkFlow.Entitys.Model.Properties;
using JNPF.WorkFlow.Interfaces.Repository;
using Newtonsoft.Json.Linq;

namespace JNPF.WorkFlow.Manager;

public class FlowTaskOtherUtil
{
    private readonly IFlowTaskRepository _flowTaskRepository;
    private readonly IUsersService _usersService;
    private readonly IRunService _runService;
    private readonly IUserManager _userManager;
    private readonly ISchedulerFactory _schedulerFactory;

    public FlowTaskOtherUtil(IFlowTaskRepository flowTaskRepository, IUsersService usersService, IRunService runService, IUserManager userManager, ISchedulerFactory schedulerFactory)
    {
        _flowTaskRepository = flowTaskRepository;
        _usersService = usersService;
        _runService = runService;
        _userManager = userManager;
        _schedulerFactory = schedulerFactory;
    }

    /// <summary>
    /// 修改当前节点经办数据.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <param name="handleStatus">审批类型（0：拒绝，1：同意）.</param>
    /// <returns></returns>
    public async Task UpdateFlowTaskOperator(FlowTaskParamter flowTaskParamter, int handleStatus)
    {
        var updateOperatorList = new List<FlowTaskOperatorEntity>(); // 要修改的经办
        // 加签
        if (flowTaskParamter.freeApproverUserId.IsNotEmptyOrNull())
        {
            #region 添加加签经办
            var freeApproverOperatorEntity = flowTaskParamter.flowTaskOperatorEntity.Copy();
            freeApproverOperatorEntity.Id = SnowflakeIdHelper.NextId();
            freeApproverOperatorEntity.ParentId = flowTaskParamter.flowTaskOperatorEntity.Id;
            freeApproverOperatorEntity.AppendHandleId = flowTaskParamter.flowTaskOperatorEntity.Id;
            freeApproverOperatorEntity.HandleId = flowTaskParamter.freeApproverUserId;
            freeApproverOperatorEntity.CreatorTime = DateTime.Now;
            freeApproverOperatorEntity.Completion = 0;
            freeApproverOperatorEntity.RollbackId = flowTaskParamter.freeApproverType == "1" ? flowTaskParamter.flowTaskOperatorEntity.Id : flowTaskParamter.flowTaskOperatorEntity.RollbackId;
            await _flowTaskRepository.CreateTaskOperator(freeApproverOperatorEntity);
            flowTaskParamter.flowTaskOperatorEntityList.Add(freeApproverOperatorEntity);
            #endregion

            flowTaskParamter.flowTaskOperatorEntity.State = 1;
            flowTaskParamter.flowTaskOperatorEntity.HandleStatus = handleStatus;
            flowTaskParamter.flowTaskOperatorEntity.Completion = 1;
            flowTaskParamter.flowTaskOperatorEntity.HandleTime = DateTime.Now;
            if (flowTaskParamter.freeApproverType == "1")
            {
                handleStatus = 10;
            }
            else
            {
                #region 加签记录
                await CreateOperatorRecode(flowTaskParamter, 6);
                #endregion
            }
        }
        else
        {
            // 当前经办非加签经办或不存在前签
            if (flowTaskParamter.flowTaskOperatorEntity.ParentId.IsNullOrEmpty() || flowTaskParamter.flowTaskOperatorEntity.RollbackId.IsNullOrEmpty() || handleStatus == 0)
            {
                if (flowTaskParamter.approversProperties.counterSign == 0 || IsAchievebilProportion(flowTaskParamter, handleStatus))
                {
                    //未审批经办
                    updateOperatorList = GetNotCompletion(flowTaskParamter.thisFlowTaskOperatorEntityList.FindAll(x => x.Id != flowTaskParamter.flowTaskOperatorEntity.Id));
                }
            }
            else
            {
                flowTaskParamter.flowTaskOperatorEntity.State = 1;
                // 前签发起人是否为初始经办人
                var rollBackOprtator = await _flowTaskRepository.GetTaskOperatorInfo(flowTaskParamter.flowTaskOperatorEntity.RollbackId);
                if (rollBackOprtator.IsNotEmptyOrNull())
                {
                    rollBackOprtator.Id = SnowflakeIdHelper.NextId();
                    rollBackOprtator.State = 0;
                    rollBackOprtator.Completion = 0;
                    rollBackOprtator.HandleStatus = 0;
                    rollBackOprtator.AppendHandleId = flowTaskParamter.flowTaskOperatorEntity.Id;
                    rollBackOprtator.HandleTime = null;
                    await _flowTaskRepository.CreateTaskOperator(rollBackOprtator);
                }
            }
            flowTaskParamter.flowTaskOperatorEntity.HandleStatus = handleStatus;
            flowTaskParamter.flowTaskOperatorEntity.Completion = 1;
            flowTaskParamter.flowTaskOperatorEntity.HandleTime = DateTime.Now;
        }
        updateOperatorList.Add(flowTaskParamter.flowTaskOperatorEntity);
        await _flowTaskRepository.UpdateTaskOperator(updateOperatorList);
        // 经办记录
        await CreateOperatorRecode(flowTaskParamter, handleStatus);
    }

    /// <summary>
    /// 获取未审经办并修改完成状态.
    /// </summary>
    /// <param name="thisFlowTaskOperatorEntityList">当前节点所有经办.</param>
    /// <returns></returns>
    public List<FlowTaskOperatorEntity> GetNotCompletion(List<FlowTaskOperatorEntity> thisFlowTaskOperatorEntityList)
    {
        var notCompletion = thisFlowTaskOperatorEntityList.FindAll(x => x.Completion == 0);
        notCompletion.ForEach(item =>
        {
            item.Completion = 1;
        });
        return notCompletion;
    }

    /// <summary>
    /// 对审批人节点分组.
    /// </summary>
    /// <param name="flowTaskOperatorEntities">所有经办.</param>
    /// <returns></returns>
    public Dictionary<string, List<FlowTaskOperatorEntity>> GroupByOperator(List<FlowTaskOperatorEntity> flowTaskOperatorEntities)
    {
        var dic = new Dictionary<string, List<FlowTaskOperatorEntity>>();
        foreach (var item in flowTaskOperatorEntities.GroupBy(x => x.TaskNodeId))
        {
            dic.Add(item.Key, flowTaskOperatorEntities.FindAll(x => x.TaskNodeId == item.Key));
        }
        return dic;
    }

    /// <summary>
    /// 保存当前未完成节点下个候选人节点的候选人.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    public List<FlowCandidatesEntity> SaveNodeCandidates(FlowTaskParamter flowTaskParamter)
    {
        var flowCandidateList = new List<FlowCandidatesEntity>();
        if (flowTaskParamter.candidateList.IsNotEmptyOrNull())
        {
            foreach (var item in flowTaskParamter.candidateList.Keys)
            {
                var node = flowTaskParamter.flowTaskNodeEntityList.Find(x => x.NodeCode == item);
                if (node != null)
                {
                    flowCandidateList.Add(new FlowCandidatesEntity()
                    {
                        Id = SnowflakeIdHelper.NextId(),
                        TaskId = node.TaskId,
                        TaskNodeId = node.Id,
                        HandleId = _userManager.UserId,
                        Account = _userManager.Account,
                        Candidates = string.Join(",", flowTaskParamter.candidateList[item]),
                        TaskOperatorId = flowTaskParamter.flowTaskOperatorEntity.Id
                    });
                }
            }
            _flowTaskRepository.CreateFlowCandidates(flowCandidateList);
        }
        if (flowTaskParamter.errorRuleUserList.IsNotEmptyOrNull())
        {
            flowCandidateList.Clear();
            foreach (var item in flowTaskParamter.errorRuleUserList.Keys)
            {
                var node = flowTaskParamter.flowTaskNodeEntityList.Find(x => x.NodeCode == item);
                if (node != null)
                {
                    flowCandidateList.Add(new FlowCandidatesEntity()
                    {
                        Id = SnowflakeIdHelper.NextId(),
                        TaskId = node.TaskId,
                        TaskNodeId = node.Id,
                        HandleId = _userManager.UserId,
                        Account = _userManager.Account,
                        Candidates = string.Join(",", flowTaskParamter.errorRuleUserList[item]),
                        TaskOperatorId = flowTaskParamter.flowTaskOperatorEntity.Id
                    });
                }
            }
            _flowTaskRepository.CreateFlowCandidates(flowCandidateList);
        }
        return flowCandidateList;
    }

    /// <summary>
    /// 获取子流程继承父流程的表单数据.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <param name="childTaskProperties">子流程属性.</param>
    /// <returns></returns>
    public async Task<object> GetSubFlowFormData(FlowTaskParamter flowTaskParamter, ChildTaskProperties childTaskProperties)
    {
        var childFlowEngine = _flowTaskRepository.GetFlowTemplateInfo(childTaskProperties.flowId);
        var thisFormId = flowTaskParamter.flowTaskNodeEntity.FormId;
        var nextFormId = childFlowEngine.flowTemplateJson.ToObject<FlowTemplateJsonModel>().properties.ToObject<StartProperties>().formId;
        var mapRule = GetMapRule(childTaskProperties.assignList, flowTaskParamter.flowTaskNodeEntity.NodeCode);
        var formData = flowTaskParamter.formData.ToObject<Dictionary<string, object>>();
        var childFormData = await _runService.SaveDataToDataByFId(thisFormId, nextFormId, mapRule, formData, true);
        return childFormData;
    }

    /// <summary>
    /// 获取流程任务名称.
    /// </summary>
    /// <param name="flowTaskSubmitModel">提交参数.</param>
    /// <returns></returns>
    public async Task GetFlowTitle(FlowTaskSubmitModel flowTaskSubmitModel)
    {
        var flowTemplateJsonModel = flowTaskSubmitModel.flowJsonModel.ToObject<FlowJsonModel>().flowTemplateJson.ToObject<FlowTemplateJsonModel>();
        var startProp = flowTemplateJsonModel.properties.ToObject<StartProperties>();
        var userName = flowTaskSubmitModel.crUser.IsNotEmptyOrNull() ? await _usersService.GetUserName(flowTaskSubmitModel.crUser, false) : _userManager.User.RealName;
        if (startProp.titleType == 1)
        {
            var formDataDic = flowTaskSubmitModel.formData.ToObject<Dictionary<string, object>>();
            formDataDic.Add("@flowFullName", flowTaskSubmitModel.flowJsonModel.fullName);
            formDataDic.Add("@flowFullCode", flowTaskSubmitModel.flowJsonModel.enCode);
            formDataDic.Add("@launchUserName", userName);
            formDataDic.Add("@launchTime", DateTime.Now.ToString("yyyy-MM-dd"));
            foreach (var item in startProp.titleContent.Substring3())
            {
                if (formDataDic.ContainsKey(item) && formDataDic[item] != null)
                {
                    startProp.titleContent = startProp.titleContent?.Replace("{" + item + "}", formDataDic[item].ToString());
                }
                else
                {
                    startProp.titleContent = startProp.titleContent?.Replace("{" + item + "}", string.Empty);
                }
            }
            flowTaskSubmitModel.flowTitle = startProp.titleContent;
        }
        else
        {
            flowTaskSubmitModel.flowTitle = string.Format("{0}的{1}", userName, flowTaskSubmitModel.flowJsonModel.fullName);
        }
    }

    /// <summary>
    /// 添加经办记录.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <param name="handleStatus">审批类型（0：拒绝，1：同意）.</param>
    /// <returns></returns>
    public async Task CreateOperatorRecode(FlowTaskParamter flowTaskParamter, int handleStatus)
    {
        FlowTaskOperatorRecordEntity flowTaskOperatorRecordEntity = new FlowTaskOperatorRecordEntity();
        flowTaskOperatorRecordEntity.HandleTime = DateTime.Now;
        #region 经办记录处理人
        if (handleStatus == 2)//提交
        {
            flowTaskOperatorRecordEntity.HandleId = flowTaskParamter.flowTaskEntity.CreatorUserId;
        }
        else if (handleStatus == 7)//转办
        {
            flowTaskOperatorRecordEntity.HandleId = _userManager.UserId;
        }
        else
        {
            if (flowTaskParamter.flowTaskOperatorEntity.IsNotEmptyOrNull() && flowTaskParamter.flowTaskOperatorEntity.HandleId.IsNotEmptyOrNull() && flowTaskParamter.flowTaskOperatorEntity.HandleId == _userManager.UserId)
            {
                flowTaskOperatorRecordEntity.HandleId = flowTaskParamter.flowTaskOperatorEntity.HandleId;
            }
            else
            {
                flowTaskOperatorRecordEntity.HandleId = _userManager.UserId;
                if (flowTaskParamter.isAuto)
                {
                    if ("JNPF".Equals(flowTaskParamter.flowTaskOperatorEntity.HandleId))
                    {
                        flowTaskOperatorRecordEntity.HandleId = null;
                    }
                    else
                    {
                        flowTaskOperatorRecordEntity.HandleId = flowTaskParamter.flowTaskOperatorEntity.HandleId;
                    }
                }
            }
        }
        #endregion
        flowTaskOperatorRecordEntity.HandleStatus = handleStatus;
        flowTaskOperatorRecordEntity.HandleOpinion = flowTaskParamter.handleOpinion;
        flowTaskOperatorRecordEntity.SignImg = new List<int> { 0, 1, 3, 4, 6, 7, 10 }.Contains(handleStatus) ? flowTaskParamter.signImg : string.Empty;
        flowTaskOperatorRecordEntity.Status = 0;
        flowTaskOperatorRecordEntity.FileList = flowTaskParamter.fileList.ToJsonString();
        flowTaskOperatorRecordEntity.TaskId = flowTaskParamter.flowTaskEntity.Id;
        flowTaskOperatorRecordEntity.NodeCode = flowTaskParamter.flowTaskEntity.ThisStepId;
        flowTaskOperatorRecordEntity.NodeName = flowTaskParamter.flowTaskEntity.ThisStep;
        flowTaskOperatorRecordEntity.OperatorId = flowTaskParamter.freeApproverUserId;
        if (flowTaskParamter.flowTaskOperatorEntity.IsNotEmptyOrNull())
        {
            if (handleStatus == 0 || handleStatus == 1)
            {
                flowTaskOperatorRecordEntity.Status = flowTaskParamter.flowTaskOperatorEntity.ParentId.IsNotEmptyOrNull() ? 1 : 0;
            }
            flowTaskOperatorRecordEntity.NodeCode = flowTaskParamter.flowTaskOperatorEntity.NodeCode;
            flowTaskOperatorRecordEntity.NodeName = flowTaskParamter.flowTaskOperatorEntity.NodeName;
            flowTaskOperatorRecordEntity.TaskOperatorId = flowTaskParamter.flowTaskOperatorEntity.Id;
            flowTaskOperatorRecordEntity.TaskNodeId = flowTaskParamter.flowTaskOperatorEntity.TaskNodeId;
        }
        else if (flowTaskParamter.flowTaskNodeEntity.IsNotEmptyOrNull())
        {
            flowTaskOperatorRecordEntity.NodeCode = flowTaskParamter.flowTaskNodeEntity.NodeCode;
            flowTaskOperatorRecordEntity.NodeName = flowTaskParamter.flowTaskNodeEntity.NodeName;
            flowTaskOperatorRecordEntity.TaskNodeId = flowTaskParamter.flowTaskNodeEntity.Id;
        }
        await _flowTaskRepository.CreateTaskOperatorRecord(flowTaskOperatorRecordEntity);
    }

    /// <summary>
    /// 判断会签人数是否达到会签比例.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <param name="handleStatus">审批类型（0：拒绝，1：同意）.</param>
    /// <returns></returns>
    public bool IsAchievebilProportion(FlowTaskParamter flowTaskParamter, int handleStatus)
    {
        if (flowTaskParamter.thisFlowTaskOperatorEntityList.Count == 0) return true;
        //完成人数（加上当前审批人）
        var comNum = flowTaskParamter.thisFlowTaskOperatorEntityList.FindAll(x => x.HandleStatus == handleStatus && x.Completion == 1 && x.State == 0).Count.ParseToDouble();
        if (flowTaskParamter.freeApproverUserId.IsNullOrEmpty())
        {
            ++comNum;
        }
        //完成比例
        var comRatio = (comNum / flowTaskParamter.thisFlowTaskOperatorEntityList.Count.ParseToDouble() * 100).ParseToInt();
        //会签配置
        var configType = handleStatus == 0 ? flowTaskParamter.approversProperties.counterSignConfig.rejectType : flowTaskParamter.approversProperties.counterSignConfig.auditType;
        var configRatio = handleStatus == 0 ? flowTaskParamter.approversProperties.counterSignConfig.rejectRatio : flowTaskParamter.approversProperties.counterSignConfig.auditRatio;
        var configNum = handleStatus == 0 ? flowTaskParamter.approversProperties.counterSignConfig.rejectNum : flowTaskParamter.approversProperties.counterSignConfig.auditNum;
        if (configType == 0 && handleStatus == 0)
        {
            configType = flowTaskParamter.approversProperties.counterSignConfig.auditType;
            configRatio = 100 - flowTaskParamter.approversProperties.counterSignConfig.auditRatio;
            configNum = flowTaskParamter.thisFlowTaskOperatorEntityList.Count - flowTaskParamter.approversProperties.counterSignConfig.auditNum;
        }
        return configType == 1 ? comRatio >= configRatio : comNum >= configNum;
    }

    /// <summary>
    /// 获取cron表达式.
    /// </summary>
    /// <param name="overTimeDuring">间隔.</param>
    /// <param name="startingTime">起始时间.</param>
    /// <param name="type">0：小时 1：分钟.</param>
    /// <returns></returns>
    public string GetCron(int overTimeDuring, DateTime startingTime, int type = 0)
    {
        if (type == 0)
        {
            return string.Format("{0} {1} 0/{2} * * ?", startingTime.Second, startingTime.Minute, overTimeDuring);
        }
        else
        {
            return string.Format("{0} 0/{1} * * * ?", startingTime.Second, overTimeDuring);
        }
    }

    /// <summary>
    /// 获取起始时间.
    /// </summary>
    /// <param name="timeOutConfig">限时配置.</param>
    /// <param name="receiveTime">接收时间.</param>
    /// <param name="createTime">发起时间.</param>
    /// <param name="formData">表单数据.</param>
    /// <returns></returns>
    public DateTime GetStartingTime(TimeOutConfig timeOutConfig, DateTime receiveTime, DateTime createTime, string formData)
    {
        var dt = DateTime.Now;
        switch (timeOutConfig.nodeLimit)
        {
            case 0:
                dt = receiveTime;
                break;
            case 1:
                dt = createTime;
                break;
            case 2:
                var jobj = formData.ToObjectOld<JObject>();
                if (jobj.ContainsKey(timeOutConfig.formField))
                {
                    try
                    {
                        var value = jobj[timeOutConfig.formField].ToString();
                        if (!DateTime.TryParse(value, out dt))
                        {
                            dt = jobj[timeOutConfig.formField].ParseToLong().TimeStampToDateTime();
                        }
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
                break;
        }
        return dt;
    }

    /// <summary>
    /// 获取下一节点表单初始数据并保存.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <param name="nextNodeList">下一节点.</param>
    /// <returns></returns>
    public async Task<Dictionary<string, object>> GetNextFormData(FlowTaskParamter flowTaskParamter, List<FlowTaskNodeEntity> nextNodeList, int type)
    {
        var nextNodeData = new Dictionary<string, object>();
        var mapRule = new List<Dictionary<string, string>>();
        var thisFormId = flowTaskParamter.flowTaskNodeEntity.FormId;
        foreach (var item in nextNodeList)
        {
            if (FlowTaskNodeTypeEnum.approver.ParseToString().Equals(item.NodeType))
            {


                var approversPro = item.NodePropertyJson.ToObject<ApproversProperties>();
                var nextFormId = item.FormId;
                if (FlowTaskNodeTypeEnum.subFlow.ParseToString().Equals(flowTaskParamter.flowTaskNodeEntity.NodeType))
                {
                    // 传递节点
                    var extendNode = approversPro.assignList.Select(x => x.nodeId).ToList();
                    // 最后审批节点
                    var lastHandleNode = (await _flowTaskRepository.GetTaskOperatorRecordList(x => x.TaskId == flowTaskParamter.flowTaskEntity.Id && (x.HandleStatus == 1 || x.HandleStatus == 2), o => o.HandleTime, SqlSugar.OrderByType.Desc)).FirstOrDefault();
                    if (extendNode.Any())
                    {
                        lastHandleNode = (await _flowTaskRepository.GetTaskOperatorRecordList(x => extendNode.Contains(x.NodeCode) && x.TaskId == flowTaskParamter.flowTaskEntity.Id && (x.HandleStatus == 1 || x.HandleStatus == 2), o => o.HandleTime, SqlSugar.OrderByType.Desc)).FirstOrDefault();
                    }
                    if (lastHandleNode.IsNotEmptyOrNull() && lastHandleNode.NodeCode.IsNotEmptyOrNull())
                    {
                        thisFormId = flowTaskParamter.flowTaskNodeEntityList.Find(x => x.Id == lastHandleNode.TaskNodeId)?.FormId;
                        mapRule = GetMapRule(approversPro.assignList, lastHandleNode.NodeCode);
                    }
                }
                else
                {
                    mapRule = GetMapRule(approversPro.assignList, flowTaskParamter.flowTaskNodeEntity.NodeCode);
                }
                var data = await _runService.GetFlowFormDataDetails(thisFormId, flowTaskParamter.flowTaskEntity.Id);
                if (data == null) data = flowTaskParamter.formData.ToObject<Dictionary<string, object>>();
                if (!data.ContainsKey("id") || data["id"].IsNullOrEmpty())//针对表单自增长优化
                {
                    data["id"] = flowTaskParamter.flowTaskEntity.Id;
                }
                List<FormOperatesModel> formOperates = await _flowTaskRepository.GetFlowFormAuthorize(flowTaskParamter.flowTaskNodeEntity);
                var systemControlList = formOperates.Where(x => !x.write).Select(x => x.id).ToList();
                var nextFormData = await _runService.SaveDataToDataByFId(thisFormId, nextFormId, mapRule, data, false, systemControlList);
            }
        }
        return nextNodeData;
    }

    /// <summary>
    /// 获取表单传递字段.
    /// </summary>
    /// <param name="assignItems">传递规则.</param>
    /// <param name="nodeCode">传递节点编码.</param>
    /// <returns></returns>
    private List<Dictionary<string, string>> GetMapRule(List<AssignItem> assignItems, string nodeCode)
    {
        if (!assignItems.Any()) return new List<Dictionary<string, string>>();
        var ruleList = assignItems.Find(x => x.nodeId == nodeCode)?.ruleList;
        var mapRule = new List<Dictionary<string, string>>();
        if (ruleList.IsNotEmptyOrNull())
        {
            foreach (var item in ruleList)
            {
                if (item.parentField.IsNotEmptyOrNull())
                {
                    var dic = new Dictionary<string, string>();
                    dic.Add(item.parentField, item.childField);
                    mapRule.Add(dic);
                }
            }
        }
        return mapRule;
    }

    /// <summary>
    /// 终止任务(包含子流程任务).
    /// </summary>
    /// <param name="flowTaskEntity">当前任务.</param>
    /// <returns></returns>
    public async Task CancelTask(FlowTaskEntity flowTaskEntity)
    {
        if (flowTaskEntity.Suspend == 1) throw Oops.Oh(ErrorCode.WF0046);
        flowTaskEntity.Status = FlowTaskStatusEnum.Cancel.ParseToInt();
        flowTaskEntity.EndTime = DateTime.Now;
        await _flowTaskRepository.UpdateTask(flowTaskEntity);
        foreach (var item in await _flowTaskRepository.GetTaskNodeList(x => x.TaskId == flowTaskEntity.Id))
        {
            _schedulerFactory.RemoveJob("Job_CS_" + item.Id);
            _schedulerFactory.RemoveJob("Job_TX_" + item.Id);
        }
        var childTaskList = await _flowTaskRepository.GetTaskList(x => flowTaskEntity.Id == x.ParentId && x.DeleteMark == null);
        foreach (var item in childTaskList)
        {
            await CancelTask(item);
        }
    }

    /// <summary>
    /// 同步发起配置.
    /// </summary>
    /// <param name="approversPro"></param>
    /// <param name="startPro"></param>
    /// <returns></returns>
    public ApproversProperties SyncApproProCofig(ApproversProperties approversPro, StartProperties startPro)
    {
        approversPro.timeLimitConfig = approversPro.timeLimitConfig.on == 2 ? startPro.timeLimitConfig : approversPro.timeLimitConfig;//限时配置
        approversPro.noticeConfig = approversPro.noticeConfig.on == 2 ? startPro.noticeConfig : approversPro.noticeConfig;//提醒配置
        approversPro.overTimeConfig = approversPro.overTimeConfig.on == 2 ? startPro.overTimeConfig : approversPro.overTimeConfig;//超时配置
        approversPro.approveMsgConfig = approversPro.approveMsgConfig.on == 2 ? startPro.approveMsgConfig : approversPro.approveMsgConfig;//同意
        approversPro.rejectMsgConfig = approversPro.rejectMsgConfig.on == 2 ? startPro.rejectMsgConfig : approversPro.rejectMsgConfig;//退回
        approversPro.copyMsgConfig = approversPro.copyMsgConfig.on == 2 ? startPro.copyMsgConfig : approversPro.copyMsgConfig;//抄送
        approversPro.overTimeMsgConfig = approversPro.overTimeMsgConfig.on == 2 ? startPro.overTimeMsgConfig : approversPro.overTimeMsgConfig;//超时
        approversPro.noticeMsgConfig = approversPro.noticeMsgConfig.on == 2 ? startPro.noticeMsgConfig : approversPro.noticeMsgConfig;//提醒
        return approversPro;
    }

    /// <summary>
    /// 去除只读字段.
    /// </summary>
    /// <param name="flowTaskParamter"></param>
    public void RemoveNoWriteData(FlowTaskParamter flowTaskParamter)
    {
        var formData = flowTaskParamter.formData.ToObject<Dictionary<string, object>>();
        var formOperates = flowTaskParamter.approversProperties.formOperates.ToObject<List<FormOperatesModel>>();
        var childTableIds = formOperates.FindAll(x => x.jnpfKey.Equals("table")).Select(x => x.id).ToList();
        var childKeyList = new Dictionary<string, List<string>>();
        foreach (var item in formOperates)
        {
            if (!item.write && formData.ContainsKey(item.id) && !item.jnpfKey.Equals("table"))
            {
                formData.Remove(item.id);
            }
        }
        foreach (var item in childTableIds)
        {
            var childKey = formOperates.Where(x => x.id.Contains(item + "-") && !x.write).Select(x => x.id.Replace(item + "-", string.Empty)).ToList();
            childKeyList.Add(item, childKey);
        }

        // 去除字表只读字段数据
        foreach (var item in childTableIds)
        {
            if (formData.ContainsKey(item))
            {
                var childFormData = formData[item].ToObject<List<Dictionary<string, object>>>();
                foreach (var item1 in childFormData)
                {
                    foreach (var item2 in childKeyList[item])
                    {
                        item1.Remove(item2);
                    }
                }
                formData[item] = childFormData;
            }
        }
        flowTaskParamter.formData = formData;
    }

    /// <summary>
    /// 保存节点表单权限并从节点属性中剔除.
    /// </summary>
    /// <param name="flowTaskNodeEntities"></param>
    /// <returns></returns>
    public async Task SaveFormOperates(List<FlowTaskNodeEntity> flowTaskNodeEntities, bool IsSave = true)
    {
        var formOperates = new List<FlowFormAuthorizeEntity>();
        foreach (var item in flowTaskNodeEntities)
        {
            var propDictionary = item.NodePropertyJson.ToObject<Dictionary<string, object>>();
            if (!FlowTaskNodeTypeEnum.subFlow.ParseToString().Equals(item.NodeType) && propDictionary.ContainsKey("formOperates"))
            {
                var formOperateList = propDictionary["formOperates"].ToObject<List<object>>();
                formOperates.Add(new FlowFormAuthorizeEntity
                {
                    Id = SnowflakeIdHelper.NextId(),
                    TaskId = item.TaskId,
                    NodeCode = item.NodeCode,
                    FormOperate = formOperateList.ToJsonStringOld()
                });
                propDictionary.Remove("formOperates");
                item.NodePropertyJson = propDictionary.ToJsonStringOld();
            }
        }
        if (IsSave)
        {
            await _flowTaskRepository.CreateFlowFormAuthorize(formOperates);
        }
    }
}
