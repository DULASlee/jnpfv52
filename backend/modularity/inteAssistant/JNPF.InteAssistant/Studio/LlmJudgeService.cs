using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Llm;
using JNPF.InteAssistant.Skills;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// P7-E02 LLM-as-Judge 服务（L4 业务效果评估）。
///
/// 2026 实践要点：
///   - 跨家族 Judge：经 SkillLlmBudgetGuard fast tier → 路由 mimo provider
///     （生成走 deepseek，Judge 走 mimo，避免自偏好偏差 +10-25% 虚高）
///   - pass/fail 二元输出（非 1-5 分制），强制暴露真实分歧
///   - input/output hash 入日志（非全文 — 六条生命线#1）
///   - Judge policy maxCalls=1 fast（ai_skill_llm_policy 种子 eval-judge）
///   - Guard fuse 时 skip L4（不阻塞主链 — 风险缓解）
/// </summary>
public interface ILlmJudgeService
{
    /// <summary>
    /// L4 业务效果评估。经 Guard fast tier 路由跨家族 mimo provider。
    /// 输出 pass/fail 二元判断（非 1-5 分制）。
    /// </summary>
    Task<LayerResult> JudgeAsync(JudgeRequest req, CancellationToken ct = default);
}

public sealed class JudgeRequest
{
    /// <summary>EvalRun 主键（BASE_AI_EVAL_RUN.F_Id）</summary>
    public long EvalRunId { get; set; }

    /// <summary>金标准用例（含期望产出）</summary>
    public EvalCaseEntity GoldenCase { get; set; } = null!;

    /// <summary>L1-L3 评估结果（Judge 引用 OutputDigest）</summary>
    public EvalPipelineResult Pipeline { get; set; } = null!;

    // 三元组 R12
    public string TenantId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public long PipelineId { get; set; }
}

public sealed class LlmJudgeService : ILlmJudgeService, ITransient
{
    private const string JudgeSkillId = "eval-judge";
    private const int JudgePassThreshold = 60;  // Score>=60 → PASS

    private readonly ISkillLlmBudgetGuard _guard;
    private readonly ISkillExecutionLogger _logger;
    private readonly ILogger<LlmJudgeService> _ilogger;

    public LlmJudgeService(
        ISkillLlmBudgetGuard guard,
        ISkillExecutionLogger logger,
        ILogger<LlmJudgeService> ilogger)
    {
        _guard = guard;
        _logger = logger;
        _ilogger = ilogger;
    }

