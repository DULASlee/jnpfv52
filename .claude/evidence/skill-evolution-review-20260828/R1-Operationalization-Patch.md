# R1 Operationalization Patch — 从"原则"到"可执行协议"

> **版本**：v6.0-R1-Patch **v2** | **日期**：2026-08-28 | **状态**：🟢 **R1 = PASS**（2026-08-28 首席架构师人工验收通过；批准进入 R2 排期，但 R2 实现待 Design Spec 再批）  
> **PASS 限定**：通过的是 R1 Context Model 的设计与操作化契约，非整个 Skill 完成，非 R2 可跳验证实施  
> **R1 冻结边界（PASS 后，约束下游）**：F-R1-① R1 不再回炉优化（除非 R2 实证 R1 缺陷）；F-R1-② R2 验"真实执行"非再做理论验证；F-R1-③ R1=Contract / R2=Consumer，实现困难走 Implementation Gap → 人工裁定，工程师不得自行改 R1  
> **前置**：R1 曾为 PARTIAL（0 PASS / 9 PARTIAL / 1 FAIL）  
> **本 Patch 目标**：不制造"伪精确"，把专家直觉转化为 AI 可执行、可复验、可停止、可升级的协议  
> **禁止（本轮已守）**：不进入 R2 实现 / 不实现 Level 1/2 acquisition / 不修改 Skill / 不修改 JNPF

### v1 → v2 变更记录（2026-08-28 二审）

| # | v2 变更 | 动因 |
|---|---------|------|
| 1 | §4.2/§6.2 契约修正：`ESCALATE` **不是第四种 Decision**，`proposed_decision` 收敛为 GO/STOP/NEED_EVIDENCE 三值；ESCALATE 是 Stop 触发的**动作**，Finding 决策态一律冻结为 NEED EVIDENCE | v1 §5.4 宣称"不引入第四种 Decision"，但 §6.2 枚举含 ESCALATE，自相矛盾；R2 实现者会无所适从 |
| 2 | 新增 **§2.5 Claim 可证伪机械判据**（不可证伪 Claim 五类反例） | 反例审查 Y02 = PARTIAL（"可证伪"依赖 Agent 判断）；架构师 PASS 标准要求全部规则可按明确条件执行，不得拖到 R2 |
| 3 | 新增 **§3.4 STOP-2 穷举记录模板**（强制留痕，逐格填写） | 反例审查 Y03 = PARTIAL（穷举算法无留痕模板，可被"抽样即宣布 Stable"绕过） |
| 4 | §1.5 C09 消耗叙事与 `C09 Post-Patch 重放`对齐：命中 STOP-1，Budget 上限是**后备闸**而非触发器 | v1 §1.5 写成"达到 I=1 上限 → STOP-3 触发"，与重放文档（STOP-1 命中）叙事不一致 |
| 5 | 代码锚点实证：`FileService.DownloadAll` 实际为 **FileService.cs:240-264**（v1/重放文档误写 240-258）；已 Read 真实代码确认无 finally 清理、临时目录交由 `/api/File/Download`（FileService.cs:271）下游消费 | Evidence 可回溯判据要求 file:line 精确；"禁止猜源码"铁律 |

---

## 0. Patch 定位与原则

### 0.1 为什么需要这个 Patch

R1 Validation 暴露的问题不是"缺数字"，是**缺可判定的机制**：
- "成本 > 收益" → 依赖主观评估
- "已获得足够证据" → 依赖专家直觉
- "直接影响判定" → 依赖语义解释

如果强行给这些装一个阈值（Time=30min / Confidence=70%），只是把**主观藏进数字里**——本质不可执行。

### 0.2 Patch 的四条设计原则

1. **可数优先于可测**：Budget 用**能数的**（模块数、层数、工件数、轮次），不用**估不准的**（时间、百分比）
2. **分档优先于统一**：不定义"所有 Finding 通用阈值"，按 Finding 的 risk × technical_nature 分档
3. **判据优先于数字**：什么时候停 = "新增证据是否改变 Decision"，而不是"花了多少时间"
4. **升级优先于硬撑**：Budget 耗尽不是失败，是**Escalation 的触发**

