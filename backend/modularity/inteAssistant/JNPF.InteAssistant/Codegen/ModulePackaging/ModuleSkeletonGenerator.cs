using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using JNPF.InteAssistant.Entitys.Ir.Contracts;

namespace JNPF.InteAssistant.Codegen.ModulePackaging;

/// <summary>
/// P9-S3a 模块骨架生成器 — 生成自包含 JNPF 业务模块的"壳"文件。
///
/// 壳文件（非实体级，每模块仅一份）：
///   - {SystemName}.csproj          ← 引用 JNPF 框架的模块项目
///   - {SystemName}Module.cs        ← JnpfModule 注册（DI + 自动发现）
///   - Configurations/menus.json    ← 菜单注册数据
///   - frontend/api/index.ts        ← API 客户端
///   - frontend/routes.ts           ← 路由注册
///   - frontend/types.ts            ← TypeScript 类型定义
///   - README.md                    ← 模块说明
///
/// 实体级文件（Entity/Service/DTO）由现有的模板渲染管线产出，
/// 此生成器只负责"壳"——模子已浇好，实体材料由编译器填入。
///
/// 零 LLM：纯模板字符串拼接，确定性。
/// </summary>
public static class ModuleSkeletonGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    /// <summary>生成模块壳文件（返回 文件相对路径 → 内容 的字典）。</summary>
    public static Dictionary<string, string> Generate(
        SkeletonPayload skeleton,
        CodegenProfile profile)
    {
        var files = new Dictionary<string, string>();
        var systemName = !string.IsNullOrWhiteSpace(skeleton.SystemName)
            ? ToPascalCase(skeleton.SystemName)
            : "GeneratedModule";
        var ns = profile.Namespace;
        var moduleName = $"{systemName}Module";

        // ① .csproj — 引用 JNPF 框架核心
        files[$"{systemName}.csproj"] = GenerateCsproj(systemName);

        // ② {SystemName}Module.cs — JnpfModule 注册
        files[$"{moduleName}.cs"] = GenerateModuleClass(ns, moduleName);

        // ③ Configurations/menus.json — 从 FormPage list 页 + RoleMatrix 派生
        files["Configurations/menus.json"] = GenerateMenusJson(skeleton, profile);

        // ④ 前端壳文件
        var entityNames = skeleton.EntityDrafts.Select(e => e.EntityName).ToList();
        files["frontend/api/index.ts"] = GenerateFrontendApi(entityNames, systemName);
        files["frontend/routes.ts"] = GenerateFrontendRoutes(entityNames, systemName);
        files["frontend/types.ts"] = GenerateFrontendTypes(skeleton);
        files["frontend/README.md"] = GenerateFrontendReadme(systemName, entityNames);

        // ⑤ README.md
        files["README.md"] = GenerateReadme(systemName, profile, entityNames);

        return files;
    }

    // ─── 壳文件生成方法 ───

    private static string GenerateCsproj(string systemName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <TargetFramework>net8.0</TargetFramework>");
        sb.AppendLine("    <ImplicitUsings>enable</ImplicitUsings>");
        sb.AppendLine("    <Nullable>enable</Nullable>");
        sb.AppendLine($"    <RootNamespace>{systemName}</RootNamespace>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine();
        sb.AppendLine("  <!-- JNPF 框架核心引用 -->");
        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine("    <ProjectReference Include=\"$(JNPF_FRAMEWORK_PATH)\\JNPF.Common.Core\\JNPF.Common.Core.csproj\" />");
        sb.AppendLine("    <ProjectReference Include=\"$(JNPF_FRAMEWORK_PATH)\\JNPF\\JNPF.csproj\" />");
        sb.AppendLine("    <ProjectReference Include=\"$(JNPF_FRAMEWORK_PATH)\\JNPF.Extras.DatabaseAccessor.SqlSugar\\JNPF.Extras.DatabaseAccessor.SqlSugar.csproj\" />");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine("</Project>");
        return sb.ToString();
    }

    private static string GenerateModuleClass(string ns, string moduleName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using JNPF.Modules;");
        sb.AppendLine("using Microsoft.AspNetCore.Builder;");
        sb.AppendLine("using Microsoft.Extensions.Configuration;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// AI 生成业务模块 — {moduleName}");
        sb.AppendLine("/// 得益于 JnpfModule 自动发现，无需修改 Program.cs。");
        sb.AppendLine("/// 所有 Service 实现 IDynamicApiController，路由自动映射。");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("[JNPF.Modules.DependsOn]");
        sb.AppendLine($"public class {moduleName} : JnpfModule");
        sb.AppendLine("{");
        sb.AppendLine("    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)");
        sb.AppendLine("    {");
        sb.AppendLine("        // 模块 DI 注册（如需自定义服务，在此注册）");
        sb.AppendLine("        // Entity/Service 类通过 ITransient/IScoped/ISingleton 自动注册");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public override void OnApplicationInitialization(IApplicationBuilder app)");
        sb.AppendLine("    {");
        sb.AppendLine("        // 模块中间件注册（如需自定义中间件，在此注册）");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// 菜单注册 JSON — 从 FormPage list 页派生菜单项 + RoleMatrix 派生权限。
    /// 编译器确定性输出，安装时由模块加载器写入 BASE_MODULE 表。
    /// </summary>
    private static string GenerateMenusJson(SkeletonPayload skeleton, CodegenProfile profile)
    {
        var menus = new List<object>();
        var sort = 1;

        // 顶级菜单：系统名
        var systemName = !string.IsNullOrWhiteSpace(skeleton.SystemName)
            ? skeleton.SystemName
            : "业务系统";

        // 子菜单：每个实体一个管理菜单
        foreach (var entity in skeleton.EntityDrafts)
        {
            var displayName = !string.IsNullOrWhiteSpace(entity.DisplayName)
                ? entity.DisplayName
                : entity.EntityName;

            menus.Add(new
            {
                id = $"menu_{entity.EntityName.ToLowerInvariant()}",
                parentId = $"menu_{ToPascalCase(skeleton.SystemName).ToLowerInvariant()}",
                fullName = displayName + "管理",
                enCode = entity.EntityName.ToLowerInvariant(),
                icon = "icon-ym-tree-organization",
                type = "menu",
                urlAddress = $"/{entity.EntityName.ToLowerInvariant()}/list",
                sortCode = sort++,
                enabledMark = true,
            });
        }

        // 顶级菜单放最后（子菜单先注册）
        var fullMenus = new List<object>
        {
            new
            {
                id = $"menu_{ToPascalCase(skeleton.SystemName).ToLowerInvariant()}",
                parentId = "0",
                fullName = systemName,
                enCode = systemName.ToLowerInvariant().Replace(" ", ""),
                icon = "icon-ym-system",
                type = "directory",  // 目录类型（含子菜单）
                urlAddress = "",
                sortCode = 99,
                enabledMark = true,
            },
        };
        fullMenus.AddRange(menus);

        // 权限矩阵（从 RoleMatrix 派生按钮权限）
        var permissions = new List<object>();
        if (profile.EnablePermission && skeleton.RoleMatrix != null)
        {
            foreach (var entity in skeleton.EntityDrafts)
            {
                var entityPerms = new List<string> { "query", "add", "edit", "delete" };
                permissions.Add(new
                {
                    moduleEnCode = entity.EntityName.ToLowerInvariant(),
                    buttons = entityPerms,
                });
            }
        }

        var menuData = new
        {
            systemName,
            menus = fullMenus,
            permissions,
        };

        return JsonSerializer.Serialize(menuData, JsonOptions);
    }

    private static string GenerateFrontendApi(List<string> entityNames, string systemName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/**");
        sb.AppendLine($" * {systemName} 业务模块 API 客户端");
        sb.AppendLine(" * <auto-generated> by JNPF AI Codegen");
        sb.AppendLine(" */");
        sb.AppendLine();
        sb.AppendLine("import { defHttp } from '/@/utils/http/axios';");
        sb.AppendLine();

        foreach (var entity in entityNames)
        {
            var route = entity.ToLowerInvariant();
            sb.AppendLine($"// ─── {entity} ───");
            sb.AppendLine($"export const {route}Api = {{");
            sb.AppendLine($"  getList: (params?: any) => defHttp.get({{ url: '/api/{route}/list', params }}),");
            sb.AppendLine($"  getInfo: (id: string) => defHttp.get({{ url: `/api/{route}/${{id}}` }}),");
            sb.AppendLine($"  create: (data: any) => defHttp.post({{ url: '/api/{route}', data }}),");
            sb.AppendLine($"  update: (id: string, data: any) => defHttp.put({{ url: `/api/{route}/${{id}}`, data }}),");
            sb.AppendLine($"  delete: (id: string) => defHttp.delete({{ url: `/api/{route}/${{id}}` }}),");
            sb.AppendLine($"}};");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string GenerateFrontendRoutes(List<string> entityNames, string systemName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/**");
        sb.AppendLine($" * {systemName} 业务模块路由");
        sb.AppendLine(" * <auto-generated> by JNPF AI Codegen");
        sb.AppendLine(" */");
        sb.AppendLine();
        sb.AppendLine("import type { AppRouteModule } from '/@/router/types';");
        sb.AppendLine();
        sb.AppendLine($"const {systemName.ToLowerInvariant()}Routes: AppRouteModule[] = [");

        for (var i = 0; i < entityNames.Count; i++)
        {
            var entity = entityNames[i];
            var route = entity.ToLowerInvariant();
            sb.AppendLine($"  {{");
            sb.AppendLine($"    path: '/{route}',");
            sb.AppendLine($"    name: '{entity}List',");
            sb.AppendLine($"    component: () => import('./views/{entity}/index.vue'),");
            sb.AppendLine($"    meta: {{ title: '{entity}管理', icon: 'icon-ym-tree-organization' }},");
            sb.AppendLine($"  }},");

            // 表单页（弹窗或独立路由）
            sb.AppendLine($"  {{");
            sb.AppendLine($"    path: '/{route}/form',");
            sb.AppendLine($"    name: '{entity}Form',");
            sb.AppendLine($"    component: () => import('./views/{entity}/Form.vue'),");
            sb.AppendLine($"    meta: {{ title: '{entity}表单', hidden: true }},");
            sb.AppendLine($"  }},");
        }

        sb.AppendLine("];");
        sb.AppendLine();
        sb.AppendLine($"export default {systemName.ToLowerInvariant()}Routes;");
        return sb.ToString();
    }

    private static string GenerateFrontendTypes(SkeletonPayload skeleton)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/**");
        sb.AppendLine(" * 业务模块 TypeScript 类型定义");
        sb.AppendLine(" * <auto-generated> by JNPF AI Codegen");
        sb.AppendLine(" */");
        sb.AppendLine();

        foreach (var entity in skeleton.EntityDrafts)
        {
            sb.AppendLine($"export interface {entity.EntityName} {{");
            foreach (var field in entity.Fields)
            {
                var tsType = MapToTsType(field.Type);
                var optional = field.Required ? "" : "?";
                sb.AppendLine($"  {ToCamelCase(field.Name)}{optional}: {tsType};");
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string GenerateFrontendReadme(string systemName, List<string> entityNames)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {systemName} 前端模块");
        sb.AppendLine();
        sb.AppendLine("本目录包含 AI 生成的 Vue3 业务页面。");
        sb.AppendLine();
        sb.AppendLine("## 页面清单");
        foreach (var e in entityNames)
            sb.AppendLine($"- `{e}` — 列表页 + 表单页");
        sb.AppendLine();
        sb.AppendLine("## 统一页面风格");
        sb.AppendLine("- **列表页**（index.vue）：查询条件集合 + 操作按钮栏 + DataGrid 表格");
        sb.AppendLine("- **表单页**（Form.vue）：表单字段 + 提交/取消按钮");
        sb.AppendLine("- **详情页**（Detail.vue）：只读字段展示");
        sb.AppendLine();
        sb.AppendLine("## 注册方式");
        sb.AppendLine("将 `routes.ts` 导入 JNPF 前端路由配置即可。");
        return sb.ToString();
    }

    private static string GenerateReadme(string systemName, CodegenProfile profile, List<string> entityNames)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {systemName} — AI 生成业务模块");
        sb.AppendLine();
        sb.AppendLine("> 由 JNPF AI 原生低代码平台确定性编译器生成（零 LLM 代码生成）");
        sb.AppendLine();
        sb.AppendLine("## 模块信息");
        sb.AppendLine($"- **系统名**: {systemName}");
        sb.AppendLine($"- **编译后端**: {profile.BackendKey}");
        sb.AppendLine($"- **实体数**: {entityNames.Count}");
        sb.AppendLine($"- **技术栈**: {profile.TechStack} + {profile.UiFramework}");
        sb.AppendLine();
        sb.AppendLine("## 实体清单");
        foreach (var e in entityNames)
            sb.AppendLine($"- `{e}`");
        sb.AppendLine();
        sb.AppendLine("## 安装步骤");
        sb.AppendLine("1. 将本目录复制到 `backend/modularity/generated/`");
        sb.AppendLine("2. 在 API.Entry.csproj 添加 ProjectReference");
        sb.AppendLine("3. 执行 `Migrations/init.sql` 创建数据库表");
        sb.AppendLine("4. 导入 `Configurations/menus.json` 注册菜单");
        sb.AppendLine("5. 将 `frontend/` 复制到前端项目的 views 目录");
        sb.AppendLine("6. 重启后端（JnpfModule 自动发现注册）");
        return sb.ToString();
    }

    // ─── 辅助方法 ───

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "GeneratedModule";
        var parts = name.Split(' ', '-', '_', '.');
        return string.Concat(parts.Where(p => !string.IsNullOrEmpty(p)).Select(p =>
            char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var pascal = ToPascalCase(name);
        return char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }

    private static string MapToTsType(string fieldType)
    {
        var t = fieldType.ToLowerInvariant();
        if (t.Contains("int") || t.Contains("long") || t.Contains("big")) return "number";
        if (t.Contains("decimal") || t.Contains("float") || t.Contains("double")) return "number";
        if (t.Contains("bool")) return "boolean";
        if (t.Contains("date") || t.Contains("time")) return "string"; // ISO date string
        return "string";
    }
}
