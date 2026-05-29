using JNPF.DependencyInjection;

namespace JNPF.Common.Models
{
    /// <summary>
    /// 附件模型
    /// 版 本：V3.3.3
    /// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
    /// 作 者：JNPF开发平台组.
    /// </summary>
    [SuppressSniffer]
    public class AnnexModel
    {
        /// <summary>
        /// 文件ID.
        /// </summary>
        public string? FileId { get; set; }

        /// <summary>
        /// 文件名称.
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// 文件大小.
        /// </summary>
        public string? FileSize { get; set; }

        /// <summary>
        /// 文件上传时间.
        /// </summary>
        public DateTime FileTime { get; set; }

        /// <summary>
        /// 文件状态.
        /// </summary>
        public string? FileState { get; set; }

        /// <summary>
        /// 文件类型.
        /// </summary>
        public string? FileType { get; set; }
    }
}
