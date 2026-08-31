# Orchestration Skill Anti-Patterns

## 反模式 1: 跳过 Phase
**问题：** 直接跳到实现，跳过 Discovery、Analysis、Design
**影响：** 缺乏上下文，容易偏离目标
**防止：** 必须完成所有 Phase，检查 Phase State 确保按顺序

## 反模式 2: Human Gate 滥用
**问题：** 任何问题都触发 Human Gate
**影响：** 人工干预过多，失去自主性
**防止：** 严格按照 H1-H5 触发条件，普通问题进入 Self-Repair

## 反模式 3: Phase 状态不一致
**问题：** Phase State 与实际执行不一致
**影响：** 后续 Phase 基于错误假设
**防止：** 每次 Phase 完成后更新状态，验证状态准确性

## 反模式 4: 缺少 Evidence
**问题：** 声称完成但缺少证据
**影响：** 无法验证真实性
**防止：** 每个 Phase 必须有 Evidence，Evidence Chain 必须完整

## 反模式 5: Skill 遗漏
**问题：** 应该加载的 Skill 没有加载
**影响：** 缺少必要检查
**防止：** 使用 Skill Routing Matrix，验证必需 Skills 已加载

## 反模式 6: Gate 决策草率
**问题：** 未充分验证就做 Gate 决策
**影响：** 错误进入下一 Phase
**防止：** 完整验证所有 Contract，记录决策理由