---

## 1. Acquisition Budget Protocol

### 1.1 五个维度

| 维度 | 定义 | 单位 | 计数规则 | 消耗方式 | 判定方式 |
|------|------|------|----------|----------|----------|
| **Scope Budget** | 允许跨越的模块/程序集数 | `modules` | 每进入新 `.csproj` 计 +1 | 从当前 Finding 所在模块算起 | 累计值 vs 分档上限 |
| **Depth Budget** | 允许跨越的调用/继承层数 | `layers` | 每跨越一层调用/继承计 +1 | 从当前类的方法算起 | 累计值 vs 分档上限 |
| **Artifact Budget** | 允许读取的类/接口/配置文件数 | `artifacts` | 首次读取一个新工件计 +1 | 累积 | 累计值 vs 分档上限 |
| **Iteration Budget** | 允许发起的 Context Acquisition 轮次 | `rounds` | 一次 Request→Result 循环 = 1 轮 | 累积 | 累计值 vs 分档上限 |
| **Time Reference** | 参考时间 | `minutes` | 累积 | — | **仅参考，不作 Stop 依据**（避免伪精确） |

### 1.2 分档规则（Risk × Technical Nature）

**Finding 的 Budget 由两个正交轴决定：**

| Risk \ Nature | Local (单类可判) | Regional (跨类边界清晰) | Systemic (跨层/跨模块) |
|---------------|------------------|--------------------------|-------------------------|
| **Critical** | D=1, A=3, I=1 | D=2, A=6, I=2, S=1 | D=3, A=10, I=3, S=2 |
| **High**       | D=1, A=3, I=1 | D=2, A=6, I=2, S=1 | D=3, A=8,  I=2, S=1 |
| **Medium**     | D=1, A=2, I=1 | D=2, A=4,  I=1, S=1 | D=2, A=6,  I=2, S=1 |
| **Low**        | D=1, A=1, I=1 | D=1, A=2,  I=1, S=0 | 不允许 Expansion（Budget = 0） |

（D=Depth, A=Artifact, I=Iteration, S=Scope；未标 S 表示默认 S=0 即不跨模块）

### 1.3 三档 Nature 的判定规则（不靠主观）

- **Local**：Finding 涉及的对象/资源/调用**全部在当前类内部**（可通过本类代码自证）
- **Regional**：Finding 涉及**直接的接口/依赖/继承关系**，跨界 1 层即可（可通过接口签名/构造函数/继承链定位）
- **Systemic**：Finding 涉及**跨层边界**（Service↔Repository↔Provider）或**跨模块依赖**（不同 .csproj）

判定顺序：先看 Local → 不满足再看 Regional → 不满足才定 Systemic。不允许跳过判定直接归到 Systemic（防"什么都归到 Systemic 从而无限扩张"）。

### 1.4 为什么这是可执行的

- Scope = **能数**：`.csproj` 边界是清晰的
- Depth = **能数**：调用层从代码可数
- Artifact = **能数**：读过的类/文件列表可枚举
- Iteration = **能数**：Request→Result 是离散事件
- **不需要**"30 分钟够不够"这种判断

### 1.5 Budget 消耗示例（v2 重写，与 C09 Post-Patch 重放叙事统一）

**C09 重放**：FileService.DownloadAll 临时目录未清理

- Risk = Medium
- Nature 判定：涉及 FileService（JNPF.Systems）→ 临时目录消费者在本类外（`/api/File/Download` 下游，FileService.cs:271）→ 判定为 **Regional**（跨类边界清晰）
- 分档：**D=2, A=4, I=1, S=1**
- 执行序列（每一步 Expansion 结束后按 §3.1 优先级顺序检查 STOP）：
  - Iteration 1：请求 Ownership Context（Level 0 人工描述 + 本类代码复核 FileService.cs:240-264 确认无 finally 清理）
  - Iteration 1 结束 → 按 §3.1 检查：STOP-4? 否 → STOP-5? 否 → **STOP-1 Evidence Sufficient? 是（五元组全过）→ STOP**
  - 预算消耗：A=1/4, D=1/2, I=1/1, S=0/1 —— 全部未超上限
