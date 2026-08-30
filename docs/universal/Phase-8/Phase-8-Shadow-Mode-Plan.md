# Phase 8 — Shadow Mode Plan

**Phase**: 8 — P8-A
**Status**: PREPARED
**Version**: v1.0
**Date**: 2026-08-30

---

## 1. 目的

Shadow Mode 是 Phase 8 的第一道生产安全门。

目标：

> 在不对生产数据库做任何修改的前提下，通过 AI 评估 + 人类独立评审的双轨对照，观察 Skill 的判断是否与人工判断一致，建立真实生产基线。

**不是**：

- 人工帮 AI 做判断
- AI 结论人类顺着确认
- 统计学意义上的泛化证明

**是**：

- AI Result vs Human Independent Review 的逐项对照
- 真实生产场景下的 Safety + Productivity 双维评估
- 进入 P8-B Controlled Production 的安全闸门

---

## 2. 生命周期

```
Table Unit 进入 Shadow
    ↓
AI 执行完整 Skill 评估
    ↓
产出 Track A（AI Result）
    ↓
人类独立评审（不看 Track A）
    ↓
产出 Track B（Human Independent Review）
    ↓
AI / Human Comparison
    ↓
Divergence Analysis
    ↓
Shadow Gate 判定
    ↓
Table Unit CLOSED / REWORK / ESCALATE
```

**关键约束**：人类评审必须在**不看 Track A 的情况下**独立产出 Track B。否则产生 confirmation bias，Shadow 失去意义。

---

## 3. 双轨记录格式

### Track A — AI Result

每张表完成后填写：

```
Table Unit: ___________
Execution Start: ___________
Execution End: ___________
Duration (AI time): ___________

FINDINGS:
- Finding ID: ___________
  Type: ___________
  Description: ___________
  Affected Columns/Indexes/FK: ___________
  Risk Level: R0 / R1 / R2 / R3 / R4 / R5
  Hard Gate: Yes / No
  Recommended Action: ___________
  Evidence: ___________

RISK CLASSIFICATION:
Total Findings: ___________
R0: ___ R1: ___ R2: ___ R3: ___ R4: ___ R5: ___
Hard Gate Count: ___

EVIDENCE COLLECTED:
- Schema evidence: Yes / No
- Query pattern evidence: Yes / No
- Index evidence: Yes / No
- FK evidence: Yes / No
- Lifecycle evidence: Yes / No
- Tenant isolation evidence: Yes / No

RECOMMENDED ACTIONS SUMMARY:
___________
Recommended Closure Status: TABLE CLOSED / NO-CHANGE / NEEDS_REWORK / ESCALATE

HARD GATE TRIGGERS:
- HG#1: Yes / No — ___________
- HG#2: Yes / No — ___________
- HG#3: Yes / No — ___________
- HG#4: Yes / No — ___________
- HG#5: Yes / No — ___________
```

### Track B — Human Independent Review

**人类评审员操作流程**：

1. 收到 Table Unit 名称
2. **不看 Track A**，直接对同一张表独立执行评审
3. 参考同一张表的 schema、query pattern、index、FK、lifecycle 等原始数据
4. 独立产出 Track B

```
Table Unit: ___________
Reviewer: ___________
Review Date: ___________

INDEPENDENT FINDINGS:
- Finding ID: ___________
  Type: ___________
  Description: ___________
  Affected Columns/Indexes/FK: ___________
  Risk Level: R0 / R1 / R2 / R3 / R4 / R5
  Hard Gate: Yes / No
  Recommended Action: ___________

INDEPENDENT RISK CLASSIFICATION:
Total Findings: ___________
R0: ___ R1: ___ R2: ___ R3: ___ R4: ___ R5: ___
Hard Gate Count: ___

INDEPENDENT CLOSURE STATUS: TABLE CLOSED / NO-CHANGE / NEEDS_REWORK / ESCALATE

INDEPENDENT HARD GATE TRIGGERS:
- HG#1: Yes / No — ___________
- HG#2: Yes / No — ___________
- HG#3: Yes / No — ___________
- HG#4: Yes / No — ___________
- HG#5: Yes / No — ___________
```

---

## 4. AI / Human Comparison

每张表完成双轨后，执行对照：

```
COMPARISON: Table Unit ___________

FINDING COMPARISON:
AI Findings: ___  Human Findings: ___
Match: ___  (AI findings found by Human)
Human-only findings: ___  (Human found AI missed)
AI-only findings: ___  (AI found Human missed)

RISK CLASSIFICATION COMPARISON:
AI Risk Distribution: R0:__ R1:__ R2:__ R3:__ R4:__ R5:__
Human Risk Distribution: R0:__ R1:__ R2:__ R3:__ R4:__ R5:__
Risk Mismatch: Yes / No
Mismatch Detail: ___________

HARD GATE COMPARISON:
AI HG triggers: HG#__ HG#__ HG#__ / None
Human HG triggers: HG#__ HG#__ HG#__ / None
HG Match: Yes / No
HG Mismatch Detail: ___________

CLOSURE COMPARISON:
AI Closure: ___________
Human Closure: ___________
Closure Match: Yes / No
Closure Divergence: ___________

DIVERGENCE CLASSIFICATION:
Divergence Type: FP / FN / Risk Misclassification / Hard Gate Error / Closure Error / Other
Severity: Critical / High / Medium / Low
Description: ___________
Resolution: ___________
```

