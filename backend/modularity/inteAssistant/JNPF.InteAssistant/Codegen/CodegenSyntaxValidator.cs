using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace JNPF.InteAssistant.Codegen;

/// <summary>
/// Roslyn 语法树前置校验（E1 TryFastSyntaxCheck 雏形）。
/// </summary>
public static class CodegenSyntaxValidator
{
    public static IReadOnlyList<string> GetSyntaxErrors(string source, string? fileName = null)
    {
        if (string.IsNullOrWhiteSpace(source))
            return new[] { "源码为空" };

        var tree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.Latest),
            fileName ?? "generated.cs");

        return tree.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .ToList();
    }

    public static void EnsureValidSyntax(string source, string? fileName = null)
    {
        var errors = GetSyntaxErrors(source, fileName);
        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"C# 语法校验失败 ({fileName ?? "generated.cs"}): {string.Join("; ", errors)}");
    }
}
