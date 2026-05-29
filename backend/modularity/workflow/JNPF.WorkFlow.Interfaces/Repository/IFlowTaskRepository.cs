using JNPF.Common.Models.User;
using JNPF.Common.Models.WorkFlow;
using JNPF.Systems.Entitys.System;
using JNPF.VisualDev.Entitys;
using JNPF.WorkFlow.Entitys.Dto.FlowBefore;
using JNPF.WorkFlow.Entitys.Dto.FlowLaunch;
using JNPF.WorkFlow.Entitys.Dto.FlowMonitor;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Model;
using SqlSugar;
using System.Linq.Expressions;

namespace JNPF.WorkFlow.Interfaces.Repository;

public interface IFlowTaskRepository
{
    #region 流程列表

    /// <summary>
    /// 列表（流程监控）.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    Task<dynamic> GetMonitorList(FlowMonitorListQuery input);

    /// <summary>
    /// 列表（我发起的）.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    Task<dynamic> GetLaunchList(FlowLaunchListQuery input);

    /// <summary>
    /// 列表（待我审批）.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    Task<dynamic> GetWaitList(FlowBeforeListQuery input);

    /// <summary>
    /// 列表（批量审批）.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    Task<dynamic> GetBatchWaitList(FlowBeforeListQuery input);

    /// <summary>
    /// 列表（我已审批）.
    /// </summary>
    /// <param name="input">请求参数</param>
    /// <returns></returns>
    Task<dynamic> GetTrialList(FlowBeforeListQuery input);

    /// <summary>
    /// 列表（抄送我的）.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    Task<dynamic> GetCirculateList(FlowBeforeListQuery input);

    /// <summary>
    /// 批量流程列表.
    /// </summary>
    /// <returns></returns>
    Task<dynamic> BatchFlowSelector();

    /// <summary>
    /// 根据分类获取审批意见.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="category"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    Task<List<FlowBeforeRecordListModel>> GetRecordListByCategory(string taskId, string category, string type = "0");

    /// <summary>
    /// 事件日志列表.
    /// </summary>
    /// <param name="taskNodeId"></param>
    /// <returns></returns>
    Task<dynamic> GetEventLog(string taskNodeId);
    #endregion

    #region 其他模块流程列表

    /// <summary>
    /// 门户列表（待我审批）.
    /// </summary>
    /// <returns></returns>
    Task<List<FlowBeforeListOutput>> GetWaitList();

    /// <summary>
    /// 门户列表（待我审批）.
    /// </summary>
    /// <returns></returns>
    Task<dynamic> GetPortalWaitList();

    /// <summary>
    /// 列表（我已审批）.
    /// </summary>
    /// <returns></returns>
    Task<List<FlowTaskEntity>> GetTrialList();

    /// <summary>
    /// 获取工作交接列表.
    /// </summary>
    /// <param name="userId">离职人.</param>
    /// <param name="type">1-待办事宜 2-负责流程.</param>
    /// <returns></returns>
    Task<List<FlowWorkModel>> GetWorkHandover(string userId, int type);

    /// <summary>
    /// 保存工作交接数据.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="taskIds"></param>
    void SaveWorkHandover(string userId, List<string> ids, int type, string handOverUserId = "");

    /// <summary>
    /// 获取组织树.
    /// </summary>
    /// <param name="orgId"></param>
    /// <returns></returns>
    List<string> GetOrgTree(string orgId);
    #endregion

    #region Other

    /// <summary>
    /// 流程信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    FlowJsonModel GetFlowTemplateInfo(string id);

    /// <summary>
    /// 流程json信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    FlowTemplateJsonEntity GetFlowTemplateJsonInfo(Expression<Func<FlowTemplateJsonEntity, bool>> expression);

    /// <summary>
    /// 表单信息.
    /// </summary>
    /// <param name="formId"></param>
    /// <returns></returns>
    Task<FlowFormModel> GetFlowFromModel(string formId);

    /// <summary>
    /// 表单信息.
    /// </summary>
    /// <param name="formId"></param>
    /// <returns></returns>
    public FlowFormEntity GetFlowFromEntity(string formId);

    /// <summary>
    /// 获取指定用户被委托人.
    /// </summary>
    /// <param name="userIds">指定用户.</param>
    /// <param name="flowId">流程id.</param>
    /// <returns></returns>
    Task<List<string>> GetDelegateUserIds(List<string> userIds, string flowId);

