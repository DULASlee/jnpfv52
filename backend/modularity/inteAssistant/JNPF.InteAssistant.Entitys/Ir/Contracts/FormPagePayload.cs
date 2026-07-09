using System.Collections.Generic;
using System.Text.Json;

namespace JNPF.InteAssistant.Entitys.Ir.Contracts;

/// <summary>
/// IR2_FormPageIR 完整契约（阶段九 P9-S1）。
///
/// 修复缺口：
///   - 补 pageType（list/form/detail）—— 之前所有页面扁平化，无法区分
///   - 补 entityBinding（页面字段绑定到哪个实体）
///   - 补 listColumns / searchFields（列表页专用）
///
/// 双向兼容：解析时兼容旧格式（无 pageType 则按页面名启发式推断）。
/// </summary>
public sealed class FormPagePayload
{
    public List<PageDefinition> Pages { get; set; } = new();

    public static FormPagePayload Parse(string payloadJson)
    {
        var payload = new FormPagePayload();
        if (string.IsNullOrWhiteSpace(payloadJson)) return payload;

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            // 兼容 pages[] 和 root fields[]
            if (root.TryGetProperty("pages", out var pagesEl) && pagesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in pagesEl.EnumerateArray())
                {
                    payload.Pages.Add(ParsePage(p));
                }
            }
            else if (root.TryGetProperty("fields", out var fieldsEl) && fieldsEl.ValueKind == JsonValueKind.Array)
            {
                // 旧格式：root 级 fields（单页），推断为 form
                var page = new PageDefinition
                {
                    PageId = GetString(root, "pageId") ?? GetString(root, "id") ?? "page1",
                    Title = GetString(root, "pageName") ?? GetString(root, "title") ?? "表单",
                    PageType = InferPageType(GetString(root, "pageName") ?? GetString(root, "title") ?? "form"),
                };
                page.Fields = ParseFields(fieldsEl);
                payload.Pages.Add(page);
            }
        }
        catch { /* 容错 */ }

        return payload;
    }

    private static PageDefinition ParsePage(JsonElement p)
    {
        var title = GetString(p, "title") ?? GetString(p, "label") ?? GetString(p, "pageName") ?? "页面";
        var page = new PageDefinition
        {
            PageId = GetString(p, "id") ?? GetString(p, "pageId") ?? "",
            Title = title,
            Path = GetString(p, "path") ?? "",
            // 双向兼容：有 pageType 读 pageType，否则按标题推断
            PageType = GetString(p, "pageType") ?? InferPageType(title),
            EntityBinding = GetString(p, "entityBinding") ?? GetString(p, "entity"),
        };

        if (p.TryGetProperty("fields", out var fieldsEl) && fieldsEl.ValueKind == JsonValueKind.Array)
            page.Fields = ParseFields(fieldsEl);

        // 列表列（列表页专用）
        if (p.TryGetProperty("listColumns", out var lcEl) && lcEl.ValueKind == JsonValueKind.Array)
            page.ListColumns = ParseStringArray(lcEl);

        // 搜索字段（列表页专用）
        if (p.TryGetProperty("searchFields", out var sfEl) && sfEl.ValueKind == JsonValueKind.Array)
            page.SearchFields = ParseStringArray(sfEl);

        return page;
    }

    private static List<PageFieldDefinition> ParseFields(JsonElement fieldsEl)
    {
        var fields = new List<PageFieldDefinition>();
        foreach (var f in fieldsEl.EnumerateArray())
        {
            fields.Add(new PageFieldDefinition
            {
                FieldId = GetString(f, "id") ?? GetString(f, "fieldId") ?? "",
                Label = GetString(f, "label") ?? "",
                ComponentType = GetString(f, "componentType") ?? GetString(f, "component") ?? "Input",
                Required = ReadBool(f, "required") ?? false,
                // 字段绑定到实体列（编译器用于映射 Entity 属性）
                EntityField = GetString(f, "entityField") ?? GetString(f, "field") ?? "",
            });
        }
        return fields;
    }

    /// <summary>按页面标题启发式推断 pageType（确定性，零 LLM）</summary>
    internal static string InferPageType(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "form";
        var t = title.ToLowerInvariant();
        if (t.Contains("列表") || t.Contains("list") || t.Contains("查询") || t.Contains("query") || t.Contains("管理"))
            return "list";
        if (t.Contains("详情") || t.Contains("detail") || t.Contains("查看"))
            return "detail";
        return "form"; // 默认表单
    }

    public void Validate()
    {
        if (Pages.Count == 0)
            throw new System.InvalidOperationException("FormPageIR 无 pages");
        foreach (var p in Pages)
        {
            if (p.Fields.Count == 0)
                throw new System.InvalidOperationException($"页面 {p.Title} 无 fields");
        }
    }

    internal static string? GetString(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop)) return null;
        return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
    }

    internal static bool? ReadBool(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop)) return null;
        return prop.ValueKind == JsonValueKind.True;
    }

    internal static List<string> ParseStringArray(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.String) return new() { el.GetString() ?? "" };
        if (el.ValueKind != JsonValueKind.Array) return new();
        return el.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
    }
}

public sealed class PageDefinition
{
    public string PageId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Path { get; set; } = "";
    /// <summary>页面类型：list / form / detail</summary>
    public string PageType { get; set; } = "form";
    /// <summary>绑定实体名（对应 EntityDraftContract.EntityName）</summary>
    public string? EntityBinding { get; set; }
    public List<PageFieldDefinition> Fields { get; set; } = new();
    /// <summary>列表页：显示的列</summary>
    public List<string> ListColumns { get; set; } = new();
    /// <summary>列表页：搜索/筛选字段</summary>
    public List<string> SearchFields { get; set; } = new();
}

public sealed class PageFieldDefinition
{
    public string FieldId { get; set; } = "";
    public string Label { get; set; } = "";
    public string ComponentType { get; set; } = "Input";
    public bool Required { get; set; }
    /// <summary>绑定到的实体列名</summary>
    public string EntityField { get; set; } = "";
}
