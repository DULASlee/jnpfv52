using Hangfire;
using JNPF.DependencyInjection;
using JNPF.Common.Core.MultiTenancy;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Common;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Enum;
using JNPF.InteAssistant.Interfaces;
using JNPF.InstantMessaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant;

/// <summary>
/// Pipeline 编排服务 — 裁决书接口实现
/// 版 本：v5.2.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2026-6-20
/// </summary>
[ApiDescriptionSettings(Tag = "AI", Name = "PipelineOrchestrator", Order = 181)]
[Route("api/pipeline")]
public class PipelineOrchestratorService : IDynamicApiController, ITransient
{
    private readonly SqlSugar.ISqlSugarClient _db;
    private readonly IHubContext<PipelineHub> _hub;
    private readonly ILogger<PipelineOrchestratorService> _logger;
    private readonly ISandboxManager _sandbox;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// 允许执行审核操作的角色集合
    /// </summary>
    private static readonly HashSet<string> AllowedReviewerRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "expert", "developer", "admin", "founder"
    };

    /// <summary>
    /// 阶段 4→5 只允许 admin/founder
    /// </summary>
    private static readonly HashSet<string> ElevatedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "founder"
    };

    public PipelineOrchestratorService(
        SqlSugar.ISqlSugarClient db,
        IHubContext<PipelineHub> hub,
        ISandboxManager sandbox,
        IConfiguration configuration,
        ILogger<PipelineOrchestratorService> logger)
    {
        _db = db;
        _hub = hub;
        _sandbox = sandbox;
        _configuration = configuration;
        _logger = logger;
    }

    #region 裁决书接口1：审核当前阶段

    /// <summary>
    /// 审核当前阶段（裁决书接口1）
    /// POST /api/pipeline/{pipelineId}/stage/{stageName}/review
    /// </summary>
    [HttpPost("{pipelineId:long}/stage/{stageName}/review")]
    public async Task<ReviewResponse> ReviewStageAsync(
        long pipelineId,
        string stageName,
        [FromBody] ReviewRequest request)
    {
        // ─── 1. 参数校验 ───
        if ((request.Action is "reject" or "request_changes") && string.IsNullOrWhiteSpace(request.Comment))
        {
            throw Oops.Bah("否决或要求修改时，评论不能为空");
        }

        if (!AllowedReviewerRoles.Contains(request.ReviewerRole))
        {
            throw Oops.Bah($"无效的审核角色: {request.ReviewerRole}，允许的角色: {string.Join(", ", AllowedReviewerRoles)}");
        }

        // ─── 2. 状态校验 ───
        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.Id == pipelineId.ToString())
            .FirstAsync();

        if (pipeline == null)
        {
            throw Oops.Bah($"流水线 {pipelineId} 不存在");
        }

        if (pipeline.StageStatus != PipelineStatus.Review)
        {
            throw Oops.Bah($"当前状态为 {pipeline.StageStatus}，不允许审核操作");
        }

        if (!string.Equals(pipeline.CurrentStage, stageName, StringComparison.OrdinalIgnoreCase))
        {
            throw Oops.Bah($"当前阶段为 {pipeline.CurrentStage}，与请求的 {stageName} 不一致");
        }

        // ─── 3. 角色门控校验 ───
        // 阶段 4→5（development→delivery）只允许 admin/founder
        if (stageName == PipelineStage.Development && !ElevatedRoles.Contains(request.ReviewerRole))
        {
            throw Oops.Bah("开发→交付阶段只能由 admin 或 founder 审核");
        }

        // ─── 4. 根据 action 分支处理 ───
        return request.Action switch
        {
            "approve" => await HandleApproveAsync(pipeline, stageName, request),
            "reject" => await HandleRejectAsync(pipeline, stageName, request),
            "request_changes" => await HandleRequestChangesAsync(pipeline, stageName, request),
            _ => throw Oops.Bah($"无效的操作类型: {request.Action}，支持: approve / reject / request_changes")
        };
    }

    #endregion

    #region 审核分支逻辑

    /// <summary>
    /// 审批通过 → validating 过渡态 → 后台校验
    /// </summary>
    private async Task<ReviewResponse> HandleApproveAsync(
        AiPipelineEntity pipeline, string stageName, ReviewRequest request)
    {
        // 4a. 流转至 validating 过渡态
        var validationId = Guid.NewGuid().ToString("N");
        pipeline.StageStatus = PipelineStatus.Validating;
        pipeline.ValidationId = validationId;
        pipeline.LastModify();

        await _db.Updateable(pipeline).ExecuteCommandAsync();

        // 4b. 提交 Hangfire 后台校验任务（持久化队列，支持重试）
        var capturedPipelineId = long.Parse(pipeline.Id);
        BackgroundJob.Enqueue<PipelineValidationJob>(job => job.ExecuteAsync(capturedPipelineId, validationId));

        // 写审核消息
        await AppendMessageAsync(pipeline.Id, stageName, "approve",
            request.Comment ?? "审核通过", request.ReviewerRole);

        // IR 版本快照
        await SnapshotIrAsync(pipeline.Id, request.ReviewerRole,
            $"阶段{stageName}审核通过");

        _logger.LogInformation(
            "流水线审核通过: PipelineId={PipelineId}, Stage={Stage}, ValidationId={ValidationId}",
            pipeline.Id, stageName, validationId);

        // 4c. 返回（框架自动包装为 RESTfulResult，code=200）
        return new ReviewResponse
        {
            Status = "validating",
            ValidationId = validationId,
            EstimatedSeconds = 10
        };
    }

    /// <summary>
    /// 否决 → 增加否决计数 → 3 轮升级 → 停留在当前阶段
    /// </summary>
    private async Task<ReviewResponse> HandleRejectAsync(
        AiPipelineEntity pipeline, string stageName, ReviewRequest request)
    {
        // 4d. 否决：增加否决计数
        pipeline.RejectCount++;
        pipeline.StageStatus = PipelineStatus.Rejected;
        pipeline.LastModify();

        // 4e. 3 轮否决→升级
        if (pipeline.RejectCount >= 3)
        {
            var tenantId = TenantResolver.Resolve();
            await _hub.Clients.Group($"tenant_{tenantId}").SendAsync("PipelineEvent", new PipelineEventPayload
            {
                EventType = "pipeline_escalated",
                PipelineId = pipeline.Id,
                Stage = stageName,
                Reason = $"已被否决{pipeline.RejectCount}次，需管理员介入",
                Actions = new List<PipelineAction>
                {
                    new() { Type = "view", Url = $"/studio/pipeline/{pipeline.Id}" },
                    new() { Type = "reset", Url = $"/api/pipeline/{pipeline.Id}/resume" }
                }
            });

            _logger.LogWarning(
                "流水线升级: PipelineId={Id}, RejectCount={Count}, Stage={Stage}",
                pipeline.Id, pipeline.RejectCount, stageName);
        }

        // 4f. 写审核记录
        await AppendMessageAsync(pipeline.Id, stageName, "reject",
            request.Comment ?? "审核否决", request.ReviewerRole);

        // 4g. IR 版本快照
        await SnapshotIrAsync(pipeline.Id, request.ReviewerRole,
            $"阶段{stageName}被否决: {request.Comment}");

        await _db.Updateable(pipeline).ExecuteCommandAsync();

        return new ReviewResponse
        {
            NextStage = stageName // 否决后停留在当前阶段
        };
    }

    /// <summary>
    /// 要求修改 → 状态回到 Running，停留在当前阶段
    /// </summary>
    private async Task<ReviewResponse> HandleRequestChangesAsync(
        AiPipelineEntity pipeline, string stageName, ReviewRequest request)
    {
        pipeline.StageStatus = PipelineStatus.Running;
        pipeline.LastModify();

        await AppendMessageAsync(pipeline.Id, stageName, "request_changes",
            request.Comment ?? "需要修改后重新提交", request.ReviewerRole);

        await SnapshotIrAsync(pipeline.Id, request.ReviewerRole,
            $"阶段{stageName}需要修改: {request.Comment}");

        await _db.Updateable(pipeline).ExecuteCommandAsync();

        _logger.LogInformation(
            "流水线要求修改: PipelineId={Id}, Stage={Stage}, Comment={Comment}",
            pipeline.Id, stageName, request.Comment);

        return new ReviewResponse
        {
            NextStage = stageName
        };
    }

    #endregion

    #region 裁决书接口2：恢复流水线

    /// <summary>
    /// 恢复流水线（从 stale 恢复）
    /// POST /api/pipeline/{id}/resume
    /// </summary>
    [HttpPost("{pipelineId:long}/resume")]
    public async Task<ReviewResponse> ResumeAsync(long pipelineId)
    {
        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.Id == pipelineId.ToString())
            .FirstAsync();

        if (pipeline == null)
        {
            throw Oops.Bah($"流水线 {pipelineId} 不存在");
        }

        if (pipeline.StageStatus != PipelineStatus.Stale)
        {
            throw Oops.Bah($"当前状态为 {pipeline.StageStatus}，只有 stale 状态可恢复");
        }

        // 恢复到进入 stale 时的阶段
        var restoreStage = pipeline.StaleFromStage ?? pipeline.CurrentStage;
        pipeline.CurrentStage = restoreStage;
        pipeline.StageStatus = PipelineStatus.Running;
        pipeline.StaleFromStage = null;
        pipeline.StaleSince = null;
        pipeline.LastModify();

        await AppendMessageAsync(pipeline.Id, restoreStage, "resume",
            "流水线已恢复", "system");

        // SignalR 推送
        var tenantId = TenantResolver.Resolve();
        await _hub.Clients.Group($"tenant_{tenantId}").SendAsync("PipelineEvent", new PipelineEventPayload
        {
            EventType = "pipeline_resumed",
            PipelineId = pipeline.Id,
            Stage = restoreStage,
            Reason = "流水线已从超时状态恢复"
        });

        await _db.Updateable(pipeline).ExecuteCommandAsync();

        _logger.LogInformation(
            "流水线恢复: PipelineId={Id}, RestoredStage={Stage}",
            pipelineId, restoreStage);

        return new ReviewResponse
        {
            NextStage = restoreStage
        };
    }

    #endregion

    #region 裁决书接口3：Stale 列表

    /// <summary>
    /// 获取超时流水线列表
    /// GET /api/pipeline/stale
    /// </summary>
    [HttpGet("stale")]
    public async Task<List<object>> GetStaleListAsync(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20)
    {
        var list = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.StageStatus == PipelineStatus.Stale)
            .OrderByDescending(x => x.StaleSince)
            .ToPageListAsync(pageIndex + 1, pageSize);

        return list.Select(p => new
        {
            p.Id,
            p.Name,
            p.CurrentStage,
            StageStatus = p.StageStatus?.ToString(),
            p.StaleFromStage,
            p.StaleSince,
            p.LastModifyTime
        }).ToList<object>();
    }

    #endregion

    #region 裁决书接口4：放弃流水线

    /// <summary>
    /// 放弃流水线（终止态）
    /// POST /api/pipeline/{id}/abandon
    /// </summary>
    [HttpPost("{pipelineId:long}/abandon")]
    public async Task<ReviewResponse> AbandonAsync(
        long pipelineId,
        [FromBody] AbandonRequest request)
    {
        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.Id == pipelineId.ToString())
            .FirstAsync();

        if (pipeline == null)
        {
            throw Oops.Bah($"流水线 {pipelineId} 不存在");
        }

        var currentUserId = GetUserId().ToString();

        // 检查关联沙箱并销毁（如果存在）
        await DestroyAssociatedSandboxIfExists(pipelineId);

        // 清理 AI 工作区目录
        var tenantId = TenantResolver.Resolve();
        StudioWorkspaceHelper.DeleteWorkspace(tenantId.ToString(), pipelineId.ToString());

        // 清除 AI 开发上下文标记
        StudioWorkspaceHelper.ClearAiDevContext();

        // 状态设为 Abandoned
        pipeline.StageStatus = PipelineStatus.Abandoned;
        pipeline.AbandonedAt = DateTime.Now;
        pipeline.AbandonedBy = currentUserId;
        pipeline.AbandonReason = request.Reason;
        pipeline.LastModify();

        // 写审计日志
        await AppendMessageAsync(pipeline.Id, pipeline.CurrentStage, "abandon",
            request.Reason ?? "流水线已放弃", "system");

        await _db.Updateable(pipeline).ExecuteCommandAsync();

        _logger.LogWarning(
            "流水线已放弃: PipelineId={Id}, Reason={Reason}, By={User}",
            pipelineId, request.Reason, currentUserId);

        return new ReviewResponse
        {
            Status = "abandoned"
        };
    }

    #endregion

    #region 裁决书接口5：预览回滚影响

    /// <summary>
    /// 预览回滚影响范围
    /// POST /api/pipeline/{id}/preview-rollback
    /// </summary>
    [HttpPost("{pipelineId:long}/preview-rollback")]
    public async Task<RollbackPreview> PreviewRollbackAsync(
        long pipelineId,
        [FromQuery] int? targetVersion = null)
    {
        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.Id == pipelineId.ToString())
            .FirstAsync();

        if (pipeline == null)
        {
            throw Oops.Bah($"流水线 {pipelineId} 不存在");
        }

        // 构建依赖图 + 传播影响
        var versions = await _db.Queryable<IrVersionEntity>()
            .Where(x => x.PipelineId == pipelineId.ToString())
            .OrderByDescending(x => x.Version)
            .ToListAsync();

        if (versions.Count == 0)
        {
            return new RollbackPreview
            {
                AffectedNodes = new List<string>(),
                EstimatedDuration = 0,
                Warnings = new List<string> { "无版本快照，无法预览回滚影响" }
            };
        }

        var currentVersion = versions.First();
        var target = targetVersion.HasValue
            ? versions.FirstOrDefault(v => v.Version == targetVersion.Value)
            : versions.Skip(1).FirstOrDefault(); // 默认回滚到上一版本

        if (target == null)
        {
            throw Oops.Bah($"目标版本 {targetVersion} 不存在");
        }

        // 计算影响范围（骨架：实际依赖图分析由前端 IR 模块完成）
        var affectedNodes = new List<string>();
        var warnings = new List<string>();

        if (!string.IsNullOrEmpty(currentVersion.Diff))
        {
            affectedNodes.Add($"版本 {currentVersion.Version} 的变更将被回滚");
        }

        if (currentVersion.Version - target.Version > 3)
        {
            warnings.Add($"跨 {currentVersion.Version - target.Version} 个版本回滚，影响范围较大");
        }

        var estimatedDuration = affectedNodes.Count * 2; // 估算每个节点 2 秒

        return new RollbackPreview
        {
            AffectedNodes = affectedNodes,
            EstimatedDuration = estimatedDuration,
            Warnings = warnings
        };
    }

    #endregion

    #region 裁决书接口6：审批摘要

    /// <summary>
    /// 获取审批摘要（配置化阈值对比）
    /// GET /api/pipeline/{id}/approval-summary
    /// </summary>
    [HttpGet("{pipelineId:long}/approval-summary")]
    public async Task<object> GetApprovalSummaryAsync(long pipelineId)
    {
        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.Id == pipelineId.ToString())
            .FirstAsync();

        if (pipeline == null)
        {
            throw Oops.Bah($"流水线 {pipelineId} 不存在");
        }

        // 配置化阈值（从 AI.json 读取，支持热更新）
        var section = _configuration.GetSection("AI:Pipeline:ReviewThresholds");
        var thresholds = new Dictionary<string, int>
        {
            ["maxRejectCount"] = section.GetValue("maxRejectCount", 3),
            ["maxStaleHours"] = section.GetValue("maxStaleHours", 72),
            ["maxExecutionMinutes"] = section.GetValue("maxExecutionMinutes", 30)
        };

        // 实际指标
        var passed = pipeline.RejectCount < thresholds["maxRejectCount"];
        var staleWarning = pipeline.StaleSince.HasValue &&
            (DateTime.Now - pipeline.StaleSince.Value).TotalHours > 0;

        var details = new List<object>
        {
            new { metric = "否决次数", actual = pipeline.RejectCount, threshold = thresholds["maxRejectCount"], passed },
            new { metric = "超时状态", actual = pipeline.StageStatus == PipelineStatus.Stale ? "是" : "否", threshold = $">{thresholds["maxStaleHours"]}h", passed = !staleWarning },
        };

        return new
        {
            pipelineId = pipelineId.ToString(),
            pipeline.Name,
            pipeline.CurrentStage,
            StageStatus = pipeline.StageStatus?.ToString(),
            pipeline.RejectCount,
            passed = passed && !staleWarning,
            details
        };
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 追加流水线消息
    /// </summary>
    private async Task AppendMessageAsync(
        string pipelineId, string stage, string role, string content, string reviewerRole)
    {
        var message = new AiPipelineMessageEntity
        {
            PipelineId = pipelineId,
            Stage = stage,
            Role = role,
            Content = $"[{reviewerRole}] {content}",
            Sequence = await GetNextSequenceAsync(pipelineId, stage)
        };
        message.Creator();

        await _db.Insertable(message).ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取下一个消息序号
    /// </summary>
    private async Task<int> GetNextSequenceAsync(string pipelineId, string stage)
    {
        var maxSeq = await _db.Queryable<AiPipelineMessageEntity>()
            .Where(x => x.PipelineId == pipelineId && x.Stage == stage)
            .MaxAsync(x => (int?)x.Sequence) ?? 0;
        return maxSeq + 1;
    }

    /// <summary>
    /// IR 版本快照
    /// </summary>
    private async Task SnapshotIrAsync(string pipelineId, string triggeredBy, string summary)
    {
        var latestVersion = await _db.Queryable<IrVersionEntity>()
            .Where(x => x.PipelineId == pipelineId)
            .OrderByDescending(x => x.Version)
            .Select(x => x.Version)
            .FirstAsync();

        var entity = new IrVersionEntity
        {
            PipelineId = pipelineId,
            Version = latestVersion + 1,
            TriggeredBy = triggeredBy,
            ChangeSummary = summary,
            ParentVersion = latestVersion > 0 ? latestVersion : null
        };
        entity.Creator();

        await _db.Insertable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 销毁关联沙箱
    /// </summary>
    private async Task DestroyAssociatedSandboxIfExists(long pipelineId)
    {
        try
        {
            var sandboxId = $"pipeline-{pipelineId}";
            var sandbox = await _sandbox.GetStatusAsync(sandboxId);
            if (sandbox != null && sandbox.Status != "destroyed")
            {
                await _sandbox.DestroyAsync(sandboxId);
                _logger.LogInformation("沙箱已销毁: SandboxId={SandboxId}, PipelineId={PipelineId}", sandboxId, pipelineId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "销毁沙箱失败: PipelineId={PipelineId}", pipelineId);
        }
    }

    private long GetUserId()
    {
        var claim = App.HttpContext?.User?.FindFirst("user_id")?.Value;
        return long.TryParse(claim, out var id) ? id : 0;
    }

    #endregion
}

// ─── 辅助 DTO ───

/// <summary>
/// 放弃请求
/// </summary>
public class AbandonRequest
{
    /// <summary>放弃原因</summary>
    public string? Reason { get; set; }
}

/// <summary>
/// 回滚预览结果
/// </summary>
public class RollbackPreview
{
    /// <summary>受影响的节点</summary>
    public List<string> AffectedNodes { get; set; } = new();

    /// <summary>预估耗时（秒）</summary>
    public int EstimatedDuration { get; set; }

    /// <summary>警告信息</summary>
    public List<string> Warnings { get; set; } = new();
}
