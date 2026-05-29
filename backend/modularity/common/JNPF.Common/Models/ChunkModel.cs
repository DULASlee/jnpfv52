using JNPF.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace JNPF.Common.Models;

/// <summary>
/// 文件分片模型
/// 版 本：V3.3.3
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组.
/// </summary>
[SuppressSniffer]
public class ChunkModel
{
    /// <summary>
    /// 当前文件块，从1开始.
    /// </summary>
    public int chunkNumber { get; set; }

    /// <summary>
    /// 当前分块大小.
    /// </summary>
    public int currentChunkSize { get; set; }

    /// <summary>
    /// 分块大小.
    /// </summary>
    public long chunkSize { get; set; }

    /// <summary>
    /// 总大小.
    /// </summary>
    public long totalSize { get; set; }

    /// <summary>
    /// 文件标识.
    /// </summary>
    public string identifier { get; set; }

    /// <summary>
    /// 文件名.
    /// </summary>
    public string fileName { get; set; }

    /// <summary>
    /// 相对路径.
    /// </summary>
    public string relativePath { get; set; }

    /// <summary>
    /// 总块数.
    /// </summary>
    public int totalChunks { get; set; }

    /// <summary>
    /// 文件存储类型.
    /// </summary>
    public string type { get; set; } = "annex";

    /// <summary>
    /// 文件后缀.
    /// </summary>
    public string extension { get; set; }

    /// <summary>
    /// 文件类型.
    /// </summary>
    public string fileType { get; set; }

    /// <summary>
    /// 上级id.
    /// </summary>
    public string parentId { get; set; }

    /// <summary>
    /// 文件大小.
    /// </summary>
    public string fileSize { get; set; }

    /// <summary>
    /// 是否生成文件名.
    /// </summary>
    public bool isUpdateName { get; set; } = true;

    /// <summary>
    /// 文件.
    /// </summary>
    public IFormFile file { get; set; }

    /// <summary>
    /// 路径类型 defaultPath(默认路径) selfPath(自定义路径).
    /// </summary>
    public string pathType { get; set; } = "defaultPath";

    /// <summary>
    /// 是否用户存储（0否1是）.
    /// </summary>
    public string isAccount { get; set; } = "0";

    /// <summary>
    /// 自定义文件夹路径.
    /// </summary>
    public string folder { get; set; }

    /// <summary>
    /// 缩略图文件名.
    /// </summary>
    public string slImgName { get; set; }
}
