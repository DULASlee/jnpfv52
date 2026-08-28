using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JnpfAnalyzer;

/// <summary>
/// R1 Correctness Gate v2 — Symbol-driven solution-level call graph.
/// 修正 v1 审查发现的缺陷：
///   R1-A 方法身份：五元组 canonical ID（ContainingType+Name+ParameterTypes+ReturnType+GenericArity+MethodKind），
///        泛型一律 OriginalDefinition 规范化（构造泛型调用映射回定义 ID，跨运行稳定）。
///   R1-B 反向边：构建时 Caller→Callee 直接登记，Unresolved 不建边。
///   R1-C Overload：参数类型入 ID，不合并。
///   R1-D Extension：ReducedFrom 归一化（reduced 形态调用解析到扩展方法定义 Symbol）。
///   R1-E Dispatch：ResolvedSymbol/DispatchKind/ResolutionStatus 三字段显式记录；
///        Interface/Virtual/Delegate 不猜实现，标 Partial。
///   其他：注册改由 Symbol 树驱动（覆盖 interface/record/struct/属性访问器/构造器/事件访问器），
///        caller 归属用 GetEnclosingSymbol（lambda/局部函数上溯到命名方法），
///        全部输出排序保证确定性，Workspace/Compilation 诊断落盘不吞。
/// </summary>
public sealed class CallGraphBuilder
{
    public const string SchemaVersion = "1.1";
    public const string ToolVersion = "1.1.0";

    /// <summary>全限定、无 global::、无 nullable 注解（跨运行稳定）、特例类型名（int 而非 Int32）、不含 nullability。</summary>
    private static readonly SymbolDisplayFormat Q = SymbolDisplayFormat.FullyQualifiedFormat
        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

    private readonly Dictionary<string, MethodInfo> _methods = new();
    private readonly Dictionary<string, ClassInfo> _classes = new();
    private readonly List<CallInfo> _calls = new();
    private readonly Dictionary<string, HashSet<string>> _calledBy = new();
    private readonly List<string> _workspaceDiagnostics = new();
    private readonly List<string> _skippedDocuments = new();
    private readonly Dictionary<(string Project, string Code), int> _compilationWarnings = new();
    private int _compilationErrorCount;