- **正确理解**：Budget 上限是**后备闸（backstop）**，不是触发器。STOP-3 只会在"证据始终不 Sufficient、又无边界可撞"时到达。v1 把本例叙述为"达到 I=1 上限 → STOP-3 触发"是错的——那样等于告诉 Agent"用完预算才算停"，恰好制造本 Patch 要防止的「能数就多数」。

---

## 2. Evidence Sufficiency Protocol

### 2.1 五元组最低门槛

**Evidence 认定 Sufficient 当且仅当以下五项全部满足：**

| 元素 | 定义 | 最低要求 | 判定方式 |
|------|------|----------|----------|
| **Claim** | 当前 Finding 的具体主张 | 必须可证伪（不是"可能有问题"） | 反例检查：能否构造"Claim 不成立"的场景 |
| **Evidence** | 支持/反驳 Claim 的具体事实 | 至少 1 条来自可指认来源（file:line / 工具输出 / 人工描述） | 来源可回溯 |
| **Impact** | Claim 对 GO/STOP/NEED 的作用 | 明确说明"若 Claim 成立则 X，不成立则 Y" | 影响链可写出 |
| **Confidence** | 证据强度 | ≥ Medium（High=可复现来源；Medium=人工描述/单点推断；Low=不完整/多处推断） | 按证据来源机械归类 |
| **Decision** | GO / STOP / NEED EVIDENCE / ESCALATE | 能从 Claim+Evidence+Impact+Confidence **唯一**推出 | 用 Decision 表比对 |

### 2.2 Confidence 判定规则（不靠"我觉得")

| Confidence | 判定条件 | 举例 |
|------------|----------|------|
| **High** | 证据来自 Level 1 静态代码 / Level 2 工具，且证据与 Claim 直接对应（无需推断） | 从 Startup.cs 读到 `AddScoped<OrderService>()` → OrderService 是 Scoped |
| **Medium** | 证据来自 Level 0 人工描述，**或** 通过 1 步静态推断得到 | 人工说"前端下载后清理" → 临时目录由外部消费 |
| **Low** | 需要 2 步以上推断，或证据之间存在冲突未解 | 从命名习惯猜测某字段用途 |

### 2.3 Evidence Sufficient 的可执行判据

**判据（全部满足才算 Sufficient）：**

1. **Claim 可证伪**：`falsifiable(Claim) = true`
2. **证据可回溯**：每条 Evidence 都有 `source: file:line | tool-output | human-statement`
3. **影响链完整**：Impact 描述了 Claim 成立/不成立分别导致什么 Decision
4. **Decision 唯一**：给定当前证据，`decide(Claim, Evidence, Impact) → Decision` 是**单值**，不是集合
5. **Confidence ≥ Medium**

**任一不满足 → NOT Sufficient → 继续 Expansion（若 Budget 允许）或 Escalation（若 Budget 耗尽）**

### 2.4 为什么不用"百分比"

"Confidence ≥ 70%" 看似精确，实则**没有校准来源**——没人能可靠报告"我对这个证据的信心是 70%"。所以本 Patch 用**离散的三档 Confidence**（High/Medium/Low）+ 明确定义，让判定可复现。

### 2.5 Claim 可证伪的机械判据（v2 新增，关闭 Y02）

判据 1（"Claim 可证伪"）在 v1 只有一句话，Y02 反例证明它可被"把 Claim 写宽"绕过。v2 将其升级为**三问 + 五类反例**的机械检查：

#### 2.5.1 三问检查（任一 NO → Claim 不可证伪 → 五元组直接不成立）

