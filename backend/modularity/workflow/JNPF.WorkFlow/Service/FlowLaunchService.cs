using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.WorkFlow.Entitys.Dto.FlowLaunch;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Interfaces.Manager;
using JNPF.WorkFlow.Interfaces.Repository;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.WorkFlow.Service;

/// <summary>
/// 流程发起.
/// </summary>
[ApiDescriptionSettings(Tag = "WorkflowEngine", Name = "FlowLaunch", Order = 305)]
[Route("api/workflow/Engine/[controller]")]
public class FlowLaunchService : IDynamicApiController, ITransient
{
    private readonly IFlowTaskRepository _flowTaskRepository;
    private readonly IFlowTaskManager _flowTaskManager;

    public FlowLaunchService(IFlowTaskRepository flowTaskRepository, IFlowTaskManager flowTaskManager)
    {
        _flowTaskRepository = flowTaskRepository;
        _flowTaskManager = flowTaskManager;
    }

    #region GET

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] FlowLaunchListQuery input)
    {
        return await _flowTaskRepository.GetLaunchList(input);
    }
    #endregion

    #region POST

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var entity = _flowTaskRepository.GetTaskFirstOrDefault(id);
        if (entity == null)
            throw Oops.Oh(ErrorCode.COM1005);
        if (entity.Suspend == 1) throw Oops.Oh(ErrorCode.WF0046);
        if (!entity.ParentId.Equals("0") && entity.ParentId.IsNotEmptyOrNull())
            throw Oops.Oh(ErrorCode.WF0003, entity.FullName);
        if (entity.FlowType == 1)
            throw Oops.Oh(ErrorCode.WF0012, entity.FullName);
        await _flowTaskRepository.DeleteSubTask(entity);
        var isOk = await _flowTaskRepository.DeleteTask(entity);
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 撤回
    /// 注意：在撤回流程时要保证你的下一节点没有处理这条记录；如已处理则无法撤销流程.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">流程经办.</param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/Withdraw")]
    public async Task Revoke(string id, [FromBody] FlowHandleModel flowHandleModel)
    {
        var flowTaskParamter = await _flowTaskRepository.GetTaskParamterByTaskId(id, flowHandleModel);
        if (flowTaskParamter.flowTaskEntity.Suspend == 1) throw Oops.Oh(ErrorCode.WF0046);
        if (await _flowTaskRepository.AnyFlowTask(x => flowTaskParamter.flowTaskEntity.Id == x.ParentId && x.DeleteMark == null && x.Suspend == 1)) throw Oops.Oh(ErrorCode.WF0046);
        if (flowTaskParamter.flowTaskEntity.Status != 1)
            throw Oops.Oh(ErrorCode.WF0010);
        if (flowTaskParamter.flowTaskEntity.ParentId.IsNotEmptyOrNull() && !flowTaskParamter.flowTaskEntity.ParentId.Equals("0"))
            throw Oops.Oh(ErrorCode.WF0015);
        await _flowTaskManager.Revoke(flowTaskParamter);
    }

    /// <summary>
    /// 催办.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpPost("Press/{id}")]
    public async Task Press(string id)
    {
        var flowTaskParamter = await _flowTaskRepository.GetTaskParamterByTaskId(id, null);
        if (flowTaskParamter.flowTaskEntity.Suspend == 1) throw Oops.Oh(ErrorCode.WF0046);
        await _flowTaskManager.Press(flowTaskParamter);
    }
    #endregion
}