    /// <summary>
    /// 获取指定用户被委托人.
    /// </summary>
    /// <param name="userId">指定用户.</param>
    /// <param name="flowId">流程id.</param>
    /// <returns></returns>
    List<string> GetToUserId(string userId, string flowId);

    /// <summary>
    /// 获取功能开发.
    /// </summary>
    /// <param name="flowId">流程id.</param>
    /// <returns></returns>
    Task<VisualDevEntity> GetVisualDevInfo(string flowId);

    /// <summary>
    /// 获取数据连接.
    /// </summary>
    /// <param name="id">id.</param>
    /// <returns></returns>
    Task<DbLinkEntity> GetLinkInfo(string id);

    /// <summary>
    /// 获取任务发起人信息.
    /// </summary>
    /// <param name="id">id.</param>
    /// <returns></returns>
    FlowUserEntity GetFlowUserEntity(string id);

    /// <summary>
    /// 新增任务发起人信息.
    /// </summary>
    /// <param name="userId">用户id.</param>
    /// <param name="taskId">任务id.</param>
    void CreateFlowUser(string userId, string taskId);

    /// <summary>
    /// 获取当前用户关系id.
    /// </summary>
    /// <returns></returns>
    List<string> GetCurrentUserObjId();

    /// <summary>
    /// 是否为功能流程.
    /// </summary>
    /// <param name="flowId"></param>
    /// <returns></returns>
    bool IsDevFlow(string flowId);

    /// <summary>
    /// 当前用户分级管理员权限.
    /// </summary>
    /// <param name="creatorUserId"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    bool GetOrgAdminAuthorize(string creatorUserId, int type);
    #endregion

    #region FlowTask

    /// <summary>
    /// 任务列表.
    /// </summary>
    /// <returns></returns>
    Task<List<FlowTaskEntity>> GetTaskList();

    /// <summary>
    /// 任务列表.
    /// </summary>
    /// <param name="flowId">引擎id.</param>
    /// <returns></returns>
    Task<List<FlowTaskEntity>> GetTaskList(string flowId);

    /// <summary>
    /// 任务列表.
    /// </summary>
    /// <param name="expression">条件.</param>
    /// <returns></returns>
    Task<List<FlowTaskEntity>> GetTaskList(Expression<Func<FlowTaskEntity, bool>> expression);

    /// <summary>
    /// 任务信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<FlowTaskEntity> GetTaskInfo(string id);

    /// <summary>
    /// 任务信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    FlowTaskEntity GetTaskFirstOrDefault(string id);

    /// <summary>
    /// 是否存在任务.
    /// </summary>
    /// <param name="expression">id</param>
    /// <returns></returns>
    Task<bool> AnyFlowTask(Expression<Func<FlowTaskEntity, bool>> expression);

    /// <summary>
    /// 任务删除.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task<int> DeleteTask(FlowTaskEntity entity);

    /// <summary>
    /// 任务删除, 非异步.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    int DeleteTaskNoAwait(FlowTaskEntity entity, bool isDel = true);

    /// <summary>
    /// 任务创建.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task<FlowTaskEntity> CreateTask(FlowTaskEntity entity);

    /// <summary>
    /// 任务更新.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task<bool> UpdateTask(FlowTaskEntity entity);

    /// <summary>
    /// 任务更新.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task<bool> UpdateTask(FlowTaskEntity entity, Expression<Func<FlowTaskEntity, object>> Expression = null);

    /// <summary>
    /// 打回流程删除所有相关数据.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="isClearRecord">是否清除记录.</param>
    /// <returns></returns>
    Task DeleteFlowTaskAllData(string taskId, bool isClearRecord = true, bool isClearCandidates = true);

    /// <summary>
    /// 打回流程删除所有相关数据.
    /// </summary>
    /// <param name="taskIds">任务di数组.</param>
    /// <param name="isClearRecord">是否清除记录.</param>
    /// <returns></returns>
    Task DeleteFlowTaskAllData(List<string> taskIds, bool isClearRecord = true);

    /// <summary>
    /// 删除子流程.
    /// </summary>
    /// <param name="flowTaskEntity"></param>
    /// <returns></returns>
    Task DeleteSubTask(FlowTaskEntity flowTaskEntity);
    #endregion

    #region FlowTaskNode

    /// <summary>
    /// 节点列表.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    Task<List<FlowTaskNodeEntity>> GetTaskNodeList(string taskId);

