using System.Text.Json;
using JNPF.Common.Core.Manager;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

[ApiDescriptionSettings(Tag = "Studio", Name = "Eval", Order = 201)]
[Route("api/studio/eval")]
public class EvalService : IDynamicApiController, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ISqlSugarClient _db;
    private readonly IUserManager _userManager;
    private readonly IEvalPipelineRunner _pipelineRunner;
    private readonly ILlmJudgeService _judge;
    private readonly IJudgeCalibrationService _calibration;

    public EvalService(
        ISqlSugarClient db,
        IUserManager userManager,
        IEvalPipelineRunner pipelineRunner,
        ILlmJudgeService judge,
        IJudgeCalibrationService calibration)
    {
        _db = db;
        _userManager = userManager;
        _pipelineRunner = pipelineRunner;
        _judge = judge;
        _calibration = calibration;
    }

    private long NewId() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    private long? UserId() => long.TryParse(_userManager.UserId, out var id) ? id : null;
    private string TenantId() => _userManager.TenantId ?? string.Empty;

    [HttpGet("golden-set")]
    public async Task<object> GetGoldenSets([FromQuery] string? domain = null, [FromQuery] int currentPage = 1, [FromQuery] int pageSize = 20)
    {
        var tenant = TenantId();
        // R12 三元组隔离：仅返回当前租户的金标准集
        var q = _db.Queryable<EvalGoldenSetEntity>()
            .Where(x => x.F_DeleteMark == null && x.F_Enabled);
        // 注：金标准集表暂无 F_TenantId 列（原表设计），按 domain 过滤；后续迁移补列后加租户过滤
        if (!string.IsNullOrEmpty(domain)) q = q.Where(x => x.F_Domain == domain);
        RefAsync<int> t = 0;
        var items = await q.OrderByDescending(x => x.F_CreatorTime).ToPageListAsync(currentPage, pageSize, t);
        return new { items, total = t.Value };
    }

    [HttpPost("golden-set/create")]
    public async Task<long> CreateGoldenSet([FromBody] GoldenSetCreateInput i)
    {
        var e = new EvalGoldenSetEntity
        {
            F_Id = NewId(),
            F_Name = i.Name,
            F_Description = i.Description,
            F_Domain = i.Domain,
            F_Enabled = true,
            F_CreatorTime = DateTime.Now,
            F_CreatorUserId = UserId(),
        };
        await _db.Insertable(e).ExecuteCommandAsync();
        return e.F_Id;
    }

    [HttpGet("golden-set/{setId}/cases")]
    public async Task<object> GetCases(long setId)
    {
        var items = await _db.Queryable<EvalCaseEntity>()
            .Where(x => x.F_SetId == setId && x.F_DeleteMark == null)
            .OrderBy(x => x.F_Stage)
            .ToListAsync();
        return new { items, total = items.Count };
    }

    [HttpPost("case/create")]
    public async Task<long> CreateCase([FromBody] EvalCaseCreateInput i)
    {
        var e = new EvalCaseEntity
        {
            F_Id = NewId(),
            F_SetId = i.SetId,
            F_Name = i.Name,
            F_Requirement = i.Requirement,
            F_ExpectedIR = i.ExpectedIR,
            F_Stage = i.Stage,
            F_ScoreThreshold = i.ScoreThreshold ?? 0.8m,
            F_Enabled = true,
            F_CreatorTime = DateTime.Now,
            F_CreatorUserId = UserId(),
        };
        await _db.Insertable(e).ExecuteCommandAsync();
        return e.F_Id;
    }

    /// <summary>
    /// 创建 eval run（pending 状态，记录三元组 R12）。
    /// 实际 L1-L3 评估由 POST /api/studio/eval/execute 触发（解耦创建与执行）。
    /// </summary>
    [HttpPost("run")]
    public async Task<object> RunEval([FromBody] EvalRunInput i)
    {
        var cases = await _db.Queryable<EvalCaseEntity>()
            .Where(x => x.F_SetId == i.SetId && x.F_DeleteMark == null && x.F_Enabled)
            .ToListAsync();
        if (cases.Count == 0) throw Oops.Bah("无可用测试用例");

        // 三元组从入参或当前用户上下文取（R12 隔离）
        var tenantId = !string.IsNullOrEmpty(i.TenantId) ? i.TenantId : TenantId();

        var run = new EvalRunEntity
        {
            F_Id = NewId(),
            F_SetId = i.SetId,
            F_RunAt = DateTime.Now,
            F_TotalCases = cases.Count,
            F_PassedCases = 0,
            F_Details = JsonSerializer.Serialize(new { status = "pending" }, JsonOptions),
            F_CreatorTime = DateTime.Now,
            F_CreatorUserId = UserId(),
            // P7-E01 三元组
            F_TenantId = tenantId,
            F_ProjectId = i.ProjectId ?? string.Empty,
            F_PipelineId = i.PipelineId ?? string.Empty,
            F_Status = "pending",
        };
        await _db.Insertable(run).ExecuteCommandAsync();
        return new { runId = run.F_Id, totalCases = cases.Count, status = "pending" };
    }

    /// <summary>
    /// 执行三层确定性评估（L1-L3），写入 F_LayerResults。
    /// L4 由 P7-E02 LlmJudgeService 单独触发；此处不调 LLM。
    /// fail-fast：L1 不过则直接返回，不跑 L2/L3。
    /// </summary>
    [HttpPost("execute")]
    public async Task<object> ExecuteEval([FromBody] EvalExecuteInput i)
    {
        var tenantId = !string.IsNullOrEmpty(i.TenantId) ? i.TenantId : TenantId();

        // 校验 eval run 归属（R12）
        var run = await _db.Queryable<EvalRunEntity>()
            .Where(x => x.F_Id == i.EvalRunId && x.F_TenantId == tenantId)
            .FirstAsync() ?? throw Oops.Bah("eval run 不存在或跨租户");

        var req = new EvalPipelineRequest
        {
            EvalRunId = i.EvalRunId,
            SkillRunId = i.SkillRunId,
            TenantId = tenantId,
            ProjectId = !string.IsNullOrEmpty(i.ProjectId) ? i.ProjectId : run.F_ProjectId,
            PipelineId = !string.IsNullOrEmpty(i.PipelineId) ? i.PipelineId : run.F_PipelineId,
            SkillId = i.SkillId ?? string.Empty,
        };

        // 标记 running（六条生命线#1 日志可追溯）
        await _db.Updateable<EvalRunEntity>()
            .SetColumns(x => new EvalRunEntity { F_Status = "running" })
            .Where(x => x.F_Id == i.EvalRunId)
            .ExecuteCommandAsync();

        var result = await _pipelineRunner.RunAsync(req);
        await _pipelineRunner.PersistLayerResultsAsync(i.EvalRunId, result);

        return new
        {
            evalRunId = i.EvalRunId,
            overallPassed = result.OverallPassed,
            l1 = result.L1,
            l2 = result.L2,
            l3 = result.L3,
            outputDigest = result.OutputDigest,
        };
    }

    /// <summary>
    /// 查询单个 eval run 详情（含分层结果 JSON 展开）。
    /// 三元组 R12 隔离：仅返回当前租户的 run。
    /// </summary>
    [HttpGet("run/{runId:long}")]
    public async Task<object> GetRun(long runId)
    {
        var tenantId = TenantId();
        var run = await _db.Queryable<EvalRunEntity>()
            .Where(x => x.F_Id == runId && x.F_TenantId == tenantId)
            .FirstAsync() ?? throw Oops.Bah("eval run 不存在或跨租户");

        // 反序列化分层结果 JSON
        LayerResultsDto? layerResults = null;
        if (!string.IsNullOrEmpty(run.F_LayerResults))
        {
            try
            {
                layerResults = JsonSerializer.Deserialize<LayerResultsDto>(run.F_LayerResults, JsonOptions);
            }
            catch { /* 忽略解析失败，返回 null */ }
        }

        return new EvalRunDetailDto
        {
            Id = run.F_Id,
            SetId = run.F_SetId,
            CaseId = run.F_CaseId,
            Status = run.F_Status,
            RunAt = run.F_RunAt,
            TenantId = run.F_TenantId,
            ProjectId = run.F_ProjectId,
            PipelineId = run.F_PipelineId,
            OverallPassed = run.F_OverallPassed,
            JudgeKappa = run.F_JudgeKappa,
            Consistency = run.F_Consistency,
            LayerResults = layerResults,
        };
    }

    /// <summary>
    /// pass^k 一致性查询（架构预留，首版 k=1）。
    /// </summary>
    [HttpGet("consistency/{caseId:long}")]
    public async Task<object> GetConsistency(long caseId, [FromQuery] int k = 1)
    {
        var tenantId = TenantId();
        var consistency = await _pipelineRunner.ComputeConsistencyAsync(caseId, tenantId, k);
        return new { caseId, k, consistency };
    }

    /// <summary>
    /// P7-E02 L4 LLM-as-Judge 评估。
    /// 经 SkillLlmBudgetGuard fast tier 路由跨家族 mimo provider；输出 pass/fail 二元。
    /// 仅在 L1-L3 通过后调用（六条生命线#2 边界：L1 fail 不跑 L4）。
    /// </summary>
    [HttpPost("judge")]
    public async Task<object> JudgeEval([FromBody] JudgeEvalInput i)
    {
        var tenantId = !string.IsNullOrEmpty(i.TenantId) ? i.TenantId : TenantId();

        // 校验 eval run 归属（R12）
        var run = await _db.Queryable<EvalRunEntity>()
            .Where(x => x.F_Id == i.EvalRunId && x.F_TenantId == tenantId)
            .FirstAsync() ?? throw Oops.Bah("eval run 不存在或跨租户");

        // 读金标准 case（提供期望产出）
        var goldenCase = await _db.Queryable<EvalCaseEntity>()
            .Where(x => x.F_Id == i.CaseId && x.F_DeleteMark == null)
            .FirstAsync();

        // 重建 L1-L3 结果（从持久化的 LayerResults，或重新跑）
        EvalPipelineResult pipeline;
        if (!string.IsNullOrEmpty(run.F_LayerResults))
        {
            pipeline = new EvalPipelineResult { RunId = run.F_Id };
            try
            {
                var layers = JsonSerializer.Deserialize<LayerResultsDto>(run.F_LayerResults, JsonOptions);
                if (layers != null)
                {
                    pipeline.L1 = layers.L1;
                    pipeline.L2 = layers.L2;
                    pipeline.L3 = layers.L3;
                    pipeline.OutputDigest = $"L1={pipeline.L1?.Passed == true};L2={pipeline.L2?.Passed == true};L3={pipeline.L3?.Passed == true}";
                }
            }
            catch { /* 忽略 */ }
        }
        else
        {
            // 无持久化结果则现场重跑 L1-L3
            var req = new EvalPipelineRequest
            {
                EvalRunId = i.EvalRunId,
                SkillRunId = i.SkillRunId,
                TenantId = tenantId,
                ProjectId = !string.IsNullOrEmpty(i.ProjectId) ? i.ProjectId : run.F_ProjectId,
                PipelineId = !string.IsNullOrEmpty(i.PipelineId) ? i.PipelineId : run.F_PipelineId,
                SkillId = i.SkillId ?? string.Empty,
            };
            pipeline = await _pipelineRunner.RunAsync(req);
        }

        // 边界：L1 fail 不跑 L4（Judge 无意义）
        if (pipeline.L1?.Passed != true)
            throw Oops.Bah("L1 组件评估未通过，跳过 L4 Judge（边界约束）");

        // 解析 pipelineId（三元组）
        long.TryParse(run.F_PipelineId, out var pipelineIdNum);

        var judgeResult = await _judge.JudgeAsync(new JudgeRequest
        {
            EvalRunId = i.EvalRunId,
            GoldenCase = goldenCase ?? new EvalCaseEntity
            {
                F_Requirement = "（未指定金标准 case）",
                F_ExpectedIR = null,
            },
            Pipeline = pipeline,
            TenantId = tenantId,
            ProjectId = run.F_ProjectId,
            PipelineId = pipelineIdNum,
        });

        // 持久化 L4 到 LayerResults
        pipeline.L4 = judgeResult;
        var layerJson = JsonSerializer.Serialize(new
        {
            l1 = pipeline.L1, l2 = pipeline.L2, l3 = pipeline.L3, l4 = pipeline.L4,
        }, JsonOptions);
        await _db.Updateable<EvalRunEntity>()
            .SetColumns(x => new EvalRunEntity { F_LayerResults = layerJson })
            .Where(x => x.F_Id == i.EvalRunId)
            .ExecuteCommandAsync();

        return new
        {
            evalRunId = i.EvalRunId,
            l4 = judgeResult,
            verdict = judgeResult.Passed ? "PASS" : "FAIL",
            metric = judgeResult.Metric,
        };
    }

    /// <summary>
    /// P7-E02 Judge 校准报告（Cohen's kappa）。
    /// kappa < 0.6 → untrusted（L4 应降级为 advisory）。
    /// </summary>
    [HttpGet("calibration")]
    public async Task<object> GetCalibration([FromQuery] int minSamples = 10)
    {
        var tenantId = TenantId();
        var report = await _calibration.CalibrateAsync(tenantId, minSamples);
        return report;
    }

    [HttpGet("history")]
    public async Task<object> GetHistory([FromQuery] long? setId = null, [FromQuery] int currentPage = 1, [FromQuery] int pageSize = 20)
    {
        var tenantId = TenantId();
        // R12 三元组隔离：仅返回当前租户的 eval run
        var q = _db.Queryable<EvalRunEntity>().Where(x => x.F_TenantId == tenantId);
        if (setId.HasValue) q = q.Where(x => x.F_SetId == setId.Value);
        RefAsync<int> t = 0;
        var items = await q.OrderByDescending(x => x.F_RunAt).ToPageListAsync(currentPage, pageSize, t);
        return new { items, total = t.Value };
    }
}

