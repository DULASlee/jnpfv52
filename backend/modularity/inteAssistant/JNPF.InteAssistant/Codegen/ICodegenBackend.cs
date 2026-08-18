using System.Collections.Generic;
using JNPF.InteAssistant.Entitys.Ir.Contracts;

namespace JNPF.InteAssistant.Codegen;

/// <summary>
/// 代码生成后端接口（对标 ABP CLI 的"选项→模板集"映射）。
///
/// 每个 ICodegenBackend 实现对应一个"目标平台"：
///   MonolithicCodegenBackend  → 单体架构（.NET 单 csproj + Vue3，现阶段默认）
///   MicroserviceCodegenBackend → 微服务（按 ServiceBoundary 拆分多服务，未来）
///   CloudNativeCodegenBackend → 云原生（+Dockerfile +K8s +Helm，未来）
///
/// 编译器核心（IR 消费 + 模板渲染 + workspace 写入）不动，只需替换 backend 实现。
/// 这正是 LLVM 后端模式：一个 IR，多个目标平台。
///
/// 现阶段只实现 MonolithicCodegenBackend，但接口已预留扩展点。
/// </summary>
public interface ICodegenBackend
{
    /// <summary>后端标识（如 "csharp-monolithic"），匹配 CodegenProfile.BackendKey。</summary>
    string BackendKey { get; }

    /// <summary>显示名（如"单体架构（C# + Vue3）"）。</summary>
    string DisplayName { get; }

    /// <summary>
    /// 根据 CodegenProfile 选择要渲染的模板 ID 列表。
    /// 对标 ABP CLI：--tiered 决定选哪些模板。
    /// </summary>
    IReadOnlyList<string> SelectTemplates(CodegenProfile profile);

    /// <summary>
    /// 解析模板的输出路径。
    /// 单体：Entitys/{className}Entity.cs
    /// 微服务：{serviceName}/Entitys/{className}Entity.cs（按 ServiceBoundary 拆分目录）
    /// </summary>
    string ResolveOutputPath(string templateId, string entityName, CodegenProfile profile);

    /// <summary>是否支持该 CodegenProfile（如 MonolithicBackend 支持 deploymentTarget=monolithic）。</summary>
    bool Supports(CodegenProfile profile);
}

/// <summary>
/// 单体架构后端（现阶段默认实现）。
///
/// 对标 ABP CLI 默认模式：单 csproj + Vue3 + SqlServer。
/// 输出结构：
///   backend/Entitys/{ClassName}Entity.cs
///   backend/Services/{ClassName}Service.cs
///   backend/Interfaces/I{ClassName}Service.cs
///   backend/Entitys/Dto/{ClassName}/*.cs
///   sql/{TableName}.sql
///   frontend/{TableName}/index.vue
///   frontend/{TableName}/Form.vue
/// </summary>
public sealed class MonolithicCodegenBackend : ICodegenBackend
{
    public static readonly string Key = "csharp-monolithic";

    public string BackendKey => Key;
    public string DisplayName => "单体架构（C# + Vue3 + SqlServer）";

    /// <summary>
    /// 单体后端模板清单。
    /// 现阶段：3 个后端模板（Entity/Service/IService）— 第二期扩展为全套。
    /// 第二期将加入：Mapper/CrInput/UpInput/ListQueryInput/InfoOutput/ListOutput/DetailOutput。
    /// 第三期将加入：前端 Vue3 模板（index.vue/Form.vue/Detail.vue/api.ts）+ SQL + 工作流 + 菜单。
    /// </summary>
    private static readonly string[] MonolithicBackendTemplates =
    {
        VmTemplateIds.Entity,
        VmTemplateIds.Service,
        VmTemplateIds.IService,
    };

    public IReadOnlyList<string> SelectTemplates(CodegenProfile profile)
    {
        var templates = new List<string>(MonolithicBackendTemplates);

        // 未来扩展点（现阶段不启用，但结构已预留）：
        // if (profile.GenerateFrontend) templates.AddRange(FrontendTemplates);
        // if (profile.GenerateMobile) templates.AddRange(MobileTemplates);
        // if (profile.EnableWorkflow) templates.AddRange(WorkflowTemplates);

        return templates;
    }

    public string ResolveOutputPath(string templateId, string entityName, CodegenProfile profile)
    {
        // 单体架构：扁平目录结构
        return templateId switch
        {
            VmTemplateIds.Entity => $"Entitys/{entityName}Entity.cs",
            VmTemplateIds.Service => $"Services/{entityName}Service.cs",
            VmTemplateIds.IService => $"Interfaces/I{entityName}Service.cs",
            _ => $"Generated/{templateId.Replace('/', '_')}",
        };
    }

    public bool Supports(CodegenProfile profile)
        => profile.BackendKey == Key
           || (profile.TechStack == "csharp" && profile.DeploymentTarget == "monolithic");
}

/// <summary>
/// 代码生成后端注册表（选择器）。
/// 根据 CodegenProfile.BackendKey 选择对应的 ICodegenBackend 实现。
/// 现阶段只注册 MonolithicCodegenBackend。
/// </summary>
public interface ICodegenBackendRegistry
{
    /// <summary>根据 profile 选择后端。</summary>
    ICodegenBackend Resolve(CodegenProfile profile);

    /// <summary>注册的后端列表（供调试/展示）。</summary>
    IReadOnlyList<ICodegenBackend> All { get; }
}

public sealed class CodegenBackendRegistry : ICodegenBackendRegistry, JNPF.DependencyInjection.ITransient
{
    private readonly IReadOnlyList<ICodegenBackend> _backends;

    public CodegenBackendRegistry()
    {
        // 现阶段只注册单体后端
        // 未来注册：MicroserviceCodegenBackend, CloudNativeCodegenBackend
        _backends = new List<ICodegenBackend>
        {
            new MonolithicCodegenBackend(),
        };
    }

    public IReadOnlyList<ICodegenBackend> All => _backends;

    public ICodegenBackend Resolve(CodegenProfile profile)
    {
        // 精确匹配 BackendKey
        var backend = _backends.FirstOrDefault(b => b.BackendKey == profile.BackendKey);
        if (backend != null) return backend;

        // 回退：Supports 匹配
        backend = _backends.FirstOrDefault(b => b.Supports(profile));
        if (backend != null) return backend;

        // 最终回退：单体（默认）
        return _backends[0];
    }
}
