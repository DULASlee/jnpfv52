using Foundry.FSPM.SemanticModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P14-04 isolation + determinism: the Rule/Decision/Diagnostic/Evidence
// records live in zero-Roslyn SemanticModel, and evaluation is a pure
// function of (rule, model) — same inputs, byte-identical verdicts.
public sealed class RuleIsolationDeterminismTests
{
    private const string ClientSource = """
        using System;
        using Foundry.FSPM.SemanticModel;

        public static class RuleClient
        {
            public static string Describe(
                FspmRule rule,
                FspmRuleDecision decision,
                FspmDiagnostic diagnostic,
                FspmEvidence evidence)
            {
                return string.Join("|",
                    rule.Id,
                    rule.Kind.ToString(),
                    ((int)rule.Kind).ToString(),
                    decision.RuleId,
                    decision.Passed.ToString(),
                    decision.Reason,
                    diagnostic.Code,
                    diagnostic.Severity,
                    evidence.SubjectIdentity,
                    evidence.SnapshotVersion);
            }
        }
        """;

    [Fact]
    public void G14_04_ROSYNLISOLATION_Rule_Records_Readable_Without_Roslyn()
    {
        var modelAssembly = typeof(FspmSemanticState).Assembly.Location;
        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        var clientCompilation = CSharpCompilation.Create(
            "RuleClient",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(ClientSource) },
            references: new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(modelAssembly),
                MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")),
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        EmitResult emit = clientCompilation.Emit(ms);
        Assert.True(emit.Success,
            "Rule records must compile against SemanticModel with zero Roslyn references: " +
            string.Join("; ", emit.Diagnostics.Select(d => d.GetMessage())));

        var clientAssembly = Assembly.Load(ms.ToArray());
        var clientType = clientAssembly.GetType("RuleClient")
            ?? throw new InvalidOperationException("Fixture broken: RuleClient missing.");
        var describe = clientType.GetMethod("Describe")
            ?? throw new InvalidOperationException("Fixture broken: Describe missing.");

        var rule = new FspmRule("R-ISO", FspmRuleKind.TypeCompatible,
            new[] { "A|P:U.User.Name" }, "string", "name is string");
        var decision = new FspmRuleDecision("R-ISO", true, "match",
            "A|P:U.User.Name", "FP-N", "A|P:U.User.Name", "FP-N", null);
        var summary = (string)describe.Invoke(null, new object[]
        {
            rule,
            decision,
            new FspmDiagnostic("RUL-TYPE", "Info", "pass", "match", null),
            new FspmEvidence("R-ISO", true, "A|P:U.User.Name", "A|P:U.User.Name",
                "FP-N", "FP-N", null, "match", "snap-1"),
        })!;

        Assert.Contains("R-ISO", summary);
        Assert.Contains("TypeCompatible", summary);
        Assert.Contains("RUL-TYPE", summary);
        Assert.Contains("snap-1", summary);
    }

    [Fact]
    public void Evaluation_IsDeterministic_AcrossRuns()
    {
        var model = RulePresenceTests.SyntheticModel();
        var rules = new[]
        {
            new FspmRule("R-1", FspmRuleKind.Required, new[] { "A|T:U.User" }, "", ""),
            new FspmRule("R-2", FspmRuleKind.Forbidden, new[] { "A|T:U.Ghost" }, "", ""),
            new FspmRule("R-3", FspmRuleKind.TypeCompatible, new[] { "A|P:U.User.Name" }, "string", ""),
            new FspmRule("R-4", FspmRuleKind.OperationCompatible, new[] { "A|M:U.User.Create" }, "void", ""),
            new FspmRule("R-5", FspmRuleKind.ExactlyOne, new[] { "A|P:U.User.Name", "A|T:U.Ghost" }, "", ""),
        };

        static string Serialize(FspmRuleDecision d)
            => string.Join("|", d.RuleId, d.Passed, d.Reason,
                d.SubjectIdentity, d.SubjectFingerprint,
                d.TargetIdentity, d.TargetFingerprint,
                d.Anchor?.Document ?? "", d.Anchor?.DeclarationAnchor ?? "");

        static string Hash(System.Collections.Generic.IReadOnlyList<FspmRuleDecision> decisions)
        {
            var joined = string.Join("\n", decisions.Select(Serialize));
            return System.Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(joined)));
        }

        var run1 = rules.Select(r =>
            Foundry.FSPM.Compiler.Semantic.Rule.FspmRuleEvaluator.Evaluate(
                new FspmRuleContext(model, "snap-1"), r)).ToList();
        var run2 = rules.Select(r =>
            Foundry.FSPM.Compiler.Semantic.Rule.FspmRuleEvaluator.Evaluate(
                new FspmRuleContext(model, "snap-1"), r)).ToList();

        Assert.All(run1, d => Assert.True(d.Passed));
        Assert.Equal(Hash(run1), Hash(run2));
    }
}
