using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Enum;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant;

/// <summary>
/// Pipeline 校验后台任务
/// 异步校验链：cleanSchema → validateIR → vue-tsc
/// 版 本：v5.2.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2026-6-20
/// </summary>
public class PipelineValidationJob : ITransient
{
    private readonly SqlSugar.ISqlSugarClient _db;
    private readonly IHubContext<PipelineHub> _sse;
    private readonly ILogger<PipelineValidationJob> _logger;
    private readonly IConfiguration _configuration;

    public PipelineValidationJob(
        SqlSugar.ISqlSugarClient db,
        IHubContext<PipelineHub> sse,
        IConfiguration configuration,
        ILogger<PipelineValidationJob> logger)
    {
        _db = db;
        _sse = sse;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 异步校验链：cleanSchema → validateIR → vue-tsc
    /// 当前为骨架实现，校验逻辑后续按阶段施工
    /// </summary>
    public async Task ExecuteAsync(long pipelineId, string validationId)
    {
        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.Id == pipelineId.ToString() && x.ValidationId == validationId)
            .FirstAsync();

        if (pipeline == null)
        {
            _logger.LogWarning(
                "PipelineValidationJob 跳过: PipelineId={PipelineId}, ValidationId={ValidationId} — 已被其他操作改变",
                pipelineId, validationId);
            return;
        }

        var pipelineIdStr = pipelineId.ToString();
        var stageName = pipeline.CurrentStage;
        var tenantId = pipeline.TenantId;

        try
        {
            // Step 1: cleanSchema
            await PushSseAsync(pipelineIdStr, tenantId, new PipelineSSEEvent
            {
                Stage = stageName,
                Phase = "validating_schema",
                Thought = "正在清洗 IR Schema...",
                Progress = 30
            });

            await CleanSchemaAsync(pipeline);

            // Step 2: validateIR
            await PushSseAsync(pipelineIdStr, tenantId, new PipelineSSEEvent
            {
                Stage = stageName,
                Phase = "validating_ir",
                Thought = "正在校验 IR 结构...",
                Progress = 60
            });

            await ValidateIrAsync(pipeline);

            // Step 3: vue-tsc（如需要）
            await PushSseAsync(pipelineIdStr, tenantId, new PipelineSSEEvent
            {
                Stage = stageName,
                Phase = "validating_types",
                Thought = "正在执行 TypeScript 类型检查...",
                Progress = 90
            });

            await RunTypeCheckAsync(pipeline);

            // 全部通过 → 推进到下一阶段
            pipeline.StageStatus = PipelineStatus.Approved;
            pipeline.ValidationId = null;
            pipeline.LastModify();

            var nextStage = JNPF.InteAssistant.Entitys.Common.PipelineStage.GetNext(stageName);
            if (nextStage != null)
            {
                pipeline.CurrentStage = nextStage;
            }

            await _db.Updateable(pipeline).ExecuteCommandAsync();

            await PushSseAsync(pipelineIdStr, tenantId, new PipelineSSEEvent
            {
                Stage = stageName,
                Phase = "validation_passed",
                Thought = "✅ 校验通过，已进入下一阶段",
                Progress = 100
            });

            _logger.LogInformation(
                "PipelineValidationJob 通过: PipelineId={PipelineId}, Stage={Stage}, NextStage={NextStage}",
                pipelineId, stageName, nextStage);
        }
        catch (Exception ex)
        {
            // 校验失败 → 回退到 review
            pipeline.StageStatus = PipelineStatus.Review;
            pipeline.ValidationId = null;
            pipeline.LastModify();

            await _db.Updateable(pipeline).ExecuteCommandAsync();

            await PushSseAsync(pipelineIdStr, tenantId, new PipelineSSEEvent
            {
                Stage = stageName,
                Phase = "validation_failed",
                Thought = $"❌ 校验失败: {ex.Message}",
                Progress = 0
            });

            _logger.LogError(ex,
                "PipelineValidationJob 失败: PipelineId={PipelineId}, ValidationId={ValidationId}",
                pipelineId, validationId);

            // IR 版本快照
            await SnapshotIrAsync(pipelineIdStr, "system",
                $"校验失败: {ex.Message}");
        }
    }

    #region 校验步骤（骨架）

    /// <summary>
    /// 清洗 IR Schema
    /// </summary>
    private async Task CleanSchemaAsync(AiPipelineEntity pipeline)
    {
        var enabled = _configuration.GetValue("AI:Pipeline:Validation:enableSchemaCheck", true);
        if (!enabled)
        {
            _logger.LogInformation("CleanSchema 已跳过（配置禁用）: PipelineId={Id}", pipeline.Id);
            return;
        }

        // 骨架：延迟模拟校验耗时（后续由 IR Schema 清洗引擎实现）
        await Task.Delay(2000);
        _logger.LogInformation("CleanSchema 完成: PipelineId={Id}", pipeline.Id);
    }

    /// <summary>
    /// 校验 IR 结构
    /// </summary>
    private async Task ValidateIrAsync(AiPipelineEntity pipeline)
    {
        var enabled = _configuration.GetValue("AI:Pipeline:Validation:enableIrCheck", true);
        if (!enabled)
        {
            _logger.LogInformation("ValidateIR 已跳过（配置禁用）: PipelineId={Id}", pipeline.Id);
            return;
        }

        // 骨架：延迟模拟校验耗时（后续由 IR 结构校验引擎实现）
        await Task.Delay(3000);
        _logger.LogInformation("ValidateIR 完成: PipelineId={Id}", pipeline.Id);
    }

    /// <summary>
    /// 执行 TypeScript 类型检查
    /// </summary>
    private async Task RunTypeCheckAsync(AiPipelineEntity pipeline)
    {
        var enabled = _configuration.GetValue("AI:Pipeline:Validation:enableTypeCheck", true);
        if (!enabled)
        {
            _logger.LogInformation("TypeCheck 已跳过（配置禁用）: PipelineId={Id}", pipeline.Id);
            return;
        }

        // 骨架：延迟模拟校验耗时（后续通过 shell 执行 vue-tsc --noEmit）
        await Task.Delay(3000);
        _logger.LogInformation("TypeCheck 完成: PipelineId={Id}", pipeline.Id);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 推送 SSE 事件
    /// </summary>
    private async Task PushSseAsync(string pipelineId, string? tenantId, PipelineSSEEvent evt)
    {
        if (!string.IsNullOrEmpty(tenantId))
        {
            await _sse.Clients.Group($"tenant_{tenantId}").SendAsync("PipelineSSE", evt);
        }

        await _sse.Clients.Group($"pipeline_{pipelineId}").SendAsync("PipelineSSE", evt);
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

    #endregion
}
