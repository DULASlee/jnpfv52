# Anti-Regression Rules — 防退化规则索引

> **分类：** L0/L1 项目规则
> 
> **来源：** 多处源文件

---

## 核心防退化铁律

### L0-03: Agent Runtime 保护

**来源：** `workflow-iron-law.md`

**规则：** 不得将 Agent Runtime 退化为 Workflow / Prompt Chain。

**检查点：**
- Runtime 必须保持自主决策能力
- 禁止硬编码流程
- 禁止线性 Prompt Chain

---

### L0-04: Capability Boundary

**来源：** `triple-key-iron-law.md`

**规则：** 不得将 Capability / Intelligence 倒灌到 Kernel。

**检查点：**
- Runtime.Core 不依赖 Capability
- Execution Boundary 不携带 Intelligence

---

### L0-02: 功能完整性

**来源：** `implementation-integrity-iron-law.md`

**规则：** 不得删除核心功能以换取实现便利。

**五禁令：**
1. 禁止给门控开逃逸通道
2. 禁止为唯一解析器引入第二源
3. 禁止改测试断言凑新行为
4. 禁止用快照重生成替代内容审查
5. 禁止跳过验收标准的核心项

---

## 防退化检查清单

### Architecture Drift

- [ ] 是否改变层级依赖方向？
- [ ] 是否引入循环依赖？
- [ ] 是否破坏 Frozen Contract？

### Contract Drift

- [ ] 是否改变 Public API？
- [ ] 是否引入 Breaking Change？
- [ ] 是否绕过权限检查？

### Capability Leakage

- [ ] 是否把 Capability 注入 Core？
- [ ] 是否把 Intelligence 注入 Execution？
- [ ] 是否引入隐藏状态？

### Workflow Regression

- [ ] 是否把 Runtime 改成 Workflow？
- [ ] 是否引入硬编码流程？
- [ ] 是否限制自主决策？

---

## Adversarial Review

**规则：** AI 必须主动站在"破坏系统"的角度审查。

### 破坏点分析

```
如果我要把 Runtime 退化成 Workflow，我会在哪里动手？
如果我要绕过 Lifecycle，我会在哪里动手？
如果我要偷偷扩大 Public API，我会在哪里动手？
```

---

## Hook 覆盖

| 检查 | Hook | 状态 |
|------|------|------|
| Multi-Tenant | `guard-tenant-filter.mjs` | ✅ |
| SQL Injection | `guard-sql-injection.mjs` | ✅ |
| Module Boundary | `guard-oa-module.mjs` | ✅ |
| Frontend Memory | `guard-frontend-leak.mjs` | ✅ |
| API Permission | `guard-auth.mjs` | ✅ |

---

## 关联文档

- `.claude/rules/workflow-iron-law.md` — Workflow Iron Law
- `.claude/rules/implementation-integrity-iron-law.md` — Implementation Integrity
- `.claude/rules/triple-key-iron-law.md` — Triple-Key
