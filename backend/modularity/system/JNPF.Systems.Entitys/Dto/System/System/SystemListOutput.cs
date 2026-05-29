using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.System
{
    /// <summary>
    /// 系统列表输出.
    /// </summary>
    [SuppressSniffer]
    public class SystemListOutput
    {
        /// <summary>
        /// id.
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// 系统名称.
        /// </summary>
        public string fullName { get; set; }

        /// <summary>
        /// 系统编码.
        /// </summary>
        public string enCode { get; set; }

        /// <summary>
        /// 图标.
        /// </summary>
        public string icon { get; set; }

        /// <summary>
        /// 排序.
        /// </summary>
        public long? sortCode { get; set; }

        /// <summary>
        /// 状态.
        /// </summary>
        public int? enabledMark { get; set; }

        /// <summary>
        /// 说明.
        /// </summary>
        public string description { get; set; }

        /// <summary>
        /// 是否主应用（0不是 1 是）.
        /// </summary>
        public int? isMain { get; set; }

        /// <summary>
        /// 是否为开发平台.
        /// </summary>
        public bool mainSystem { get; set; } = false;
    }
}
