using JNPF.DependencyInjection;
using JNPF.Systems.Entitys.Dto.Permission.OrganizeAdministrator;
using JNPF.Systems.Entitys.Permission;

namespace JNPF.Systems.Common.OrganizeAdmin;

/// <summary>
/// Pure helpers for OrganizeAdministratorService.GetSelector
/// (permission flag merge/map, -1 key strip, org→selector node, tree gap repair).
/// DB queries stay in the service.
/// </summary>
[SuppressSniffer]
public static class OrganizeAdminSelectorHelpers
{
    /// <summary>
    /// True when any of the 8 this/sub layer permission flags equals 1.
    /// </summary>
    public static bool HasAnyLayerPermission(OrganizeAdministratorEntity entity)
    {
        return entity.ThisLayerAdd.Equals(1) || entity.ThisLayerEdit.Equals(1)
            || entity.ThisLayerDelete.Equals(1) || entity.ThisLayerSelect.Equals(1)
            || entity.SubLayerAdd.Equals(1) || entity.SubLayerEdit.Equals(1)
            || entity.SubLayerDelete.Equals(1) || entity.SubLayerSelect.Equals(1);
    }

    /// <summary>
    /// True when any of the 4 sub-layer permission flags equals 1.
    /// </summary>
    public static bool HasAnySubLayerPermission(OrganizeAdministratorEntity entity)
    {
        return entity.SubLayerAdd.Equals(1) || entity.SubLayerEdit.Equals(1)
            || entity.SubLayerDelete.Equals(1) || entity.SubLayerSelect.Equals(1);
    }

    /// <summary>
    /// UI merge of admin vs target-user flag for one permission cell.
    /// Branches: both 0 → -1; admin(1|3)+user1 → 1; admin(1|3)+user0 → 0;
    /// admin0+user1 → 2; admin0+user3 → 3; else → 0 (unset).
    /// </summary>
    public static int MergeAdminUserPermissionFlag(int adminVal, int userVal)
    {
        if (adminVal.Equals(0) && userVal.Equals(0)) return -1;
        if ((adminVal.Equals(1) || adminVal.Equals(3)) && userVal.Equals(1)) return 1;
        if ((adminVal.Equals(1) || adminVal.Equals(3)) && userVal.Equals(0)) return 0;
        if (adminVal.Equals(0) && userVal.Equals(1)) return 2;
        if (adminVal.Equals(0) && userVal.Equals(3)) return 3;
        return 0;
    }

    /// <summary>
    /// Apply MergeAdminUserPermissionFlag across all 8 permission fields on the selector DTO.
    /// </summary>
    public static void ApplyMergedAdminUserPermissionFlags(
        OrganizeAdministratorEntity admin,
        OrganizeAdministratorEntity user,
        OrganizeAdministratorSelectorOutput output)
    {
        output.thisLayerAdd = MergeAdminUserPermissionFlag(admin.ThisLayerAdd, user.ThisLayerAdd);
        output.thisLayerEdit = MergeAdminUserPermissionFlag(admin.ThisLayerEdit, user.ThisLayerEdit);
        output.thisLayerDelete = MergeAdminUserPermissionFlag(admin.ThisLayerDelete, user.ThisLayerDelete);
        output.thisLayerSelect = MergeAdminUserPermissionFlag(admin.ThisLayerSelect, user.ThisLayerSelect);
        output.subLayerAdd = MergeAdminUserPermissionFlag(admin.SubLayerAdd, user.SubLayerAdd);
        output.subLayerEdit = MergeAdminUserPermissionFlag(admin.SubLayerEdit, user.SubLayerEdit);
        output.subLayerDelete = MergeAdminUserPermissionFlag(admin.SubLayerDelete, user.SubLayerDelete);
        output.subLayerSelect = MergeAdminUserPermissionFlag(admin.SubLayerSelect, user.SubLayerSelect);
    }

