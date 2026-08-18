using System;
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
using JNPF.InteAssistant.Entitys.Ir;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// P7-E04 记忆遗忘 + 生产 trace→eval 闭环。
///
/// 2026 实践要点（基于真实代码审计后的务实调整）：
///   - 现有 ContextBuilderService.Build 已有 token budget 压缩（超 8000 token 取半），无需重建
///   - 种子表（ai_seed_templates）是静态行业模板，无动态访问时间字段，不实现理论化评分公式
///   - 核心价值：失败 trace 回写 GoldenSet（生产 trace→eval 闭环）
///   - 边界：不删 ai_ir_events（只裁剪/归档 Prompt 上下文相关数据）
///   - 三元组 R12 隔离
/// </summary>
public interface IMemoryRetentionService
{
    /// <summary>
    /// 收集失败的 skill_run，回写到 GoldenSet 的 auto_seed case 池。
    /// 2026 实践：生产 trace → 离线 eval 闭环（失败的线上 trace 自动进入 eval 集）。
    /// </summary>
    Task<FailureCollectionReport> CollectFailureTracesAsync(string tenantId, int sinceDays = 7, CancellationToken ct = default);

    /// <summary>收集需求分析域信号，回写 GoldenSet auto_seed。</summary>
    Task<FailureCollectionReport> CollectRequirementSignalsAsync(string tenantId, int sinceDays = 7, CancellationToken ct = default);
}

public sealed class MemoryRetentionService : IMemoryRetentionService, ITransient
{
    private const string AutoSeedDomain = "auto_seed";  // 系统自动维护的「失败回归集」domain 标记
    private const int BatchSize = 50;

    private readonly ISqlSugarClient _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MemoryRetentionService> _logger;

    public MemoryRetentionService(
        ISqlSugarClient db,
        IConfiguration configuration,
        ILogger<MemoryRetentionService> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<FailureCollectionReport> CollectFailureTracesAsync(string tenantId, int sinceDays = 7, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-sinceDays);

        // 1. 扫描失败的 skill_run（三元组 R12）
        var failedRuns = await _db.Queryable<AiSkillRunEntity>()
            .Where(x => x.TenantId == tenantId && x.StartedAt >= since && x.Status == "failed")
            .Take(BatchSize)
            .Select(x => new
            {
                x.Id, x.SkillId, x.ProjectId, x.PipelineId,
                x.ErrorMessage, x.StartedAt, x.TokenConsumed,
            })
            .ToListAsync(ct);

        if (failedRuns.Count == 0)
            return new FailureCollectionReport { Collected = 0, Status = "no_failures" };

        // 2. 查找或创建 auto_seed 金标准集（按租户隔离）
        var autoSet = await EnsureAutoSeedSetAsync(tenantId, ct);

        // 3. 去重：已收集过的 run（按 F_Requirement 包含 runId 判断）
        var existingCases = await _db.Queryable<EvalCaseEntity>()
            .Where(x => x.F_SetId == autoSet.F_Id && x.F_DeleteMark == null)
            .Select(x => new { x.F_Id, x.F_Requirement })
            .ToListAsync(ct);
        var collectedRunIds = new HashSet<string>(
            existingCases.Select(c => ExtractRunId(c.F_Requirement)),
            StringComparer.Ordinal);

        // 4. 回写：每个失败 run 转为一个 case（期望产出待人工补）
        var newCases = new List<EvalCaseEntity>();
        foreach (var run in failedRuns)
        {
            if (collectedRunIds.Contains(run.Id)) continue;  // 去重

            var requirement = $"[auto_seed:run={run.Id}] skill={run.SkillId} 失败于 {run.StartedAt:yyyy-MM-dd HH:mm}UTC，错误: {Truncate(run.ErrorMessage, 200)}";
            newCases.Add(new EvalCaseEntity
            {
                F_Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + newCases.Count,
                F_SetId = autoSet.F_Id,
                F_Name = $"auto_seed-{run.SkillId}-{run.Id[..Math.Min(8, run.Id.Length)]}",
                F_Requirement = requirement,
                F_ExpectedIR = "",  // 期望产出待人工补（2026 实践：先收集，后标注）
                F_Stage = null,
                F_ScoreThreshold = 0.8m,
                F_Enabled = true,
                F_CreatorTime = DateTime.Now,
            });
        }

        if (newCases.Count > 0)
        {
            await _db.Insertable(newCases).ExecuteCommandAsync(ct);
        }

        return new FailureCollectionReport
        {
            Collected = newCases.Count,
            Skipped = failedRuns.Count - newCases.Count,
            GoldenSetId = autoSet.F_Id,
            Status = newCases.Count > 0 ? "collected" : "all_duplicated",
        };
    }

