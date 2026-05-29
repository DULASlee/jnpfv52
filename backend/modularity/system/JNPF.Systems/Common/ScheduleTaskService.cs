using JNPF.Common.Dtos.Datainterface;
using JNPF.Common.Extension;
using JNPF.Common.Models;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Systems.Entitys.Entity.System;
using JNPF.Systems.Interfaces.System;
using JNPF.TaskScheduler.Entitys;
using JNPF.TaskScheduler.Interfaces.TaskScheduler;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.Properties;
using JNPF.WorkFlow.Interfaces.Manager;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.Systems.Common;

/// <summary>
/// 定时任务(内部调用).
/// </summary>
[ApiDescriptionSettings(Name = "ScheduleTask", Order = 306)]
[Route("[controller]")]
public class ScheduleTaskService : IDynamicApiController, ITransient
{
    private readonly IScheduleService _scheduleService;
    private readonly IFlowTaskManager _flowTaskManager;
    private readonly ITimeTaskService _timeTaskService;
    private readonly IDataInterfaceService _dataInterfaceService;

    public ScheduleTaskService(
      IScheduleService scheduleService,
      IFlowTaskManager flowTaskManager,
      ITimeTaskService timeTaskService,
      IDataInterfaceService dataInterfaceService)
    {
        _scheduleService = scheduleService;
        _flowTaskManager = flowTaskManager;
        _timeTaskService = timeTaskService;
        _dataInterfaceService = dataInterfaceService;
    }

    /// <summary>
    /// 定时任务.
    /// </summary>
    /// <param name="taskCode"></param>
    /// <param name="scheduleTaskModel"></param>
    /// <returns></returns>
    [HttpPost("{taskCode}")]
    public async Task<dynamic> ScheduleTask(string taskCode, [FromBody] ScheduleTaskModel scheduleTaskModel)
    {
        return "";  //临时
        switch (taskCode)
        {
            case "schedule":
                var scheduleEntity = scheduleTaskModel.taskParams["entity"].ToObject<ScheduleEntity>();
                var userList = scheduleTaskModel.taskParams["userList"].ToObject<List<string>>();
                var type = scheduleTaskModel.taskParams["type"].ToString();
                var enCode = scheduleTaskModel.taskParams["enCode"].ToString();
                await _scheduleService.SendScheduleMsg(scheduleEntity, userList, type, enCode);
                break;
            case "flowtask":
                var approPro = scheduleTaskModel.taskParams["approPro"].ToObject<ApproversProperties>();
                var flowTaskParamter = scheduleTaskModel.taskParams["flowTaskParamter"].ToObject<FlowTaskParamter>();
                var nodeId = scheduleTaskModel.taskParams["nodeId"].ToString();
                var count = scheduleTaskModel.taskParams["count"].ParseToInt();
                var isTimeOut = scheduleTaskModel.taskParams["isTimeOut"].ParseToBool();
                var isAtOnce = scheduleTaskModel.taskParams["isAtOnce"].ParseToBool();
                await _flowTaskManager.NotifyEvent(approPro, flowTaskParamter, nodeId, count, isTimeOut, isAtOnce);
                break;
            case "timetask":
                var entity = scheduleTaskModel.taskParams["entity"].ToObject<TimeTaskEntity>();
                await _timeTaskService.PerformJob(entity);
                break;
            case "datainterface":
                var id = scheduleTaskModel.taskParams["id"].ToString();
                var input = scheduleTaskModel.taskParams["input"].ToString().ToObject<DataInterfacePreviewInput>();
                _dataInterfaceService.GetDatainterfaceParameter(input);
                return await _dataInterfaceService.GetDataInterfaceData(id, input, 3);
            default:
                break;
        }
        return string.Empty;
    }
}
