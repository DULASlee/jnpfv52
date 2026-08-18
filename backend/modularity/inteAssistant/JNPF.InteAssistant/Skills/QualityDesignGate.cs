using JNPF.FriendlyException;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 设计启动质量可选门控（25 §6 / 28）：
/// - 有 <c>sa_quality_score</c> 行时强制 PassesGate（总分≥60、结构≥70、无 CRITICAL）
/// - 无评分行时放行（兼容旧 pipeline），但记 Warning
/// - 有 CRITICAL 一致性发现时一律拒绝（哨兵 PASSED 不计）
/// </summary>
public static class QualityDesignGate
{
    public sealed record Snapshot(
        bool HasScore,
        decimal? TotalScore,
        decimal? StructureScore,
        int CriticalCount,
        bool Passes);

    public static async Task<Snapshot> EvaluateAsync(
        ISqlSugarClient db,
        string tenantId,
        string projectId,
        string pipelineId,
        CancellationToken ct = default)
    {
        var score = await db.Ado.SqlQuerySingleAsync<ScoreRow>("""
            SELECT TOP 1
                F_StructureScore AS StructureScore,
                F_TotalScore AS TotalScore
            FROM sa_quality_score
            WHERE F_TenantId = @tenantId AND F_ProjectId = @projectId AND F_PIPELINE_ID = @pipelineId
            ORDER BY F_CreatedAt DESC
            """, new { tenantId, projectId, pipelineId });

        var criticalCount = await db.Ado.GetIntAsync("""
            SELECT COUNT(1)
            FROM sa_consistency
            WHERE F_TenantId = @tenantId AND F_ProjectId = @projectId AND F_PIPELINE_ID = @pipelineId
              AND F_Severity = N'CRITICAL'
              AND ISNULL(F_CheckType, N'') <> N'PASSED'
            """, new { tenantId, projectId, pipelineId });

        if (score == null)
        {
            // 无评分：可选门控 → 仅 CRITICAL 阻断
            var passesWithoutScore = criticalCount == 0;
            return new Snapshot(false, null, null, criticalCount, passesWithoutScore);
        }

        var total = score.TotalScore;
        var structure = score.StructureScore;
        var passes = total >= 60m && structure >= 70m && criticalCount == 0;
        return new Snapshot(true, total, structure, criticalCount, passes);
    }

    /// <summary>
    /// 设计启动强制检查。无评分且无 CRITICAL → 放行；否则须 Passes。
    /// </summary>
    public static async Task EnsureCanRunDesignAsync(
        ISqlSugarClient db,
        string tenantId,
        string projectId,
        string pipelineId,
        ILogger logger,
        CancellationToken ct = default)
    {
        var snap = await EvaluateAsync(db, tenantId, projectId, pipelineId, ct);
        if (snap.Passes)
        {
            if (!snap.HasScore)
                logger.LogWarning(
                    "设计启动：无 sa_quality_score，跳过分数门控（仍校验 CRITICAL=0）pipeline={PipelineId}",
                    pipelineId);
            return;
        }

        if (snap.CriticalCount > 0)
            throw Oops.Bah($"一致性存在 {snap.CriticalCount} 条 CRITICAL，禁止启动设计 Skill")
                .StatusCode(StatusCodes.Status400BadRequest);

        throw Oops.Bah(
                $"质量门控未通过：总分={snap.TotalScore:0.#}（须≥60）、结构分={snap.StructureScore:0.#}（须≥70）")
            .StatusCode(StatusCodes.Status400BadRequest);
    }

    private sealed class ScoreRow
    {
        public decimal StructureScore { get; set; }
        public decimal TotalScore { get; set; }
    }
}
