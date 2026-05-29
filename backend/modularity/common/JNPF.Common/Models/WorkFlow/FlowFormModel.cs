namespace JNPF.Common.Models.WorkFlow
{
    public class FlowFormModel
    {
        /// <summary>
        /// 表单id.
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// 编码.
        /// </summary>
        public string? enCode { get; set; }

        /// <summary>
        /// 名称.
        /// </summary>
        public string? fullName { get; set; }

        /// <summary>
        /// 分类.
        /// </summary>
        public string? category { get; set; }

        /// <summary>
        /// Web地址.
        /// </summary>
        public string? urlAddress { get; set; }

        /// <summary>
        /// APP地址.
        /// </summary>
        public string? appUrlAddress { get; set; }

        /// <summary>
        /// 表单json.
        /// </summary>
        public string? propertyJson { get; set; }

        /// <summary>
        /// 描述.
        /// </summary>
        public string? description { get; set; }

        /// <summary>
        /// 排序码.
        /// </summary>
        public long? sortCode { get; set; }

        /// <summary>
        /// 流程类型（0：发起流程，1：功能流程）.
        /// </summary>
        public int? flowType { get; set; }

        /// <summary>
        /// 表单类型（1：系统表单 2：自定义表单）.
        /// </summary>
        public int? formType { get; set; }

        /// <summary>
        /// 关联表单.
        /// </summary>
        public string? tableJson { get; set; }

        /// <summary>
        /// 数据源id.
        /// </summary>
        public string? dbLinkId { get; set; }

        /// <summary>
        /// 接口路径.
        /// </summary>
        public string? interfaceUrl { get; set; }

        /// <summary>
        /// 表单json草稿.
        /// </summary>
        public string? draftJson { get; set; }

        /// <summary>
        /// 引擎id.
        /// </summary>
        public string? flowId { get; set; }
    }
}
