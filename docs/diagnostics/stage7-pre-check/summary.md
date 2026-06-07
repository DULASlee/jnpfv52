# 阶段 7 前置检查汇总

> 日期：2026-06-07
> 参与：工程师 A（检查 1/3/6）+ 工程师 B（检查 2/4/5）+ 架构师（汇总）

---

## 检查结果概览

| 检查项 | 工程师 | 结果摘要 | 阶段 7 影响 |
|---|---|---|---|
| 1.1 性能监控工具 | A | MiniProfiler 唯一 APM，无生产级可观测性 | OTel 从零引入，无冲突 |
| 1.2 TraceId/Activity | A | 37 处使用，TraceIdMiddleware 已兼容 Activity.Current?.Id | 低风险，可与 OTel 共存 |
| 1.3 日志配置 | A | Serilog JSON 输出 + FromLogContext | 可通过 Serilog.Sinks.OpenTelemetry 桥接 OTLP |
| 2.1 CI/CD 配置 | B | GitHub Actions 3 流水线（CI + CD Staging + CD Production） | 扩展现有，勿重建 |
| 2.2 构建配置 | B | Directory.Build.props, global.json (SDK 8.0.410), 3 个 .editorconfig | Analyzer 可加至 Build.props |
| 3.1 DataAnnotation | A | 仅 3 处 [Required]（框架 Options），业务代码 0 处 | 迁移成本极低 |
| 3.2 自定义验证 | A | DataValidationAttribute + SensitiveDetectionAttribute（框架级），无 FluentValidation | 从零创建 ~80-100 Validator |
| 4.1 DynamicApiController | B | 路由模板 `api/{module}/[controller]/{action}`，框架已有版本机制 | 激活现有能力即可 |
| 4.2 路由模板 | B | 非统一格式，115 文件含 HTTP 动词特性，无现有版本号段 | 需统一方案 |
| 4.3 Swagger | B | 按 Tag 分组，JWT Bearer，支持多文档 | 可直接与 API 版本联动 |
| 5.1 迁移机制 | B | 混合模式（CodeFirst + 手动 SQL），127 实体，7 个 SQL 脚本 | 需新建受控迁移系统 |
| 5.2 Schema 管理 | B | SQL Server，2 DB（主库 + sundial），无版本历史表 | DbUp / 自定义 Runner |
| 5.3 Outbox 表 | B | 已通过 outbox_migration.sql 创建 | 纳入迁移管理 |
| 6.1 现有 Analyzer | A | Roslynator 4.3.0 + StyleCop 1.2.0-beta.406（CI_BUILD 门控） | 移除门控，无条件启用 |
| 6.2 .editorconfig | A | 3 文件 ~140+ 规则，无 DiagnosticSuppress | 按目录添加 severity override |
| 6.3 存量文件清单 | A | JNPF001:37处, JNPF002:4处(生产), JNPF003:24处, JNPF006:3处 | 分级抑制策略 |

---

## 各任务方案微调

### 7.1 OpenTelemetry — 无需调整，补充细节

**原方案：** 引入 OpenTelemetry SDK + ASP.NET Core / HttpClient instrumentation

**检查发现：**
- MiniProfiler 是唯一 APM，开发工具而非生产级 → OTel 无冲突
- TraceIdMiddleware 已兼容 `Activity.Current?.Id` → OTel 的 Activity 可被自动复用
- Serilog 配置完善 → 加 `Serilog.Sinks.OpenTelemetry` 即可桥接 OTLP

**微调：**
- 增加 `Serilog.Sinks.OpenTelemetry` NuGet 包
- `SerilogBootstrap.Configure()` 中添加条件 Sink：`WriteTo.OpenTelemetry()`
- TraceIdMiddleware 中补充 `Activity.Current?.AddTag("traceId", traceId)` 以丰富 Span 数据
- 保留现有 TraceIdMiddleware（与 OTel 互补），不替换

---

### 7.2 CI/CD — 从「新建」调整为「扩展」

**原方案：** 新建 `.github/workflows/ci.yml`

**检查发现：**
- 项目已有完整的 3 流水线 GitHub Actions（ci.yml + cd-staging.yml + cd-production.yml）
- 覆盖：编译、测试、Docker 构建推送、SSH 零停机部署、健康检查
- 缺失：代码质量门禁（SonarQube）、回滚机制、.dockerignore

**调整为：**
- 不新建流水线，扩展现有 ci.yml
- 添加：SonarQube 质量门禁 job、dotnet format 校验、Code Coverage 上报
- 补充：`.dockerignore` 文件、`build.sh`/`Makefile` 本地构建便利脚本
- 修复：后端 Dockerfile 中 `dotnet:6.0` → `dotnet:8.0`（与 global.json 对齐）

---

### 7.3 FluentValidation — 无需调整，补充规模估算

**原方案：** 从零创建 FluentValidation 集成

**检查发现：**
- DataAnnotation 几乎不存在（3 处 `[Required]` 在框架 Options，业务代码 0 处）
- FluentValidation 完全不存在
- 现有框架验证：DataValidationAttribute（类型级验证模式）+ SensitiveDetectionAttribute（敏感词）

