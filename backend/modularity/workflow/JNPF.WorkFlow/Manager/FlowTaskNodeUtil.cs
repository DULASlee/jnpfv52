using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.FriendlyException;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Enum;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Interfaces.Repository;

namespace JNPF.WorkFlow.Manager;

public class FlowTaskNodeUtil
{
    private readonly IFlowTaskRepository _flowTaskRepository;

    public FlowTaskNodeUtil(IFlowTaskRepository flowTaskRepository)
    {
        _flowTaskRepository = flowTaskRepository;
    }

    #region 节点处理

    /// <summary>
    /// 判断分流节点是否完成
    /// (因为分流节点最终只能是一个 所以只需判断下一节点中的其中一个的上节点完成情况).
    /// </summary>
    /// <param name="flowTaskNodeEntityList">所有节点.</param>
    /// <param name="nextNodeCode">下一个节点编码.</param>
    /// <param name="flowTaskNodeEntity">当前节点.</param>
    /// <returns></returns>
    public bool IsShuntNodeCompletion(List<FlowTaskNodeEntity> flowTaskNodeEntityList, string nextNodeCode, FlowTaskNodeEntity flowTaskNodeEntity)
    {
        var shuntNodeCodeList = flowTaskNodeEntityList.FindAll(x => x.NodeNext.IsNotEmptyOrNull() &&
        x.NodeCode != flowTaskNodeEntity.NodeCode && x.NodeNext.Contains(nextNodeCode) && x.Completion == 0 && x.State == 0);
        return shuntNodeCodeList.Count == 0;
    }

    /// <summary>
    /// 替换审批同意任务当前节点.
    /// </summary>
    /// <param name="flowTaskNodeEntityList">所有节点.</param>
    /// <param name="nextNodeCodeList">替换数据.</param>
    /// <param name="thisStepId">源数据.</param>
    /// <returns></returns>
    public string GetThisStepId(List<FlowTaskNodeEntity> flowTaskNodeEntityList, List<string> nextNodeCodeList, string thisStepId)
    {
        var replaceNodeCodeList = new List<string>();
        nextNodeCodeList.ForEach(item =>
        {
            var nodeCode = new List<string>();
            var nodeEntityList = flowTaskNodeEntityList.FindAll(x => x.NodeNext.IsNotEmptyOrNull() && x.NodeNext.Contains(item));
            nodeCode = nodeEntityList.Select(x => x.NodeCode).ToList();
            replaceNodeCodeList = replaceNodeCodeList.Union(nodeCode).ToList();
        });
        var thisNodeList = new List<string>();
        if (thisStepId.IsNotEmptyOrNull())
        {
            thisNodeList = thisStepId.Split(",").ToList();
        }
        //去除当前审批节点并添加下个节点
        var list = thisNodeList.Except(replaceNodeCodeList).Union(nextNodeCodeList);
        return string.Join(",", list.ToArray());
    }

    /// <summary>
    /// 替换审批撤回当前任务节点.
    /// </summary>
    /// <param name="nextNodeCodeList">下一节点编码.</param>
    /// <param name="thisStepId">当前待处理节点.</param>
    /// <returns></returns>
    public string GetRecallThisStepId(List<FlowTaskNodeEntity> nextNodeCodeList, string thisStepId)
    {
        var replaceNodeCodeList = new List<string>();
        foreach (var item in nextNodeCodeList)
        {
            var nodeCode = item.NodeNext.Split(",").ToList();
            replaceNodeCodeList = replaceNodeCodeList.Union(nodeCode).ToList();
        }

        var thisNodeList = new List<string>();
        if (thisStepId.IsNotEmptyOrNull())
        {
            thisNodeList = thisStepId.Split(",").ToList();
        }
        //去除当前审批节点并添加下个节点
        var list = thisNodeList.Except(replaceNodeCodeList).Union(nextNodeCodeList.Select(x => x.NodeCode));
        return string.Join(",", list.ToArray());
    }

    /// <summary>
    /// 驳回替换任务当前节点.
    /// </summary>
    /// <param name="flowTaskNodeEntityList">驳回节点下所有节点.</param>
    /// <param name="upNodeCodes">当前节点.</param>
    /// <param name="thisStepId">当前待处理节点.</param>
    /// <returns></returns>
    public string GetRejectThisStepId(List<FlowTaskNodeEntity> flowTaskNodeEntityList, List<string> upNodeCodes, string thisStepId)
    {
        // 驳回节点下所有节点编码
        var removeNodeCodeList = flowTaskNodeEntityList.Select(x => x.NodeCode).ToList();
        var ids = thisStepId.Split(",").ToList();
        var thisNodes = ids.Except(removeNodeCodeList).Union(upNodeCodes).ToList();
        return string.Join(",", thisNodes);
    }

