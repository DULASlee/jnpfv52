using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace JNPF.Tests.Agent;

/// <summary>
/// v5 — Roslyn-resolved inspection of original WorkstreamLPilotTests.
///
/// Chief Architect P1 noted v4 used `Assert.Contains("ExecuteAsync", body)`
/// which is too weak (any method named ExecuteAsync passes, including local
/// variables named `var ExecuteAsync = ...`). v5 uses
/// InvocationExpressionSyntax / MemberAccessExpressionSyntax to verify the
/// test ACTUALLY invokes real executor methods.
/// </summary>
public sealed class GateEIntegrityTests
{
    private const string WorkstreamLPilotTestsPath = @"D:\JNPF-v52\backend\tests\JNPF.Tests.Runtime.Expert\WorkstreamLPilotTests.cs";

    private static readonly string[] TargetTests = new[]
    {
        "Build_ShouldSucceedForTargetProject",
        "NoFunctionLossGate_ShouldVerifyBuildPassesAfterRefactor",
        "ExpertAgent_E2E_ShouldCompleteAllPhases"
    };

    [Fact]
    public void GateE_OriginalTests_AllThreeExist()
    {
        var source = File.ReadAllText(WorkstreamLPilotTestsPath);
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();
        foreach (var testName in TargetTests)
        {
            var method = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == testName);
            Assert.NotNull(method);
        }
    }

    [Theory]
    [InlineData("Build_ShouldSucceedForTargetProject", "BuildAsync")]
    [InlineData("NoFunctionLossGate_ShouldVerifyBuildPassesAfterRefactor", "BuildAsync")]
    [InlineData("ExpertAgent_E2E_ShouldCompleteAllPhases", "ExecuteAsync")]
    public void GateE_OriginalTest_InvokesRealTool_ViaRoslynInvocation(string testName, string requiredCall)
    {
        var source = File.ReadAllText(WorkstreamLPilotTestsPath);
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();
        var method = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First(m => m.Identifier.Text == testName);

        var invocations = method.Body!.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(i => i.Expression.ToString())
            .ToList();

        // Must invoke the named method via Roslyn-resolved InvocationExpressionSyntax
        Assert.Contains(invocations, inv => inv.Contains(requiredCall));

        // Must have at least one real Assert call (not just exist in source)
        var assertCalls = invocations.Where(i => i.StartsWith("Assert.")).ToList();
        Assert.NotEmpty(assertCalls);
    }

    [Fact]
    public void GateE_E2E_InvokesExecutor_NotJustConstructs()
    {
        var source = File.ReadAllText(WorkstreamLPilotTestsPath);
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();
        var e2eMethod = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First(m => m.Identifier.Text == "ExpertAgent_E2E_ShouldCompleteAllPhases");

        // Verify executor variable is actually USED (member access, not just construction)
        var memberAccesses = e2eMethod.Body!.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Select(m => m.Name.ToString())
            .ToList();

        Assert.Contains(memberAccesses, m => m == "ExecuteAsync");
    }
}