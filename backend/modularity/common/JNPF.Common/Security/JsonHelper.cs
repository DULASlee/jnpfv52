using JNPF.JsonSerialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JNPF.Common.Security;

/// <summary>
/// JsonHelper
/// 版 本：V3.4.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组.
/// </summary>
public static class JsonHelper
{
    /// <summary>
    /// 序列化提供器.
    /// </summary>
    public static IJsonSerializerProvider _jsonSerializer = App.GetService(typeof(NewtonsoftJsonSerializerProvider), App.RootServices) as IJsonSerializerProvider;

    /// <summary>
    /// Object 转 JSON字符串.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string ToJsonString(this object obj)
    {
        return obj == null ? string.Empty : JsonConvert.SerializeObject(obj);
    }

    /// <summary>
    /// Object 转 JSON字符串(时间格式不转换).
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string ToJsonStringOld(this object obj)
    {
        return obj == null ? string.Empty : JsonConvert.SerializeObject(obj);
    }

    /// <summary>
    /// Object 转 JSON字符串.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="jsonSerializerOptions">序列化规则.</param>
    /// <returns></returns>
    public static string ToJsonString(this object obj, object jsonSerializerOptions = default)
    {
        return obj == null ? string.Empty : JsonConvert.SerializeObject(obj);
    }

    /// <summary>
    /// JSON 字符串转 Object.
    /// </summary>
    /// <typeparam name="T">动态类型.</typeparam>
    /// <param name="json">对象.</param>
    /// <returns></returns>
    public static T ToObject<T>(this string json)
    {
        return _ = JsonConvert.DeserializeObject<T>(json) ?? default(T);
    }

    /// <summary>
    /// JSON 字符串转 Object.
    /// </summary>
    /// <typeparam name="T">动态类型.</typeparam>
    /// <param name="json">对象.</param>
    /// <returns></returns>
    public static T ToObjectOld<T>(this string json)
    {
        return _ = JsonConvert.DeserializeObject<T>(json) ?? default(T);
    }

    /// <summary>
    /// JSON 字符串转 Object.
    /// </summary>
    /// <typeparam name="T">动态类型.</typeparam>
    /// <param name="json">对象.</param>
    /// <param name="jsonSerializerOptions">序列化规则.</param>
    /// <returns></returns>
    public static T ToObject<T>(this string json, object jsonSerializerOptions = default)
    {
        return _ = JsonConvert.DeserializeObject<T>(json) ?? default(T);
    }

    /// <summary>
    /// Object 转 对象.
    /// </summary>
    /// <typeparam name="T">动态类型.</typeparam>
    /// <param name="json"></param>
    /// <returns></returns>
    public static T ToObject<T>(this object json)
    {
        return _ = ToJsonString(json).ToObject<T>() ?? default(T);
    }

    /// <summary>
    /// Object 转 对象.
    /// </summary>
    /// <typeparam name="T">动态类型.</typeparam>
    /// <param name="json"></param>
    /// <param name="jsonSerializerOptions">序列化规则.</param>
    /// <returns></returns>
    public static T ToObject<T>(this object json, object jsonSerializerOptions = default)
    {
        return _ = ToJsonString(json, jsonSerializerOptions).ToObject<T>(jsonSerializerOptions) ?? default(T);
    }

    /// <summary>
    /// 字符串转动态类型List.
    /// </summary>
    /// <typeparam name="T">动态类型.</typeparam>
    /// <param name="json"></param>
    /// <returns></returns>
    public static List<T> ToList<T>(this string json)
    {
        return _ = JsonConvert.DeserializeObject<List<T>>(json) ?? null;
    }

    /// <summary>
    /// 字符串转动态类型List.
    /// </summary>
    /// <typeparam name="T">动态类型.</typeparam>
    /// <param name="json"></param>
    /// <param name="jsonSerializerOptions">序列化规则.</param>
    /// <returns></returns>
    public static List<T> ToList<T>(this string json, object jsonSerializerOptions = default)
    {
        return _ = JsonConvert.DeserializeObject<List<T>>(json) ?? null;
    }

    /// <summary>
    /// 字符串 转 JObject.
    /// </summary>
    /// <param name="json">字符串.</param>
    /// <returns></returns>
    public static JObject ToObject(this string json)
    {
        return json == null ? JObject.Parse("{}") : JObject.Parse(json.Replace("&nbsp;", string.Empty));
    }

    /// <summary>
    /// 字符串 转 JSON.
    /// </summary>
    /// <param name="json">字符串.</param>
    /// <returns></returns>
    public static string PraseToJson(string json)
    {
        JsonSerializer s = new JsonSerializer();
        JsonReader reader = new JsonTextReader(new StringReader(json));
        object jsonObject = s.Deserialize(reader);
        StringWriter sWriter = new StringWriter();
        JsonWriter writer = new JsonTextWriter(sWriter);
        writer.Formatting = Formatting.Indented;
        s.Serialize(writer, jsonObject);
        return sWriter.ToString();
    }
}