**微调：**
- 初始 Sprint: 创建 ~20 个高优先级 Validator（LoginInput, UserCrInput, UserUpInput, RoleCrInput 等）
- 后续 Sprint: 扩展至 ~80-100 覆盖所有 DTO
- 集成点：`Program.cs` 的 `AddInject()` 链中添加 `AddFluentValidationAutoValidation()`
- 保留 DataValidationAttribute 管道（并行运行，非替换）
- 无 DataAnnotation 迁移负担 → 无后向兼容顾虑

---

### 7.4 API 版本控制 — 从「引入外部库」调整为「激活框架能力」

**原方案：** 使用 `Asp.Versioning.Mvc` 外部库

**检查发现：**
- **框架已内置版本支持**：`VersionSeparator`("v"), `VersionInFront`(true), `ApiDescriptionSettings.Version`
- 现有 ~115 个 Service 使用非版本化路由 `api/{module}/[controller]/{action}`
- `SpecificationDocumentBuilder` 已支持多组 Swagger Doc（`GroupOpenApiInfos`）

**调整为：**
- **不引入外部版本库**，激活 JNPF 框架内置能力
- 路由方案：`api/v1/system/System/{id}`（URL path versioning）
- 粗粒度：`[ApiDescriptionSettings(Module="v1")]` 在 Service 类级别
- 细粒度：`[ApiDescriptionSettings(Version="2")]` 在 Action 方法级别
- Swagger：每个版本一个 Document Group，通过 `GroupOpenApiInfos` 配置
- 默认为 v1 实现向后兼容（无版本号路由 → 路由到 v1）

---

### 7.5 数据库迁移 — 保持原方案，补充具体选型

**原方案：** 新建迁移项目

**检查发现：**
- 当前为无治理的混合模式：127 实体（SqlSugar CodeFirst 特性）+ 7 个手动 SQL 脚本
- 无版本历史表、无执行顺序保证、无回滚
- 2 个数据库：主库 + jnpf_sundial（需独立迁移上下文）
- Outbox 表已通过 `outbox_migration.sql` 创建

**微调：**
- **选型建议：DbUp**（轻量、无 EF 依赖、纯 SQL 脚本、与 SqlSugar 兼容）
- 创建 2 个迁移上下文：`MainDbMigrations` + `SundialDbMigrations`
- 脚本重命名：`0001_initial_schema.sql`, `0002_outbox.sql`, `0003_logging.sql` 等
- 创建 `SYS_SCHEMA_VERSION` 表记录已执行脚本
- 将现有 7 个 SQL 脚本纳入迁移项目并分配序号
- CodeFirst 保留用于本地开发和新模块快速原型，生产部署使用 DbUp 脚本

---

### 7.6 Roslyn Analyzer — 无需调整，补充抑制策略

**原方案：** 新建 `JNPF.Analyzers` 项目

**检查发现：**
- Roslynator + StyleCop 已存在但仅在 `CI_BUILD=true` 时启用
- 存量禁止模式：JNPF001(37处), JNPF002(4处生产), JNPF003(24处), JNPF006(3处)
- 无自定义 JNPF Analyzer
- `dotnet.ruleset` 已抑制 ~80+ 规则

**微调：**
- 创建 `JNPF.Analyzers` 项目含 4 个 Analyzer（JNPF001/002/003/006）
- **移除 CI_BUILD 门控** — StyleCop + Roslynator 在所有构建中启用
- **分级抑制策略：**

| 规则 | 框架代码 (`framework/**`) | 应用代码 | 严重性 |
|---|---|---|---|
| JNPF001 | suggestion（.editorconfig） | warning | Warning |
| JNPF002 | #pragma（ADR-002 模式） | warning | Warning |
| JNPF003 | suggestion（.editorconfig） | warning | Warning |
| JNPF006 | N/A | error（#pragma 抑制 Quartz 接口） | Error |

- 在 `backend/.editorconfig` 中添加：
  ```
  [framework/**/*.cs]
  dotnet_diagnostic.JNPF001.severity = suggestion
  dotnet_diagnostic.JNPF003.severity = suggestion
  ```

---

## 总体评估

### 无需调整的任务

- **7.1 OpenTelemetry** — 当前可观测性几乎为零，从零引入无冲突
- **7.3 FluentValidation** — DataAnnotation 不存在，无迁移负担
- **7.6 Roslyn Analyzer** — 存量模式已精确统计，抑制策略清晰

### 需要方案微调的任务

- **7.2 CI/CD** — 从「新建」调整为「扩展现有 3 流水线」
- **7.4 API 版本控制** — 从「外部库」调整为「激活框架内置能力」
- **7.5 数据库迁移** — 选型 DbUp，建立 2 个迁移上下文

### 关键风险

| 风险 | 严重性 | 缓解措施 |
|---|---|---|
| 后端 Dockerfile 用 .NET 6.0 vs global.json 8.0 | 中 | 更新为 .NET 8.0 |
| ImageSharp 3.0.2 5 个已知漏洞（2 高危） | 中 | 升级到最新安全版本 |
| CI_BUILD 门控导致本地开发不跑 Analyzer | 低 | 移除门控，所有构建生效 |
| 数据库迁移无执行历史 | 高 | 阶段 7.5 优先解决 |

### 阻塞项

**无阻塞项。** 所有检查均未发现阻止阶段 7 启动的问题。6 个任务均可按微调后的方案并行推进。
