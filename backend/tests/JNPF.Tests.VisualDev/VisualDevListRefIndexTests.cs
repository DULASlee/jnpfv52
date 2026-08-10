using System.Diagnostics;
using System.Text;
using JNPF.Systems.Entitys.System;
using JNPF.VisualDev.Entitys.Dto.VisualDev;
using JNPF.VisualDev.Query;
using Xunit;

namespace JNPF.Tests.VisualDev;

/// <summary>
/// VisualDevListRefIndex 等价性与性能验证：
/// 倒排索引必须与原始 O(n×m) Contains 扫描在既定语义契约下完全等价，且不能出现回归。
/// </summary>
public class VisualDevListRefIndexTests
{
    /// <summary>原始实现（oracle）：逐模块 × 逐行 Contains，首中即 break。</summary>
    private static Dictionary<string, HashSet<string>> Oracle(
        IReadOnlyList<ModuleEntity> modules,
        IReadOnlyList<VisualDevListOutput> items,
        Func<string, string?> getPath)
    {
        var refMap = new Dictionary<string, HashSet<string>>();
        foreach (var module in modules)
        {
            if (string.IsNullOrEmpty(module.PropertyJson)) continue;
            var path = getPath(module.Id) ?? module.FullName;
            foreach (var item in items)
            {
                if (module.PropertyJson.Contains(item.id))
                {
                    if (!refMap.TryGetValue(item.id, out var set))
                        refMap[item.id] = set = new HashSet<string>();
                    set.Add(path);
                    break;
                }
            }
        }
        return refMap;
    }

    private static string Id(long n) => n.ToString("D19");

    private static VisualDevListOutput Item(string id) => new() { id = id };

    private static ModuleEntity Module(string id, string? propertyJson, string category = "Web", string? fullName = null) =>
        new()
        {
            Id = id,
            Category = category,
            PropertyJson = propertyJson,
            FullName = fullName ?? $"模块/{id}",
        };

    [Fact]
    public void 空页或空模块集合_返回空索引()
    {
        var emptyItems = new List<VisualDevListOutput>();
        var modules = new List<ModuleEntity> { Module("m1", $"{{\"id\":\"{Id(1)}\"}}") };
        Assert.Empty(VisualDevListRefIndex.BuildRefIndex(modules, emptyItems, _ => null));

        var items = new List<VisualDevListOutput> { Item(Id(1)) };
        Assert.Empty(VisualDevListRefIndex.BuildRefIndex(new List<ModuleEntity>(), items, _ => null));
    }

    [Fact]
    public void PropertyJson为空_跳过()
    {
        var items = new List<VisualDevListOutput> { Item(Id(1)) };
        var modules = new List<ModuleEntity>
        {
            Module("m1", null),
            Module("m2", string.Empty),
        };
        Assert.Empty(VisualDevListRefIndex.BuildRefIndex(modules, items, _ => null));
    }

    [Fact]
    public void 与Oracle在随机语料上等价()
    {
        var rand = new Random(20260808);
        var allIds = Enumerable.Range(0, 80).Select(i => Id(i)).ToList();
        var pageIds = allIds.Take(20).ToList();
        var items = pageIds.Select(Item).ToList();
        var modules = new List<ModuleEntity>();

        for (var i = 0; i < 400; i++)
        {
            var sb = new StringBuilder("{");
            var embedded = rand.Next(0, 4);
            for (var e = 0; e < embedded; e++)
            {
                var pick = allIds[rand.Next(allIds.Count)];
                var wrapper = (e % 3) switch
                {
                    0 => $"\"field{e}\":\"{pick}\"",
                    1 => $"\"field{e}\":{pick}",
                    _ => $"\"field{e}\":\"prefix-{pick}-suffix\"",
                };
                if (e > 0) sb.Append(',');
                sb.Append(wrapper);
            }
            sb.Append('}');
            modules.Add(Module($"m{i}", sb.ToString(), i % 2 == 0 ? "Web" : "App", $"路径/{i}"));
        }

        var expected = Oracle(modules, items, _ => null);
        var actual = VisualDevListRefIndex.BuildRefIndex(modules, items, _ => null);

        Assert.Equal(expected.Keys.OrderBy(x => x), actual.Keys.OrderBy(x => x));
        foreach (var kv in expected)
            Assert.Equal(kv.Value.OrderBy(x => x), actual[kv.Key].OrderBy(x => x));
    }