    public async Task<FailureCollectionReport> CollectRequirementSignalsAsync(string tenantId, int sinceDays = 7, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-sinceDays);
        var eventTypes = new[]
        {
            IrEventTypes.RequirementSpecPmReviewed,
            IrEventTypes.RequirementAmendmentProposed,
            IrEventTypes.RequirementAmendmentApplied,
            IrEventTypes.StageConfirmed,
        };

        var events = await _db.Queryable<AiIrEventEntity>()
            .Where(x => x.TenantId == tenantId && x.CreatedAt >= since && eventTypes.Contains(x.EventType))
            .OrderBy(x => x.CreatedAt)
            .Take(BatchSize * 4)
            .Select(x => new
            {
                x.Id,
                x.ProjectId,
                x.PipelineId,
                x.EventType,
                x.Payload,
                x.CreatedAt,
                x.Sequence,
            })
            .ToListAsync(ct);

        if (events.Count == 0)
            return new FailureCollectionReport { Collected = 0, Status = "no_requirement_signals" };

        var autoSet = await EnsureAutoSeedSetAsync(tenantId, ct);
        var existingCases = await _db.Queryable<EvalCaseEntity>()
            .Where(x => x.F_SetId == autoSet.F_Id && x.F_DeleteMark == null)
            .Select(x => new { x.F_Id, x.F_Requirement })
            .ToListAsync(ct);
        var collectedKeys = new HashSet<string>(
            existingCases.Select(c => ExtractReqSeedKey(c.F_Requirement)),
            StringComparer.Ordinal);

        var proposeCounts = events
            .Where(e => e.EventType == IrEventTypes.RequirementAmendmentProposed)
            .GroupBy(e => (e.ProjectId, e.PipelineId))
            .ToDictionary(g => g.Key, g => g.Count());

        var newCases = new List<EvalCaseEntity>();
        foreach (var evt in events)
        {
            var signals = BuildRequirementSignals(
                tenantId,
                evt.ProjectId,
                evt.PipelineId,
                evt.EventType,
                evt.Payload,
                evt.CreatedAt,
                evt.Id,
                evt.Sequence,
                proposeCounts.TryGetValue((evt.ProjectId, evt.PipelineId), out var count) ? count : 0);

            foreach (var signal in signals)
            {
                if (!collectedKeys.Add(signal.Key))
                    continue;

                newCases.Add(new EvalCaseEntity
                {
                    F_Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + newCases.Count,
                    F_SetId = autoSet.F_Id,
                    F_Name = $"auto_seed-{signal.Tag}-{evt.PipelineId}-{evt.Sequence}",
                    F_Requirement = signal.Requirement,
                    F_ExpectedIR = "",
                    F_Stage = null,
                    F_ScoreThreshold = signal.ScoreThreshold,
                    F_Enabled = true,
                    F_CreatorTime = DateTime.Now,
                });
            }
        }

        if (newCases.Count > 0)
            await _db.Insertable(newCases).ExecuteCommandAsync(ct);

        _logger.LogInformation(
            "需求分析信号采集完成 tenant={TenantId} sinceDays={SinceDays} events={EventCount} collected={Collected}",
            tenantId, sinceDays, events.Count, newCases.Count);

