---
name: spec
description: 查询 OpenSpec 知识库（specs、changes、ADRs）。当用户问架构决策、项目能力、规格说明、或想理解系统设计原理时触发。
---

# Spec Query — OpenSpec Knowledge Base

查询项目的 OpenSpec 知识库，快速定位架构决策、能力规格、变更提案。

> **OpenSpec 职责：** 仅知识库，不写代码、不跟日常 tasks（见 `.cursor/rules/toolchain-division.mdc`）

## 执行步骤

### Step 1: 列出知识库内容

读取并展示 OpenSpec 目录结构：

```bash
# 能力规格（已定稿）
ls openspec/specs/

# 变更提案（进行中）
ls openspec/changes/

# 架构决策记录
ls openspec/adr/
```

输出格式：

```
## OpenSpec 知识库

### 能力规格（specs/）— 已定稿
| 规格 | 描述 | 状态 |
|------|------|------|
| jnpf-v52-workspace | JNPF v5.2 工作区基线 | ✅ 定稿 |
| iot-capability-phase1 | IoT/MES 能力边界 Phase 1 | ✅ 定稿 |
| frontend-align-dist-v1 | 前端对齐方案 v1 | ✅ 定稿 |

### 变更提案（changes/）— 进行中
| 提案 | 描述 | 阶段 |
|------|------|------|
| add-logging-audit-spec | 日志审计规格 | 草稿 |

### 架构决策记录（adr/）
| ADR | 标题 | 决策 |
|-----|------|------|
| ADR-001 | 拒绝本地向量 RAG | 采用 OpenSpec 知识库替代 |
```

### Step 2: 根据用户意图定位

询问用户想查询什么：

| 用户意图 | 查询位置 |
|---------|---------|
| "MES/IoT 能做什么" | `openspec/specs/iot-capability-phase1/spec.md` |
| "项目基线包含什么" | `openspec/specs/jnpf-v52-workspace/spec.md` |
| "前端架构方案" | `openspec/specs/frontend-align-dist-v1/spec.md` |
| "为什么不用 RAG" | `openspec/adr/ADR-001-reject-local-vector-rag.md` |
| "日志审计怎么设计" | `openspec/changes/add-logging-audit-spec/` |

### Step 3: 读取并摘要

读取用户指定的规格文件，输出结构化摘要：

```
## 规格摘要：[规格名]

### 核心结论
- [1-3 句话概括]

### 关键约束
- [列出硬性约束]

### 实现状态
- [已实现 / 规划中 / 评估中]

### 相关文档
- [关联的 docs/ 或代码位置]
```

### Step 4: 提供下一步建议

根据查询结果建议：
- 如果查询的是能力规格 → 建议阅读对应的 `docs/架构迭代/` 评估文档
- 如果查询的是变更提案 → 建议查看 `tasks.md` 了解实施进度
- 如果查询的是 ADR → 建议了解决策背景
