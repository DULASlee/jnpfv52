using JNPF.ClayObject;
using JNPF.Common.Configuration;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Dtos.Datainterface;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Models.InteAssistant;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.Engine.Entity.Model.Integrate;
using JNPF.InteAssistant.Engine.Dto;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.JsonSerialization;
using JNPF.RemoteRequest.Extensions;
using JNPF.Systems.Entitys.Permission;
using JNPF.UnifyResult;
using Mapster;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace JNPF.InteAssistant.Engine;

/// <summary>
/// 集成助手-运行核心.
/// </summary>
public class InteAssistantRun : ITransient, IDisposable
{
    /// <summary>
    /// 服务提供器.
    /// </summary>
    private readonly IServiceScope _serviceScope;

    private readonly ICacheManager _cacheManager;

    private readonly ISqlSugarClient _sqlSugarClient;

    /// <summary>
    /// 租户管理.
    /// </summary>
    private readonly ITenantManager _tenantManager;

    /// <summary>
    /// 初始化一个<see cref="InteAssistantRun"/>类型的新实例.
    /// </summary>
    public InteAssistantRun(
        IServiceScopeFactory serviceScopeFactory,
        ICacheManager cacheManager,
        ITenantManager tenantManager,
        ISqlSugarClient sqlSugarClient)
    {
        _serviceScope = serviceScopeFactory.CreateScope();
        _sqlSugarClient = sqlSugarClient;
        _cacheManager = cacheManager;
        _tenantManager = tenantManager;
    }

    /// <summary>
    /// 集成助手任务纲要.
    /// </summary>
    /// <param name="model">集成助手模版.</param>
    /// <param name="inteAssisType">集成助手类型 1-触发,2-定时.</param>
    /// <param name="integrateId">集成助手ID.</param>
    public async Task<InteAssiTaskOutlineModel> InteAssiTaskOutline(DesignModel model, int inteAssisType, string integrateId)
    {
        var inteTaskOutline = new InteAssiTaskOutlineModel
        {
            integrateId = integrateId,
            formId = model.properties.formId,
            ruleList = model.properties.ruleList,
            ruleMatchLogic = model.properties.ruleMatchLogic,
            triggerEvent = model.properties.triggerEvent,
            startTime = DateTime.Now,
            inteAssisType = inteAssisType,
            nodeAttributes = GetNodeGeneralitieList(model)
        };
        return inteTaskOutline;
    }

