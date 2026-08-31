# Master Governance — 主控文件

> **目的：** 作为 Governance Foundation 的主入口，协调所有子模块
> 
> **生效日期：** 2026-08-31

---

## Governance 模块结构

```
00-governance/
├── MASTER-GOVERNANCE.md    ← 本文件
├── GOVERNANCE-INDEX.md     ← 规则映射表
├── L0-LAWS.md             ← L0 宪法索引
├── L1-PROJECT-RULES.md    ← L1 项目规则索引
├── L2-PHASE-RULES.md      ← L2 Phase 规则索引
├── HUMAN-GATE-RULES.md    ← Human Gate 规则
└── HUMAN-GATE-RULES.yaml   ← Human Gate 机器可读版本
```

---

## 核心职责

| 组件 | 职责 |
|------|------|
| MASTER-GOVERNANCE.md | 主入口，协调子模块 |
| GOVERNANCE-INDEX.md | 规则映射表（Single Source of Truth 索引）|
| L0-LAWS.md | L0 宪法级规则索引 |
| L1-PROJECT-RULES.md | L1 项目规则索引 |
| L2-PHASE-RULES.md | L2 Phase 规则索引 |
| HUMAN-GATE-RULES.md | Human Gate 决策规则 |
| HUMAN-GATE-RULES.yaml | Human Gate 机器可读版本 |

---

## L0/L1/L2 三级治理

### 优先级

```
L0 (宪法级) > L1 (项目级) > L2 (Phase 级)
```

### 强制执行层

| 层级 | 机制 | AI 能否绕过 |
|------|------|------------|
| **L0** | Hook exit 2 | 无法绕过 |
| **L1** | Hook exit 1 / Review | 可继续但警告 |
| **L2** | 纯约定 | 靠 AI 自觉 |

---

## Human Gate 快速参考

| ID | 名称 | Action | 典型场景 |
|----|------|--------|---------|
| H1 | 架构冲突 | PAUSE | Runtime ↔ Capability 冲突 |
| H2 | 需求冲突 | PAUSE | 需求 A vs 需求 B |
| H3 | Breaking Change | PAUSE + CR | 冻结 API 修改 |
| H4 | 跨 Section | PAUSE | Section 8 ↔ Section 9 |
| H5 | 安全/数据风险 | EMERGENCY | 数据丢失风险 |

---

## 规则加载顺序

1. L0-LAWS.md — 宪法索引
2. L1-PROJECT-RULES.md — 项目规则索引
3. L2-PHASE-RULES.md — Phase 规则索引（按当前 Phase）
4. HUMAN-GATE-RULES.yaml — Human Gate 机器可读规则

---

## 维护规则

### 添加新规则

1. **确定层级：** L0 / L1 / L2
2. **添加到源文件：** `.claude/rules/` 或 Section 文档
3. **更新 GOVERNANCE-INDEX.md：** 添加映射条目
4. **更新对应 L* 文件：** 添加索引条目

### 修改规则

1. **修改源文件**
2. **验证 GOVERNANCE-INDEX.md 映射正确**
3. **验证无冲突**

### 删除规则

1. **从 GOVERNANCE-INDEX.md 移除**
2. **从对应 L* 文件移除**
3. **归档源文件**

---

## 冲突检测

当遇到规则冲突时：

1. **优先级：** L0 > L1 > L2
2. **Frozen 优先：** Frozen Contract 优先
3. **Human Gate 优先：** H1-H5 触发时暂停
4. **业务优先：** Business First 凌驾

---

## 验证清单

- [x] 所有现有 Rules 已映射
- [x] L0/L1/L2 分类完成
- [x] Human Gate H1-H5 定义清晰
- [x] H2 与 H3 边界明确（frozen_contract_violation → H3）
- [x] 冲突处理规则明确

---

## 关联文档

- `../README.md` — Control Plane 入口
- `GOVERNANCE-INDEX.md` — 规则映射表
- `L0-LAWS.md` — L0 宪法索引
- `L1-PROJECT-RULES.md` — L1 项目规则索引
- `L2-PHASE-RULES.md` — L2 Phase 规则索引
- `HUMAN-GATE-RULES.md` — Human Gate 规则
- `.claude/rules/` — 规则源文件目录
