using JNPF.InteAssistant.Codegen.TemplateContext;
using JNPF.ViewEngine;

namespace JNPF.InteAssistant.Codegen;

/// <summary>
/// JNPF Razor .vm 渲染器（每 artifact 一次 ViewEngine 调用）。
/// </summary>
public sealed class VmTemplateRenderer
{
    private readonly IViewEngine _viewEngine;
    private readonly string _templateRoot;

    public VmTemplateRenderer(IViewEngine viewEngine, string templateRoot)
    {
        _viewEngine = viewEngine ?? throw new ArgumentNullException(nameof(viewEngine));
        _templateRoot = templateRoot ?? throw new ArgumentNullException(nameof(templateRoot));
    }

    public static VmTemplateRenderer CreateDefault(string? templateRoot = null)
    {
        templateRoot ??= VmTemplateCatalog.ResolveDefaultTemplateRoot();
        return new VmTemplateRenderer(new JNPF.ViewEngine.ViewEngine(), templateRoot);
    }

    public string Render(string templateId, Ir2CodegenContext context)
    {
        var path = VmTemplateCatalog.ResolvePath(_templateRoot, templateId);
        var content = File.ReadAllText(path);
        var cacheKey = SanitizeCacheFileName($"{context.SampleId}__{templateId}");
        return _viewEngine.RunCompileFromCached(content, context.ToViewModel(), cacheKey);
    }

    private static string SanitizeCacheFileName(string name) =>
        string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
}