    public async Task<CallGraphResult> BuildAsync(string solutionPath)
    {
        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (_, args) =>
        {
            _workspaceDiagnostics.Add($"[{args.Diagnostic.Kind}] {args.Diagnostic.Message}");
        };

        var solution = await workspace.OpenSolutionAsync(solutionPath);
        var result = new CallGraphResult
        {
            SchemaVersion = SchemaVersion,
            ToolVersion = ToolVersion,
            GeneratedAt = DateTime.UtcNow,
            Solution = solutionPath,
            Projects = new()
        };

        // ── Pass 0：加载 C# 工程 ─────────────────────────────────────────
        var projects = new List<(Project Proj, Compilation Comp)>();
        foreach (var project in solution.Projects.OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            if (project.Language != LanguageNames.CSharp)
            {
                _skippedDocuments.Add($"project(non-csharp): {project.Name} [{project.Language}]");
                continue;
            }
            var compilation = await project.GetCompilationAsync();
            if (compilation is null)
            {
                _workspaceDiagnostics.Add($"[error] compilation is null: {project.Name}");
                continue;
            }
            projects.Add((project, compilation));
        }

        // ── Pass 1：Symbol 树注册（类 + 方法全集，先建索引后采调用）────
        foreach (var (proj, comp) in projects)
        {
            RegisterCompilationSymbols(comp, proj.Name);
            CollectCompilationDiagnostics(comp, proj.Name);
        }

        // ── Pass 2：Syntax + SemanticModel 采集调用边 ────────────────────
        foreach (var (proj, comp) in projects)
        {
            var projectInfo = new ProjectInfo
            {
                Name = proj.Name,
                FilePath = proj.FilePath ?? "",
                Documents = new()
            };

            foreach (var tree in comp.SyntaxTrees.OrderBy(t => t.FilePath, StringComparer.Ordinal))
            {
                if (string.IsNullOrEmpty(tree.FilePath))
                {
                    _skippedDocuments.Add($"document(generated): {proj.Name} [{SummarizeRoot(tree)}]");
                    continue;
                }

                var model = comp.GetSemanticModel(tree);
                var root = await tree.GetRootAsync();
                var docInfo = new DocumentInfo { FilePath = tree.FilePath, Classes = new() };

                // 文档级类视图（供 projects→documents→classes 钻取；仅本文件声明的类）
                foreach (var typeDecl in root.DescendantNodes()
                             .OfType<TypeDeclarationSyntax>()
                             .OrderBy(n => n.SpanStart))
                {
                    var tSym = model.GetDeclaredSymbol(typeDecl) as INamedTypeSymbol;
                    if (tSym is null || !_classes.TryGetValue(TypeKey(tSym), out var cls)) continue;
                    docInfo.Classes.Add(new DocumentClassRef { Name = cls.Name, MethodIds = cls.MethodIds });
                }

                foreach (var inv in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    CaptureInvocation(model, inv, tree.FilePath);

                foreach (var obj in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
                    CaptureObjectCreation(model, obj, tree.FilePath);

                projectInfo.Documents.Add(docInfo);
            }

            result.Projects.Add(projectInfo);
        }

        // ── 组装（全部确定性排序）──────────────────────────────────────
        result.Calls = _calls
            .OrderBy(c => c.CallerMethodId, StringComparer.Ordinal)
            .ThenBy(c => c.File, StringComparer.Ordinal)
            .ThenBy(c => c.Line)
            .ToList();
        foreach (var m in _methods.Values)
            m.CalledBy = _calledBy.TryGetValue(m.MethodId, out var set)
                ? set.OrderBy(x => x, StringComparer.Ordinal).ToList()
                : new List<string>();
        result.Methods = _methods.Values.OrderBy(m => m.MethodId, StringComparer.Ordinal).ToList();
        foreach (var c in _classes.Values)
            c.MethodIds.Sort(StringComparer.Ordinal);
        result.Classes = _classes.Values.OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
        result.Diagnostics = new DiagnosticsInfo
        {
            WorkspaceWarnings = _workspaceDiagnostics.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            SkippedDocuments = _skippedDocuments.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            CompilationErrorCount = _compilationErrorCount,
            CompilationWarnings = _compilationWarnings
                .OrderBy(kv => kv.Key.Project, StringComparer.Ordinal)
                .ThenBy(kv => kv.Key.Code, StringComparer.Ordinal)
                .Select(kv => new WarningCount { Project = kv.Key.Project, Code = kv.Key.Code, Count = kv.Value })
                .ToList()
        };
        return result;
    }

    // ────────────────────────── Symbol 注册 ──────────────────────────

    private void RegisterCompilationSymbols(Compilation comp, string projectName)
    {
        // 只遍历源程序集自身（compilation.Assembly），不遍历 GlobalNamespace——
        // 否则会把引用的 BCL/NuGet 程序集全部当作分析对象，污染覆盖计数并拖慢真实方案。
        VisitContainer(comp.Assembly.GlobalNamespace, projectName);
    }

    private void VisitContainer(INamespaceOrTypeSymbol container, string projectName)
    {
        foreach (var member in container.GetMembers())
        {
            switch (member)
            {
                case INamespaceSymbol ns:
                    VisitContainer(ns, projectName);
                    break;
                case INamedTypeSymbol type when !type.IsAnonymousType && !type.IsTupleType:
                    RegisterType(type, projectName);
                    break;
                case IMethodSymbol method:
                    RegisterMethod(method, projectName);
                    break;
                case IPropertySymbol prop:
                    if (prop.GetMethod is { } g) RegisterMethod(g, projectName);
                    if (prop.SetMethod is { } s) RegisterMethod(s, projectName);
                    break;
                case IEventSymbol e:
                    if (e.AddMethod is { } a) RegisterMethod(a, projectName);
                    if (e.RemoveMethod is { } r) RegisterMethod(r, projectName);
                    break;
            }
        }
    }

    private void RegisterType(INamedTypeSymbol type, string projectName)
    {
        var key = TypeKey(type);
        if (!_classes.ContainsKey(key))
        {
            var file = type.DeclaringSyntaxReferences.FirstOrDefault()
                ?.GetSyntax().GetLocation().GetLineSpan().Path ?? "";
            _classes[key] = new ClassInfo
            {
                Name = type.ToDisplayString(Q),
                Namespace = type.ContainingNamespace?.ToDisplayString(Q) ?? "",
                TypeKind = type.TypeKind.ToString(),
                File = file ?? "",
                MethodIds = new()
            };
        }
        VisitContainer(type, projectName);
    }

    private void RegisterMethod(IMethodSymbol method, string projectName)
    {
        // 跳过编译器生成的合成方法体源头无关项：partial 方法声明无实现仍登记（无害）
        var id = MethodId(method);
        if (id is null) return;
        if (_methods.TryGetValue(id, out var existing))
        {
            if (existing != null)
            {
                if (!existing.SourceProjects.Contains(projectName)) existing.SourceProjects.Add(projectName);
                if (existing.IsExternal && method.DeclaringSyntaxReferences.Length > 0) existing.IsExternal = false;
            }
            return;
        }

        var location = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation();
        var span = location?.GetLineSpan();
        var info = new MethodInfo
        {
            MethodId = id,
            Name = method.Name,
            ContainingType = method.ContainingType?.ToDisplayString(Q) ?? "",
            Namespace = method.ContainingNamespace?.ToDisplayString(Q) ?? "",
            File = span?.Path ?? "",
            Line = span is null ? 0 : span.Value.StartLinePosition.Line + 1,
            ReturnType = method.ReturnType.ToDisplayString(Q),
            Parameters = method.Parameters.Select(p => new ParameterInfo
            {
                Name = p.Name,
                Type = p.Type.ToDisplayString(Q),
                RefKind = p.RefKind.ToString()
            }).ToList(),
            IsAsync = method.IsAsync,
            Accessibility = method.DeclaredAccessibility.ToString(),
            GenericArity = method.Arity,
            MethodKind = method.MethodKind.ToString(),
            IsExternal = method.DeclaringSyntaxReferences.Length == 0,
            SourceProjects = new List<string> { projectName }
        };
        _methods[id] = info;
        var classKey = method.ContainingType is null ? null : TypeKey(method.ContainingType);
        if (classKey != null && _classes.TryGetValue(classKey, out var cls) && !cls.MethodIds.Contains(id))
            cls.MethodIds.Add(id);
    }

    private void CollectCompilationDiagnostics(Compilation comp, string projectName)
    {
        foreach (var d in comp.GetDiagnostics())
        {
            if (d.Severity == DiagnosticSeverity.Error) { _compilationErrorCount++; continue; }
            if (d.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Info)
            {
                var key = (projectName, d.Id);
                _compilationWarnings[key] = _compilationWarnings.TryGetValue(key, out var n) ? n + 1 : 1;
            }
        }
    }

    // ────────────────────────── 调用采集 ──────────────────────────

    private void CaptureInvocation(SemanticModel model, InvocationExpressionSyntax inv, string file)
    {
        var caller = ResolveCaller(model, inv);
        if (caller is null) return;
        var callerId = RegisterOnDemand(caller);

        var symbolInfo = model.GetSymbolInfo(inv);
        if (symbolInfo.Symbol is IMethodSymbol ms)
        {
            AddCall(callerId, ms, inv, file, symbolInfo.CandidateReason.ToString());
        }
        else
        {
            // Symbol 无法确定：候选非空记 Partial，完全无候选记 Unresolved。绝不猜测。
            var candidates = symbolInfo.CandidateSymbols.Length;
            _calls.Add(new CallInfo
            {
                CallerMethodId = callerId,
                TargetMethodId = "UNRESOLVED",
                TargetClass = "",
                TargetMethod = inv.Expression.ToString(),
                TargetReturnType = "",
                File = file,
                Line = Line(inv),
                DispatchKind = "Unknown",
                ResolutionStatus = candidates > 0 ? "Partial" : "Unresolved",
                ResolvedSymbol = "",
                TargetInSolution = false,
                Note = $"CandidateReason={symbolInfo.CandidateReason}, Candidates={candidates}"
            });
        }
    }

    private void CaptureObjectCreation(SemanticModel model, ObjectCreationExpressionSyntax obj, string file)
    {
        var caller = ResolveCaller(model, obj);
        if (caller is null) return;
        var callerId = RegisterOnDemand(caller);
        var symbolInfo = model.GetSymbolInfo(obj);
        if (symbolInfo.Symbol is IMethodSymbol ctor)
            AddCall(callerId, ctor, obj, file, "None");
    }

    private void AddCall(string callerId, IMethodSymbol symbol, SyntaxNode node, string file, string candidateReason)
    {
        // R1-D：reduced 扩展方法调用归一化到定义 Symbol
        var definition = symbol.OriginalDefinition;
        var targetSym = definition.ReducedFrom ?? definition;
        var targetId = MethodId(targetSym);
        if (targetId is null) return;

        var (kind, status) = ClassifyDispatch(symbol, targetSym);
        var inSolution = _methods.TryGetValue(targetId, out var tm) && !tm.IsExternal;

        _calls.Add(new CallInfo
        {
            CallerMethodId = callerId,
            TargetMethodId = targetId,
            TargetClass = targetSym.ContainingType?.ToDisplayString(Q) ?? "",
            TargetMethod = targetSym.Name,
            TargetReturnType = targetSym.ReturnType.ToDisplayString(Q),
            File = file,
            Line = Line(node),
            DispatchKind = kind,
            ResolutionStatus = status,
            ResolvedSymbol = targetSym.ToDisplayString(Q),
            TargetInSolution = inSolution,
            Note = candidateReason == "None" ? "" : $"CandidateReason={candidateReason}"
        });

        // R1-B：反向边建边——仅当目标可确定且在 Unresolved/Partial(无目标) 之外。
        // Interface/Virtual 的 Partial 目标本身就是合法边（目标 = 接口方法/基方法），必须建边。
        if (status != "Unresolved")
        {
            if (!_calledBy.TryGetValue(targetId, out var set))
                _calledBy[targetId] = set = new HashSet<string>();
            set.Add(callerId);
        }
    }

    private static (string Kind, string Status) ClassifyDispatch(IMethodSymbol original, IMethodSymbol target)
    {
        if (target.MethodKind == MethodKind.DelegateInvoke)
            return ("Delegate", "Partial");                       // 委托目标运行时才可知，不猜
        if (target.MethodKind == MethodKind.ExplicitInterfaceImplementation)
            return ("Interface", "Partial");
        if (target.ContainingType?.TypeKind == TypeKind.Interface)
            return ("Interface", "Partial");                      // 不声称唯一实现
        if (original.IsExtensionMethod || target.IsExtensionMethod || target.ReducedFrom is not null)
            return ("Extension", "Resolved");                     // 扩展方法是静态绑定，目标确定
        if (target.IsAbstract || target.IsVirtual || target.IsOverride)
            return ("Virtual", "Partial");                        // 静态无法去虚拟化
        if (target.MethodKind == MethodKind.Constructor)
            return ("Constructor", "Resolved");
        if (target.IsStatic)
            return ("Static", "Resolved");
        return ("Direct", "Resolved");
    }

    private IMethodSymbol? ResolveCaller(SemanticModel model, SyntaxNode node)
    {
        var symbol = model.GetEnclosingSymbol(node.SpanStart) as IMethodSymbol;
        var cur = symbol;
        // lambda / 匿名函数 / 局部函数上溯到命名方法（保持调用归属可读）
        while (cur is not null &&
               cur.MethodKind is MethodKind.AnonymousFunction or MethodKind.LambdaMethod or MethodKind.LocalFunction)
        {
            cur = cur.ContainingSymbol as IMethodSymbol;
        }
        return cur;
    }

    private string RegisterOnDemand(IMethodSymbol caller)
    {
        var id = MethodId(caller);
        if (id is null) return "UNKNOWN-CALLER";
        if (!_methods.ContainsKey(id))
            RegisterMethod(caller, "<on-demand>");
        return id;
    }

    // ────────────────────────── 身份（R1-A/C）──────────────────────────

    /// <summary>
    /// 五元组 canonical ID：ContainingType + Name + GenericArity + ParameterTypes + ReturnType（+MethodKind 防局部函数撞名）。
    /// 一律 OriginalDefinition 规范化 → 构造泛型调用稳定映射回定义 ID。
    /// </summary>
    private static string? MethodId(IMethodSymbol m)
    {
        if (m.ContainingType is null && m.MethodKind is not (MethodKind.AnonymousFunction or MethodKind.LambdaMethod or MethodKind.LocalFunction))
            return null;
        var def = m.OriginalDefinition;
        var containing = def.ContainingType?.ToDisplayString(Q) ?? "<none>";
        var arity = def.Arity > 0 ? $"`{def.Arity}" : "";
        var ps = string.Join(",", def.Parameters.Select(p => p.Type.ToDisplayString(Q)));
        var ret = def.ReturnType.ToDisplayString(Q);
        var kind = def.MethodKind == MethodKind.Ordinary ? "" : $"[{def.MethodKind}]";
        return $"M:{containing}.{def.Name}{arity}({ps}):{ret}{kind}";
    }

    private static string TypeKey(INamedTypeSymbol t) => t.ToDisplayString(Q);

    private static int Line(SyntaxNode node) =>
        node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    private static string SummarizeRoot(SyntaxTree tree)
    {
        var first = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.NamespaceDeclarationSyntax>().FirstOrDefault();
        return first is null ? "<no-namespace>" : $"namespace {first.Name}";
    }
}

// ══════════════════════════════ 数据模型（§4）══════════════════════════════

public class CallGraphResult
{
    [JsonPropertyName("schema_version")] public string SchemaVersion { get; set; } = "";
    [JsonPropertyName("tool_version")] public string ToolVersion { get; set; } = "";
    [JsonPropertyName("generated_at")] public DateTime GeneratedAt { get; set; }
    [JsonPropertyName("solution")] public string Solution { get; set; } = "";
    [JsonPropertyName("projects")] public List<ProjectInfo> Projects { get; set; } = new();
    [JsonPropertyName("classes")] public List<ClassInfo> Classes { get; set; } = new();
    [JsonPropertyName("methods")] public List<MethodInfo> Methods { get; set; } = new();
    [JsonPropertyName("calls")] public List<CallInfo> Calls { get; set; } = new();
    [JsonPropertyName("diagnostics")] public DiagnosticsInfo Diagnostics { get; set; } = new();
}

public class ProjectInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("file_path")] public string FilePath { get; set; } = "";
    [JsonPropertyName("documents")] public List<DocumentInfo> Documents { get; set; } = new();
}

