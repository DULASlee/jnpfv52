using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using JNPF.InteAssistant.Entitys.Dto.Skills;
using JNPF.InteAssistant.Sa;

namespace JNPF.InteAssistant.Skills;

public sealed record RequirementSlot(
    string SlotId,
    string Industry,
    string Title,
    string Description,
    double Gain,
    IReadOnlyList<string> Keywords);

public static class RequirementSlotCatalog
{
    public static IReadOnlyList<RequirementSlot> Slots { get; } = new[]
    {
        new RequirementSlot("leave.types", "leave-approval", "请假类型", "确认年假、病假、事假、调休等类型是否都覆盖。", 0.92, new[] { "请假", "休假", "假期", "年假", "病假", "事假", "调休" }),
        new RequirementSlot("leave.approval-levels", "leave-approval", "审批层级", "确认按部门、天数、角色配置一到多级审批。", 0.9, new[] { "请假", "审批", "流程", "层级" }),
        new RequirementSlot("leave.delegate-submit", "leave-approval", "代提规则", "确认主管或人事是否允许代员工提交申请。", 0.72, new[] { "请假", "代提", "人事" }),
        new RequirementSlot("approval.recall", "approval", "撤回与驳回", "确认发起人撤回、审批驳回后的状态与再次提交规则。", 0.78, new[] { "审批", "驳回", "撤回" }),
        new RequirementSlot("approval.notify", "approval", "通知方式", "确认提交、通过、驳回时是否需要站内信、短信或企微通知。", 0.62, new[] { "审批", "通知", "消息", "短信" }),
    };
}

public static class SlotInformationGainSelector
{
    public static IReadOnlyList<RequirementSlot> SelectTopSlots(
        SaNineViewCompileResult compileResult,
        string? previousAnswersText,
        int take = 3,
        IReadOnlyList<string>? filledSlotIds = null)
    {
        var filled = ClarificationAnswerPatchMapper.DetectFilledSlots(previousAnswersText, filledSlotIds);
        var corpus = BuildCorpus(compileResult);

        return RequirementSlotCatalog.Slots
            .Where(slot => slot.Keywords.Any(k => corpus.Contains(k, StringComparison.OrdinalIgnoreCase)))
            .Where(slot => !filled.Contains(slot.SlotId))
            .OrderByDescending(slot => slot.Gain)
            .ThenBy(slot => slot.SlotId, StringComparer.Ordinal)
            .Take(Math.Max(0, take))
            .ToList();
    }

    private static string BuildCorpus(SaNineViewCompileResult compileResult)
    {
        var sb = new StringBuilder();
        sb.AppendLine(compileResult.Source.SystemName);
        sb.AppendLine(compileResult.Source.RequirementSummary);
        foreach (var e in compileResult.Source.BusinessEvents)
            sb.AppendLine($"{e.EventId} {e.EventName} {e.Description}");
        foreach (var entity in compileResult.Source.EntityDrafts)
            sb.AppendLine($"{entity.EntityName} {entity.DisplayName} {entity.Description}");
        return sb.ToString();
    }
}

