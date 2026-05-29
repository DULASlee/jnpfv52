using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DbBackup;

/// <summary>
/// 数据备份列表输出.
/// </summary>
[SuppressSniffer]
public class DbBackupListOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 文件名.
    /// </summary>
    public string fileName { get; set; }

    /// <summary>
    /// 文件大小.
    /// </summary>
    public string fileSize { get; set; }

    /// <summary>
    /// 备份时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 文件下载地址.
    /// </summary>
    public string fileUrl { get; set; }
}