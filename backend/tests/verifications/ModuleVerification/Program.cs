using JNPF.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("=== Module Graph Builder Verification Tests ===\n");

var passed = 0;
var failed = 0;

void Assert(bool condition, string testName, string detail = "")
{
    if (condition)
    {
        Console.WriteLine($"  PASS: {testName}");
        passed++;
    }
    else
    {
        Console.WriteLine($"  FAIL: {testName} {detail}");
        failed++;
    }
}

// ─── Test 1: Linear dependency A→B→C → [C, B, A] ───
Console.WriteLine("Test 1: Linear dependency A→B→C");
{
    var result = ModuleGraphBuilder.Build(new[] { typeof(LinearA), typeof(LinearB), typeof(LinearC) });
    Assert(result.Count == 3, "Count == 3");
    Assert(result[0] == typeof(LinearC), "C first (no deps)");
    Assert(result[1] == typeof(LinearB), "B second (depends on C)");
    Assert(result[2] == typeof(LinearA), "A last (depends on B)");
}

// ─── Test 2: Diamond DAG A→B, A→C, B→D, C→D ───
Console.WriteLine("\nTest 2: Diamond DAG");
{
    var result = ModuleGraphBuilder.Build(new[] { typeof(DiamondA), typeof(DiamondB), typeof(DiamondC), typeof(DiamondD) }).ToList();
    Assert(result.Count == 4, "Count == 4");
    Assert(result[0] == typeof(DiamondD), "D first (no deps)");
    Assert(result.IndexOf(typeof(DiamondA)) > result.IndexOf(typeof(DiamondB)), "A after B");
    Assert(result.IndexOf(typeof(DiamondA)) > result.IndexOf(typeof(DiamondC)), "A after C");
}

// ─── Test 3: Cycle detection A→B→C→A ───
Console.WriteLine("\nTest 3: Cycle detection");
{
    var caught = false;
    try
    {
        ModuleGraphBuilder.Build(new[] { typeof(CycleA), typeof(CycleB), typeof(CycleC) });
    }
    catch (ModuleLoadException ex)
    {
        caught = true;
        Assert(ex.CircularPath.Count > 0, "CircularPath not empty");
        Assert(ex.CircularPath.Contains(typeof(CycleA)), "Path contains A");
        Assert(ex.CircularPath.Contains(typeof(CycleB)), "Path contains B");
        Assert(ex.CircularPath.Contains(typeof(CycleC)), "Path contains C");
        Console.WriteLine($"    Circular path: {string.Join(" → ", ex.CircularPath.Select(t => t.Name))}");
    }
    Assert(caught, "ModuleLoadException thrown");
}

// ─── Test 4: No dependencies → registration order ───
Console.WriteLine("\nTest 4: No dependencies");
{
    var result = ModuleGraphBuilder.Build(new[] { typeof(IndependentX), typeof(IndependentY) });
    Assert(result.Count == 2, "Count == 2");
    Assert(result.Contains(typeof(IndependentX)), "Contains X");
    Assert(result.Contains(typeof(IndependentY)), "Contains Y");
}

// ─── Test 5: LegacyModule always first ───
Console.WriteLine("\nTest 5: LegacyModule first");
{
    var result = ModuleGraphBuilder.Build(new[] { typeof(LegacyModule), typeof(LinearA), typeof(LinearB), typeof(LinearC) });
    Assert(result[0] == typeof(LegacyModule), "LegacyModule is first");
    Assert(result[^1] == typeof(LinearA), "LinearA is last");
}

// ─── Test 6: JnpfModule base class ───
Console.WriteLine("\nTest 6: JnpfModule base class");
{
    var module = new LegacyModule();
    Assert(module.Dependencies.Count == 0, "LegacyModule has no dependencies");
    Assert(module.GetType() == typeof(LegacyModule), "Type check");
}

// ─── Summary ───
Console.WriteLine($"\n=== Results: {passed} passed, {failed} failed ===");
Environment.ExitCode = failed > 0 ? 1 : 0;

// ═══════════════════════════════════════════════
// Test module definitions
// ═══════════════════════════════════════════════

// Linear: C → B → A
[JNPF.Modules.DependsOn(typeof(LinearB))]
public class LinearA : JnpfModule { }

[JNPF.Modules.DependsOn(typeof(LinearC))]
public class LinearB : JnpfModule { }

[JNPF.Modules.DependsOn]
public class LinearC : JnpfModule { }

// Diamond: D ← B ← A, D ← C ← A
[JNPF.Modules.DependsOn(typeof(DiamondB), typeof(DiamondC))]
public class DiamondA : JnpfModule { }

[JNPF.Modules.DependsOn(typeof(DiamondD))]
public class DiamondB : JnpfModule { }

[JNPF.Modules.DependsOn(typeof(DiamondD))]
public class DiamondC : JnpfModule { }

[JNPF.Modules.DependsOn]
public class DiamondD : JnpfModule { }

// Cycle: A → B → C → A
[JNPF.Modules.DependsOn(typeof(CycleB))]
public class CycleA : JnpfModule { }

[JNPF.Modules.DependsOn(typeof(CycleC))]
public class CycleB : JnpfModule { }

[JNPF.Modules.DependsOn(typeof(CycleA))]
public class CycleC : JnpfModule { }

// Independent
[JNPF.Modules.DependsOn]
public class IndependentX : JnpfModule { }

[JNPF.Modules.DependsOn]
public class IndependentY : JnpfModule { }