/// <summary>
/// 三轮澄清答案 → Typed IR 补丁（确定性）。解决「答完题不写回骨架」导致 PM 分长期偏低。
/// </summary>
public static class ClarificationAnswerPatchMapper
{
    public static IReadOnlyList<string> DetectFilledSlots(
        string? answersText,
        IEnumerable<string>? explicitSlotIds = null)
    {
        var filled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (explicitSlotIds != null)
        {
            foreach (var id in explicitSlotIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    filled.Add(id.Trim());
            }
        }

        var text = answersText ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return filled.ToList();

        foreach (var slot in RequirementSlotCatalog.Slots)
        {
            if (filled.Contains(slot.SlotId))
                continue;
            if (text.Contains(slot.SlotId, StringComparison.OrdinalIgnoreCase)
                || text.Contains(slot.Title, StringComparison.OrdinalIgnoreCase))
            {
                filled.Add(slot.SlotId);
                continue;
            }

            // 用户答案常只含选项文案（如「年假、病假」），用 ≥2 个行业关键词判定已覆盖
            var hit = slot.Keywords.Count(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
            if (hit >= 2)
                filled.Add(slot.SlotId);
        }

        return filled.OrderBy(x => x, StringComparer.Ordinal).ToList();
    }

    public static IReadOnlyList<AmendmentPatch> BuildPatches(
        string? answersText,
        IReadOnlyList<string> filledSlotIds,
        string? existingSummary = null)
    {
        var patches = new List<AmendmentPatch>();
        foreach (var slotId in filledSlotIds.Distinct(StringComparer.OrdinalIgnoreCase))
            patches.AddRange(PatchesForSlot(slotId, answersText));

        if (!string.IsNullOrWhiteSpace(answersText))
        {
            var clarificationBlock = answersText.Trim();
            if (clarificationBlock.Length > 2000)
                clarificationBlock = clarificationBlock[..2000];
            var merged = string.IsNullOrWhiteSpace(existingSummary)
                ? clarificationBlock
                : existingSummary.Trim() + "\n\n【澄清确认】\n" + clarificationBlock;
            if (merged.Length > 4000)
                merged = merged[..4000];
            patches.Insert(0, new AmendmentPatch(
                AmendmentPatchOperation.PatchSummary,
                "summary",
                "summary",
                Description: merged));
        }

        return patches;
    }

    private static IEnumerable<AmendmentPatch> PatchesForSlot(string slotId, string? answersText)
    {
        var detail = string.IsNullOrWhiteSpace(answersText) ? slotId : answersText.Trim();
        if (detail.Length > 400)
            detail = detail[..400];

        switch (slotId)
        {
            case "leave.types":
                yield return new AmendmentPatch(
                    AmendmentPatchOperation.AddField, "LeaveRequest", "leaveType",
                    Type: "string", Required: true, Description: detail);
                yield return new AmendmentPatch(
                    AmendmentPatchOperation.PatchRule, "RULE-LEAVE-TYPES", "请假类型覆盖",
                    Description: "请假类型以用户澄清为准：" + detail, ScopeEventId: "EV-001");
                break;
            case "leave.approval-levels":
                yield return new AmendmentPatch(
                    AmendmentPatchOperation.AddStateTransition, "LeaveRequest", "LeaveRequest",
                    From: "Draft", To: "PendingApproval", ScopeEventId: "EV-001", Description: detail);
                yield return new AmendmentPatch(
                    AmendmentPatchOperation.AddStateTransition, "LeaveRequest", "LeaveRequest",
                    From: "PendingApproval", To: "Approved", ScopeEventId: "EV-002", Description: detail);
                yield return new AmendmentPatch(
                    AmendmentPatchOperation.PatchRule, "RULE-APPROVAL-LEVELS", "审批层级",
                    Description: "审批层级以用户澄清为准：" + detail, ScopeEventId: "EV-001");
                break;
            case "leave.delegate-submit":
                yield return new AmendmentPatch(
                    AmendmentPatchOperation.AddField, "LeaveRequest", "submittedBy",
                    Type: "string", Required: false, Description: detail);
                yield return new AmendmentPatch(
                    AmendmentPatchOperation.PatchRule, "RULE-DELEGATE-SUBMIT", "代提规则",
                    Description: "代提规则以用户澄清为准：" + detail, ScopeEventId: "EV-001");
                break;
            case "approval.recall":
                yield return new AmendmentPatch(
                    AmendmentPatchOperation.AddStateTransition, "LeaveRequest", "LeaveRequest",
                    From: "PendingApproval", To: "Withdrawn", ScopeEventId: "EV-003", Description: detail);
                yield return new AmendmentPatch(
                    AmendmentPatchOperation.AddStateTransition, "LeaveRequest", "LeaveRequest",
                    From: "PendingApproval", To: "Rejected", ScopeEventId: "EV-004", Description: detail);
                yield return new AmendmentPatch(
                    AmendmentPatchOperation.PatchRule, "RULE-RECALL-REJECT", "撤回与驳回",
                    Description: "撤回/驳回规则以用户澄清为准：" + detail);
                break;
            case "approval.notify":
                yield return new AmendmentPatch(
                    AmendmentPatchOperation.PatchRule, "RULE-NOTIFY", "审批通知",
                    Description: "通知方式以用户澄清为准：" + detail);
                break;
            default:
                yield return new AmendmentPatch(
                    AmendmentPatchOperation.PatchRule,
                    "RULE-" + slotId.Replace('.', '-').ToUpperInvariant(),
                    slotId,
                    Description: detail);
                break;
        }
    }
}

public sealed record RequirementGraphGap(string Source, string Code, string Message);

public static class RequirementConflictGraph
{
    public static IReadOnlyList<RequirementGraphGap> FindGaps(SaNineViewCompileResult compileResult)
    {
        var gaps = new List<RequirementGraphGap>();
        var entityNames = compileResult.Source.EntityDrafts
            .Select(e => e.EntityName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var eventIds = compileResult.Source.BusinessEvents
            .Select(e => e.EventId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var entity in compileResult.Source.EntityDrafts)
        {
            if (entity.Fields.Count == 0)
                gaps.Add(new RequirementGraphGap("graph", "entity.no-fields", $"实体 {entity.EntityName} 没有字段定义"));

            foreach (var relation in entity.Relations)
            {
                if (!entityNames.Contains(relation.ToEntity))
                    gaps.Add(new RequirementGraphGap("graph", "relation.missing-entity", $"实体 {entity.EntityName} 关系指向不存在实体 {relation.ToEntity}"));
            }
        }

        foreach (var rule in compileResult.Source.BusinessRules)
        {
            if (!string.IsNullOrWhiteSpace(rule.ScopeEventId) && !eventIds.Contains(rule.ScopeEventId))
                gaps.Add(new RequirementGraphGap("graph", "rule.missing-event", $"规则 {rule.RuleId} 绑定了不存在的事件 {rule.ScopeEventId}"));
        }

        foreach (var transition in compileResult.Source.StateTransitions)
        {
            if (!entityNames.Contains(transition.Entity))
                gaps.Add(new RequirementGraphGap("graph", "transition.missing-entity", $"状态流转引用了不存在实体 {transition.Entity}"));
            if (!string.IsNullOrWhiteSpace(transition.TriggerEventId) && !eventIds.Contains(transition.TriggerEventId))
                gaps.Add(new RequirementGraphGap("graph", "transition.missing-event", $"状态流转 {transition.Entity} 绑定了不存在事件 {transition.TriggerEventId}"));
        }

        return gaps;
    }
}

public static class RequirementConfidencePolicy
{
    public static int ApplyPmScoreCap(int score, SaNineViewCompileResult? compileResult)
    {
        if (compileResult == null)
            return score;
        return compileResult.Assumptions.Any(a => a.Confidence < 0.5m)
            ? Math.Min(score, 84)
            : score;
    }
}

public static class AmendmentPatchApplier
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static IReadOnlyList<AmendmentPatch> ParsePatches(JsonElement root)
    {
        if (!root.TryGetProperty("patches", out var el) || el.ValueKind != JsonValueKind.Array)
            return Array.Empty<AmendmentPatch>();

        var patches = new List<AmendmentPatch>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;
            var opText = ReadJsonString(item, "operation", "op", "type");
            if (!Enum.TryParse<AmendmentPatchOperation>(opText, ignoreCase: true, out var operation))
                continue;

            patches.Add(new AmendmentPatch(
                operation,
                ReadJsonString(item, "target", "entity", "eventId") ?? "",
                ReadJsonString(item, "name", "field", "eventName", "ruleId") ?? "",
                ReadJsonString(item, "displayName", "display_name"),
                ReadJsonString(item, "dataType", "fieldType", "type"),
                ReadJsonString(item, "description", "summary"),
                item.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.True,
                ReadJsonString(item, "references", "ref"),
                ReadJsonString(item, "scopeEventId", "scope"),
                ReadJsonString(item, "from"),
                ReadJsonString(item, "to")));
        }