public class GoldenSetCreateInput
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Domain { get; set; }
}

public class EvalCaseCreateInput
{
    public long SetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Requirement { get; set; } = string.Empty;
    public string? ExpectedIR { get; set; }
    public int? Stage { get; set; }
    public decimal? ScoreThreshold { get; set; }
}

public class EvalRunInput
{
    public long SetId { get; set; }
    /// <summary>三元组 R12（可选，默认取当前用户租户）</summary>
    public string? TenantId { get; set; }
    public string? ProjectId { get; set; }
    public string? PipelineId { get; set; }
}

/// <summary>P7-E01 执行三层评估输入</summary>
public class EvalExecuteInput
{
    public long EvalRunId { get; set; }
    /// <summary>被评估的 skill_run id（ai_skill_runs.F_Id, string/GUID）</summary>
    public string SkillRunId { get; set; } = string.Empty;
    public string? SkillId { get; set; }
    public string? TenantId { get; set; }
    public string? ProjectId { get; set; }
    public string? PipelineId { get; set; }
}

/// <summary>P7-E02 L4 Judge 评估输入</summary>
public class JudgeEvalInput
{
    public long EvalRunId { get; set; }
    /// <summary>金标准 case id（提供期望产出，可选）</summary>
    public long? CaseId { get; set; }
    /// <summary>被评估的 skill_run id（无持久化 LayerResults 时现场重跑 L1-L3）</summary>
    public string? SkillRunId { get; set; }
    public string? SkillId { get; set; }
    public string? TenantId { get; set; }
    public string? ProjectId { get; set; }
    public string? PipelineId { get; set; }
}
