using System.Text.RegularExpressions;
using JNPF.Systems.Entitys.System;
using JNPF.VisualDev.Entitys.Dto.VisualDev;

namespace JNPF.VisualDev.Query;

/// <summary>
/// 在线开发列表「模块引用路径」倒排索引。
/// 语义契约：功能 id 在模块 PropertyJson 中以独立 19 位数字 token 出现（JSON 字符串值或数字）；
/// 以 O(模块 JSON 总字节数) 的一次扫描替代 O(模块数 × 行数 × JSON 长度) 的 Contains 匹配，
/// 并保持「同一模块只计入列表顺序中第一个命中的功能」的原始行为。
/// </summary>
public static class VisualDevListRefIndex
{
    private static readonly Regex IdTokenRegex = new(@"(?<!\d)\d{19}(?!\d)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// 构建 功能id → 引用路径集合 的倒排索引。
    /// </summary>
    /// <param name="modules">当前页引用的候选模块（Web/App 已按类别过滤）。</param>
    /// <param name="items">当前页功能列表（顺序即列表顺序）。</param>
    /// <param name="getPath">模块id → 已预计算的路径；返回 null 时回退模块 FullName。</param>
    public static Dictionary<string, HashSet<string>> BuildRefIndex(
        IReadOnlyList<ModuleEntity> modules,
        IReadOnlyList<VisualDevListOutput> items,
        Func<string, string?> getPath)
    {
        var refMap = new Dictionary<string, HashSet<string>>();
        if (items.Count == 0)
            return refMap;

        var pageIds = new HashSet<string>(items.Select(x => x.id));
        foreach (var module in modules)
        {
            if (string.IsNullOrEmpty(module.PropertyJson))
                continue;

            // 扫描一次 JSON，收集本模块引用到的、且在本页内的功能 id。
            var matched = new HashSet<string>();
            foreach (Match match in IdTokenRegex.Matches(module.PropertyJson))
            {
                if (pageIds.Contains(match.Value))
                    matched.Add(match.Value);
            }
            if (matched.Count == 0)
                continue;

            // 保持原始行为：同一模块只计入列表顺序中第一个命中的功能。
            string? firstId = null;
            foreach (var item in items)
            {
                if (matched.Contains(item.id))
                {
                    firstId = item.id;
                    break;
                }
            }
            if (firstId is null)
                continue;

            var path = getPath(module.Id);
            if (string.IsNullOrEmpty(path))
                path = module.FullName;
            if (string.IsNullOrEmpty(path))
                continue;

            if (!refMap.TryGetValue(firstId, out var set))
                refMap[firstId] = set = new HashSet<string>();
            set.Add(path);
        }

        return refMap;
    }
}
