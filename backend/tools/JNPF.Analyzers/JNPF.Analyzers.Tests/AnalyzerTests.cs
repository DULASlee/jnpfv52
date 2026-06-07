using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace JNPF.Analyzers.Tests;

public class AnalyzerTests
{
    // JNPF001: App.GetService / App.GetRequiredService 检测
    [Fact]
    public void JNPF001_AppGetService_Detected()
    {
        var code = @"
class TestClass {
    void M() {
        var s = App.GetService<IMenuService>();
    }
}";
        var diagnostics = GetDiagnostics(code, new AppServiceLocatorAnalyzer());
        Assert.Contains(diagnostics, d => d.Id == "JNPF001");
    }

    [Fact]
    public void JNPF001_NoAppService_NotDetected()
    {
        var code = @"
class TestClass {
    private readonly IMenuService _s;
    public TestClass(IMenuService s) { _s = s; }
}";
        var diagnostics = GetDiagnostics(code, new AppServiceLocatorAnalyzer());
        Assert.DoesNotContain(diagnostics, d => d.Id == "JNPF001");
    }

    // JNPF002: DataExecuting = 赋值检测
    [Fact]
    public void JNPF002_DataExecutingAssignment_Detected()
    {
        var code = @"
class TestClass {
    void M() {
        db.Aop.DataExecuting = (oldValue, entityInfo) => { };
    }
}";
        var diagnostics = GetDiagnostics(code, new DataExecutingAnalyzer());
        Assert.Contains(diagnostics, d => d.Id == "JNPF002");
    }

    [Fact]
    public void JNPF002_AddAssignment_NotDetected()
    {
        var code = @"
class TestClass {
    void M() {
        db.Aop.DataExecuting += (oldValue, entityInfo) => { };
    }
}";
        var diagnostics = GetDiagnostics(code, new DataExecutingAnalyzer());
        Assert.DoesNotContain(diagnostics, d => d.Id == "JNPF002");
    }

    // JNPF003: CreateScope 检测
    [Fact]
    public void JNPF003_CreateScope_Detected()
    {
        var code = @"
class TestClass {
    void M() {
        var scope = serviceProvider.CreateScope();
    }
}";
        var diagnostics = GetDiagnostics(code, new CreateScopeAnalyzer());
        Assert.Contains(diagnostics, d => d.Id == "JNPF003");
    }

    [Fact]
    public void JNPF003_NoCreateScope_NotDetected()
    {
        var code = @"
class TestClass {
    void M() {
        var p = serviceProvider;
    }
}";
        var diagnostics = GetDiagnostics(code, new CreateScopeAnalyzer());
        Assert.DoesNotContain(diagnostics, d => d.Id == "JNPF003");
    }

    // JNPF005: 构造函数直接注入 ISqlSugarClient
    [Fact]
    public void JNPF005_DirectSqlSugarClient_Detected()
    {
        var code = @"
class TestClass {
    public TestClass(ISqlSugarClient client) { }
}";
        var diagnostics = GetDiagnostics(code, new DirectSqlSugarAnalyzer());
        Assert.Contains(diagnostics, d => d.Id == "JNPF005");
    }

    [Fact]
    public void JNPF005_Repository_NotDetected()
    {
        var code = @"
class TestClass {
    public TestClass(ISqlSugarRepository<UserEntity> repo) { }
}";
        var diagnostics = GetDiagnostics(code, new DirectSqlSugarAnalyzer());
        Assert.DoesNotContain(diagnostics, d => d.Id == "JNPF005");
    }

    // JNPF006: async void 检测
    [Fact]
    public void JNPF006_AsyncVoid_Detected()
    {
        var code = @"
class TestClass {
    async void M() {
        await Task.Delay(100);
    }
}";
        var diagnostics = GetDiagnostics(code, new AsyncVoidAnalyzer());
        Assert.Contains(diagnostics, d => d.Id == "JNPF006");
    }

    [Fact]
    public void JNPF006_AsyncTask_NotDetected()
    {
        var code = @"
class TestClass {
    async Task M() {
        await Task.Delay(100);
    }
}";
        var diagnostics = GetDiagnostics(code, new AsyncVoidAnalyzer());
        Assert.DoesNotContain(diagnostics, d => d.Id == "JNPF006");
    }

    // JNPF004: [BypassOutbox] 无注释检测
    [Fact]
    public void JNPF004_BypassOutboxNoComment_Detected()
    {
        var code = @"
using System;

[AttributeUsage(AttributeTargets.Method)]
public class BypassOutboxAttribute : Attribute { }

class TestClass {
    [BypassOutbox]
    async Task M() { await Task.Delay(1); }
}
";
        var diagnostics = GetDiagnostics(code, new BypassOutboxAnalyzer());
        Assert.Contains(diagnostics, d => d.Id == "JNPF004");
    }

    private static ImmutableArray<Diagnostic> GetDiagnostics(string code, DiagnosticAnalyzer analyzer)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var compilation = CSharpCompilation.Create("TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var options = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create(analyzer), options);

        return compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
    }
}
