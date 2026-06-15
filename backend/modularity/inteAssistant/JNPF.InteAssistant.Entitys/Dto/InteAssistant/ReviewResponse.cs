namespace JNPF.InteAssistant.Entitys.Dto.InteAssistant;

/// <summary>
/// 审核响应 DTO
/// </summary>
public class ReviewResponse
{
    /// <summary>
    /// 校验中（approve 时返回）
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// 校验任务 ID（用于 SSE 追踪）
    /// </summary>
    public string? ValidationId { get; set; }

    /// <summary>
    /// 预估耗时（秒）
    /// </summary>
    public int? EstimatedSeconds { get; set; }

    /// <summary>
    /// 下一阶段（直接流转时返回）
    /// </summary>
    public string? NextStage { get; set; }
}
