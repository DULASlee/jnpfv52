using System.Text;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Studio;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Skills;

public sealed record RequirementEvolutionSeed(long CaseId, string Tag, string Summary);

public interface IRequirementEvolutionContext
{
    Task<IReadOnlyList<RequirementEvolutionSeed>> RetrieveSeedsAsync(
        string tenantId,
        string projectId,
        long pipelineId,
        string text,
        int topK = 3,
        CancellationToken ct = default);
}

public sealed class RequirementEvolutionContext : IRequirementEvolutionContext, ITransient
{
    private const int CandidateLimit = 80;

    private readonly ISqlSugarClient _db;
    private readonly ILogger<RequirementEvolutionContext> _logger;

    public RequirementEvolutionContext(ISqlSugarClient db, ILogger<RequirementEvolutionContext> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RequirementEvolutionSeed>> RetrieveSeedsAsync(
        string tenantId,
        string projectId,
        long pipelineId,
        string text,
        int topK = 3,
        CancellationToken ct = default)
    {
        var terms = ExtractTerms(text);
        var setIds = await _db.Queryable<EvalGoldenSetEntity>()
            .Where(x => x.F_Domain == "auto_seed"
                        && x.F_Description.Contains(TenantMarker(tenantId))
                        && x.F_Enabled
                        && x.F_DeleteMark == null)
            .Select(x => x.F_Id)
            .ToListAsync(ct);
        if (setIds.Count == 0)
            return Array.Empty<RequirementEvolutionSeed>();

        var candidates = await _db.Queryable<EvalCaseEntity>()
            .Where(x => setIds.Contains(x.F_SetId)
                        && x.F_DeleteMark == null
                        && x.F_Enabled
                        && x.F_Requirement.Contains("[auto_seed:req="))
            .OrderByDescending(x => x.F_CreatorTime)
            .Take(CandidateLimit)
            .Select(x => new { x.F_Id, x.F_Requirement })
            .ToListAsync(ct);

        var ranked = candidates
            .Select(c => new
            {
                c.F_Id,
                Requirement = c.F_Requirement,
                Tag = ExtractMarker(c.F_Requirement, "tag="),
                Score = Score(c.F_Requirement, terms, tenantId, projectId, pipelineId),
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.F_Id)
            .Take(Math.Max(0, topK))
            .Select(x => new RequirementEvolutionSeed(x.F_Id, x.Tag, Truncate(x.Requirement, 240)))
            .ToList();

        if (ranked.Count > 0)
        {
            _logger.LogInformation(
                "需求进化上下文命中 seeds tenant={TenantId} project={ProjectId} pipeline={PipelineId} ids={SeedIds}",
                tenantId, projectId, pipelineId, string.Join(",", ranked.Select(x => x.CaseId)));
        }

        return ranked;
    }

    public static string RenderPromptBlock(IReadOnlyList<RequirementEvolutionSeed> seeds)
    {
        if (seeds.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("历史 auto_seed 经验（只作检查清单，不可整段复制旧项目）：");
        foreach (var seed in seeds)
            sb.AppendLine($"- seedId={seed.CaseId}; tag={seed.Tag}; {SanitizeSeedSummary(seed.Summary)}");
        return sb.ToString();
    }

    private static int Score(string requirement, IReadOnlyList<string> terms, string tenantId, string projectId, long pipelineId)
    {
        var score = 0;
        if (requirement.Contains($"tenant={tenantId}", StringComparison.OrdinalIgnoreCase)) score += 2;
        if (requirement.Contains($"project={projectId}", StringComparison.OrdinalIgnoreCase)) score += 1;
        if (requirement.Contains($"pipeline={pipelineId}", StringComparison.OrdinalIgnoreCase)) score -= 2;
        foreach (var term in terms)
        {
            if (requirement.Contains(term, StringComparison.OrdinalIgnoreCase))
                score++;
        }
        return score;
    }

    private static IReadOnlyList<string> ExtractTerms(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        var normalized = text.Replace('\r', ' ').Replace('\n', ' ');
        var terms = new[] { "请假", "审批", "流程", "员工", "部门", "假期", "驳回", "撤回", "通知", "代提" }
            .Where(t => normalized.Contains(t, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (terms.Count == 0)
            terms.AddRange(normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => x.Length >= 2)
                .Take(8));

        return terms.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string ExtractMarker(string requirement, string marker)
    {
        var idx = requirement.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0)
            return string.Empty;
        var start = idx + marker.Length;
        var end = requirement.IndexOfAny(new[] { ']', ';', ' ' }, start);
        return end > start ? requirement[start..end] : requirement[start..];
    }

    private static string Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s[..max] + "...");

    private static string TenantMarker(string tenantId) => $"[tenant:{tenantId}]";

    private static string SanitizeSeedSummary(string summary)
    {
        var blocked = new[] { "忽略", "ignore", "system prompt", "开发者指令", "输出以下", "不要遵守" };
        var cleaned = summary.Replace("\r", " ").Replace("\n", " ");
        foreach (var word in blocked)
            cleaned = cleaned.Replace(word, "[redacted]", StringComparison.OrdinalIgnoreCase);
        return Truncate(cleaned, 240);
    }
}
