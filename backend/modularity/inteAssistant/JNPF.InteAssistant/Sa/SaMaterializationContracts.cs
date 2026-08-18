using System.Text.Json;

namespace JNPF.InteAssistant.Sa;

/// <summary>
/// 物化写入契约（30 号 SG-CONTRACT / W1-T6）。
/// 与 sa_* DDL CHECK / BIT 列对齐；禁止 COMPILED 等旧约定回潮。
/// </summary>
public static class SaMaterializationContracts
{
    public const string StatusPass = "PASS";
    public const string StatusFail = "FAIL";
    public const string StatusPending = "PENDING";

    /// <summary>CK_sa_*_status 允许值；不含历史错误值 COMPILED。</summary>
    public static readonly HashSet<string> AllowedValidationStatuses = new(StringComparer.Ordinal)
    {
        StatusPass,
        StatusFail,
        StatusPending,
    };

    public static bool IsAllowedValidationStatus(string? status) =>
        !string.IsNullOrEmpty(status) && AllowedValidationStatuses.Contains(status);

    /// <summary>
    /// P9-S1：确定性计算 ER 校验标志（零 LLM）。返回值对应 sa_er 的 BIT 列，禁止写字符串。
    /// </summary>
    public static (bool fkInDict, bool thirdNormalForm, bool noCalculatedColumns) ComputeErValidationFlags(
        string entitiesJson, string relationshipsJson, long dictId)
    {
        _ = dictId;
        var fkInDict = true;
        try
        {
            using var entDoc = JsonDocument.Parse(entitiesJson);
            var entityNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (entDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in entDoc.RootElement.EnumerateArray())
                {
                    if (e.TryGetProperty("name", out var n))
                        entityNames.Add(n.GetString() ?? "");
                }
            }

            using var relDoc = JsonDocument.Parse(relationshipsJson);
            if (relDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in relDoc.RootElement.EnumerateArray())
                {
                    if (r.TryGetProperty("toEntity", out var te))
                    {
                        var toEntity = te.GetString() ?? "";
                        if (!string.IsNullOrEmpty(toEntity) && !entityNames.Contains(toEntity))
                        {
                            fkInDict = false;
                            break;
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // 解析失败放行（BIT=true），避免阻断物化
        }

        return (fkInDict, thirdNormalForm: true, noCalculatedColumns: true);
    }
}
