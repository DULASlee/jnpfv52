using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Model.Properties;

namespace JNPF.WorkFlow.Entitys.Model
{
    public class FlowTaskParamter : FlowHandleModel
    {
        /// <summary>
        /// 当前任务.
        /// </summary>
        public FlowTaskEntity flowTaskEntity { get; set; }

        /// <summary>
        /// 开始节点属性.
        /// </summary>
        public StartProperties startProperties { get; set; }

        /// <summary>
        /// 所有节点(可用).
        /// </summary>
        public List<FlowTaskNodeEntity> flowTaskNodeEntityList { get; set; }

        /// <summary>
        /// 当前节点.
        /// </summary>
        public FlowTaskNodeEntity flowTaskNodeEntity { get; set; }

        /// <summary>
        /// 当前节点属性.
        /// </summary>
        public ApproversProperties approversProperties { get; set; }

        /// <summary>
        /// 当前经办.
        /// </summary>
        public FlowTaskOperatorEntity flowTaskOperatorEntity { get; set; }

        /// <summary>
        /// 当前节点所有经办.
        /// </summary>
        public List<FlowTaskOperatorEntity> thisFlowTaskOperatorEntityList { get; set; }

        #region 容器

        /// <summary>
        /// 下一节点所有经办.
        /// </summary>
        public List<FlowTaskOperatorEntity> flowTaskOperatorEntityList { get; set; } = new List<FlowTaskOperatorEntity>();

        /// <summary>
        /// 当前节点抄送.
        /// </summary>
        public List<FlowTaskCirculateEntity> flowTaskCirculateEntityList { get; set; } = new List<FlowTaskCirculateEntity>();

        /// <summary>
        /// 异常节点.
        /// </summary>
        public List<FlowTaskCandidateModel> errorNodeList { get; set; } = new List<FlowTaskCandidateModel>();
        #endregion

        /// <summary>
        /// 是否自动审批.
        /// </summary>
        public bool isAuto { get; set; } = false;
    }
}
