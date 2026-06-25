using JNPF.Common.Core.Manager;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// 业务规则 CRUD 服务 (Sprint 3 - S3-3)
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "KnowledgeRule", Order = 196)]
[Route("api/studio/knowledge")]
public class KnowledgeRuleService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly IUserManager _userManager;

    public KnowledgeRuleService(ISqlSugarClient db, IUserManager userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    /// <summary>
    /// 规则列表（按租户过滤，支持类型筛选）
    /// </summary>
    [HttpGet("rules")]
    public async Task<object> GetRules(
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Queryable<KnowledgeRuleEntity>()
            .Where(r => !r.F_DeleteMark && r.F_Enabled && r.F_TenantId == _userManager.TenantId);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(r => r.F_Type == type);

        RefAsync<int> total = 0;
        var items = await query
            .OrderByDescending(r => r.F_ModifyTime ?? r.F_CreatorTime)
            .Select(r => new
            {
                r.F_Id, r.F_Name, r.F_Description, r.F_Type,
                r.F_Entity, r.F_Fields, r.F_Config,
                r.F_Source, r.F_Version, r.F_Enabled,
                CreateTime = r.F_CreatorTime, r.F_ModifyTime,
            })
            .ToPageListAsync(page, pageSize, total);

        return new { total = total.Value, items };
    }

    /// <summary>
    /// 创建规则
    /// </summary>
    [HttpPost("rule/create")]
    public async Task<dynamic> CreateRule([FromBody] KnowledgeRuleCreateInput input)
    {
        var entity = new KnowledgeRuleEntity
        {
            F_Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            F_TenantId = _userManager.TenantId,
            F_Name = input.Name,
            F_Description = input.Description,
            F_Type = input.Type ?? "decision-table",
            F_Entity = input.Entity,
            F_Fields = input.Fields,
            F_Config = input.Config,
            F_Source = input.Source ?? "human-created",
            F_Version = 1,
            F_Enabled = true,
            F_CreatorTime = DateTime.Now,
        };

        await _db.Insertable(entity).ExecuteCommandAsync();
        return new { id = entity.F_Id, version = entity.F_Version };
    }

    /// <summary>
    /// 更新规则
    /// </summary>
    [HttpPut("rule/{id}/update")]
    public async Task UpdateRule(long id, [FromBody] KnowledgeRuleUpdateInput input)
    {
        var entity = await _db.Queryable<KnowledgeRuleEntity>()
            .Where(r => r.F_Id == id && r.F_TenantId == _userManager.TenantId && !r.F_DeleteMark)
            .FirstAsync();

        if (entity == null) throw new InvalidOperationException("规则不存在");

        if (input.Name != null) entity.F_Name = input.Name;
        if (input.Description != null) entity.F_Description = input.Description;
        if (input.Config != null) entity.F_Config = input.Config;
        if (input.Fields != null) entity.F_Fields = input.Fields;
        if (input.Source != null) entity.F_Source = input.Source;
        entity.F_Version++;
        entity.F_ModifyTime = DateTime.Now;
        entity.F_ModifyUserId = long.TryParse(_userManager.UserId, out var uid) ? uid : null;

        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除规则（软删除）
    /// </summary>
    [HttpDelete("rule/{id}/delete")]
    public async Task DeleteRule(long id)
    {
        var entity = await _db.Queryable<KnowledgeRuleEntity>()
            .Where(r => r.F_Id == id && r.F_TenantId == _userManager.TenantId && !r.F_DeleteMark)
            .FirstAsync();

        if (entity == null) throw new InvalidOperationException("规则不存在");

        entity.F_DeleteMark = true;
        entity.F_ModifyTime = DateTime.Now;
        await _db.Updateable(entity).ExecuteCommandAsync();
    }
}

public class KnowledgeRuleCreateInput
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? Entity { get; set; }
    public string? Fields { get; set; }
    public string? Config { get; set; }
    public string? Source { get; set; }
}

public class KnowledgeRuleUpdateInput
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Config { get; set; }
    public string? Fields { get; set; }
    public string? Source { get; set; }
}