    public async Task<LayerResult> JudgeAsync(JudgeRequest req, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var runId = $"eval-{req.EvalRunId}";

        try
        {
            // 1. 经 Guard：eval-judge policy maxCalls=1 fast → 路由 mimo provider
            //    fuse 时 Guard 抛 Oops.Oh(429)；此处 catch 降级为 skip（不阻塞）
            LlmCallSlot slot;
            try
            {
                slot = await _guard.AcquireAsync(
                    req.ProjectId, JudgeSkillId, runId,
                    req.TenantId, req.PipelineId, ct);
            }
            catch (JNPF.FriendlyException.AppFriendlyException ex)
            {
                _ilogger.LogWarning("Judge Guard 拒绝（budget fuse），L4 降级 skip: {Code}", ex.Message);
                _logger.LogPhase("JudgeSkipped", "guard_rejected", sw.ElapsedMilliseconds,
                    eventId: "L4", message: "budget_guard_rejected");
                return Skip("budget_guard_rejected", sw.ElapsedMilliseconds);
            }

            // 2. 构建 Judge prompt（pass/fail 二元）
            var prompt = BuildJudgePrompt(req.GoldenCase, req.Pipeline);
            var inputHash = Sha256(prompt)[..8];

            // 3. 调 LLM（跨家族 mimo）—— input hash 入日志
            _logger.LogPhase("JudgeCall", "started", sw.ElapsedMilliseconds,
                eventId: $"L4:in={inputHash}");

            var request = new ChatCompletionRequest
            {
                ProviderCode = slot.ProviderCode,  // Guard 已解析为 mimo（fast tier）
                SystemPrompt = "你是严格的质量评审。只能回答 PASS 或 FAIL，并给一句理由。",
                Messages = new() { new("user", prompt) },
                Temperature = 0.0,        // Judge 确定性，温度 0
                MaxTokens = 200,          // pass/fail + 理由，无需长输出
                ResponseFormat = "text",
                TimeoutMs = slot.TimeoutMs,
            };

            var response = await _guard.ExecuteAsync(slot, request, ct);
            var outputHash = Sha256(response.Content)[..8];

            _logger.LogPhase("JudgeCall", response.IsSuccess ? "completed" : "failed",
                sw.ElapsedMilliseconds, eventId: $"L4:in={inputHash},out={outputHash}",
                message: $"tokens={response.TokensIn + response.TokensOut}");

            // 4. Guard 释放
            _guard.ReleaseRun(runId, JudgeSkillId);

            if (!response.IsSuccess)
            {
                return Fail($"Judge 调用失败: {response.Error}", sw.ElapsedMilliseconds);
            }

            // 5. 解析 pass/fail（强制二元，拒绝 1-5 分制）
            var (passed, reason) = ParsePassFail(response.Content);
            return new LayerResult
            {
                Passed = passed,
                Metric = $"judge={(passed ? "PASS" : "FAIL")}|hash={inputHash}/{outputHash}",
                Warnings = passed ? new() : new() { reason },
                ElapsedMs = sw.ElapsedMilliseconds,
            };
        }
        catch (System.Exception ex)
        {
            _ilogger.LogWarning(ex, "L4 Judge 异常 evalRun={EvalRunId}", req.EvalRunId);
            return Fail($"L4 异常: {ex.Message}", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// 构建 Judge prompt（强制 pass/fail 二元 + 要求引用具体片段）。
    /// 2026 实践：pass/fail 二元 > 1-5 分制，避免聚类中间值。
    /// </summary>
    private static string BuildJudgePrompt(EvalCaseEntity golden, EvalPipelineResult p)
    {
        var expected = string.IsNullOrEmpty(golden.F_ExpectedIR)
            ? "（无明确期望，按业务合理性判断）"
            : golden.F_ExpectedIR;

        return $"""
            请判断以下 Skill 产出是否满足期望。

            【需求】{golden.F_Requirement}
            【期望产出】{expected}
            【L1-L3 评估】{p.OutputDigest ?? "（未提供）"}
            【产出摘要】{p.OutputDigest}

            判断标准：
            1. 产出是否满足需求的核心业务目标
            2. 产出是否结构完整、无明显缺失

            只能按以下格式回答（首词必须是 PASS 或 FAIL）：
            PASS|<一句理由>
            或
            FAIL|<一句理由>
            """;
    }

    /// <summary>
    /// 解析 Judge 输出为 pass/fail 二元。
    /// 接受 "PASS|理由" / "FAIL|理由" / "PASS" / "FAIL" / 含 pass/fail 关键词的句子。
    /// 拒绝 1-5 分制（如输出纯数字则视为 FAIL）。
    /// </summary>
    private static (bool passed, string reason) ParsePassFail(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return (false, "Judge 输出为空");

        var trimmed = content.Trim();

        // 首词 PASS / FAIL
        if (trimmed.StartsWith("PASS", StringComparison.OrdinalIgnoreCase))
        {
            var reason = trimmed.Length > 4 ? trimmed[4..].TrimStart('|', '：', ':', ' ') : "";
            return (true, reason);
        }
        if (trimmed.StartsWith("FAIL", StringComparison.OrdinalIgnoreCase))
        {
            var reason = trimmed.Length > 4 ? trimmed[4..].TrimStart('|', '：', ':', ' ') : "";
            return (false, reason);
        }

        // 纯数字（1-5 分制）→ 拒绝，视为 FAIL
        if (int.TryParse(trimmed, out var score))
        {
            return (score >= JudgePassThreshold, $"分数 {score}（按 ≥{JudgePassThreshold} 阈值二元化）");
        }

        // 关键词兜底
        var upper = trimmed.ToUpperInvariant();
        if (upper.Contains("PASS")) return (true, trimmed);
        if (upper.Contains("FAIL")) return (false, trimmed);

        return (false, $"无法解析 Judge 输出: {trimmed[..Math.Min(50, trimmed.Length)]}");
    }

    private static LayerResult Skip(string reason, long elapsedMs) => new()
    {
        Passed = false,
        Metric = $"skip:{reason}",
        Warnings = new() { reason },
        ElapsedMs = elapsedMs,
    };

    private static LayerResult Fail(string reason, long elapsedMs) => new()
    {
        Passed = false,
        Metric = reason,
        ElapsedMs = elapsedMs,
    };

    private static string Sha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