    /// <summary>
    /// 节点列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="orderByExpression"></param>
    /// <param name="orderByType"></param>
    /// <returns></returns>
    Task<List<FlowTaskNodeEntity>> GetTaskNodeList(Expression<Func<FlowTaskNodeEntity, bool>> expression, Expression<Func<FlowTaskNodeEntity, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc);

    /// <summary>
    /// 节点信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<FlowTaskNodeEntity> GetTaskNodeInfo(string id);

    /// <summary>
    /// 节点信息.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    Task<FlowTaskNodeEntity> GetTaskNodeInfo(Expression<Func<FlowTaskNodeEntity, bool>> expression);

    /// <summary>
    /// 节点创建.
    /// </summary>
    /// <param name="entitys"></param>
    /// <returns></returns>
    Task<bool> CreateTaskNode(List<FlowTaskNodeEntity> entitys);

    /// <summary>
    /// 节点更新.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task<bool> UpdateTaskNode(FlowTaskNodeEntity entity);

    /// <summary>
    /// 节点更新.
    /// </summary>
    /// <param name="entitys"></param>
    /// <returns></returns>
    Task<bool> UpdateTaskNode(List<FlowTaskNodeEntity> entitys);
    #endregion

    #region FlowTaskOperator

    /// <summary>
    /// 经办列表.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    Task<List<FlowTaskOperatorEntity>> GetTaskOperatorList(string taskId);

    /// <summary>
    /// 经办列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="orderByExpression"></param>
    /// <param name="orderByType"></param>
    /// <returns></returns>
    Task<List<FlowTaskOperatorEntity>> GetTaskOperatorList(Expression<Func<FlowTaskOperatorEntity, bool>> expression, Expression<Func<FlowTaskOperatorEntity, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc);

    /// <summary>
    /// 依次审批经办列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="orderByExpression"></param>
    /// <param name="orderByType"></param>
    /// <returns></returns>
    Task<List<FlowTaskOperatorUserEntity>> GetTaskOperatorUserList(Expression<Func<FlowTaskOperatorUserEntity, bool>> expression, Expression<Func<FlowTaskOperatorUserEntity, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc);

    /// <summary>
    /// 经办信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<FlowTaskOperatorEntity> GetTaskOperatorInfo(string id);

    /// <summary>
    /// 经办信息.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    Task<FlowTaskOperatorEntity> GetTaskOperatorInfo(Expression<Func<FlowTaskOperatorEntity, bool>> expression);

    /// <summary>
    /// 经办删除.
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    Task<int> DeleteTaskOperator(List<string> ids);

    /// <summary>
    /// 依次经办删除.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    Task<int> DeleteTaskOperatorUser(Expression<Func<FlowTaskOperatorUserEntity, bool>> expression);

    /// <summary>
    /// 经办创建.
    /// </summary>
    /// <param name="entitys"></param>
    /// <returns></returns>
    Task<bool> CreateTaskOperator(List<FlowTaskOperatorEntity> entitys);

    /// <summary>
    /// 依次经办创建.
    /// </summary>
    /// <param name="entitys"></param>
    /// <returns></returns>
    Task<bool> CreateTaskOperatorUser(List<FlowTaskOperatorUserEntity> entitys);

    /// <summary>
    /// 经办创建.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task<bool> CreateTaskOperator(FlowTaskOperatorEntity entity);

    /// <summary>
    /// 经办更新.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task<bool> UpdateTaskOperator(FlowTaskOperatorEntity entity);

    /// <summary>
    /// 经办更新.
    /// </summary>
    /// <param name="entitys"></param>
    /// <returns></returns>
    Task<bool> UpdateTaskOperator(List<FlowTaskOperatorEntity> entitys);

    /// <summary>
    /// 是否存在依次审批经办.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    bool AnyTaskOperatorUser(Expression<Func<FlowTaskOperatorUserEntity, bool>> expression);

    /// <summary>
    /// 经办更新.
    /// </summary>
    /// <param name="entitys"></param>
    /// <returns></returns>
    Task<bool> UpdateTaskOperatorUser(List<FlowTaskOperatorUserEntity> entitys);
    #endregion

    #region FlowTaskOperatorRecord

    /// <summary>
    /// 经办记录列表.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    Task<List<FlowTaskOperatorRecordEntity>> GetTaskOperatorRecordList(string taskId);