| # | 问题 | 通过标准 |
|---|------|----------|
| FQ-1 | **存在性**：能否写出一段代码/一条运行时观测，使该 Claim 为假？ | 能具体说出"在 file:line 看到 X 则 Claim 假" |
| FQ-2 | **判定对象**：Claim 是否绑定到明确的对象 + 状态？（哪个类/方法/资源，处于什么状态） | 对象与状态均可指认，不是"这个系统/这类问题" |
| FQ-3 | **判定人一致性**：两个互不沟通的 Agent 拿同一份代码，能否对"Claim 是否成立"给出相同答案？ | 语义无自由发挥空间（不含"合理/适当/尽量/更好"类修饰词） |

#### 2.5.2 不可证伪 Claim 五类反例（命中任一 → 拒绝该 Claim，要求重写）

| 类型 | 反例（应被拒绝） | 为什么不可证伪 | 合格改写示例 |
|------|------------------|----------------|--------------|
| N1 模糊优化型 | "这个方法可能可以优化" | 任何代码都存在可优化性，永真即不可证伪 | "本方法内 `new Random()` 每次调用重建，导致种子碰撞（可检查：调用点无静态实例）" |
| N2 无对象型 | "存在资源泄漏风险" | 未绑定具体资源与路径 | "`FileDown` 返回的 FileStream 在异常路径上无 Dispose（可检查：try 块无 using/finally）" |
| N3 无判据型 | "如果数据量大会出问题" | "大"无界限，真假无法判定 | "当 `input.Count > 1` 时 foreach 内每次执行一次独立查询（N+1 形态，可检查：循环体含 ToListAsync）" |
| N4 不可观测型 | "高并发下可能死锁" | 无现有观测手段能证伪（且 R1 阶段禁 Level 2） | 降级为 NEED EVIDENCE 冻结，不得作为 Claim |
| N5 泛化断言型 | "全仓所有 Service 都缺事务" | 全称量词超出 Finding 责任边界（违反 STOP-4） | 绑定单类："OrderService.Save 的两次 Insertable 无同一事务包裹" |

#### 2.5.3 与判据 4（Decision 唯一）的关系

N1 类 Claim 之所以危险，是因为它宽到 GO/STOP 都能自圆其说 → 判据 4 也会失败。FQ-3 与判据 4 是**双重闸**：判据 1 挡"内容上不可证伪"，判据 4 挡"即便可证伪但推不出唯一决策"。两关都过才算数。

---

## 3. Stop Condition Protocol

### 3.1 五种 STOP 及优先级

按**判定顺序**排列（先命中先 Stop）：

| 顺序 | STOP | 触发条件（可执行） | 动作 |
|------|------|--------------------|------|
| 1 | **STOP-4: Boundary Reached** | 剩余可获取 Context 需要跨越 Scope Budget 允许模块；或触发 v4 "跨模块 STOP" 红线 | 保留当前证据 → 直接进入 v4 Decision 门 |
| 2 | **STOP-5: Escalation Required** | 见 §4 Escalation 触发条件 | Finding 决策态冻结为 **NEED EVIDENCE** + 提交 Escalation Pack（ESCALATE 是动作，不是第四种 Decision，见 §4.0） |
| 3 | **STOP-1: Evidence Sufficient** | §2.3 五判据全部满足 | 进入 Decision Re-entry |
| 4 | **STOP-2: Decision Stable** | 见 §3.2 | 进入 Decision Re-entry（不再 Expansion） |
| 5 | **STOP-3: Budget Exhausted** | 任一 Budget 维度达到 §1.2 分档上限 | 若 Confidence ≥ Medium → 进入 Decision；若 Confidence < Medium → 转 STOP-5 |

**关键**：STOP-4 / STOP-5 **优先级最高**，即使证据看似足够也不允许继续（防止"因为能查就查下去"）。

### 3.2 STOP-2 Decision Stable 的可执行判定

**Decision Stable 定义**：在**剩余 Budget 允许获取的所有 Context** 中，**没有任何一种**能改变当前 Decision。

**判定算法（AI 可执行）**：