    /// <summary>
    /// 获取集成助手节点列表.
    /// </summary>
    /// <param name="taskId">任务ID.</param>
    /// <param name="formId">表单ID.</param>
    /// <param name="creatorUserId">创建人员ID.</param>
    /// <param name="tenantId">租户ID.</param>
    /// <param name="inteAssisType">集成助手类型.</param>
    /// <param name="fullName">集成助手名称.</param>
    /// <param name="templateJson">集成模版.</param>
    /// <param name="dataList">数据列表.</param>
    /// <param name="taskOutlineNodeList">任务纲要节点列表.</param>
    /// <returns></returns>
    public async Task<InteAssisantRunModel> GetIntegrateNodeList(string taskId, string formId, string creatorUserId, string tenantId, int inteAssisType, string fullName, DesignModel templateJson, List<InteAssiDataModel> dataList, List<TaskOutlineNode> taskOutlineNodeList, IntegrateTaskEntity? taskEntity = null)
    {
        if (string.IsNullOrEmpty(creatorUserId))
        {
            if (KeyVariable.MultiTenancy && !string.IsNullOrEmpty(tenantId))
            {
                await _tenantManager.ChangTenant((SqlSugarScope)_sqlSugarClient, tenantId);
            }

            var userEntity = await _sqlSugarClient.CopyNew().Queryable<UserEntity>().FirstAsync(it => it.Account.Equals("admin"));
            creatorUserId = userEntity.Id;
        }
        var taskData = dataList.ToJsonString();
        var nodeList = new List<IntegrateNodeEntity>();

        foreach (var item in taskOutlineNodeList)
        {
            if (nodeList.Any(it => it.ResultType.Equals(0)))
                break;
            var nodeId = SnowflakeIdHelper.NextId();
            var nodeData = new IntegrateNodeEntity
            {
                Id = nodeId,
                TaskId = taskId,
                FormId = formId,
                NodeType = item.type.ToString(),
                StartTime = DateTime.Now,
                NodeCode = item.nodeId,
                NodeName = item.title,
                NodeNext = item.nextNodeId,
                EnabledMark = 1,
                CreatorTime = DateTime.Now,
                IsRetry = 1,
            };
            switch (item.type)
            {
                case NodeType.addData:
                    {
                        RESTfulResult<object> result = new RESTfulResult<object>();

                        try
                        {
                            switch (inteAssisType)
                            {
                                case 1:
                                case 3:
                                    {
                                        var data = dataList.FirstOrDefault();

                                        var targetForm = GetTargetForm(data.Data, item.transferList);

                                        if (item.enableFlow == 1)
                                            targetForm.Add("flowId", item.flowId);

                                        string parameter = new { id = string.Empty, data = targetForm.ToJsonString(), isInteAssis = false }.ToJsonString();
                                        var requestAddress = string.Format("/api/visualdev/OnlineDev/{0}", item.formId);

                                        result = await InteAssistantHttpClient(1, requestAddress, parameter, creatorUserId, tenantId);
                                    }

                                    break;
                                case 2:
                                    {
                                        var ndoeErrorMarking = false;
                                        if (dataList.Count == 0)
                                        {
                                            result = new RESTfulResult<object>
                                            {
                                                code = 200,
                                            };
                                        }
                                        foreach (var data in dataList)
                                        {
                                            List<string> ids = new List<string>();
                                            var ruleList = GetRuleList(item.ruleList, data.Data);

                                            string superQuery = new { matchLogic = item.ruleMatchLogic, conditionList = ruleList }.ToJsonString();
                                            string parameter = new { currentPage = 1, modelId = item.formId, pageSize = 999999, superQueryJson = superQuery, isOnlyId = 1 }.ToJsonString();
                                            var requestAddress = string.Format("/api/visualdev/OnlineDev/{0}/List", item.formId);

                                            result = await InteAssistantHttpClient(1, requestAddress, parameter, creatorUserId, tenantId);

                                            var resultData = Clay.Parse(result.data.ToString());
                                            List<object> idList = resultData.list.Deserialize<List<object>>();

                                            switch (idList.Count > 0)
                                            {
                                                case true:
                                                    switch (item.addRule)
                                                    {
                                                        case 0:

                                                            if (result.code == 200)
                                                            {
                                                                break;
                                                            }
                                                            break;
                                                        case 1:
                                                            var targetForm = GetTargetForm(data.Data, item.transferList);

                                                            if (item.enableFlow == 1)
                                                                targetForm.Add("flowId", item.flowId);

                                                            parameter = new { id = string.Empty, data = targetForm.ToJsonString(), isInteAssis = false }.ToJsonString();
                                                            requestAddress = string.Format("/api/visualdev/OnlineDev/{0}", item.formId);

                                                            result = await InteAssistantHttpClient(1, requestAddress, parameter, creatorUserId, tenantId);

                                                            break;
                                                    }

                                                    break;
                                            }

                                            if (result.code != 200)
                                            {
                                                ndoeErrorMarking = true;
                                            }
                                        }

                                        if (ndoeErrorMarking)
                                        {
                                            result = new RESTfulResult<object>
                                            {
                                                code = 400,
                                                msg = "执行异常"
                                            };
                                        }
                                    }

                                    break;
                            }
                        }
                        catch (Exception)
                        {
                            result = new RESTfulResult<object>
                            {
                                code = 400,
                                msg = "执行异常"
                            };
                        }

                        nodeData.EndTime = DateTime.Now;
                        nodeData.ResultType = result.code == 200 ? 1 : 0;
                        nodeData.ErrorMsg = result?.code == 200 ? null : result.msg.ToString();
                        nodeList.Add(nodeData);

                        switch (nodeData.ResultType)
                        {
                            case 0:
                                // 推送消息
                                await ExecuteNotice(templateJson.properties.failMsgConfig, "MBXTJC001", fullName, templateJson.properties.msgUserType, templateJson.properties.msgUserIds, creatorUserId, tenantId, taskEntity);
                                break;
                        }
                    }

                    break;
                case NodeType.dataInterface:
                    {
                        RESTfulResult<object> result = new RESTfulResult<object>();
                        try
                        {
                            switch (inteAssisType)
                            {
                                case 1:
                                case 3:
                                    {
                                        var data = dataList.FirstOrDefault();
                                        var targetForm = new Dictionary<string, object>();
                                        targetForm.Add("@formId", data.DataId);
                                        var input = new DataInterfacePreviewInput
                                        {
                                            paramList = item.templateJson.Adapt<List<DataInterfaceParameter>>(),
                                            sourceData = data.Data,
                                            systemParamter = targetForm,
                                        };
                                        var scheduleTaskModel = new ScheduleTaskModel();
                                        scheduleTaskModel.taskParams.Add("id", item.formId);
                                        scheduleTaskModel.taskParams.Add("input", input.ToJsonString());

                                        var requestAddress = string.Format("/ScheduleTask/datainterface");

                                        result = await InteAssistantHttpClient(1, requestAddress, scheduleTaskModel, creatorUserId, tenantId);
                                    }

                                    break;
                                case 2:
                                    {
                                        foreach (var data in dataList)
                                        {
                                            var targetForm = new Dictionary<string, object>();
                                            targetForm.Add("@formId", data.DataId);
                                            var input = new DataInterfacePreviewInput
                                            {
                                                paramList = item.templateJson.Adapt<List<DataInterfaceParameter>>(),
                                                sourceData = data.Data,
                                                systemParamter = targetForm,
                                            };
                                            var scheduleTaskModel = new ScheduleTaskModel();
                                            scheduleTaskModel.taskParams.Add("id", item.formId);
                                            scheduleTaskModel.taskParams.Add("input", input.ToJsonString());

                                            var requestAddress = string.Format("/ScheduleTask/datainterface");

                                            result = await InteAssistantHttpClient(1, requestAddress, scheduleTaskModel, creatorUserId, tenantId);
                                        }
                                    }

                                    break;
                            }
                        }
                        catch (Exception)
                        {
                            result = new RESTfulResult<object>
                            {
                                code = 400,
                                msg = "执行异常"
                            };
                        }

                        nodeData.EndTime = DateTime.Now;
                        nodeData.ResultType = result.code == 200 ? 1 : 0;
                        nodeData.ErrorMsg = result.code == 200 ? null : result.msg.ToString();
                        nodeList.Add(nodeData);

                        switch (nodeData.ResultType)
                        {
                            case 0:
                                // 推送消息
                                await ExecuteNotice(templateJson.properties.failMsgConfig, "MBXTJC001", fullName, templateJson.properties.msgUserType, templateJson.properties.msgUserIds, creatorUserId, tenantId, taskEntity);
                                break;
                        }
                    }

                    break;
                case NodeType.deleteData:
                    {
                        var ids = new List<string>();
                        RESTfulResult<object> result = new RESTfulResult<object>();
                        try
                        {
                            switch (inteAssisType)
                            {
                                case 1:
                                case 3:
                                    {
                                        var data = dataList.FirstOrDefault();

                                        var superQuery = string.Empty;
                                        if (item.ruleList.Count > 0)
                                        {
                                            var ruleList = GetRuleList(item.ruleList, data.Data);
                                            superQuery = new { matchLogic = item.ruleMatchLogic, conditionList = ruleList }.ToJsonString();
                                        }

                                        string parameter = new { currentPage = 1, modelId = item.formId, pageSize = 999999, superQueryJson = superQuery, isOnlyId = 1 }.ToJsonString();
                                        var requestAddress = string.Format("/api/visualdev/OnlineDev/{0}/List", item.formId);

                                        result = await InteAssistantHttpClient(1, requestAddress, parameter, creatorUserId, tenantId);
                                        var resultData = Clay.Parse(result.data.ToString());

                                        List<InteAssisOnlyIdListResultModel> idList = resultData.list.Deserialize<List<InteAssisOnlyIdListResultModel>>();
                                        foreach (var id in idList)
                                        {
                                            ids.Add(id.id);
                                        }
                                    }

                                    break;
                                case 2:
                                    {
                                        foreach (var data in dataList)
                                        {
                                            var ruleList = GetRuleList(item.ruleList, data.Data);

                                            string superQuery = new { matchLogic = item.ruleMatchLogic, conditionList = ruleList }.ToJsonString();
                                            string parameter = new { currentPage = 1, modelId = item.formId, pageSize = 999999, superQueryJson = superQuery, isOnlyId = 1 }.ToJsonString();
                                            var requestAddress = string.Format("/api/visualdev/OnlineDev/{0}/List", item.formId);

                                            result = await InteAssistantHttpClient(1, requestAddress, parameter, creatorUserId, tenantId);
                                            var resultData = Clay.Parse(result.data.ToString());
                                            List<InteAssisOnlyIdListResultModel> idList = resultData.list.Deserialize<List<InteAssisOnlyIdListResultModel>>();
                                            foreach (var id in idList)
                                            {
                                                ids.Add(id.id);
                                            }
                                        }
                                    }

                                    break;
                            }
                        }
                        catch (Exception)
                        {
                            result = new RESTfulResult<object>
                            {
                                code = 400,
                                msg = "执行异常"
                            };
                        }

                        // 定时触发时需要去重
                        switch (inteAssisType)
                        {
                            case 2:
                                ids = ids.Distinct().ToList();
                                break;
                        }

                        // 批量删除
                        switch (ids.Count > 0)
                        {
                            case true:

                                string parameter = string.Empty;
                                switch (inteAssisType)
                                {
                                    case 1:
                                        {
                                            parameter = new { ids, isInteAssis = false }.ToJsonString();
                                        }

                                        break;
                                    case 2:
                                        {
                                            parameter = new { ids, isInteAssis = false, deleteRule = item.deleteRule }.ToJsonString();
                                        }

                                        break;
                                }
                                var requestAddress = string.Format("/api/visualdev/OnlineDev/batchDelete/{0}", item.formId);

                                result = await InteAssistantHttpClient(1, requestAddress, parameter, creatorUserId, tenantId);

                                break;
                            default:
                                result = new RESTfulResult<object>
                                {
                                    code = 200,
                                };
                                break;
                        }

                        nodeData.EndTime = DateTime.Now;
                        nodeData.ResultType = result.code == 200 ? 1 : 0;
                        nodeData.ErrorMsg = result.code == 200 ? null : result.msg.ToString();
                        nodeList.Add(nodeData);

                        switch (nodeData.ResultType)
                        {
                            case 0:
                                // 推送消息
                                await ExecuteNotice(templateJson.properties.failMsgConfig, "MBXTJC001", fullName, templateJson.properties.msgUserType, templateJson.properties.msgUserIds, creatorUserId, tenantId,taskEntity);
                                break;
                        }

                    }

                    break;
                case NodeType.end:
                    {
                        nodeData.EndTime = DateTime.Now;
                        nodeData.ResultType = 1;
                        nodeData.IsRetry = 1;
                        nodeList.Add(nodeData);
                    }

                    break;
                case NodeType.getData:
                    {
                        RESTfulResult<object> result = new RESTfulResult<object>();
                        dataList = new List<InteAssiDataModel>();
                        try
                        {
                            switch (item.formType)
                            {
                                case 1:
                                case 2:
                                    {
                                        var superQuery = string.Empty;
                                        if (item.ruleList.Count > 0)
                                            superQuery = new { matchLogic = item.ruleMatchLogic, conditionList = item.ruleList }.ToJsonString();
                                        string parameter = new { currentPage = 1, modelId = item.formId, pageSize = 999999, superQueryJson = superQuery, isProcessReviewCompleted = item.formType.Equals(1) ? 0 : 1, isConvertData = 1 }.ToJsonString();
                                        var requestAddress = string.Format("/api/visualdev/OnlineDev/{0}/List", item.formId);

                                        result = await InteAssistantHttpClient(1, requestAddress, parameter, creatorUserId, tenantId);
                                        var resultData = Clay.Parse(result.data.ToString());
                                        string list = resultData.list.ToString();
                                        var idList = list.ToObject<List<Dictionary<string, object>>>();
                                        foreach (var subtable in idList)
                                        {
                                            dataList.Add(new InteAssiDataModel
                                            {
                                                Data = subtable.ToJsonString(),
                                            });
                                        }
                                    }

                                    break;
                                case 3:
                                    {
                                        var input = new DataInterfacePreviewInput
                                        {
                                            paramList = item.interfaceTemplateJson.Adapt<List<DataInterfaceParameter>>()
                                        };

                                        var scheduleTaskModel = new ScheduleTaskModel();
                                        scheduleTaskModel.taskParams.Add("id", item.formId);
                                        scheduleTaskModel.taskParams.Add("input", input.ToJsonString());

                                        var requestAddress = string.Format("/ScheduleTask/datainterface");

                                        result = await InteAssistantHttpClient(1, requestAddress, scheduleTaskModel, creatorUserId, tenantId);
                                        if (result.data.IsNotEmptyOrNull())
                                        {
                                            var resultData = Clay.Parse(result.data.ToString());
                                            IEnumerable<dynamic> idList = resultData.AsEnumerator<dynamic>();
                                            foreach (var subtable in idList)
                                            {
                                                Dictionary<string, object> data = subtable.ToDictionary();
                                                var formFieldList = item.formFieldList.Adapt<List<InterfaceFieldModel>>();
                                                var newData = new Dictionary<string, object>();
                                                foreach (var field in formFieldList)
                                                {
                                                    if (!field.id.Equals("@formId"))
                                                        newData[field.id] = data[field.defaultValue];
                                                }

                                                dataList.Add(new InteAssiDataModel
                                                {
                                                    Data = newData.ToJsonString(),
                                                });
                                            }
                                        }
                                    }

                                    break;
                            }
                        }
                        catch (Exception)
                        {
                            result = new RESTfulResult<object>
                            {
                                code = 400,
                                msg = "执行异常"
                            };
                        }

                        taskData = dataList.ToJsonString();

                        nodeData.EndTime = DateTime.Now;
                        nodeData.ResultType = result.code == 200 ? 1 : 0;
                        nodeData.ErrorMsg = result.code == 200 ? null : result.msg.ToString();
                        nodeList.Add(nodeData);

                        switch (nodeData.ResultType)
                        {
                            case 0:
                                // 推送消息
                                await ExecuteNotice(templateJson.properties.failMsgConfig, "MBXTJC001", fullName, templateJson.properties.msgUserType, templateJson.properties.msgUserIds, creatorUserId, tenantId, taskEntity);
                                break;
                        }
                    }

                    break;
                case NodeType.message:
                    {
                        RESTfulResult<object> result = new RESTfulResult<object>();
                        switch (inteAssisType)
                        {
                            case 1:
                            case 3:
                                {
                                    string parameter = new { templateJson = item.templateJson, msgUserIds = item.msgUserIds, data = dataList.FirstOrDefault().Data }.ToJsonString();
                                    var requestAddress = string.Format("/api/VisualDev/Integrate/MessageNotice");

                                    result = await InteAssistantHttpClient(1, requestAddress, parameter, creatorUserId, tenantId);
                                }

                                break;
                            case 2:
                                var ndoeErrorMarking = false;
                                foreach (var data in dataList)
                                {
                                    string parameter = new { templateJson = item.templateJson, msgUserIds = item.msgUserIds, data = data.Data }.ToJsonString();
                                    var requestAddress = string.Format("/api/VisualDev/Integrate/MessageNotice");

                                    result = await InteAssistantHttpClient(1, requestAddress, parameter, creatorUserId, tenantId);

                                    if (result.code != 200)
                                    {
                                        ndoeErrorMarking = true;
                                    }
                                }

                                if (ndoeErrorMarking)
                                {
                                    result = new RESTfulResult<object>
                                    {
                                        code = 400,
                                        msg = "执行异常"
                                    };
                                }

                                break;
                        }

                        nodeData.EndTime = DateTime.Now;
                        nodeData.ResultType = result.code == 200 ? 1 : 0;
                        nodeData.ErrorMsg = result.code == 200 ? null : result.msg.ToString();
                        nodeList.Add(nodeData);

                        switch (nodeData.ResultType)
                        {
                            case 0:
                                // 推送消息
                                await ExecuteNotice(templateJson.properties.failMsgConfig, "MBXTJC001", fullName, templateJson.properties.msgUserType, templateJson.properties.msgUserIds, creatorUserId, tenantId, taskEntity);
                                break;
                        }
                    }

                    break;
                case NodeType.start:
                    {
                        nodeData.EndTime = DateTime.Now;
                        nodeData.ResultType = 1;
                        nodeList.Add(nodeData);

                        // 推送消息
                        await ExecuteNotice(templateJson.properties.startMsgConfig, "MBXTJC002", fullName, templateJson.properties.msgUserType, templateJson.properties.msgUserIds, creatorUserId, tenantId, taskEntity);
                    }

                    break;
                case NodeType.updateData:
                    {
                        var ids = new List<string>();
                        RESTfulResult<object> result = new RESTfulResult<object>();
                        try
                        {
                            switch (inteAssisType)
                            {
                                case 1:
                                case 3:
                                    {
                                        var data = dataList.FirstOrDefault();

                                        var superQuery = string.Empty;
                                        if (item.ruleList.Count > 0)
                                        {
                                            var ruleList = GetRuleList(item.ruleList, data.Data);
                                            superQuery = new { matchLogic = item.ruleMatchLogic, conditionList = ruleList }.ToJsonString();
                                        }

                                        string parameter = new { currentPage = 1, modelId = item.formId, pageSize = 999999, superQueryJson = superQuery, isOnlyId = 1 }.ToJsonString();
                                        var requestAddress = string.Format("/api/visualdev/OnlineDev/{0}/List", item.formId);

                                        result = await InteAssistantHttpClient(1, requestAddress, parameter, creatorUserId, tenantId);
                                        var resultData = Clay.Parse(result.data.ToString());
                                        List<InteAssisOnlyIdListResultModel> idList = resultData.list.Deserialize<List<InteAssisOnlyIdListResultModel>>();
                                        foreach (var id in idList)
                                        {
                                            ids.Add(id.id);
                                        }

                                        var targetForm = GetTargetForm(data.Data, item.transferList);
                                        result = new RESTfulResult<object>();

                                        switch (ids.Count == 0)
                                        {
                                            case true:
                                                switch (item.unFoundRule)
                                                {
                                                    case 0:
                                                        result = new RESTfulResult<object>
                                                        {
                                                            code = 200,
                                                        };
                                                        break;
                                                    case 1:
                                                        {
                                                            parameter = new { id = string.Empty, data = targetForm.ToJsonString(), isInteAssis = false }.ToJsonString();
                                                            requestAddress = string.Format("/api/visualdev/OnlineDev/{0}", item.formId);
                                                            result = await InteAssistantHttpClient(1, requestAddress, parameter, creatorUserId, tenantId);
                                                        }

                                                        break;
                                                }

                                                break;
                                            default:
                                                {
                                                    parameter = new { idList = ids, data = targetForm.ToJsonString(), isInteAssis = false }.ToJsonString();
                                                    requestAddress = string.Format("/api/visualdev/OnlineDev/batchUpdate/{0}", item.formId);
                                                    result = await InteAssistantHttpClient(2, requestAddress, parameter, creatorUserId, tenantId);
                                                }

                                                break;
                                        }
                                    }

                                    break;
                                case 2:
                                    {
                                        var ndoeErrorMarking = false;
                                        foreach (var data in dataList)
                                        {
                                            var ruleList = GetRuleList(item.ruleList, data.Data);
                                            string superQuery = new { matchLogic = item.ruleMatchLogic, conditionList = ruleList }.ToJsonString();
                                            string parameter = new { currentPage = 1, modelId = item.formId, pageSize = 999999, superQueryJson = superQuery, isOnlyId = 1 }.ToJsonString();
                                            var requestAddress = string.Format("/api/visualdev/OnlineDev/{0}/List", item.formId);

                                            result = await InteAssistantHttpClient(1, requestAddress, parameter, creatorUserId, tenantId);
                                            var resultData = Clay.Parse(result.data.ToString());
                                            List<InteAssisOnlyIdListResultModel> idList = resultData.list.Deserialize<List<InteAssisOnlyIdListResultModel>>();
                                            foreach (var id in idList)
                                            {
                                                ids.Add(id.id);
                                            }

                                            var targetForm = GetTargetForm(data.Data, item.transferList);
                                            result = new RESTfulResult<object>();

                                            switch (ids.Count == 0)
                                            {
                                                case true:
                                                    switch (item.unFoundRule)
                                                    {
                                                        case 0:
                                                            result = new RESTfulResult<object>
                                                            {
                                                                code = 200,
                                                            };
                                                            break;
                                                        case 1:
                                                            {
                                                                parameter = new { id = string.Empty, data = targetForm.ToJsonString(), isInteAssis = false }.ToJsonString();
                                                                requestAddress = string.Format("/api/visualdev/OnlineDev/{0}", item.formId);
                                                                result = await InteAssistantHttpClient(1, requestAddress, parameter, creatorUserId, tenantId);
                                                            }

                                                            break;
                                                    }
                                                    break;
                                                default:
                                                    parameter = new { idList = ids, data = targetForm.ToJsonString(), isInteAssis = false }.ToJsonString();
                                                    requestAddress = string.Format("/api/visualdev/OnlineDev/batchUpdate/{0}", item.formId);
                                                    result = await InteAssistantHttpClient(2, requestAddress, parameter, creatorUserId, tenantId);
                                                    break;
                                            }

                                            if (result.code != 200)
                                            {
                                                ndoeErrorMarking = true;
                                            }
                                        }

                                        if (ndoeErrorMarking)
                                        {
                                            result = new RESTfulResult<object>
                                            {
                                                code = 400,
                                                msg = "执行异常"
                                            };
                                        }
                                    }

                                    break;
                            }
                        }
                        catch (Exception)
                        {
                            result = new RESTfulResult<object>
                            {
                                code = 400,
                                msg = "执行异常"
                            };
                        }

                        nodeData.EndTime = DateTime.Now;
                        nodeData.ResultType = result.code == 200 ? 1 : 0;
                        nodeData.ErrorMsg = result.code == 200 ? null : result.msg?.ToString();
                        nodeList.Add(nodeData);

                        switch (nodeData.ResultType)
                        {
                            case 0:
                                // 推送消息
                                await ExecuteNotice(templateJson.properties.failMsgConfig, "MBXTJC001", fullName, templateJson.properties.msgUserType, templateJson.properties.msgUserIds, creatorUserId, tenantId, taskEntity);
                                break;
                        }
                    }

                    break;
                case NodeType.launchFlow:
                    {
                        RESTfulResult<object> result = new RESTfulResult<object>();

                        try
                        {
                            switch (inteAssisType)
                            {
                                case 1:
                                case 3:
                                    {
                                        var data = dataList.FirstOrDefault();

                                        var targetForm = GetTargetForm(data.Data, item.transferList);

                                        string parameter = new { candidateType = 3, countersignOver = true, flowId = item.flowId, formData = targetForm, status = 0, autoSubmit = true }.ToJsonString();

                                        var requestAddress = string.Format("/api/workflow/Engine/FlowTask");

                                        foreach (var initiator in item.initiator)
                                        {
                                            result = await InteAssistantHttpClient(1, requestAddress, parameter, initiator, tenantId);
                                        }
                                    }

                                    break;
                                case 2:
                                    {
                                        var ndoeErrorMarking = false;
                                        if (dataList.Count == 0)
                                        {
                                            result = new RESTfulResult<object>
                                            {
                                                code = 200,
                                            };
                                        }

                                        foreach (var data in dataList)
                                        {
                                            var targetForm = GetTargetForm(data.Data, item.transferList);

                                            string parameter = new { candidateType = 3, countersignOver = true, flowId = item.flowId, formData = targetForm, status = 0, autoSubmit = true }.ToJsonString();

                                            var requestAddress = string.Format("/api/workflow/Engine/FlowTask");

                                            foreach (var initiator in item.initiator)
                                            {
                                                result = await InteAssistantHttpClient(1, requestAddress, parameter, initiator, tenantId);
                                            }
                                        }

                                        if (ndoeErrorMarking)
                                        {
                                            result = new RESTfulResult<object>
                                            {
                                                code = 400,
                                                msg = "执行异常"
                                            };
                                        }
                                    }

                                    break;
                            }
                        }
                        catch (Exception)
                        {
                            result = new RESTfulResult<object>
                            {
                                code = 400,
                                msg = "执行异常"
                            };
                        }

                        nodeData.EndTime = DateTime.Now;
                        nodeData.ResultType = result.code == 200 ? 1 : 0;
                        nodeData.ErrorMsg = result?.code == 200 ? null : result.msg.ToString();
                        nodeList.Add(nodeData);

                        switch (nodeData.ResultType)
                        {
                            case 0:
                                // 推送消息
                                await ExecuteNotice(templateJson.properties.failMsgConfig, "MBXTJC001", fullName, templateJson.properties.msgUserType, templateJson.properties.msgUserIds, creatorUserId, tenantId, taskEntity);
                                break;
                        }
                    }

                    break;

            }
        }

        return new InteAssisantRunModel
        {
            TaskData = taskData,
            NodeEntity = nodeList
        };
    }

