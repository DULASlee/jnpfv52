using JNPF.Common.Core.Manager;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// 已生成系统服务 — 流水线产出查询 + 红点联动 (Sprint 1)
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "GeneratedProject", Order = 192)]
[Route("api/studio/ai")]
public class GeneratedProjectService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly IUserManager _userManager;
    private readonly StudioMenuService _menuService;

    private const long GENERATED_SYSTEMS_MENU_ID = 100000102;

    public GeneratedProjectService(ISqlSugarClient db, IUserManager userManager, StudioMenuService menuService)
    {
        _db = db;
        _userManager = userManager;
        _menuService = menuService;
    }

    /// <summary>
    /// 获取已生成系统列表
    /// </summary>
    [HttpGet("project/list")]
    public async Task<object> GetProjectList(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        RefAsync<int> total = 0;
        var userId = long.TryParse(_userManager.UserId, out var id) ? id : 0L;

        var query = _db.Queryable<GeneratedProjectEntity>()
            .Where(p => !p.F_DeleteMark && p.F_TenantId == _userManager.TenantId);

        // 非管理员只能看自己的
        if (!_userManager.IsAdministrator)
            query = query.Where(p => p.F_UserId == userId);

        var items = await query.OrderByDescending(p => p.F_ModifyTime ?? p.F_CreatorTime)
            .ToPageListAsync(page, pageSize, total);

        return new
        {
            total = total.Value,
            items = items.Select(p => new
            {
                id = p.F_Id,
                projectName = p.F_ProjectName,
                description = p.F_Description,
                currentStage = p.F_CurrentStage,
                pipelineStatus = p.F_PipelineStatus,
                sandboxUrl = p.F_SandboxUrl,
                sourceZipUrl = p.F_SourceZipUrl,
                createTime = p.F_CreatorTime,
                updateCount = p.F_UpdateCount,
                isRead = p.F_IsRead,
            }).ToList(),
        };
    }

    /// <summary>
    /// 标记已读（清除红点）
    /// </summary>
    [HttpPost("project/{id}/mark-read")]
    public async Task MarkRead(long id)
    {
        var userId = long.TryParse(_userManager.UserId, out var uid) ? uid : 0L;
        var project = await _db.Queryable<GeneratedProjectEntity>()
            .Where(p => p.F_Id == id && p.F_TenantId == _userManager.TenantId && p.F_UserId == userId)
            .FirstAsync();

        if (project == null) return;

        project.F_IsRead = true;
        project.F_UpdateCount = 0;
        project.F_ModifyTime = DateTime.Now;
        await _db.Updateable(project).ExecuteCommandAsync();

        // 清除菜单红点
        await _menuService.IncrementBadge(GENERATED_SYSTEMS_MENU_ID, userId, _userManager.TenantId, -9999);
    }

    /// <summary>
    /// 流水线完成回调 — 新增/更新已生成系统 + 触发红点
    /// </summary>
    public async Task OnPipelineCompleted(long pipelineId, long projectId, string tenantId, long userId)
    {
        await _menuService.IncrementBadge(GENERATED_SYSTEMS_MENU_ID, userId, tenantId, 1);
        await _db.Ado.ExecuteCommandAsync(
            @"UPDATE BASE_AI_GENERATED_PROJECT SET F_IsRead=0, F_UpdateCount=F_UpdateCount+1, F_ModifyTime=GETDATE()
              WHERE F_Id=@pid AND F_TenantId=@tid AND F_UserId=@uid",
            new { pid = projectId, tid = tenantId, uid = userId });
    }
}