    /// <summary>
    /// 根据当前节点编码获取节点名称.
    /// </summary>
    /// <param name="flowTaskNodeEntityList">所有节点.</param>
    /// <param name="thisStepId">当前待处理节点.</param>
    /// <returns></returns>
    public string GetThisStep(List<FlowTaskNodeEntity> flowTaskNodeEntityList, string thisStepId)
    {
        var ids = thisStepId.Split(",").ToList();
        var nextNodeNameList = new List<string>();
        foreach (var item in ids)
        {
            var name = flowTaskNodeEntityList.Find(x => x.NodeCode.Equals(item)).NodeName;
            nextNodeNameList.Add(name);
        }
        return string.Join(",", nextNodeNameList);
    }

    /// <summary>
    /// 获取驳回节点.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <returns></returns>
    public List<FlowTaskNodeEntity> GetRejectNode(FlowTaskParamter flowTaskParamter)
    {
        //驳回节点(除了上一节点，所有驳回节点都是单节点)
        var upflowTaskNodeEntityList = new List<FlowTaskNodeEntity>();
        if (flowTaskParamter.flowTaskNodeEntity.NodeUp == "1")
        {
            upflowTaskNodeEntityList = flowTaskParamter.flowTaskNodeEntityList.FindAll(x => x.NodeNext.IsNotEmptyOrNull() && x.NodeNext.Contains(flowTaskParamter.flowTaskNodeEntity.NodeCode));
        }
        else
        {
            var upflowTaskNodeEntity = flowTaskParamter.flowTaskNodeEntityList.Find(x => x.NodeCode == flowTaskParamter.rejectStep);
            upflowTaskNodeEntityList.Add(upflowTaskNodeEntity);
        }
        return upflowTaskNodeEntityList;
    }

    /// <summary>
    /// 修改节点完成状态.
    /// </summary>
    /// <param name="taskNodeList">修改节点.</param>
    /// <param name="state">状态.</param>
    /// <returns></returns>
    public async Task RejectUpdateTaskNode(List<FlowTaskNodeEntity> taskNodeList, int state)
    {
        foreach (var item in taskNodeList)
        {
            item.Completion = state;
            await _flowTaskRepository.UpdateTaskNode(item);
        }
    }

    /// <summary>
    /// 处理并保存节点.
    /// </summary>
    /// <param name="entitys">节点list.</param>
    public void UpdateNodeSort(List<FlowTaskNodeEntity> entitys)
    {
        var startNodes = entitys.FindAll(x => x.NodeType.Equals("start"));
        if (startNodes.Count > 0)
        {
            var startNode = startNodes[0].NodeCode;
            long num = 0L;
            long maxNum = 0L;
            var max = new List<long>();
            var _treeList = new List<FlowTaskNodeEntity>();
            NodeList(entitys, startNode, _treeList, num, max);
            max.Sort();
            if (max.Count > 0)
            {
                maxNum = max[max.Count - 1];
            }
            var nodeNext = "end";
            foreach (var item in entitys)
            {
                var type = item.NodeType;
                var node = _treeList.Find(x => x.NodeCode.Equals(item.NodeCode));
                if (item.NodeNext.IsEmpty())
                {
                    item.NodeNext = nodeNext;
                }
                if (node.IsNotEmptyOrNull())
                {
                    item.SortCode = node.SortCode;
                    item.State = 0;
                    if (item.NodeNext.IsEmpty())
                    {
                        item.NodeNext = nodeNext;
                    }
                }
            }
            entitys.Add(new FlowTaskNodeEntity()
            {
                Id = SnowflakeIdHelper.NextId(),
                NodeCode = nodeNext,
                NodeName = "结束",
                Completion = 0,
                CreatorTime = DateTime.Now,
                SortCode = maxNum + 1,
                TaskId = _treeList[0].TaskId,
                NodePropertyJson = startNodes[0].NodePropertyJson,
                NodeType = "endround",
                State = 0
            });
        }
    }

