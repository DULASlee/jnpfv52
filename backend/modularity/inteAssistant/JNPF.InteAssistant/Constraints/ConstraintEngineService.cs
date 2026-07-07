using System.Text.Json;
using System.Text.RegularExpressions;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Skills;

namespace JNPF.InteAssistant.Constraints;

public sealed class ConstraintViolation
{
    public string RuleId { get; init; } = string.Empty;
    public string Severity { get; init; } = "warning";
    public string Message { get; init; } = string.Empty;
    public string? FragmentType { get; init; }
    public string? FragmentId { get; init; }
}

public sealed class ConstraintCheckResult
{
    public IReadOnlyList<ConstraintViolation> Violations { get; init; } = Array.Empty<ConstraintViolation>();
    public int CriticalCount => Violations.Count(v => v.Severity == "critical");
    public int WarningCount => Violations.Count(v => v.Severity == "warning");
    public bool Passed => CriticalCount == 0;
    public bool EventAppended { get; init; }
}

public interface IConstraintEngineService
{
    ConstraintCheckResult Evaluate(IrSnapshot snapshot);
    Task<ConstraintCheckResult> CheckProjectAsync(
        string projectId,
        string tenantId,
        bool persistReport,
        string? skillId,
        CancellationToken ct = default);
}

/// <summary>
/// 分层约束引擎 MVP（P3-B06）— 规则表 + 分层方向检测，仅报告不自动修复。
/// </summary>
public sealed class ConstraintEngineService : IConstraintEngineService, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly Regex CreateTableRegex = new(
        @"CREATE\s+TABLE\s+(?:\[dbo\]\.)?\[([^\]]+)\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IIrEventStoreService _eventStore;

    public ConstraintEngineService(IIrEventStoreService eventStore) => _eventStore = eventStore;

    public ConstraintCheckResult Evaluate(IrSnapshot snapshot)
    {
        var violations = new List<ConstraintViolation>();
        violations.AddRange(CheckC001_DbReferencesController(snapshot));
        violations.AddRange(CheckC002_UiFieldsWithoutIr1Source(snapshot));
        violations.AddRange(CheckC003_DdlTableNameMismatch(snapshot));
        violations.AddRange(CheckC004_ArchitectureModuleContradiction(snapshot));
        return new ConstraintCheckResult { Violations = violations };
    }

    public async Task<ConstraintCheckResult> CheckProjectAsync(
        string projectId,
        string tenantId,
        bool persistReport,
        string? skillId,
        CancellationToken ct = default)
    {
        var snapshot = await BuildSnapshotAsync(projectId, tenantId, ct);
        var result = Evaluate(snapshot);

        if (!persistReport || result.Violations.Count == 0)
            return result;

        var payload = JsonSerializer.Serialize(new
        {
            checkedAt = DateTime.UtcNow.ToString("O"),
            criticalCount = result.CriticalCount,
            warningCount = result.WarningCount,
            violations = result.Violations.Select(v => new
            {
                v.RuleId,
                v.Severity,
                v.Message,
                v.FragmentType,
                v.FragmentId,
            }),
        }, JsonOptions);

        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.ConstraintViolationReported,
            FragmentId = $"constraints:{projectId}",
            FragmentType = "IR2_ConstraintReport",
            FragmentVersion = 1,
            Payload = payload,
            SkillId = skillId ?? "constraint-engine",
        }, ct);

        return new ConstraintCheckResult
        {
            Violations = result.Violations,
            EventAppended = true,
        };
    }

    private static IEnumerable<ConstraintViolation> CheckC001_DbReferencesController(IrSnapshot snapshot)
    {
        var ddl = snapshot.Find(IrFragmentTypes.DDL, IrStabilityStates.Stable)
            ?? snapshot.Find(IrFragmentTypes.DDL);
        if (ddl == null) yield break;

        var ddlText = ExtractDdlText(ddl.Payload);
        if (string.IsNullOrWhiteSpace(ddlText)) yield break;

        if (Regex.IsMatch(ddlText, @"REFERENCES\s+\[?dbo\]?\.\[?\w*Controller\w*\]?", RegexOptions.IgnoreCase)
            || Regex.IsMatch(ddlText, @"FOREIGN\s+KEY\s*\([^)]+\)\s*REFERENCES\s+\[?\w*Controller", RegexOptions.IgnoreCase)
            || ddlText.Contains("Controller", StringComparison.OrdinalIgnoreCase)
                && ddlText.Contains("REFERENCES", StringComparison.OrdinalIgnoreCase))
        {
            yield return new ConstraintViolation
            {
                RuleId = "C-001",
                Severity = "critical",
                Message = "DDL 存在 DB 层引用 Controller 的分层违规（检测到 Controller + REFERENCES）",
                FragmentType = IrFragmentTypes.DDL,
                FragmentId = ddl.FragmentId,
            };
        }
    }

    private static IEnumerable<ConstraintViolation> CheckC002_UiFieldsWithoutIr1Source(IrSnapshot snapshot)
    {
        var ui = snapshot.Find(IrFragmentTypes.FormPageIR, IrStabilityStates.Stable)
            ?? snapshot.Find(IrFragmentTypes.FormPageIR);
        if (ui == null) yield break;

        var ir1Fields = CollectIr1FieldNames(snapshot);
        if (ir1Fields.Count == 0) yield break;

        foreach (var fieldId in ExtractUiFieldIds(ui.Payload))
        {
            if (string.IsNullOrWhiteSpace(fieldId)) continue;
            if (ir1Fields.Contains(fieldId)) continue;

            yield return new ConstraintViolation
            {
                RuleId = "C-002",
                Severity = "warning",
                Message = $"UI 字段 '{fieldId}' 无 IR-1 来源",
                FragmentType = IrFragmentTypes.FormPageIR,
                FragmentId = ui.FragmentId,
            };
        }
    }

    private static IEnumerable<ConstraintViolation> CheckC003_DdlTableNameMismatch(IrSnapshot snapshot)
    {
        var ddl = snapshot.Find(IrFragmentTypes.DDL, IrStabilityStates.Stable)
            ?? snapshot.Find(IrFragmentTypes.DDL);
        if (ddl == null) yield break;

        var expected = CollectEntityDraftTableNames(snapshot);
        if (expected.Count == 0) yield break;

        var ddlText = ExtractDdlText(ddl.Payload);
        var actual = CreateTableRegex.Matches(ddlText)
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var table in expected)
        {
            if (actual.Contains(table)) continue;
            yield return new ConstraintViolation
            {
                RuleId = "C-003",
                Severity = "critical",
                Message = $"DDL 缺少 IR-1 entityDrafts 表 '{table}'",
                FragmentType = IrFragmentTypes.DDL,
                FragmentId = ddl.FragmentId,
            };
        }
    }

    private static IEnumerable<ConstraintViolation> CheckC004_ArchitectureModuleContradiction(IrSnapshot snapshot)
    {
        var violations = new List<ConstraintViolation>();
        var arch = snapshot.Find(IrFragmentTypes.Architecture, IrStabilityStates.Stable)
            ?? snapshot.Find(IrFragmentTypes.Architecture);
        if (arch == null) return violations;

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(arch.Payload);
        }
        catch (JsonException)
        {
            violations.Add(new ConstraintViolation
            {
                RuleId = "C-004",
                Severity = "warning",
                Message = "Architecture 片段 JSON 无法解析，跳过模块一致性校验",
                FragmentType = IrFragmentTypes.Architecture,
                FragmentId = arch.FragmentId,
            });
            return violations;
        }

        using (doc)
        {
            var root = doc.RootElement;
            var pattern = root.TryGetProperty("pattern", out var pEl) ? pEl.GetString()?.ToLowerInvariant() : null;
            if (string.IsNullOrEmpty(pattern)) return violations;

            var layers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (root.TryGetProperty("modules", out var modules) && modules.ValueKind == JsonValueKind.Array)
            {
                foreach (var mod in modules.EnumerateArray())
                {
                    if (mod.ValueKind != JsonValueKind.Object)
                        continue;

                    if (mod.TryGetProperty("layer", out var layerEl))
                    {
                        var layer = layerEl.GetString();
                        if (!string.IsNullOrWhiteSpace(layer))
                            layers.Add(layer);
                    }
                }
            }

            if (pattern == "layered" && layers.Count > 0
                && !layers.Contains("presentation") && !layers.Contains("application"))
            {
                violations.Add(new ConstraintViolation
                {
                    RuleId = "C-004",
                    Severity = "warning",
                    Message = "架构模式为 layered，但 modules 缺少 presentation/application 层",
                    FragmentType = IrFragmentTypes.Architecture,
                    FragmentId = arch.FragmentId,
                });
            }

            if (pattern == "cqrs" && layers.Count > 0 && !layers.Contains("read") && !layers.Contains("write"))
            {
                violations.Add(new ConstraintViolation
                {
                    RuleId = "C-004",
                    Severity = "warning",
                    Message = "架构模式为 cqrs，但 modules 未区分 read/write 侧",
                    FragmentType = IrFragmentTypes.Architecture,
                    FragmentId = arch.FragmentId,
                });
            }
        }

        return violations;
    }

    private async Task<IrSnapshot> BuildSnapshotAsync(string projectId, string tenantId, CancellationToken ct)
    {
        var dtos = await _eventStore.ListSnapshotsAsync(projectId, tenantId, ct);
        var fragments = dtos.Select(d => new IrSnapshotFragment
        {
            FragmentId = d.FragmentId,
            FragmentType = d.FragmentType,
            StabilityState = d.StabilityState,
            Payload = d.Payload is string s ? s : JsonSerializer.Serialize(d.Payload, JsonOptions),
            SaStepsCompleted = d.SaStepsCompleted ?? Array.Empty<string>(),
        }).ToList();
        return new IrSnapshot { Fragments = fragments };
    }

    private static string ExtractDdlText(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("ddl", out var ddlEl))
                return ddlEl.GetString() ?? string.Empty;
        }
        catch
        {
            /* raw sql */
        }

        return payload.Contains("CREATE TABLE", StringComparison.OrdinalIgnoreCase) ? payload : string.Empty;
    }

    private static HashSet<string> CollectIr1FieldNames(IrSnapshot snapshot)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var skeleton = snapshot.Find(IrFragmentTypes.Skeleton) ?? snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable);
        if (skeleton != null)
            AddFieldsFromJson(skeleton.Payload, names, "entityDrafts", "fields", "name", "id");

        foreach (var spec in snapshot.Fragments.Where(f =>
            f.FragmentType == IrFragmentTypes.EventSpec || f.FragmentId.StartsWith("eventspec:", StringComparison.Ordinal)))
        {
            AddFieldsFromJson(spec.Payload, names, "confirmedFields", fieldNameProp: "name", altProp: "id");
        }

        return names;
    }

    private static HashSet<string> CollectEntityDraftTableNames(IrSnapshot snapshot)
    {
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skeleton = snapshot.Find(IrFragmentTypes.Skeleton) ?? snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable);
        if (skeleton == null) return tables;

        try
        {
            using var doc = JsonDocument.Parse(skeleton.Payload);
            if (!doc.RootElement.TryGetProperty("entityDrafts", out var drafts)
                || drafts.ValueKind != JsonValueKind.Array)
                return tables;

            foreach (var draft in drafts.EnumerateArray())
            {
                if (draft.TryGetProperty("tableName", out var tn))
                {
                    var name = tn.GetString();
                    if (!string.IsNullOrWhiteSpace(name))
                        tables.Add(name);
                }
            }
        }
        catch
        {
            /* ignore */
        }

        return tables;
    }

    private static IEnumerable<string> ExtractUiFieldIds(string payload)
    {
        var ids = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (!doc.RootElement.TryGetProperty("pages", out var pages) || pages.ValueKind != JsonValueKind.Array)
                return ids;

            foreach (var page in pages.EnumerateArray())
            {
                if (!page.TryGetProperty("fields", out var fields) || fields.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var field in fields.EnumerateArray())
                {
                    if (field.TryGetProperty("id", out var idEl))
                    {
                        var id = idEl.GetString();
                        if (!string.IsNullOrWhiteSpace(id))
                            ids.Add(id);
                    }
                }
            }
        }
        catch
        {
            /* ignore */
        }

        return ids;
    }

    private static void AddFieldsFromJson(
        string payload,
        HashSet<string> names,
        string arrayProp,
        string? nestedArray = null,
        string fieldNameProp = "name",
        string? altProp = null)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (!doc.RootElement.TryGetProperty(arrayProp, out var arr) || arr.ValueKind != JsonValueKind.Array)
                return;

            foreach (var item in arr.EnumerateArray())
            {
                if (nestedArray != null
                    && item.TryGetProperty(nestedArray, out var nested)
                    && nested.ValueKind == JsonValueKind.Array)
                {
                    foreach (var f in nested.EnumerateArray())
                        TryAddFieldName(f, names, fieldNameProp, altProp);
                }
                else
                {
                    TryAddFieldName(item, names, fieldNameProp, altProp);
                }
            }
        }
        catch
        {
            /* ignore */
        }
    }

    private static void TryAddFieldName(JsonElement el, HashSet<string> names, string prop, string? altProp)
    {
        if (el.TryGetProperty(prop, out var nameEl))
        {
            var n = nameEl.GetString();
            if (!string.IsNullOrWhiteSpace(n))
                names.Add(n);
        }

        if (altProp != null && el.TryGetProperty(altProp, out var altEl))
        {
            var a = altEl.GetString();
            if (!string.IsNullOrWhiteSpace(a))
                names.Add(a);
        }
    }
}