    /// <summary>
    /// Admin-only visible cell: admin has 1|3 → 0 (unchecked editable), else → -1 (hidden).
    /// </summary>
    public static int MapAdminOnlyPermissionFlag(int adminVal)
    {
        return adminVal.Equals(1) || adminVal.Equals(3) ? 0 : -1;
    }

    /// <summary>
    /// Apply MapAdminOnlyPermissionFlag across all 8 permission fields.
    /// </summary>
    public static void ApplyAdminOnlyPermissionFlags(
        OrganizeAdministratorEntity admin,
        OrganizeAdministratorSelectorOutput output)
    {
        output.thisLayerAdd = MapAdminOnlyPermissionFlag(admin.ThisLayerAdd);
        output.thisLayerEdit = MapAdminOnlyPermissionFlag(admin.ThisLayerEdit);
        output.thisLayerDelete = MapAdminOnlyPermissionFlag(admin.ThisLayerDelete);
        output.thisLayerSelect = MapAdminOnlyPermissionFlag(admin.ThisLayerSelect);
        output.subLayerAdd = MapAdminOnlyPermissionFlag(admin.SubLayerAdd);
        output.subLayerEdit = MapAdminOnlyPermissionFlag(admin.SubLayerEdit);
        output.subLayerDelete = MapAdminOnlyPermissionFlag(admin.SubLayerDelete);
        output.subLayerSelect = MapAdminOnlyPermissionFlag(admin.SubLayerSelect);
    }

    /// <summary>
    /// User-only path (no admin row for org): 0 → -1; 1 → 2; 3 (or 1) → 3; else → 0.
    /// Third branch keeps legacy <c>Equals(3) || Equals(1)</c> wording (1 already handled above).
    /// </summary>
    public static int MapUserOnlyPermissionFlag(int userVal)
    {
        if (userVal.Equals(0)) return -1;
        if (userVal.Equals(1)) return 2;
        if (userVal.Equals(3) || userVal.Equals(1)) return 3;
        return 0;
    }

    /// <summary>
    /// Apply MapUserOnlyPermissionFlag across all 8 permission fields.
    /// </summary>
    public static void ApplyUserOnlyPermissionFlags(
        OrganizeAdministratorEntity user,
        OrganizeAdministratorSelectorOutput output)
    {
        output.thisLayerAdd = MapUserOnlyPermissionFlag(user.ThisLayerAdd);
        output.thisLayerEdit = MapUserOnlyPermissionFlag(user.ThisLayerEdit);
        output.thisLayerDelete = MapUserOnlyPermissionFlag(user.ThisLayerDelete);
        output.thisLayerSelect = MapUserOnlyPermissionFlag(user.ThisLayerSelect);
        output.subLayerAdd = MapUserOnlyPermissionFlag(user.SubLayerAdd);
        output.subLayerEdit = MapUserOnlyPermissionFlag(user.SubLayerEdit);
        output.subLayerDelete = MapUserOnlyPermissionFlag(user.SubLayerDelete);
        output.subLayerSelect = MapUserOnlyPermissionFlag(user.SubLayerSelect);
    }

    /// <summary>
    /// Sub-layer expand inheritance for one flag.
    /// <paramref name="inheritAs"/> is 3 for GetSelector UI; Save uses 1 — do not unify blindly.
    /// </summary>
    public static int ResolveExpandedFlag(bool hasExisting, int existingFlag, int parentSubLayerFlag, int inheritAs = 3)
    {
        if (!hasExisting)
            return parentSubLayerFlag.Equals(1) ? inheritAs : 0;
        if (existingFlag.Equals(1) || existingFlag.Equals(3))
            return existingFlag;
        return parentSubLayerFlag.Equals(1) ? inheritAs : 0;
    }

