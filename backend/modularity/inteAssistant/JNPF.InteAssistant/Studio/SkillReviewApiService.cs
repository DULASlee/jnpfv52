using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JNPF.Common.Core.Manager;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Skills.Cognitive;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// P7-E03 人工抽检 API（HITL 评分写入）。
///
/// 2026 实践要点：
///   - 「两位专家独立达成一致」原则：同一 run 支持多人独立评分（计算 inter-rater agreement）
///   - 复用 IExperienceRecorder.RecordReviewAsync 写 SkillReviewRecorded IR 事件（审计回放）
///   - 结构化 score/verdict/comment/reviewer 写 BASE_AI_SKILL_REVIEW（供 Judge Cohen's kappa 校准 join）
///   - 三元组 R12 隔离：review 不可跨租户
///   - 评分二元口径与 Judge 对齐（Score≥60 → PASS）
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "SkillReview", Order = 202)]
[Route("api/studio/skills/review")]
public class SkillReviewApiService : IDynamicApiController, ITransient
{
    private const int PassThreshold = 60;  // 与 Judge 对齐：Score≥60 → PASS

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ISqlSugarClient _db;
    private readonly IUserManager _userManager;
    private readonly IExperienceRecorder _experience;

    public SkillReviewApiService(
        ISqlSugarClient db,
        IUserManager userManager,
        IExperienceRecorder experience)
    {
        _db = db;
        _userManager = userManager;
        _experience = experience;
    }

    private long NewId() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    private long? UserId() => long.TryParse(_userManager.UserId, out var id) ? id : null;
    private string TenantId() => _userManager.TenantId ?? string.Empty;

    /// <summary>
    /// POST /api/studio/skills/review — 提交人工抽检评分。
    /// 写入 BASE_AI_SKILL_REVIEW（结构化）+ SkillReviewRecorded IR 事件（审计回放）。
    /// </summary>
    [HttpPost]
    public async Task<object> SubmitReview([FromBody] SkillReviewInput input, CancellationToken ct)
    {
        var tenantId = !string.IsNullOrEmpty(input.TenantId) ? input.TenantId : TenantId();

        // 1. 校验 skill_run 存在且属于当前租户（三元组 R12 — 六条生命线#5 隔离）
        var run = await _db.Queryable<AiSkillRunEntity>()
            .Where(x => x.Id == input.SkillRunId && x.TenantId == tenantId)
            .Select(x => new { x.Id, x.SkillId, x.ProjectId, x.PipelineId })
            .FirstAsync(ct) ?? throw Oops.Bah("skill_run 不存在或跨租户");

        // 2. 评分范围校验（0-100）
        if (input.Score < 0 || input.Score > 100)
            throw Oops.Bah("评分必须在 0-100 之间");

        // 3. 二元口径（与 Judge 对齐）：Score≥60 → PASS
        var verdict = input.Score >= PassThreshold ? "PASS" : "FAIL";

        // 4. 写入 BASE_AI_SKILL_REVIEW（结构化，三元组从 run 回填）
        var review = new SkillReviewEntity
        {
            F_Id = NewId(),
            F_SkillRunId = input.SkillRunId,
            F_EvalRunId = input.EvalRunId,
            F_SkillId = run.SkillId,
            F_Score = input.Score,
            F_Verdict = verdict,
            F_Comment = input.Comment,
            F_ReviewerId = UserId(),
            F_ReviewerName = !string.IsNullOrEmpty(_userManager.RealName)
                ? _userManager.RealName
                : _userManager.Account,
            F_TenantId = tenantId,
            F_ProjectId = !string.IsNullOrEmpty(input.ProjectId) ? input.ProjectId : run.ProjectId,
            F_PipelineId = !string.IsNullOrEmpty(input.PipelineId) ? input.PipelineId : run.PipelineId,
            F_CreatorTime = DateTime.Now,
        };
        await _db.Insertable(review).ExecuteCommandAsync(ct);

        // 5. 写 SkillReviewRecorded IR 事件（审计回放，复用经验回流机制）
        //    verdict + 结构化 detail，供 IR 观测台回放
        await _experience.RecordReviewAsync(
            projectId: review.F_ProjectId,
            tenantId: tenantId,
            skillId: run.SkillId,
            runId: input.SkillRunId,
            verdict: verdict,
            detailJson: JsonSerializer.Serialize(new
            {
                reviewId = review.F_Id,
                score = input.Score,
                comment = input.Comment,
                reviewerId = review.F_ReviewerId,
                reviewerName = review.F_ReviewerName,
                evalRunId = input.EvalRunId,
            }, JsonOptions), ct);

        return new
        {
            ok = true,
            reviewId = review.F_Id,
            skillRunId = input.SkillRunId,
            score = input.Score,
            verdict,
        };
    }

