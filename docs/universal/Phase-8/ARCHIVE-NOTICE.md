# Phase 8 · 文件归档说明（架构师重整 · 2026-08-30）

> **本目录定位**：Phase 8 数据库重构工作的**开发期执行证据**，仅保留操作层面的文件。
> **架构级长期资产**已迁移至 `docs/architecture/v52/database-modernization/`。

---

## 当前 Phase 8 目录内容

仅保留**开发期需要的文件**：

| 文件 | 用途 | 保留原因 |
|------|------|---------|
| `Phase-8-JNPF-Table-Refactoring-Master-Execution-Plan.md` | 阶段执行计划 | 历史计划文档，查询需要 |
| `Phase-8-KPI-Tracking.md` | KPI 跟踪 | 阶段 KPI 历史记录 |
| `Phase-8-Shadow-Mode-Plan.md` | 影子模式计划 | P8-A 阶段遗留 |
| `Phase-8-Shadow-Mode-Table-Selection.md` | 影子模式表选择 | P8-A 阶段遗留 |
| `phase-gate-state.md` | 治理闸门状态记录 | 阶段闸门历史 |
| `Production-Progress-Ledger.md` | 生产进度跟踪 | 进度跟踪需要 |
| `p8-a/` `p8-b/` `p8-c/`（目录） | 批次执行证据 | 22 个批次的 PRE-FLIGHT / SQL / evidence / closure |
| `p8-c/p8-c1-*`（决策文档） | 生产范围决策 | P8-C.1 决策历史 |

---

## 已迁移的架构级长期资产（2026-08-30 重整）

以下 6 个文件原在本目录下，已迁移至 `docs/architecture/v52/database-modernization/`：

| 原路径 | 新路径 | 受众 |
|--------|--------|------|
| `docs/universal/Phase-8/JNPF-数据库架构重构-成果报告.md` | `docs/architecture/v52/database-modernization/JNPF-数据库现代化治理-架构设计与工作成果报告.md` | 客户/管理层/团队工程师 |
| `docs/universal/Phase-8/JNPF-AI-数据库治理-转型报告.md` | `docs/architecture/v52/database-modernization/JNPF-AI-数据库治理-转型报告.md` | 管理层/技术委员会 |
| `docs/universal/Phase-8/JNPF-表级重构-管理层报告.md` | `docs/architecture/v52/database-modernization/JNPF-表级重构-管理层报告.md` | 管理层 |
| `docs/universal/Phase-8/JNPF-表级重构-技术变更目录.md` | `docs/architecture/v52/database-modernization/JNPF-表级重构-技术变更目录.md` | 架构师/DBA |
| `docs/universal/Phase-8/JNPF-表级重构-登记表.csv` | `docs/architecture/v52/database-modernization/JNPF-表级重构-登记表.csv` | AI/工具 |
| `docs/universal/Phase-8/Phase-8-最终关闭报告.md` | `docs/architecture/v52/database-modernization/Phase-8-最终关闭报告.md` | 项目历史归档 |

---

## 重整原则（架构师视角）

### 为什么这次重整

1. **`docs/architecture/v52/` 是架构文档的归属** — 根据 `ARCHITECTURE_DOC_RULES.md`，架构内参必须放在 `v52/`，根目录禁止新增正文。
2. **Phase 8 是开发阶段，不是架构资产** — `docs/universal/Phase-8/` 是特定工作阶段的执行目录，应仅放开发期证据。
3. **避免架构文档埋没** — 客户、管理层、架构师阅读时，找不到放在 `Phase-8/` 下的成果。

### 重整边界

| 类型 | 归类原则 | 位置 |
|------|---------|------|
| **架构级长期资产** | 受众为非执行人员、可被未来项目复用、记录架构设计 | `docs/architecture/v52/database-modernization/` |
| **开发期执行证据** | 受众为开发人员、批次执行证据、操作日志 | `docs/universal/Phase-8/` |
| **Skill 工程化使用文档** | 工具使用参考 | `docs/构建AI软件工程agent闭环体系/` |
| **架构决策记录（ADR）** | 决策历史 | `docs/adr/` |

---

## 关联资产索引

### 在本目录（Phase 8）
- 22 个批次的执行证据：`p8-b/batch-01..06/` 与 `p8-c/batch-07..28/`
- 阶段 KPI / 影子模式 / 闸门状态 / 进度跟踪（保留为本阶段操作日志）

### 架构级（v52/database-modernization/）
- 6 个主要成果文档（见上表）
- 详细阅读路径见 `docs/architecture/v52/database-modernization/README.md`

### ADR（决策记录）
- `docs/adr/ADR-019-table-refactoring-expert-skill-v1-freeze.md`
- `docs/adr/ADR-020-r2-comp-primary-validation.md`
- `docs/adr/ADR-021-triple-key-iron-law.md`
- `docs/adr/ADR-022-no-change-active-judgment.md`
- `docs/adr/ADR-023-schema-drift-pre-execution.md`

### Skill 工程化使用
- `docs/构建AI软件工程agent闭环体系/table-refactoring-expert-skill-v1.md`

---

> **本次重整由核心架构师于 2026-08-30 完成**。
> **原则**：架构资产归架构目录，开发证据归开发阶段目录。
> **结果**：所有文档在正确位置，未来项目复用更顺畅。


