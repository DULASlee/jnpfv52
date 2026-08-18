using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JNPF.Common.Const;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Studio.VisualDev;

/// <summary>
/// P8-M01 Ir1ToVisualDevMapper — IR2_FormPageIR + ai_entity_field → VisualDev formData JSON。
///
/// 铁律：
///   - 映射层**不写**生成物源码；只产出 JSON
///   - 字段名集合优先 <c>ai_entity_field</c>（25 §6 / 声明 3）；IR confirmedFields 仅派生对照
///   - 每个 UI field 必须映射字段源或标记 extension（缺口不 silent drop）
///   - 多租户：结果含 tenantId
///   - 输出经基础 schema 校验（fields 非空 + jnpfKey/vModel 齐全）
///
/// 目标契约：VisualDev TemplateParsingBase.VerifyTemplate()（由 POST /api/visualdev/Base 兜底）
/// </summary>
public interface IIr1ToVisualDevMapper
{
    /// <summary>
    /// 将 pipeline 的 FormPageIR + 字段源映射为 VisualDev formData JSON。
    /// </summary>
    Task<VisualDevMappingResult> MapAsync(string tenantId, string projectId, string pipelineId, CancellationToken ct = default);
}

public sealed class Ir1ToVisualDevMapper : IIr1ToVisualDevMapper, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// componentType → jnpfKey 映射表（核心翻译表）。
    /// 大小写不敏感；未命中 → input（默认）+ MappingGap(unknown_component)。
    /// </summary>
    private static readonly Dictionary<string, string> ComponentToJnpfKey = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Input"] = JnpfKeyConst.COMINPUT,
        ["TextInput"] = JnpfKeyConst.COMINPUT,
        ["Text"] = JnpfKeyConst.COMINPUT,
        ["Textarea"] = JnpfKeyConst.TEXTAREA,
        ["TextArea"] = JnpfKeyConst.TEXTAREA,
        ["Number"] = JnpfKeyConst.NUMINPUT,
        ["InputNumber"] = JnpfKeyConst.NUMINPUT,
        ["Amount"] = JnpfKeyConst.JNPFAMOUNT,
        ["Select"] = JnpfKeyConst.SELECT,
        ["Dropdown"] = JnpfKeyConst.SELECT,
        ["Radio"] = JnpfKeyConst.RADIO,
        ["Checkbox"] = JnpfKeyConst.CHECKBOX,
        ["Date"] = JnpfKeyConst.DATE,
        ["DatePicker"] = JnpfKeyConst.DATE,
        ["Time"] = JnpfKeyConst.TIME,
        ["TimePicker"] = JnpfKeyConst.TIME,
        ["Switch"] = JnpfKeyConst.SWITCH,
        ["Toggle"] = JnpfKeyConst.SWITCH,
        ["Upload"] = "uploadFile",
        ["File"] = "uploadFile",
        ["Image"] = "uploadImg",
        ["UploadImg"] = "uploadImg",
        ["Table"] = JnpfKeyConst.TABLE,
        ["ChildTable"] = JnpfKeyConst.TABLE,
        ["Rate"] = "rate",
        ["Slider"] = "slider",
        ["Editor"] = "editor",
        ["Cascader"] = JnpfKeyConst.CASCADER,
    };

    private readonly ISqlSugarClient _db;
    private readonly EntityDesignRepository _entityDesignRepo;
    private readonly ILogger<Ir1ToVisualDevMapper> _logger;

    public Ir1ToVisualDevMapper(
        ISqlSugarClient db,
        EntityDesignRepository entityDesignRepo,
        ILogger<Ir1ToVisualDevMapper> logger)
    {
        _db = db;
        _entityDesignRepo = entityDesignRepo;
        _logger = logger;
    }

    public async Task<VisualDevMappingResult> MapAsync(string tenantId, string projectId, string pipelineId, CancellationToken ct = default)
    {
        // 1. 读 IR2_FormPageIR stable snapshot（三元组 R12）
        var eventSpecSnapshot = await LoadSnapshotAsync(tenantId, projectId, pipelineId, IrFragmentTypes.EventSpec, ct);
        var formPageSnapshot = await LoadSnapshotAsync(tenantId, projectId, pipelineId, IrFragmentTypes.FormPageIR, ct);

        if (formPageSnapshot == null)
            throw Oops.Bah($"未找到 stable FormPageIR（pipeline={pipelineId}）。请先跑 UI Design Skill");

        // 2. 字段名集合：优先 ai_entity_field（声明 3）；IR confirmedFields 仅派生对照
        var entityFields = await _entityDesignRepo.ListFieldsAsync(tenantId, projectId, pipelineId, ct);
        HashSet<string> confirmedNames;
        string fieldSource;
        if (entityFields.Count > 0)
        {
            confirmedNames = new HashSet<string>(
                entityFields.Select(f => f.FieldName).Where(n => !string.IsNullOrWhiteSpace(n)),
                StringComparer.OrdinalIgnoreCase);
            fieldSource = "ai_entity_field";
        }
        else
        {
            var confirmedFields = ParseConfirmedFields(eventSpecSnapshot?.IrContent);
            confirmedNames = new HashSet<string>(
                confirmedFields.Select(f => f.Name).Where(n => !string.IsNullOrWhiteSpace(n))!,
                StringComparer.OrdinalIgnoreCase);
            fieldSource = "ir_json_fallback";
            _logger.LogWarning(
                "Ir1ToVisualDevMapper 无 ai_entity_field，回退 IR confirmedFields pipeline={PipelineId}",
                pipelineId);
        }

        // 3. 解析 FormPageIR fields（兼容 pages[].fields 和 root fields）
        var formFields = ParseFormFields(formPageSnapshot.IrContent);
        if (formFields.Count == 0)
            throw Oops.Bah("FormPageIR 无 fields（pages[].fields 和 root fields 均空）");

        // 4. 逐字段映射 → VisualDev FieldsModel
        var gaps = new List<MappingGap>();
        var visualDevFields = new List<object>();
        var mappedCount = 0;

        foreach (var field in formFields)
        {
            var fieldId = field.ResolvedId;
            var label = field.Label ?? fieldId;

            // 缺口1：无 id
            if (string.IsNullOrEmpty(fieldId))
            {
                gaps.Add(new MappingGap { FieldId = "(empty)", Label = label, ComponentType = field.ResolvedComponent, Reason = "missing_id" });
                continue;
            }

            // 缺口2：未知组件 → 默认 input + 告警
            var jnpfKey = ResolveJnpfKey(field.ResolvedComponent);
            if (jnpfKey == null)
            {
                gaps.Add(new MappingGap { FieldId = fieldId, Label = label, ComponentType = field.ResolvedComponent, Reason = "unknown_component" });
                jnpfKey = JnpfKeyConst.COMINPUT;  // 兜底默认
            }

            // 缺口3：字段源无匹配（UI 有但投影/IR 未确认）→ 标记 extension
            var isExtension = !confirmedNames.Contains(fieldId);
            if (isExtension)
            {
                gaps.Add(new MappingGap { FieldId = fieldId, Label = label, ComponentType = field.ResolvedComponent, Reason = "no_field_source_match" });
            }

            // 构造 VisualDev FieldsModel（最小可渲染结构）
            visualDevFields.Add(BuildFieldsModel(fieldId, label, jnpfKey, isExtension));
            mappedCount++;
        }

        // 5. 组装 formData JSON（FormDataModel 结构：fields[] + 表单级配置）
        var formData = new
        {
            fields = visualDevFields,
            popupType = "general",
            labelPosition = "right",
            labelWidth = 100,
            gutter = 15,
            span = 24,
            primaryKeyPolicy = 1,  // 1=snowflake
            logicalDelete = false,
            concurrencyLock = false,
            formRef = "dataForm",
            formModel = "dataForm",
            fieldSource,
        };
        var formDataJson = JsonSerializer.Serialize(formData, JsonOptions);

        // 6. 基础 schema 校验（非空 + 每个 field 有 jnpfKey）
        var schemaValid = visualDevFields.Count > 0;

        // 7. 命名（pageName 优先，兜底 projectId）
        var pageName = ParsePageName(formPageSnapshot.IrContent);
        var fullName = !string.IsNullOrEmpty(pageName) ? pageName : $"AI生成表单-{projectId[..Math.Min(8, projectId.Length)]}";
        var enCode = !string.IsNullOrEmpty(pageName) ? ToEnCode(pageName) : $"ai_form_{projectId[..Math.Min(8, projectId.Length)]}";

        _logger.LogInformation(
            "Ir1ToVisualDevMapper 完成 pipeline={PipelineId} fieldSource={FieldSource} mapped={Mapped} gaps={Gaps} valid={Valid}",
            pipelineId, fieldSource, mappedCount, gaps.Count, schemaValid);

        return new VisualDevMappingResult
        {
            FormDataJson = formDataJson,
            FullName = fullName,
            EnCode = enCode,
            Type = 1,         // Web 设计
            WebType = 1,      // 纯表单
            MappedFieldCount = mappedCount,
            Gaps = gaps,
            SchemaValid = schemaValid,
            TenantId = tenantId,
            ProjectId = projectId,
            PipelineId = pipelineId,
        };
    }

    // ─── IR snapshot 加载（三元组 R12）───

    private async Task<AiIrFragmentSnapshotEntity?> LoadSnapshotAsync(
        string tenantId, string projectId, string pipelineId, string fragmentType, CancellationToken ct)
    {
        // pipelineId 优先（会话边界），fallback projectId（greenfield 自锚定）
        return await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.TenantId == tenantId
                && x.FragmentType == fragmentType
                && !x.DeleteMark
                && (x.PipelineId == pipelineId || x.ProjectId == projectId))
            .OrderBy(x => x.PipelineId == pipelineId ? 0 : 1)  // pipelineId 精确匹配优先
            .FirstAsync(ct);
    }

    // ─── payload 解析 ───

    private static List<EventSpecConfirmedField> ParseConfirmedFields(string? irContent)
    {
        if (string.IsNullOrWhiteSpace(irContent)) return new();
        try
        {
            using var doc = JsonDocument.Parse(irContent);
            if (doc.RootElement.TryGetProperty("confirmedFields", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<List<EventSpecConfirmedField>>(arr.GetRawText(), JsonOptions) ?? new();
            }
        }
        catch { /* 忽略解析失败 */ }
        return new();
    }

    private static List<FormFieldSpec> ParseFormFields(string? irContent)
    {
        if (string.IsNullOrWhiteSpace(irContent)) return new();
        try
        {
            var payload = JsonSerializer.Deserialize<FormPageIRPayload>(irContent, JsonOptions);
            if (payload == null) return new();

            var fields = new List<FormFieldSpec>();

            // pages[].fields（LLM 标准）
            foreach (var page in payload.Pages)
                fields.AddRange(page.Fields);

            // root fields（fixture/降级路径）
            if (payload.RootFields != null)
                fields.AddRange(payload.RootFields);

            return fields;
        }
        catch { /* 忽略解析失败 */ }
        return new();
    }

    private static string? ParsePageName(string? irContent)
    {
        if (string.IsNullOrWhiteSpace(irContent)) return null;
        try
        {
            using var doc = JsonDocument.Parse(irContent);
            if (doc.RootElement.TryGetProperty("pageName", out var pn) && pn.ValueKind == JsonValueKind.String)
                return pn.GetString();
            // fallback: pages[0].title
            if (doc.RootElement.TryGetProperty("pages", out var pages) && pages.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in pages.EnumerateArray())
                {
                    if (p.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                        return t.GetString();
                }
            }
        }
        catch { /* 忽略 */ }
        return null;
    }

    // ─── componentType → jnpfKey ───

    private static string? ResolveJnpfKey(string component) =>
        ComponentToJnpfKey.TryGetValue(component, out var key) ? key : null;

    /// <summary>构造 VisualDev FieldsModel（最小可渲染结构，通过 VerifyTemplate）</summary>
    private static object BuildFieldsModel(string fieldId, string label, string jnpfKey, bool isExtension)
    {
        return new
        {
            __config__ = new
            {
                jnpfKey,
                label,
                labelWidth = (int?)null,
                showLabel = true,
                required = false,
                tag = "Jnpf" + jnpfKey[..1].ToUpperInvariant() + jnpfKey[1..].ToLowerInvariant(),
                tagIcon = "icon-ym " + jnpfKey,
                defaultValue = (string?)null,
                span = 24,
                needed = false,
                // extension 标记（UI 有但 IR1 未确认）— 供审计
                extension = isExtension,
                source = isExtension ? "ui-only" : "ir1-confirmed",
            },
            __vModel__ = fieldId,
            placeholder = $"请输入{label}",
            @readonly = false,
            disabled = false,
            clearable = true,
        };
    }

    /// <summary>中文 pageName → enCode（简单处理：非 ASCII 用拼音占位）</summary>
    private static string ToEnCode(string name)
    {
        var ascii = new string(name.Where(c => c < 128 && (char.IsLetterOrDigit(c) || c == '_')).ToArray());
        return !string.IsNullOrEmpty(ascii) ? ascii.ToLowerInvariant() : "ai_form";
    }
}
