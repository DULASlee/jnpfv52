using JNPF.Common.Core.Manager;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

[ApiDescriptionSettings(Tag = "Studio", Name = "UiTemplate", Order = 199)]
[Route("api/studio/ui-template")]
public class UiTemplateService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly IUserManager _userManager;
    public UiTemplateService(ISqlSugarClient db, IUserManager userManager) { _db = db; _userManager = userManager; }
    private long NewId() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    [HttpGet("market")]
    public async Task<object> GetMarket([FromQuery] string? category = null, [FromQuery] int currentPage = 1, [FromQuery] int pageSize = 20)
    {
        var q = _db.Queryable<UiTemplateEntity>().Where(x => x.F_DeleteMark == null && x.F_Enabled);
        if (!string.IsNullOrEmpty(category)) q = q.Where(x => x.F_Category == category);
        RefAsync<int> t = 0;
        var items = await q.OrderByDescending(x => x.F_UseCount).ToPageListAsync(currentPage, pageSize, t);
        return new { items, total = t.Value };
    }

    [HttpGet("workshop")]
    public async Task<object> GetWorkshop([FromQuery] int currentPage = 1, [FromQuery] int pageSize = 20)
    {
        var uid = long.TryParse(_userManager.UserId, out var id) ? id : 0L;
        RefAsync<int> t = 0;
        var items = await _db.Queryable<UiTemplateEntity>().Where(x => x.F_DeleteMark == null && x.F_DesignerId == uid)
            .ToPageListAsync(currentPage, pageSize, t);
        return new { items, total = t.Value };
    }

    [HttpPost("create")]
    public async Task<long> Create([FromBody] UiTemplateCreateInput i)
    {
        var uid = long.TryParse(_userManager.UserId, out var id) ? id : 0L;
        var e = new UiTemplateEntity { F_Id = NewId(), F_TenantId = _userManager.TenantId, F_Name = i.Name, F_Description = i.Description, F_Category = i.Category, F_ThumbnailUrl = i.ThumbnailUrl, F_TemplateData = i.TemplateData, F_Source = "community", F_DesignerId = uid, F_UseCount = 0, F_Rating = 5.0m, F_Enabled = true, F_CreatorTime = DateTime.Now, F_CreatorUserId = uid };
        await _db.Insertable(e).ExecuteCommandAsync();
        return e.F_Id;
    }

    [HttpPut("{id}/update")]
    public async Task Update(long id, [FromBody] UiTemplateCreateInput i) { await _db.Updateable<UiTemplateEntity>().SetColumns(it => it.F_Name, i.Name).SetColumns(it => it.F_Description, i.Description).SetColumns(it => it.F_ModifyTime, DateTime.Now).Where(it => it.F_Id == id).ExecuteCommandAsync(); }

    [HttpDelete("{id}/delete")]
    public async Task Delete(long id) { await _db.Updateable<UiTemplateEntity>().SetColumns(it => it.F_DeleteMark, 1).Where(it => it.F_Id == id).ExecuteCommandAsync(); }
}

public class UiTemplateCreateInput { public string Name { get; set; } = string.Empty; public string? Description { get; set; } public string? Category { get; set; } public string? ThumbnailUrl { get; set; } public string TemplateData { get; set; } = string.Empty; }
