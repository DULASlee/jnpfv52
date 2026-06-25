using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

[ApiDescriptionSettings(Tag = "Studio", Name = "SandboxConfig", Order = 203)]
[Route("api/studio/knowledge")]
public class SandboxConfigService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    public SandboxConfigService(ISqlSugarClient db) { _db = db; }

    [HttpGet("sandbox-config")]
    public async Task<object> GetConfig()
    {
        var active = await _db.Ado.GetIntAsync("SELECT COUNT(*) FROM BASE_SANDBOX WHERE F_Status='running' AND F_DeleteMark=0");
        var total = await _db.Ado.GetIntAsync("SELECT COUNT(*) FROM BASE_SANDBOX WHERE F_DeleteMark=0");
        return new
        {
            defaults = new { cpuCount = 1, memoryMb = 1024, timeoutSeconds = 300, maxConcurrency = 5, dbStrategy = "shared", autoDestroy = true },
            current = new { activeInstances = active, totalInstances = total },
        };
    }

    [HttpPut("sandbox-config/update")]
    public Task UpdateConfig([FromBody] SandboxGlobalConfigInput input) => Task.CompletedTask;

    [HttpGet("sandbox/list")]
    public async Task<object> GetInstances([FromQuery] string? status = null, [FromQuery] int currentPage = 1, [FromQuery] int pageSize = 20)
    {
        var sql = "SELECT * FROM BASE_SANDBOX WHERE F_DeleteMark=0";
        if (!string.IsNullOrEmpty(status)) sql += " AND F_Status=@status";
        sql += " ORDER BY F_CreatorTime DESC";
        var dt = await _db.Ado.GetDataTableAsync(sql, string.IsNullOrEmpty(status) ? Array.Empty<SugarParameter>() : new[] { new SugarParameter("@status", status) });
        return new { items = dt, total = dt.Rows.Count };
    }
}

public class SandboxGlobalConfigInput { public int? CpuCount { get; set; } public int? MemoryMb { get; set; } public int? TimeoutSeconds { get; set; } public int? MaxConcurrency { get; set; } public string? DbStrategy { get; set; } public bool? AutoDestroy { get; set; } }
