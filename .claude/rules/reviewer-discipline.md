# Reviewer 纪律 — 质量审查专用规则包（V3.0 新增）

> **定位：** Reviewer 角色的唯一规则来源。其他规则文件的审查维度已聚合于此。
> **加载时机：** Phase REVIEW 阶段，按需加载（L2 层）。
> **设计原则：** Reviewer 不需要知道"规则为什么存在"，只需要知道"如何检查"和"如何分级"。

---

## 审查维度速查表（5 维度 × 3 级别）

| 维度 | 检查项 | 自动验证 | Reviewer 复核 | 置信度 |
|:---|:---|:---:|:---:|:---:|
| **D1 架构合规** | R1-R10 红线 | Hook L0 已拦截 | Reviewer 复核 Hook 漏检 | HIGH |
| **D2 工程铁律** | TODO/吞异常/未验证假设 | grep 扫描 | Reviewer 判断 | MED |
| **D3 专家陷阱** | Trap 1-14 | 部分可工具验证 | Reviewer 深度检查 | LOW-MED |
| **D4 代码质量** | 方法长度/重复/命名 | 工具可部分覆盖 | Reviewer 判断 | MED |
| **D5 测试覆盖** | 新增代码是否有测试 | 文件存在性检查 | Reviewer 判断 | HIGH |

> **关键优化：** D1（架构合规）的 R1-R10 已由 Hook L0 在写入时硬阻断，Reviewer **不需要重复检查**，只需确认"Hook 是否漏检"（极低概率）。Reviewer 的精力应集中在 D2-D5。

---

## 审查输出格式（三级质量门）

### 🔴 BLOCK（必须修复，阻塞流程）

触发条件：
- 发现 Hook L0 漏检的架构红线违规
- 发现可导致生产事故的代码（如：SQL 注入绕过参数化查询）
- 发现严重性能问题（如：无分页的全表查询）

```
[BLOCK] {规则ID} | 置信度: {HIGH/MED/LOW} | 文件:行号
  问题: {一句话描述}
  证据: {代码片段}
  修复: {具体代码}
  为什么Hook没拦住: {分析原因，用于优化Hook}
```

### 🟡 WARN（建议修复，不阻塞但需记录）

触发条件：
- 代码异味（方法 >50 行、重复代码、魔法值 >2位）
- 边界条件未处理（null 检查、并发安全）
- 测试覆盖不足（新增逻辑无对应测试）

```
[WARN] {规则ID} | 置信度: {HIGH/MED/LOW} | 文件:行号
  问题: {描述}
  风险: {不修复的后果}
  建议: {具体改进方案}
```

### 🟢 NOTE（信息提示，仅记录）

触发条件：
- 代码风格偏好（与项目惯例不一致但不影响功能）
- 可优化的实现方式
- 文档缺失（公共方法无 XML 注释）

---

## 自动验证工具链（Reviewer 专用）

| 工具 | 用途 | 命令 |
|:---|:---|:---|
| `grep-audit` | 扫描 TODO/吞异常/硬编码 | `grep -rn "TODO\|FIXME\|catch.*{}"` |
| `mapster-check` | 验证 Adapt 是否覆盖审计字段 | 检查 `Adapt` 调用前后是否有 `.Ignore` |
| `pagination-check` | 验证列表查询是否有分页 | `grep -rn "ToListAsync\|ToPageListAsync"` |
| `async-suffix-check` | 验证 IDynamicApiController 方法无 Async 后缀 | `grep -rn "Async\s*("` |
| `tenant-filter-verify` | 验证原生 SQL 是否含 TenantId | `grep -rn "SqlQuery\|SqlQueryable"` 后确认 |

---

## 反馈闭环协议（Reviewer → 规则进化）

发现新问题 MUST 按以下路径反馈：

```
发现新问题（不在现有规则中）
  │
  ├─ 是架构红线遗漏？ → 更新 architecture-redlines.md（R11+）
  │                     更新 Hook 覆盖矩阵
  │
  ├─ 是专家陷阱遗漏？ → 更新 jnpf-expert-traps.md（Trap 15+）
  │                     在本文档中标记检查方法
  │
  ├─ 是 Hook 误报/漏报？ → 更新对应 guard-*.mjs
  │                        更新 test-hooks.mjs 用例
  │
  └─ 是代码模式问题？ → 更新本文档的自动验证工具链
                        更新 coder-reminders.md
```

---

## 关联文件

- 架构红线（R1-R10）→ `architecture-redlines.md`（Reviewer 不复检，只确认 Hook 覆盖）
- 工程铁律 → `engineering-laws.md`（加载 Law 2 验证方法论）
- 专家陷阱 → `jnpf-expert-traps.md`（加载 Trap 检查清单）
