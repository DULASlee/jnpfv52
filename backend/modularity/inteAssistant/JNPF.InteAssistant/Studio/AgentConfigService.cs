using JNPF.Common.Core.Manager;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// 智能体管理服务 (Sprint 4 - S4-2)
/// CRUD + Skills/MCP 管理 + 测试运行
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "AgentConfig", Order = 197)]
[Route("api/studio/agent")]
public class AgentConfigService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly IUserManager _userManager;

    public AgentConfigService(ISqlSugarClient db, IUserManager userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private long NewId() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    private long? UserId() => long.TryParse(_userManager.UserId, out var id) ? id : null;

    // ═══════════════════ 智能体 CRUD ═══════════════════

    [HttpGet("list")]
    public async Task<object> GetList(
        [FromQuery] int currentPage = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] string? agentType = null)
    {
        var query = _db.Queryable<AgentConfigEntity>().Where(x => !x.F_DeleteMark);
        if (!string.IsNullOrEmpty(keyword))
            query = query.Where(x => x.F_Name.Contains(keyword) || x.F_AgentCode.Contains(keyword));
        if (!string.IsNullOrEmpty(agentType))
            query = query.Where(x => x.F_AgentType == agentType);

        RefAsync<int> total = 0;
        var list = await query.OrderBy(x => x.F_Sort).OrderByDescending(x => x.F_CreatorTime)
            .ToPageListAsync(currentPage, pageSize, total);
        return new { items = list, total = total.Value, currentPage, pageSize };
    }

    [HttpGet("{id}")]
    public async Task<object> GetDetail(long id)
    {
        var entity = await _db.Queryable<AgentConfigEntity>()
            .Where(x => x.F_Id == id && !x.F_DeleteMark).FirstAsync()
            ?? throw new InvalidOperationException("智能体不存在");

        var skills = await _db.Queryable<AgentSkillEntity>()
            .Where(x => x.F_AgentId == id && !x.F_DeleteMark).ToListAsync();

        return new { agent = entity, skills };
    }

    [HttpPost("create")]
    public async Task<long> Create([FromBody] AgentCreateInput input)
    {
        if (await _db.Queryable<AgentConfigEntity>().AnyAsync(x => x.F_AgentCode == input.AgentCode && !x.F_DeleteMark))
            throw new InvalidOperationException($"智能体编码 {input.AgentCode} 已存在");

        var entity = new AgentConfigEntity
        {
            F_Id = NewId(), F_AgentCode = input.AgentCode, F_Name = input.Name,
            F_Description = input.Description, F_AgentType = input.AgentType,
            F_PromptTemplateId = input.PromptTemplateId, F_SystemPrompt = input.SystemPrompt,
            F_ModelProvider = input.ModelProvider ?? "deepseek", F_ModelName = input.ModelName ?? "deepseek-chat",
            F_Temperature = input.Temperature ?? 0.7m, F_MaxTokens = input.MaxTokens ?? 4096,
            F_Config = input.Config, F_Enabled = true, F_Sort = input.Sort,
            F_CreatorTime = DateTime.Now, F_CreatorUserId = UserId(),
        };
        await _db.Insertable(entity).ExecuteCommandAsync();
        return entity.F_Id;
    }

    [HttpPut("{id}/update")]
    public async Task Update(long id, [FromBody] AgentUpdateInput input)
    {
        var entity = await _db.Queryable<AgentConfigEntity>().Where(x => x.F_Id == id && !x.F_DeleteMark).FirstAsync()
            ?? throw new InvalidOperationException("智能体不存在");

        if (input.Name != null) entity.F_Name = input.Name;
        if (input.Description != null) entity.F_Description = input.Description;
        if (input.SystemPrompt != null) entity.F_SystemPrompt = input.SystemPrompt;
        if (input.ModelProvider != null) entity.F_ModelProvider = input.ModelProvider;
        if (input.ModelName != null) entity.F_ModelName = input.ModelName;
        if (input.Temperature.HasValue) entity.F_Temperature = input.Temperature.Value;
        if (input.MaxTokens.HasValue) entity.F_MaxTokens = input.MaxTokens.Value;
        if (input.Config != null) entity.F_Config = input.Config;
        if (input.Sort.HasValue) entity.F_Sort = input.Sort.Value;
        entity.F_ModifyTime = DateTime.Now;
        entity.F_ModifyUserId = UserId();

        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    [HttpDelete("{id}/delete")]
    public async Task Delete(long id)
    {
        await _db.Updateable<AgentConfigEntity>()
            .SetColumns(x => x.F_DeleteMark, true)
            .SetColumns(x => x.F_ModifyTime, DateTime.Now)
            .SetColumns(x => x.F_ModifyUserId, UserId())
            .Where(x => x.F_Id == id).ExecuteCommandAsync();
    }

    // ═══════════════════ Skills ═══════════════════

    [HttpGet("{agentId}/skills")]
    public async Task<object> GetSkills(long agentId)
    {
        var list = await _db.Queryable<AgentSkillEntity>()
            .Where(x => x.F_AgentId == agentId && !x.F_DeleteMark).ToListAsync();
        return new { items = list, total = list.Count };
    }

    [HttpPost("skill/create")]
    public async Task<long> CreateSkill([FromBody] SkillCreateInput input)
    {
        var entity = new AgentSkillEntity
        {
            F_Id = NewId(), F_AgentId = input.AgentId, F_SkillCode = input.SkillCode,
            F_Name = input.Name, F_Description = input.Description, F_SkillType = input.SkillType,
            F_Config = input.Config, F_Enabled = true,
            F_CreatorTime = DateTime.Now, F_CreatorUserId = UserId(),
        };
        await _db.Insertable(entity).ExecuteCommandAsync();
        return entity.F_Id;
    }

    [HttpPut("skill/{id}/update")]
    public async Task UpdateSkill(long id, [FromBody] SkillUpdateInput input)
    {
        await _db.Updateable<AgentSkillEntity>()
            .SetColumns(x => x.F_Name, input.Name)
            .SetColumns(x => x.F_Config, input.Config)
            .SetColumns(x => x.F_ModifyTime, DateTime.Now)
            .Where(x => x.F_Id == id).ExecuteCommandAsync();
    }

    [HttpDelete("skill/{id}/delete")]
    public async Task DeleteSkill(long id)
    {
        await _db.Updateable<AgentSkillEntity>().SetColumns(x => x.F_DeleteMark, true)
            .Where(x => x.F_Id == id).ExecuteCommandAsync();
    }

    // ═══════════════════ MCP ═══════════════════

    [HttpGet("mcp/list")]
    public async Task<object> GetMcpConfigs()
    {
        var list = await _db.Queryable<McpConfigEntity>().Where(x => !x.F_DeleteMark).ToListAsync();
        return new { items = list, total = list.Count };
    }

    [HttpPost("mcp/create")]
    public async Task<long> CreateMcp([FromBody] McpCreateInput input)
    {
        var entity = new McpConfigEntity
        {
            F_Id = NewId(), F_Name = input.Name, F_Endpoint = input.Endpoint,
            F_Protocol = input.Protocol ?? "sse", F_AuthType = input.AuthType, F_AuthConfig = input.AuthConfig,
            F_Status = "disconnected", F_Enabled = true,
            F_CreatorTime = DateTime.Now, F_CreatorUserId = UserId(),
        };
        await _db.Insertable(entity).ExecuteCommandAsync();
        return entity.F_Id;
    }

    [HttpPost("mcp/{id}/test")]
    public async Task<object> TestMcp(long id)
    {
        var entity = await _db.Queryable<McpConfigEntity>().Where(x => x.F_Id == id && !x.F_DeleteMark).FirstAsync()
            ?? throw new InvalidOperationException("MCP 配置不存在");

        var start = DateTime.Now;
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await http.GetAsync(entity.F_Endpoint);
            var latency = (DateTime.Now - start).TotalMilliseconds;
            entity.F_Status = response.IsSuccessStatusCode ? "connected" : "error";
            entity.F_LastTestTime = DateTime.Now;
            entity.F_LastTestResult = $"HTTP {(int)response.StatusCode}, {latency:F0}ms";
            await _db.Updateable(entity).UpdateColumns(x => new { x.F_Status, x.F_LastTestTime, x.F_LastTestResult }).ExecuteCommandAsync();
            return new { connected = response.IsSuccessStatusCode, message = entity.F_LastTestResult, latency };
        }
        catch (Exception ex)
        {
            entity.F_Status = "error"; entity.F_LastTestTime = DateTime.Now; entity.F_LastTestResult = ex.Message;
            await _db.Updateable(entity).UpdateColumns(x => new { x.F_Status, x.F_LastTestTime, x.F_LastTestResult }).ExecuteCommandAsync();
            return new { connected = false, message = ex.Message, latency = (DateTime.Now - start).TotalMilliseconds };
        }
    }
}

// ── DTOs ──

public class AgentCreateInput
{
    public string AgentCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AgentType { get; set; } = string.Empty;
    public long? PromptTemplateId { get; set; }
    public string? SystemPrompt { get; set; }
    public string? ModelProvider { get; set; }
    public string? ModelName { get; set; }
    public decimal? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public string? Config { get; set; }
    public int Sort { get; set; }
}

public class AgentUpdateInput
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? SystemPrompt { get; set; }
    public string? ModelProvider { get; set; }
    public string? ModelName { get; set; }
    public decimal? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public string? Config { get; set; }
    public int? Sort { get; set; }
}

public class SkillCreateInput
{
    public long AgentId { get; set; }
    public string SkillCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SkillType { get; set; }
    public string? Config { get; set; }
}

public class SkillUpdateInput
{
    public string? Name { get; set; }
    public string? Config { get; set; }
}

public class McpCreateInput
{
    public string Name { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string? Protocol { get; set; }
    public string? AuthType { get; set; }
    public string? AuthConfig { get; set; }
}
