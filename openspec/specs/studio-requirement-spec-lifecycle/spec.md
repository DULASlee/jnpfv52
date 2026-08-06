# Studio 需求说明书生命周期（Requirement Spec Lifecycle）

> **Authority:** `deliverables/02-requirement-spec.md` 正文 + `BASE_AI_PIPELINE_S2_PROGRESS` 进度行  
> **Architecture:** `docs/architecture/requirement-spec-state-machine-refactor-plan.md`

## 1. 双状态机

| 枚举 | 粒度 | 用途 |
|------|------|------|
| `S2PipelineStage` | 细 | 门控→PM 完善→九步→澄清→渲染/确认→Finalize |
| `RequirementSpecPhase` | 粗 | 02 文档生命周期（Absent…Finalized/Superseded） |

## 2. 存储分层

| 层 | 位置 | 内容 |
|----|------|------|
| L1 | `deliverables/02-requirement-spec.md` | 正式 Markdown 全文（唯一正文源） |
| L2 | `BASE_AI_PIPELINE_S2_PROGRESS` | `pipelineStage` + `specPhase` + `clarRound` + hash |
| L3 | IR `requirement:{pipelineId}` | Working text（Refining only） |
| L4 | IR `requirement-spec-state:{pipelineId}` | 投影 metadata（无全文） |
| L5 | IR 九步/澄清事件 | 审计与 PM 终评输入 |

**Invariant：** Phase ≥ `Rendered` 时 Finalize/预览/下载 **MUST** 读 L1；**MUST NOT** 用 IR 事件全文兜底。

## 3. 合法转换（SpecPhase）

```
Absent/Refining → Rendered → Confirmed → PmReviewed → Finalized
Rendered|Confirmed → Superseded → Refining
```

Finalize 副作用：`AnalysisCompleted{finalized:true}` + `StageConfirmed{S2}`。

## 4. API

- **推进：** `POST /api/studio/skills/requirement-analysis/{pipelineId}/run`（唯一主路径）
- **读 02：** `GET …/spec-content` → `phase, pipelineStage, contentHash, markdown, canUserConfirm`
- **Deprecated：** `POST …/confirm-requirement-spec` → 转调 run

## 5. 正式版 Gate

Markdown MUST contain:

- `# 需求分析规格说明书`
- `请你确认需求分析说明书`

## 6. 验收

- 新 pipeline：Rendered 后 02 hash 稳定；Confirm 后 Finalize 不空 UserRequirement
- 343/407 抽样：Resolver Phase 与 UI 卡片一致
- `dotnet test …RequirementSpec` 绿
