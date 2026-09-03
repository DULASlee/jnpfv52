# 未解决问题清单

> 团队共享，提交到 Git。AI 发现但未能在当前会话修复的问题。
> 每项 MUST 包含：问题描述、复现步骤、修复方案、影响评估。
> 人类定期审阅并决定优先级。

---

## 🔴 ISSUE-001：后端 NU5026 预存编译错误

**发现日期**：2026-06-05
**严重程度**：🟡 中
**状态**：🔴 待处理

**问题描述**：
`dotnet build JNPF.API.Entry.csproj --no-restore` 报错 NU5026：找不到 `JNPF.Extras.DatabaseAccessor.SqlSugar.xml` 文件

**复现步骤**：
1. 确保后端 API 未运行（避免 DLL 锁定干扰）
2. 执行 `dotnet build backend/application/JNPF.API.Entry/JNPF.API.Entry.csproj --no-restore`
3. 观察到 NU5026 错误

**根因分析**：
项目配置了 `<GenerateDocumentationFile>true</GenerateDocumentationFile>` 但 XML 文件未生成，NuGet pack 阶段找不到该文件

**修复方案**：
方案 A：在 JNPF.Extras.DatabaseAccessor.SqlSugar.csproj 中确保 XML 文档文件生成
方案 B：在 csproj 中设置 `<GenerateDocumentationFile>false</GenerateDocumentationFile>`（如果不需 XML 文档）
方案 C：hook 已通过 `-p:IsPackable=false` 绕过，不影响冒烟测试

**影响评估**：
- 不修复会导致：`dotnet build --no-restore` 失败，但不影响运行时
- 已在 hook 中用 `-p:IsPackable=false` 绕过
- 长期应修复以保持构建干净

---

---

## P14-SEM-01：ParametersMatch 显示形比对（ backlog，不修）

**发现日期**：2026-09-04（P13 冻结前自检）｜ **严重程度**：中 ｜ **状态**：P14 backlog

**问题描述**：CSharpResolver.ParametersMatch 用 ToDisplayString() 序数比对；string 命中、System.String 落空，同义不同形即 NotFound。
**复现**：ParametersMatchCharacterizationTests.FullyQualifiedSpelling_DoesNotMatch_CurrentLimitation（基线测试）
**修复方案**：P14 中解析请求类型为 ITypeSymbol 后用 SymbolEqualityComparer 比对并纳入 RefKind；以该测试为 before/after 基线
**影响评估**：不修则 P14 传全限定名默默 NotFound；修偏则基线测试变红

---

## P14-API-01：全异步管线（sync-over-async 残留，不修）

**发现日期**：2026-09-04 ｜ **严重程度**：低（当前宿主无 SynchronizationContext，未复现 deadlock）｜ **状态**：P14 backlog

**问题描述**：GetSemanticModel / ConditionalCompilationFacts.From / Resolver host 装配内阻塞等待异步 Roslyn API。
**修复方案**：P14 加 *Async 端到端 API + CancellationToken；同步方法薄转发或标 Obsolete
**影响评估**：不修则未来 ASP.NET/IDE 宿主有 deadlock 风险

---

## P14-SEM-02：TargetFramework/Configuration 语义源缺席（维持缺席）

**发现日期**：2026-09-04 ｜ **严重程度**：低（已显式缺席，未伪造）｜ **状态**：P14 backlog

**问题描述**：Roslyn Project 模型不提供 TFM/Configuration；ConditionalCompilationFacts 仅含 PreprocessorSymbols/OptimizationLevel/LanguageVersion。若 P14 需要，须找 MSBuild 属性源，不得猜测。
**修复方案**：P14 调研 MSBuild 属性读取或接受永久缺席
**影响评估**：低；条件编译存在性以 Roslyn 当前 Compilation 为准，不受影响
