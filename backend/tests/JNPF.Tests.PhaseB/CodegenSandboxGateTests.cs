using JNPF.InteAssistant.Codegen;
using JNPF.InteAssistant.Codegen.TemplateContext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// D3-GATE — leave-simple 渲染产物 sandbox dotnet build。
/// </summary>
public static class CodegenSandboxGateTests
{
    public static async Task RunAllAsync()
    {
        await TestLeaveSimpleSandboxBuildAsync();
        Console.WriteLine("[D3] CodegenSandboxGate: leave-simple dotnet build PASS");
    }

    private static async Task TestLeaveSimpleSandboxBuildAsync()
    {
        var samplesDir = TemplateRenderSamplesTests.ResolveSamplesDirPublic();
        var samplePath = Path.Combine(samplesDir, "leave-simple.json");
        var templateRoot = VmTemplateCatalog.ResolveDefaultTemplateRoot();

        var builder = new TemplateContextBuilder();
        var renderer = VmTemplateRenderer.CreateDefault(templateRoot);
        var context = builder.BuildFromSampleJson(samplePath);

        var rendered = new Dictionary<string, string>();
        foreach (var templateId in VmTemplateIds.LockedBackendTemplates)
            rendered[templateId] = renderer.Render(templateId, context);

        var writer = new CodegenWorkspaceWriter();
        var backendRoot = writer.WriteSandbox(context, rendered);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        using var loggerFactory = LoggerFactory.Create(static _ => { });
        var sandbox = new CodeSandboxService(config, loggerFactory.CreateLogger<CodeSandboxService>());

        var syntax = sandbox.TryFastSyntaxCheck(backendRoot);
        if (!syntax.Success)
            throw new InvalidOperationException($"SyntaxCheckFail: {syntax.ErrorMessage}");

        var result = await sandbox.ValidateBuildAsync(backendRoot);
        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"D3-GATE BuildFail phase={result.Phase} exit={result.ExitCode}\n" +
                $"stderr: {result.StandardError}\nstdout: {result.StandardOutput}");
        }

        Console.WriteLine($"  leave-simple sandbox build OK ({result.Elapsed.TotalSeconds:F1}s) → {backendRoot}");
    }
}
