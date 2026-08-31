using System.Reflection;
using System.Text;

namespace JNPF.Tools.RuntimeApiSurfaceCheck;

/// <summary>
/// Runtime.Core v0.1 API Surface Freeze Check Tool.
///
/// 比较当前编译输出的 public API surface 与 approved baseline。
/// 输出格式：
///   PASS — No unexpected public surface diff
///   FAIL — Unexpected types/members detected
/// </summary>
public static class Program
{
    // Approved baseline: 6 public types
    private static readonly HashSet<string> ApprovedPublicTypes = new()
    {
        "JNPF.Runtime.Core.RuntimeContext",
        "JNPF.Runtime.Core.RuntimeSession",
        "JNPF.Runtime.Core.RuntimeState",
        "JNPF.Runtime.Core.IRuntimeLifecycleController",
        "JNPF.Runtime.Core.RuntimeLifecycleController",
        "JNPF.Runtime.Core.RuntimeStateMachine"
    };

    // Approved public members per type
    private static readonly Dictionary<string, HashSet<string>> ApprovedPublicMembers = new()
    {
        ["JNPF.Runtime.Core.RuntimeContext"] = new()
        {
            "get_TenantId",
            "get_ProjectId",
            "get_PipelineId",
            "get_CreatedAtUtc",
            "get_CreatorUserId",
            "get_Metadata",
            "Create",
            "WithMetadata"
        },
        ["JNPF.Runtime.Core.RuntimeSession"] = new()
        {
            "get_SessionId",
            "get_Context",
            "get_State",
            "get_StateChangedAtUtc",
            "get_StateReason"
            // Constructor is internal — NOT public
        },
        ["JNPF.Runtime.Core.RuntimeState"] = new()
        {
            "Created",
            "Initialized",
            "Running",
            "Paused",
            "Completed",
            "Failed",
            "Disposed"
        },
        ["JNPF.Runtime.Core.IRuntimeLifecycleController"] = new()
        {
            "get_CurrentSession",
            "InitializeAsync",
            "StartAsync",
            "PauseAsync",
            "ResumeAsync",
            "CompleteAsync",
            "FailAsync",
            "DisposeAsync"
        },
        ["JNPF.Runtime.Core.RuntimeLifecycleController"] = new()
        {
            "get_CurrentSession",
            "InitializeAsync",
            "StartAsync",
            "PauseAsync",
            "ResumeAsync",
            "CompleteAsync",
            "FailAsync",
            "DisposeAsync"
        },
        ["JNPF.Runtime.Core.RuntimeStateMachine"] = new()
        {
            "CanTransition",
            "Transition"
        }
    };

    public static int Main(string[] args)
    {
        Console.WriteLine("=== Runtime.Core v0.1 API Surface Freeze Check ===");
        Console.WriteLine();

        var assembly = typeof(JNPF.Runtime.Core.RuntimeContext).Assembly;
        Console.WriteLine($"Assembly: {assembly.GetName().Name}");
        Console.WriteLine($"Version: {assembly.GetName().Version}");
        Console.WriteLine();

        var issues = new List<string>();
        var unexpectedTypes = new List<string>();
        var unexpectedMembers = new List<string>();
        var runtimeSessionPublicCtors = new List<string>();

        // Extract public types
        var publicTypes = assembly.GetExportedTypes()
            .Where(t => t.IsPublic && !t.IsNested)
            .ToList();

        Console.WriteLine($"Public Types Found: {publicTypes.Count}");

        // Check for unexpected public types
        foreach (var type in publicTypes)
        {
            var fullName = type.FullName ?? type.Name;
            if (!ApprovedPublicTypes.Contains(fullName))
            {
                unexpectedTypes.Add(fullName);
                issues.Add($"[UNEXPECTED TYPE] {fullName}");
            }

            // Check public members
            if (ApprovedPublicMembers.TryGetValue(fullName, out var approvedMembers))
            {
                var publicMembers = GetPublicMembers(type);
                foreach (var member in publicMembers)
                {
                    if (!approvedMembers.Contains(member))
                    {
                        unexpectedMembers.Add($"{fullName}.{member}");
                        issues.Add($"[UNEXPECTED MEMBER] {fullName}.{member}");
                    }
                }
            }

            // Special check: RuntimeSession constructor must NOT be public
            if (type.FullName == "JNPF.Runtime.Core.RuntimeSession")
            {
                var publicCtors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                if (publicCtors.Length > 0)
                {
                    foreach (var ctor in publicCtors)
                    {
                        runtimeSessionPublicCtors.Add($"RuntimeSession..ctor (public)");
                        issues.Add($"[VIOLATION] RuntimeSession has public constructor — must be internal");
                    }
                }
            }
        }

        // Check for missing types (should not happen if baseline is correct)
        foreach (var approvedType in ApprovedPublicTypes)
        {
            if (!publicTypes.Any(t => t.FullName == approvedType))
            {
                issues.Add($"[MISSING TYPE] {approvedType} — removed from public surface");
            }
        }

        Console.WriteLine();
        Console.WriteLine("--- Diff Results ---");

        if (issues.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ PASS — No unexpected public surface diff");
            Console.WriteLine();
            Console.WriteLine("Runtime.Core v0.1 API Surface is FROZEN.");
            Console.ResetColor();

            Console.WriteLine();
            Console.WriteLine("Public API Summary:");
            Console.WriteLine($"  Types: {publicTypes.Count} (expected: 6)");
            Console.WriteLine($"  RuntimeSession constructor: internal ✅");

            return 0;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ FAIL — {issues.Count} issue(s) detected");
            Console.WriteLine();
            foreach (var issue in issues)
            {
                Console.WriteLine($"  {issue}");
            }
            Console.ResetColor();

            Console.WriteLine();
            Console.WriteLine("Action required: Review and fix API surface violations.");
            Console.WriteLine("Do NOT proceed to Phase 2-B until API Freeze is verified.");

            return 1;
        }
    }

    private static HashSet<string> GetPublicMembers(Type type)
    {
        var members = new HashSet<string>();

        // Properties
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            members.Add($"get_{prop.Name}");
            if (prop.SetMethod?.IsPublic == true)
                members.Add($"set_{prop.Name}");
        }

        // Methods (exclude property getters/setters, constructors, special methods)
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            if (method.IsSpecialName) continue; // skip property accessors
            if (method.DeclaringType != type) continue; // inherited
            members.Add(method.Name);
        }

        // Events
        foreach (var evt in type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            members.Add($"add_{evt.Name}");
            members.Add($"remove_{evt.Name}");
        }

        // Fields (rarely public in managed code, but check anyway)
        // Exclude enum underlying value field (value__) — CLR implementation detail, not public API
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
        {
            if (field.Name == "value__") continue; // enum underlying type — ignore
            members.Add(field.Name);
        }

        // Nested public types
        foreach (var nested in type.GetNestedTypes(BindingFlags.Public))
        {
            members.Add(nested.Name);
        }

        return members;
    }
}
