using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;

namespace JNPF.InteAssistant.Ir;

public interface IIoiValidatorService
{
    void Validate(string eventSpecPayload);
}

/// <summary>
/// IOI 不变量校验 MVP（P2-B09）
/// </summary>
public sealed class IoiValidatorService : IIoiValidatorService, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public void Validate(string eventSpecPayload)
    {
        if (string.IsNullOrWhiteSpace(eventSpecPayload))
            throw Oops.Bah("EventSpec payload 不能为空");

        using var doc = JsonDocument.Parse(eventSpecPayload);
        var root = doc.RootElement;

        if (root.TryGetProperty("ioiInvariants", out var invariants)
            && invariants.ValueKind == JsonValueKind.Array)
        {
            foreach (var inv in invariants.EnumerateArray())
            {
                if (inv.TryGetProperty("expression", out var expr)
                    && expr.GetString()?.Contains("INVALID", StringComparison.OrdinalIgnoreCase) == true)
                {
                    throw Oops.Bah("IOI 不变量校验失败: 检测到 INVALID 表达式");
                }
            }
        }

        if (root.TryGetProperty("confirmedFields", out var fields)
            && fields.ValueKind == JsonValueKind.Array
            && fields.GetArrayLength() == 0)
        {
            throw Oops.Bah("EventSpec 至少包含 1 个 confirmedFields");
        }
    }
}