    [Fact]
    public void 同一模块引用多个功能_只计入列表顺序第一个()
    {
        var items = new List<VisualDevListOutput> { Item(Id(1)), Item(Id(2)) };
        var json = $"{{\"a\":\"{Id(2)}\",\"b\":\"{Id(1)}\"}}";
        var modules = new List<ModuleEntity> { Module("m1", json) };

        var actual = VisualDevListRefIndex.BuildRefIndex(modules, items, _ => null);
        Assert.Single(actual);
        Assert.Contains(Id(1), actual.Keys);
    }

    [Fact]
    public void getPath为空时_回退FullName()
    {
        var items = new List<VisualDevListOutput> { Item(Id(1)) };
        var modules = new List<ModuleEntity>
        {
            Module("m1", $"{{\"id\":\"{Id(1)}\"}}", fullName: "兜底路径"),
        };

        var actual = VisualDevListRefIndex.BuildRefIndex(modules, items, _ => null);
        Assert.Equal(new[] { "兜底路径" }, actual[Id(1)]);
    }

    [Fact]
    public void getPath命中时_优先使用缓存路径()
    {
        var items = new List<VisualDevListOutput> { Item(Id(1)) };
        var modules = new List<ModuleEntity>
        {
            Module("m1", $"{{\"id\":\"{Id(1)}\"}}", fullName: "兜底路径"),
        };

        var actual = VisualDevListRefIndex.BuildRefIndex(modules, items, _ => "系统A/模块B");
        Assert.Equal(new[] { "系统A/模块B" }, actual[Id(1)]);
    }

    [Fact]
    public void 更长连续数字串_超出契约_文档化()
    {
        // 原始 Contains 会命中 20 位数字串中的 19 位子串；倒排索引按独立 19 位 token 解析，不命中。
        // 实际 PropertyJson 中功能 id 均为独立 JSON 值，该差异仅作防御性文档。
        var id = Id(1);
        var items = new List<VisualDevListOutput> { Item(id) };
        var modules = new List<ModuleEntity> { Module("m1", $"{{\"a\":\"9{id}0\"}}") };

        Assert.Empty(VisualDevListRefIndex.BuildRefIndex(modules, items, _ => null));
        Assert.NotEmpty(Oracle(modules, items, _ => null));
    }

    [Fact]
    public void 大规模语料_索引构建在预算内()
    {
        var items = Enumerable.Range(0, 200).Select(i => Item(Id(i))).ToList();
        var modules = new List<ModuleEntity>();
        for (var i = 0; i < 2000; i++)
        {
            var a = i % 200;
            // 前 200 个模块各单独引用一个 id，保证全覆盖；
            // 其余模块成对引用，增加单模块扫描压力。
            var json = i < 200
                ? $"{{\"f0\":\"{Id(a)}\"}}"
                : $"{{\"f0\":\"{Id(a)}\",\"f1\":\"{Id((i + 37) % 200)}\"}}";
            modules.Add(Module($"m{i}", json));
        }

        var sw = Stopwatch.StartNew();
        var actual = VisualDevListRefIndex.BuildRefIndex(modules, items, _ => null);
        sw.Stop();

        Assert.Equal(200, actual.Count);
        Assert.True(sw.ElapsedMilliseconds < 2000, $"索引构建耗时 {sw.ElapsedMilliseconds}ms 超出预算");
    }
}
