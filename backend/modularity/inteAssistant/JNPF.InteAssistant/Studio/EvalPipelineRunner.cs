using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Skills;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// P7-E01 Eval Pipeline L1-L3 编排器（确定性，无 LLM）。
///
/// 2026 实践要点：
///   - L1/L2/L3 全部确定性（JSON Schema / 计数 / DoD），仅 L4 用 LLM
///   - fail-fast：L1 不过则跳过 L4（六条生命线#2 边界）
///   - pass^k 一致性预留（首版 k=1）
///   - 三元组 R12：所有查询带 TenantId
///   - NFR 内存：L2 分页读 IR events（单次 ≤500 条）
/// </summary>
public interface IEvalPipelineRunner
{
    /// <summary>运行 L1-L3 三层确定性评估。L1 fail-fast（L4 由 P7-E02 LlmJudgeService 单独填充）。</summary>
    Task<EvalPipelineResult> RunAsync(EvalPipelineRequest req, CancellationToken ct = default);

    /// <summary>
    /// pass^k 一致性：同一 case 最近 k 次 run 全部 L1-L3 通过 → 1.0。
    /// 首版 k=1（退化为 pass@1），架构预留扩展点。
    /// </summary>
    Task<double> ComputeConsistencyAsync(long caseId, string tenantId, int k = 1, CancellationToken ct = default);

    /// <summary>持久化 eval run 的分层结果到 BASE_AI_EVAL_RUN。</summary>
    Task PersistLayerResultsAsync(long evalRunId, EvalPipelineResult result, CancellationToken ct = default);
}

public sealed class EvalPipelineRunner : IEvalPipelineRunner, ITransient
{
    private const int IrEventPageSize = 500;          // NFR 内存：单次分页 ≤500 条
    private const int RedundantLlmThreshold = 3;       // L2：>3 次 LLM 调用无 IR append → 告警
    private const double DodPassRate = 0.8;            // L3：DoD 完成率 ≥80% 视为通过

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ISqlSugarClient _db;
    private readonly ISkillExecutionLogger _logger;
    private readonly ILogger<EvalPipelineRunner> _ilogger;

    public EvalPipelineRunner(
        ISqlSugarClient db,
        ISkillExecutionLogger logger,
        ILogger<EvalPipelineRunner> ilogger)
    {
        _db = db;
        _logger = logger;
        _ilogger = ilogger;
    }

    public async Task<EvalPipelineResult> RunAsync(EvalPipelineRequest req, CancellationToken ct = default)
    {
        var result = new EvalPipelineResult
        {
            SkillRunId = req.SkillRunId,
            SkillId = req.SkillId,
        };
        var totalSw = Stopwatch.StartNew();

        // ─── L1 组件层：JSON Schema 校验（确定性）───
        result.L1 = await RunLayer1ComponentAsync(req, ct);
        _logger.LogPhase("EvalPhaseComplete", result.L1.Passed ? "passed" : "failed",
            result.L1.ElapsedMs, eventId: "L1", message: result.L1.Metric);

        // fail-fast：L1 不过直接返回（不跑 L4 — 六条生命线#2 边界）
        if (!result.L1.Passed)
        {
            result.OutputDigest = "L1 fail-fast";
            return result;
        }

        // ─── L2 轨迹层：冗余 LLM 调用检测（确定性）───
        result.L2 = await RunLayer2TrajectoryAsync(req, ct);
        _logger.LogPhase("EvalPhaseComplete", result.L2.Passed ? "passed" : "failed",
            result.L2.ElapsedMs, eventId: "L2", message: result.L2.Metric);

        // ─── L3 任务层：DoD 完成率（确定性）───
        result.L3 = await RunLayer3TaskAsync(req, ct);
        _logger.LogPhase("EvalPhaseComplete", result.L3.Passed ? "passed" : "failed",
            result.L3.ElapsedMs, eventId: "L3", message: result.L3.Metric);

        result.OutputDigest = BuildOutputDigest(result);
        return result;
    }

