using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

[ApiDescriptionSettings(Tag = "Studio", Name = "ModelRouting", Order = 198)]
[Route("api/studio/pipeline")]
public class ModelRoutingService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;

    public ModelRoutingService(ISqlSugarClient db) { _db = db; }

    private long NewId() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    [HttpGet("model-routing")]
    public async Task<object> GetRoutingConfig()
    {
        var list = await _db.Queryable<ModelRoutingEntity>()
            .Where(x => !x.F_DeleteMark).OrderBy(x => x.F_Stage).OrderBy(x => x.F_Priority).ToListAsync();

        return new
        {
            stages = list.GroupBy(x => new { x.F_Stage, x.F_StageName }).Select(g => new
            {
                stage = g.Key.F_Stage,
                stageName = g.Key.F_StageName,
                providers = g.Select(x => new
                {
                    id = x.F_Id, provider = x.F_Provider, model = x.F_Model,
                    priority = x.F_Priority, maxRetries = x.F_MaxRetries, timeoutMs = x.F_TimeoutMs,
                    circuitBreakerThreshold = x.F_CircuitBreakerThreshold,
                    circuitBreakerResetMs = x.F_CircuitBreakerResetMs, enabled = x.F_Enabled,
                }).ToList(),
            }).ToList(),
        };
    }

    [HttpPut("model-routing/{id}/update")]
    public async Task UpdateRouting(long id, [FromBody] ModelRoutingUpdateInput input)
    {
        var e = await _db.Queryable<ModelRoutingEntity>().Where(x => x.F_Id == id && !x.F_DeleteMark).FirstAsync()
            ?? throw new InvalidOperationException("路由策略不存在");
        if (input.Provider != null) e.F_Provider = input.Provider;
        if (input.Model != null) e.F_Model = input.Model;
        if (input.Priority.HasValue) e.F_Priority = input.Priority.Value;
        if (input.MaxRetries.HasValue) e.F_MaxRetries = input.MaxRetries.Value;
        if (input.TimeoutMs.HasValue) e.F_TimeoutMs = input.TimeoutMs.Value;
        if (input.CircuitBreakerThreshold.HasValue) e.F_CircuitBreakerThreshold = input.CircuitBreakerThreshold.Value;
        if (input.CircuitBreakerResetMs.HasValue) e.F_CircuitBreakerResetMs = input.CircuitBreakerResetMs.Value;
        if (input.Enabled.HasValue) e.F_Enabled = input.Enabled.Value;
        e.F_ModifyTime = DateTime.Now;
        await _db.Updateable(e).ExecuteCommandAsync();
    }

    [HttpPost("model-routing/add")]
    public async Task<long> AddRouting([FromBody] ModelRoutingCreateInput input)
    {
        var entity = new ModelRoutingEntity
        {
            F_Id = NewId(), F_Stage = input.Stage, F_StageName = input.StageName,
            F_Provider = input.Provider, F_Model = input.Model, F_Priority = input.Priority,
            F_MaxRetries = input.MaxRetries, F_TimeoutMs = input.TimeoutMs,
            F_CircuitBreakerThreshold = input.CircuitBreakerThreshold,
            F_CircuitBreakerResetMs = input.CircuitBreakerResetMs,
            F_Enabled = true, F_CreatorTime = DateTime.Now,
        };
        await _db.Insertable(entity).ExecuteCommandAsync();
        return entity.F_Id;
    }

    [HttpDelete("model-routing/{id}/delete")]
    public async Task DeleteRouting(long id)
    {
        await _db.Updateable<ModelRoutingEntity>().SetColumns(x => x.F_DeleteMark, true)
            .Where(x => x.F_Id == id).ExecuteCommandAsync();
    }
}

public class ModelRoutingUpdateInput
{
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public int? Priority { get; set; }
    public int? MaxRetries { get; set; }
    public int? TimeoutMs { get; set; }
    public int? CircuitBreakerThreshold { get; set; }
    public int? CircuitBreakerResetMs { get; set; }
    public bool? Enabled { get; set; }
}

public class ModelRoutingCreateInput
{
    public int Stage { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Priority { get; set; } = 2;
    public int MaxRetries { get; set; } = 3;
    public int TimeoutMs { get; set; } = 60000;
    public int CircuitBreakerThreshold { get; set; } = 3;
    public int CircuitBreakerResetMs { get; set; } = 300000;
}
