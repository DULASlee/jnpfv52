using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.System;

namespace JNPF.VisualDev.Release;

/// <summary>
/// Pure planner for FuncToMenu release targets (Web/App → systemId/parentId pairs).
/// Extracted from VisualDevService.FuncToMenu — behavior-preserving.
/// </summary>
public static class FuncToMenuReleasePlanner
{
    /// <summary>
    /// Build platform → list of { systemId → parentId } maps for menu publish.
    /// </summary>
    public static Dictionary<string, List<Dictionary<string, string>>> BuildReleaseTargets(
        bool publishPc,
        bool publishApp,
        IReadOnlyCollection<string> pcModuleParentIds,
        IReadOnlyCollection<string> appModuleParentIds,
        IReadOnlyCollection<string> sysIdList,
        IReadOnlyList<ModuleEntity> modulesLinkedToFeature,
        IReadOnlyList<ModuleEntity> allModules)
    {
        var release = new Dictionary<string, List<Dictionary<string, string>>>();
        var sysIds = sysIdList as HashSet<string> ?? new HashSet<string>(sysIdList);

        if (publishPc)
        {
            release["Web"] = BuildPlatformEntries(
                "Web",
                pcModuleParentIds,
                sysIds,
                modulesLinkedToFeature,
                allModules);
        }

        if (publishApp)
        {
            release["App"] = BuildPlatformEntries(
                "App",
                appModuleParentIds,
                sysIds,
                modulesLinkedToFeature,
                allModules);
        }

        return release;
    }

    private static List<Dictionary<string, string>> BuildPlatformEntries(
        string category,
        IReadOnlyCollection<string> parentIds,
        HashSet<string> sysIds,
        IReadOnlyList<ModuleEntity> modulesLinkedToFeature,
        IReadOnlyList<ModuleEntity> allModules)
    {
        var dic = new List<Dictionary<string, string>>();

        foreach (var item in modulesLinkedToFeature.Where(it => it.Category.Equals(category)))
            dic.Add(new Dictionary<string, string> { { item.SystemId, item.ParentId } });

        foreach (var item in parentIds)
        {
            if (sysIds.Contains(item))
            {
                dic.Add(new Dictionary<string, string> { { item, "-1" } });
            }
            else
            {
                var module = allModules.FirstOrDefault(it => it.Id.Equals(item));
                if (module.IsNullOrEmpty())
                    throw Oops.Oh(ErrorCode.D4021);
                dic.Add(new Dictionary<string, string> { { module.SystemId, module.Id } });
            }
        }

        return dic;
    }
}
