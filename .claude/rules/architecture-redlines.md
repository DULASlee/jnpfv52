# JNPF Architecture Redlines (架构铁律)

> **定位：** 本文档是 JNPF v5.2 项目**唯一**的架构级铁律清单。任何架构级约束 MUST 在此登记。
> 编码规范、调试流程、测试纪律等非架构级规则 → 各自独立 rule 文件。
> **新增条款流程：** 提出 → 团队讨论 → 在此文档追加 → Hook 实现（如可达 L0）→ `test-hooks.mjs` 补测试。

---

## 执行层级（AI 能否绕过）

| 层级 | 机制 | 说明 |
|---|---|---|
| **L0 硬阻断** | Hook `exit 2` | AI 无法绕过。`guard-*.mjs` 在 Write/Edit 前拦截违规内容。 |
| **L1 警告** | Hook `exit 1` | AI 收到警告但可继续。需人工把关。 |
| **L2 约定** | 纯自然语言 | 靠 AI 自觉。长会话漂移率 ~50%。违反由 code-reviewer 子代理在 Step 6 检测。 |

> 验证命令：`node scripts/test-hooks.mjs`（28 用例覆盖 R4/R5/R6/R7/R8 + 基础守卫 + MultiEdit）

---

## 红线清单

### R1 — API Generation（API 自动生成）

**规则：** Service 实现 `IDynamicApiController` 自动映射 API。NEVER 手写 Controller 类。

**理由：** JNPF 的路由、参数绑定、响应包装全部由框架根据接口自动生成。手写 Controller 与框架冲突，路由重复、响应格式不一致。

**后果：** 手写 Controller → 重复路由注册 / 绕过 RESTfulResult 包装 / API 文档缺失。

**执行层：** L2 | **Hook：** 无（建议未来加路径检测拦截 `Controller.cs` 新建）

**关联陷阱 (jnpf-expert-traps.md)：**
- Trap 1: 重命名 Service 方法 = URL 变更 = 全前端 404
- Trap 6: 方法名带 Async 后缀 → 路由含 `Async`，前端调用 404
- Trap 9: Service 类 public 方法 = 公开 API 端点

---

### R2 — Unified Response（统一响应格式）

**规则：** `RESTfulResult<T>` 自动包装所有返回值。业务异常用 `Oops.Bah()`，系统异常用 `Oops.Oh()`。NEVER 手动 `new RESTfulResult<T>()` 或 throw raw `Exception`。

**理由：** 框架自动将返回值包装为 `{ code, data, msg }`。手动再包一层 → 双层嵌套 → 前端解析失败。

**关键映射：**
- `Oops.Bah("msg")` → HTTP 200 + `{ code: 非200, msg }` → 前端展示 msg
- `Oops.Oh("msg")` → HTTP 500 + error log → 前端展示 "Internal Server Error"
- `code: 600` → JWT 过期，前端自动跳转登录

**后果：** Oops.Bah/Oops.Oh 混用 → 用户看到 "Internal Server Error" 而非真正的业务提示。

**执行层：** L2 | **Hook：** 无

**关联陷阱：**
- Trap 4: Oops.Bah vs Oops.Oh 用错 = 用户看到错误信息
- Trap 5: 手动 `new RESTfulResult<T>()` → 双层 data 嵌套

---

### R3 — Codegen Boundary（代码生成边界）

**规则：** 生成代码有 bug → 修 `.vm` 模板源码。NEVER 直接改模板输出目录下的 `.cs`/`.vue` 文件。

**理由：** 直接改输出文件 → 下次生成时被覆盖 → bug 回归。修模板才是可持续方案。

**后果：** 改输出文件 → 代码生成时被覆盖 → 用户报告 "之前修好的 bug 又出现了"。

**执行层：** L2 | **Hook：** 无

**关联陷阱：**
- Trap 12: `.vm` 模板用 Velocity 语法，不是 C#。`#if($x)` 而非 `if (x == null)`

---

### R4 — Multi-Tenant Isolation（多租户隔离）⚠️ 最高安全风险

