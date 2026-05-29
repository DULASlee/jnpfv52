using JNPF.DependencyInjection;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Dynamic;
using System.Reflection;

namespace JNPF.Common.Extension;

/// <summary>
/// 多线程下的字典辅助扩展方法.
/// </summary>
[SuppressSniffer]
public static class ConcurrentDictionaryExtensions
{
    private static readonly ConcurrentDictionary<RuntimeTypeHandle, PropertyInfo[]> _dynamicObjectProperties = new ConcurrentDictionary<RuntimeTypeHandle, PropertyInfo[]>();

    /// <summary>
    /// 获取object属性.
    /// </summary>
    /// <typeparam name="T">对象类型.</typeparam>
    /// <returns></returns>
    private static PropertyInfo[] GetObjectProperties<T>()
    {
        var type = typeof(T);
        var key = type.TypeHandle;
        PropertyInfo[] queryPts = null;

        _dynamicObjectProperties.TryGetValue(key, out queryPts);

        if (queryPts == null)
        {
            queryPts = type.GetProperties();
            _dynamicObjectProperties.TryAdd(key, queryPts);
        }

        return queryPts;
    }

    /// <summary>
    /// 合并2个对象.
    /// </summary>
    /// <typeparam name="TSource">对象1类型.</typeparam>
    /// <typeparam name="TTarget">对象2类型.</typeparam>
    /// <param name="s">对象1实例.</param>
    /// <param name="t">对象2实例.</param>
    /// <returns>合并后的动态对象.</returns>
    public static IDictionary<string, object> MergerObject<TSource, TTarget>(TSource s, TTarget t)
    {
        var targetPts = GetObjectProperties<TSource>();

        PropertyInfo[] mergerPts = null;
        var _type = t.GetType();
        mergerPts = _type.Name.Contains("<>") ? _type.GetProperties() : GetObjectProperties<TTarget>();

        var dynamicResult = new ExpandoObject() as IDictionary<string, object>;

        foreach (var p in targetPts)
        {
            var attributes = p.GetCustomAttributes(typeof(JsonIgnoreAttribute), true);
            if (attributes.FirstOrDefault() != null) continue;

            dynamicResult.Add(p.Name, p.GetValue(s, null));
        }

        foreach (var p in mergerPts)
        {
            var attributes = p.GetCustomAttributes(typeof(JsonIgnoreAttribute), true);
            if (attributes.FirstOrDefault() != null) continue;

            dynamicResult.Add(p.Name, p.GetValue(t, null));
        }

        return dynamicResult;
    }

    /// <summary>
    /// 对象2值赋值至对象1内.
    /// </summary>
    /// <typeparam name="TSource">对象1类型.</typeparam>
    /// <typeparam name="TTarget">对象2类型.</typeparam>
    /// <param name="s">对象1实例.</param>
    /// <param name="t">对象2实例.</param>
    /// <returns>合并后的动态对象.</returns>
    public static IDictionary<string, object> AssignmentObject<TSource, TTarget>(TSource s, TTarget t)
    {
        var targetPts = GetObjectProperties<TSource>();

        PropertyInfo[] mergerPts = null;
        var _type = t.GetType();
        mergerPts = _type.Name.Contains("<>") ? _type.GetProperties() : GetObjectProperties<TTarget>();

        var dynamicResult = new ExpandoObject() as IDictionary<string, object>;

        foreach (var p in targetPts)
        {
            var attributes = p.GetCustomAttributes(typeof(JsonIgnoreAttribute), true);
            if (attributes.FirstOrDefault() != null) continue;

            dynamicResult.Add(p.Name, p.GetValue(s, null));
        }

        foreach (var p in mergerPts)
        {
            var attributes = p.GetCustomAttributes(typeof(JsonIgnoreAttribute), true);
            if (attributes.FirstOrDefault() != null) continue;

            dynamicResult[p.Name] = p.GetValue(t, null);
        }

        return dynamicResult;
    }

    /// <summary>
    /// 合并2个对象.
    /// var result = MergerListObject<KeyValue, dynamic>(kk, new { p = new KeyValue2() { key2 = "dadad", key3 = "biubiu" } });
    /// </summary>
    /// <typeparam name="TSource">对象1类型.</typeparam>
    /// <typeparam name="TTarget">对象2类型.</typeparam>
    /// <param name="s">对象1实例.</param>
    /// <param name="t">对象2实例.</param>
    /// <returns>合并后的动态对象.</returns>
    public static List<IDictionary<string, object>> MergerListObject<TSource, TTarget>(List<TSource> s, TTarget t)
    {
        var targetPts = GetObjectProperties<TSource>();

        PropertyInfo[] mergerPts = null;
        var _type = t.GetType();
        mergerPts = _type.Name.Contains("<>") ? _type.GetProperties() : GetObjectProperties<TTarget>();

        var result = new List<IDictionary<string, object>>();

        s.ForEach(x =>
        {
            var dynamicResult = new ExpandoObject() as IDictionary<string, object>;

            foreach (var p in targetPts)
            {
                var attributes = p.GetCustomAttributes(typeof(JsonIgnoreAttribute), true);
                if (attributes.FirstOrDefault() != null) continue;

                dynamicResult.Add(p.Name, p.GetValue(x, null));
            }

            foreach (var p in mergerPts)
            {
                var attributes = p.GetCustomAttributes(typeof(JsonIgnoreAttribute), true);
                if (attributes.FirstOrDefault() != null) continue;

                dynamicResult.Add(p.Name, p.GetValue(t, null));
            }

            result.Add(dynamicResult);
        });

        return result;
    }
}