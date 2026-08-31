# Batch 30+ Scope Clarification（2026-08-31）

## Chief Architect 明确

**Batch 30+ 仍然不是 "修复 Batch"。**

它首先是：

> **Gap Decision Batch**

30.1-30.7 输出是**经过证据验证的最终决策矩阵**，不直接产生生产 Schema 修改。

## 每个 Gap 必须明确落入 5 种终态之一

```
FIX              - 明确授权后续 Migration
NO_CHANGE        - 已证明符合 Target Contract
DEFERRED         - 已批准延后（需决策记录）
EXCLUDED         - 已批准排除（需决策记录）
BLOCKED          - 受限于外部约束（需决策记录）
```

## 当前 AI 工程师契约

```
AI Engineer      = STOP
JNPF Schema      = LOCKED
Next Action      = Batch 30+ Gap Review Bundle
Next Human Gate  = Batch 30+ Gap Review Acceptance
```

## 禁止（保持）

```
Schema DDL           = 0
CREATE INDEX         = 0
DROP                 = 0
Constraint Change    = 0
Column Change        = 0
ORM Change           = 0
Entity Change        = 0
Production Migration  = 0
```

## 额外约束

- 无需提前做任何其他工作
- 无需扩大范围
- 不继续 Batch 29 已完成的工作
- 不启动 Phase 31 (Migration Specification)
- 等待 Chief Architect 触发

## 触发条件

只有 Chief Architect 明确发送 "execute BATCH 30+ GAP REVIEW BUNDLE" 后，AI 工程师才执行 30.1-30.7。
完成后立即 STOP，不自动进入 Batch 30+ Gap Review Acceptance Gate。
