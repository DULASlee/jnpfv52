# Admin CurrentUser NRE 诊断 — 暂停状态 (2026-07-07)

> **状态**：暂停（等环境稳定）。未 commit 的方案 A 改动保留在工作树。

## 背景
admin 登录后 `GET /api/oauth/CurrentUser` 返回 `code:500 + NRE`（`UserManager.GetUserInfo():421 User.SystemId`，User 为 null，来自 `_repository.GetSingle(u => u.Id == UserId)` 返回 null）。

## 已做（保留，不撤销）
1. **方案 A 代码（未 commit，工作树保留）**：
   - `backend/framework/JNPF.Extras.DatabaseAccessor.SqlSugar/TenantContext/AdminBypassGuard.cs`（新建，静态 `IsAdministrator()` 读 "Administrator" claim）
   - `SqlSugarDbContextProvider.cs:37` 加 `&& !AdminBypassGuard.IsAdministrator()`
   - `ColumnIsolationStrategy.cs:13` 加超管豁免 `return`
   - 三处都带 `// r4-safe:` 注释
   - framework 单独 build EXIT=0 ✅
2. **数据修复（DB 改动，保留）**：`UPDATE BASE_USER SET F_TENANT_ID='0' WHERE F_ACCOUNT='admin'`（admin 原本 NULL）
3. **诊断 log**：已全部撤销（UserManager / AdminBypassGuard / SqlSugarDbContextProvider 恢复干净）
4. **集成测试脚本**：`scripts/test-admin-bypass.mjs`（已 commit）

## ⚠️ 关键不确定性（用户洞察，重要）

**环境抖动导致诊断数据不可信**：
- 后端有其他工程师并行改 inteAssistant 代码（P9-S1 升级），导致 build/重启不停
- 症状：`port ready 0s`（dotnet run 不可能 0s 就绪）、build 在 `1 错误 ↔ 0 错误` 波动、`IrSchemaValidator.cs` 等文件被实时重写
- **我在这种状态下抓的运行时数据可能全是假象**：
  - `dbc.log: IsMultiTenant=False` —— 可能是后端未正常启动时的值
  - `bypass.log 空`（AdminBypassGuard 没调用）—— 可能是 curl 连到残留实例
  - `user.log 空`（User getter 没执行）+ NRE 在 User.SystemId —— 矛盾，说明抓的不是真实执行路径
  - 最新 NRE 日志时间戳（16:05）比 curl 时间（16:14）早 10 分钟 —— curl 可能没到真实后端

**因此**：我基于这些数据得出的"`IsMultiTenant=False` → ITenantFilter 不附加 → 推翻 ITenantFilter 根因"**这个推翻本身可能也是误判**。方案 A（ITenantFilter 超管豁免）可能是对的。

## 旧 debug_report.md（e897b473）的修正
`workspace/debug_report.md` 写的"根因：ITenantFilter 排除 admin"是 `[INFERRED, post-hoc]`，基于 SQL 模拟而非运行时验证。但我在抖动环境下又"推翻"了它——**两次结论都不可信**。该文档需环境稳定后用真实数据重写。

## 待办（环境稳定后恢复）
> 触发条件：其他工程师 inteAssistant 改完、后端能稳定跑（port ready 后等 ≥10s 才 curl）

1. 启动**一个稳定后端实例**（确认 :5000 listener PID 启动时间 > 5s 前，curl 返回非缓存响应）
2. 重新抓 `GetDbContext` 的 `IsMultiTenant`（确认真实值——是 False 还是抖动假象）
3. **dispatch jnpf-debugger**：netcoredbg-mcp 附加后端，断点 `UserManager.User` getter，抓：
   - `UserId` 实际值（claim name 是否对）
   - `GetSingle` 返回（null 还是 admin）
   - SqlSugar Context 的 QueryFilter 集合（到底附加了哪些 filter）
   - `GetSingle` 实际 SQL（用 SqlSugar AOP SQL log）
4. 确认 NRE 真根因后：
   - 若是 ITenantFilter（IsMultiTenant 真为 True）→ 方案 A 已就位，验证 AdminBypassGuard 生效
   - 若是 SoftDelete/别的 filter → 新修复
   - 若是 UserId claim 问题 → 改 claim 解析
5. 用真实数据重写 `debug_report.md`（更正或确认根因）

## 关联文件
- 旧（存疑）：`workspace/debug_report.md` (commit e897b473)
- 本状态：`workspace/debug-status.md`
- 测试脚本：`scripts/test-admin-bypass.mjs`
- 方案 A 改动：见"已做"第 1 项
