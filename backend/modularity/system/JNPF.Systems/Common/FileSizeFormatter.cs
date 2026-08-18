using JNPF.DependencyInjection;

namespace JNPF.Systems.Common;

/// <summary>
/// 文件大小格式化工具——将字节数转为人类可读格式.
/// </summary>
[SuppressSniffer]
public static class FileSizeFormatter
{
    private static readonly string[] Suffixes = ["B", "KB", "MB", "GB"];

    /// <summary>
    /// 格式化字节数为人类可读字符串.
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <returns>格式化后的字符串，如 "1.50 KB"</returns>
    public static string Format(long bytes)
    {
        if (bytes < 0)
            return "0 B";

        var order = 0;
        double size = bytes;

        while (size >= 1024 && order < Suffixes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return order == 0
            ? $"{bytes} B"
            : $"{size:F2} {Suffixes[order]}";
    }
}
