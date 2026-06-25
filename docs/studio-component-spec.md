# Baobab-Studio 核心组件规格

**版本：** v1.0.0
**创建日期：** 2026-06-12
**关联里程碑：** Sprint 0-B 终验 → 阶段五（前端交互层）

---

## 概述

Baobab-Studio 是 JNPF v5.2 的 AI 驱动低代码 Studio。以下 5 个核心组件构成了其交互层骨架。

所有组件在 **Sprint 0-B 中仅交付骨架占位**（`src/views/studio/index.vue`），完整功能在 **阶段五（前端交互层）** 交付。

---

## 1. AiChatPanel — AI 对话面板

**用途：** 五阶段流水线的主交互入口。开发者在此与 LLM 进行多轮对话，AI 逐步生成/修改表单 Schema。

**骨架状态：** Studio 视图中的"流水线状态"面板（占位 `a-empty` 组件）

**阶段五规格：**

| 维度 | 规格 |
|---|---|
| 组件层级 | `src/views/studio/components/AiChatPanel.vue` |
| 核心依赖 | `jnpf-content-wrapper`、`a-card`、`a-input`（textarea mode）、`a-button`、`a-list` |
| 消息类型 | user / assistant / system / tool（对齐 `BASE_AI_PIPELINE_MESSAGE.F_ROLE`） |
| 流式支持 | SSE 流式渲染（`text/event-stream`），逐 token 打字机效果 |
| 阶段可视化 | 面板顶部显示当前阶段进度条（draft → generating → validating → compiling → done） |
| IR 预览 | 对话中可展开查看当前 `FormPageIR` JSON 树 |
| 版本控制 | 每次 LLM 交互产生新的 IR 快照，支持回退到历史版本 |
| Accessibility | 键盘 Enter 发送、Shift+Enter 换行、Esc 取消生成 |

**数据流：**
```
用户输入 → AiChatPanel → LlmGatewayService.ChatAsync() → SSE 流式返回 → 打字机渲染
                                                              ↓
                                       结构化输出 → irToSchema() → VisualDev 编辑器
```

---

## 2. IrDiffViewer — IR 差异对比器

**用途：** 并排对比 LLM 修改前后的 `FormPageIR` 差异，帮助开发者审查 AI 变更。

**骨架状态：** 未在 Day 9 骨架中独立展示（未来集成到 Studio 面板）

**阶段五规格：**

| 维度 | 规格 |
|---|---|
| 组件层级 | `src/views/studio/components/IrDiffViewer.vue` |
| 核心依赖 | `a-card`、`a-col`、自定义 diff 渲染（JSON patch） |
| 对比模式 | 并排（side-by-side）/ 统一（unified）两种视图 |
| 差异粒度 | 字段级（新增/删除/修改）、属性级（config 变更）、表达式级 |
| 高亮策略 | 新增 → 绿色背景、删除 → 红色背景、修改 → 黄色背景 |
| 操作按钮 | 接受全部 / 拒绝全部 / 逐条接受 |
| 数据源 | `FormPageIR` before/after JSON 对象 |
| 性能 | 200 字段以内的 IR diff < 100ms |

**数据流：**
```
LLM 生成新 IR → IrDiffViewer(beforeIR, afterIR) → 渲染差异树 → 用户逐条审核
                                                              ↓
                                                    接受 → 覆盖当前 IR
```

---

## 3. SelfPlayDashboard — 自对弈评估仪表板

**用途：** 展示 LLM 自我对抗评估（Self-Play Evals）的结果，监控五阶段流水线的生成质量。

**骨架状态：** Studio 视图中的"知识图谱"面板（Phase 2 激活）

**阶段五规格：**

| 维度 | 规格 |
|---|---|
| 组件层级 | `src/views/studio/components/SelfPlayDashboard.vue` |
| 核心依赖 | `a-card`、`a-row`/`a-col`、`a-statistic`、`a-progress`、ECharts |
| KPI 指标 | 生成成功率、平均重试次数、schema 有效性评分、IR validation error 数 |
| 图表 | 按阶段的成功率趋势图（折线图）、按 model 的评分对比（柱状图） |
| 数据源 | `BASE_AI_CALL_LOG` 聚合查询 + `BASE_AI_PIPELINE` 状态 |
| 刷新策略 | WebSocket 推送流水线状态变更 + 每 30s 轮询统计 |
| 时间范围 | 今日 / 本周 / 本月 / 自定义 |
| Accessibility | 图表含 `aria-label`、数据表格备选视图 |

