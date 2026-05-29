using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Globalization;

namespace JNPF.JsonSerialization;

/// <summary>
/// DateTime 类型序列化
/// </summary>
[SuppressSniffer]
public class NewtonsoftDateTimeJsonConverter : DateTimeConverterBase
{
    internal static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// 读.
    /// </summary>
    /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
    /// <param name="objectType">Type of the object.</param>
    /// <param name="existingValue">The existing property value of the JSON that is being converted.</param>
    /// <param name="serializer">The calling serializer.</param>
    /// <returns>The object value.</returns>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var nullable = IsNullable(objectType);

        DateTimeFormatInfo dtFormat = new DateTimeFormatInfo();
        dtFormat.ShortDatePattern = "yyyy-MM-dd HH:mm:ss";

        if (reader.TokenType == JsonToken.Null || string.IsNullOrEmpty(reader.Value.ToString()))
        {
            if (!nullable)
                throw new JsonSerializationException(string.Format(CultureInfo.InvariantCulture, "Cannot convert null value to {0}.", objectType));

            return null;
        }

        long milliseconds = 0;

        if (reader.TokenType == JsonToken.Integer)
            milliseconds = (long)reader.Value!;
        else if (reader.TokenType == JsonToken.String)
            if (!long.TryParse((string)reader.Value!, out milliseconds))
                //throw new JsonSerializationException(string.Format(CultureInfo.InvariantCulture, "Cannot convert invalid value to {0}.", objectType));
                return Convert.ToDateTime(reader.Value.ToString(), dtFormat);
            else if (reader.TokenType == JsonToken.Date)
                return reader.Value;
            else
                throw new JsonSerializationException(string.Format(CultureInfo.InvariantCulture, "Unexpected token parsing date. Expected Integer or String, got {0}.", reader.TokenType));

        if (milliseconds >= 0)
        {
            var d = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).ToLocalTime().DateTime;

            var t = nullable ? Nullable.GetUnderlyingType(objectType) : objectType;
            if (t == typeof(DateTimeOffset))
                return new DateTimeOffset(d, TimeSpan.Zero);

            return d;
        }
        else
            //throw new JsonSerializationException(string.Format(CultureInfo.InvariantCulture, "Cannot convert value that is before Unix epoch of 00:00:00 UTC on 1 January 1970 to {0}.", objectType));
            return DateTime.Now;
    }

    /// <summary>
    /// 写入.
    /// </summary>
    /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
    /// <param name="value">The value.</param>
    /// <param name="serializer">The calling serializer.</param>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        long timestamp;
        if (value is DateTime)
        {
            timestamp = (long)((DateTime)value).ToUniversalTime().Subtract(UnixEpoch).TotalMilliseconds;
        }
        else
        {
            if (!(value is DateTimeOffset))
            {
                throw new JsonSerializationException("Expected date object value.");
            }

            timestamp = (long)((DateTimeOffset)value).ToUniversalTime().Subtract(UnixEpoch).TotalMilliseconds;
        }

        writer.WriteValue(timestamp);
    }

    /// <summary>
    /// 类型是否为空.
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    private static bool IsNullable(Type t)
    {
        if (t.IsValueType)
        {
            return IsNullableType(t);
        }

        return true;
    }

    private static bool IsNullableType(Type t)
    {
        if (t.IsGenericType)
        {
            return t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
        return false;
    }
}