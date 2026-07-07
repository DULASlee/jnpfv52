using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JNPF.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// P7-E02 Judge 校准服务（Cohen's kappa）。
///
/// 2026 实践核心风险控制：
///   - 未校准的 Judge 可能显示"完美指标"但与人类共识 Cohen's kappa 仅 0.31
///   - kappa < 0.6 不允许 L4 gating（降级为 advisory）
///   - 月度校准（EvalCalibrationJob 触发）
///
/// 校准数据源：join EvalRun.F_LayerResults.l4.passed（Judge）与
///            BASE_AI_SKILL_REVIEW.F_Verdict（人工），按 EvalRunId 配对。
/// </summary>
public interface IJudgeCalibrationService
{
    /// <summary>
    /// 校准 Judge：对已有人工 review 的 case，比较 Judge 与人类的 Cohen's kappa。
    /// kappa < 0.6 → Status=untrusted（禁止 L4 gating）。
    /// </summary>
    Task<JudgeCalibrationReport> CalibrateAsync(string tenantId, int minSamples = 10, CancellationToken ct = default);
}

public sealed class JudgeCalibrationService : IJudgeCalibrationService, ITransient
{
    private const double KappaTrustedThreshold = 0.6;  // kappa >= 0.6 才允许 gating
    private const int DefaultMinSamples = 10;          // 务实：项目样本不足，首版阈值 10

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ISqlSugarClient _db;
    private readonly ILogger<JudgeCalibrationService> _logger;

    public JudgeCalibrationService(ISqlSugarClient db, ILogger<JudgeCalibrationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<JudgeCalibrationReport> CalibrateAsync(string tenantId, int minSamples = DefaultMinSamples, CancellationToken ct = default)
    {
        // 1. 取该租户所有有 L4 Judge 结果的 eval run
        var judgeRuns = await _db.Queryable<EvalRunEntity>()
            .Where(x => x.F_TenantId == tenantId && x.F_LayerResults != null)
            .Select(x => new { x.F_Id, x.F_LayerResults })
            .ToListAsync(ct);

        if (judgeRuns.Count == 0)
            return InsufficientReport(0, "无 Judge 评估记录");

        // 2. 取该租户所有人工 review（按 EvalRunId 索引）
        var reviews = await _db.Queryable<SkillReviewEntity>()
            .Where(x => x.F_TenantId == tenantId && x.F_EvalRunId != null)
            .Select(x => new { x.F_EvalRunId, x.F_Verdict })
            .ToListAsync(ct);

        if (reviews.Count == 0)
            return InsufficientReport(0, "无人工抽检记录");

        // 3. 配对：同一 EvalRunId 同时有 Judge + 人工 verdict
        var pairs = new List<JudgeHumanPair>();
        foreach (var run in judgeRuns)
        {
            var judgePassed = TryParseJudgeVerdict(run.F_LayerResults);
            if (judgePassed == null) continue;  // L4 未跑或解析失败

            // 取该 eval run 的人工 verdict（多人时取多数决）
            var runReviews = reviews.Where(r => r.F_EvalRunId == run.F_Id).ToList();
            if (runReviews.Count == 0) continue;

            var humanPassCount = runReviews.Count(r =>
                r.F_Verdict.Equals("PASS", StringComparison.OrdinalIgnoreCase));
            var humanPassed = humanPassCount * 2 >= runReviews.Count;  // 多数决

            pairs.Add(new JudgeHumanPair
            {
                JudgePassed = judgePassed.Value,
                HumanPassed = humanPassed,
                EvalRunId = run.F_Id,
            });
        }

        if (pairs.Count < minSamples)
            return InsufficientReport(pairs.Count,
                $"样本不足（{pairs.Count}<{minSamples}），L4 降级为 advisory");

        // 4. 计算 Cohen's kappa（二元）
        var kappa = ComputeCohenKappa(pairs);
        var agreeCount = pairs.Count(p => p.JudgePassed == p.HumanPassed);
        var disagreeCount = pairs.Count - agreeCount;

        var trusted = kappa >= KappaTrustedThreshold;
        var report = new JudgeCalibrationReport
        {
            Status = trusted ? "trusted" : "untrusted",
            Kappa = Math.Round(kappa, 3),
            SampleCount = pairs.Count,
            AgreeCount = agreeCount,
            DisagreeCount = disagreeCount,
            RecommendAction = trusted
                ? $"kappa={kappa:F3} ≥ {KappaTrustedThreshold}，可继续 L4 gating"
                : $"kappa={kappa:F3} < {KappaTrustedThreshold}，需人工 re-calibrate Judge prompt",
            CalibratedAt = DateTime.UtcNow,
        };

        _logger.LogInformation(
            "Judge 校准完成 tenant={TenantId} kappa={Kappa} samples={Samples} status={Status}",
            tenantId, report.Kappa, report.SampleCount, report.Status);

        return report;
    }

    /// <summary>从 F_LayerResults JSON 解析 L4 Judge verdict（l4.passed）。</summary>
    private static bool? TryParseJudgeVerdict(string? layerResultsJson)
    {
        if (string.IsNullOrWhiteSpace(layerResultsJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(layerResultsJson);
            if (doc.RootElement.TryGetProperty("l4", out var l4) && l4.ValueKind == JsonValueKind.Object)
            {
                if (l4.TryGetProperty("passed", out var passedEl)
                    && (passedEl.ValueKind == JsonValueKind.True || passedEl.ValueKind == JsonValueKind.False))
                    return passedEl.GetBoolean();
            }
        }
        catch { /* 忽略解析失败 */ }
        return null;
    }

    /// <summary>
    /// 二元 Cohen's kappa（pass/fail）。
    /// 公式：kappa = (po - pe) / (1 - pe)，po=观察一致率，pe=期望一致率。
    /// kappa=1 完全一致，kappa=0 等于随机，kappa<0 低于随机。
    /// </summary>
    internal static double ComputeCohenKappa(List<JudgeHumanPair> pairs)
    {
        var n = pairs.Count;
        if (n == 0) return 0;

        // 混淆矩阵（二元）
        var a = pairs.Count(x => x.JudgePassed && x.HumanPassed);    // 都 PASS
        var d = pairs.Count(x => !x.JudgePassed && !x.HumanPassed);  // 都 FAIL
        var b = pairs.Count(x => x.JudgePassed && !x.HumanPassed);   // Judge PASS / Human FAIL
        var c = pairs.Count(x => !x.JudgePassed && x.HumanPassed);   // Judge FAIL / Human PASS

        var po = (double)(a + d) / n;  // 观察一致率
        var pe = ((double)(a + b) * (a + c) + (double)(c + d) * (b + d)) / ((double)n * n);  // 期望一致率

        if (Math.Abs(pe - 1.0) < double.Epsilon) return 1.0;  // 全一致边界
        return (po - pe) / (1.0 - pe);
    }

    private static JudgeCalibrationReport InsufficientReport(int count, string message) => new()
    {
        Status = "insufficient_samples",
        Kappa = null,
        SampleCount = count,
        AgreeCount = 0,
        DisagreeCount = 0,
        RecommendAction = message,
        CalibratedAt = DateTime.UtcNow,
    };
}
