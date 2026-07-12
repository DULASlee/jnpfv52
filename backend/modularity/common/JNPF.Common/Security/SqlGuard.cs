using System.Text.RegularExpressions;
using JNPF.FriendlyException;

namespace JNPF.Common.Security;

/// <summary>
/// SQL 标识符（表名/字段名）安全校验
/// </summary>
public static partial class SqlGuard
{
    /// <summary>
    /// 合法 SQL 标识符正则：字母或下划线开头，后接字母/数字/下划线
    /// </summary>
    private static readonly Regex ValidIdentifierRegex = new(
        @"^[a-zA-Z_][a-zA-Z0-9_]*$",
        RegexOptions.Compiled);

    /// <summary>
    /// 校验 SQL 标识符（表名/字段名）是否为合法格式，否则抛出业务异常
    /// </summary>
    /// <param name="identifier">待校验的标识符</param>
    /// <param name="label">标识符类别标签（用于错误消息，如 "表名"、"字段名"）</param>
    /// <exception cref="JNPFException">标识符包含非法字符时抛出</exception>
    public static void ValidateIdentifier(string identifier, string label = "标识符")
    {
        if (string.IsNullOrEmpty(identifier) || !ValidIdentifierRegex.IsMatch(identifier))
        {
            throw Oops.Bah($"非法的{label}：{identifier}");
        }
    }
}
