using System.Text;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Sa;

namespace JNPF.InteAssistant.Studio;

public interface IRequirementSpecDocumentService
{
    string BuildSkeletonMarkdown(string skeletonJson);
    string BuildRequirementSpecMarkdown(
        long pipelineId,
        string userRequirement,
        IReadOnlyList<IrFragmentSnapshotDto> eventSpecs,
        string? pipelineTitle = null);
}

/// <summary>
/// 需求分析业务文档生成（01-skeleton.md / 02-requirement-spec.md《需求分析说明书》）。
/// </summary>
public sealed class RequirementSpecDocumentService : IRequirementSpecDocumentService, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private static readonly Dictionary<string, string> StepTitles = new(StringComparer.Ordinal)
    {
        ["DomainModel"] = "系统边界",
        ["AggregateDesign"] = "数据流图 (DFD)",
        ["EventCatalog"] = "业务流程 (BPM)",
        ["CommandQuery"] = "数据字典",
        ["IntegrationPoints"] = "过程规格 (PSpec)",
        ["WorkflowSpec"] = "判定表 / 工作流规则",
        ["UISpec"] = "状态机 (STD)",
        ["DataModel"] = "ER 数据模型",
        ["DeliveryChecklist"] = "UI 界面规格",
    };

    public string BuildSkeletonMarkdown(string skeletonJson)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# IR-0 产品骨架（01-skeleton）");
        sb.AppendLine();
        sb.AppendLine($"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        try
        {
            using var doc = JsonDocument.Parse(skeletonJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("businessEvents", out var events) && events.ValueKind == JsonValueKind.Array)
            {
                sb.AppendLine("## 业务事件清单");
                sb.AppendLine();
                sb.AppendLine("| eventId | eventName | complexity | dependsOn |");
                sb.AppendLine("|---------|-----------|------------|-----------|");
                foreach (var e in events.EnumerateArray())
                {
                    var id = GetStr(e, "eventId");
                    var name = GetStr(e, "eventName");
                    var hint = GetStr(e, "complexityHint");
                    var deps = e.TryGetProperty("dependsOn", out var d)
                        ? (d.ValueKind == JsonValueKind.Array
                            ? string.Join(", ", d.EnumerateArray().Select(x => x.GetString()))
                            : d.GetString())
                        : "";
                    sb.AppendLine($"| {id} | {name} | {hint} | {deps} |");
                }
                sb.AppendLine();
            }

            if (root.TryGetProperty("roleMatrix", out var roles) && roles.ValueKind == JsonValueKind.Array)
            {
                sb.AppendLine("## 角色矩阵");
                sb.AppendLine();
                sb.AppendLine("```json");
                sb.AppendLine(JsonSerializer.Serialize(roles, JsonOptions));
                sb.AppendLine("```");
                sb.AppendLine();
            }

            if (root.TryGetProperty("entityDrafts", out var entities) && entities.ValueKind == JsonValueKind.Array)
            {
                sb.AppendLine("## 实体草案");
                sb.AppendLine();
                sb.AppendLine("```json");
                sb.AppendLine(JsonSerializer.Serialize(entities, JsonOptions));
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }
        catch
        {
            sb.AppendLine("> 骨架 JSON 解析失败，附原始内容：");
            sb.AppendLine();
            sb.AppendLine("```json");
            sb.AppendLine(skeletonJson);
            sb.AppendLine("```");
        }

        sb.AppendLine("---");
        sb.AppendLine("请确认业务事件切分是否合理。确认后进入 SA 九步需求分析。");
        return sb.ToString();
    }

    public string BuildRequirementSpecMarkdown(
        long pipelineId,
        string userRequirement,
        IReadOnlyList<IrFragmentSnapshotDto> eventSpecs,
        string? pipelineTitle = null)
    {
        var systemName = RequirementTitleHelper.ExtractSystemName(userRequirement, pipelineTitle);
        var documentTitle = RequirementTitleHelper.BuildDocumentTitle(systemName);
        var sb = new StringBuilder();
        sb.AppendLine($"# {documentTitle}");
        sb.AppendLine();
        sb.AppendLine($"> 流水线 ID：`{pipelineId}`");
        sb.AppendLine($"> 系统名称：{systemName}");
        sb.AppendLine($"> 生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"> 业务事件数：{eventSpecs.Count}");
        sb.AppendLine($"> 交付物文件：`02-requirement-spec.md`");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(userRequirement))
        {
            sb.AppendLine("## 0. 原始需求摘要");
            sb.AppendLine();
            var summary = userRequirement.Length > 4000
                ? userRequirement[..4000] + "\n\n…（已截断，完整文本见 00-merged-requirement.md）"
                : userRequirement;
            sb.AppendLine(summary);
            sb.AppendLine();
        }

        sb.AppendLine("## 1. 业务事件索引");
        sb.AppendLine();
        sb.AppendLine("| # | eventId | eventName | SA 步完成 |");
        sb.AppendLine("|---|---------|-----------|-----------|");
        for (var i = 0; i < eventSpecs.Count; i++)
        {
            var spec = eventSpecs[i];
            var eventId = spec.FragmentId?.Replace("eventspec:", "", StringComparison.OrdinalIgnoreCase) ?? "-";
            var eventName = TryParseEventName(spec.Payload) ?? eventId;
            var steps = spec.SaStepsCompleted?.Length ?? 0;
            sb.AppendLine($"| {i + 1} | {eventId} | {eventName} | {steps}/9 |");
        }
        sb.AppendLine();

        var section = 2;
        foreach (var spec in eventSpecs.OrderBy(s => s.FragmentId, StringComparer.Ordinal))
        {
            var eventId = spec.FragmentId?.Replace("eventspec:", "", StringComparison.OrdinalIgnoreCase) ?? "unknown";
            var eventName = TryParseEventName(spec.Payload) ?? eventId;
            sb.AppendLine($"## {section}. 业务事件：{eventName}（{eventId}）");
            section++;

            AppendConfirmedFieldsAndRules(sb, spec.Payload);
            AppendSaStepSections(sb, spec.Payload);
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine("## 确认事项");
        sb.AppendLine();
        sb.AppendLine("请确认以上需求分析（业务事件、数据字段、业务规则、流程与 UI 规格）是否准确。");
        sb.AppendLine("如有补充或修正，请在 Studio 对话中说明，或使用 EventSpec 增量修订。");
        return sb.ToString();
    }

    private static void AppendConfirmedFieldsAndRules(StringBuilder sb, object? payload)
    {
        if (!TryParsePayloadRoot(payload, out var root))
            return;

        if (root.TryGetProperty("confirmedFields", out var fields) && fields.ValueKind == JsonValueKind.Array)
        {
            sb.AppendLine();
            sb.AppendLine("### 确认字段（confirmedFields）");
            sb.AppendLine();
            sb.AppendLine("| 字段名 | 类型 | 必填 | 来源 |");
            sb.AppendLine("|--------|------|------|------|");
            foreach (var f in fields.EnumerateArray())
            {
                sb.AppendLine($"| {GetStr(f, "name")} | {GetStr(f, "type")} | {GetBool(f, "required")} | {GetStr(f, "source")} |");
            }
        }

        if (root.TryGetProperty("businessRules", out var rules) && rules.ValueKind == JsonValueKind.Array)
        {
            sb.AppendLine();
            sb.AppendLine("### 业务规则（businessRules）");
            sb.AppendLine();
            foreach (var r in rules.EnumerateArray())
            {
                sb.AppendLine($"- **{GetStr(r, "ruleId")}**：{GetStr(r, "description")}（{GetStr(r, "source")}）");
            }
        }
    }

    private static void AppendSaStepSections(StringBuilder sb, object? payload)
    {
        if (!TryParsePayloadRoot(payload, out var root))
            return;

        if (!root.TryGetProperty("previousSteps", out var steps) || steps.ValueKind != JsonValueKind.Object)
            return;

        foreach (var stepName in SaStepMapping.IrStepOrder)
        {
            if (!steps.TryGetProperty(stepName, out var stepData))
                continue;

            var title = StepTitles.GetValueOrDefault(stepName, stepName);
            sb.AppendLine();
            sb.AppendLine($"### SA · {title}（{stepName}）");
            sb.AppendLine();
            var text = TruncateJson(stepData.GetRawText(), 3000);
            sb.AppendLine("```json");
            sb.AppendLine(text);
            sb.AppendLine("```");
        }
    }

    private static bool TryParsePayloadRoot(object? payload, out JsonElement root)
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

    private static string? TryParseEventName(object? payload)
    {
        if (!TryParsePayloadRoot(payload, out var root))
            return null;
        return GetStr(root, "eventName");
    }

    private static string TruncateJson(string text, int max)
        => text.Length <= max ? text : text[..max] + "\n…（已截断）";

    private static string GetStr(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) ? v.ToString() : "";

    private static string GetBool(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.True ? "是" : "否";
}
