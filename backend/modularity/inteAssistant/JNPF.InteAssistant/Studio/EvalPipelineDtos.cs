using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// Eval Pipeline 四层评估结果（L1-L4）。
/// L1-L3 由 EvalPipelineRunner 确定性计算（P7-E01）；L4 由 LlmJudgeService 填充（P7-E02）。
/// 2026 实践：pass^k 一致性 > pass@k，首版 k=1 退化为 pass@1，架构预留扩展点。
/// </summary>
public class EvalPipelineResult
{
    public long RunId { get; set; }

    /// <summary>关联的 skill_run id（三元组范围内的具体执行）</summary>
    public string? SkillRunId { get; set; }

    public string? SkillId { get; set; }

    /// <summary>L1 组件层：单 Skill 产出 JSON Schema 校验（确定性，无 LLM）</summary>
    public LayerResult? L1 { get; set; }

    /// <summary>L2 轨迹层：冗余 LLM 调用检测（确定性，无 LLM）</summary>
    public LayerResult? L2 { get; set; }

    /// <summary>L3 任务层：DoD 完成率（确定性，无 LLM）</summary>
    public LayerResult? L3 { get; set; }

    /// <summary>L4 业务层：Judge 评分（P7-E02 填充，pass/fail 二元）</summary>
    public LayerResult? L4 { get; set; }

    /// <summary>
    /// pass^k 一致性：同一 case 重复运行 k 次全部通过才算 1.0。
    /// 首版 k=1（退化为 pass@1）；后续按 EvalRunInput.K 可配置。
    /// </summary>
    public double? Consistency { get; set; }

    /// <summary>L1-L3 综合结论（fail-fast：L1 不过则整体 fail，L4 不参与）</summary>
    public bool OverallPassed => L1?.Passed == true && L2?.Passed == true && L3?.Passed == true;

    /// <summary>产出摘要（供 L4 Judge prompt 引用，避免传全文）</summary>
    public string? OutputDigest { get; set; }
}

/// <summary>
/// 单层评估结果。强制 pass/fail 二元判断（2026 实践：二元 > 1-5 分制，暴露真实分歧）。
/// </summary>
public class LayerResult
{
    public bool Passed { get; set; }

    /// <summary>度量指标描述，如 "schema_ok"、"llm_calls=5,ir_appends=2"</summary>
    public string Metric { get; set; } = string.Empty;

    /// <summary>告警列表（L2 轨迹冗余、L3 DoD 缺失项等）</summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>该层耗时（ms）</summary>
    public long ElapsedMs { get; set; }
}

/// <summary>P7-E01 三层评估输入（绑定具体 skill_run + 三元组 R12）</summary>
public class EvalPipelineRequest
{
    /// <summary>EvalRun 主键（BASE_AI_EVAL_RUN.F_Id）</summary>
    public long EvalRunId { get; set; }

    /// <summary>被评估的 skill_run id（ai_skill_runs.F_Id, string/GUID）</summary>
    public string SkillRunId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string PipelineId { get; set; } = string.Empty;
    public string SkillId { get; set; } = string.Empty;
}

/// <summary>历史 eval run 查询 DTO（含分层结果展开）</summary>
public class EvalRunDetailDto
{
    public long Id { get; set; }
    public long SetId { get; set; }
    public long? CaseId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RunAt { get; set; }

    public string TenantId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string PipelineId { get; set; } = string.Empty;

    public bool? OverallPassed { get; set; }
    public decimal? JudgeKappa { get; set; }
    public decimal? Consistency { get; set; }

    /// <summary>反序列化后的分层结果（F_LayerResults JSON）</summary>
    [JsonPropertyName("layerResults")]
    public LayerResultsDto? LayerResults { get; set; }
}

public class LayerResultsDto
{
    public LayerResult? L1 { get; set; }
    public LayerResult? L2 { get; set; }
    public LayerResult? L3 { get; set; }
    public LayerResult? L4 { get; set; }
}
