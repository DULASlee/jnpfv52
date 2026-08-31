# JNPF V5.2 知识图谱文档集

> 用途：为 DKEE（领域知识进化引擎）提供结构化的初始种子数据
> 创建日期：2026-06-11
> 状态：初版完成，可被 graphify 重新提取

---

## 文档清单

| 文档 | 内容 | 覆盖度 | 来源 |
|------|------|--------|------|
| `01-domain-model.md` | 领域划分、实体关系、业务规则、RBAC/工作流/表单领域模型 | 首次结构化 | 代码 + graph.json |
| `02-architecture-skeleton.md` | 部署拓扑、分层架构、中间件管线、租户隔离、Outbox、安全风险 | 已有 65%，整理迁移 | graph.json + ADR |
| `03-frontend-ir-status.md` | IR 类型定义、67 组件映射、函数签名分析、eval 修复记录、AI 探针 | 首次结构化 | 代码 + jnpf-survey |
| `04-ai-capability-gap-analysis.md` | AI 当前状态（零）、竞品AI矩阵、小白AI冲击、差异化方向、路线图 | 首次撰写 | 公开资料 + 设计文档 |
| `production-func-analysis.txt` | 生产环境 Schema 函数复杂度原始分析 | 从外部迁入 | jnpf-survey/phase0/ |

---

## 与 graphify 知识图谱的关系

```
 docs/
    ├── knowledge-graph/     ← 本目录：结构化知识（手工撰写）
    │   └── 可被 graphify 重新提取为 graph.json 节点
    │
    ├── architecture/         ← 架构文档（graphify 已提取）
    ├── adr/                  ← 架构决策记录（graphify 已提取，含 Phase 8 ADR-019~023）
    ├── frontend-architecture/← 前端重构设计（graphify 已提取）
    └── ...

 graphify-out/
    └── graph.json            ← 1497 nodes, 1616 edges（自动提取）
        └── 本目录文档将成为新的语义节点
```

---

## 下一步

1. graphify --update 重新提取，将本目录文档纳入知识图谱
2. DKEE Writer 从 IR 直接生成知识图谱（非文档路径）
3. 每季度更新一次领域模型和架构骨架

## 注意

> Phase 8 决策沉淀至 `docs/adr/ADR-019~023`，**不**在本目录（knowledge-graph 是 graphify 提取用，非决策沉淀用）。
> Skill 工程化使用文档至 `docs/构建AI软件工程agent闭环体系/table-refactoring-expert-skill-v1.md`。