public class DocumentInfo
{
    [JsonPropertyName("file_path")] public string FilePath { get; set; } = "";
    [JsonPropertyName("classes")] public List<DocumentClassRef> Classes { get; set; } = new();
}

public class DocumentClassRef
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("method_ids")] public List<string> MethodIds { get; set; } = new();
}

public class ClassInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("namespace")] public string Namespace { get; set; } = "";
    [JsonPropertyName("type_kind")] public string TypeKind { get; set; } = "";
    [JsonPropertyName("file")] public string File { get; set; } = "";
    [JsonPropertyName("methods")] public List<string> MethodIds { get; set; } = new();
}

public class MethodInfo
{
    [JsonPropertyName("method_id")] public string MethodId { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("containing_type")] public string ContainingType { get; set; } = "";
    [JsonPropertyName("namespace")] public string Namespace { get; set; } = "";
    [JsonPropertyName("file")] public string File { get; set; } = "";
    [JsonPropertyName("line")] public int Line { get; set; }
    [JsonPropertyName("return_type")] public string ReturnType { get; set; } = "";
    [JsonPropertyName("parameters")] public List<ParameterInfo> Parameters { get; set; } = new();
    [JsonPropertyName("is_async")] public bool IsAsync { get; set; }
    [JsonPropertyName("accessibility")] public string Accessibility { get; set; } = "";
    [JsonPropertyName("generic_arity")] public int GenericArity { get; set; }
    [JsonPropertyName("method_kind")] public string MethodKind { get; set; } = "";
    [JsonPropertyName("is_external")] public bool IsExternal { get; set; }
    [JsonPropertyName("source_projects")] public List<string> SourceProjects { get; set; } = new();
    [JsonPropertyName("called_by")] public List<string> CalledBy { get; set; } = new();
}