**规则：** 新 SqlSugar 查询 MUST 确保租户过滤生效。漏过滤 = 跨租户数据泄漏。

**强制要求：**
1. `Queryable<T>()` 自动附加 `ITenantFilter`（全局查询过滤器），正常使用即可
2. `Ado.SqlQuery` / `SqlQueryable` / 原生 SQL → MUST 手动加 `WHERE TenantId = @tid`
3. `Updateable<T>` / `Deleteable<T>` → MUST 链式调用 `.Where(...)` 限定租户范围
4. NEVER 调用 `DisableGlobalFilter("TenantFilter")` 除非 DBA 级跨租户管理（需加 `// r4-safe: <理由>` 注释豁免）

**后果：** 查询不带 TenantId → 租户 A 看到租户 B 的数据 → 数据安全事故。

**执行层：** **L0** | **Hook：** `guard-tenant-filter.mjs` — 拦截 DisableGlobalFilter / Updateable无Where / 原生SQL无WHERE

**关联陷阱：**
- Trap 7: ITenantFilter 只在根 Queryable 自动生效，子查询/JOIN/Ado.SqlQuery 不自动过滤
- Trap 8: Updateable/Deleteable 不自动附加 TenantId，无 Where = 跨租户修改全部数据
- Trap 13: 自定义 Global Filter 优先级可能覆盖 TenantFilter，加后 MUST 验证 `ToSql()` 输出

---

### R5 — Module Boundary（模块边界）

**规则：** OA 模块已禁用，NEVER 修改其代码。IoT/MES 模块未创建，NEVER scaffold 新文件。

**已知状态：**
- `JNPF.OA.API.Entry` → **禁用**，禁止写入
- `JNPF.IoT.*` → **不存在**，禁止创建
- `JNPF.MES.*` → **不存在**，禁止创建

**后果：** 修改禁用模块 → 引入未测试的代码路径 → 部署后不可预期行为。

**执行层：** **L0** | **Hook：** `guard-oa-module.mjs` — 拦截 OA/IoT/MES 路径下的文件写入

---

### R6 — Frontend Memory Safety（前端内存安全）

**规则：** 前端 `setTimeout`/`setInterval`/`EventSource`/`WebSocket` MUST 遵循 6 条铁律。

**完整规则：** → `.claude/rules/frontend-memory-leak.md`

**6 条铁律摘要：**
1. 定时器返回值 MUST 保存到变量
2. `onUnmounted` MUST 清理所有定时器
3. EventSource 重连 MUST 有上限（MAX_RETRIES）
4. `onerror` 中 NEVER 直接同步调用 `connect()` — 必须经 `setTimeout` + 计数器
5. EventSource URL MUST 通过 `buildEventSourceUrl()` 加 `/dev` 前缀 + `?token=`
6. EventSource 时序：先 `connectSSE()` 再 `POST /execute`

**后果：** 组件销毁后定时器/SSE 持续运行 → 内存泄漏 → 浏览器卡死。

**执行层：** **L0** | **Hook：** `guard-frontend-leak.mjs` — 拦截无 clear / 无 retry cap / onerror 直连

---

### R7 — SQL Injection Defense（SQL 注入防御）

**规则：** 动态 SQL MUST 参数化。NEVER 用字符串拼接用户输入到 SQL 语句。

**完整规则：** → `.claude/rules/sql-safety.md`

**高危模式（全部 L0 阻断）：**
- `$"DROP TABLE {tableName}"` / `$"DELETE FROM ..."`
- `$"SELECT ... WHERE Name = '{userInput}'"`
- `string.Format("SELECT ...", ...)`
- `Ado.SqlQuery($"...")` / `Ado.ExecuteCommand($"...")`

**白名单例外：** 动态表名/列名 → 白名单验证后方可拼接。

**后果：** SQL 注入 → 数据泄露 / 数据删除 / 权限提升。

**执行层：** **L0** | **Hook：** `guard-sql-injection.mjs` — 拦截 $"...SQL..." / string.Format(SQL) / Ado+$

---