    /// <summary>
    /// 获取公共节点.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public List<TaskOutlineNode> GetNodeGeneralitieList(DesignModel model)
    {
        List<TaskOutlineNode> list = new List<TaskOutlineNode>();
        var node = model.properties.Adapt<TaskOutlineNode>();
        node.type = model.type;
        node.nodeId = model.nodeId;
        switch (model.childNode != null)
        {
            case true:
                node.nextNodeId = model.childNode.nodeId;
                break;
            case false:
                node.nextNodeId = "end";
                break;

        }

        list.Add(node);
        switch (model.childNode != null)
        {
            case true:
                list.AddRange(GetNodeGeneralitieList(model.childNode));
                break;
            case false:
                list.Add(new TaskOutlineNode
                {
                    type = NodeType.end,
                    nodeId = "end",
                    title = "结束",
                });
                break;

        }

        return list;
    }

    /// <summary>
    /// 获取当前进程地址.
    /// </summary>
    /// <returns></returns>
    private string GetLocalAddress()
    {
        var server = _serviceScope.ServiceProvider.GetRequiredService<IServer>();
        var addressesFeature = server.Features.Get<IServerAddressesFeature>();
        var addresses = addressesFeature?.Addresses;
        return addresses?.FirstOrDefault()?.Replace("[::]", "localhost");
    }

