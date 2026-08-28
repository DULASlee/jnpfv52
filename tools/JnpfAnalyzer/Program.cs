using JnpfAnalyzer;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

// JnpfAnalyzer — R1 Correctness Gate 工具
// 模式 1（分析）: JnpfAnalyzer --solution <path> --output <dir>
// 模式 2（取证）: JnpfAnalyzer --extract <callgraph.json> --filter <TypeName[,TypeName…]> --out <file.json>

// MSBuildWorkspace 依赖：先注册本机 MSBuild（必须在任何 Microsoft.Build 类型加载前）
Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();

// dotnet CLI 可能保留前导 "--" 分隔符；先规整掉非选项前缀，再按位置识别 --extract
args = args.SkipWhile(a => a != "--extract" && !a.StartsWith("--")).ToArray();
var extractIdx = Array.FindIndex(args, a => a == "--extract");
if (extractIdx >= 0 && args.Length >= extractIdx + 2)
{
    RunExtract(args[extractIdx..]);
    return;
}

if (args.Length < 4)
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  JnpfAnalyzer --solution <path> --output <dir>");
    Console.Error.WriteLine("  JnpfAnalyzer --extract <callgraph.json> --filter <Type[,...]> --out <file.json>");
    Environment.Exit(1);
}

var solutionPath = "";
var outputDir = "";
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--solution" && i + 1 < args.Length) solutionPath = args[++i];
    else if (args[i] == "--output" && i + 1 < args.Length) outputDir = args[++i];
}
if (string.IsNullOrEmpty(solutionPath) || string.IsNullOrEmpty(outputDir))
{
    Console.Error.WriteLine("Error: --solution and --output are required");
    Environment.Exit(1);
}
if (!File.Exists(solutionPath))
{
    Console.Error.WriteLine($"Error: Solution file not found: {solutionPath}");
    Environment.Exit(1);
}

Console.WriteLine($"Building call graph for: {solutionPath}");
Console.WriteLine($"Output directory: {outputDir}");
Console.WriteLine($"Tool version: {CallGraphBuilder.ToolVersion} / schema: {CallGraphBuilder.SchemaVersion}");

Directory.CreateDirectory(outputDir);

var builder = new CallGraphBuilder();
var result = await builder.BuildAsync(solutionPath);