    /// <summary>
    /// GET /api/studio/skills/review/{skillRunId} — 查看 run 的所有 review。
    /// 支持多人独立评分（校准 inter-rater agreement）。
    /// 三元组 R12：仅返回当前租户的 review。
    /// </summary>
    [HttpGet("{skillRunId}")]
    public async Task<object> GetReviews(string skillRunId, CancellationToken ct)
    {
        var tenantId = TenantId();
        var reviews = await _db.Queryable<SkillReviewEntity>()
            .Where(x => x.F_SkillRunId == skillRunId && x.F_TenantId == tenantId)
            .OrderByDescending(x => x.F_CreatorTime)
            .ToListAsync(ct);

        // 计算 inter-rater 统计（多人评分一致性）
        var stats = ComputeInterRaterStats(reviews);

        return new
        {
            items = reviews,
            total = reviews.Count,
            stats,
        };
    }

    /// <summary>
    /// GET /api/studio/skills/review/by-eval/{evalRunId} — 按 eval run 查 review（Judge 校准 join 用）。
    /// </summary>
    [HttpGet("by-eval/{evalRunId:long}")]
    public async Task<object> GetReviewsByEvalRun(long evalRunId, CancellationToken ct)
    {
        var tenantId = TenantId();
        var reviews = await _db.Queryable<SkillReviewEntity>()
            .Where(x => x.F_EvalRunId == evalRunId && x.F_TenantId == tenantId)
            .OrderByDescending(x => x.F_CreatorTime)
            .ToListAsync(ct);

        return new { items = reviews, total = reviews.Count };
    }

    /// <summary>
    /// 计算 inter-rater 统计（多人评分一致性）。
    /// 方差过大 → 争议样本 → 进入 Judge re-calibration 候选池。
    /// </summary>
    private static InterRaterStats ComputeInterRaterStats(List<SkillReviewEntity> reviews)
    {
        if (reviews.Count == 0)
            return new InterRaterStats();

        var scores = reviews.Select(r => (double)r.F_Score).ToList();
        var mean = scores.Average();
        var variance = scores.Count > 1
            ? scores.Sum(s => (s - mean) * (s - mean)) / scores.Count
            : 0;
        var stdDev = System.Math.Sqrt(variance);

        var passCount = reviews.Count(r => r.F_Verdict == "PASS");
        var failCount = reviews.Count - passCount;
        // 多数决 verdict（平票时为 DISPUTED）
        var majorityVerdict = passCount > failCount ? "PASS"
            : failCount > passCount ? "FAIL"
            : "DISPUTED";

        return new InterRaterStats
        {
            ReviewerCount = reviews.Select(r => r.F_ReviewerId).Distinct().Count(),
            MeanScore = System.Math.Round(mean, 1),
            StdDev = System.Math.Round(stdDev, 2),
            PassCount = passCount,
            FailCount = failCount,
            MajorityVerdict = majorityVerdict,
            // 标准差 >15 视为争议（评分严重分歧）
            IsDisputed = stdDev > 15,
        };
    }
}

/// <summary>人工抽检评分输入</summary>
public class SkillReviewInput
{
    /// <summary>被评审的 skill_run id（ai_skill_runs.F_Id, string/GUID）</summary>
    public string SkillRunId { get; set; } = string.Empty;

    /// <summary>评分 0-100（≥60 → PASS，与 Judge 二元口径对齐）</summary>
    public int Score { get; set; }

    /// <summary>评审意见</summary>
    public string? Comment { get; set; }

    /// <summary>关联的 eval run（可选，Judge 校准 join 用）</summary>
    public long? EvalRunId { get; set; }

    // 三元组 R12（可选，默认从 skill_run 回填）
    public string? TenantId { get; set; }
    public string? ProjectId { get; set; }
    public string? PipelineId { get; set; }
}

/// <summary>多人评分一致性统计（inter-rater agreement）</summary>
public class InterRaterStats
{
    public int ReviewerCount { get; set; }
    public double MeanScore { get; set; }
    public double StdDev { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    /// <summary>多数决 verdict（PASS/FAIL/DISPUTED）</summary>
    public string MajorityVerdict { get; set; } = "PASS";
    /// <summary>标准差>15 → 争议样本（评分严重分歧）</summary>
    public bool IsDisputed { get; set; }
}
