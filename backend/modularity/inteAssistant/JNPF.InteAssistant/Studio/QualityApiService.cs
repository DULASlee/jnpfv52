using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.InteAssistant.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// 28 号质量/一致性查询 API——供 E2E 与 Studio 消费 sa_consistency / sa_quality_score。
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "StudioQuality", Order = 194)]
[AllowAnonymous]
[Route("api/studio/quality")]
public class QualityApiService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly IPipelineTripleResolver _tripleResolver;

    public QualityApiService(ISqlSugarClient db, IPipelineTripleResolver tripleResolver)
    {
        _db = db;
        _tripleResolver = tripleResolver;
    }

    /// <summary>查询最新一轮一致性检查结果（三元组过滤）。</summary>
    [HttpGet("{pipelineId:long}/consistency")]
    public async Task<object> GetConsistency(long pipelineId)
    {
        var triple = await _tripleResolver.ResolveAsync(pipelineId);

        var rows = await _db.Ado.SqlQueryAsync<dynamic>("""
            SELECT F_Id AS id, F_CheckType AS checkType, F_Severity AS severity,
                   F_ConflictsJson AS conflictsJson, F_AssumptionsJson AS assumptionsJson,
                   F_GapsJson AS gapsJson, F_RoundNumber AS roundNumber, F_CreatedAt AS createdAt
            FROM sa_consistency
            WHERE F_TenantId = @tenantId AND F_ProjectId = @projectId AND F_PIPELINE_ID = @pipelineId
            ORDER BY F_CreatedAt DESC, F_CheckType
            """, new
        {
            tenantId = triple.TenantId,
            projectId = triple.ProjectId,
            pipelineId = triple.PipelineId.ToString(),
        });

        return rows;
    }

    /// <summary>查询最新一轮质量评分（三元组过滤）。</summary>
    [HttpGet("{pipelineId:long}/score")]
    public async Task<object> GetScore(long pipelineId)
    {
        var triple = await _tripleResolver.ResolveAsync(pipelineId);

        var row = await _db.Ado.SqlQuerySingleAsync<dynamic>("""
            SELECT TOP 1
                F_Id AS id,
                F_RoundNumber AS roundNumber,
                F_StructureScore AS structureScore,
                F_CoverageScore AS coverageScore,
                F_ConsistencyScore AS consistencyScore,
                F_DepthScore AS depthScore,
                F_DddScore AS dddScore,
                F_TotalScore AS totalScore,
                F_CreatedAt AS createdAt
            FROM sa_quality_score
            WHERE F_TenantId = @tenantId AND F_ProjectId = @projectId AND F_PIPELINE_ID = @pipelineId
            ORDER BY F_CreatedAt DESC
            """, new
        {
            tenantId = triple.TenantId,
            projectId = triple.ProjectId,
            pipelineId = triple.PipelineId.ToString(),
        });

        if (row == null)
            return new { totalScore = (decimal?)null, message = "尚未生成质量评分" };

        return row;
    }
}
