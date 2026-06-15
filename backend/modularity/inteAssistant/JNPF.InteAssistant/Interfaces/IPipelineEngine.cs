namespace JNPF.InteAssistant.Interfaces;

/// <summary>
/// 五阶段 AI 流水线引擎
/// 阶段: requirement → architecture → design → development → delivery
/// 对齐前端 src/core/ai/pipeline/orchestrator.ts OrchestratorAgent
/// </summary>
public interface IPipelineEngine
{
    /// <summary>
    /// 创建流水线
    /// </summary>
    Task<PipelineResult> CreateAsync(
        PipelineCreateRequest request, long tenantId, long userId, CancellationToken ct = default);

    /// <summary>
    /// 启动流水线（进入第一阶段）
    /// </summary>
    Task<PipelineResult> StartAsync(long pipelineId, CancellationToken ct = default);

    /// <summary>
    /// 执行指定阶段
    /// </summary>
    Task<StageResult> ExecuteStageAsync(
        long pipelineId, string stageName, CancellationToken ct = default);

    /// <summary>
    /// 人工确认阶段产出
    /// </summary>
    Task<StageResult> ConfirmStageAsync(
        long stageId, StageConfirmation confirmation, CancellationToken ct = default);

    /// <summary>
    /// 获取流水线详情
    /// </summary>
    Task<PipelineDetail> GetDetailAsync(long pipelineId, CancellationToken ct = default);

    /// <summary>
    /// 分页查询流水线列表
    /// </summary>
    Task<List<PipelineSummary>> ListAsync(
        long tenantId, int pageIndex, int pageSize, CancellationToken ct = default);
}

// ─── DTO ───

public record PipelineCreateRequest
{
    public string Name { get; init; } = "";
    public string PipelineType { get; init; } = "full_app";
    public string UserRequirement { get; init; } = "";
}

public record PipelineResult
{
    public long PipelineId { get; init; }
    public string Name { get; init; } = "";
    public string CurrentStage { get; init; } = "";
    public string Status { get; init; } = "";
}

public record PipelineDetail
{
    public long Id { get; init; }
    public string Name { get; init; } = "";
    public string CurrentStage { get; init; } = "";
    public string Status { get; init; } = "";
    public List<StageInfo> Stages { get; init; } = new();
}

public record PipelineSummary
{
    public long Id { get; init; }
    public string Name { get; init; } = "";
    public string PipelineType { get; init; } = "";
    public string CurrentStage { get; init; } = "";
    public string Status { get; init; } = "";
    public DateTime UpdatedAt { get; init; }
}

public record StageInfo
{
    public long Id { get; init; }
    public string StageName { get; init; } = "";
    public string Status { get; init; } = "";
    public int StageOrder { get; init; }
    public int? TokensUsed { get; init; }
}

public record StageResult
{
    public long StageId { get; init; }
    public string StageName { get; init; } = "";
    public string Status { get; init; } = "";
    public string? Output { get; init; }
}

public record StageConfirmation
{
    public bool Approved { get; init; }
    public string Comment { get; init; } = "";
}
