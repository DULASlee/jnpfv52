# Studio 需求说明书生命周期（Requirement Spec Lifecycle）

> **Change:** `openspec/changes/20260718-requirement-spec-state-machine/`  
> **Authority:** IR Write Model + `deliverables/02-requirement-spec.md` 正文

## 1. Phase 枚举（唯一真相）

| Phase | 含义 | 用户可见 |
|-------|------|----------|
| `Absent` | 门控后尚无 working text | 完善需求中 |
| `Refining` | 步骤①–③ | 澄清/九步 |
| `Rendered` | 02 已 formal 落盘 | 预览/下载/确认 |
| `Confirmed` | 用户确认 | Finalize 进行中 |
| `PmReviewed` | PM 终评完成 | 同左或 review fail |
| `Finalized` | Analyst Finalize 完成 | 可进架构 |
| `Superseded` | 用户反馈作废 | 重跑完善 |

## 2. 存储分层

| 层 | 位置 | 内容 |
|----|------|------|
| L1 | `{StudioWorkspace}/{tenant}/{project}/{pipeline}/deliverables/02-requirement-spec.md` | 正式 Markdown 全文 |
| L2 | IR fragment `requirement-spec-state:{pipelineId}` | `{ phase, version, contentHash, contentLength }` |
| L3 | IR fragment `requirement:{pipelineId}` (`IR0_Requirement`) | Working text（Refining only） |
| L4 | `IR1_EventSpec` | 九步分析（PM 终评输入，非说明书） |

**Invariant：** Phase ≥ `Rendered` 时，Finalize/下载/预览 **MUST** 读 L1；**MUST NOT** 用 L3 working text 或 IR 事件全文兜底。

## 3. 合法转换

```
Absent/Refining → Rendered     (Render:  formal gate PASS + write L1)
Rendered → Confirmed           (Confirm: user + L1 exists)
Confirmed → PmReviewed         (PmReview)
PmReviewed → Finalized         (Finalize: analyst + AnalysisCompleted.finalized=true + StageConfirmed S2)
Rendered|Confirmed → Superseded (Supersede: user feedback)
Superseded → Refining          (ResumeAfterSupersede)
```

## 4. 读者矩阵

| 组件 | 允许读取 |
|------|----------|
| `IRequirementSpecStateResolver` | L1+L2+L3+事件序 |
| `RequirementAnalysisOrchestrator` | 仅 `RequirementSpecSnapshot` |
| `SkillHarness` / analyst-skill | `FormalMarkdown` via orchestrator（Phase ≥ Confirmed） |
| 前端 spec-content API | Resolver 输出 |
| PM 步骤①–③ | `WorkingText` only |

## 5. API

- **推进：** `POST /api/studio/skills/requirement-analysis/{pipelineId}/run`（唯一）
- **读 02：** `GET …/spec-content` 返回 `phase, version, contentHash, markdown`
- **Deprecated：** `POST …/confirm-requirement-spec` → 转调 run

## 6. 正式版 Gate

Markdown MUST contain:

- `# 需求分析规格说明书`
- `请你确认需求分析说明书`

## 7. 验收

- 新 pipeline：Rendered 后 02 hash 稳定；Confirm 后 Finalize 不空 UserRequirement
- 343/407 抽样：Resolver Phase 与 UI 一致
- `dotnet test …RequirementSpecState` 绿
