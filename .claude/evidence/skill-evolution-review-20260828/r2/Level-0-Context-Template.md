# Level-0-Context-Template — 人工上下文请求卡（R2 机制包 · 交付物 1）

> **版本**：v6.0-R2-M1 | **日期**：2026-08-28 | **地位**：R2 Consumer 实现细则（非 R1 条款，不改 R1）  
> **上游契约（冻结）**：`../R1-Operationalization-Patch.md` v2 §2.2（Level 0 ⇒ Confidence ≤ Medium）、§4（Escalation Pack）、§6.1（Request 结构）  
> **范式来源**：本项目 ADR-005 交互式澄清问答（结构化选择题、每轮 3-5 题、末项恒"其他+文本"、关键题必答）——复用范式，不改其实现

---

## 1. 何时发 Level-0 卡

前置全部成立才允许发（否则违反 R1 §3 触发/优先级纪律）：

1. P0 单类取证已完成（v4 硬门 1），且
2. 五元组判据 4（Decision 唯一）不满足——缺失的证据**决定** GO/STOP 分叉，且
3. 该缺失 Context **无法**由 Level 1 静态源取得（对照 `Level-1-Context-Acquisition.md` §2 证据源清单逐项排除），且
4. 剩余 Iteration Budget > 0（Budget 已尽则直接走 §3.1 STOP-3 分支，发卡的轮次消耗按 §3 Pending 规则处理）。

**禁止**：为"更完整"发卡（违反 R1 §2 判据精神 = 能查就查）。

## 2. Context Request Card（JSON 契约）

```json
{
  "card_id": "{case-id}-L0-{n}",
  "finding_identity": "类名.方法名 + Finding 一句话",
  "claim": "已过 R1 §2.5 FQ-1/2/3 的可证伪主张",
  "claim_fq_gate": {"fq1":"pass","fq2":"pass","fq3":"pass"},
  "missing_evidence": "要人回答的具体问题（绑定 Context Type）",
  "budget_consumed_snapshot": {"scope":"x/y","depth":"x/y","artifact":"x/y","iteration":"x/y"},
  "why_level1_insufficient": "逐项说明哪些静态源为什么回答不了（引用 L1 清单行）",
  "questions": [
    {"id":"q1","type":"single|multi|text","prompt":"…","options":["…","其他+文本"],"required":true}
  ],
  "consequence_map": "q1 各答案分别把 Decision 推向什么（让回答人知道自己在裁什么）"
}
```

规则：
- 3–5 题；每题末项恒为"其他 + 文本"；关键题 `required=true`——必答，否则不得进入重决策（ADR-005 硬门语义）。
- `consequence_map` 必填：AI 必须先自证"答案如何改变决策"，防止把设计责任外包给人。
- 卡与回复**双份存档**为 evidence（R1 §2.3 判据 2 可回溯）：请求卡 + 原始回复，不得只存 AI 转述。

## 3. 回答接收契约

- 人回答登记为 `{source:"human-statement", card_id, snippet:"原文引用(非转述)", confidence:"M"}`。
- **原文引用义务**：AI 只能引用回复原文，禁止"理解后复述"升格语义（复述 = 制造第二条证据源，违反唯一源）。
- Confidence 封顶 **Medium**（R1 §2.2）。单条 Level-0 证据不得独立支撑 GO——判据 4 要求"唯一推出"，Medium 单源对 GO（要改代码）几乎必然不唯一；对 STOP 可成立（"外部有消费者，我不能局部清"）。
- 与代码证据冲突时：按 §2.2 定级处理，High(代码) 直接胜出；仅当**两条都是 High** 才构成 E2（防止把 E2 当挡箭牌推卸裁决——R1 冻结，不改）。

## 4. Pending 与超时细则（R2 补充，不回写 R1）

R1 未定义"卡已发、人未回"的状态。本模板规定：

| 状态 | 记账 | 禁令 |
|------|------|------|
| **Pending**（卡已发未回） | 不占 Iteration、不算 Evidence | 不得以"已在等人"为由宣布任何 STOP 以外的终态；不得把 Pending 当 Sufficient |
| **Replied** | 闭环该轮，Iteration +1 | 转述升格（违反 §3 原文引用） |
| **Timeout**（验证场景=人不可得） | 等同该 CT 不可获取：走 §3.1 STOP-3 → Confidence 检查 → E1/交人分支 | 伪造 human-statement evidence（Validator INV-5 直接 FAIL：manifest.simulated_human=false 时含 human-statement 即造假） |

## 5. R2 验证场景内的"模拟人"规则（F-E 声明）

盲重放没有真人。验证场景采用**预置回复文件**：

- 场景目录 `human-response.md`，含模拟声明抬头（`> R2 验证用模拟人回复，非生产 Level 0`）+ 问答原文。
- trace 中该证据标 `human-statement` + `simulated: true`，Validator 按 manifest.simulated_human 校验存在性。
- 生产使用时 Level 0 = 真人（Studio 走 ADR-005 桥接，Studio 外用 markdown 卡），模拟人机制**仅限验证场景**。
- RB-B2 提供**故意矛盾**的模拟回复，用于验证"AI 不滥用 E2 推卸裁决"。

## 6. Studio 桥接（兼容性声明，不实施）

JNPF Studio 环境内，本卡映射到既有澄清问答链（`RequirementGateService`/`BudgetGuard` 范式）：question 结构同构（single/multi/text + 必答），consequence_map 对应暂停/恢复语义。桥接实现在 R4 决策整合时经人工批准后进行；本轮 0 触碰产品代码。
