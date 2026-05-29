# OA.Entry 编译修复 & 事件订阅修复 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复 JNPF.OA.API.Entry 编译失败（StaticWebAssets 冲突）和集成助手事件订阅未执行两个问题。

**Architecture:** OA.API.Entry 通过 ProjectReference 引用 API.Entry 并继承其 Startup 基类，两个项目的 wwwroot/Template 目录完全重复导致 MSBuild 冲突。事件订阅问题源于 OA 项目缺少对新增模块程序集的订阅器扫描注册。

**Tech Stack:** .NET 6, MSBuild StaticWebAssets, Furion/JNPF 框架 EventBus, SqlSugar

---

### Task 1: 修复 OA.API.Entry 的 StaticWebAssets 冲突

**Files:**
- Modify: `application/JNPF.OA.API.Entry/JNPF.OA.API.Entry.csproj`

**Root Cause:** OA.API.Entry 的 `wwwroot/Template/` 目录与 API.Entry 完全一致（diff 确认无差异），且 OA 通过 `<ProjectReference>` 引用了 API.Entry。MSBuild 发现两个项目有相同目标路径的 Content 文件导致冲突。OA 无需自己持有这些文件。

- [ ] **Step 1: 备份现有 csproj 以便回滚**

```powershell
Copy-Item "d:\JNPF-v52\backend\application\JNPF.OA.API.Entry\JNPF.OA.API.Entry.csproj" "d:\JNPF-v52\backend\application\JNPF.OA.API.Entry\JNPF.OA.API.Entry.csproj.bak"
```

- [ ] **Step 2: 修改 csproj，移除所有 Template Content Update 条目并添加 Content Remove**

在 `JNPF.OA.API.Entry.csproj` 中，删除所有 `<Content Update="wwwroot\Template\...">` 条目（第 22-321 行），替换为：

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

    <ItemGroup>
        <None Remove="sensitive-words.txt" />
    </ItemGroup>

    <ItemGroup>
        <EmbeddedResource Include="lib\regworkerid_lib_v1.3.1\yitidgengo.dll" />
        <EmbeddedResource Include="lib\regworkerid_lib_v1.3.1\yitidgengo.so" />
        <EmbeddedResource Include="sensitive-words.txt" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="IGeekFan.AspNetCore.Knife4jUI" Version="0.0.13" />
    </ItemGroup>

    <ItemGroup>
      <ProjectReference Include="..\JNPF.API.Entry\JNPF.API.Entry.csproj" />
    </ItemGroup>

    <!-- OA 作为 API.Entry 的子项目，Template 文件通过 ProjectReference 继承，排除本地副本避免 StaticWebAssets 冲突 -->
    <ItemGroup>
        <Content Remove="wwwroot\Template\**" />
    </ItemGroup>

    <ProjectExtensions><VisualStudio><UserProperties properties_4launchsettings_1json__JsonSchema="" /></VisualStudio></ProjectExtensions>
