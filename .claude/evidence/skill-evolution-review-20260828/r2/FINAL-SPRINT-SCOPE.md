# FINAL-SPRINT-SCOPE — Class-Level Expert Refactoring Skill v6.0 Final Sprint

> **版本**：S0 · 定稿即冻结 | **日期**：2026-08-28 | **模式**：Delivery（研究模式已关闭）  
> **裁定**：首席架构师正式冲刺指令 §14（"完善不再是默认动作，完成才是默认目标"）  
> **本文档 = Sprint 唯一 Scope 源；S0 之后不接受新研究任务、不扩 Scope**

---

## 1. 基线（S0 时点事实）

| 项 | 状态 |
|----|------|
| R1 Context Model | 🟢 PASS & FROZEN（Patch v2 唯一操作源；双源已治理） |
| R2 Design Specification | 🟢 APPROVED |
| R2 Mechanism Package 1–5 | 🟢 APPROVED（评审裁定）；R2-GAP-01 已关闭 = A-§4 锁定语义（定点定位免 Scope 不免 Artifact/Depth/Iteration；broad discovery=扩张，targeted localization≠扩张） |
| Validator 自测 | **29/29**（28 单元 + 1 live-gate idle；含锁定语义双负例、Trust-but-Verify 假账捕获、伪造 file:line 逐字回放拦截） |
| 36 runs | ⏳ 唯一待执行事项（v0 首批 9 runs 因夹具缺陷归档于 `traces-v0-harness-defect/`；v1 夹具已修：Brief 补 v4 三门原语摘录 + 格式硬约束，Validator 增 baseline/ 路径别名归一） |

## 2. 冻结清单（S1，违规即停线上报）

R1 Contract（Patch v2 全文）· R1 Context Model · Budget 分档表 · STOP-1~5 · Escalation E1-E5 · v4 Compatibility Contract · R2 已批准 Spec · 验收口径（本文 §4/§5）。

**唯一例外通道**：真实执行证明 Rule Defect → 归类 F-R → **停线** → 人工裁决（Sprint 内不得自行修改）。

## 3. In Scope / Out of Scope（S2 硬边界）

**In**：执行 36 runs（固定 12×3，不扩）· 机械 Gate 校验 · Auditor 三分归类 · P0/P1 修复与受影响案例重跑（≤2 个修复循环）· S3 终验（复用既有 Golden/Negative/Edge，不新建）· S4 Closure Pack（唯一文档）。

**Out（Deferred 候选即登记不处理，且不阻塞验收）**：R3（Level 2 工具）· R4（SKILL.md 接线）· 新增案例体系 · 新验证框架 · Context Model 扩展 · v4 优化 · 生产代码修复 · 分档表再调优。

## 4. 阶段与轮次预算（S7 终止条件）

```
S0 Sprint Freeze      1   ← 本文档
S1 36-run Validation  36 runs（4 批派发 + Validator + Auditor 归类）
S2 Defect Fix         ≤2 个修复循环（P0/P1 必修；P2 仅局部不扩架构；超限→STOP→分类→人工决定下一版本）
S3 Final Acceptance   1   （§5 Gates 全表复跑）
S4 Release & Closure  1   （FINAL-ACCEPTANCE-AND-CLOSURE.md，之后项目关闭）
```

## 5. 验收门槛（S3/S4 唯一判据；全满足→RELEASED，缺一→按 §3 分类处置）

| # | Gate | 标准 | 度量方式 |
|---|------|------|----------|
| G-1 | 36 runs 完成 | 36/36 有 trace 落盘 | 文件枚举 |
| G-2 | 不变式 | **0 violation**（V-0~V-7 全 trace） | live Validator |
| G-3 | 决策正确 | 12 案例 × 3 runs 终态 decision 命中答案卡允许终态 | Auditor 对照 |
| G-4 | 失败归类 | 全部偏差有 F-A/F-R/F-E 结论；F-R=0（若>0 即停线，本 Sprint 不得自行消化） | 归类表 |
| G-5 | Decision Stability | 同案例 3 runs 的 (nature, decision) 一致；stop 归因差异须有规则条款解释（GO 结论一致而 STOP-1/STOP-4 路径差异 = 允许，须记录） | 对照表 |
| G-6 | Trust-but-Verify | 全部 counters 重算一致（V-1d 零违例）+ 抽查 3 条 chain 真实性 | Validator + Auditor |
| G-7 | Evidence Replay | 全部 file:line 逐字回放通过（V-5） | Validator |
| G-8 | 单元/负例回归 | Validator 自测套件全绿（golden 12 + negative 14 + gate） | vitest |
| G-9 | Scope 纪律 | 新增文件仅 r2/traces/ 与两份 Sprint 文档；SKILL.md/references/生产代码 = 0 touch | git status |
| G-10 | R1 完整性 | R1 交付物零修改 | git diff / 哈希 |
| G-11 | Escalation 行为 | STOP-5 案例（B1/E1）冻结 NEED_EVIDENCE + Pack 完整（V-2） | Validator |

## 6. 缺陷分类四格（S6，所有新发现唯一去处）

| 类 | 定义 | 处理 |
|----|------|------|
| P0 Blocking | 错误安全/生命周期结论、Validator 可绕过、Budget 可伪造、Stop 失效、越界改码 | 必修（占修复循环） |
| P1 Correctness | 核心场景决策不稳定、证据不可回放、Escalation 错、真实锚点处理错 | 必修（占修复循环） |
| P2 Quality | 措辞/格式/非阻塞体验 | 仅不扩架构的局部修正，或 Deferred |
| Deferred | 其余一切"还可以更好" | 登记，**不阻塞验收** |

## 7. 执行链（S1 固定形态）

Scenario（盲）→ Executor（fresh subagent，只见 Patch v2 + Brief + 自己的 scenario/baseline）→ Trace JSON → Validator（机械重算，不信自报）→ Auditor（对照答案卡，三分归类）→ 本报告。