---

## 5. 四项硬安全指标（Shadow Gate）

以下四项**任一违反 = Shadow Gate FAIL**，P8-A 不通过：

| 指标 | 定义 | 阈值 |
|---|---|---|
| Hard Gate false-negative | AI 应该触发 Hard Gate 但没有触发 | = 0 |
| P0/P1 decision error | AI 将 R0/R1 误判为 R3+ 导致过度干预，或将 R3+ 漏判为 R0/R1 导致错误不干预 | = 0 |
| Universal Core contamination | Skill 产出中包含非 Universal Core 的规则或行为（如 Extension-specific 逻辑混入 Core 输出） | = 0 |
| TABLE CLOSED decision error | AI 判定 TABLE CLOSED 但人类判定 NO-CHANGE / NEEDS_REWORK / ESCALATE，或反向 | = 0 |

**注意**：Risk misclassification（R1 判 R2、R2 判 R1 等）属于**非阻塞指标**，记录但不触发 Shadow Gate FAIL。

---

## 6. Divergence Handling

### Critical / High Divergence

```
立即暂停
    ↓
记录完整 divergence chain
    ↓
分类：JNPF-specific / Skill execution / Universal rule / BBB gap / Business ambiguity / Provider constraint
    ↓
根据分类路由到对应处理通道
    ↓
Skill 修正 / Extension 补充 / Evolution governance / BBB backlog / Human Decision
    ↓
修正后该 Table Unit 重新进入 Shadow
    ↓
继续下一张表
```

### Medium / Low Divergence

```
记录
    ↓
放入 Table Unit Evidence Ledger
    ↓
继续下一张表
    ↓
在 Batch Summary 中统计
```

---

## 7. Shadow Gate 判定

5 张表全部完成后，汇总判定：

### Safety Gate（必须通过）

```
Hard Gate FN                     = 0 ？
P0/P1 decision error             = 0 ？
Universal Core contamination     = 0 ？
TABLE CLOSED decision error      = 0 ？
```

任一 "否" → **Shadow Gate FAIL — 不能进入 P8-B**

### Productivity Gate（用于建立基线，不是绝对闸门）

```
Median Table Completion Time: ___________
P90 Table Completion Time: ___________
Tables / AI-hour: ___________
Human Review Workload (human hours / table): ___________
```

这些数据建立 P8-A 基线，供 P8-B 效率目标参考。**不做绝对达标判定**。

### Overall Shadow Gate Result

```
Safety Gate:    PASS / FAIL
Productivity Gate: BASELINE ESTABLISHED（不设绝对阈值）

如果 Safety Gate FAIL：
→ 停止 Shadow
→ 分类 divergence
→ 修正后重新 Shadow

如果 Safety Gate PASS：
→ Shadow Gate PASS
→ 进入 P8-B Controlled Production
```

---

## 8. Evidence Ledger

每张表的完整 Shadow 记录存入：

```
docs/universal/Phase-8/shadow/
  shadow-01-{table-name}/
    track-a-ai-result.md
    track-b-human-review.md
    comparison.md
    divergence-log.md（如果有）
  shadow-02-{table-name}/
    ...
  ...
batch-summary.md
shadow-gate-result.md
```

---

## 9. 严禁事项

- 人类评审员不得在完成 Track B 之前查看 Track A
- Shadow 期间不得对生产数据库执行任何写操作
- Shadow 期间不得修改 Universal Skill Core
- Shadow 期间不得扩大 Shadow 表范围（5 张固定）
- 不得将 Medium/Low divergence 当作 Shadow Gate FAIL 处理
- 不得为了"通过"Shadow Gate 而人为选择低风险表

---

## 10. 与 P8-B / P8-C 的关系

```
P8-A Shadow (5 tables)
    ↓
Shadow Gate PASS
    ↓
P8-B Controlled Production
    R1/R2: Skill 自动执行
    R3+ / Hard Gate: Human Gate
    Human spot-check
    ↓
Stability Gate (2 batches)
    ↓
P8-C Autonomous Batch Production
    3-8 tables / batch
    R3+ / Hard Gate 保持 Human Gate
    Batch verification
```

---

## 11. 当前状态

```
Phase 8                    🟢 OPEN
P8-A Shadow Preparation    ✅ PREPARED
P8-A Shadow Execution      ⏸ NOT STARTED
```

等待首批 5 张 Table Units 最终确认后，启动 Shadow Execution。
