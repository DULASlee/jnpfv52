namespace JNPF.Modules;

/// <summary>
/// 模块拓扑排序引擎（Kahn 算法）.
/// </summary>
public static class ModuleGraphBuilder
{
    /// <summary>
    /// 对模块类型进行拓扑排序.
    /// </summary>
    /// <param name="moduleTypes">所有模块类型</param>
    /// <returns>拓扑排序后的类型列表（依赖在前，被依赖在后）</returns>
    /// <exception cref="ModuleLoadException">检测到循环依赖</exception>
    public static IReadOnlyList<Type> Build(IEnumerable<Type> moduleTypes)
    {
        var types = moduleTypes.Distinct().ToList();
        var descriptors = types.Select(t => new ModuleDescriptor(t)).ToDictionary(d => d.ModuleType);

        // 构建邻接表和入度表
        var inDegree = new Dictionary<Type, int>();
        var adjacency = new Dictionary<Type, List<Type>>();

        foreach (var descriptor in descriptors.Values)
        {
            if (!inDegree.ContainsKey(descriptor.ModuleType))
                inDegree[descriptor.ModuleType] = 0;

            if (!adjacency.ContainsKey(descriptor.ModuleType))
                adjacency[descriptor.ModuleType] = new List<Type>();

            foreach (var dep in descriptor.Dependencies)
            {
                // 只处理在当前扫描范围内的依赖
                if (!descriptors.ContainsKey(dep)) continue;

                // dep → descriptor.ModuleType（dep 必须先于当前模块加载）
                if (!adjacency.ContainsKey(dep))
                    adjacency[dep] = new List<Type>();

                adjacency[dep].Add(descriptor.ModuleType);
                inDegree[descriptor.ModuleType] = inDegree.GetValueOrDefault(descriptor.ModuleType) + 1;
            }
        }

        // Kahn 算法
        var queue = new Queue<Type>();
        foreach (var (type, degree) in inDegree)
        {
            if (degree == 0)
                queue.Enqueue(type);
        }

        var sorted = new List<Type>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sorted.Add(current);

            if (!adjacency.ContainsKey(current)) continue;

            foreach (var neighbor in adjacency[current])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                    queue.Enqueue(neighbor);
            }
        }

        // 循环依赖检测
        if (sorted.Count != descriptors.Count)
        {
            // 找出参与环路的节点
            var cycleNodes = descriptors.Keys
                .Where(t => !sorted.Contains(t))
                .ToList();

            var cyclePath = BuildCyclePath(cycleNodes, adjacency, descriptors);
            var cycleNames = string.Join(" → ", cyclePath.Select(t => t.Name));

            throw new ModuleLoadException(
                cyclePath.FirstOrDefault()?.Name ?? "Unknown",
                cyclePath,
                $"检测到模块循环依赖: {cycleNames}");
        }

        return sorted;
    }

    /// <summary>
    /// 构建环路路径（用于诊断信息）.
    /// </summary>
    private static IReadOnlyList<Type> BuildCyclePath(
        List<Type> cycleNodes,
        Dictionary<Type, List<Type>> adjacency,
        Dictionary<Type, ModuleDescriptor> descriptors)
    {
        // 简单返回环路节点，附加第一个节点形成闭环
        if (cycleNodes.Count == 0) return Array.Empty<Type>();

        var path = new List<Type>(cycleNodes);
        path.Add(cycleNodes[0]); // 闭环
        return path;
    }
}