    /// <summary>
    /// 执行任务过程通知.
    /// </summary>
    /// <param name="mesConfig">信息配置.</param>
    /// <param name="msgEnCode">消息编码.</param>
    /// <param name="defaultTitle">默认标题.</param>
    /// <param name="msgUserType">通知人类型.</param>
    /// <param name="msgUserIds">通知人.</param>
    /// <param name="creatorUserId">创建人ID.</param>
    /// <param name="tenantId">租户ID.</param>
    /// <returns></returns>
    private async Task ExecuteNotice(MessageConfig mesConfig, string msgEnCode, string defaultTitle, List<int> msgUserType, List<string> msgUserIds, string creatorUserId, string tenantId, IntegrateTaskEntity? taskEntity = null)
    {
        var dicHerader = new Dictionary<string, object>();
        if (KeyVariable.MultiTenancy && !string.IsNullOrEmpty(tenantId))
        {
            await _tenantManager.ChangTenant((SqlSugarScope)_sqlSugarClient, tenantId);
        }

        var user = await _sqlSugarClient.CopyNew().Queryable<UserEntity>().FirstAsync(it => it.Id.Equals(creatorUserId));

        // 生成实时token
        var toKen = NetHelper.GetToken(user.Id, user.Account, user.RealName, user.IsAdministrator, tenantId);
        dicHerader.Add("Authorization", toKen);

        var localAddress = GetLocalAddress();

        string parameter = new { mesConfig = mesConfig, msgEnCode = msgEnCode, defaultTitle = defaultTitle, msgUserType = msgUserType, msgUserIds = msgUserIds, creatorUserId = creatorUserId, taskEntity = taskEntity }.ToJsonString();
        string path = string.Format("{0}/api/VisualDev/Integrate/ExecuteNotice", localAddress);

        await path.SetJsonSerialization<NewtonsoftJsonSerializerProvider>().SetContentType("application/json").SetHeaders(dicHerader).SetBody(parameter).PostAsync();
    }