        return patches;
    }

    public static IReadOnlyList<AmendmentPatch> MergePatches(
        IEnumerable<AmendmentPatch> primary,
        IEnumerable<AmendmentPatch> secondary)
    {
        var result = new List<AmendmentPatch>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var patch in primary.Concat(secondary))
        {
            var key = $"{patch.Operation}|{patch.Target}|{patch.Name}|{patch.From}|{patch.To}";
            if (!seen.Add(key))
                continue;
            result.Add(patch);
        }

        return result;
    }

    public static string ApplyToSkeletonJson(string skeletonJson, IEnumerable<AmendmentPatch> patches)
    {
        var root = JsonNode.Parse(skeletonJson)?.AsObject()
                   ?? throw new JsonException("Skeleton JSON 根节点必须是对象");

        foreach (var patch in patches)
            ApplyPatch(root, patch);

        return root.ToJsonString(JsonOptions);
    }

    private static void ApplyPatch(JsonObject root, AmendmentPatch patch)
    {
        switch (patch.Operation)
        {
            case AmendmentPatchOperation.PatchSummary:
                if (!string.IsNullOrWhiteSpace(patch.Description))
                    root["requirementSummary"] = patch.Description;
                break;
            case AmendmentPatchOperation.AddEntity:
                AddEntity(root, patch);
                break;
            case AmendmentPatchOperation.AddField:
                AddField(root, patch);
                break;
            case AmendmentPatchOperation.AddEvent:
                AddEvent(root, patch);
                break;
            case AmendmentPatchOperation.PatchRule:
                AddRule(root, patch);
                break;
            case AmendmentPatchOperation.AddStateTransition:
                AddStateTransition(root, patch);
                break;
        }
    }

    private static void AddEntity(JsonObject root, AmendmentPatch patch)
    {
        var entities = EnsureArray(root, "entityDrafts");
        var name = FirstNonEmpty(patch.Name, patch.Target);
        if (entities.OfType<JsonObject>().Any(e => Same(e["entityName"], name)))
            return;

        entities.Add(new JsonObject
        {
            ["entityName"] = name,
            ["displayName"] = FirstNonEmpty(patch.DisplayName, name),
            ["tableName"] = ToSnakeUpper(name),
            ["description"] = patch.Description ?? "",
            ["fields"] = new JsonArray
            {
                new JsonObject { ["name"] = "id", ["type"] = "string", ["required"] = true, ["primaryKey"] = true },
            },
        });
    }

    private static void AddField(JsonObject root, AmendmentPatch patch)
    {
        var entities = EnsureArray(root, "entityDrafts");
        var entity = entities.OfType<JsonObject>().FirstOrDefault(e => Same(e["entityName"], patch.Target));
        if (entity == null)
        {
            AddEntity(root, new AmendmentPatch(AmendmentPatchOperation.AddEntity, patch.Target, patch.Target));
            entity = entities.OfType<JsonObject>().First(e => Same(e["entityName"], patch.Target));
        }

        var fields = EnsureArray(entity, "fields");
        if (fields.OfType<JsonObject>().Any(f => Same(f["name"], patch.Name)))
            return;

        var field = new JsonObject
        {
            ["name"] = patch.Name,
            ["type"] = patch.Type ?? "string",
            ["required"] = patch.Required,
            ["primaryKey"] = false,
        };
        if (!string.IsNullOrWhiteSpace(patch.References))
            field["references"] = patch.References;
        fields.Add(field);
    }

    private static void AddEvent(JsonObject root, AmendmentPatch patch)
    {
        var events = EnsureArray(root, "businessEvents");
        var id = FirstNonEmpty(patch.Target, patch.Name);
        if (events.OfType<JsonObject>().Any(e => Same(e["eventId"], id) || Same(e["eventName"], patch.Name)))
            return;

        events.Add(new JsonObject
        {
            ["eventId"] = id,
            ["eventName"] = patch.Name,
            ["complexityHint"] = patch.Type ?? "simple",
            ["description"] = patch.Description ?? "",
        });
    }

    private static void AddRule(JsonObject root, AmendmentPatch patch)
    {
        var rules = EnsureArray(root, "businessRules");
        var id = FirstNonEmpty(patch.Target, patch.Name);
        if (rules.OfType<JsonObject>().Any(r => Same(r["ruleId"], id)))
            return;

        rules.Add(new JsonObject
        {
            ["ruleId"] = id,
            ["scopeEventId"] = patch.ScopeEventId,
            ["description"] = FirstNonEmpty(patch.Description, patch.Name),
        });
    }

    private static void AddStateTransition(JsonObject root, AmendmentPatch patch)
    {
        var transitions = EnsureArray(root, "stateTransitions");
        if (transitions.OfType<JsonObject>().Any(t =>
                Same(t["entity"], patch.Target) && Same(t["from"], patch.From) && Same(t["to"], patch.To)))
            return;

        transitions.Add(new JsonObject
        {
            ["entity"] = patch.Target,
            ["from"] = patch.From ?? "",
            ["to"] = patch.To ?? "",
            ["triggerEventId"] = patch.ScopeEventId,
        });
    }

    private static JsonArray EnsureArray(JsonObject obj, string name)
    {
        if (obj[name] is JsonArray existing)
            return existing;
        var array = new JsonArray();
        obj[name] = array;
        return array;
    }

    private static bool Same(JsonNode? node, string? value)
        => string.Equals(node?.GetValue<string>(), value, StringComparison.OrdinalIgnoreCase);

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? "";

    private static string ToSnakeUpper(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "AI_ENTITY";
        var chars = new List<char>();
        foreach (var ch in value.Trim())
        {
            if (char.IsUpper(ch) && chars.Count > 0)
                chars.Add('_');
            chars.Add(char.IsLetterOrDigit(ch) ? char.ToUpperInvariant(ch) : '_');
        }
        return string.Join("", chars).Replace("__", "_");
    }

    private static string? ReadJsonString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String)
            {
                var value = el.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }

        return null;
    }
}
