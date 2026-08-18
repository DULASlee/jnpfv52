# 需求说明书状态机 + 编排器 Resolver 重构（2026-07-18）

## Why

S2 阶段 BUG 根因：编排器无单一「说明书 Phase」真相，靠 scattered IR 事件推断 → PM/Analyst/前端各读各源（407 UserRequirement 空、双 confirm 路径等）。

## What

- 定义 `RequirementSpecPhase` + `S2PipelineStage` 双状态机；02 文件 = 正式正文唯一源
- L2 进度表 `BASE_AI_PIPELINE_S2_PROGRESS` + `IRequirementSpecStateResolver`
- IR 事件 payload 瘦身；投影 `requirement-spec-state:{pipelineId}`
- 编排器 Phase switch；废弃旧三轮 RunRoundAsync 与独立 confirm 主路径

## Status

- [x] P4 阶段 1：Resolver + xUnit
- [x] P4 阶段 2：L2 progress + 编排器写 progress
- [x] P4 阶段 3：payload 瘦身 + IR 投影 + StageConfirmed(S2)
- [x] P4 阶段 4：前端单确认卡片 + confirm API 转调 run
- [x] P4 阶段 5：删 RunRoundAsync 死代码 + 343/407 抽样单测 + OpenSpec 入主库
