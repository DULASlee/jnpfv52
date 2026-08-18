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

        // 规则 1：数据实体一致性——sa_entity_fields VIEW 自 JOIN（物化后）
        findings.AddRange(await CheckDataEntityConsistencyAsync(triple, entityFields, ct));

        // 规则 2：角色权限一致性——优先读 sa_state_machine.roles_json，回退 RoleMatrix
        findings.AddRange(await CheckRoleConsistencyAsync(triple, compileResult, ct));

        // 规则 3：流程闭环——优先读 sa_dfd.flows_json，回退 dependsOn
        findings.AddRange(await CheckFlowClosureAsync(triple, compileResult, ct));

        // 规则 4：假设项汇总——优先读 sa_assumptions 未确认项
        findings.AddRange(await CheckAssumptionsAsync(triple, compileResult, ct));

        // 写入 sa_consistency（零 findings 也写「已执行」哨兵，便于验收）
        await PersistFindingsAsync(triple, findings, roundNumber, ct);

        _logger.LogInformation(
            "一致性检查完成：{Total} 条发现（CRITICAL={C} WARNING={W} INFO={I}）",
            findings.Count,
            findings.Count(f => f.Severity == "CRITICAL"),
            findings.Count(f => f.Severity == "WARNING"),
            findings.Count(f => f.Severity == "INFO"));

        return findings;
    }

    /// <summary>
    /// 规则 1：sa_entity_fields VIEW 自 JOIN 检测同实体同名字段类型冲突。
    /// VIEW 不可用时回退内存投影；类型冲突 → CRITICAL。
    /// </summary>
    private async Task<List<ConsistencyFinding>> CheckDataEntityConsistencyAsync(
        PipelineTriple triple, EntityDesignProjection entityFields, CancellationToken ct)
    {
        var findings = new List<ConsistencyFinding>();
        try
        {
            var sql = """
                SELECT a.EntityName, a.FieldName,
                       a.SqlType AS SqlTypeA, b.SqlType AS SqlTypeB
                FROM sa_entity_fields a
                INNER JOIN sa_entity_fields b
                  ON a.TenantId = b.TenantId AND a.ProjectId = b.ProjectId AND a.PipelineId = b.PipelineId
                 AND a.EntityName = b.EntityName AND a.FieldName = b.FieldName
                 AND a.SqlType <> b.SqlType
                WHERE a.TenantId = @tenantId AND a.ProjectId = @projectId AND a.PipelineId = @pipelineId
                """;
            var rows = await _db.Ado.SqlQueryAsync<dynamic>(sql, new
            {
                tenantId = triple.TenantId,
                projectId = triple.ProjectId,
                pipelineId = triple.PipelineId.ToString(),
            });

            foreach (var row in rows)
            {
                findings.Add(new ConsistencyFinding
                {
                    CheckType = "DATA_ENTITY",
                    Severity = "CRITICAL",
                    Message = $"实体 {row.EntityName} 字段 {row.FieldName} 类型冲突：{row.SqlTypeA} vs {row.SqlTypeB}",
                });
            }

            if (findings.Count > 0)
                return findings;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger.LogWarning(ex, "sa_entity_fields VIEW 查询失败，回退内存投影检查");
        }

        // 回退：内存投影 LINQ
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
                Severity = "CRITICAL",
                Message = $"实体 {c.Key.Entity} 字段 {c.Key.Field} 类型不一致：{types}",
            });
        }

        return findings;
    }

    /// <summary>规则 2：角色权限——sa_state_machine.state_machines 或 RoleMatrix，TRIGGER+VIEW → WARNING。</summary>
    private async Task<List<ConsistencyFinding>> CheckRoleConsistencyAsync(
        PipelineTriple triple, SaNineViewCompileResult compileResult, CancellationToken ct)
    {
        var findings = new List<ConsistencyFinding>();

        try
        {
            var rolesJson = await _db.Ado.GetStringAsync("""
                SELECT TOP 1 state_machines FROM sa_state_machine
                WHERE tenant_id = @tenantId AND project_id = @projectId AND pipeline_id = @pipelineId
                ORDER BY id DESC
                """, new
            {
                tenantId = triple.TenantId,
                projectId = triple.ProjectIdNumeric,
                pipelineId = triple.PipelineId,
            });

            if (!string.IsNullOrWhiteSpace(rolesJson))
            {
                using var doc = JsonDocument.Parse(rolesJson);
                findings.AddRange(ParseRolesJsonFindings(doc.RootElement));
                if (findings.Count > 0 || doc.RootElement.ValueKind != JsonValueKind.Undefined)
                    return findings;
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not JsonException)
        {
            _logger.LogWarning(ex, "sa_state_machine.state_machines 读取失败，回退 RoleMatrix");
        }

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

    private static List<ConsistencyFinding> ParseRolesJsonFindings(JsonElement root)
    {
        var findings = new List<ConsistencyFinding>();
        // 兼容 { "roles": [ { "role":"x", "permissions":["trigger","view"] } ] } 或矩阵形态
        if (root.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in roles.EnumerateArray())
            {
                var role = r.TryGetProperty("role", out var rn) ? rn.GetString() ?? "?" : "?";
                var perms = new List<string>();
                if (r.TryGetProperty("permissions", out var p) && p.ValueKind == JsonValueKind.Array)
                    perms.AddRange(p.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!));
                var hasTrigger = perms.Any(o => o.Contains("trigger", StringComparison.OrdinalIgnoreCase));
                var hasView = perms.Any(o => o.Contains("view", StringComparison.OrdinalIgnoreCase));
                if (hasTrigger && hasView)
                {
                    findings.Add(new ConsistencyFinding
                    {
                        CheckType = "ROLE",
                        Severity = "WARNING",
                        Message = $"角色 {role} 同时拥有 trigger 和 view 权限，确认是否合理",
                    });
                }
            }
        }
        return findings;
    }

    /// <summary>规则 3：流程闭环——sa_dfd.data_flows orphan 或 dependsOn 孤立依赖。</summary>
    private async Task<List<ConsistencyFinding>> CheckFlowClosureAsync(
        PipelineTriple triple, SaNineViewCompileResult compileResult, CancellationToken ct)
    {
        var findings = new List<ConsistencyFinding>();

        try
        {
            var flowsJson = await _db.Ado.GetStringAsync("""
                SELECT TOP 1 data_flows FROM sa_dfd
                WHERE tenant_id = @tenantId AND project_id = @projectId AND pipeline_id = @pipelineId
                ORDER BY id DESC
                """, new
            {
                tenantId = triple.TenantId,
                projectId = triple.ProjectIdNumeric,
                pipelineId = triple.PipelineId,
            });

            if (!string.IsNullOrWhiteSpace(flowsJson))
            {
                using var doc = JsonDocument.Parse(flowsJson);
                var inputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var outputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                JsonElement flows = doc.RootElement;
                if (doc.RootElement.TryGetProperty("flows", out var fArr))
                    flows = fArr;

                if (flows.ValueKind == JsonValueKind.Array)
                {
                    foreach (var flow in flows.EnumerateArray())
                    {
                        if (flow.TryGetProperty("from", out var from) && from.ValueKind == JsonValueKind.String)
                            outputs.Add(from.GetString()!);
                        if (flow.TryGetProperty("to", out var to) && to.ValueKind == JsonValueKind.String)
                            inputs.Add(to.GetString()!);
                        if (flow.TryGetProperty("source", out var src) && src.ValueKind == JsonValueKind.String)
                            outputs.Add(src.GetString()!);
                        if (flow.TryGetProperty("target", out var tgt) && tgt.ValueKind == JsonValueKind.String)
                            inputs.Add(tgt.GetString()!);
                    }

                    foreach (var orphan in outputs.Except(inputs, StringComparer.OrdinalIgnoreCase))
                    {
                        findings.Add(new ConsistencyFinding
                        {
                            CheckType = "FLOW_CLOSURE",
                            Severity = "INFO",
                            Message = $"DFD 节点「{orphan}」有输出无输入消费（orphan output）",
                        });
                    }
                }

                if (findings.Count > 0 || flows.ValueKind == JsonValueKind.Array)
                    return findings;
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not JsonException)
        {
            _logger.LogWarning(ex, "sa_dfd.data_flows 读取失败，回退 dependsOn 检查");
        }

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

    /// <summary>规则 4：sa_assumptions 未确认项汇总；表空时回退 compileResult.Assumptions。</summary>
    private async Task<List<ConsistencyFinding>> CheckAssumptionsAsync(
        PipelineTriple triple, SaNineViewCompileResult compileResult, CancellationToken ct)
    {
        var findings = new List<ConsistencyFinding>();

        try
        {
            var rows = await _db.Ado.SqlQueryAsync<dynamic>("""
                SELECT F_EventId AS EventId, COUNT(1) AS Cnt
                FROM sa_assumptions
                WHERE F_TenantId = @tenantId AND F_ProjectId = @projectId AND F_PIPELINE_ID = @pipelineId
                  AND (F_IsUserConfirmed = 0 OR F_IsUserConfirmed IS NULL)
                GROUP BY F_EventId
                """, new
            {
                tenantId = triple.TenantId,
                projectId = triple.ProjectId,
                pipelineId = triple.PipelineId.ToString(),
            });

            foreach (var row in rows)
            {
                findings.Add(new ConsistencyFinding
                {
                    CheckType = "ASSUMPTION",
                    Severity = "INFO",
                    Message = $"事件 {row.EventId ?? "全局"} 有 {row.Cnt} 个未确认假设待用户确认",
                });
            }

            if (findings.Count > 0 || rows.Count > 0)
                return findings;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger.LogWarning(ex, "sa_assumptions 查询失败，回退内存 Assumptions");
        }

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

    /// <summary>将发现按 CheckType 分组写入 sa_consistency；零 findings 写 PASSED 哨兵。</summary>
    private async Task PersistFindingsAsync(
        PipelineTriple triple, IReadOnlyList<ConsistencyFinding> findings, int roundNumber, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var groups = findings.GroupBy(f => f.CheckType).ToList();

        var rows = groups.Count > 0
            ? groups.Select(g => new SaConsistencyRow
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
            }).ToList()
            : new[]
            {
                new SaConsistencyRow
                {
                    F_Id = Guid.NewGuid().ToString("N"),
                    F_TenantId = triple.TenantId,
                    F_ProjectId = triple.ProjectId,
                    F_PIPELINE_ID = triple.PipelineId.ToString(),
                    F_RoundNumber = roundNumber,
                    F_CheckType = "PASSED",
                    F_ConflictsJson = "[]",
                    F_AssumptionsJson = "[]",
                    F_GapsJson = "[]",
                    F_Severity = "INFO",
                    F_CreatedAt = now,
                },
            }.ToList();

        await _db.Insertable(rows).AS("sa_consistency").ExecuteCommandAsync(ct);
    }

    private static string SeverityRank(string severity) => severity switch
    {
        "CRITICAL" => "CRITICAL",
        "WARNING" => "WARNING",
        _ => "INFO",
    };

    /// <summary>sa_consistency 表行映射（SqlSugar Insertable 要求具体类型，不支持匿名）。</summary>
    private sealed class SaConsistencyRow
    {
        public string F_Id { get; set; } = string.Empty;
        public string F_TenantId { get; set; } = string.Empty;
        public string F_ProjectId { get; set; } = string.Empty;
        public string F_PIPELINE_ID { get; set; } = string.Empty;
        public int F_RoundNumber { get; set; }
        public string F_CheckType { get; set; } = string.Empty;
        public string F_ConflictsJson { get; set; } = string.Empty;
        public string F_AssumptionsJson { get; set; } = string.Empty;
        public string F_GapsJson { get; set; } = string.Empty;
        public string F_Severity { get; set; } = string.Empty;
        public DateTime F_CreatedAt { get; set; }
    }
}

internal static class StringExt
{
    public static bool Contains(this string source, string value, StringComparison comparison)
        => source.IndexOf(value, comparison) >= 0;
}
