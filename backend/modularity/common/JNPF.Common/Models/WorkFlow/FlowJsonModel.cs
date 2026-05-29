namespace JNPF.Common.Models.WorkFlow
{
    public class FlowJsonModel
    {
        /// <summary>
        /// id.
        /// </summary>
        public string? id { get; set; }

        /// <summary>
        /// 流程id.
        /// </summary>
        public string? templateId { get; set; }

        /// <summary>
        /// 可见范围.
        /// </summary>
        public int? visibleType { get; set; }

        /// <summary>
        /// 版本.
        /// </summary>
        public string? version { get; set; }

        /// <summary>
        /// 流程JOSN包.
        /// </summary>
        public string? flowTemplateJson { get; set; }

        /// <summary>
        /// 流程分类.
        /// </summary>
        public string? category { get; set; }

        /// <summary>
        /// 流程编号.
        /// </summary>
        public string? enCode { get; set; }

        /// <summary>
        /// 流程名称.
        /// </summary>
        public string? fullName { get; set; }

        /// <summary>
        /// 流程类型.
        /// </summary>
        public int? type { get; set; }

        /// <summary>
        /// 所属流程名称.
        /// </summary>
        public string? flowName { get; set; }
    }
}
