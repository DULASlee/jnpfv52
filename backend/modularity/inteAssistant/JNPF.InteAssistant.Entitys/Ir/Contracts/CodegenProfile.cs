using System.Collections.Generic;

namespace JNPF.InteAssistant.Entitys.Ir.Contracts;

/// <summary>
/// 代码生成配置文件（对标 ABP CLI 的命令行选项，但从 IR 派生而非人工输入）。
///
/// 对标 ABP CLI 维度：
///   --database-provider → DatabaseProvider（sqlserver/mysql/postgresql/oracle）
///   --ui                → UiFramework（vue/react/angular）
///   --tiered            → DeploymentTarget（monolithic/microservice/cloud-native）
///   --mobile            → MobileTarget（uniapp/none）
///   --template-source   → TechStack（csharp/java/go/python/rust）
///
/// 默认值（现阶段）：sqlserver/vue3/monolithic/none/csharp
/// 未来：从 IR2_Architecture.deploymentTarget + 澄清问答答案派生。
///
/// 此 Profile 由编译器读取，决定：
///   1. 选哪个 ICodegenBackend（单体/微服务/云原生）
///   2. 模板内的条件分支（DB 列类型、前端框架语法、是否生成移动端）
/// </summary>
public sealed class CodegenProfile
{
    // ─── 对标 ABP CLI 选项 ───

    /// <summary>数据库类型（对标 --database-provider）。默认 sqlserver。</summary>
    public string DatabaseProvider { get; set; } = "sqlserver";

    /// <summary>前端框架（对标 --ui）。默认 vue3。</summary>
    public string UiFramework { get; set; } = "vue3";

    /// <summary>部署架构（对标 --tiered）。默认 monolithic。</summary>
    public string DeploymentTarget { get; set; } = "monolithic";

    /// <summary>移动端（对标 --mobile）。默认 none（不生成）。</summary>
    public string MobileTarget { get; set; } = "none";

    /// <summary>后端技术栈（对标 --template-source）。默认 csharp。</summary>
    public string TechStack { get; set; } = "csharp";

    // ─── 超越 ABP 的业务维度 ───

    /// <summary>是否生成工作流（IR2_SystemDesign.workflowNodes 非空时为 true）。</summary>
    public bool EnableWorkflow { get; set; } = false;

    /// <summary>是否生成权限（IR0_Skeleton.roleMatrix 非空时为 true）。</summary>
    public bool EnablePermission { get; set; } = false;

    /// <summary>实体数量（决定多实体循环次数）。</summary>
    public int EntityCount { get; set; } = 0;

    /// <summary>命名空间（从 SystemName 派生）。</summary>
    public string Namespace { get; set; } = "JNPF.Generated";

    // ─── 工厂方法 ───

    /// <summary>
    /// 从 IR 派生 CodegenProfile（编译时选项，零人工输入）。
    /// 现阶段：全部用默认值（单体/sqlserver/vue3/none/csharp）。
    /// 未来：从 IR2_Architecture.deploymentTarget + 澄清问答答案派生。
    /// </summary>
    public static CodegenProfile FromIr(
        SkeletonPayload? skeleton,
        ArchitecturePayload? architecture)
    {
        var profile = new CodegenProfile();

        // 从骨架派生命名空间 + 实体数 + 权限标志
        if (skeleton != null)
        {
            profile.Namespace = ToPascalCase(skeleton.SystemName) + ".Generated";
            profile.EntityCount = skeleton.EntityDrafts.Count;
            profile.EnablePermission = skeleton.RoleMatrix != null && skeleton.RoleMatrix.Roles.Count > 0;
        }

        // 未来：从架构派生部署目标 + 技术栈
        if (architecture != null)
        {
            // profile.DeploymentTarget = architecture.DeploymentTarget;  // 未来启用
            // profile.TechStack = architecture.TechStack;
        }

        return profile;
    }

    /// <summary>SystemName → PascalCase 命名空间</summary>
    private static string ToPascalCase(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "JNPF";
        // 移除空格/特殊字符，首字母大写
        var parts = name.Split(' ', '-', '_', '.');
        return string.Concat(parts.Where(p => !string.IsNullOrEmpty(p)).Select(p =>
            char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
    }

    /// <summary>后端标识（用于选择 ICodegenBackend 实现）。</summary>
    public string BackendKey => $"{TechStack}-{DeploymentTarget}";

    /// <summary>是否生成前端（MobileTarget != none 或 UiFramework != none）。</summary>
    public bool GenerateFrontend => UiFramework != "none";

    /// <summary>是否生成移动端。</summary>
    public bool GenerateMobile => MobileTarget != "none";
}

/// <summary>
/// IR2_Architecture 完整契约（P9-S1 遗留补建）。
///
/// 对标 ABP CLI 的"架构选择"，但固化进 IR。
/// 当前 architect-skill 只在 layered/cqrs 之间选（代码风格）。
/// 未来需扩展 deploymentTarget（部署架构）+ serviceBoundaries（微服务边界）。
/// </summary>
public sealed class ArchitecturePayload
{
    /// <summary>代码组织风格：layered / cqrs（当前 architect-skill 已产出）。</summary>
    public string Pattern { get; set; } = "layered";

    /// <summary>
    /// 部署架构（对标 ABP --tiered）：
    ///   monolithic（单体，默认）
    ///   microservice（微服务，按 ServiceBoundaries 拆分）
    ///   cloud-native（云原生，+Dockerfile +K8s +Helm）
    /// 现阶段固定 monolithic，未来从澄清问答派生。
    /// </summary>
    public string DeploymentTarget { get; set; } = "monolithic";

    /// <summary>后端技术栈：csharp / java / go / python / rust。现阶段固定 csharp。</summary>
    public string TechStack { get; set; } = "csharp";

    /// <summary>数据库类型：sqlserver / mysql / postgresql / oracle。现阶段固定 sqlserver。</summary>
    public string DatabaseProvider { get; set; } = "sqlserver";

    /// <summary>前端框架：vue3 / react / angular。现阶段固定 vue3。</summary>
    public string UiFramework { get; set; } = "vue3";

    /// <summary>模块列表（当前 architect-skill 已产出）。</summary>
    public List<string> Modules { get; set; } = new();

    /// <summary>
    /// 微服务边界（deploymentTarget=microservice 时用）。
    /// 每项定义一个服务的实体归属 + 服务间通信方式。
    /// 现阶段为空（单体不需要）。
    /// </summary>
    public List<ServiceBoundary> ServiceBoundaries { get; set; } = new();

    /// <summary>模块-实体归属（哪个实体属于哪个模块/服务）。</summary>
    public List<ModuleEntity> ModuleEntities { get; set; } = new();
}

/// <summary>微服务边界定义（未来用）。</summary>
public sealed class ServiceBoundary
{
    public string ServiceName { get; set; } = "";
    public string ServiceRoute { get; set; } = "";  // 服务间通信路由
    public List<string> EntityNames { get; set; } = new();  // 该服务负责的实体
    public string Communication { get; set; } = "http";  // http/grpc/event-bus
}

/// <summary>模块-实体归属（未来用，微服务拆分依据）。</summary>
public sealed class ModuleEntity
{
    public string ModuleName { get; set; } = "";
    public string EntityName { get; set; } = "";
}
