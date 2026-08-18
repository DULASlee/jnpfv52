using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace JNPF.InteAssistant.Codegen;

/// <summary>
/// 加载 <c>arch-guard-rules.yaml</c>（A2 唯一配置源）。
/// </summary>
public static class ArchGuardRulesLoader
{
    private static readonly Lazy<ArchGuardRulesDocument> Cached = new(LoadInternal);

    public static ArchGuardRulesDocument Load() => Cached.Value;

    public static IReadOnlyList<ArchGuardRuleDefinition> GetOrderedRules()
    {
        var doc = Load();
        if (doc.Execution?.ScanOrder is not { Count: > 0 })
            return doc.Rules;

        var byId = doc.Rules.ToDictionary(r => r.Id, StringComparer.Ordinal);
        var ordered = new List<ArchGuardRuleDefinition>();
        foreach (var id in doc.Execution.ScanOrder)
        {
            if (byId.TryGetValue(id, out var rule))
                ordered.Add(rule);
        }

        foreach (var rule in doc.Rules)
        {
            if (!ordered.Any(r => r.Id == rule.Id))
                ordered.Add(rule);
        }

        return ordered;
    }

    private static ArchGuardRulesDocument LoadInternal()
    {
        var path = ResolveRulesPath();
        if (!File.Exists(path))
            throw new FileNotFoundException($"arch-guard-rules.yaml 未找到: {path}");

        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeConverter(new YamlStringOrListConverter())
            .IgnoreUnmatchedProperties()
            .Build();

        var doc = deserializer.Deserialize<ArchGuardRulesDocument>(yaml)
            ?? throw new InvalidOperationException("arch-guard-rules.yaml 解析为空");

        if (doc.Rules.Count == 0)
            throw new InvalidOperationException("arch-guard-rules.yaml 未定义任何规则");

        return doc;
    }

    public static string ResolveRulesPath()
    {
        var assemblyDir = AppContext.BaseDirectory;
        var copied = Path.Combine(assemblyDir, "Codegen", "arch-guard-rules.yaml");
        if (File.Exists(copied))
            return copied;

        var repoRoot = CodegenWorkspacePaths.ResolveRepoRoot();
        var source = Path.Combine(
            repoRoot,
            "backend",
            "modularity",
            "inteAssistant",
            "JNPF.InteAssistant",
            "Codegen",
            "arch-guard-rules.yaml");
        return source;
    }
}

internal sealed class YamlStringOrListConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(YamlStringOrList);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var list = new YamlStringOrList();
        if (parser.Current is Scalar scalar)
        {
            list.Values.Add(scalar.Value);
            parser.MoveNext();
            return list;
        }

        if (parser.Current is SequenceStart)
        {
            parser.MoveNext();
            while (parser.Current is not SequenceEnd)
            {
                if (parser.Current is Scalar item)
                {
                    list.Values.Add(item.Value);
                    parser.MoveNext();
                }
                else
                {
                    parser.MoveNext();
                }
            }

            parser.MoveNext();
            return list;
        }

        return list;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
        throw new NotSupportedException();
}
