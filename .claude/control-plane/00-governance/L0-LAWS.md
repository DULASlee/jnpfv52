# L0 — Immutable Engineering Laws（宪法级）

> **层级：** 绝对不可违反的工程铁律
> 
> **执行层：** L0 硬阻断（Hook exit 2）
> 
> **来源：** 所有 L0 规则的内容存储在 `GOVERNANCE-INDEX.md` 引用的源文件中

---

## 核心铁律

### L0-01: Frozen Contract 保护

**来源：** `.claude/rules/business-first-iron-law.md`

**规则：** 不得破坏已冻结的 Public Contract、API Surface、Database Contract。

**强制要求：**
- API Freeze 时必须建立 Baseline
- 任何修改必须通过 Contract Change Request
- Breaking Change 必须 Human Gate

---

### L0-02: 功能完整性

**来源：** `.claude/rules/implementation-integrity-iron-law.md`

**规则：** 不得删除核心功能以换取实现便利。"Minimal implementation" 不得被解释为"砍掉核心功能"。

**强制要求：**
- 实现驱动测试，不是测试驱动实现
- 五禁令：门控逃逸、唯一源破坏、修改断言、重生成快照替代审查、跳过验收

---

### L0-03: Agent Runtime 保护

**来源：** `.claude/rules/workflow-iron-law.md`

**规则：** 不得将 Agent Runtime 退化为 Workflow / Prompt Chain。

**强制要求：**
- Runtime 必须保持自主决策能力
- 禁止退化模式：硬编码流程、线性 Prompt Chain

---

### L0-04: Capability Boundary

**来源：** `.claude/rules/triple-key-iron-law.md`

**规则：** 不得将 Capability / Intelligence 倒灌到 Kernel。

**强制要求：**
- Runtime.Core 不依赖 Capability
- Execution Boundary 不携带 Intelligence

---

### L0-05: 测试诚信

**来源：** `.claude/rules/implementation-integrity-iron-law.md`

**规则：** 不得为了通过测试修改测试掩盖实现缺陷。测试失败先查实现，非先改测试。

**强制要求：**
- 改测试断言前必须回答原意图
- 禁止"喂门控"

---

### L0-06: Breaking Change 控制

**来源：** `.claude/rules/architecture-redlines.md`

**规则：** Breaking Change 必须经 Human Gate 审批，不得静默引入。

**强制要求：**
- Public API Breaking Change → H3
- Database Contract Breaking Change → H3
- Frozen Contract Modification → H3

---

### L0-07: Evidence-Driven

**来源：** `.claude/rules/workflow-iron-law.md`

**规则：** 验证证据优先于"看起来正确"。声称完成前必须有客观证据。

**强制要求：**
- 所有声称必须有可追溯证据
- Evidence Chain 必须完整

---

### L0-08: 自主闭环

**来源：** `.claude/rules/workflow-iron-law.md`

**规则：** 不得跳过 Implementation → Test → Review → Repair → Verification 闭环。

**强制要求：**
- 4 环节强制：Self Evaluation → Self Test → Self Repair → Reviewer Review
- 缺少任一环节状态不得标记为完成

---

### L0-09: 三元组完整性

**来源：** `.claude/rules/triple-key-iron-law.md`

**规则：** 所有数据实体必须携带 tenantId/projectId/pipelineId，三者完整、独立、可分离。

**强制要求：**
- 禁止三元组缩写为二元组
- 禁止 pipelineId 当 projectId 使用
- 禁止 projectId 当 pipelineId 使用

---

### L0-10: 多租户隔离

**来源：** `.claude/rules/architecture-redlines.md` R4

**规则：** 新 SqlSugar 查询必须确保租户过滤生效。漏过滤 = 跨租户数据泄漏。

**强制要求：**
- Ado.SqlQuery / SqlQueryable / 原生 SQL 必须手动加 WHERE TenantId
- Updateable/Deleteable 必须链式调用 .Where()

---

### L0-11: SQL 注入防御

**来源：** `.claude/rules/architecture-redlines.md` R7

**规则：** 动态 SQL 必须参数化。禁止字符串拼接用户输入到 SQL。

**强制要求：**
- 禁止 $"SELECT ... WHERE Name = '{userInput}'"
- 禁止 string.Format("SELECT ...", ...)
- 动态表名/列名必须白名单验证

---

### L0-12: 前端内存安全

**来源：** `.claude/rules/architecture-redlines.md` R6

**规则：** setTimeout/setInterval/EventSource/WebSocket 必须遵循 6 条铁律。

**强制要求：**
- 定时器返回值必须保存到变量
- onUnmounted 必须清理所有定时器
- EventSource 重连必须有上限（MAX_RETRIES）

---

### L0-13: API 权限声明

**来源：** `.claude/rules/architecture-redlines.md` R8

**规则：** 每个 IDynamicApiController 必须声明权限属性。

**强制要求：**
- [AllowAnonymous] - 公开端点
- [SecurityDefine("权限码")] - 角色受限
- [Authorize] - 已认证即可访问

---

## L0 强制执行

| L0 ID | Hook | 拦截内容 |
|--------|------|---------|
| L0-09 | - | Triple-Key（code-reviewer 检测） |
| L0-10 | `guard-tenant-filter.mjs` | 跨租户数据泄漏 |
| L0-11 | `guard-sql-injection.mjs` | SQL 注入 |
| L0-12 | `guard-frontend-leak.mjs` | 内存泄漏 |
| L0-13 | `guard-auth.mjs` | API 权限缺失 |

---

## 关联文档

- `GOVERNANCE-INDEX.md` — 完整规则映射表
- `.claude/rules/` — 规则源文件目录
