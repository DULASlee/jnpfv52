using JNPF.Common.Models.WorkFlow;
using JNPF.WorkFlow.Entitys.Dto.FlowBefore;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.Properties;

namespace JNPF.WorkFlow.Interfaces.Manager;

public interface IFlowTaskManager
{
    /// <summary>
    /// 获取任务详情.
    /// </summary>
    /// <param name="id">任务id.</param>
    /// <param name="flowId">流程id.</param>
    /// <param name="taskNodeId">节点id.</param>
    /// <param name="taskOperatorId">经办id.</param>
    /// <returns></returns>
    Task<FlowBeforeInfoOutput> GetFlowBeforeInfo(string id, string flowId, string taskNodeId, string taskOperatorId = null);

    /// <summary>
    /// 保存.
    /// </summary>
    /// <param name="flowTaskSubmitModel">提交参数.</param>
    /// <returns></returns>
    Task<FlowTaskEntity> Save(FlowTaskSubmitModel flowTaskSubmitModel);

    /// <summary>
    /// 提交.
    /// </summary>
    /// <param name="flowTaskSubmitModel">提交参数.</param>
    /// <returns></returns>
    Task<dynamic> Submit(FlowTaskSubmitModel flowTaskSubmitModel);

    /// <summary>
    /// 审批(同意).
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <param name="isAuto">是否自动审批.</param>
    /// <returns></returns>
    Task<dynamic> Audit(FlowTaskParamter flowTaskParamter, bool isAuto = false);

    /// <summary>
    /// 审批(拒绝).
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    Task<dynamic> Reject(FlowTaskParamter flowTaskParamter);

    /// <summary>
    /// 审批(撤回).
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <param name="flowTaskOperatorRecordEntity">经办记录.</param>
    Task Recall(FlowTaskParamter flowTaskParamter, FlowTaskOperatorRecordEntity flowTaskOperatorRecordEntity);

    /// <summary>
    /// 流程撤回.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    Task Revoke(FlowTaskParamter flowTaskParamter);

    /// <summary>
    /// 终止.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    Task Cancel(FlowTaskParamter flowTaskParamter);

    /// <summary>
    /// 指派.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    Task Assigned(FlowTaskParamter flowTaskParamter);

    /// <summary>
    /// 转办.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    Task Transfer(FlowTaskParamter flowTaskParamter);

    /// <summary>
    /// 催办.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    Task Press(FlowTaskParamter flowTaskParamter);

    /// <summary>
    /// 获取候选人.
    /// </summary>
    /// <param name="id">经办id.</param>
    /// <param name="flowHandleModel">审批参数.</param>
    /// <param name="type">0:候选节点编码，1：候选人.</param>
    /// <returns></returns>
    Task<dynamic> GetCandidateModelList(string id, FlowHandleModel flowHandleModel, int type = 0);

    /// <summary>
    /// 批量审批节点列表.
    /// </summary>
    /// <param name="flowId">流程id.</param>
    /// <returns></returns>
    Task<dynamic> NodeSelector(string flowId);

    /// <summary>
    /// 获取批量审批候选人.
    /// </summary>
    /// <param name="flowId">流程id.</param>
    /// <param name="flowTaskOperatorId">经办id.</param>
    /// <returns></returns>
    Task<dynamic> GetBatchCandidate(string flowId, string flowTaskOperatorId);

    /// <summary>
    /// 审批根据条件变更节点.
    /// </summary>
    /// <param name="flowEngineEntity">流程实例.</param>
    /// <param name="formData">表单数据.</param>
    /// <param name="flowTaskOperatorEntity">经办实例.</param>
    /// <returns></returns>
    Task AdjustNodeByCon(FlowJsonModel flowEngineEntity, object formData, FlowTaskOperatorEntity flowTaskOperatorEntity, bool isBranchFlow = false);

    /// <summary>
    /// 判断驳回节点是否存在子流程.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    bool IsSubFlowUpNode(FlowTaskParamter flowTaskParamter);

    /// <summary>
    /// 获取批量任务的表单数据.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    Task<object> GetBatchOperationData(FlowTaskParamter flowTaskParamter);

    /// <summary>
    /// 详情操作验证.
    /// </summary>
    /// <param name="taskOperatorId">经办id.</param>
    /// <param name="flowHandleModel">审批参数.</param>
    /// <returns></returns>
    Task<FlowTaskParamter> Validation(string taskOperatorId, FlowHandleModel flowHandleModel);

    /// <summary>
    /// 变更/复活.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    Task<dynamic> Change(FlowTaskParamter flowTaskParamter);

    /// <summary>
    /// 驳回审批节点列表.
    /// </summary>
    /// <param name="taskOperatorId">经办id.</param>
    /// <returns></returns>
    Task<dynamic> RejectNodeList(string taskOperatorId);

    /// <summary>
    /// 挂起.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    Task Suspend(FlowTaskParamter flowTaskParamter);

    /// <summary>
    /// 恢复.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    Task Restore(FlowTaskParamter flowTaskParamter);

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
    Task NotifyEvent(ApproversProperties approPro, FlowTaskParamter flowTaskParamter, string nodeId, int count, bool isTimeOut, bool isAtOnce = false);

    /// <summary>
    /// 转向.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    Task<dynamic> Swerve(FlowTaskParamter flowTaskParamter);

    Task<bool> IsSwerve(FlowTaskParamter flowTaskParamter);
}