    /// <summary>
    /// 经办记录列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="orderByExpression"></param>
    /// <param name="orderByType"></param>
    /// <returns></returns>
    Task<List<FlowTaskOperatorRecordEntity>> GetTaskOperatorRecordList(Expression<Func<FlowTaskOperatorRecordEntity, bool>> expression, Expression<Func<FlowTaskOperatorRecordEntity, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc);

    /// <summary>
    /// 经办记录列表.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    Task<List<FlowTaskOperatorRecordModel>> GetTaskOperatorRecordModelList(string taskId);

    /// <summary>
    /// 经办记录信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<FlowTaskOperatorRecordEntity> GetTaskOperatorRecordInfo(string id);

    /// <summary>
    /// 经办记录信息.
    /// </summary>
    /// <param name="expression">条件.</param>
    /// <returns></returns>
    Task<FlowTaskOperatorRecordEntity> GetTaskOperatorRecordInfo(Expression<Func<FlowTaskOperatorRecordEntity, bool>> expression);

    /// <summary>
    /// 经办记录创建.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task<bool> CreateTaskOperatorRecord(FlowTaskOperatorRecordEntity entity);

    /// <summary>
    /// 经办记录作废.
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    Task DeleteTaskOperatorRecord(List<string> ids);

    /// <summary>
    /// 经办记录作废.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    Task DeleteTaskOperatorRecord(Expression<Func<FlowTaskOperatorRecordEntity, bool>> expression);
    #endregion

    #region FlowTaskCirculate

    /// <summary>
    /// 传阅创建.
    /// </summary>
    /// <param name="entitys"></param>
    /// <returns></returns>
    Task<bool> CreateTaskCirculate(List<FlowTaskCirculateEntity> entitys);
    #endregion

    #region FlowTaskCandidates

    /// <summary>
    /// 候选人创建.
    /// </summary>
    /// <param name="entitys"></param>
    void CreateFlowCandidates(List<FlowCandidatesEntity> entitys);

    /// <summary>
    /// 候选人删除.
    /// </summary>
    /// <param name="expression"></param>
    void DeleteFlowCandidates(Expression<Func<FlowCandidatesEntity, bool>> expression);

    /// <summary>
    /// 候选人获取.
    /// </summary>
    /// <param name="nodeId"></param>
    List<string> GetFlowCandidates(string nodeId);
    #endregion

    #region 系统表单
    Task GetSysTableFromService(string enCode, object data, string id, int type);
    #endregion

    #region FlowTaskParamter
    /// <summary>
    /// 根据任务id获取任务引擎参数.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="flowHandleModel"></param>
    /// <returns></returns>
    Task<FlowTaskParamter> GetTaskParamterByTaskId(string taskId, FlowHandleModel flowHandleModel);

    /// <summary>
    /// 根据节点id获取任务引擎参数.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="flowHandleModel"></param>
    /// <returns></returns>
    Task<FlowTaskParamter> GetTaskParamterByNodeId(string nodeId, FlowHandleModel flowHandleModel);

    /// <summary>
    /// 根据经办id获取任务引擎参数.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="flowHandleModel"></param>
    /// <returns></returns>
    Task<FlowTaskParamter> GetTaskParamterByOperatorId(string operatorId, FlowHandleModel flowHandleModel);
    #endregion

    #region FlowRejectData
    /// <summary>
    /// 驳回数据信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<FlowRejectDataEntity> GetRejectDataInfo(string id);

    /// <summary>
    /// 驳回数据创建.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="taskNodeId"></param>
    /// <returns></returns>
    Task<string> CreateRejectData(string taskId, string taskNodeId);

    /// <summary>
    /// 驳回数据重启.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task UpdateRejectData(FlowRejectDataEntity entity);
    #endregion

    #region FlowEventLog
    /// <summary>
    /// 插入事件日志.
    /// </summary>
    /// <param name="flowEventLogEntity"></param>
    /// <returns></returns>
    Task CreateEventLog(FlowEventLogEntity flowEventLogEntity);
    #endregion

    #region FlowFormAuthorize

    Task CreateFlowFormAuthorize(List<FlowFormAuthorizeEntity> flowFormAuthorizeEntitys);

    Task<dynamic> GetFlowFormAuthorize(string taskId, string nodeCode);

    Task<dynamic> GetFlowFormAuthorize(FlowTaskNodeEntity flowTaskNodeEntity);
    #endregion
}
