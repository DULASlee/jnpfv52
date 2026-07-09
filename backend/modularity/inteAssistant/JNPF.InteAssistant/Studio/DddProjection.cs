using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Sa;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// 28 号 §3：DDD 实时推导器——从 SA 产出 + ai_entity_field 推导 5 个领域设计视角。
/// 纯 C#，无 LLM，&lt; 50ms，渲染时计算，不写入任何物理表。
/// </summary>
/// <remarks>
/// v2.0 核心：DDD 不再是 5 张表 + 5 个增强器，而是 SA 产出的"投影维度"。
/// 5 个视角全部标记 confidence，渲染到需求分析书 §3。
/// confidence 来源：推导依据的充分程度（有外键→高，无外键靠命名→低）。
/// </remarks>
public interface IDddProjection
{
    /// <summary>从 SA 编译结果 + 实体字段投影推导 5 个 DDD 视角。</summary>
    DddProjectionResult Project(SaNineViewCompileResult compileResult, EntityDesignProjection entityFields);
}

/// <summary>DDD 5 视角推导结果（渲染时用，不落库）。</summary>
public sealed class DddProjectionResult
{
    public DddDomainModel DomainModel { get; init; } = new();
    public DddAggregateDesign AggregateDesign { get; init; } = new();
    public DddEventCatalog EventCatalog { get; init; } = new();
    public DddCqrs Cqrs { get; init; } = new();
    public DddIntegration Integration { get; init; } = new();

    /// <summary>5 视角综合 confidence（0-1），供质量评分器 DDD 维度计算。</summary>
    public double OverallConfidence =>
        (DomainModel.Confidence + AggregateDesign.Confidence + EventCatalog.Confidence
         + Cqrs.Confidence + Integration.Confidence) / 5.0;
}

public abstract class DddViewBase
{
    public double Confidence { get; init; }
}

public sealed class DddDomainModel : DddViewBase
{
    /// <summary>推导的子域（外键密集聚类）。</summary>
    public List<string> SubDomains { get; init; } = new();
    /// <summary>核心域（引用频次最高的实体）。</summary>
    public string? CoreDomain { get; init; }
}

public sealed class DddAggregateDesign : DddViewBase
{
    /// <summary>聚合根实体（PK 端被其他实体引用的）。</summary>
    public List<string> RootEntities { get; init; } = new();
    /// <summary>聚合分组：聚合根 → 同聚合的实体列表。</summary>
    public Dictionary<string, List<string>> Aggregates { get; init; } = new();
}

public sealed class DddEventCatalog : DddViewBase
{
    public List<string> Events { get; init; } = new();
    /// <summary>事件依赖（from dependsOn）。</summary>
    public List<(string From, string To)> Dependencies { get; init; } = new();
}

public sealed class DddCqrs : DddViewBase
{
    public List<string> Commands { get; init; } = new();
    public List<string> Queries { get; init; } = new();
}

public sealed class DddIntegration : DddViewBase
{
    /// <summary>集成点（SYSTEM 类型外部交互 → SYNC_API）。</summary>
    public List<string> IntegrationPoints { get; init; } = new();
}

public sealed class DddProjection : IDddProjection, ITransient
{
    private readonly ILogger<DddProjection> _logger;

    public DddProjection(ILogger<DddProjection> logger) => _logger = logger;

    public DddProjectionResult Project(SaNineViewCompileResult compileResult, EntityDesignProjection entityFields)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var result = new DddProjectionResult
        {
            DomainModel = DeriveDomainModel(compileResult, entityFields),
            AggregateDesign = DeriveAggregateDesign(compileResult, entityFields),
            EventCatalog = DeriveEventCatalog(compileResult),
            Cqrs = DeriveCqrs(compileResult),
            Integration = DeriveIntegration(compileResult),
        };

        sw.Stop();
        _logger.LogInformation(
            "DDD 推导完成：5 视角，confidence={Conf:F2}，耗时 {Ms}ms",
            result.OverallConfidence, sw.ElapsedMilliseconds);