```
1. 枚举剩余 Budget 允许获取的 Context Type 集合 CT = {Call, DI, Ownership, DataFlow, CrossLayer} 中尚未获取的
2. 对每个 ct ∈ CT：
   a. 假设获得该 ct 的最坏情况证据（即最不利于当前 Decision 的证据）
   b. 用 §2 五元组重跑 Decision
   c. 若 Decision 不变，则 ct 不影响稳定性
3. 若所有 ct 都不影响 → Decision Stable = true → STOP-2
4. 若存在 ct 会影响 → Decision Stable = false → 继续 Expansion 该 ct（若 Budget 允许）
```

**为什么这是可执行的**：不再依赖"我觉得够了"，而是**穷举剩余可获取 Context 是否可能翻转决策**。

### 3.3 五种 STOP 的关系图

```
Expansion 每一步
  ↓
[STOP-4?] → YES → Boundary STOP（保留证据进 Decision）
  ↓ NO
[STOP-5?] → YES → Escalate（决策态冻结 NEED EVIDENCE + 交人）
  ↓ NO
[STOP-1?] → YES → Sufficient（进 Decision）
  ↓ NO
[STOP-2?] → YES → Stable（进 Decision）
  ↓ NO
[STOP-3?] → YES → Budget 耗尽 → Confidence 检查 → 进 Decision 或转 STOP-5
  ↓ NO
继续 Expansion
```

**同时命中的记录规则（v2）**：多个 STOP 条件同时满足时，按本序列记录**最先命中者**为 `stop_triggered`。特别地，STOP-3 与 STOP-4 同时满足时记 STOP-4——归因不同：STOP-4 是责任边界（外部约束），STOP-3 是预算消耗（内部配额），二者的 Escalation 建议（E4 vs E1）不同。

### 3.4 STOP-2 穷举记录模板（v2 新增，关闭 Y03）

§3.2 算法可被"只模拟一两种就宣布 Stable"绕过（Y03）。v2 规定：**宣布 Decision Stable 必须提交下表，一格不缺；有空格 = 未穷举 = 不得触发 STOP-2。**

| CT（未获取类型） | 剩余 Budget 内可获取？ | 最不利于当前 Decision 的假设证据（具体写） | 重跑 §2 五元组后 Decision | 翻转？ |
|------------------|------------------------|---------------------------------------------|---------------------------|--------|
| Call | 是/否（若否，注明哪个维度封顶） | …… | …… | YES/NO |
| DI | 是/否 | …… | …… | YES/NO |
| Ownership | 是/否 | …… | …… | YES/NO |
| DataFlow | 是/否 | …… | …… | YES/NO |
| CrossLayer | 是/否 | …… | …… | YES/NO |

**机械校验规则（可被下一轮 Review 复验）**：

1. 五行**必须全部存在**，不允许删行——"剩余 Budget 不可获取"也是一行合法答案（含封顶维度名）。
2. "最不利假设证据"列**不允许为空或写'无'**，除非该行"可获取？=否"。
3. 出现任一 `翻转？= YES` 且对应 CT 可获取 → Decision Stable = false → 必须继续 Expansion 该 CT（回到 §1 Budget 检查）。
4. 全部行 `翻转？= NO` 或不可获取 → STOP-2 成立。
5. 本表随 Finding 记录归档，供人工抽检"穷举是否彻底"——留痕把 Y03 从「依赖自觉」变为「依赖可检查的产物」。

---

## 4. Escalation Protocol

### 4.0 ESCALATE 的定位（v2 新增，修复契约自相矛盾）

**ESCALATE 不是第四种 Decision。** v4 三门（GO / STOP / NEED EVIDENCE）保持封闭，v6 不引入第四门：

