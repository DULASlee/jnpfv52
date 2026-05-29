using JNPF.DependencyInjection;

namespace JNPF.Common.Security;

/// <summary>
/// 比较器辅助类，用于快速创建<see cref="IComparer{T}"/>接口的实例.
/// </summary>
/// <example>
/// var comparer1 = Comparison[Person].CreateComparer(p => p.ID); var comparer2 = Comparison[Person].CreateComparer(p => p.Name); var comparer3 = Comparison[Person].CreateComparer(p => p.Birthday.Year).
/// </example>
/// <typeparam name="T">要比较的类型.</typeparam>
[SuppressSniffer]
public static class ComparisonHelper<T>
{
    /// <summary>
    /// 创建指定对比委托<paramref name="keySelector"/>的实例.
    /// </summary>
    public static IComparer<T> CreateComparer<TV>(Func<T, TV> keySelector)
    {
        return new CommonComparer<TV>(keySelector);
    }

    /// <summary>
    /// 创建指定对比委托<paramref name="keySelector"/>与结果二次比较器<paramref name="comparer"/>的实例
    /// </summary>
    public static IComparer<T> CreateComparer<TV>(Func<T, TV> keySelector, IComparer<TV> comparer)
    {
        return new CommonComparer<TV>(keySelector, comparer);
    }

    /// <summary>
    /// 常见的比较器.
    /// </summary>
    /// <typeparam name="TV">要比较的类型.</typeparam>
    private class CommonComparer<TV> : IComparer<T>
    {
        private readonly IComparer<TV> _comparer;
        private readonly Func<T, TV> _keySelector;

        /// <summary>
        /// 构造函数.
        /// </summary>
        /// <param name="keySelector"></param>
        /// <param name="comparer"></param>
        public CommonComparer(Func<T, TV> keySelector, IComparer<TV> comparer)
        {
            _keySelector = keySelector;
            _comparer = comparer;
        }

        /// <summary>
        /// 构造函数.
        /// </summary>
        /// <param name="keySelector"></param>
        public CommonComparer(Func<T, TV> keySelector)
            : this(keySelector, Comparer<TV>.Default)
        {
        }

        /// <summary>
        /// 比较.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(T x, T y)
        {
            return _comparer.Compare(_keySelector(x), _keySelector(y));
        }
    }
}