using System.Text;
using System.Text.Json;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// 设计阶段 Skill 产出 → deliverables/03~06 可读文件（SUP-05）。
/// </summary>
public static class DesignDeliverableFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public static string BuildArchitectureMarkdown(object? payload)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# 架构设计说明书（03-architecture）");
        sb.AppendLine();
        sb.AppendLine($"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        if (TryGetJsonRoot(payload, out var root))
        {
            if (root.TryGetProperty("pattern", out var pattern))
            {
                sb.AppendLine($"## 架构模式");
                sb.AppendLine();
                sb.AppendLine($"`{pattern}`");
                sb.AppendLine();
            }

            if (root.TryGetProperty("modules", out var modules) && modules.ValueKind == JsonValueKind.Array)
            {
                sb.AppendLine("## 模块划分");
                sb.AppendLine();
                sb.AppendLine("| 模块 | 职责 |");
                sb.AppendLine("|------|------|");
                foreach (var m in modules.EnumerateArray())
                {
                    if (m.ValueKind == JsonValueKind.String)
                    {
                        sb.AppendLine($"| {m.GetString()} | — |");
                    }
                    else if (m.ValueKind == JsonValueKind.Object)
                    {
                        sb.AppendLine($"| {GetStr(m, "name")} | {GetStr(m, "responsibility")} |");
                    }
                }
                sb.AppendLine();
            }

            if (root.TryGetProperty("candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array)
            {
                sb.AppendLine("## 架构候选（ToT）");
                sb.AppendLine();
                var idx = 0;
                foreach (var c in candidates.EnumerateArray())
                {
                    idx++;
                    if (c.ValueKind == JsonValueKind.Object)
                    {
                        sb.AppendLine($"### 候选 {idx} — score {GetStr(c, "score")}");
                        sb.AppendLine();
                        sb.AppendLine("```json");
                        sb.AppendLine(Truncate(c.GetRawText(), 4000));
                        sb.AppendLine("```");
                        sb.AppendLine();
                    }
                    else
                    {
                        sb.AppendLine($"### 候选 {idx}");
                        sb.AppendLine();
                        sb.AppendLine("```json");
                        sb.AppendLine(Truncate(c.GetRawText(), 4000));
                        sb.AppendLine("```");
                        sb.AppendLine();
                    }
                }
            }
        }

        sb.AppendLine("## 原始 JSON");
        sb.AppendLine();
        sb.AppendLine("```json");
        sb.AppendLine(PayloadToPrettyJson(payload));
        sb.AppendLine("```");
        return sb.ToString();
    }

    public static string BuildSystemDesignMarkdown(object? payload)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# 系统总体设计说明书（04-system-design）");
        sb.AppendLine();
        sb.AppendLine($"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("> SystemDesignLocked — IR-2 四片段一致性锁定");
        sb.AppendLine();

        if (TryGetJsonRoot(payload, out var root)
            && root.TryGetProperty("references", out var refs)
            && refs.ValueKind == JsonValueKind.Object)
        {
            sb.AppendLine("## 引用片段");
            sb.AppendLine();
            sb.AppendLine("| 类型 | FragmentId |");
            sb.AppendLine("|------|------------|");
            sb.AppendLine($"| Architecture | {GetStr(refs, "architectureFragmentId")} |");
            sb.AppendLine($"| DDL | {GetStr(refs, "ddlFragmentId")} |");
            sb.AppendLine($"| FormPageIR | {GetStr(refs, "formPageFragmentId")} |");
            sb.AppendLine();
        }

        sb.AppendLine("## 锁定详情");
        sb.AppendLine();
        sb.AppendLine("```json");
        sb.AppendLine(PayloadToPrettyJson(payload));
        sb.AppendLine("```");
        return sb.ToString();
    }

    public static string ExtractDdlSql(object? payload)
    {
        if (!TryGetJsonRoot(payload, out var root))
            return PayloadToPrettyJson(payload);

        if (root.TryGetProperty("ddl", out var ddl) && ddl.ValueKind == JsonValueKind.String)
            return ddl.GetString() ?? "";

        return PayloadToPrettyJson(payload);
    }

    public static string ExtractFormPageIrJson(object? payload)
    {
        if (!TryGetJsonRoot(payload, out var root))
            return PayloadToPrettyJson(payload);

        if (root.TryGetProperty("pages", out _))
            return JsonSerializer.Serialize(root, JsonOptions);

        return PayloadToPrettyJson(payload);
    }

    public static string? FindEventPayload(
        IReadOnlyList<AppendIrEventRequest> events,
        string eventType)
    {
        var evt = events.LastOrDefault(e => e.EventType == eventType);
        if (evt?.Payload == null) return null;
        return evt.Payload is string s ? s : JsonSerializer.Serialize(evt.Payload, JsonOptions);
    }

    public static object? FindSnapshotPayload(
        IReadOnlyList<IrFragmentSnapshotDto> snapshots,
        string fragmentType)
    {
        return snapshots
            .Where(s => string.Equals(s.FragmentType, fragmentType, StringComparison.Ordinal))
            .OrderByDescending(s => s.CurrentVersion)
            .FirstOrDefault()?.Payload;
    }

    private static bool TryGetJsonRoot(object? payload, out JsonElement root)
    {
        root = default;
        if (payload == null) return false;
        try
        {
            var json = payload switch
            {
                string s => s,
                JsonElement el => el.GetRawText(),
                _ => JsonSerializer.Serialize(payload, JsonOptions),
            };
            using var doc = JsonDocument.Parse(json);
            root = doc.RootElement.Clone();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string PayloadToPrettyJson(object? payload)
    {
        if (payload == null) return "{}";
        if (payload is string s)
        {
            try
            {
                using var doc = JsonDocument.Parse(s);
                return JsonSerializer.Serialize(doc.RootElement, JsonOptions);
            }
            catch
            {
                return s;
            }
        }

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static string GetStr(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) ? v.ToString() : "";

    private static string Truncate(string text, int max)
        => text.Length <= max ? text : text[..max] + "\n…（已截断）";
}
