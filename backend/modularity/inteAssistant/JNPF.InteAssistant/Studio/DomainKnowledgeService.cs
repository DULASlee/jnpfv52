using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.InteAssistant.Entitys.Entity;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

[ApiDescriptionSettings(Tag = "Studio", Name = "DomainKnowledge", Order = 202)]
[Route("api/studio/knowledge")]
public class DomainKnowledgeService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    public DomainKnowledgeService(ISqlSugarClient db) { _db = db; }

    [HttpGet("domain")]
    public async Task<object> GetNodes([FromQuery] string? domain = null, [FromQuery] int currentPage = 1, [FromQuery] int pageSize = 20)
    {
        var q = _db.Queryable<KnowledgeNodeEntity>().ClearFilter();
        RefAsync<int> t = 0;
        var items = await q.ToPageListAsync(currentPage, pageSize, t);
        return new { items, total = t.Value };
    }

    [HttpGet("domain/{id}/detail")]
    public async Task<object> GetDetail(long id)
    {
        var node = await _db.Queryable<KnowledgeNodeEntity>().ClearFilter().Where(x => x.Id == id.ToString()).FirstAsync()
            ?? throw new InvalidOperationException("知识节点不存在");
        return new { node };
    }

    [HttpGet("domain/stats")]
    public async Task<object> GetStats()
    {
        var totalNodes = await _db.Queryable<KnowledgeNodeEntity>().ClearFilter().CountAsync();
        var totalEdges = await _db.Queryable<KnowledgeEdgeEntity>().ClearFilter().CountAsync();
        return new { totalNodes, totalEdges };
    }
}
