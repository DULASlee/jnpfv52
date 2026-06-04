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
