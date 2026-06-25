namespace JNPF.InteAssistant.Entitys.Dto.InteAssistant;

/// <summary>
/// 审核请求 DTO
/// 裁决书接口1：审核当前阶段
/// </summary>
public class ReviewRequest
{
    /// <summary>
    /// 操作类型：approve / reject / request_changes
    /// </summary>
    public string Action { get; set; } = "approve";

    /// <summary>
    /// 评论（reject/request_changes 时必填）
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// 审核者角色：expert / developer / admin / founder
    /// </summary>
    public string ReviewerRole { get; set; } = "expert";
}
