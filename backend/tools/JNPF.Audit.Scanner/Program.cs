using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JNPF.Audit.Scanner;

/// <summary>
/// S1-Final 后端结构性重构审计 — 复杂度全仓扫描（只读）。
/// 输出：complexity-inventory.csv（全方法指标）+ audit-scope-stats.txt（范围与分层汇总）。
/// 用法：dotnet run --project tools/JNPF.Audit.Scanner [--backend <path>] [--out <dir>]
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        var backend = Arg(args, "--backend", @"d:\JNPF-v52\backend");
        var outDir = Arg(args, "--out", @"d:\JNPF-v52\.claude\evidence\backend-structural-audit");

        if (!Directory.Exists(backend))
        {
            Console.Error.WriteLine($"backend not found: {backend}");
            return 2;
        }

        Directory.CreateDirectory(outDir);

        var files = Directory.EnumerateFiles(backend, "*.cs", SearchOption.AllDirectories)
            .Where(f => !IsExcluded(f))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sbCsv = new StringBuilder();
        sbCsv.AppendLine("File,Class,Method,CC,LOC,Params,NestingDepth,IfCount,SwitchCases,TryCatch,Returns,Calls");

        var classCount = 0;
        var methodCount = 0;
        var aClass = 0; // CC>=30
        var bClass = 0; // 20-29
        var cClass = 0; // 15-19
        var ccTotal = 0L;

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(text, path: file);
            var root = tree.GetCompilationUnitRoot();
            var rel = Path.GetRelativePath(backend, file).Replace('\\', '/');

            classCount += root.DescendantNodes().OfType<ClassDeclarationSyntax>().Count();

            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var body = (SyntaxNode)method.Body ?? method.ExpressionBody;
                if (body == null)
                    continue; // 抽象/接口/partial 声明

                methodCount++;
                var cc = ComplexityWalker.Compute(body);
                ccTotal += cc;

                var enclosingClass = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                var loc = body.GetLocation().GetLineSpan().Span.End.Line - body.GetLocation().GetLineSpan().Span.Start.Line + 1;
                var nesting = MaxNesting(body);
                var ifCount = body.DescendantNodes().OfType<IfStatementSyntax>().Count();
                var switchCases = body.DescendantNodes().OfType<CaseSwitchLabelSyntax>().Count()
                                  + body.DescendantNodes().OfType<CasePatternSwitchLabelSyntax>().Count();
                var tryCatch = body.DescendantNodes().OfType<TryStatementSyntax>().Count();
                var returns = body.DescendantNodes().OfType<ReturnStatementSyntax>().Count();
                var calls = body.DescendantNodes().OfType<InvocationExpressionSyntax>().Count();

                sbCsv.AppendLine(
                    $"{Csv(rel)},{Csv(enclosingClass?.Identifier.Text ?? "")},{Csv(method.Identifier.Text)}," +
                    $"{cc},{loc},{method.ParameterList.Parameters.Count},{nesting},{ifCount},{switchCases},{tryCatch},{returns},{calls}");

                if (cc >= 30) aClass++;
                else if (cc >= 20) bClass++;
                else if (cc >= 15) cClass++;
            }
        }

        File.WriteAllText(Path.Combine(outDir, "complexity-inventory.csv"), sbCsv.ToString(), Encoding.UTF8);

        var stats = new StringBuilder();
        var avgCc = methodCount == 0 ? 0 : (double)ccTotal / methodCount;
        stats.AppendLine($"BACKEND={backend}");
        stats.AppendLine($"FILES={files.Count}");
        stats.AppendLine($"CLASSES={classCount}");
        stats.AppendLine($"METHODS={methodCount}");
        stats.AppendLine($"AVG_CC={avgCc:F2}");
        stats.AppendLine($"A_CC_GE_30={aClass}");
        stats.AppendLine($"B_CC_20_29={bClass}");
        stats.AppendLine($"C_CC_15_19={cClass}");
        File.WriteAllText(Path.Combine(outDir, "audit-scope-stats.txt"), stats.ToString(), Encoding.UTF8);

        Console.WriteLine(stats.ToString());
        Console.WriteLine($"CSV={Path.Combine(outDir, "complexity-inventory.csv")}");
        return 0;
    }

    private static bool IsExcluded(string path)
    {
        var norm = path.Replace('\\', '/');
        return norm.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
               || norm.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
               || norm.Contains("/tests/", StringComparison.OrdinalIgnoreCase)
               || norm.Contains("/tools/", StringComparison.OrdinalIgnoreCase);
    }

    private static int MaxNesting(SyntaxNode node)
    {
        var max = 0;
        Walk(node, 0);
        return max;

        void Walk(SyntaxNode n, int depth)
        {
            var inc = n is IfStatementSyntax or ForStatementSyntax
                          or ForEachStatementSyntax or ForEachVariableStatementSyntax
                          or WhileStatementSyntax or DoStatementSyntax
                          or SwitchStatementSyntax or TryStatementSyntax
                ? 1
                : 0;
            var d = depth + inc;
            if (d > max) max = d;
            foreach (var child in n.ChildNodes())
                Walk(child, d);
        }
    }

    private static string Arg(string[] args, string name, string fallback)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return fallback;
    }

    private static string Csv(string value) =>
        value.Contains(',') ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
}
