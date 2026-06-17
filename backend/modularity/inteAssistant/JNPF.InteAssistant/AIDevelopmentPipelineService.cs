using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.InteAssistant.Entitys.Common;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant;

/// <summary>
/// AI 开发流水线 API — 完整的五阶段 AI 辅助开发平台
/// </summary>
[ApiDescriptionSettings(Tag = "AI", Name = "AIPipeline", Order = 180)]
[Route("api/founder/ai/pipeline")]
public class AIDevelopmentPipelineService : IDynamicApiController, ITransient
{
    private readonly IPipelineEngine _pipelineEngine;
    private readonly DetailedDesignOrchestrator _designOrchestrator;
    private readonly ILogger<AIDevelopmentPipelineService> _logger;
    private readonly ISandboxManager _sandbox;
    private readonly ISqlSugarClient _db;

    public AIDevelopmentPipelineService(
        IPipelineEngine pipelineEngine,
        DetailedDesignOrchestrator designOrchestrator,
        ISandboxManager sandbox,
        ISqlSugarClient sqlSugarClient,
        ILogger<AIDevelopmentPipelineService> logger)
    {
        _pipelineEngine = pipelineEngine;
        _designOrchestrator = designOrchestrator;
        _sandbox = sandbox;
        _db = sqlSugarClient;
        _logger = logger;
    }

    /// <summary>
    /// 创建流水线
    /// </summary>
    [HttpPost("create")]
    public async Task<PipelineResult> CreateAsync([FromBody] PipelineCreateRequest request)
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();
        return await _pipelineEngine.CreateAsync(request, tenantId, userId);
    }

    /// <summary>
    /// 启动流水线
    /// </summary>
    [HttpPost("{pipelineId:long}/start")]
    public async Task<PipelineResult> StartAsync(long pipelineId)
    {
        return await _pipelineEngine.StartAsync(pipelineId);
    }

    /// <summary>
    /// 执行当前阶段
    /// </summary>
    [HttpPost("{pipelineId:long}/execute")]
    public async Task<StageResult> ExecuteStageAsync(
        long pipelineId, [FromBody] ExecuteStageRequest request)
    {
        return await _pipelineEngine.ExecuteStageAsync(pipelineId, request.StageName);
    }

    /// <summary>
    /// 确认阶段（人工审核）
    /// </summary>
    [HttpPost("stage/{stageId:long}/confirm")]
    public async Task<StageResult> ConfirmStageAsync(
        long stageId, [FromBody] StageConfirmation confirmation)
    {
        return await _pipelineEngine.ConfirmStageAsync(stageId, confirmation);
    }

    /// <summary>
    /// 获取流水线详情
    /// </summary>
    [HttpGet("{pipelineId:long}")]
    public async Task<PipelineDetail> GetDetailAsync(long pipelineId)
    {
        return await _pipelineEngine.GetDetailAsync(pipelineId);
    }

    /// <summary>
    /// 流水线列表
    /// </summary>
    [HttpGet("list")]
    public async Task<List<PipelineSummary>> ListAsync(
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20)
    {
        return await _pipelineEngine.ListAsync(GetTenantId(), pageIndex, pageSize);
    }

    /// <summary>
    /// 执行详细设计（6 SubAgent 并行）
    /// </summary>
    [HttpPost("{pipelineId:long}/detailed-design")]
    public async Task<DetailedDesignResult> ExecuteDetailedDesignAsync(
        long pipelineId, CancellationToken ct)
    {
        var pipeline = await _pipelineEngine.GetDetailAsync(pipelineId);
        if (pipeline?.CurrentStage != PipelineStage.Design)
            throw new InvalidOperationException("当前阶段不是总体设计阶段");

        var context = new DetailedDesignContext
        {
            ProjectName = pipeline.Name,
            Requirements = "从流水线获取的需求",
            TenantId = GetTenantId()
        };

        return await _designOrchestrator.ExecuteAsync(context, null, ct);
    }

    /// <summary>
    /// SSE 事件流 — 推送流水线状态变更（与前端 useSSEConnection 对接）
    /// </summary>
    [HttpGet("{pipelineId:long}/events")]
    public async Task GetPipelineEvents(long pipelineId, CancellationToken ct)
    {
        var response = App.HttpContext!.Response;
        response.ContentType = "text/event-stream";
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["X-Accel-Buffering"] = "no";

        while (!ct.IsCancellationRequested)
        {
            PipelineDetail? detail = null;
            try { detail = await _pipelineEngine.GetDetailAsync(pipelineId); } catch { detail = null; }

            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                stage = detail?.CurrentStage?.ToString(),
                status = detail?.Status?.ToString(),
                timestamp = DateTime.UtcNow
            });
            var data = $"data: {json}\n\n";
            await response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes(data), ct);
            await response.Body.FlushAsync(ct);
            await Task.Delay(3000, ct);
        }
    }

    /// <summary>
    /// 获取流水线 IR 版本快照
    /// </summary>
    [HttpGet("{pipelineId:long}/ir")]
    public async Task<object> GetPipelineIRAsync(long pipelineId)
    {
        var pid = pipelineId.ToString();
        var dt = await _db.Ado.GetDataTableAsync(
            "SELECT TOP 1 F_IR_SNAPSHOT, F_VERSION, F_DIFF, F_CHANGE_SUMMARY, F_VALIDATION_RESULT, F_SNAPSHOT_AT FROM BASE_IR_VERSION WHERE F_PIPELINE_ID = @pid ORDER BY F_VERSION DESC",
            new SugarParameter("@pid", pid));

        if (dt.Rows.Count == 0)
            return new { pipelineId = pid, irSnapshot = (string?)null, irVersion = 0 };

        var row = dt.Rows[0];
        return new
        {
            pipelineId = pid,
            irSnapshot = row["F_IR_SNAPSHOT"] as string,
            irVersion = Convert.ToInt32(row["F_VERSION"]),
            diff = row["F_DIFF"] as string,
            changeSummary = row["F_CHANGE_SUMMARY"] as string,
            validationResult = row["F_VALIDATION_RESULT"] as string,
            snapshotAt = row["F_SNAPSHOT_AT"] as DateTime?
        };
    }

    private long GetTenantId()
    {
        var claim = App.HttpContext?.User?.FindFirst("tenant_id")?.Value;
        return long.TryParse(claim, out var id) ? id : 0;
    }

    private long GetUserId()
    {
        var claim = App.HttpContext?.User?.FindFirst("user_id")?.Value;
        return long.TryParse(claim, out var id) ? id : 0;
    }
}

public record ExecuteStageRequest
{
    public string StageName { get; init; } = "";
}
