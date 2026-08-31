using System.Reflection;
using JNPF.Runtime.Capability;
using JNPF.Runtime.Capability.Capabilities;
using JNPF.Runtime.Capability.Constraints;
using JNPF.Runtime.Capability.Loading;
using JNPF.Runtime.Capability.Modes;
using JNPF.Runtime.Capability.Registry;
using Xunit;

namespace JNPF.Tests.Section9.Modes;

/// <summary>
/// Section 9 边界合规验证（M8 / M14 / M16 / M17 / M18 / LOCK-H02 / Constraint-14）。
///
/// 通过反射扫描 Section 9 程序集，验证：
///   - 不得引用 Runtime / Section 8 类型；
///   - 不得包含 LLM / Prompt / Reasoner / Workflow / Step / DAG 概念；
///   - 不得包含 Singleton Mode 字段；
///   - 不修改 Runtime 行为（M8：Mode 不依赖 Runtime）。
/// </summary>
public sealed class BoundaryComplianceTests
{
    // 精准匹配 Section 8 类型名 + Workflow / Intelligence 概念（不能用 "Runtime" 否则会命中 JNPF.Runtime.Capability.* 命名空间）
    private static readonly string[] ForbiddenTypeSubstrings =
    {
        "AgentSession",
        "RuntimeLifecycle",
        "AgentLoop",
        "ActionExecutor",
        "EvidenceStore",
        "ExecutionState",
        "ExtensionHook",
        "Workflow",
        "Dag",
        "Reasoning",
        "Reasoner",
        "Prompt",
        "Think"
    };

    private static readonly string[] ForbiddenLiterals =
    {
        "Llm",
        "GPT",
        "Claude",
        "OpenAI",
        "AzureOpenAI"
    };

    [Fact]
    public void CapabilityNamespace_DoesNotMentionRuntimeOrIntelligence()
    {
        AssertNamespaceBoundary(typeof(Capability).Assembly);
    }

    [Fact]
    public void ConstraintsNamespace_DoesNotMentionRuntimeOrIntelligence()
    {
        AssertNamespaceBoundary(typeof(IConstraint).Assembly);
    }

    [Fact]
    public void ModesNamespace_DoesNotMentionRuntimeOrIntelligence()
    {
        AssertNamespaceBoundary(typeof(IMode).Assembly);
    }

    [Fact]
    public void LoadingNamespace_DoesNotMentionRuntimeOrIntelligence()
    {
        AssertNamespaceBoundary(typeof(IModeProvider).Assembly);
    }

    [Fact]
    public void RegistryNamespace_DoesNotMentionRuntimeOrIntelligence()
    {
        AssertNamespaceBoundary(typeof(IModeRegistry).Assembly);
    }

    [Fact]
    public void IMode_Assembly_HasNoIntelligenceMembers()
    {
        var asm = typeof(IMode).Assembly;
        foreach (var type in asm.GetTypes())
        {
            foreach (var literal in ForbiddenLiterals)
            {
                Assert.False(
                    type.FullName!.Contains(literal, StringComparison.OrdinalIgnoreCase),
                    $"Type '{type.FullName}' contains forbidden literal '{literal}' (LOCK-H02).");
            }
        }
    }

    [Fact]
    public void IMode_HasNoPublicFields()
    {
        // M16 Purity：IMode 不持有可变字段（含 Singleton 状态）。
        var iMode = typeof(IMode);
        Assert.Empty(iMode.GetFields(BindingFlags.Public | BindingFlags.Instance));
        Assert.Empty(iMode.GetFields(BindingFlags.Public | BindingFlags.Static));
    }

    [Fact]
    public void DefaultModeProvider_HasNoStaticModeInstance()
    {
        // Iron Law-05 + Gate-9-5：Provider 不得维护静态 Mode 实例。
        var type = typeof(DefaultModeProvider);

        var staticModeFields = type.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(f => typeof(IMode).IsAssignableFrom(f.FieldType))
            .ToList();

        Assert.Empty(staticModeFields);
    }

    [Fact]
    public void DefaultModeRegistry_HasNoMutableCollections()
    {
        // Registry 是只读元数据，不可暴露可变集合。
        var registryType = typeof(DefaultModeRegistry);
        var mutableProps = registryType.GetProperties()
            .Where(p =>
            {
                var t = p.PropertyType;
                return t.IsGenericType &&
                       (t.GetGenericTypeDefinition() == typeof(Dictionary<,>) ||
                        t.GetGenericTypeDefinition() == typeof(List<>));
            })
            .Select(p => p.Name)
            .ToList();

        Assert.Empty(mutableProps);
    }

    private static void AssertNamespaceBoundary(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            var fullName = type.FullName ?? string.Empty;
            foreach (var forbidden in ForbiddenTypeSubstrings)
            {
                Assert.False(
                    fullName.Contains(forbidden, StringComparison.OrdinalIgnoreCase),
                    $"Type '{fullName}' contains forbidden substring '{forbidden}'. " +
                    $"Section 9 must not reference Runtime / Intelligence / Workflow types " +
                    $"(M8 + M14 + M16 + M17).");
            }
        }
    }
}
