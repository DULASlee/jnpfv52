using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

[ApiDescriptionSettings(Tag = "Studio", Name = "PipelineStageConfig", Order = 200)]
[Route("api/studio/pipeline")]
public class PipelineStageConfigService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    public PipelineStageConfigService(ISqlSugarClient db) { _db = db; }

    [HttpGet("stages")]
    public async Task<object> GetStages()
    {
        var list = await _db.Queryable<PipelineStageConfigEntity>().Where(x => !x.F_DeleteMark).OrderBy(x => x.F_Stage).ToListAsync();
        return new { items = list };
    }

    [HttpPut("stage/{stageNumber}/update")]
    public async Task UpdateStage(int stageNumber, [FromBody] StageConfigUpdateInput input)
    {
        var e = await _db.Queryable<PipelineStageConfigEntity>().Where(x => x.F_Stage == stageNumber && !x.F_DeleteMark).FirstAsync()
            ?? throw new InvalidOperationException($"阶段 {stageNumber} 配置不存在");
        if (input.StageName != null) e.F_StageName = input.StageName;
        if (input.Description != null) e.F_Description = input.Description;
        if (input.AgentCode != null) e.F_AgentCode = input.AgentCode;
        if (input.PromptTemplateId.HasValue) e.F_PromptTemplateId = input.PromptTemplateId;
        if (input.TimeoutSeconds.HasValue) e.F_TimeoutSeconds = input.TimeoutSeconds.Value;
        if (input.RequireConfirm.HasValue) e.F_RequireConfirm = input.RequireConfirm.Value;
        if (input.AllowRollback.HasValue) e.F_AllowRollback = input.AllowRollback.Value;
        if (input.Enabled.HasValue) e.F_Enabled = input.Enabled.Value;
        e.F_ModifyTime = DateTime.Now;
        await _db.Updateable(e).ExecuteCommandAsync();
    }
}

public class StageConfigUpdateInput { public string? StageName { get; set; } public string? Description { get; set; } public string? AgentCode { get; set; } public long? PromptTemplateId { get; set; } public int? TimeoutSeconds { get; set; } public bool? RequireConfirm { get; set; } public bool? AllowRollback { get; set; } public bool? Enabled { get; set; } }
