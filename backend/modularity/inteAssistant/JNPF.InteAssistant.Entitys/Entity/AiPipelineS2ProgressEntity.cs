using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// S2 需求分析流水线进度（L2 唯一真相：pipelineStage + specPhase + clarRound）。
/// 版 本：v5.2.0 · CR-20260718-01 P4 阶段 2
/// </summary>
[SugarTable("BASE_AI_PIPELINE_S2_PROGRESS", TableDescription = "S2需求分析流水线进度")]
public class AiPipelineS2ProgressEntity : TenantCLDSEntityBase
{
    /// <summary>项目 ID（三元组 projectId）。</summary>
    [SugarColumn(ColumnName = "F_PROJECT_ID")]
    public string ProjectId { get; set; } = "";

    /// <summary>流水线 ID（三元组 pipelineId，字符串存储）。</summary>
    [SugarColumn(ColumnName = "F_PIPELINE_ID")]
    public string PipelineId { get; set; } = "";

    /// <summary>细粒度流水线阶段（<see cref="Dto.Skills.S2PipelineStage"/> 整型）。</summary>
    [SugarColumn(ColumnName = "F_PIPELINE_STAGE")]
    public int PipelineStage { get; set; }

    /// <summary>说明书文档态（<see cref="Dto.Skills.RequirementSpecPhase"/> 整型）。</summary>
    [SugarColumn(ColumnName = "F_SPEC_PHASE")]
    public int SpecPhase { get; set; }

    /// <summary>结构化澄清当前轮次（0=无）。</summary>
    [SugarColumn(ColumnName = "F_CLAR_ROUND")]
    public int ClarRound { get; set; }

    /// <summary>说明书版本号（Supersede 时 +1）。</summary>
    [SugarColumn(ColumnName = "F_SPEC_VERSION")]
    public int SpecVersion { get; set; } = 1;

    /// <summary>02 正式版 SHA256（Rendered+）。</summary>
    [SugarColumn(ColumnName = "F_CONTENT_HASH", IsNullable = true)]
    public string? ContentHash { get; set; }

    /// <summary>02 正式版字节长度。</summary>
    [SugarColumn(ColumnName = "F_CONTENT_LENGTH", IsNullable = true)]
    public int? ContentLength { get; set; }

    /// <summary>是否在等用户（追问/澄清/确认）。</summary>
    [SugarColumn(ColumnName = "F_AWAITING_USER")]
    public bool AwaitingUser { get; set; }
}
