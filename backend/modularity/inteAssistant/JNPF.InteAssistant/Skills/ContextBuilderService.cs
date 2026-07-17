using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Entity;
using SqlSugar;

namespace JNPF.InteAssistant.Skills;

public interface IContextBuilderService
{
    PromptContext Build(SkillInformationNeeds needs, IrSnapshot snapshot, IReadOnlyList<SeedTemplateMatch> seeds);
    Task<IReadOnlyList<SeedTemplateMatch>> FindSeedMatchesAsync(string userRequirement, CancellationToken ct = default);
}

public sealed class ContextBuilderService : IContextBuilderService, ITransient
{
    private const int TokenBudgetChars = 8000 * 4;
    private readonly IDomainSeedService _seedService;

    public ContextBuilderService(IDomainSeedService seedService) => _seedService = seedService;

    public PromptContext Build(SkillInformationNeeds needs, IrSnapshot snapshot, IReadOnlyList<SeedTemplateMatch> seeds)
    {
        var fragments = snapshot.Fragments
            .Where(f => needs.IrFragmentTypes.Count == 0
                || needs.IrFragmentTypes.Contains(f.FragmentType, StringComparer.Ordinal))
            .Where(f => IrSnapshot.StabilityRank(f.StabilityState) >= IrSnapshot.StabilityRank(needs.RequiredStability))
            .ToList();

        var totalChars = fragments.Sum(f => f.Payload.Length);
        var summary = string.Empty;
        if (totalChars > TokenBudgetChars)
        {
            fragments = fragments.Take(Math.Max(1, fragments.Count / 2)).ToList();
            summary = "[Context compressed due to token budget]";
        }

        return new PromptContext
        {
            IrFragments = fragments,
            SeedData = seeds.Take(20).ToList(),
            CompressedSummary = summary,
        };
    }

    public async Task<IReadOnlyList<SeedTemplateMatch>> FindSeedMatchesAsync(string userRequirement, CancellationToken ct = default)
        => await _seedService.MatchAsync(userRequirement, ct);
}

public interface IDomainSeedService
{
    Task<IReadOnlyList<SeedTemplateMatch>> MatchAsync(string keyword, CancellationToken ct = default);
    Task<int> EnsureSeedDataAsync(CancellationToken ct = default);
    decimal ScoreCandidate(string candidateJson, IReadOnlyList<SeedTemplateMatch> seeds);
}

public sealed class DomainSeedService : IDomainSeedService, ITransient
{
    private readonly ISqlSugarClient _db;

    public DomainSeedService(ISqlSugarClient db) => _db = db;

    public async Task<IReadOnlyList<SeedTemplateMatch>> MatchAsync(string keyword, CancellationToken ct = default)
    {
        await EnsureSeedDataAsync(ct);
        var templates = await _db.Queryable<AiSeedTemplateEntity>()
            .Where(x => !x.DeleteMark)
            .ToListAsync(ct);

        if (string.IsNullOrWhiteSpace(keyword))
            return Array.Empty<SeedTemplateMatch>();

        // CR-20260717-01 §5.2: 关联筛选 — 避免短行业标签（如 "oa"/"hr"）误匹配
        // 规则：① 事件名（中文短语，如"请假"/"工单"）做双向 Contains；
        //       ② 行业标签仅当 keyword 显式包含完整行业词（加边界保护）时才匹配，防止 "load" 命中 "oa" 等。
        var trimmedKeyword = keyword.Trim();
        return templates
            .Where(t =>
            {
                // 事件名匹配（主要关联信号）
                if (trimmedKeyword.Contains(t.EventNamePattern, StringComparison.OrdinalIgnoreCase)
                    || t.EventNamePattern.Contains(trimmedKeyword, StringComparison.OrdinalIgnoreCase))
                    return true;

                // 行业匹配（辅助信号）— 仅当行业标签 ≥3 字符时才做 Contains，避免 2 字符标签误匹配
                if (t.Industry is { Length: >= 3 } industry
                    && trimmedKeyword.Contains(industry, StringComparison.OrdinalIgnoreCase))
                    return true;

                return false;
            })
            .Take(20)
            .Select(Map)
            .ToList();
    }

    public async Task<int> EnsureSeedDataAsync(CancellationToken ct = default)
    {
        var count = await _db.Queryable<AiSeedTemplateEntity>().CountAsync(ct);
        if (count >= 40) return count;

        var seeds = BuildDefaultSeeds();
        foreach (var seed in seeds)
        {
            var exists = await _db.Queryable<AiSeedTemplateEntity>()
                .AnyAsync(x => x.TemplateId == seed.TemplateId, ct);
            if (exists) continue;
            await _db.Insertable(seed).ExecuteCommandAsync(ct);
        }

        return await _db.Queryable<AiSeedTemplateEntity>().CountAsync(ct);
    }

    public decimal ScoreCandidate(string candidateJson, IReadOnlyList<SeedTemplateMatch> seeds)
    {
        if (seeds.Count == 0) return 0.5m;
        var score = 0m;
        foreach (var seed in seeds)
        {
            if (candidateJson.Contains(seed.EventNamePattern, StringComparison.OrdinalIgnoreCase))
                score += seed.CoverageScore;
        }

        return Math.Min(1m, score / Math.Max(1, seeds.Count));
    }

    private static SeedTemplateMatch Map(AiSeedTemplateEntity e) => new()
    {
        TemplateId = e.TemplateId,
        Industry = e.Industry,
        EventNamePattern = e.EventNamePattern,
        ComplexityHint = e.ComplexityHint,
        CoverageScore = e.CoverageScore,
        TemplateJson = e.TemplateJson,
    };

    private static List<AiSeedTemplateEntity> BuildDefaultSeeds()
    {
        var industries = new[] { "hr", "oa", "manufacturing", "engineering" };
        var events = new[]
        {
            ("LeaveRequest", "请假", "auto"),
            ("ExpenseClaim", "报销", "simple"),
            ("PurchaseOrder", "采购", "simple"),
            ("WorkOrder", "工单", "complex"),
            ("QualityInspection", "质检", "simple"),
            ("EmployeeOnboard", "入职", "auto"),
            ("AttendanceRecord", "考勤", "auto"),
            ("ProjectApproval", "立项", "complex"),
            ("ContractReview", "合同", "complex"),
            ("AssetTransfer", "资产", "simple"),
        };

        var list = new List<AiSeedTemplateEntity>();
        var idx = 0;
        foreach (var industry in industries)
        {
            foreach (var (eventId, name, hint) in events)
            {
                idx++;
                list.Add(new AiSeedTemplateEntity
                {
                    Id = Guid.NewGuid().ToString("N"),
                    TemplateId = $"seed-{industry}-{eventId}".ToLowerInvariant(),
                    Industry = industry,
                    EventNamePattern = name,
                    ComplexityHint = hint,
                    CoverageScore = hint == "auto" ? 0.90m : 0.80m,
                    TemplateJson = $$"""{"eventId":"BE-{{idx:D3}}","eventName":"{{name}}","complexityHint":"{{hint}}"}""",
                });
            }
        }

        return list;
    }
}
