# PM 新流程澄清续跑 + 深度优化修复（2026-07-17）

## Why

新 4 步 PM 流水线（门控→骨架→九步→步骤③完善）在 E2E 暴露两类卡死：

1. **0 轮澄清 deepen 循环**：步骤③ LLM 返回 completed 但不产出 `pending_question`，编排器反复重跑整段流式完善（60–90s 无 UI 变化）。
2. **结构化澄清作答后无续跑**：用户经 `ClarificationCard` 提交答案（`ClarificationAnswered`）后，编排器只认 `PmClarificationTurn`，误回步骤①，SSE 无进展。

## What

- 澄清轮次不足时改走 `GenerateClarificationAsync(forceQuestions: true)` 专用出题，不再递归 `RefineFromAnalysisStreamAsync`。
- 新增「结构化澄清已作答 → 续跑步骤③」恢复分支（读 stable `clarification:requirement:*` + `answersText`）。
- 步骤①③ / PSpec LLM 长等待增加 15s heartbeat SSE。
- E2E 登录路由对齐 History 模式（`/login`、`/studio/ai/submit-requirement`）。

## Evidence

| 项 | 路径 |
|---|---|
| E2E 澄清卡片出现 | `.claude/evidence/e2e-pm-clarification-after-fix.png` |
| 步骤③卡住诊断 | `.claude/evidence/probe-pm-step3-*.json` |
| xUnit | `dotnet test … --filter FullyQualifiedName~Pm` 80/80 |
| 探测脚本 | `scripts/probe-pm-step3-stuck.mjs` |

## Status

- [x] 实现 + 单测 + E2E（pipeline 398，~70s 出追问卡片）
- [ ] 用户侧「答题→第2轮/说明书」全路径复验（需重启后端）
- [ ] `/opsx:archive` 合并本 change → `openspec/specs/studio-clarification/spec.md`