        return new FailureCollectionReport
        {
            Collected = newCases.Count,
            Skipped = events.Count - newCases.Count,
            GoldenSetId = autoSet.F_Id,
            Status = newCases.Count > 0 ? "collected" : "all_duplicated",
        };
    }

    /// <summary>
    /// 确保 auto_seed 金标准集存在（按租户隔离）。
    /// 注：EvalGoldenSetEntity 当前无 F_TenantId 列（原表设计），用 F_Domain="auto_seed" + F_Description 标记租户。
    /// </summary>
    private async Task<EvalGoldenSetEntity> EnsureAutoSeedSetAsync(string tenantId, CancellationToken ct)
    {
        var existing = await _db.Queryable<EvalGoldenSetEntity>()
            .Where(x => x.F_Domain == AutoSeedDomain
                        && x.F_Description.Contains(TenantMarker(tenantId))
                        && x.F_Enabled
                        && x.F_DeleteMark == null)
            .FirstAsync(ct);

        if (existing != null) return existing;

        var set = new EvalGoldenSetEntity
        {
            F_Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            F_Name = "自动失败回归集",
            F_Description = $"{TenantMarker(tenantId)} 自动收集失败 skill_run 与需求分析信号的金标准集。生产 trace→eval 闭环。",
            F_Domain = AutoSeedDomain,
            F_Enabled = true,
            F_CreatorTime = DateTime.Now,
        };
        await _db.Insertable(set).ExecuteCommandAsync(ct);
        return set;
    }

    /// <summary>从 requirement 提取 runId（去重判断用）</summary>
    private static string ExtractRunId(string requirement)
    {
        const string marker = "run=";
        var idx = requirement.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0) return string.Empty;
        var start = idx + marker.Length;
        var end = requirement.IndexOf(']', start);
        return end > start ? requirement[start..end] : requirement[start..];
    }

    private static string ExtractReqSeedKey(string requirement)
    {
        const string marker = "[auto_seed:req=";
        var idx = requirement.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0) return string.Empty;
        var start = idx + marker.Length;
        var end = requirement.IndexOf(']', start);
        return end > start ? requirement[start..end] : requirement[start..];
    }

    private static IReadOnlyList<RequirementSignalSeed> BuildRequirementSignals(
        string tenantId,
        string projectId,
        string pipelineId,
        string eventType,
        string payload,
        DateTime createdAt,
        string eventId,
        long sequence,
        int proposeCount)
    {
        var signals = new List<RequirementSignalSeed>();
        switch (eventType)
        {
            case IrEventTypes.RequirementSpecPmReviewed:
                var score = ReadInt(payload, "score");
                if (score is < 85)
                {
                    signals.Add(CreateSignal(
                        "req-pm-lowscore",
                        0.8m,
                        tenantId,
                        projectId,
                        pipelineId,
                        eventId,
                        sequence,
                        createdAt,
                        $"PM 终评低分 score={score}; gaps={ReadJsonArrayText(payload, "gaps")}"));
                }
                break;
            case IrEventTypes.RequirementAmendmentProposed:
                if (proposeCount > 1)
                {
                    signals.Add(CreateSignal(
                        "req-amend-correction",
                        0.85m,
                        tenantId,
                        projectId,
                        pipelineId,
                        eventId,
                        sequence,
                        createdAt,
                        $"同 pipeline 多次 Propose，疑似理解纠正；summary={ReadNestedString(payload, "understanding", "summaryMarkdown")}"));
                }
                break;
            case IrEventTypes.RequirementAmendmentApplied:
                signals.Add(CreateSignal(
                    "req-amend-success",
                    0.9m,
                    tenantId,
                    projectId,
                    pipelineId,
                    eventId,
                    sequence,
                    createdAt,
                    $"用户确认并应用补充需求；delta={ReadString(payload, "deltaText")}"));
                break;
            case IrEventTypes.StageConfirmed:
                if (ReadBool(payload, "forceConfirm") == true)
                {
                    signals.Add(CreateSignal(
                        "req-force-confirm",
                        0.6m,
                        tenantId,
                        projectId,
                        pipelineId,
                        eventId,
                        sequence,
                        createdAt,
                        $"用户强制确认需求分析；pmScore={ReadInt(payload, "pmScore")?.ToString() ?? "null"}"));
                }
                break;
        }

        return signals;
    }

    private static RequirementSignalSeed CreateSignal(
        string tag,
        decimal scoreThreshold,
        string tenantId,
        string projectId,
        string pipelineId,
        string eventId,
        long sequence,
        DateTime createdAt,
        string summary)
    {
        var key = $"{tag}:{projectId}:{pipelineId}:{sequence}";
        var requirement =
            $"[auto_seed:req={key}] tag={tag}; tenant={tenantId}; project={projectId}; pipeline={pipelineId}; event={eventId}; at={createdAt:yyyy-MM-dd HH:mm}UTC; {Truncate(summary, 360)}";
        return new RequirementSignalSeed(key, tag, scoreThreshold, requirement);
    }

    private static int? ReadInt(string json, string property)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(property, out var el) && el.TryGetInt32(out var value) ? value : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool? ReadBool(string json, string property)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(property, out var el)) return null;
            return el.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => null,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ReadString(string json, string property)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(property, out var el)) return "";
            return el.ValueKind == JsonValueKind.String ? el.GetString() ?? "" : el.ToString();
        }
        catch (JsonException)
        {
            return "";
        }
    }

    private static string ReadNestedString(string json, string objectProperty, string property)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(objectProperty, out var obj)
                || obj.ValueKind != JsonValueKind.Object
                || !obj.TryGetProperty(property, out var el))
                return "";
            return el.ValueKind == JsonValueKind.String ? el.GetString() ?? "" : el.ToString();
        }
        catch (JsonException)
        {
            return "";
        }
    }

    private static string ReadJsonArrayText(string json, string property)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(property, out var el) || el.ValueKind != JsonValueKind.Array)
                return "";
            return string.Join("；", el.EnumerateArray().Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : x.ToString()));
        }
        catch (JsonException)
        {
            return "";
        }
    }

    private static string Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s[..max] + "…");

    private static string TenantMarker(string tenantId) => $"[tenant:{tenantId}]";

    private sealed record RequirementSignalSeed(string Key, string Tag, decimal ScoreThreshold, string Requirement);
}

