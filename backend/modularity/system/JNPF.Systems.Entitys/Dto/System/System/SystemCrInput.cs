using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.System
{
    /// <summary>
    /// 系统功能创建输入.
    /// </summary>
    [SuppressSniffer]
    public class SystemCrInput
    {
        /// <summary>
        /// id.
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// 名称.
        /// </summary>
        public string fullName { get; set; }

        /// <summary>
        /// 编码.
        /// </summary>
        public string enCode { get; set; }

        /// <summary>
        /// 图标.
        /// </summary>
        public string icon { get; set; }

        /// <summary>
        /// 状态(1-可用,0-禁用).
        /// </summary>
        public int? enabledMark { get; set; }

        /// <summary>
        /// 说明.
        /// </summary>
        public string description { get; set; }

        /// <summary>
        /// 排序.
        /// </summary>
        public long? sortCode { get; set; }

        /// <summary>
        /// 扩展字段.
        /// </summary>
        public string propertyJson { get; set; }

        /// <summary>
        /// 导航图标.
        /// </summary>
        public string navigationIcon { get; set; }

        /// <summary>
        /// logo图标.
        /// </summary>
        public string workLogoIcon { get; set; }

        /// <summary>
        /// 协同办公（0-关闭，1-开启）.
        /// </summary>
        public int? workflowEnabled { get; set; }
    }
}
