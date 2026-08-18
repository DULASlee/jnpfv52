using JNPF.Common.Core.MultiTenancy;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Infrastructure.Background;
using JNPF.InteAssistant.Runtime;
using SqlSugar;

namespace JNPF.InteAssistant.Infrastructure.Security;

public interface IPipelineTripleResolver
{
    Task<PipelineTriple> ResolveAsync(
        long pipelineId,
        RequestContext? bgCtx = null,
        string? tenantSnapshot = null,
        CancellationToken ct = default);
}

/// <summary>
/// 从 BASE_AI_PIPELINE 解析 tenantId + projectId + pipelineId（禁止 pipeline≡project 混用）。
/// </summary>
public sealed class PipelineTripleResolver : IPipelineTripleResolver, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ITenantGuard _tenantGuard;

    public PipelineTripleResolver(ISqlSugarClient db, ITenantGuard tenantGuard)
    {
        _db = db;
        _tenantGuard = tenantGuard;
    }

    public async Task<PipelineTriple> ResolveAsync(
        long pipelineId,
        RequestContext? bgCtx = null,
        string? tenantSnapshot = null,
        CancellationToken ct = default)
    {
        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .FirstAsync(x => x.Id == pipelineId.ToString(), ct);

        if (pipeline == null)
            throw Oops.Oh("流水线不存在");

        var tenantId = ResolveEffectiveTenantId(bgCtx, tenantSnapshot, pipeline.TenantId);

        if (!_tenantGuard.VerifyOwnership(pipeline, tenantId) && !IsSuperTenantFromContext(bgCtx, tenantSnapshot))
            throw Oops.Oh("无权访问该流水线");

        var pipelineTenantId = pipeline.TenantId ?? tenantId;
        if (!string.Equals(pipelineTenantId, tenantId, StringComparison.Ordinal)
            && !IsSuperTenantFromContext(bgCtx, tenantSnapshot))
            throw Oops.Oh("跨租户访问被拒绝");

        var projectId = string.IsNullOrWhiteSpace(pipeline.ProjectId)
            ? pipelineId.ToString()
            : pipeline.ProjectId;

        return new PipelineTriple(pipelineTenantId, projectId, pipelineId);
    }

    private static string ResolveEffectiveTenantId(RequestContext? bgCtx, string? tenantSnapshot, string? pipelineTenantId)
    {
        foreach (var candidate in new[] { bgCtx?.TenantId, tenantSnapshot, pipelineTenantId })
        {
            if (!string.IsNullOrWhiteSpace(candidate) && candidate != "-1")
                return candidate;
        }

        var resolved = TenantResolver.Resolve();
        return resolved >= 0 ? resolved.ToString() : (pipelineTenantId ?? "-1");
    }

    private static bool IsSuperTenantFromContext(RequestContext? bgCtx, string? tenantSnapshot = null)
    {
        foreach (var raw in new[] { bgCtx?.TenantId, tenantSnapshot })
        {
            if (!string.IsNullOrWhiteSpace(raw) && long.TryParse(raw, out var tid)
                && tid == TenantResolver.PlatformTenantId)
                return true;
        }

        return false;
    }
}
