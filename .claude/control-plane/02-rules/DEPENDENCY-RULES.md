# Dependency Rules — 依赖规则索引

> **分类：** L1 项目规则
> 
> **来源：** 多处源文件

---

## 层依赖规则

**来源：** `triple-key-iron-law.md` + Section 8/9 架构

### Runtime Layer Boundary

| From | To | 允许 |
|------|-----|------|
| Runtime.Core | Infrastructure | ✅ |
| Runtime.Core | Common | ✅ |
| Runtime.Core | Capability | ❌ |
| Capability | Runtime.Core | ✅ |
| Intelligence | Capability | ✅ |

---

## 三元组依赖

**来源：** `triple-key-iron-law.md`

### 依赖方向

```
tenantId → projectId → pipelineId
```

### 约束

- 禁止 pipelineId 当 projectId 使用
- 禁止 projectId 当 pipelineId 使用
- 禁止三元组缩写

---

## 模块依赖

**来源：** `architecture-redlines.md` R5

### 已知状态

| 模块 | 状态 |
|------|------|
| OA | 禁用 |
| IoT | 不存在 |
| MES | 不存在 |

---

## 依赖方向检查

**来源：** `engineering-laws.md`

### 允许

- Common → Infrastructure
- Application → Modularity
- Modularity → Infrastructure

### 禁止

- Infrastructure → Application
- Modularity → Common (循环)
- Application → Infrastructure (循环)

---

## Hook 覆盖

| 检查 | Hook | 状态 |
|------|------|------|
| Module Boundary | `guard-oa-module.mjs` | ✅ |

---

## 关联文档

- `.claude/rules/triple-key-iron-law.md` — Triple-Key
- `.claude/rules/architecture-redlines.md` — Architecture Redlines
- `.claude/rules/engineering-laws.md` — Engineering Laws
