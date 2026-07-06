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

## 🔴 ISSUE-002：guard-write.mjs 八层守卫合并未完成 —— R4-R8 L0 防护实质性缺失

**发现日期**：2026-07-07
**严重程度**：🔴 高（P1 —— L0 防护宣称但未实现，R4 多租户漏过滤可能致跨租户数据泄漏）
**状态**：🔴 待处理
**发现来源**：jnpf-tester/jnpf-debugger 子 agent 任务 T9 回归步骤（pre-existing，与本会话改动无关）

**问题描述**：
CLAUDE.md Hooks 表与 `.claude/rules/architecture-redlines.md` 宣称 `guard-write.mjs` 实现"统一八层守卫 L1-L8"，其中 L4=模块边界R5 / L5=多租户R4 / L6=注入R7 / L7=权限R8 / L8=前端泄漏R6（全部 L0 硬阻断，AI 无法绕过）。实际：

1. 旧独立 guard（`guard-oa-module.mjs` / `guard-sql-injection.mjs` / `guard-auth.mjs` / `guard-tenant-filter.mjs` / `guard-frontend-leak.mjs`）**已删除**（Glob `.claude/hooks/*.mjs` 仅余 7 个：guard-bash / guard-finish / guard-reviewer / guard-skill-load / guard-write / hook-lib / session-scheduler）
2. `guard-write.mjs` 全文仅含 **L1/L2/L3/L4** 四层，且 **L4 是"AI 开发态工作区隔离"**，与 CLAUDE.md 宣称的"L4=模块边界R5"不符
3. **L5/L6/L7/L8 根本不存在于文件中** —— R4/R5/R6/R7(完整)/R8 均未实现
4. `scripts/test-hooks.mjs` 仍 `runHook('guard-oa-module.mjs', …)` 等引用旧文件名 → MODULE_NOT_FOUND → exit 1 → **28 用例全 FAIL**

**复现步骤**：
1. `echo '{"tool_name":"Write","tool_input":{"file_path":"backend/application/JNPF.OA.API.Entry/Foo.cs","content":"x"}}' | node .claude/hooks/guard-write.mjs; echo "exit=$?"` → 期望 exit=2（R5 BLOCK），**实际 exit=0**（放行）
2. `node scripts/test-hooks.mjs` → 28 用例全 FAIL，失败模式统一"期望 2 实际 1"（BLOCK 类）/ "期望 0 实际 1"（ALLOW 类）

**根因分析**：
CLAUDE.md 记载"已删除的独立 guard (oa/sql/auth/tenant/leak) 已合并为 guard-write L4-L8"。但合并**从未完成**——只迁移了 L1(密钥)/L2(空文件)/L3(通用安全扫描，含部分 SQL/eval/XSS)，L4 被替换为新的"工作区隔离"用途，原 R4-R8 逻辑随旧文件删除而丢失。test-hooks.mjs 未同步更新。

**修复方案**（代码级）：
1. **恢复守卫逻辑**：从 git 历史（旧独立 guard 文件最后的提交）提取 R4/R5/R6/R7/R8 检测逻辑，在 `guard-write.mjs` 补为 L5-L9（或恢复独立文件 + settings.json hook 注册）
   - L5 R4：拦截 `DisableGlobalFilter("TenantFilter")` / `Updateable<T>()` 无 `.Where(...)` / 原生 SQL 无 `WHERE TenantId`
   - L6 R5：拦截 `JNPF.OA.*` 写入 / `JNPF.IoT.*` / `JNPF.MES.*` 创建
   - L7 R7：完整 SQL 注入（L3 现仅部分覆盖 `$"SELECT/INSERT/UPDATE/DELETE/DROP..."` + `string.Format(SQL)` + `Ado+$`）
   - L8 R8：拦截含 `IDynamicApiController` 但无 `[AllowAnonymous]/[SecurityDefine]/[Authorize]` 的 .cs
   - L9 R6：拦截 `setTimeout/setInterval/EventSource` 无 clear / 无 retry cap / onerror 直连
2. **修测试**：`scripts/test-hooks.mjs` 的 `runHook` 调用从旧文件名改为 `guard-write.mjs`，payload 契约对齐（content 而非独立 guard 的字段）
3. **跑回归**：`node scripts/test-hooks.mjs` 28 用例全绿
4. **修文档**：核对 CLAUDE.md Hooks 表 L4-L8 标签与实际实现一致

**影响评估**：
- 不修复会导致：R4 多租户 / R5 模块边界 / R6 前端泄漏 / R8 权限声明 四条 L0 红线**无 hook 防护**，仅靠 AI 自觉（L2，长会话漂移率 ~50%）。R7 部分覆盖。`architecture-redlines.md` 的"AI 无法绕过"承诺**虚假**
- 修复工作量：~2-4 小时（恢复 5 个 guard 逻辑 + 修 28 用例测试 + 文档核对）
- 临时缓解：code-reviewer 子代理在 Phase 6 仍会检测 R4-R8（L2 约定），但非硬阻断

---
