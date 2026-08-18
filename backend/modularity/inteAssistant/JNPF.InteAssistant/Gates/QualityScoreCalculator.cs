using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Runtime;
using JNPF.InteAssistant.Sa;
using JNPF.InteAssistant.Studio;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Gates;

/// <summary>
/// 28 号 §6：质量评分器——5 维度加权（结构 25% + 覆盖 25% + 一致性 20% + 深度 15% + DDD 15%）。
/// 产出 sa_quality_score 表记录。
/// </summary>
public interface IQualityScoreCalculator
{
    /// <summary>计算 5 维度质量评分并写入 sa_quality_score 表。</summary>
    Task<QualityScore> CalculateAsync(
        PipelineTriple triple, SaNineViewCompileResult compileResult,
        EntityDesignProjection entityFields, DddProjectionResult dddResult,
        IReadOnlyList<ConsistencyFinding> consistencyFindings, int roundNumber,
        CancellationToken ct = default);
}

/// <summary>5 维度质量评分（0-100）。</summary>
public sealed class QualityScore
{
    public decimal StructureScore { get; init; }   // 结构完整度
    public decimal CoverageScore { get; init; }    // 决策覆盖率
    public decimal ConsistencyScore { get; init; } // 一致性
    public decimal DepthScore { get; init; }       // 深度
    public decimal DddScore { get; init; }         // DDD 增强

    /// <summary>综合评分 = 各维度加权。</summary>
    public decimal TotalScore =>
        Math.Round(StructureScore * 0.25m + CoverageScore * 0.25m
                   + ConsistencyScore * 0.20m + DepthScore * 0.15m
                   + DddScore * 0.15m, 2);

    /// <summary>是否通过质量门（28 号 §7.3：综合 ≥60 / 无 CRITICAL / 结构 ≥70）。</summary>
    public bool PassesGate(int criticalCount) =>
        TotalScore >= 60m && criticalCount == 0 && StructureScore >= 70m;
}