var jsonOpts = new JsonSerializerOptions { WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
var prettyOpts = new JsonSerializerOptions { WriteIndented = true };

// 全量图（紧凑格式，供 --extract 二次取证；属临时产物，不入 Git）
await File.WriteAllTextAsync(Path.Combine(outputDir, "callgraph.json"),
    JsonSerializer.Serialize(result, jsonOpts));

var summary = new
{
    result.SchemaVersion,
    result.ToolVersion,
    ProjectCount = result.Projects.Count,
    DocumentCount = result.Projects.Sum(p => p.Documents.Count),
    ClassCount = result.Classes.Count,
    MethodCount = result.Methods.Count,
    CallCount = result.Calls.Count,
    ResolvedCalls = result.Calls.Count(c => c.ResolutionStatus == "Resolved"),
    PartialCalls = result.Calls.Count(c => c.ResolutionStatus == "Partial"),
    UnresolvedCalls = result.Calls.Count(c => c.ResolutionStatus == "Unresolved"),
    ExtensionCalls = result.Calls.Count(c => c.DispatchKind == "Extension"),
    InterfaceCalls = result.Calls.Count(c => c.DispatchKind == "Interface"),
    VirtualCalls = result.Calls.Count(c => c.DispatchKind == "Virtual"),
    DelegateCalls = result.Calls.Count(c => c.DispatchKind == "Delegate"),
    ConstructorCalls = result.Calls.Count(c => c.DispatchKind == "Constructor"),
    StaticCalls = result.Calls.Count(c => c.DispatchKind == "Static"),
    DirectCalls = result.Calls.Count(c => c.DispatchKind == "Direct"),
    UnknownCalls = result.Calls.Count(c => c.DispatchKind == "Unknown"),
    WorkspaceDiagnosticCount = result.Diagnostics.WorkspaceWarnings.Count,
    SkippedDocumentCount = result.Diagnostics.SkippedDocuments.Count,
    CompilationErrorCount = result.Diagnostics.CompilationErrorCount,
    CompilationWarningKindCount = result.Diagnostics.CompilationWarnings.Count
};

await File.WriteAllTextAsync(Path.Combine(outputDir, "summary.json"),
    JsonSerializer.Serialize(summary, prettyOpts));
await File.WriteAllTextAsync(Path.Combine(outputDir, "diagnostics.json"),
    JsonSerializer.Serialize(result.Diagnostics, prettyOpts));

Console.WriteLine();
Console.WriteLine("=== Call Graph Summary ===");
foreach (var prop in summary.GetType().GetProperties())
    Console.WriteLine($"{prop.Name}: {prop.GetValue(summary)}");
Console.WriteLine();
Console.WriteLine($"Output written to: {outputDir}");
return;

// ══════════════════════════ extract 取证模式 ══════════════════════════

static void RunExtract(string[] args)
{
    // args[0] == "--extract"，args[1] = graph path（调用方已切片）
    // 最小 6 token：--extract <graph> --filter <Type> --out <file>
    if (args.Length < 6)
    {
        Console.Error.WriteLine("Usage: JnpfAnalyzer --extract <callgraph.json> --filter <Type[,...]> --out <file.json>");
        Environment.Exit(1);
    }
    var graphPath = args[1];
    string? filter = null, outPath = null;
    for (int i = 2; i < args.Length; i++)
    {
        if (args[i] == "--filter" && i + 1 < args.Length) filter = args[++i];
        else if (args[i] == "--out" && i + 1 < args.Length) outPath = args[++i];
    }
    if (filter is null || outPath is null || !File.Exists(graphPath))
    {
        Console.Error.WriteLine("Error: --filter / --out required and graph file must exist");
        Environment.Exit(1);
    }

    var types = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    using var doc = JsonDocument.Parse(File.ReadAllText(graphPath));
    var rootEl = doc.RootElement;

    bool MatchesType(string? containingType) =>
        containingType is not null && types.Any(t =>
            containingType == t ||
            containingType.EndsWith("." + t, StringComparison.Ordinal) ||
            containingType.Contains("+" + t) ||
            containingType.Split('.').Last() == t);

    // 收集目标类型的方法 ID 集
    var methodIds = new HashSet<string>(StringComparer.Ordinal);
    var methods = new List<JsonObject>();
    foreach (var m in rootEl.GetProperty("methods").EnumerateArray())
    {
        if (!MatchesType(m.GetProperty("containing_type").GetString())) continue;
        methodIds.Add(m.GetProperty("method_id").GetString()!);
        methods.Add(JsonNode.Parse(m.GetRawText())!.AsObject());
    }

    // calls：caller 属于目标方法集，或 target_class 命中目标类型
    var calls = new List<JsonObject>();
    foreach (var c in rootEl.GetProperty("calls").EnumerateArray())
    {
        var caller = c.GetProperty("caller_method_id").GetString();
        var targetClass = c.GetProperty("target_class").GetString();
        var targetId = c.GetProperty("target_method_id").GetString();
        bool inbound = caller is not null && methodIds.Contains(caller);
        bool outbound = MatchesType(targetClass) || (targetId is not null && methodIds.Contains(targetId));
        if (inbound || outbound) calls.Add(JsonNode.Parse(c.GetRawText())!.AsObject());
    }

    // classes：目标类型（含方法 ID 列表）
    var classes = new List<JsonObject>();
    foreach (var cl in rootEl.GetProperty("classes").EnumerateArray())
    {
        if (!MatchesType(cl.GetProperty("name").GetString())) continue;
        classes.Add(JsonNode.Parse(cl.GetRawText())!.AsObject());
    }

    static JsonArray ToArray(List<JsonObject> items)
    {
        var arr = new JsonArray();
        foreach (var o in items) arr.Add(JsonNode.Parse(o.ToJsonString()));
        return arr;
    }

    var outDoc = new JsonObject
    {
        ["schema_version"] = rootEl.GetProperty("schema_version").GetString(),
        ["tool_version"] = rootEl.GetProperty("tool_version").GetString(),
        ["solution"] = rootEl.GetProperty("solution").GetString(),
        ["filter"] = filter,
        ["note"] = "evidence subgraph extracted from callgraph.json; generated_at excluded (see run summary.json)",
        ["classes"] = ToArray(classes),
        ["methods"] = ToArray(methods),
        ["calls"] = ToArray(calls)
    };
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
    File.WriteAllText(outPath, outDoc.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"extract: classes={classes.Count} methods={methods.Count} calls={calls.Count} -> {outPath}");
}