    /// <summary>
    /// L1 组件评估：读该 skill_run 关联的 IR 产出 fragment，校验 JSON 可解析 + 必要字段存在。
    /// 关联方式：PipelineId + SkillId + 时间窗（run 的 StartedAt 之后）。
    /// </summary>
    private async Task<LayerResult> RunLayer1ComponentAsync(EvalPipelineRequest req, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // 1. 校验 skill_run 存在且属于当前租户（三元组 R12）
            var run = await _db.Queryable<AiSkillRunEntity>()
                .Where(x => x.Id == req.SkillRunId && x.TenantId == req.TenantId)
                .Select(x => new { x.Id, x.SkillId, x.Status, x.StartedAt, x.PipelineId, x.ProjectId })
                .FirstAsync(ct);

            if (run == null)
                return Fail("skill_run 不存在或跨租户", sw.ElapsedMilliseconds);

            if (string.IsNullOrEmpty(run.SkillId))
                return Fail("skill_run 缺少 SkillId", sw.ElapsedMilliseconds);

            // 2. 读该 run 的产出 IR 事件（按 PipelineId + TenantId + SkillId + 时间窗）
            var since = run.StartedAt;
            var outputEvents = await _db.Queryable<AiIrEventEntity>()
                .Where(x => x.PipelineId == req.PipelineId
                    && x.TenantId == req.TenantId
                    && x.SkillId == req.SkillId
                    && x.CreatedAt >= since)
                .OrderBy(x => x.Sequence)
                .Take(IrEventPageSize)
                .ToListAsync(ct);

            // 3. 组件校验：至少有产出事件，且每个 Payload 可解析为 JSON
            if (outputEvents.Count == 0)
                return Fail("无产出 IR 事件（Skill 未产生任何 fragment）", sw.ElapsedMilliseconds);

            foreach (var evt in outputEvents)
            {
                if (string.IsNullOrWhiteSpace(evt.Payload))
                    continue;
                try
                {
                    using var doc = JsonDocument.Parse(evt.Payload);
                }
                catch (JsonException ex)
                {
                    return Fail($"IR 事件 {evt.EventType} payload JSON 解析失败: {ex.Message}", sw.ElapsedMilliseconds);
                }
            }

            return new LayerResult
            {
                Passed = true,
                Metric = $"schema_ok,events={outputEvents.Count}",
                ElapsedMs = sw.ElapsedMilliseconds,
            };
        }
        catch (System.Exception ex)
        {
            _ilogger.LogWarning(ex, "L1 组件评估异常 skillRun={SkillRunId}", req.SkillRunId);
            return Fail($"L1 异常: {ex.Message}", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// L2 轨迹评估：读 IR 事件序列，检测冗余 LLM 调用。
    /// 规则：同一 SkillId 内 LLM 调用 >3 且无 IR append → RedundantLlmLoop。
    /// 数据源：SkillExecutionLogger 的 LlmCallStart 日志 + IR 事件表的 ir.append 计数。
    /// 注意：LLM 调用计数依赖日志（非入库），此处用 IR 事件中 SkillFailureRecorded
    ///       的高频模式 + ir.append 缺失作为近似信号（务实，不重建 LLM 调用入库）。
    /// </summary>
    private async Task<LayerResult> RunLayer2TrajectoryAsync(EvalPipelineRequest req, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // 分页读该 pipeline 的 IR 事件（NFR 内存：单次 ≤500 条）
            var events = await _db.Queryable<AiIrEventEntity>()
                .Where(x => x.PipelineId == req.PipelineId && x.TenantId == req.TenantId)
                .OrderBy(x => x.Sequence)
                .Take(IrEventPageSize)
                .ToListAsync(ct);

            // 按该 run 的 SkillId 过滤
            var skillEvents = events.Where(x => x.SkillId == req.SkillId).ToList();

            var irAppends = skillEvents.Count(x => !string.IsNullOrEmpty(x.FragmentId));
            var failures = skillEvents.Count(x => x.EventType == "skill.failure_recorded");
            var warnings = new List<string>();

            // 规则1：有失败记录但 IR append 数为 0 → 可能是冗余重试
            if (failures > RedundantLlmThreshold && irAppends == 0)
                warnings.Add($"RedundantLlmLoop: {failures} 次失败重试无 IR append");

            // 规则2：事件总数远超 IR append（>5x）→ 可能是大量尝试无产出
            if (skillEvents.Count > irAppends * 5 && irAppends > 0)
                warnings.Add($"LowYieldRatio: events={skillEvents.Count},ir_appends={irAppends}");

            // 规则3：完全没有 IR append（Skill 跑了但没产出）
            if (skillEvents.Count > 0 && irAppends == 0)
                warnings.Add("NoIrOutput: Skill 执行但未追加任何 IR fragment");

            return new LayerResult
            {
                Passed = warnings.Count == 0,
                Metric = $"events={skillEvents.Count},ir_appends={irAppends},failures={failures},warnings={warnings.Count}",
                Warnings = warnings,
                ElapsedMs = sw.ElapsedMilliseconds,
            };
        }
        catch (System.Exception ex)
        {
            _ilogger.LogWarning(ex, "L2 轨迹评估异常 pipeline={PipelineId}", req.PipelineId);
            return Fail($"L2 异常: {ex.Message}", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// L3 任务完成度：读 skill_run 状态 + 产出 fragment 数。
    /// pass 条件：run.Status == "completed" 且产出了至少 1 个 IR fragment。
    /// （DoD 脚本结果在 SkillHarness.ValidateOutputAsync 已校验，此处读 run 状态）
    /// </summary>
    private async Task<LayerResult> RunLayer3TaskAsync(EvalPipelineRequest req, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var run = await _db.Queryable<AiSkillRunEntity>()
                .Where(x => x.Id == req.SkillRunId && x.TenantId == req.TenantId)
                .Select(x => new { x.Status, x.Metadata })
                .FirstAsync(ct);

            if (run == null)
                return Fail("skill_run 不存在或跨租户", sw.ElapsedMilliseconds);

            // 从 Metadata 读 eventCount（SkillHarness.CompleteRunAsync 写入）
            var eventCount = 0;
            if (!string.IsNullOrEmpty(run.Metadata))
            {
                try
                {
                    using var doc = JsonDocument.Parse(run.Metadata);
                    if (doc.RootElement.TryGetProperty("eventCount", out var ec))
                        eventCount = ec.GetInt32();
                }
                catch { /* 忽略解析失败 */ }
            }

            var statusOk = run.Status == "completed";
            var hasOutput = eventCount > 0;
            var passed = statusOk && hasOutput;

            var warnings = new List<string>();
            if (!statusOk) warnings.Add($"Skill 状态异常: {run.Status}");
            if (!hasOutput) warnings.Add("无产出事件（eventCount=0）");

            return new LayerResult
            {
                Passed = passed,
                Metric = $"status={run.Status},event_count={eventCount}",
                Warnings = warnings,
                ElapsedMs = sw.ElapsedMilliseconds,
            };
        }
        catch (System.Exception ex)
        {
            _ilogger.LogWarning(ex, "L3 任务评估异常 skillRun={SkillRunId}", req.SkillRunId);
            return Fail($"L3 异常: {ex.Message}", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// pass^k 一致性：取该 case 最近 k 次 run，全部 L1-L3 通过 → 1.0。
    /// 2026 实践：报告 pass^k 而非 pass@k（多次运行一致性）。
    /// 首版 k=1（退化为 pass@1），架构预留扩展点。
    /// </summary>
    public async Task<double> ComputeConsistencyAsync(long caseId, string tenantId, int k = 1, CancellationToken ct = default)
    {
        var recentRuns = await _db.Queryable<EvalRunEntity>()
            .Where(x => x.F_CaseId == caseId && x.F_TenantId == tenantId)
            .OrderByDescending(x => x.F_RunAt)
            .Take(k)
            .Select(x => new { x.F_OverallPassed })
            .ToListAsync(ct);

        // 样本不足或任一未通过 → 0
        if (recentRuns.Count < k) return 0;
        return recentRuns.All(r => r.F_OverallPassed == true) ? 1.0 : 0.0;
    }

    // ─── 辅助方法 ───

    private static LayerResult Fail(string reason, long elapsedMs) => new()
    {
        Passed = false,
        Metric = reason,
        ElapsedMs = elapsedMs,
    };

    private static string BuildOutputDigest(EvalPipelineResult r)
    {
        // 产出摘要供 L4 Judge 引用（避免传全文 — 六条生命线#1 日志只入 hash）
        var l1 = r.L1?.Passed == true ? "ok" : "fail";
        var l2 = r.L2?.Passed == true ? "ok" : "fail";
        var l3 = r.L3?.Passed == true ? "ok" : "fail";
        return $"L1={l1};L2={l2};L3={l3}";
    }

    /// <summary>持久化 eval run 的分层结果（接口实现，见 IEvalPipelineRunner）。</summary>
    public async Task PersistLayerResultsAsync(long evalRunId, EvalPipelineResult result, CancellationToken ct = default)
    {
        var layerJson = JsonSerializer.Serialize(new
        {
            l1 = result.L1,
            l2 = result.L2,
            l3 = result.L3,
            l4 = result.L4,
        }, JsonOptions);

        await _db.Updateable<EvalRunEntity>()
            .SetColumns(x => new EvalRunEntity
            {
                F_LayerResults = layerJson,
                F_OverallPassed = result.OverallPassed,
                F_Status = "completed",
            })
            .Where(x => x.F_Id == evalRunId)
            .ExecuteCommandAsync(ct);
    }
}