    /// <summary>
    /// Copy inherited sub-layer permissions onto <paramref name="target"/> (Selector inheritAs=3).
    /// </summary>
    public static void ApplyInheritedSubLayerFlags(
        OrganizeAdministratorEntity target,
        OrganizeAdministratorEntity? existing,
        OrganizeAdministratorEntity parentWithSubLayer,
        int inheritAs = 3)
    {
        var hasExisting = existing != null;
        target.ThisLayerAdd = ResolveExpandedFlag(hasExisting, existing?.ThisLayerAdd ?? 0, parentWithSubLayer.SubLayerAdd, inheritAs);
        target.ThisLayerEdit = ResolveExpandedFlag(hasExisting, existing?.ThisLayerEdit ?? 0, parentWithSubLayer.SubLayerEdit, inheritAs);
        target.ThisLayerDelete = ResolveExpandedFlag(hasExisting, existing?.ThisLayerDelete ?? 0, parentWithSubLayer.SubLayerDelete, inheritAs);
        target.ThisLayerSelect = ResolveExpandedFlag(hasExisting, existing?.ThisLayerSelect ?? 0, parentWithSubLayer.SubLayerSelect, inheritAs);
        target.SubLayerAdd = ResolveExpandedFlag(hasExisting, existing?.SubLayerAdd ?? 0, parentWithSubLayer.SubLayerAdd, inheritAs);
        target.SubLayerEdit = ResolveExpandedFlag(hasExisting, existing?.SubLayerEdit ?? 0, parentWithSubLayer.SubLayerEdit, inheritAs);
        target.SubLayerDelete = ResolveExpandedFlag(hasExisting, existing?.SubLayerDelete ?? 0, parentWithSubLayer.SubLayerDelete, inheritAs);
        target.SubLayerSelect = ResolveExpandedFlag(hasExisting, existing?.SubLayerSelect ?? 0, parentWithSubLayer.SubLayerSelect, inheritAs);
    }

    /// <summary>
    /// Map organize entity to selector tree node shell (ids / name / icon / tree path).
    /// </summary>
    public static OrganizeAdministratorSelectorOutput MapOrganizeToSelectorNode(
        OrganizeEntity org,
        bool useDescriptionAsFullName)
    {
        return new OrganizeAdministratorSelectorOutput
        {
            id = org.Id,
            organizeId = org.Id,
            fullName = useDescriptionAsFullName ? org.Description : org.FullName,
            parentId = org.ParentId,
            category = org.Category,
            icon = org.Category.Equals("company")
                ? "icon-ym icon-ym-tree-organization3"
                : "icon-ym icon-ym-tree-department1",
            organizeIdTree = org.OrganizeIdTree,
        };
    }

    /// <summary>
    /// Remove dictionary keys whose value equals -1 (hidden permission cells).
    /// Mutates <paramref name="nodes"/> in place.
    /// </summary>
    public static void StripNegativePermissionKeys(IList<Dictionary<string, object>> nodes)
    {
        foreach (var item in nodes)
        {
            if (!item.ContainsValue(-1)) continue;
            foreach (var key in item.Where(x => x.Value.Equals(-1)).Select(x => x.Key).ToList())
                item.Remove(key);
        }
    }

    /// <summary>
    /// Repair missing parent links in the flat selector list (断层): re-parent to nearest
    /// ancestor still in the list, or root (-1); strip ancestor prefix from fullName.
    /// </summary>
    public static void RepairOrgSelectorTreeGaps(List<OrganizeAdministratorSelectorOutput> result)
    {
        result.Where(x => x.parentId != "-1").OrderByDescending(x => x.organizeIdTree.Length).ToList().ForEach(item =>
        {
            if (!result.Any(x => x.id.Equals(item.parentId)))
            {
                var pItem = result.Find(x => x.id != item.id && item.organizeIdTree.Contains(x.organizeIdTree));
                if (pItem != null)
                {
                    item.parentId = pItem.id;
                    item.fullName = item.fullName.Replace(pItem.fullName + "/", string.Empty);
                }
                else
                {
                    item.parentId = "-1";
                }
            }
            else
            {
                var pItem = result.Find(x => x.id.Equals(item.parentId));
                item.fullName = item.fullName.Replace(pItem.fullName + "/", string.Empty);
            }
        });
    }
}
