namespace JNPF.Common.Models.WorkFlow
{
    /// <summary>
    /// 工作流提交模型.
    /// </summary>
    public class FlowTaskSubmitModel : FlowTaskOtherModel
    {
        /// <summary>
        /// 任务主键id(id有值则是修改，反之就是新增)
        /// 在线开发：因为保存无需生成任务只有提交才会创建任务且所以id传空
        /// 代码生成：无论保存还是提交第一次id传空，编辑时候id的值为processId.
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// 流程id.
        /// </summary>
        public string flowId { get; set; }

        /// <summary>
        /// 关联表数据id(必定有值).
        /// </summary>
        public string processId { get; set; }

        /// <summary>
        /// 任务标题.
        /// </summary>
        public string flowTitle { get; set; }

        /// <summary>
        /// 紧急程度.
        /// </summary>
        public int? flowUrgent { get; set; }

        /// <summary>
        /// 任务编码(数据来源前端传参或通过单据规则获取).
        /// </summary>
        public string billNo { get; set; }

        /// <summary>
        /// 表单数据.
        /// </summary>
        public object formData { get; set; }

        /// <summary>
        /// 状态 1:保存，0提交..
        /// </summary>
        public int status { get; set; }

        /// <summary>
        /// 审批修改权限1：可写，0：可读..
        /// </summary>
        public int approvaUpType { get; set; } = 0;

        /// <summary>
        /// true：系统表单，false：自定义表单.
        /// </summary>
        public bool isSysTable { get; set; } = true;

        /// <summary>
        /// 是否功能设计.
        /// </summary>
        public bool isDev { get; set; } = false;

        /// <summary>
        /// 流程父流程id(0:顶级流程，其他：子流程) 工作流使用.
        /// </summary>
        public string parentId { get; set; } = "0";

        /// <summary>
        /// 流程发起人 工作流使用.
        /// </summary>
        public string crUser { get; set; } = null;

        /// <summary>
        /// 是否异步流程 工作流使用.
        /// </summary>
        public bool isAsync { get; set; } = false;

        /// <summary>
        /// 流程信息.
        /// </summary>
        public FlowJsonModel flowJsonModel { get; set; }

        /// <summary>
        /// 是否委托发起流程 工作流使用.
        /// </summary>
        public bool isDelegate { get; set; } = false;
    }

    /// <summary>
    /// 流程任务其他参数.
    /// </summary>
    public class FlowTaskOtherModel
    {
        /// <summary>
        /// 候选人.
        /// </summary>
        public Dictionary<string, List<string>> candidateList { get; set; }

        /// <summary>
        /// 选择分支.
        /// </summary>
        public List<string> branchList { get; set; }

        /// <summary>
        /// 异常审批人.
        /// </summary>
        public Dictionary<string, List<string>> errorRuleUserList { get; set; }

        /// <summary>
        /// 自定义抄送人.
        /// </summary>
        public string? copyIds { get; set; }

        /// <summary>
        /// 委托发起人.
        /// </summary>
        public List<string> delegateUserList { get; set; } = new List<string>();

        /// <summary>
        /// 子流程任务实例.
        /// </summary>
        public string childTask { get; set; }

        /// <summary>
        /// 是否自动提交.
        /// </summary>
        public bool autoSubmit { get; set; }
    }
}
