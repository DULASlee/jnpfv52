using JNPF.DependencyInjection;

namespace JNPF.Common.Configuration;

/// <summary>
/// 配置文件.
/// </summary>
[SuppressSniffer]
public class FileVariable
{
    public static string SystemPath = KeyVariable.SystemPath;

    public static string MultiSystemPath = KeyVariable.MultiSystemPath;


    /// <summary>
    /// 用户头像存储路径.
    /// </summary>
    public static string UserAvatarFilePath = Path.Combine(SystemPath, "UserAvatar");

    /// <summary>
    /// 临时文件存储路径.
    /// </summary>
    public static string TemporaryFilePath = Path.Combine(SystemPath, "TemporaryFile");

    /// <summary>
    /// 备份数据存储路径.
    /// </summary>
    public static string DataBackupFilePath = Path.Combine(SystemPath, "DataBackupFile");

    /// <summary>
    /// IM内容文件存储路径.
    /// </summary>
    public static string IMContentFilePath = Path.Combine(SystemPath, "IMContentFile");

    /// <summary>
    /// 系统文件存储路径.
    /// </summary>
    public static string SystemFilePath = Path.Combine(MultiSystemPath, "SystemFile");

    /// <summary>
    /// 微信公众号资源存储路径.
    /// </summary>
    public static string MPMaterialFilePath = Path.Combine(SystemPath, "MPMaterial");

    /// <summary>
    /// 文档管理存储路径.
    /// </summary>
    public static string DocumentFilePath = Path.Combine(SystemPath, "DocumentFile");

    /// <summary>
    /// 生成代码路径.
    /// </summary>
    public static string GenerateCodePath = Path.Combine(SystemPath, "CodeGenerate");

    /// <summary>
    /// 文件在线预览存储PDF.
    /// </summary>
    public static string DocumentPreviewFilePath = Path.Combine(SystemPath, "DocumentPreview");

    /// <summary>
    /// 邮件文件存储路径.
    /// </summary>
    public static string EmailFilePath = Path.Combine(SystemPath, "EmailFile");

    /// <summary>
    /// 大屏图片路径.
    /// </summary>
    public static string BiVisualPath = Path.Combine(SystemPath, "BiVisualPath");

    /// <summary>
    /// 模板路径.
    /// </summary>
    public static string TemplateFilePath = Path.Combine(SystemPath, "TemplateFile");
}