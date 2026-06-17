using System.Text.Json;
using JNPF.Common.Core.Manager;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// Studio 菜单服务 — 核心权限过滤 + 动态菜单树 (Sprint 1)
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "StudioMenu", Order = 190)]
[Route("api/studio/menu")]
public class StudioMenuService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly IUserManager _userManager;

    public StudioMenuService(ISqlSugarClient db, IUserManager userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    /// <summary>
    /// 获取当前用户可见的菜单树
    /// </summary>
    [HttpGet("user-menus")]
    public async Task<List<StudioMenuDto>> GetUserMenus()
    {
        // 1. 获取用户角色列表
        var userRoles = await GetUserRolesAsync();

        // 2. 获取所有启用且未删除的菜单
        var allMenus = await _db.Queryable<StudioMenuEntity>()
            .Where(m => !m.F_DeleteMark && m.F_Enabled)
            .OrderBy(m => m.F_Sort)
            .ToListAsync();

        // 3. 权限过滤（管理员跳过角色检查，返回全部菜单）
        var visibleMenus = _userManager.IsAdministrator
            ? allMenus
            : allMenus.Where(m => IsMenuVisible(m, userRoles)).ToList();

        // 4. 红点数据（menuId=100000102=已生成系统）
        var badges = await _db.Queryable<MenuBadgeEntity>()
            .Where(b => b.F_UserId.ToString() == _userManager.UserId
                     && b.F_Count > 0)
            .ToListAsync();

        // 5. 构建树
        return BuildMenuTree(visibleMenus, 0, badges);
    }

    /// <summary>
    /// 标记菜单已读（清除红点）
    /// </summary>
    [HttpPost("badge/read")]
    public async Task MarkBadgeRead([FromBody] MarkBadgeReadInput input)
    {
        var userId = long.TryParse(_userManager.UserId, out var id) ? id : 0L;
        var badge = await _db.Queryable<MenuBadgeEntity>()
            .Where(b => b.F_MenuId == input.MenuId && b.F_UserId == userId
                     && b.F_TenantId == _userManager.TenantId)
            .FirstAsync();

        if (badge != null)
        {
            badge.F_Count = 0;
            badge.F_ModifyTime = DateTime.Now;
            await _db.Updateable(badge).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 增加红点计数（供流水线完成时调用）
    /// </summary>
    public async Task IncrementBadge(long menuId, long userId, string tenantId, int increment = 1)
    {
        var existing = await _db.Queryable<MenuBadgeEntity>()
            .Where(b => b.F_MenuId == menuId && b.F_UserId == userId && b.F_TenantId == tenantId)
            .FirstAsync();

        if (existing != null)
        {
            existing.F_Count = Math.Max(0, existing.F_Count + increment);
            existing.F_ModifyTime = DateTime.Now;
            await _db.Updateable(existing).ExecuteCommandAsync();
        }
        else if (increment > 0)
        {
            await _db.Insertable(new MenuBadgeEntity
            {
                F_Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                F_MenuId = menuId,
                F_UserId = userId,
                F_TenantId = tenantId,
                F_Count = increment,
                F_CreatorTime = DateTime.Now
            }).ExecuteCommandAsync();
        }
    }

    #region Private

    private bool IsMenuVisible(StudioMenuEntity menu, List<string> userRoles)
    {
        if (menu.F_IsPublic) return true;
        if (string.IsNullOrWhiteSpace(menu.F_RequiredRoles)) return true;

        try
        {
            var requiredRoles = JsonSerializer.Deserialize<List<string>>(menu.F_RequiredRoles) ?? new();
            if (requiredRoles.Count == 0) return true;
            return requiredRoles.Any(r => userRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
        }
        catch { return false; }
    }

    private List<StudioMenuDto> BuildMenuTree(List<StudioMenuEntity> all, long parentId, List<MenuBadgeEntity> badges)
    {
        return all.Where(m => m.F_ParentId == parentId).Select(m => new StudioMenuDto
        {
            Id = m.F_Id,
            ParentId = m.F_ParentId,
            Name = m.F_Name,
            Icon = m.F_Icon,
            Url = m.F_Url,
            Sort = m.F_Sort,
            Comment = m.F_Comment,
            DataScope = m.F_DataScope,
            ExpandPhase = m.F_ExpandPhase,
            BadgeCount = badges.Where(b => b.F_MenuId == m.F_Id).Sum(b => b.F_Count),
            Children = BuildMenuTree(all, m.F_Id, badges)
        }).ToList();
    }

    private async Task<List<string>> GetUserRolesAsync()
    {
        var userId = _userManager.UserId;
        var dt = await _db.Ado.GetDataTableAsync(
            @"SELECT r.f_en_code FROM base_role r
              INNER JOIN base_user_relation ur ON r.f_id = ur.f_object_id AND ur.f_object_type = 'Role'
              WHERE ur.f_user_id = @uid AND r.f_delete_mark = 0 AND r.f_enabled_mark = 1",
            new SugarParameter("@uid", userId));
        return dt.Rows.Cast<System.Data.DataRow>().Select(r => r["f_en_code"]?.ToString() ?? "").ToList();
    }

    #endregion
}
