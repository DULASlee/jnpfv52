using JNPF.DependencyInjection;

namespace JNPF.Common.Extension;

/// <summary>
/// 布尔值<see cref="bool"/>类型的扩展辅助操作类.
/// </summary>
[SuppressSniffer]
public static class BooleanExtensions
{
    /// <summary>
    /// 把布尔值转换为小写字符串.
    /// </summary>
    /// <param name="value"></param>
    public static string ToLower(this bool value)
    {
        return value.ToString().ToLower();
    }

    /// <summary>
    /// 如果条件成立，则抛出异常.
    /// </summary>
    public static void TrueThrow(this bool flag, Exception exception)
    {
        if (flag)
            throw exception;
    }

    /// <summary>
    /// 获取布尔值.
    /// </summary>
    private static bool? GetBool(this object data)
    {
        switch (data.ToString().Trim().ToLower())
        {
            case "0":
                return false;
            case "1":
                return true;
            case "是":
                return true;
            case "否":
                return false;
            case "yes":
                return true;
            case "no":
                return false;
            default:
                return null;
        }
    }
}