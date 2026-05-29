using JNPF.DependencyInjection;
using System.Text;

namespace JNPF.Common.Extension;

/// <summary>
/// Enumerable集合扩展方法.
/// </summary>
[SuppressSniffer]
public static class EnumerableExtensions
{
    /// <summary>
    /// 将集合展开并分别转换成字符串，再以指定的分隔符衔接，拼成一个字符串返回。默认分隔符为逗号.
    /// </summary>
    /// <param name="collection"> 要处理的集合. </param>
    /// <param name="separator"> 分隔符，默认为逗号. </param>
    /// <returns> 拼接后的字符串. </returns>
    public static string ExpandAndToString<T>(this IEnumerable<T> collection, string separator = ",")
    {
        return collection.ExpandAndToString(item => item?.ToString() ?? string.Empty, separator);
    }

    /// <summary>
    /// 循环集合的每一项，调用委托生成字符串，返回合并后的字符串。默认分隔符为逗号.
    /// </summary>
    /// <param name="collection">待处理的集合.</param>
    /// <param name="itemFormatFunc">单个集合项的转换委托.</param>
    /// <param name="separator">分隔符，默认为逗号.</param>
    /// <typeparam name="T">泛型类型.</typeparam>
    /// <returns></returns>
    public static string ExpandAndToString<T>(this IEnumerable<T> collection, Func<T, string> itemFormatFunc, string separator = ",")
    {
        collection = collection as IList<T> ?? collection.ToList();
        if (!collection.Any())
        {
            return string.Empty;
        }

        StringBuilder sb = new StringBuilder();
        int i = 0;
        int count = collection.Count();
        foreach (T item in collection)
        {
            if (i == count - 1)
            {
                sb.Append(itemFormatFunc(item));
            }
            else
            {
                sb.Append(itemFormatFunc(item) + separator);
            }

            i++;
        }

        return sb.ToString();
    }

    /// <summary>
    /// 集合是否为空.
    /// </summary>
    /// <param name="collection"> 要处理的集合. </param>
    /// <typeparam name="T"> 动态类型. </typeparam>
    /// <returns> 为空返回True，不为空返回False. </returns>
    public static bool IsEmpty<T>(this IEnumerable<T> collection)
    {
        collection = collection as IList<T> ?? collection.ToList();
        return !collection.Any();
    }
}