public sealed class QualityScoreCalculator : IQualityScoreCalculator, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ISqlSugarClient _db;
    private readonly ILogger<QualityScoreCalculator> _logger;

    public QualityScoreCalculator(ISqlSugarClient db, ILogger<QualityScoreCalculator> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<QualityScore> CalculateAsync(
        PipelineTriple triple, SaNineViewCompileResult compileResult,
        EntityDesignProjection entityFields, DddProjectionResult dddResult,
        IReadOnlyList<ConsistencyFinding> consistencyFindings, int roundNumber,
        CancellationToken ct = default)
    {
        var score = new QualityScore
        {
            StructureScore = CalcStructureScore(compileResult, entityFields),
            CoverageScore = CalcCoverageScore(compileResult),
            ConsistencyScore = CalcConsistencyScore(consistencyFindings),
            DepthScore = CalcDepthScore(compileResult),
            DddScore = CalcDddScore(dddResult),
        };

        await PersistScoreAsync(triple, score, roundNumber, ct);

        _logger.LogInformation(
            "质量评分：结构={S} 覆盖={C} 一致性={Con} 深度={D} DDD={Ddd} 综合={Total}",
            score.StructureScore, score.CoverageScore, score.ConsistencyScore,
            score.DepthScore, score.DddScore, score.TotalScore);

        return score;
    }

    /// <summary>结构完整度：SA 九步产出完整度 + 事件/实体数量合理性。</summary>
    private static decimal CalcStructureScore(SaNineViewCompileResult compileResult, EntityDesignProjection entityFields)
    {
        var eventCount = compileResult.EventResults.Count;
        var entityCount = entityFields.TableNames().Count;
        var fieldCount = entityFields.Fields.Count;

        // 基础分：有事件+有实体+有字段
        var score = 0m;
        if (eventCount > 0) score += 30;
        if (entityCount > 0) score += 25;
        if (fieldCount > 0) score += 20;
        // 事件数 3-15 为合理范围
        if (eventCount is >= 3 and <= 15) score += 15;
        // 实体数 2-12 为合理范围
        if (entityCount is >= 2 and <= 12) score += 10;

        return Math.Min(score, 100);
    }

    /// <summary>决策覆盖率：业务规则 + 假设项覆盖度（假设多=分析深但需确认，少=可能遗漏）。</summary>
    private static decimal CalcCoverageScore(SaNineViewCompileResult compileResult)
    {
        var ruleCount = compileResult.Source.BusinessRules.Count;
        var assumptionCount = compileResult.Assumptions.Count;
        var eventCount = compileResult.EventResults.Count;

        if (eventCount == 0) return 0;

        // 业务规则覆盖率：规则数 / 事件数（每个事件至少 1 条规则为理想）
        var ruleCoverage = Math.Min((decimal)ruleCount / eventCount, 1.0m) * 50;
        // 假设项覆盖：有假设=分析师考虑了边界，但过多也不好
        var assumptionScore = assumptionCount == 0 ? 20 : Math.Min(assumptionCount * 5, 50);

        return Math.Round(ruleCoverage + assumptionScore, 2);
    }

    /// <summary>一致性：无 CRITICAL=100，每条 WARNING 扣 5，无 INFO 加分。</summary>
    private static decimal CalcConsistencyScore(IReadOnlyList<ConsistencyFinding> findings)
    {
        var critical = findings.Count(f => f.Severity == "CRITICAL");
        var warning = findings.Count(f => f.Severity == "WARNING");

        if (critical > 0) return Math.Max(0, 100 - critical * 20);

        return Math.Max(0, 100 - warning * 5);
    }

    /// <summary>深度：复杂事件占比（complex 事件多=分析深）+ StateTransitions 覆盖。</summary>
    private static decimal CalcDepthScore(SaNineViewCompileResult compileResult)
    {
        var events = compileResult.Source.BusinessEvents;
        if (events.Count == 0) return 0;

        var complexRatio = (decimal)events.Count(e =>
            e.ComplexityHint.Contains("complex", StringComparison.OrdinalIgnoreCase)
            || e.ComplexityHint.Contains("medium", StringComparison.OrdinalIgnoreCase)) / events.Count;

        var stateCoverage = compileResult.Source.StateTransitions.Count > 0 ? 30 : 10;

        return Math.Round(complexRatio * 70 + stateCoverage, 2);
    }

    /// <summary>DDD 增强：5 视角各有数据（各 20 分），confidence &lt; 0.5 减半。</summary>
    private static decimal CalcDddScore(DddProjectionResult ddd)
    {
        var score = 0m;

        // 视角 1 领域模型
        if (ddd.DomainModel.SubDomains.Count > 0 || ddd.DomainModel.CoreDomain != null)
            score += ddd.DomainModel.Confidence >= 0.5 ? 20 : 10;

        // 视角 2 聚合设计
        if (ddd.AggregateDesign.RootEntities.Count > 0)
            score += ddd.AggregateDesign.Confidence >= 0.5 ? 20 : 10;

        // 视角 3 事件目录
        if (ddd.EventCatalog.Events.Count > 0)
            score += ddd.EventCatalog.Confidence >= 0.5 ? 20 : 10;

        // 视角 4 CQRS
        if (ddd.Cqrs.Commands.Count + ddd.Cqrs.Queries.Count > 0)
            score += ddd.Cqrs.Confidence >= 0.5 ? 20 : 10;

        // 视角 5 集成点
        if (ddd.Integration.IntegrationPoints.Count > 0)
            score += ddd.Integration.Confidence >= 0.5 ? 20 : 10;

        return Math.Round(score, 2);
    }

    private async Task PersistScoreAsync(PipelineTriple triple, QualityScore score, int roundNumber, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var rows = new List<SaQualityScoreRow>
        {
            new()
            {
                F_Id = Guid.NewGuid().ToString("N"),
                F_TenantId = triple.TenantId,
                F_ProjectId = triple.ProjectId,
                F_PIPELINE_ID = triple.PipelineId.ToString(),
                F_RoundNumber = roundNumber,
                F_StructureScore = score.StructureScore,
                F_CoverageScore = score.CoverageScore,
                F_ConsistencyScore = score.ConsistencyScore,
                F_DepthScore = score.DepthScore,
                F_DddScore = score.DddScore,
                F_TotalScore = score.TotalScore,
                F_CreatedAt = now,
            },
        };

        await _db.Insertable(rows).AS("sa_quality_score").ExecuteCommandAsync(ct);
    }

    /// <summary>sa_quality_score 表行映射（SqlSugar Insertable 要求具体类型，不支持匿名）。</summary>
    private sealed class SaQualityScoreRow
    {
        public string F_Id { get; set; } = string.Empty;
        public string F_TenantId { get; set; } = string.Empty;
        public string F_ProjectId { get; set; } = string.Empty;
        public string F_PIPELINE_ID { get; set; } = string.Empty;
        public int F_RoundNumber { get; set; }
        public decimal F_StructureScore { get; set; }
        public decimal F_CoverageScore { get; set; }
        public decimal F_ConsistencyScore { get; set; }
        public decimal F_DepthScore { get; set; }
        public decimal F_DddScore { get; set; }
        public decimal F_TotalScore { get; set; }
        public DateTime F_CreatedAt { get; set; }
    }
}

internal static class QualityStringExt
{
    public static bool Contains(this string source, string value, StringComparison comparison)
        => source.IndexOf(value, comparison) >= 0;
}
