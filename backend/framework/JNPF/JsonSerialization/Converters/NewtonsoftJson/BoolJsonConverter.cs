using Newtonsoft.Json;

namespace JNPF.JsonSerialization;

/// <summary>
/// Bool 类型序列化
/// </summary>
[SuppressSniffer]
public class BoolJsonConverter : JsonConverter
{
    /// <summary>
    /// 序列化.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="serializer"></param>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        writer.WriteValue(Convert.ToInt32(value));
    }

    /// <summary>
    /// 反序列化.
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="objectType"></param>
    /// <param name="existingValue"></param>
    /// <param name="serializer"></param>
    /// <returns></returns>
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.Value == null) return null;
        switch (reader.Value.ToString().ToLower().Trim())
        {
            case "true":
            case "yes":
            case "y":
            case "1":
                return true;
            case "false":
            case "no":
            case "n":
            case "0":
                return false;
        }

        return new JsonSerializer().Deserialize(reader, objectType);
    }

    /// <summary>
    /// 判断是否为Bool类型.
    /// </summary>
    /// <param name="objectType">类型.</param>
    /// <returns>为bool类型则可以进行转换.</returns>
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(bool);
    }
}