    /// <summary>
    /// 集成助手远程请求客户端.
    /// </summary>
    /// <param name="requestModeType">请求方式.</param>
    /// <param name="requestAddress">请求地址.</param>
    /// <param name="parameter">请求参数.</param>
    /// <param name="userId">token用户ID.</param>
    /// <param name="tenantId">token租户.</param>
    /// <returns></returns>
    private async Task<RESTfulResult<object>> InteAssistantHttpClient(int requestModeType, string requestAddress, object parameter, string userId, string tenantId)
    {
        var response = new RESTfulResult<object>();

        var dicHerader = new Dictionary<string, object>();
        if (KeyVariable.MultiTenancy && !string.IsNullOrEmpty(tenantId))
        {
            await _tenantManager.ChangTenant((SqlSugarScope)_sqlSugarClient, tenantId);
        }

        var user = await _sqlSugarClient.CopyNew().Queryable<UserEntity>().FirstAsync(it => it.Id.Equals(userId));

        // 生成实时token
        var toKen = NetHelper.GetToken(user.Id, user.Account, user.RealName, user.IsAdministrator, tenantId);
        dicHerader.Add("Authorization", toKen);

        var localAddress = GetLocalAddress();

        var path = string.Format("{0}{1}", localAddress, requestAddress);

        switch (requestModeType)
        {
            // post
            case 1:
                response = (await path.SetJsonSerialization<NewtonsoftJsonSerializerProvider>().SetContentType("application/json").SetHeaders(dicHerader).SetBody(parameter).PostAsStringAsync()).ToObject<RESTfulResult<object>>();
                break;

            // put
            case 2:
                response = (await path.SetJsonSerialization<NewtonsoftJsonSerializerProvider>().SetContentType("application/json").SetHeaders(dicHerader).SetBody(parameter).PutAsStringAsync()).ToObject<RESTfulResult<object>>();
                break;
        }

        return response;
    }