- **Decision（结论态）**：Finding 最终记录的值 ∈ {GO, STOP, NEED EVIDENCE}。
- **ESCALATE（动作态）**：STOP-5 命中后 AI 执行的**交人动作**，其输出物是 Escalation Pack。
- **映射规则**：任何触发 Escalation 的 Finding，其 Decision 一律冻结为 **NEED EVIDENCE / BLOCKED-BY-HUMAN**，直至人工回复。人工回复后按 v4 三门重新进入决策（Escalation Pack 中的 `human_decision_required` 即人工可做的动作集合）。
- **与 v4 NEED EVIDENCE 语义的衔接**：v4 中 NEED EVIDENCE = "证据不足，冻结待补充"；v6 Escalation 是它的一个特化——"缺口已明确打包，等人补"。C07（Level 2 不可用 → NEED EVIDENCE）在 v6 协议下同时是 E3 Escalation 的实例：**Decision 仍是 NEED EVIDENCE，额外多了交人动作**。二者不再互斥（v1 中 C07 与 E3 的关系未定义，R2 实现者会在"记 NEED 还是记 ESCALATE"上二义）。

### 4.1 五种 Escalation 触发条件

| 类型 | 触发条件（可执行） | 输出内容 |
|------|--------------------|---------|
| **E1: BudgetExhausted** | 任一 Budget 达上限 且 Confidence < Medium | 缺什么证据 + 建议扩哪档 Budget |
| **E2: EvidenceConflict** | 两条及以上 **High** 置信度证据指向不同 Decision | 冲突对立方 + 各自证据 |
| **E3: RuntimeDependent** | 剩余可获取 Context 全部属于 Level 2（工具/运行时） | 需要什么运行时数据 + 建议采集方式 |
| **E4: BoundaryCross** | 判定为 Regional 但实际追踪跨越 S 上限 | 需跨越的模块列表 + 建议是否升级 Nature 档 |
| **E5: DecisionUnstable** | 同一 Finding 用 §3.2 稳定判定算法跑出 ≥2 种 Decision | 各种可能的 Decision + 依赖的关键假设 |

### 4.2 Escalation 输出契约

```json
{
  "escalation_type": "E1|E2|E3|E4|E5",
  "finding_identity": "string",
  "finding_decision_record": "NEED_EVIDENCE",
  "current_claim": "string",
  "current_evidence": [{"source": "...", "content": "...", "confidence": "H|M|L"}],
  "current_confidence": "Low|Medium",
  "budget_consumed": {"scope": "x/y", "depth": "x/y", "artifact": "x/y", "iteration": "x/y"},
  "missing_information": "string",
  "candidate_decisions": ["GO", "STOP", "NEED_EVIDENCE"],
  "human_decision_required": "GO|STOP|NEED_EVIDENCE|APPROVE_MORE_CONTEXT|REDEFINE_NATURE|REDEFINE_SCOPE",
  "recommended_action": "string"
}
```

**`finding_decision_record` 恒为 NEED_EVIDENCE**（§4.0 映射规则）；`candidate_decisions` 是给人看的备选，不是 AI 已做的选择。

### 4.3 核心纪律：Escalation 不是失败

**在 R1 中确立：**

> 当 AI 在给定 Budget 内无法形成 Confidence ≥ Medium 的稳定 Decision 时，**Escalation 是唯一正确动作**，硬撑 GO 或硬撑 STOP 都是纪律违反。

**这与 v4 三安全阀一致**：
- "没有证据就做高级优化" → 违反
- "没有 benchmark 就宣称性能提升" → 违反
- "Finding 自动改代码" → 违反

v6 追加：
- **"没有稳定 Decision 就强行选一个"** → 违反

### 4.4 Escalation 与 STOP-5 的关系

- STOP-5 是**时机**：什么时候停止 Expansion
- Escalation 是**动作**：停止后干什么

所有 E1-E5 都会触发 STOP-5；但 STOP-5 不一定意味着 Finding 未解决，也可能只是需要人类介入下一步。

---

## 5. v4 Compatibility Contract

### 5.1 四概念职责物理隔离