### R8 — API Permission Declaration（API 权限声明）

**规则：** 新 API（IDynamicApiController 实现类）MUST 声明权限属性：`[AllowAnonymous]` / `[SecurityDefine]` / `[Authorize]`。

**背景：** `JwtHandler` 当前 bypass JWT 校验（临时态）。无权限声明的 API = 未认证即可访问。

**三种合法声明：**
- `[AllowAnonymous]` — 公开端点（登录、健康检查）
- `[SecurityDefine("权限码")]` — 角色受限端点
- `[Authorize]` — 已认证即可访问

**后果：** 新增 API 无权限声明 → 未认证用户可直接调用 → 越权访问。

**执行层：** **L0** | **Hook：** `guard-auth.mjs` — 拦截含 `IDynamicApiController` 但无权限属性的 .cs 文件写入

---

### R9 — Architect Fidelity（架构师指令忠实执行）

**规则：** 编码前 MUST 输出需求提取清单（`📋 需求提取清单`）；编码后逐条标注实现状态（`✅已实现 / ⚠️偏离 / ❌未实现`）。

**流程：** → `.claude/rules/workflow.md` Step 1.5

**偏离/未实现需附：** 理由 + 审批记录。无审批记录 → 流程违规，MUST 退回补救。

**执行层：** L2 | **Hook：** 无（输出 gate，code-reviewer 在 Step 6 检查）

---

### R10 — Bug Discovery Protocol（Bug 发现强制上报）

**规则：** 在任何代码（含相邻代码、被调用模块、依赖）中发现 BUG，MUST 执行结构化上报，NEVER 沉默跳过。

**协议：** → `.claude/rules/engineering-laws.md` Law 1（**单一信源**，勿在本文档重复）

**严重级别与动作：**
| 级别 | 定义 | 动作 |
|---|---|---|
| P0 | 数据安全/崩溃/数据丢失 | 不等审批，先修复再汇报 |
| P1 | 功能错误/逻辑缺陷 | 暂停编码，等用户审批后修复 |
| P2 | 代码异味/性能隐患 | 记录到 todo_write，Step 7 报告中列出 |
| P3 | 样式/文案 | 记录后继续 |

**后果：** 发现 bug 沉默跳过 → 技术债务累积 → 生产事故。

**执行层：** L2 | **Hook：** 无（输出 gate）

---

## Hook 覆盖矩阵

| 红线 | Hook | 拦截内容 | 测试覆盖 |
|---|---|---|---|
| R4 | `guard-tenant-filter.mjs` | DisableGlobalFilter / Updateable无Where / 原生SQL无WHERE | 5 用例 |
| R5 | `guard-oa-module.mjs` | OA/IoT/MES 路径写入 | 4 用例 |
| R6 | `guard-frontend-leak.mjs` | setTimeout无clear / EventSource无retry / onerror直连 | 4 用例 |
| R7 | `guard-sql-injection.mjs` | $"SQL" / string.Format(SQL) / Ado+$ | 3 用例 |
| R8 | `guard-auth.mjs` | IDynamicApiController 无权限属性 | 3 用例 |
| R1-R3,R9,R10 | — | L2 约定，无 hook | code-reviewer 子代理检测 |
| 安全扫描 | `guard-write.mjs` | 密钥文件 / 空文件 / 硬编码密钥 / eval / 命令注入 / SQL拼接(.cs) | 6 用例 |

---

## 新增红线检查清单

添加新架构红线前，完成以下检查：

- [ ] 是否有明确的违规后果（不是"不好"而是"生产事故/数据泄漏/系统宕机"）？
- [ ] 是否可自动化检测（正则/静态分析）？→ 可检测 → 实现 L0 Hook
- [ ] 是否不可自动化？→ L2 约定 → 写入 code-reviewer 检查维度
- [ ] 是否在 `test-hooks.mjs` 中添加了测试用例？
- [ ] 是否在本文档中登记并更新 Hook 覆盖矩阵？
- [ ] 是否更新了关联的 trap 文件（如涉及 jnpf-expert-traps.md）？