    /// <summary>
    /// 获取转移表单字段.
    /// </summary>
    /// <param name="data">原数据.</param>
    /// <param name="transferList">转移配置.</param>
    /// <returns></returns>
    private Dictionary<string, object> GetTargetForm(string data, List<transferConfig> transferList)
    {
        // 整理数据转递配置 以目标表 为主
        List<TargetLogicModel> targetLogicModels = new List<TargetLogicModel>();

        // 目标表 非子表
        var goalNonSubTable = transferList.Where(it => !it.targetField.IsMatch("^tableField\\d{3}")).ToList();

        // 目标表 子表
        var goalSubTable = transferList.Where(it => it.targetField.IsMatch("^tableField\\d{3}")).ToList();

        var triggerSubTable = new List<string>();

        // 分类数据
        foreach (var targetLogicModel in goalNonSubTable)
        {
            var model = targetLogicModel.Adapt<TargetLogicModel>();
            switch (targetLogicModel.sourceValue.IsMatch("^tableField\\d{3}"))
            {
                case true:
                    var tableName = targetLogicModel.sourceValue.Match("^tableField\\d{3}");
                    switch (!string.IsNullOrEmpty(tableName) && !triggerSubTable.Contains(tableName))
                    {
                        case true:
                            triggerSubTable.Add(tableName);
                            break;
                    }

                    model.assignType = 2;
                    break;
                default:
                    model.assignType = 1;
                    break;
            }

            targetLogicModels.Add(model);
        }

        // 分类子表数据
        foreach (var item in goalSubTable)
        {
            var tableName = item.targetField.Match("^tableField\\d{3}");
            if (!targetLogicModels.Any(it => it.targetField.Equals(tableName)))
            {
                var subTableList = transferList.Where(it => it.targetField.StartsWith(tableName)).Adapt<List<TargetLogicModel>>();
                foreach (var targetLogicModel in subTableList)
                {
                    var triggerTableName = targetLogicModel.sourceValue.Match("^tableField\\d{3}");
                    switch (!string.IsNullOrEmpty(triggerTableName) && !triggerSubTable.Contains(triggerTableName))
                    {
                        case true:
                            triggerSubTable.Add(triggerTableName);
                            break;
                    }

                    switch (targetLogicModel.sourceValue.IsMatch("^tableField\\d{3}"))
                    {
                        case true:
                            targetLogicModel.assignType = 4;
                            break;
                        default:
                            targetLogicModel.assignType = 3;
                            break;
                    }

                    targetLogicModel.targetField = targetLogicModel.targetField.Replace(string.Format("{0}-", tableName), "");
                }

                targetLogicModels.Add(new TargetLogicModel
                {
                    targetField = tableName,
                    SubTable = subTableList
                });
            }
        }

        var clay = Clay.Parse(data);

        // 将被触发表 数据转移子表数据 提取出来
        var triggerSubTableDataList = new Dictionary<string, List<Dictionary<string, object>>>();
        var triggerSubTableDataCount = new Dictionary<string, int>();
        foreach (var trigger in triggerSubTable)
        {
            // 判断触发数据有没有对应子表数据
            if (clay.IsDefined(trigger))
            {
                // 获取子表全部数据
                var subTabelData = clay[trigger];

                // 子表数据转粘土对象
                var subTableClay = Clay.Parse(subTabelData.ToString());
                IEnumerable<dynamic> subTableList = subTableClay.AsEnumerator<dynamic>();
                var subTableDicList = new List<Dictionary<string, object>>();
                foreach (var subtable in subTableList)
                {
                    subTableDicList.Add(subtable.ToDictionary());
                }
                triggerSubTableDataList.Add(trigger, subTableDicList);
                triggerSubTableDataCount.Add(trigger, subTableDicList.Count());
            }
        }

        // 触发表 子表最大行数
        var numRange = triggerSubTableDataCount.ToList();
        int subTableNum = 0;
        if (numRange.Count > 0)
            subTableNum = numRange.Max(triggerSubTableDataCount => triggerSubTableDataCount.Value);
        var targetForm = new Dictionary<string, object>();

        foreach (var item in targetLogicModels)
        {
            if (item.SubTable?.Count > 0)
            {
                var subTableList = new List<Dictionary<string, object>>();
                for (int i = 0; i < subTableNum; i++)
                {
                    var subTableValue = new Dictionary<string, object>();
                    foreach (var goal in item.SubTable)
                    {
                        var trigger = goal.sourceValue.Match("^tableField\\d{3}");
                        var field = goal.sourceValue.Replace(string.Format("{0}-", trigger), string.Empty);
                        var truthList = triggerSubTableDataList.Where(it => it.Key.Equals(trigger)).FirstOrDefault().Value;

                        // 最终赋值
                        switch (goal.assignType)
                        {
                            case 3:
                                {
                                    switch (item.sourceType)
                                    {
                                        case 1:
                                            switch (item.sourceValue.Equals("@formId"))
                                            {
                                                case true:
                                                    {
                                                        var value = clay["id"];
                                                        targetForm.Add(item.targetField, value == null ? null : value.ToString());
                                                    }
                                                    break;
                                                default:
                                                    if (clay.IsDefined(goal.sourceValue))
                                                    {
                                                        var value = clay[goal.sourceValue];
                                                        subTableValue.Add(goal.targetField, value == null ? null : value.ToString());
                                                    }
                                                    break;
                                            }
                                            break;
                                        default:
                                            subTableValue.Add(goal.targetField, clay[goal.sourceValue]?.ToString());
                                            break;
                                    }
                                }
                                break;
                            case 4:
                                if (truthList.Count > i)
                                {
                                    subTableValue.Add(goal.targetField, goal.sourceType == 1 ? truthList[i][field]?.ToString() : goal.sourceValue);
                                }
                                break;
                        }
                    }

                    // 行赋值
                    subTableList.Add(subTableValue);
                }

                if (subTableNum == 0)
                {
                    var subTableValue = new Dictionary<string, object>();
                    foreach (var goal in item.SubTable)
                    {
                        // 最终赋值
                        switch (goal.assignType)
                        {
                            case 3:
                                {
                                    switch (item.sourceType)
                                    {
                                        case 1:
                                            switch (item.sourceValue.Equals("@formId"))
                                            {
                                                case true:
                                                    {
                                                        var value = clay["id"];
                                                        targetForm.Add(item.targetField, value == null ? null : value.ToString());
                                                    }
                                                    break;
                                                default:
                                                    if (clay.IsDefined(goal.sourceValue))
                                                    {
                                                        var value = clay[goal.sourceValue];
                                                        subTableValue.Add(goal.targetField, value == null ? null : value.ToString());
                                                    }
                                                    break;
                                            }
                                            break;
                                        default:
                                            subTableValue.Add(goal.targetField, clay[goal.sourceValue]?.ToString());
                                            break;
                                    }
                                }
                                break;
                        }
                    }

                    // 行赋值
                    subTableList.Add(subTableValue);
                }

                if (subTableList.Count > 0)
                {
                    targetForm.Add(item.targetField, subTableList);
                }
            }
            else
            {
                switch (item.assignType)
                {
                    case 1:
                        {
                            switch (item.sourceType)
                            {
                                case 1:
                                    switch (item.sourceValue.Equals("@formId"))
                                    {
                                        case true:
                                            {
                                                var value = clay["id"];
                                                targetForm.Add(item.targetField, value == null ? null : value.ToString());
                                            }
                                            break;
                                        default:
                                            if (clay.IsDefined(item.sourceValue))
                                            {
                                                var value = clay[item.sourceValue];
                                                targetForm.Add(item.targetField, value == null ? null : value.ToString());
                                            }
                                            break;
                                    }
                                    break;
                                default:
                                    targetForm.Add(item.targetField, item.sourceValue);
                                    break;
                            }
                        }
                        break;
                    case 2:
                        {
                            var tableName = item.sourceValue.Match("^tableField\\d{3}");
                            var field = item.sourceValue.Replace(string.Format("{0}-", tableName), string.Empty);

                            if (clay.IsDefined(tableName))
                            {
                                // 获取子表全部数据
                                var subTabelData = clay[tableName];

                                // 子表数据转粘土对象
                                var subTableClay = Clay.Parse(subTabelData.ToString());

                                List<Dictionary<string, object>> dataCount = subTableClay.Deserialize<List<Dictionary<string, object>>>();
                                if (dataCount.Count > 0)
                                {
                                    // 粘土对象转数组/集合
                                    IEnumerable<dynamic> subTableList = subTableClay.AsEnumerator<dynamic>();

                                    // 子传主 支取子表第一条数据
                                    var subTable = subTableList.FirstOrDefault();

                                    // 子表行转换成 dictionary
                                    Dictionary<string, object> subTableDic = subTable.ToDictionary();
                                    targetForm.Add(item.targetField, item.sourceType == 1 ? subTableDic[field]?.ToString() : item.sourceValue);
                                }
                            }
                        }

                        break;
                }
            }
        }

        return targetForm;
    }