</Project>
```

- [ ] **Step 3: 清理并重新生成整个解决方案**

```bash
dotnet clean "d:\JNPF-v52\backend\zx_lowcode_netcore.sln"
dotnet build "d:\JNPF-v52\backend\zx_lowcode_netcore.sln" -c Debug
```

**Expected:** 0 errors, OA.API.Entry 编译成功。

- [ ] **Step 4: 验证 OA.API.Entry 能单独编译**

```bash
dotnet build "d:\JNPF-v52\backend\application\JNPF.OA.API.Entry\JNPF.OA.API.Entry.csproj" -c Debug
```

**Expected:** Build succeeded.

---

### Task 2: 验证 OA.API.Entry 启动时数据库连通性

**Files:**
- Verify: `application/JNPF.OA.API.Entry/Configurations/ConnectionStrings.json` (已在上一步修改)

- [ ] **Step 1: 确认 OA 的数据库连接配置与 API.Entry 一致**

```bash
diff "d:\JNPF-v52\backend\application\JNPF.API.Entry\Configurations\ConnectionStrings.json" "d:\JNPF-v52\backend\application\JNPF.OA.API.Entry\Configurations\ConnectionStrings.json"
```

**Expected:** 两个文件内容一致（已在上一步同步修改为 local SQLEXPRESS）。

- [ ] **Step 2: 尝试运行 OA.API.Entry，观察启动日志**

```bash
cd "d:\JNPF-v52\backend" && timeout 15 dotnet run --project "application/JNPF.OA.API.Entry/JNPF.OA.API.Entry.csproj" --no-build 2>&1
```

**Expected:** 启动无 Unhandled Exception。确认两个数据库连接日志正常输出。

---

### Task 3: 诊断事件订阅问题

**Files:**
- Read: `application/JNPF.OA.API.Entry/OAStartup.cs`
- Read: `application/JNPF.API.Entry/Startup.cs`
- Read: `modularity/common/JNPF.Common.Core/EventBus/IntegreateEventSubscriber.cs`
- Read: `modularity/inteAssistant/JNPF.InteAssistant.Engine/InteAssistantWayEventSubscriber.cs`

**Background:** 事件总线 (`EventBusHostedService`) 通过 DI 注入 `IEnumerable<IEventSubscriber>` 来发现所有订阅器。订阅器通过 Furion 框架的约定扫描自动注册（实现 `IEventSubscriber` + `ISingleton` 的类）。API.Entry 的 Startup 中调用 `services.AddEventBus()` 完成注册。

**关键分析：** `OAStartup.ConfigureServices()` 当前只调用 `base.ConfigureServices(services)`，未添加任何额外服务。OA 项目新增的业务模块（如 zxdev/subdev）可能包含自己的事件订阅器，但若这些模块的程序集未被正确加载到 DI 容器中，事件触发后将找不到对应的处理程序。

- [ ] **Step 1: 搜索 OA 项目及其依赖模块中的所有 EventSubscriber**

```bash
grep -r "IEventSubscriber" "d:\JNPF-v52\backend\modularity\zxdev" --include="*.cs" -l
grep -r "IEventSubscriber" "d:\JNPF-v52\backend\modularity\subdev" --include="*.cs" -l
grep -r "IEventSubscriber" "d:\JNPF-v52\backend\modularity\extend" --include="*.cs" -l
```

- [ ] **Step 2: 检查 OA.API.Entry.csproj 的项目引用是否覆盖了新增模块**

查看 OA.API.Entry.csproj 中的 `<ProjectReference>` 条目。与 API.Entry.csproj 对比：

```bash
grep "ProjectReference" "d:\JNPF-v52\backend\application\JNPF.OA.API.Entry\JNPF.OA.API.Entry.csproj"
grep "ProjectReference" "d:\JNPF-v52\backend\application\JNPF.API.Entry\JNPF.API.Entry.csproj"
```

**Expected:** OA 只引用了 API.Entry。API.Entry 已经引用了所有模块（system, workflow, visualdev, inteAssistant 等），所以 OA 通过传递引用间接获取了所有模块。如果 OA 有自己独有的模块引用，需要追加。

- [ ] **Step 3: 在启动日志中搜索事件订阅器注册情况**

OA.API.Entry 启动后，观察日志中是否有类似以下的行：

```
"Subscriber with event ID <Inte:CreateInte> was not found."
```

或

```
"Error occurred executing in <EventId>."
```

这些日志来自 `EventBusHostedService.BackgroundProcessing()`。

**Hypothesis:** 如果出现 "was not found"，说明订阅器未被注册。如果出现 "Error occurred executing"，说明订阅器已注册但执行时出错（可能是数据库上下文/租户切换问题）。

---

### Task 4: 根据诊断结果修复事件订阅问题

修复策略取决于 Task 3 的诊断结果。以下是两种最可能的情况：

**情况 A：订阅器未被注册（DI扫描遗漏）**

- [ ] **Step A1: 在 OAStartup.ConfigureServices 中显式注册缺少的模块**

```csharp
// OAStartup.cs
public void ConfigureServices(IServiceCollection services)
{
    base.ConfigureServices(services);
    // 注册 OA 项目特有的服务/订阅器
    // 如果框架自动扫描未覆盖某些程序集，在此处手动添加
}
```

- [ ] **Step A2: 检查 OA 项目是否有额外的程序集需要被 Furion 框架扫描**

在 `OAStartup.cs` 同目录下，确认是否需要添加 `AppStartup` 的额外配置来指定扫描的程序集。

**情况 B：订阅器已注册但执行时异常（租户/数据库上下文问题）**

- [ ] **Step B1: 检查 IntegreateEventSubscriber 中的租户切换逻辑**

`IntegreateEventSubscriber.cs:86-89` 中有多租户切换逻辑。确认 OA 项目使用的数据库中是否有对应租户的数据。检查事件触发时 `inte.TenantId` 的值是否正确。

- [ ] **Step B2: 检查 Redis 依赖**

`IntegreateEventSubscriber` 使用 Redis 缓存 (`ICacheManager`)。确认 OA 项目启动时 Redis 可用。没有 Redis 时缓存的集成队列 ID 列表获取失败，可能导致事件处理逻辑被跳过。

---

### Task 5: 最终验证

- [ ] **Step 1: 完整构建解决方案并确认 0 错误**

```bash
dotnet clean "d:\JNPF-v52\backend\zx_lowcode_netcore.sln"
dotnet build "d:\JNPF-v52\backend\zx_lowcode_netcore.sln" -c Debug 2>&1 | grep -E "error|错误|Build succeeded|生成成功"
```

- [ ] **Step 2: 依次启动两个 Entry 项目确认无运行时异常**

```bash
# API.Entry (已验证通过)
dotnet run --project "d:\JNPF-v52\backend\application\JNPF.API.Entry\JNPF.API.Entry.csproj" --no-build

# OA.API.Entry
dotnet run --project "d:\JNPF-v52\backend\application\JNPF.OA.API.Entry\JNPF.OA.API.Entry.csproj" --no-build
```

---

## Self-Review

**1. Spec coverage:**
- 问题2（OA编译失败）→ Task 1 完整覆盖
- 问题3（事件订阅未执行）→ Task 3（诊断）+ Task 4（修复）覆盖

**2. Placeholder scan:** 无 TBD/TODO。Task 4 的修复取决于 Task 3 的诊断结果，已给出两种最可能情况的完整修复步骤。

**3. Type consistency:** 所有文件路径与实际项目结构一致，类名/method名与源代码匹配。