        return result;
    }

    /// <summary>视角 1 领域模型：Union-Find 聚类（外键密集→子域）+ 引用频次→核心域。</summary>
    private static DddDomainModel DeriveDomainModel(SaNineViewCompileResult compileResult, EntityDesignProjection entityFields)
    {
        // 从 entityFields 的 References 构建实体引用关系
        var refGraph = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in entityFields.Fields)
        {
            if (string.IsNullOrWhiteSpace(f.ReferencesTable)) continue;
            var from = f.EntityName;
            var to = f.ReferencesTable!;
            if (!refGraph.ContainsKey(from)) refGraph[from] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!refGraph.ContainsKey(to)) refGraph[to] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            refGraph[from].Add(to);
            refGraph[to].Add(from); // 无向图
        }

        // Union-Find 聚类
        var parent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string Find(string x)
        {
            if (!parent.ContainsKey(x)) parent[x] = x;
            return parent[x] != x ? parent[x] = Find(parent[x]) : x;
        }
        void Union(string a, string b) { parent[Find(a)] = Find(b); }

        foreach (var (from, neighbors) in refGraph)
        {
            parent.TryAdd(from, from);
            foreach (var to in neighbors) Union(from, to);
        }

        var subDomains = parent.Keys.Select(Find).Distinct().Select(g =>
        {
            var members = parent.Where(kv => Find(kv.Key) == g).Select(kv => kv.Key).ToList();
            return string.Join("+", members.OrderBy(m => m));
        }).ToList();

        // 核心域 = 被引用最多的实体
        var refCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in entityFields.Fields)
        {
            if (string.IsNullOrWhiteSpace(f.ReferencesTable)) continue;
            refCount[f.ReferencesTable!] = refCount.GetValueOrDefault(f.ReferencesTable!) + 1;
        }
        var coreDomain = refCount.OrderByDescending(kv => kv.Value).FirstOrDefault().Key;

        var hasRefs = refGraph.Count > 0;
        return new DddDomainModel
        {
            SubDomains = subDomains.Count > 0 ? subDomains : entityFields.TableNames().ToList(),
            CoreDomain = coreDomain,
            Confidence = hasRefs ? 0.8 : 0.4,
        };
    }

    /// <summary>视角 2 聚合设计：1:N 强依赖→同聚合，PK 端=聚合根。</summary>
    private static DddAggregateDesign DeriveAggregateDesign(SaNineViewCompileResult compileResult, EntityDesignProjection entityFields)
    {
        // 聚合根 = 被 ReferencesTable 引用的实体（PK 端）
        var referencedTables = entityFields.Fields
            .Where(f => !string.IsNullOrWhiteSpace(f.ReferencesTable))
            .Select(f => f.ReferencesTable!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var rootEntities = entityFields.TableNames()
            .Where(t => referencedTables.Contains(t))
            .ToList();

        // 聚合分组：聚合根 → 引用它的实体
        var aggregates = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in rootEntities)
        {
            var members = entityFields.Fields
                .Where(f => string.Equals(f.ReferencesTable, root, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.EntityName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (members.Count > 0) aggregates[root] = members;
        }

        return new DddAggregateDesign
        {
            RootEntities = rootEntities,
            Aggregates = aggregates,
            Confidence = rootEntities.Count > 0 ? 0.75 : 0.3,
        };
    }

    /// <summary>视角 3 事件目录：BPM 节点→事件，DFD 流→输入/输出。</summary>
    private static DddEventCatalog DeriveEventCatalog(SaNineViewCompileResult compileResult)
    {
        var events = compileResult.EventResults.Select(e => $"{e.EventId}: {e.EventName}").ToList();

        var dependencies = new List<(string From, string To)>();
        foreach (var evt in compileResult.Source.BusinessEvents)
        {
            if (evt.DependsOn is { Count: > 0 })
            {
                foreach (var dep in evt.DependsOn)
                    dependencies.Add((evt.EventId, dep));
            }
        }

        return new DddEventCatalog
        {
            Events = events,
            Dependencies = dependencies,
            Confidence = events.Count > 0 ? 0.9 : 0.2,
        };
    }

    /// <summary>视角 4 CQRS：改状态→Command，只读→Query。</summary>
    private static DddCqrs DeriveCqrs(SaNineViewCompileResult compileResult)
    {
        var commands = new List<string>();
        var queries = new List<string>();

        foreach (var evt in compileResult.EventResults)
        {
            // 从事件名/复杂度推断：simple 且名称含"查询/查看/list/get"→Query，否则→Command
            var name = evt.EventName ?? evt.EventId;
            var lower = name.ToLowerInvariant();
            if (lower.Contains("查询") || lower.Contains("查看") || lower.Contains("list")
                || lower.Contains("get") || lower.Contains("view") || lower.Contains("report"))
                queries.Add($"{evt.EventId}: {name}");
            else
                commands.Add($"{evt.EventId}: {name}");
        }

        return new DddCqrs
        {
            Commands = commands,
            Queries = queries,
            Confidence = 0.6, // 基于命名启发式，confidence 中等
        };
    }

    /// <summary>视角 5 集成点：SYSTEM→SYNC_API，其他→UNKNOWN。</summary>
    private static DddIntegration DeriveIntegration(SaNineViewCompileResult compileResult)
    {
        // 从 scope 步骤产出推导外部系统边界
        var points = new List<string>();
        if (compileResult.ProjectSteps.TryGetValue("Scope", out var scopeObj))
        {
            try
            {
                var scopeJson = scopeObj?.ToString() ?? "{}";
                using var doc = JsonDocument.Parse(scopeJson);
                if (doc.RootElement.TryGetProperty("externalActors", out var actors)
                    && actors.ValueKind == JsonValueKind.Array)
                {
                    foreach (var a in actors.EnumerateArray())
                    {
                        var name = a.ValueKind == JsonValueKind.String
                            ? a.GetString() : a.TryGetProperty("name", out var n) ? n.GetString() : null;
                        if (!string.IsNullOrWhiteSpace(name))
                            points.Add($"{name} → SYNC_API");
                    }
                }
            }
            catch { /* scope 解析失败，降级空列表 */ }
        }

        return new DddIntegration
        {
            IntegrationPoints = points,
            Confidence = points.Count > 0 ? 0.7 : 0.3,
        };
    }
}
