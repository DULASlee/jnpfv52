using JNPF.Common.Core.Manager;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

[ApiDescriptionSettings(Tag = "Studio", Name = "Eval", Order = 201)]
[Route("api/studio/eval")]
public class EvalService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly IUserManager _userManager;
    public EvalService(ISqlSugarClient db, IUserManager userManager) { _db = db; _userManager = userManager; }
    private long NewId() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    private long? UserId() => long.TryParse(_userManager.UserId, out var id) ? id : null;

    [HttpGet("golden-set")]
    public async Task<object> GetGoldenSets([FromQuery] string? domain = null, [FromQuery] int currentPage = 1, [FromQuery] int pageSize = 20)
    {
        var q = _db.Queryable<EvalGoldenSetEntity>().Where(x => x.F_DeleteMark == null && x.F_Enabled);
        if (!string.IsNullOrEmpty(domain)) q = q.Where(x => x.F_Domain == domain);
        RefAsync<int> t = 0;
        var items = await q.OrderByDescending(x => x.F_CreatorTime).ToPageListAsync(currentPage, pageSize, t);
        return new { items, total = t.Value };
    }

    [HttpPost("golden-set/create")]
    public async Task<long> CreateGoldenSet([FromBody] GoldenSetCreateInput i) { var e = new EvalGoldenSetEntity { F_Id = NewId(), F_Name = i.Name, F_Description = i.Description, F_Domain = i.Domain, F_Enabled = true, F_CreatorTime = DateTime.Now, F_CreatorUserId = UserId() }; await _db.Insertable(e).ExecuteCommandAsync(); return e.F_Id; }

    [HttpGet("golden-set/{setId}/cases")]
    public async Task<object> GetCases(long setId) { var items = await _db.Queryable<EvalCaseEntity>().Where(x => x.F_SetId == setId && x.F_DeleteMark == null).OrderBy(x => x.F_Stage).ToListAsync(); return new { items, total = items.Count }; }

    [HttpPost("case/create")]
    public async Task<long> CreateCase([FromBody] EvalCaseCreateInput i) { var e = new EvalCaseEntity { F_Id = NewId(), F_SetId = i.SetId, F_Name = i.Name, F_Requirement = i.Requirement, F_ExpectedIR = i.ExpectedIR, F_Stage = i.Stage, F_ScoreThreshold = i.ScoreThreshold ?? 0.8m, F_Enabled = true, F_CreatorTime = DateTime.Now, F_CreatorUserId = UserId() }; await _db.Insertable(e).ExecuteCommandAsync(); /* test case count updated async */ return e.F_Id; }

    [HttpPost("run")]
    public async Task<object> RunEval([FromBody] EvalRunInput i) { var cases = await _db.Queryable<EvalCaseEntity>().Where(x => x.F_SetId == i.SetId && x.F_DeleteMark == null && x.F_Enabled).ToListAsync(); if (cases.Count == 0) throw new InvalidOperationException("无可用测试用例"); var run = new EvalRunEntity { F_Id = NewId(), F_SetId = i.SetId, F_RunAt = DateTime.Now, F_TotalCases = cases.Count, F_PassedCases = 0, F_Details = "{\"status\":\"pending\"}", F_CreatorTime = DateTime.Now, F_CreatorUserId = UserId() }; await _db.Insertable(run).ExecuteCommandAsync(); return new { runId = run.F_Id, totalCases = cases.Count, status = "pending" }; }

    [HttpGet("history")]
    public async Task<object> GetHistory([FromQuery] long? setId = null, [FromQuery] int currentPage = 1, [FromQuery] int pageSize = 20) { var q = _db.Queryable<EvalRunEntity>(); if (setId.HasValue) q = q.Where(x => x.F_SetId == setId.Value); RefAsync<int> t = 0; var items = await q.OrderByDescending(x => x.F_RunAt).ToPageListAsync(currentPage, pageSize, t); return new { items, total = t.Value }; }
}

public class GoldenSetCreateInput { public string Name { get; set; } = string.Empty; public string? Description { get; set; } public string? Domain { get; set; } }
public class EvalCaseCreateInput { public long SetId { get; set; } public string Name { get; set; } = string.Empty; public string Requirement { get; set; } = string.Empty; public string? ExpectedIR { get; set; } public int? Stage { get; set; } public decimal? ScoreThreshold { get; set; } }
public class EvalRunInput { public long SetId { get; set; } }