    /// <summary>
    /// 递归获取经过的节点.
    /// </summary>
    /// <param name="dataAll">所有节点.</param>
    /// <param name="nodeCode">节点编码.</param>
    /// <param name="_treeList">节点集合(容器).</param>
    /// <param name="num">递归次数.</param>
    /// <param name="max">递归次数值.</param>
    public void NodeList(List<FlowTaskNodeEntity> dataAll, string nodeCode, List<FlowTaskNodeEntity> _treeList, long num, List<long> max)
    {
        num++;
        max.Add(num);
        foreach (var item in dataAll)
        {
            if (item.NodeCode.Contains(nodeCode))
            {
                item.SortCode = num;
                item.State = 0;
                _treeList.Add(item);
                foreach (var nodeNext in item.NodeNext.Split(","))
                {
                    long nums = _treeList.FindAll(x => x.NodeCode.Equals(nodeNext)).Count;
                    if (nodeNext.IsNotEmptyOrNull() && nums == 0)
                    {
                        NodeList(dataAll, nodeNext, _treeList, num, max);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 递归节点下所有节点.
    /// </summary>
    /// <param name="flowTaskNodeList">所有节点.</param>
    /// <param name="nodeNext">下一节点.</param>
    /// <param name="flowTaskNodeEntities">指定节点下所有节点.</param>
    public async Task GetAllNextNode(List<FlowTaskNodeEntity> flowTaskNodeList, string nodeNext, List<FlowTaskNodeEntity> flowTaskNodeEntities)
    {
        var nextNodes = nodeNext.Split(",").ToList();
        var flowTaskNodeEntityList = flowTaskNodeList.FindAll(x => nextNodes.Contains(x.NodeCode));
        flowTaskNodeEntities.AddRange(flowTaskNodeEntityList);
        foreach (var item in flowTaskNodeEntityList)
        {
            if (!FlowTaskNodeTypeEnum.end.ParseToString().Equals(item.NodeCode))
            {
                await GetAllNextNode(flowTaskNodeList, item.NodeNext, flowTaskNodeEntities);
            }
        }
    }

    /// <summary>
    /// 递归节点.
    /// </summary>
    /// <param name="flowTaskNodeList">所有节点.</param>
    /// <param name="nodeCode">当前节点.</param>
    /// <param name="flowTaskNodeEntities">指定节点下所有节点.</param>
    /// <param name="isUp">向上递归.</param>
    public async Task RecursiveNode(List<FlowTaskNodeEntity> flowTaskNodeList, string nodeCode, List<FlowTaskNodeEntity> flowTaskNodeEntities, bool isUp = false)
    {
        var thisNodeEntity = flowTaskNodeList.Find(x => x.NodeCode == nodeCode);
        var flowTaskNodeEntityList = new List<FlowTaskNodeEntity>();
        var nodeType = string.Empty;
        if (isUp)
        {
            flowTaskNodeEntityList = flowTaskNodeList.FindAll(x => !FlowTaskNodeTypeEnum.end.ParseToString().Equals(x.NodeCode) && x.NodeNext.Contains(nodeCode));
            nodeType = FlowTaskNodeTypeEnum.start.ParseToString();
        }
        else
        {
            var nextNodes = thisNodeEntity.NodeNext.Split(",").ToList();
            flowTaskNodeEntityList = flowTaskNodeList.FindAll(x => nextNodes.Contains(x.NodeCode));
            nodeType = FlowTaskNodeTypeEnum.end.ParseToString();

        }

        foreach (var item in flowTaskNodeEntityList)
        {
            if (!flowTaskNodeEntities.Any(x => x.Id == item.Id))
            {
                flowTaskNodeEntities.Add(item);
            }
            if (!nodeType.Equals(item.NodeCode))
            {
                await RecursiveNode(flowTaskNodeList, item.NodeCode, flowTaskNodeEntities, isUp);
            }
        }
    }

    /// <summary>
    /// 根据选择分支变更节点.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    public async Task ChangeNodeListByBranch(FlowTaskParamter flowTaskParamter)
    {
        if (flowTaskParamter.branchList.IsNotEmptyOrNull() && flowTaskParamter.branchList.Count > 0)
        {
            // 解决分流合流选择分支后后面的节点只能选择第一个分流节点已选择的分支节点
            var isShuntNodeCompletion = IsShuntNodeCompletion(flowTaskParamter.flowTaskNodeEntityList, flowTaskParamter.flowTaskNodeEntity.NodeNext, flowTaskParamter.flowTaskNodeEntity);
            flowTaskParamter.flowTaskNodeEntityList.RemoveAll(x => FlowTaskNodeTypeEnum.end.ParseToString().Equals(x.NodeCode));
            foreach (var item in flowTaskParamter.flowTaskNodeEntityList)
            {
                //if (flowTaskParamter.flowTaskNodeEntity.Id.IsNotEmptyOrNull() && flowTaskParamter.flowTaskNodeEntity.Id.Equals(item.Id))
                //{
                //    item.NodeNext = string.Join(",", flowTaskParamter.branchList);
                //}
                if (item.NodeNext.Contains(flowTaskParamter.branchList.FirstOrDefault()) && isShuntNodeCompletion)
                {
                    item.NodeNext = string.Join(",", flowTaskParamter.branchList);
                }
                item.State = 1;
                item.SortCode = null;
            }
            UpdateNodeSort(flowTaskParamter.flowTaskNodeEntityList);
            await _flowTaskRepository.UpdateTaskNode(flowTaskParamter.flowTaskNodeEntityList);
            //重新获取当前节点
            flowTaskParamter.flowTaskNodeEntity = flowTaskParamter.flowTaskNodeEntityList.Find(x => x.Id == flowTaskParamter.flowTaskNodeEntity.Id);
        }
    }

    /// <summary>
    /// 驳回处理.
    /// </summary>
    /// <param name="flowTaskParamter">任务参数.</param>
    /// <param name="rejectNodeList">驳回节点.</param>
    /// <param name="rejectNodeCodeList">驳回节点编码.</param>
    /// <param name="rejectNodeCompletion">驳回节点完成度.</param>
    /// <returns></returns>
    public async Task<List<FlowTaskNodeEntity>> RejectManager(FlowTaskParamter flowTaskParamter, List<FlowTaskNodeEntity> rejectNodeList, List<string> rejectNodeCodeList, List<int> rejectNodeCompletion)
    {
        if (!rejectNodeList.Any()) throw Oops.Oh(ErrorCode.WF0032);
        // 保存驳回现有数据.
        if (flowTaskParamter.approversProperties.rejectType == 2)
        {
            flowTaskParamter.flowTaskEntity.RejectDataId = await _flowTaskRepository.CreateRejectData(flowTaskParamter.flowTaskEntity.Id, flowTaskParamter.flowTaskEntity.ThisStepId);
        }
        // 驳回节点下所有节点.
        var rejectNodeNextAllList = new List<FlowTaskNodeEntity>();
        await GetAllNextNode(flowTaskParamter.flowTaskNodeEntityList, rejectNodeList.FirstOrDefault().NodeNext, rejectNodeNextAllList);
        // 驳回到发起
        if (rejectNodeList.Any(x => FlowTaskNodeTypeEnum.start.ParseToString().Equals(x.NodeType)))
        {
            flowTaskParamter.flowTaskEntity.ThisStepId = rejectNodeList.FirstOrDefault().NodeCode;
            flowTaskParamter.flowTaskEntity.ThisStep = "开始";
            flowTaskParamter.flowTaskEntity.Completion = 0;
            flowTaskParamter.flowTaskEntity.FlowUrgent = 0;
            flowTaskParamter.flowTaskEntity.Status = FlowTaskStatusEnum.Reject.ParseToInt();
        }
        else
        {
            // 清空驳回节点下所有节点候选人/异常节点处理人
            if (flowTaskParamter.approversProperties.rejectType == 1)
            {
                _flowTaskRepository.DeleteFlowCandidates(x => rejectNodeNextAllList.Select(x => x.Id).Contains(x.TaskNodeId));
            }
            flowTaskParamter.flowTaskEntity.Completion = rejectNodeCompletion.Min();
            if (flowTaskParamter.flowTaskNodeEntity.NodeUp == "1")
            {
                flowTaskParamter.flowTaskEntity.ThisStepId = GetRejectThisStepId(rejectNodeNextAllList, rejectNodeCodeList, flowTaskParamter.flowTaskEntity.ThisStepId);
                flowTaskParamter.flowTaskEntity.ThisStep = GetThisStep(flowTaskParamter.flowTaskNodeEntityList, flowTaskParamter.flowTaskEntity.ThisStepId);
            }
            else
            {
                flowTaskParamter.flowTaskEntity.ThisStepId = string.Join(",", rejectNodeCodeList);
                flowTaskParamter.flowTaskEntity.ThisStep = GetThisStep(flowTaskParamter.flowTaskNodeEntityList, flowTaskParamter.flowTaskEntity.ThisStepId);
            }
            var rejectNodeIds = rejectNodeList.Union(rejectNodeNextAllList).ToList().Select(x => x.Id).ToList();
            await RejectUpdateTaskNode(rejectNodeList.Union(rejectNodeNextAllList).ToList(), 0);
            // 删除驳回节点下所有经办
            var rejectList = (await _flowTaskRepository.GetTaskOperatorList(x => x.TaskId == flowTaskParamter.flowTaskEntity.Id && rejectNodeIds.Contains(x.TaskNodeId))).OrderBy(x => x.HandleTime).Select(x => x.Id).ToList();
            await _flowTaskRepository.DeleteTaskOperator(rejectList);
            //删除驳回节点经办记录
            var rejectRecodeList = (await _flowTaskRepository.GetTaskOperatorRecordList(x => x.TaskId == flowTaskParamter.flowTaskEntity.Id && rejectNodeIds.Contains(x.TaskNodeId))).OrderBy(x => x.HandleTime).Select(x => x.Id).ToList();
            await _flowTaskRepository.DeleteTaskOperatorRecord(rejectRecodeList);
        }
        return rejectNodeNextAllList.FindAll(x => FlowTaskNodeTypeEnum.subFlow.ParseToString().Equals(x.NodeType));
    }
    #endregion
}
