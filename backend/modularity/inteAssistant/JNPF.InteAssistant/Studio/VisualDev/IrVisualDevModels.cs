using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JNPF.InteAssistant.Studio.VisualDev;

/// <summary>
/// P8-M01 IR2 FormPageIR typed model（消除 LLM 产出的 component/componentType 歧义）。
///
/// 生产者：UiDesignSkillService.GenerateFormPageIrAsync（LLM 输出 pages[].fields[]）。
/// 原为 opaque JSON，此处定义强类型契约供 mapper 消费。
/// 兼容两种字段名：component（TesterSkillInputBuilder 读）+ componentType（LLM prompt 写）。
/// </summary>
public class FormPageIRPayload
{
    [JsonPropertyName("pages")]
    public List<FormPageSpec> Pages { get; set; } = new();

    /// <summary>降级：部分 fixture 把 fields 放在 root（非 pages 内）。mapper 两者都支持。</summary>
    [JsonPropertyName("fields")]
    public List<FormFieldSpec>? RootFields { get; set; }

    [JsonPropertyName("pageName")]
    public string? PageName { get; set; }
}

public class FormPageSpec
{
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }  // list|form|detail|dashboard

    [JsonPropertyName("fields")]
    public List<FormFieldSpec> Fields { get; set; } = new();
}

public class FormFieldSpec
{
    /// <summary>字段 ID（LLM 写 id；fixture 写 fieldId）。mapper 两者都接受。</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("fieldId")]
    public string? FieldId { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    /// <summary>组件类型（LLM prompt 要求 componentType；fixture/消费方读 component）。</summary>
    [JsonPropertyName("componentType")]
    public string? ComponentType { get; set; }

    [JsonPropertyName("component")]
    public string? Component { get; set; }

    /// <summary>解析后的统一 ID（Id ?? FieldId）</summary>
    [JsonIgnore]
    public string ResolvedId => Id ?? FieldId ?? string.Empty;

    /// <summary>解析后的统一 componentType（component ?? componentType）</summary>
    [JsonIgnore]
    public string ResolvedComponent => Component ?? ComponentType ?? "Input";
}

/// <summary>
/// IR1 EventSpec confirmedField typed model。
/// 生产者：EventSpecAssembler.ExtractConfirmedFields（{name, type, required, source}）。
/// </summary>
public class EventSpecConfirmedField
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "NVARCHAR(255)";

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }
}

/// <summary>mapper 缺口报告（每个未映射字段一条，不 silent drop）</summary>
public class MappingGap
{
    /// <summary>UI field id（FormPageIR）</summary>
    public string FieldId { get; set; } = string.Empty;

    /// <summary>UI field label</summary>
    public string? Label { get; set; }

    /// <summary>UI componentType</summary>
    public string? ComponentType { get; set; }

    /// <summary>缺口类型：no_ir1_match（无 IR1 confirmedField）/ unknown_component（未知组件）/ missing_id</summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// mapper 输出结果（独立 DTO，避免 inteAssistant 引用 VisualDev.Entitys 导致循环依赖）。
/// 调用方可据此构造 VisualDevCrInput 并 POST /api/visualdev/Base。
/// </summary>
public class VisualDevMappingResult
{
    /// <summary>formData JSON（可直接作为 VisualDevCrInput.formData 的值）</summary>
    public string? FormDataJson { get; set; }

    /// <summary>建议的 VisualDev 功能名（来自 pageName 或 project）</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>建议的 enCode（来自 pageName 拼音/英文，兜底 projectId）</summary>
    public string EnCode { get; set; } = string.Empty;

    /// <summary>建议的 type（1=Web设计 默认；3=流程表单 当 enableFlow）</summary>
    public int Type { get; set; } = 1;

    /// <summary>建议的 webType（1=纯表单 默认）</summary>
    public int WebType { get; set; } = 1;

    /// <summary>已映射字段数</summary>
    public int MappedFieldCount { get; set; }

    /// <summary>缺口报告（每条对应一个 MappingGapReported 事件）</summary>
    public List<MappingGap> Gaps { get; set; } = new();

    /// <summary>是否通过基础 schema 校验（非空 fields + 每个 field 有 jnpfKey/vModel）</summary>
    public bool SchemaValid { get; set; }

    public string TenantId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string PipelineId { get; set; } = string.Empty;
}