public class ParameterInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("ref_kind")] public string RefKind { get; set; } = "";
}

public class CallInfo
{
    [JsonPropertyName("caller_method_id")] public string CallerMethodId { get; set; } = "";
    [JsonPropertyName("target_method_id")] public string TargetMethodId { get; set; } = "";
    [JsonPropertyName("target_class")] public string TargetClass { get; set; } = "";
    [JsonPropertyName("target_method")] public string TargetMethod { get; set; } = "";
    [JsonPropertyName("target_return_type")] public string TargetReturnType { get; set; } = "";
    [JsonPropertyName("file")] public string File { get; set; } = "";
    [JsonPropertyName("line")] public int Line { get; set; }
    [JsonPropertyName("dispatch_kind")] public string DispatchKind { get; set; } = "";
    [JsonPropertyName("resolution_status")] public string ResolutionStatus { get; set; } = "";
    [JsonPropertyName("resolved_symbol")] public string ResolvedSymbol { get; set; } = "";
    [JsonPropertyName("target_in_solution")] public bool TargetInSolution { get; set; }
    [JsonPropertyName("note")] public string Note { get; set; } = "";
}

public class DiagnosticsInfo
{
    [JsonPropertyName("workspace_warnings")] public List<string> WorkspaceWarnings { get; set; } = new();
    [JsonPropertyName("skipped_documents")] public List<string> SkippedDocuments { get; set; } = new();
    [JsonPropertyName("compilation_error_count")] public int CompilationErrorCount { get; set; }
    [JsonPropertyName("compilation_warnings")] public List<WarningCount> CompilationWarnings { get; set; } = new();
}

public class WarningCount
{
    [JsonPropertyName("project")] public string Project { get; set; } = "";
    [JsonPropertyName("code")] public string Code { get; set; } = "";
    [JsonPropertyName("count")] public int Count { get; set; }
}
