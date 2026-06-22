using System;

namespace JNPF.InteAssistant.Gates;

/// <summary>
/// SA 门控共享常量
/// </summary>
public static class GateConstants
{
    /// <summary>支持的图片扩展名</summary>
    public static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

    /// <summary>是否为图片文件</summary>
    public static bool IsImageFile(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(ext) && ImageExtensions.Contains(ext);
    }

    /// <summary>支持的文档扩展名</summary>
    public static readonly HashSet<string> DocumentExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".docx", ".doc", ".xlsx", ".xls", ".pdf", ".txt", ".csv", ".md" };

    /// <summary>是否为文档文件</summary>
    public static bool IsDocumentFile(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(ext) && DocumentExtensions.Contains(ext);
    }

    /// <summary>小文件阈值（字节）— 超过此值写临时文件</summary>
    public const int LargeFileThreshold = 5 * 1024 * 1024; // 5MB
}