**数据流：**
```
BASE_AI_CALL_LOG → AggregatedStats → SelfPlayDashboard → ECharts 渲染
BASE_AI_PIPELINE  → PipelineStatus   ↗
```

---

## 4. KnowledgeGraphExplorer — 知识图谱浏览器

**用途：** 可视化浏览和查询知识图谱（`BASE_KNOWLEDGE_NODE` / `BASE_KNOWLEDGE_EDGE`）。

**骨架状态：** Studio 视图中的"知识图谱"面板（Phase 2 激活）

**阶段五规格：**

| 维度 | 规格 |
|---|---|
| 组件层级 | `src/views/studio/components/KnowledgeGraphExplorer.vue` |
| 核心依赖 | `a-card`、`a-input-search`、ECharts（force-directed graph）/ `vis-network` |
| 可视化 | 力导向图（force-directed graph），节点按 label 着色 |
| 交互 | 点击节点展开邻居、拖拽、缩放、搜索高亮 |
| 查询模式 | 关键字搜索 + BFS 邻居扩展 + 路径查询（node A → node B） |
| 节点详情 | 侧边栏展示 Properties JSON + 关联边列表 |
| 数据源 | `KnowledgeGraphStore.QueryNeighborsAsync()` / `SearchNodesAsync()` |
| 性能 | 500 节点以内图的首次渲染 < 2s |
| Accessibility | 节点列表备选视图（表格模式）、键盘导航 |

**数据流：**
```
用户搜索 → KnowledgeGraphStore.SearchNodesAsync() → 节点列表
用户点击 → KnowledgeGraphStore.QueryNeighborsAsync(depth=1) → 子图渲染
```

---

## 5. NarrativePatternBrief — 叙事模式简报器

**用途：** 为 LLM 提供项目上下文"简报"——即从知识图谱中提取当前项目的领域模型、数据关系、表单模式，形成一段结构化的文本，作为 LLM prompt 的前缀。

**骨架状态：** 无 UI 组件，作为 `LlmGatewayService.ChatAsync()` 的内部 prompt 构建步骤。

**阶段五规格：**

| 维度 | 规格 |
|---|---|
| 路径 | `src/core/narrative/pattern-brief.ts`（纯逻辑）+ `src/views/studio/components/NarrativePatternBrief.vue`（配置 UI） |
| 核心逻辑 | 从 `KnowledgeGraphStore` 加载项目节点和边 → 构建 JSON → 渲染为 Markdown 文本 |
| 简报结构 | 项目名称 → 核心实体列表 → 实体间关系 → 表单模式 → 自定义提示语 |
| Prompt 注入 | 作为 `system` 角色的第一条消息注入 pipeline 对话 |
| 可配置项 | 简报长度（short/medium/long）、包含实体数上限、是否包含字段详情 |
| 数据源 | `KnowledgeGraphStore.QueryNeighborsAsync(projectNodeId, depth=2)` |
| 缓存 | 简报在流水线启动时生成一次，存内存，流水线结束时释放 |
| 性能 | 简报生成 < 500ms |

**数据流：**
```
流水线启动 → NarrativePatternBrief.generate(projectId) → Markdown 文本
                                                              ↓
                                         注入 LlmGatewayService.ChatAsync() system prompt
```

---

## 组件依赖关系

```
AiChatPanel
├── LlmGatewayService（API 调用）
├── IrDiffViewer（diff 渲染）
├── NarrativePatternBrief（prompt 前缀）
└── PipelineStatus（阶段可视化）

IrDiffViewer
├── FormPageIR（数据模型）
└── irToSchema()（逆向转换）

SelfPlayDashboard
├── AiCallLogService（统计数据）
├── BASE_AI_PIPELINE（流水线状态）
└── ECharts（图表渲染）

KnowledgeGraphExplorer
├── KnowledgeGraphStore（图谱查询）
└── ECharts / vis-network（图可视化）

NarrativePatternBrief
├── KnowledgeGraphStore（上下文加载）
└── Prompt Template（BASE_AI_PROMPT_TEMPLATE）
```

---

## 交付时间线

| 组件 | Sprint 0-B | 阶段五 |
|---|---|---|
| AiChatPanel | 骨架占位 | 完整交互 + SSE 流式 |
| IrDiffViewer | — | 完整 diff 对比 |
| SelfPlayDashboard | 骨架占位 | 完整仪表板 + ECharts |
| KnowledgeGraphExplorer | 骨架占位 | 完整图浏览器 |
| NarrativePatternBrief | — | 完整简报生成 + Prompt 注入 |

---

*本规格文档随阶段五开发推进持续更新。*
