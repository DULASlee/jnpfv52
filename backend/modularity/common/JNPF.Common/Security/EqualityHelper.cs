using JNPF.DependencyInjection;

namespace JNPF.Common.Security;

/// <summary>
/// 相等比较辅助类，用于快速创建<see cref="IEqualityComparer{T}"/>的实例.
/// </summary>
/// <example>
/// var equalityComparer1 = EqualityHelper[Person].CreateComparer(p => p.ID);
/// var equalityComparer2 = EqualityHelper[Person].CreateComparer(p => p.Name);
/// var equalityComparer3 = EqualityHelper[Person].CreateComparer(p => p.Birthday.Year).
/// </example>
/// <typeparam name="T">要比较的类型.</typeparam>
[SuppressSniffer]
public static class EqualityHelper<T>
{
    /// <summary>
    /// 创建指定对比委托<paramref name="keySelector"/>的实例.
    /// </summary>
    public static IEqualityComparer<T> CreateComparer<TV>(Func<T, TV> keySelector)
    {
        return new CommonEqualityComparer<TV>(keySelector);
    }

    /// <summary>
    /// 创建指定对比委托<paramref name="keySelector"/>与结果二次比较器<paramref name="comparer"/>的实例.
    /// </summary>
    public static IEqualityComparer<T> CreateComparer<TV>(Func<T, TV> keySelector, IEqualityComparer<TV> comparer)
    {
        return new CommonEqualityComparer<TV>(keySelector, comparer);
    }

    /// <summary>
    /// 共同平等比较器.
    /// </summary>
    /// <typeparam name="TV">要比较的类型.</typeparam>
    private class CommonEqualityComparer<TV> : IEqualityComparer<T>
    {
        private readonly IEqualityComparer<TV> _comparer;
        private readonly Func<T, TV> _keySelector;

        public CommonEqualityComparer(Func<T, TV> keySelector, IEqualityComparer<TV> comparer)
        {
            _keySelector = keySelector;
            _comparer = comparer;
        }

        public CommonEqualityComparer(Func<T, TV> keySelector)
            : this(keySelector, EqualityComparer<TV>.Default)
        { }

        public bool Equals(T x, T y)
        {
            return _comparer.Equals(_keySelector(x), _keySelector(y));
        }

        public int GetHashCode(T obj)
        {
            return _comparer.GetHashCode(_keySelector(obj));
        }
    }
}