/// <summary>失败收集报告</summary>
public class FailureCollectionReport
{
    public int Collected { get; set; }
    public int Skipped { get; set; }
    public long GoldenSetId { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// P7-E04 记忆遗忘 API（触发失败 trace 收集 + IR event 计数校验）。
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "SkillMemory", Order = 204)]
[Route("api/studio/skill-memory")]
public class SkillMemoryApiService : IDynamicApiController, ITransient
{
    private readonly IMemoryRetentionService _retention;
    private readonly ISqlSugarClient _db;
    private readonly IUserManager _userManager;

    public SkillMemoryApiService(
        IMemoryRetentionService retention,
        ISqlSugarClient db,
        IUserManager userManager)
    {
        _retention = retention;
        _db = db;
        _userManager = userManager;
    }

    private string TenantId() => _userManager.TenantId ?? string.Empty;

    /// <summary>
    /// POST /api/studio/skills/memory/collect-failures
    /// 触发失败 trace 收集（生产 trace→eval 闭环）。
    /// </summary>
    [HttpPost("collect-failures")]
    public async Task<object> CollectFailures(CancellationToken ct, [FromQuery] int sinceDays = 7)
        => await _retention.CollectFailureTracesAsync(TenantId(), sinceDays, ct);

    /// <summary>
    /// POST /api/studio/skill-memory/collect-req-signals?sinceDays=7
    /// 触发需求分析域信号采集（PM低分、Amend纠正/成功、强制确认）。
    /// </summary>
    [HttpPost("collect-req-signals")]
    public async Task<object> CollectReqSignals(CancellationToken ct, [FromQuery] int sinceDays = 7)
        => await _retention.CollectRequirementSignalsAsync(TenantId(), sinceDays, ct);

    /// <summary>
    /// GET /api/studio/skills/memory/ir-count?pipelineId=xxx
    /// 校验：记忆遗忘不删除 IR events（边界约束验证）。
    /// </summary>
    [HttpGet("ir-count")]
    public async Task<object> GetIrCount(CancellationToken ct, [FromQuery] string? pipelineId = null)
    {
        var tenantId = TenantId();
        var q = _db.Queryable<AiIrEventEntity>()
            .Where(x => x.TenantId == tenantId);
        if (!string.IsNullOrEmpty(pipelineId))
            q = q.Where(x => x.PipelineId == pipelineId);

        var count = await q.CountAsync(ct);
        return new { pipelineId, irEventCount = count, note = "记忆遗忘不删除 IR events（边界约束）" };
    }
}