| 概念 | 版本 | 作用阶段 | 决定的问题 | 生效时机 | 与其他的边界 |
|------|------|----------|-----------|----------|-------------|
| **Semantic Budget** | v4 | Fix 阶段 | "改代码的语义范围" | Decision=GO 之后 | 不管"看了多少代码" |
| **Context Budget** | v6 | Expansion 阶段 | "看代码的最大范围" | Decision 之前 | 不管"改多少代码" |
| **Evidence Threshold** | v6 | Decision 阶段 | "证据是否够下判断" | 每次 STOP/Decision 判定时 | 不管"范围多大" |
| **Stop Condition** | v6 | Expansion 阶段 | "什么时候必须停止调查" | Expansion 每一步 | 不管"证据本身对不对" |

### 5.2 职责边界证明（不重叠）

**问题 A：Semantic Budget 和 Context Budget 会不会同时管范围？**

- **不会**。Semantic Budget 只在 Decision=GO 之后触发，管"这次 Fix 允许改哪 3 行"
- Context Budget 只在 Decision 之前触发，管"为了形成 Decision 允许读多少代码"
- 两阶段互斥，不会重叠

**问题 B：Evidence Threshold 和 Stop Condition 会不会互相覆盖？**

- **不会**。Evidence Threshold 判"够不够"（判据）
- Stop Condition 判"停不停"（时机）
- 一个 Finding 可能证据够（Threshold 通过），但因为 STOP-4 边界已越，仍停止
- 也可能证据不够（Threshold 未通过），但因为 STOP-3 Budget 耗尽，必须停止并 Escalation

### 5.3 v4 五大核心纪律的继承证明

| v4 纪律 | v6 如何增强（不弱化） |
|---------|---------------------|
| **Evidence-driven** | Evidence Sufficiency 五元组强制 Claim→Evidence→Impact→Confidence→Decision |
| **Bounded scope** | Context Budget + Stop Condition + Scope Boundary 三重限制（v4 只有单类自然边界） |
| **Risk classification** | Budget 按 risk × nature 分档（v4 Risk 只影响优先级，不影响 Expansion） |
| **Quantitative verification** | Budget 全部维度可数（模块/层/工件/轮次） |
| **Human control** | Escalation Protocol 明确 5 种 AI 必须交人的时机 |

### 5.4 v6 与 v4 的关系一句话

> **v6 = v4 的证据驱动决策系统 + 一个受纪律约束的"看代码"预算机制。**
>
> v6 不引入第四种 Decision（GO/STOP/NEED EVIDENCE 仍是三种）；v6 只是在**证据不足时**给了 AI 一个"合法去查"的通道，并给这个通道上了预算、判据、停止、升级四道闸。

---

## 6. R2 接口契约更新

### 6.1 Context Expansion Request

```json
{
  "finding_identity": "string",
  "claim": "string（可证伪）",
  "risk_level": "Critical|High|Medium|Low",
  "technical_nature": "Local|Regional|Systemic",
  "missing_evidence": "string",
  "required_context_type": "Call|DI|Ownership|DataFlow|CrossLayer",
  "required_level": "Level0|Level1|Level2",
  "budget_allocation": {
    "scope": "int", "depth": "int", "artifact": "int", "iteration": "int"
  },
  "stop_conditions_active": ["STOP-1","STOP-2","STOP-3","STOP-4","STOP-5"]
}
```

### 6.2 Context Expansion Result（v2 修正：三门封闭）

```json
{
  "requested_context": "object（同上）",
  "acquired_evidence": [{"source": "...", "content": "...", "confidence": "H|M|L"}],
  "evidence_source_level": "Level0|Level1|Level2",
  "budget_consumed": {"scope": "x/y", "depth": "x/y", "artifact": "x/y", "iteration": "x/y"},
  "stop_triggered": "STOP-1|STOP-2|STOP-3|STOP-4|STOP-5|NOT_STOPPED",
  "evidence_sufficient": "bool（按 §2.3 判据）",
  "decision_stable": "bool（按 §3.2 算法 + §3.4 模板留痕）",
  "proposed_decision": "GO|STOP|NEED_EVIDENCE",
  "escalation": "object 或 null（按 §4.2 契约；非 null 时 stop_triggered 必为 STOP-5 且 proposed_decision 必为 NEED_EVIDENCE）"
}
```

