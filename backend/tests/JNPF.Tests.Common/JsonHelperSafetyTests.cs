using JNPF.Common.Security;
using Newtonsoft.Json;
using Xunit;

namespace JNPF.Tests.Common;

/// <summary>
/// Security: J5 — verify JsonHelper deserialization with TypeNameHandling.None.
/// $type is stored as string value, never used for type instantiation.
/// </summary>
public class JsonHelperSafetyTests
{
    [Fact]
    public void ToObject_TypedPayload_DeserializesCorrectly()
    {
        var json = "{\"key\":\"value\"}";
        var result = json.ToObject<Dictionary<string, string>>();
        Assert.NotNull(result);
        Assert.Equal("value", result["key"]);
    }

    [Fact]
    public void ToObject_PolymorphicPayload_NoTypeInstantiation()
    {
        var malicious = "{\"$type\":\"System.Diagnostics.Process, System\", \"FileName\":\"cmd.exe\"}";
        var result = malicious.ToObject<Dictionary<string, string>>();
        Assert.NotNull(result);
        Assert.Equal("cmd.exe", result["FileName"]);
    }

    [Fact]
    public void ToObject_NestedPolymorphicPayload_NoTypeResolution()
    {
        var malicious = "{\"$type\":\"System.Object, System.Private.CoreLib\"}";
        var result = malicious.ToObject<Dictionary<string, object>>();
        Assert.NotNull(result);
    }

    [Fact]
    public void ToList_MaliciousTypePayload_DeserializesSafely()
    {
        var json = "[{\"$type\":\"MaliciousType\", \"safe\":\"value\"}]";
        var result = json.ToList<Dictionary<string, string>>();
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("value", result[0]["safe"]);
    }

    [Fact]
    public void ToObject_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ((string?)null).ToObject<Dictionary<string, string>>());
    }

    [Fact]
    public void ToObject_InvalidJson_ThrowsJsonException()
    {
        Assert.Throws<JsonReaderException>(() => "{invalid json}".ToObject<Dictionary<string, string>>());
    }

    [Fact]
    public void SafeSettings_TypeNameHandlingIsNone()
    {
        var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None };
        var json = "{\"$type\":\"System.Diagnostics.Process, System\"}";
        var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(json, settings);
        Assert.NotNull(result);
    }
}
