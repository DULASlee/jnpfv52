using System.Data;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Runtime;
using JNPF.InteAssistant.Sa;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Gates;

/// <summary>
/// 28 号 §5：一致性检查器——LINQ + SQL 混合，4 条规则，Round 3 一次性执行。
/// 产出 sa_consistency 表记录（三元组隔离）。
/// </summary>
public interface IConsistencyChecker
{
    /// <summary>执行 4 条一致性检查，返回报告列表，并写入 sa_consistency 表。</summary>
    Task<IReadOnlyList<ConsistencyFinding>> CheckAsync(
        PipelineTriple triple, SaNineViewCompileResult compileResult,
        EntityDesignProjection entityFields, int roundNumber, CancellationToken ct = default);
}

/// <summary>单条一致性发现。</summary>
public sealed class ConsistencyFinding
{
    public string CheckType { get; init; } = string.Empty;  // DATA_ENTITY / ROLE / FLOW_CLOSURE / ASSUMPTION
    public string Severity { get; init; } = "INFO";          // CRITICAL / WARNING / INFO
    public string Message { get; init; } = string.Empty;
}

public sealed class ConsistencyChecker : IConsistencyChecker, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ISqlSugarClient _db;
    private readonly ILogger<ConsistencyChecker> _logger;

    public ConsistencyChecker(ISqlSugarClient db, ILogger<ConsistencyChecker> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ConsistencyFinding>> CheckAsync(
        PipelineTriple triple, SaNineViewCompileResult compileResult,
        EntityDesignProjection entityFields, int roundNumber, CancellationToken ct = default)
    {
        var findings = new List<ConsistencyFinding>();

        // 规则 1：数据实体一致性——同实体字段类型冲突
        findings.AddRange(CheckDataEntityConsistency(entityFields));

        // 规则 2：角色权限一致性——同一角色 TRIGGER+VIEW 共存
        findings.AddRange(CheckRoleConsistency(compileResult));

        // 规则 3：流程闭环——DFD 流中 output - input = orphan
        findings.AddRange(CheckFlowClosure(compileResult));

        // 规则 4：假设项汇总——未确认假设去重统计
        findings.AddRange(CheckAssumptions(compileResult));

        // 写入 sa_consistency 表（按 CheckType 分组，每组一条记录）
        await PersistFindingsAsync(triple, findings, roundNumber, ct);

        _logger.LogInformation(
            "一致性检查完成：{Total} 条发现（CRITICAL={C} WARNING={W} INFO={I}）",
            findings.Count,
            findings.Count(f => f.Severity == "CRITICAL"),
            findings.Count(f => f.Severity == "WARNING"),
            findings.Count(f => f.Severity == "INFO"));

        return findings;
    }

    /// <summary>规则 1：数据实体一致性（内存 LINQ）——同实体同名字段类型不一致 → WARNING。</summary>
    private static List<ConsistencyFinding> CheckDataEntityConsistency(EntityDesignProjection entityFields)
    {
        var findings = new List<ConsistencyFinding>();

        // 28 号 §5 规则 1 原设计是 sa_entity_fields VIEW 自 JOIN；
        // v2.0 极简：投影已在内存 entityFields.Fields，直接 LINQ（无需 SQL 自 JOIN）。
        var conflicts = entityFields.Fields
            .GroupBy(f => (Entity: f.EntityName.ToLowerInvariant(), Field: f.FieldName.ToLowerInvariant()))
            .Where(g => g.Select(x => x.SqlType).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .ToList();

        foreach (var c in conflicts)
        {
            var types = string.Join(", ", c.Select(x => x.SqlType).Distinct(StringComparer.OrdinalIgnoreCase));
            findings.Add(new ConsistencyFinding
            {
                CheckType = "DATA_ENTITY",
                Severity = "WARNING",
                Message = $"实体 {c.Key.Entity} 字段 {c.Key.Field} 类型不一致：{types}",
            });
        }

        return findings;
    }

    /// <summary>规则 2：角色权限一致性（内存 LINQ）——roleMatrix 中同一角色 TRIGGER+VIEW 共存 → WARNING。</summary>
    private static List<ConsistencyFinding> CheckRoleConsistency(SaNineViewCompileResult compileResult)
    {
        var findings = new List<ConsistencyFinding>();
        var roleMatrix = compileResult.Source.RoleMatrix;
        if (roleMatrix == null) return findings;

        foreach (var (eventId, roleOps) in roleMatrix.Matrix)
        {
            foreach (var (role, ops) in roleOps)
            {
                var hasTrigger = ops.Any(o => o.Contains("trigger", StringComparison.OrdinalIgnoreCase));
                var hasView = ops.Any(o => o.Contains("view", StringComparison.OrdinalIgnoreCase));
                if (hasTrigger && hasView)
                {
                    findings.Add(new ConsistencyFinding
                    {
                        CheckType = "ROLE",
                        Severity = "WARNING",
                        Message = $"事件 {eventId} 角色 {role} 同时拥有 trigger 和 view 权限，确认是否合理",
                    });
                }
            }
        }

        return findings;
    }

    /// <summary>规则 3：流程闭环（内存 LINQ）——dependsOn 引用了不存在的事件 → INFO。</summary>
    private static List<ConsistencyFinding> CheckFlowClosure(SaNineViewCompileResult compileResult)
    {
        var findings = new List<ConsistencyFinding>();
        var knownEvents = compileResult.Source.BusinessEvents
            .Select(e => e.EventId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var evt in compileResult.Source.BusinessEvents)
        {
            if (evt.DependsOn is null) continue;
            foreach (var dep in evt.DependsOn)
            {
                if (!knownEvents.Contains(dep))
                {
                    findings.Add(new ConsistencyFinding
                    {
                        CheckType = "FLOW_CLOSURE",
                        Severity = "INFO",
                        Message = $"事件 {evt.EventId} 依赖 {dep}，但该事件不在清单中（孤立依赖）",
                    });
                }
            }
        }

        return findings;
    }

    /// <summary>规则 4：假设项汇总——未确认假设去重统计 → INFO。</summary>
    private static List<ConsistencyFinding> CheckAssumptions(SaNineViewCompileResult compileResult)
    {
        var findings = new List<ConsistencyFinding>();

        var unconfirmed = compileResult.Assumptions
            .Where(a => a.Confidence < 0.6m)
            .GroupBy(a => a.EventId)
            .ToList();

        foreach (var group in unconfirmed)
        {
            findings.Add(new ConsistencyFinding
            {
                CheckType = "ASSUMPTION",
                Severity = "INFO",
                Message = $"事件 {group.Key} 有 {group.Count()} 个低置信度假设（<0.6）待用户确认",
            });
        }

        return findings;
    }

    /// <summary>将发现按 CheckType 分组写入 sa_consistency 表。</summary>
    private async Task PersistFindingsAsync(
        PipelineTriple triple, IReadOnlyList<ConsistencyFinding> findings, int roundNumber, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var rows = findings
            .GroupBy(f => f.CheckType)
            .Select(g => new
            {
                F_Id = Guid.NewGuid().ToString("N"),
                F_TenantId = triple.TenantId,
                F_ProjectId = triple.ProjectId,
                F_PIPELINE_ID = triple.PipelineId.ToString(),
                F_RoundNumber = roundNumber,
                F_CheckType = g.Key,
                F_ConflictsJson = JsonSerializer.Serialize(
                    g.Where(f => f.Severity == "CRITICAL").Select(f => f.Message).ToList(), JsonOptions),
                F_AssumptionsJson = JsonSerializer.Serialize(
                    g.Where(f => f.Severity == "WARNING").Select(f => f.Message).ToList(), JsonOptions),
                F_GapsJson = JsonSerializer.Serialize(
                    g.Where(f => f.Severity == "INFO").Select(f => f.Message).ToList(), JsonOptions),
                F_Severity = g.Max(f => SeverityRank(f.Severity)),
                F_CreatedAt = now,
            })
            .ToList();

        if (rows.Count == 0) return;

        await _db.Insertable(rows).AS("sa_consistency").ExecuteCommandAsync(ct);
    }

    private static string SeverityRank(string severity) => severity switch
    {
        "CRITICAL" => "CRITICAL",
        "WARNING" => "WARNING",
        _ => "INFO",
    };
}

internal static class StringExt
{
    public static bool Contains(this string source, string value, StringComparison comparison)
        => source.IndexOf(value, comparison) >= 0;
}
