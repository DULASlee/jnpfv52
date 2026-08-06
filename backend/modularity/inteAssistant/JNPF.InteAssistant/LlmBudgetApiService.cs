using JNPF.Common.Core.MultiTenancy;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Infrastructure.Security;
using JNPF.InteAssistant.Llm;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant;

/// <summary>
/// 项目 LLM 预算查询 API（阶段三 P3-L01）
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "StudioLlmBudget", Order = 194)]
[Route("api/studio/llm")]
public class LlmBudgetApiService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ITenantGuard _tenantGuard;

    public LlmBudgetApiService(ISqlSugarClient db, ITenantGuard tenantGuard)
    {
        _db = db;
        _tenantGuard = tenantGuard;
    }

    /// <summary>GET /api/studio/llm/budget/{projectId}</summary>
    [HttpGet("budget/{projectId}")]
    public async Task<object> GetBudgetAsync(string projectId)
    {
        var tenantId = ResolveTenantId();
        var project = await _db.Queryable<AiProjectEntity>()
            .FirstAsync(x => x.Id == projectId && x.TenantId == tenantId && !x.DeleteMark);

        if (project == null)
            throw Oops.Oh("项目不存在");

        // 旧默认 50 万 → 500 万（未跑迁移脚本时自动升级）
        if (project.TokenBudget > 0 && project.TokenBudget <= 500_000)
        {
            project.TokenBudget = LlmBudgetDefaults.DefaultProjectTokenBudget;
            var tier = TokenBudgetTierService.ComputeTier(project.TokenConsumed, project.TokenBudget);
            await _db.Updateable<AiProjectEntity>()
                .SetColumns(x => new AiProjectEntity
                {
                    TokenBudget = project.TokenBudget,
                    LlmBudgetStatus = tier,
                    LastModifyTime = DateTime.UtcNow,
                })
                .Where(x => x.Id == projectId && x.TenantId == tenantId)
                .ExecuteCommandAsync();
            project.LlmBudgetStatus = tier;
        }

        var recentCalls = await TryLoadRecentCallsAsync(projectId, tenantId);

        var tokenBudget = project.TokenBudget > 0 ? project.TokenBudget : LlmBudgetDefaults.DefaultProjectTokenBudget;
        var remaining = Math.Max(0, tokenBudget - project.TokenConsumed);
        var reserveThreshold = (long)(tokenBudget * 0.95);
        var budgetStatus = TokenBudgetTierService.ComputeTier(project.TokenConsumed, tokenBudget);

        return new
        {
            projectId,
            tenantId,
            tokenBudget,
            tokenConsumed = project.TokenConsumed,
            tokenRemaining = remaining,
            reserveThreshold,
            budgetStatus,
            canRunDesign = project.TokenConsumed < reserveThreshold,
            recentCalls = recentCalls.Select(c => new
            {
                c.RunId,
                c.SkillId,
                c.Model,
                promptTokens = c.PromptTokens,
                completionTokens = c.CompletionTokens,
                c.LatencyMs,
                c.CreatorTime,
            }),
        };
    }

    private async Task<IReadOnlyList<AiCallLogEntity>> TryLoadRecentCallsAsync(string projectId, string tenantId)
    {
        try
        {
            return await _db.Queryable<AiCallLogEntity>()
                .Where(x => x.ProjectId == projectId && x.TenantId == tenantId)
                .OrderByDescending(x => x.CreatorTime)
                .Take(20)
                .ToListAsync();
        }
        catch (Exception)
        {
            // BASE_AI_CALL_LOG 未执行阶段三迁移时降级为空列表
            return Array.Empty<AiCallLogEntity>();
        }
    }

    private string ResolveTenantId()
    {
        var resolved = TenantResolver.Resolve();
        return resolved >= 0 ? resolved.ToString() : throw Oops.Oh("无法解析租户");
    }
}
