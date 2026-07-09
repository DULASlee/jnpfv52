using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JNPF.Common.Core.Manager;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.InteAssistant.Llm;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// P7-E04 Skill 质量排行榜 — SQL 聚合 ai_skill_runs 成功率。
///
/// 2026 实践要点：
///   - 质量等级复用 TokenBudgetTierService 分级（A=green/B=yellow/C=red/D=fuse）
///   - 三元组 R12 隔离：仅返回当前租户的数据（六条生命线#5）
///   - 展示在 IR 观测台新 Tab「Skill 质量」【前端 P7-F01】
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "SkillQuality", Order = 203)]
[Route("api/studio/skill-quality")]
public class SkillQualityBoardService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly IUserManager _userManager;

    public SkillQualityBoardService(ISqlSugarClient db, IUserManager userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private string TenantId() => _userManager.TenantId ?? string.Empty;

    /// <summary>
    /// GET /api/studio/skills/quality-board?sinceDays=30
    /// 单条 SQL 聚合 + tier 分级 + 三元组过滤。
    /// </summary>
    [HttpGet("board")]
    public async Task<object> GetBoard([FromQuery] int sinceDays = 30, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-sinceDays);
        var tenantId = TenantId();

        // 单条 SQL 聚合（三元组过滤 — 六条生命线#5 隔离）
        var rows = await _db.Queryable<Entitys.Entity.AiSkillRunEntity>()
            .Where(x => x.TenantId == tenantId && x.StartedAt >= since)
            // 仅统计已结束的 run（running 状态不计入成功率分母，避免误判）
            .Where(x => x.Status == "completed" || x.Status == "failed" || x.Status == "aborted" || x.Status == "cancelled")
            .GroupBy(x => x.SkillId)
            .Select(x => new
            {
                SkillId = x.SkillId,
                Total = SqlFunc.AggregateCount(x.Id),
                SuccessCount = SqlFunc.AggregateSum(SqlFunc.IIF(x.Status == "completed", 1, 0)),
                FailCount = SqlFunc.AggregateSum(SqlFunc.IIF(x.Status != "completed", 1, 0)),
                AvgTokens = SqlFunc.AggregateAvg(x.TokenConsumed),
                LastRunAt = SqlFunc.AggregateMax(x.StartedAt),
            })
            .ToListAsync(ct);

        // 计算衍生指标（在内存，避免复杂 SQL）
        var board = rows
            .OrderByDescending(r => (double)r.SuccessCount / System.Math.Max(1, r.Total))
            .Select(r =>
            {
                var successRate = r.Total > 0 ? (double)r.SuccessCount / r.Total : 0;
                return new QualityBoardItem
                {
                    SkillId = r.SkillId,
                    TotalRuns = r.Total,
                    SuccessCount = r.SuccessCount,
                    FailCount = r.FailCount,
                    SuccessRate = System.Math.Round(successRate, 3),
                    AvgTokens = r.AvgTokens,
                    LastRunAt = r.LastRunAt,
                    // 质量等级：复用 TokenBudgetTierService 的分级思路（A/B/C/D ↔ green/yellow/red/fuse）
                    Grade = ClassifyGrade(successRate),
                };
            })
            .ToList();

        return new QualityBoardResult
        {
            Items = board,
            SinceDays = sinceDays,
            TotalSkills = board.Count,
            // 整体健康度（所有 Skill 的加权平均成功率）
            OverallSuccessRate = board.Count > 0
                ? System.Math.Round((double)board.Sum(b => b.SuccessCount) / System.Math.Max(1, board.Sum(b => b.TotalRuns)), 3)
                : 0,
        };
    }

    /// <summary>
    /// 质量等级：复用 TokenBudgetTierService 阈值映射（A/B/C/D ↔ green/yellow/red/fuse）。
    /// A(≥0.95 绿) / B(≥0.80 黄) / C(≥0.60 红) / D(<0.60 fuse)
    /// </summary>
    private static string ClassifyGrade(double successRate) => successRate switch
    {
        >= 0.95 => "A",   // green — 健康
        >= 0.80 => "B",   // yellow — 关注
        >= 0.60 => "C",   // red — 需改进
        _ => "D",         // fuse — 严重问题
    };
}

/// <summary>质量榜单项</summary>
public class QualityBoardItem
{
    public string SkillId { get; set; } = string.Empty;
    public int TotalRuns { get; set; }
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public double SuccessRate { get; set; }
    public long AvgTokens { get; set; }
    public DateTime LastRunAt { get; set; }
    /// <summary>质量等级 A/B/C/D（↔ green/yellow/red/fuse）</summary>
    public string Grade { get; set; } = "D";
}

/// <summary>质量榜结果</summary>
public class QualityBoardResult
{
    public List<QualityBoardItem> Items { get; set; } = new();
    public int SinceDays { get; set; }
    public int TotalSkills { get; set; }
    public double OverallSuccessRate { get; set; }
}
