using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Ir;

namespace JNPF.InteAssistant.Ir;

public interface IIrSchemaValidator
{
    void Validate(string eventType, string payload);
}

/// <summary>
/// IR Schema v1 MVP 校验（阶段一 P1-B08）
/// </summary>
public sealed class IrSchemaValidator : IIrSchemaValidator, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public void Validate(string eventType, string payload)
    {
        if (eventType != IrEventTypes.SkeletonCreated)
            return;

        if (string.IsNullOrWhiteSpace(payload))
            throw Oops.Bah("SkeletonCreated payload 不能为空");

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(payload);
        }
        catch (JsonException)
        {
            throw Oops.Bah("SkeletonCreated payload 必须是合法 JSON");
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (!root.TryGetProperty("businessEvents", out var eventsEl)
                || eventsEl.ValueKind != JsonValueKind.Array
                || eventsEl.GetArrayLength() == 0)
            {
                throw Oops.Bah("SkeletonCreated 缺少非空 businessEvents 数组");
            }

            foreach (var evt in eventsEl.EnumerateArray())
            {
                if (!evt.TryGetProperty("eventId", out var idEl) || idEl.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(idEl.GetString()))
                {
                    throw Oops.Bah("businessEvents 每项必须包含 eventId");
                }

                if (!evt.TryGetProperty("eventName", out var nameEl) || nameEl.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(nameEl.GetString()))
                {
                    throw Oops.Bah("businessEvents 每项必须包含 eventName");
                }
            }
        }
    }
}