### 6.3 契约的关键性质（v2 更新）

- **Request 输入完整** → Result 输出**唯一**（可复现）
- **Result 不包含主观评价**（如"我觉得这样改更好"），只有依据 §2/§3 判据得到的 Decision
- **Decision 恒为三值**（GO/STOP/NEED EVIDENCE，v4 三门封闭）；"交人"通过 `escalation` 字段表达，**不占用 Decision 枚举位**
- **不变式**：`escalation != null ⇔ stop_triggered == STOP-5 ⇒ proposed_decision == NEED_EVIDENCE`。R2 实现可用此式做契约测试
- **ESCALATE 是合法输出动作，不是异常，也不是决策**（§4.0）

---

## 7. 总结

### 7.1 Patch 解决了什么

| R1 原问题 | Patch 解决方案 |
|-----------|---------------|
| "成本 > 收益"依赖主观 | §3.2 Decision Stable 判定算法（可穷举）+ §3.4 留痕模板（v2） |
| "已获得足够证据"无定义 | §2.3 Evidence Sufficient 五判据（可回溯） |
| "Claim 可证伪"依赖直觉（Y02） | §2.5 三问 + 五类反例（v2，机械检查） |
| STOP-2 穷举可被抽样绕过（Y03） | §3.4 强制逐格记录模板（v2） |
| Context Budget 无单位 | §1.1-1.2 五维 Budget + 分档规则（可数） |
| 何时必须停止 Expansion 模糊 | §3.1 五种 STOP 优先级序列（可执行）+ 同时命中记录规则（v2） |
| AI 何时必须承认不知道没定义 | §4.1 E1-E5 Escalation 触发（可判定） |
| ESCALATE 与 v4 三门冲突（v1 契约矛盾） | §4.0 映射规则 + §6.2 不变式（v2） |
| v4 与 v6 关系不清 | §5.1-5.4 四概念职责物理隔离 + 五纪律继承证明 |

### 7.2 Patch 没做什么（保持纪律）

- ❌ 没有引入 Level 1/2 acquisition 实现（属于 R2）
- ❌ 没有开发任何工具（属于 R3）
- ❌ 没有修改 SKILL.md / references（属于 Skill 修改，不在本轮范围）
- ❌ 没有修改 JNPF 生产代码
- ❌ 没有为了 PASS 而迎合数字（Budget 用可数单位，不是伪精确阈值）
- ❌ 没有宣布 R1 = PASS——升级 PASS 是人工验收的专属动作（human control）

### 7.3 Patch 交付的下游影响

- **R1 十项验收预期**：R1-07 从 FAIL → 具备 PASS 资格（Budget 有明确单位+分档）；其他 9 项 PARTIAL → 具备 PASS 资格（每项都有可执行判据）。**最终判定留给人工验收。**
- **C09 预期**：PARTIAL → 具备 PASS 资格（用 STOP-1 五元组替代"成本 > 收益"；代码锚点 FileService.cs:240-264 已实证复核）
- **X01-X08 预期**：全部保持 PASS（Patch 只加强，不削弱）；新增 Y 系列中 Y02/Y03 已随 §2.5/§3.4 关闭
- **新增**：Positive Cases (PC) + Negative Cases (NC) + Escalation Cases (EC) 覆盖
- **下游文件一致性**：5 份基础 R1 规格（Context-Budget / Context-Expansion-Rules / V6-Context-Model / Context-Level-Model / V6-R1-Design-and-Verification）已加**废止横幅 + 映射表**，操作性判据以本 Patch（v2）为唯一源，消除双源冲突

具体重放结果见 `R1-Validation-Matrix.md` / `C01-C10-Decision-Replay.md` / `R1-Counterexample-Review.md` 更新版。

---

**本 Patch 待与更新后的 4 份验证文件一并提交人工验收。**
