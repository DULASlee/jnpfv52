using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Dto.FlowForm
{
    [SuppressSniffer]
    public class FlowFormListOutput
    {
        /// <summary>
        /// id.
        /// </summary>
        public string? id { get; set; }

        /// <summary>
        /// 流程名称.
        /// </summary>
        public string? fullName { get; set; }

        /// <summary>
        /// 流程编号.
        /// </summary>
        public string? enCode { get; set; }

        /// <summary>
        /// 排序码.
        /// </summary>
        public long? sortCode { get; set; }

        /// <summary>
        /// 表单类型(数据字典-流程表单类型).
        /// </summary>
        public int? formType { get; set; }

        /// <summary>
        /// 流程分类(数据字典-工作流-流程分类).
        /// </summary>
        public string? category { get; set; }

        /// <summary>
        /// 流程类型（0：发起流程，1：功能流程）.
        /// </summary>
        public int? type { get; set; }

        /// <summary>
        /// 图标.
        /// </summary>
        public string? icon { get; set; }

        /// <summary>
        /// 图标背景.
        /// </summary>
        public string? iconBackground { get; set; }

        /// <summary>
        /// 说明.
        /// </summary>
        public string? description { get; set; }

        /// <summary>
        /// 状态(0-关闭，1-开启).
        /// </summary>
        public int? enabledMark { get; set; }

        /// <summary>
        /// 表单JSON包.
        /// </summary>
        public string? formData { get; set; }

        /// <summary>
        /// 流程JOSN包.
        /// </summary>
        public string? flowTemplateJson { get; set; }

        /// <summary>
        /// 表信息数据.
        /// </summary>
        public string? tables { get; set; }

        /// <summary>
        /// 数据库连接id.
        /// </summary>
        public string? dbLinkId { get; set; }

        /// <summary>
        /// app地址.
        /// </summary>
        public string? appFormUrl { get; set; }

        /// <summary>
        /// 表单地址.
        /// </summary>
        public string? formUrl { get; set; }

        /// <summary>
        /// 创建人.
        /// </summary>
        public string? creatorUser { get; set; }

        /// <summary>
        /// 创建时间.
        /// </summary>
        public DateTime? creatorTime { get; set; }

        /// <summary>
        /// 修改时间.
        /// </summary>
        public DateTime? lastModifyTime { get; set; }

        /// <summary>
        /// 修改人.
        /// </summary>
        public string? lastModifyUser { get; set; }

        /// <summary>
        /// 表单json草稿.
        /// </summary>
        public string? draftJson { get; set; }

        /// <summary>
        /// 接口路径.
        /// </summary>
        public string? interfaceUrl { get; set; }

        /// <summary>
        /// 关联表单.
        /// </summary>
        public string? tableJson { get; set; }

        /// <summary>
        /// 流程类型（0：发起流程，1：功能流程）.
        /// </summary>
        public int? flowType { get; set; }

        /// <summary>
        /// 表单模型.
        /// </summary>
        public string? propertyJson { get; set; }

        /// <summary>
        /// Web地址.
        /// </summary>
        public string urlAddress { get; set; }

        /// <summary>
        /// APP地址.
        /// </summary>
        public string appUrlAddress { get; set; }

        /// <summary>
        /// 发布状态.
        /// </summary>
        public int? isRelease { get; set; }

        /// <summary>
        /// 是否引用(0-否 1-是).
        /// </summary>
        public int? isQuote { get; set; }
    }
}