    /// <summary>
    /// 获取数据接口参数.
    /// </summary>
    /// <param name="data">原数据.</param>
    /// <param name="templateJson">模版.</param>
    /// <returns></returns>
    private Dictionary<string, object> GetInterfaceParameter(string data, List<InteAssisInterfaceParameterModel> templateJson)
    {
        Dictionary<string, object> result = new Dictionary<string, object>();
        dynamic? clay = null;
        if (!string.IsNullOrEmpty(data))
        {
            clay = Clay.Parse(data);
        }
        foreach (var template in templateJson)
        {
            var relationField = template.relationField;
            if (template.isSubTable)
            {
                var tableName = relationField.Match("^tableField\\d{3}");
                var field = relationField.Replace(string.Format("{0}-", tableName), string.Empty);

                switch (string.IsNullOrEmpty(data))
                {
                    case false:
                        if (clay.IsDefined(tableName))
                        {
                            // 获取子表全部数据
                            var subTabelData = clay[tableName];

                            // 子表数据转粘土对象
                            var subTableClay = Clay.Parse(subTabelData.ToString());

                            List<Dictionary<string, object>> dataCount = subTableClay.Deserialize<List<Dictionary<string, object>>>();
                            if (dataCount.Count > 0)
                            {
                                // 粘土对象转数组/集合
                                IEnumerable<dynamic> subTableList = subTableClay.AsEnumerator<dynamic>();

                                // 取子表第一条数据
                                var subTable = subTableList.FirstOrDefault();

                                // 子表行转换成 dictionary
                                Dictionary<string, object> subTableDic = subTable.ToDictionary();

                                result.Add(template.field, (string)subTableDic[field]);
                            }
                            else
                            {
                                result.Add(template.field, string.Empty);
                            }
                        }
                        break;
                    case true:
                        result.Add(template.field, template.relationField);
                        break;
                }
            }
            else
            {
                string value = string.Empty;
                switch (string.IsNullOrEmpty(data))
                {
                    case false:
                        if (clay.IsDefined(relationField))
                            value = clay[relationField]?.ToString();
                        break;
                }
                result.Add(template.field, template.sourceType == 1 ? value : template.relationField);
            }
        }

        return result;
    }

    /// <summary>
    /// 替换规则列表内值.
    /// </summary>
    /// <param name="ruleList">规则列表.</param>
    /// <param name="data">真实数据.</param>
    /// <returns></returns>
    private List<object> GetRuleList(List<object> ruleList, string data)
    {
        var clay = data.ToObject<Dictionary<string, object>>();
        var ruleLogicList = ruleList.ToObject<List<InteAssisConditionGroup>>();
        foreach (var rule in ruleLogicList)
        {
            foreach (var ruleLogic in rule.groups)
            {
                string fieldValue = string.Empty;
                if (ruleLogic.symbol != "notNull")
                {
                    if (ruleLogic.fieldValue != null)
                    {
                        fieldValue = ruleLogic.fieldValue.ToString();
                        if (fieldValue.Equals("@formId"))
                            fieldValue = "id";
                    }

                    string value = ruleLogic.fieldValueType == 1 ? clay[fieldValue]?.ToString() : fieldValue;
                    ruleLogic.fieldValue = value;
                }
            }
        }

        return ruleLogicList.ToObject<List<object>>();
    }

    public void Dispose()
    {
        _serviceScope.Dispose();
    }
}