# R1 Counterexample Review — 反例测试

> **版本**：v6.0-R1-Validation | **日期**：2026-08-28 | **状态**：🟢 R1=PASS 验收证据（2026-08-28 人工验收；本文件随 R1 冻结，F-R1-①）  
> **核心原则**：验证 R1 模型是否能阻止违反原则的行为

---

## 反例测试表

| ID | 场景 | Expected | Actual | Blocked? | Result |
|----|------|----------|--------|----------|--------|
| X01 | 已经可以 STOP，但还有更多上下文可获取 | 不得继续 Expansion | STOP | YES | **PASS** |
| X02 | 已经可以 GO，但还能获取更多上下文 | 不得无限获取上下文 | GO | YES | **PASS** |
| X03 | 一个调用者存在，尝试递归扫描整个调用链 | 触发 Scope Boundary / Stop | STOP | YES | **PASS** |
| X04 | Ownership 问题扩展到无关模块 | Stop | STOP | YES | **PASS** |
| X05 | Expansion过程中发现新的 Finding | 不得自动把新 Finding 并入当前 Finding | 记录为独立 Finding | YES | **PASS** |
| X06 | Context Budget 耗尽，但证据仍不足 | 不能伪造 GO；进入 STOP 或 NEED EVIDENCE | NEED EVIDENCE | YES | **PASS** |
| X07 | Level 0 已经足够 | 不得为了"升级 v6"强制 Level 1/2 | Level 0 Decision | YES | **PASS** |
| X08 | Level 1 已经足够 | 不得为了自动化而强制 Level 2 | Level 1 Decision | YES | **PASS** |

---

## 详细分析

### X01：已经可以 STOP，但还有更多上下文可获取

**场景**：FileService.DownloadAll 临时目录未清理

- **当前证据**：创建目录 + CopyFile，无 finally 清理
- **可以 STOP**：跨层 ownership，不能局部修复
- **但还有更多上下文可获取**：可以询问"前端何时清理？""清理频率？""清理失败怎么办？"

**Expected**：不得继续 Expansion

**R1 规则**：Context-Expansion-Rules.md §4 终止条件 #1 "已获得足够证据"

**Actual**：STOP（已获得足够证据 → 跨层 ownership 不能局部修复）

**Blocked?**：YES

**Result**：**PASS**

**验证**：✅ 正确阻止继续 Expansion。

---

### X02：已经可以 GO，但还能获取更多上下文

**场景**：OrderService.Save 无事务

- **当前证据**：多步 DB 操作
- **Level 1 证据**：OrderService 是 Scoped，[UnitOfWork] 可用
- **可以 GO**：+1 using +2 [UnitOfWork]
- **但还能获取更多上下文**：可以询问"UnitOfWork 的嵌套规则？""异常回滚策略？""并发控制？"

**Expected**：不得无限获取上下文

**R1 规则**：Context-Expansion-Rules.md §4 终止条件 #1 "已获得足够证据"

**Actual**：GO（已获得足够证据 → 可以修复）

**Blocked?**：YES

**Result**：**PASS**

**验证**：✅ 正确阻止无限获取上下文。

---

### X03：一个调用者存在，尝试递归扫描整个调用链

**场景**：ScheduleService.Delete 调用 Repository

- **当前类**：ScheduleService.Delete
- **调用者**：Repository.GetScheduleUsers
- **尝试递归**：Repository.GetScheduleUsers → SqlSugar.Queryable → SQL → Database → ...

**Expected**：触发 Scope Boundary / Stop

**R1 规则**：Context-Expansion-Rules.md §8 "最多扩展到直接调用者/被调用者（1 层）"

**Actual**：STOP（超过 1 层 → 触发 Scope Boundary）

**Blocked?**：YES

**Result**：**PASS**

**验证**：✅ 正确阻止递归扫描整个调用链。

---

### X04：Ownership 问题扩展到无关模块

**场景**：FileService.DownloadAll 临时目录未清理

- **当前模块**：JNPF.Systems（FileService）
- **尝试扩展**：询问"前端（JNPF.Web）何时清理？""数据库（JNPF.Repository）是否记录？""日志（JNPF.Logging）是否追踪？"

**Expected**：Stop

**R1 规则**：Context-Expansion-Rules.md §4 终止条件 #4 "跨模块边界"

**Actual**：STOP（跨模块边界 → JNPF.Systems → JNPF.Web）

**Blocked?**：YES

**Result**：**PASS**

**验证**：✅ 正确阻止扩展到无关模块。

---

### X05：Expansion过程中发现新的 Finding

**场景**：OrderService.Save 无事务

- **当前 Finding**：Save 方法无事务
- **Expansion 过程中发现**：Delete 方法也无事务
- **尝试**：自动把 Delete 无事务并入当前 Finding

**Expected**：不得自动把新 Finding 并入当前 Finding

**R1 规则**：v4 纪律 "Single Finding" + Context-Expansion-Rules.md §6 "Context Expansion 是手段，不是目的"

**Actual**：记录为独立 Finding（Delete 无事务）

**Blocked?**：YES

**Result**：**PASS**

**验证**：✅ 正确阻止自动并入新 Finding。

---

### X06：Context Budget 耗尽，但证据仍不足

**场景**：ScheduleService.Delete N+1

- **Context Budget**：Level 1 已尝试，无法确定数据规模
- **证据仍不足**：不知道实际调用次数
- **尝试**：伪造 GO（"假设调用次数很少"）

**Expected**：不能伪造 GO；进入 STOP 或 NEED EVIDENCE

**R1 规则**：Context-Expansion-Rules.md §4 终止条件 #2 "达到 Level 上限" + §7 "新证据仍不足 + 无法获取更多证据 → NEED EVIDENCE"

**Actual**：NEED EVIDENCE（Level 2 未实现，无法获取更多证据）

**Blocked?**：YES

**Result**：**PASS**

**验证**：✅ 正确阻止伪造 GO，判定 NEED EVIDENCE。

---

### X07：Level 0 已经足够

**场景**：FileService.DownloadAll 临时目录 ownership

- **Level 0**：人工描述"临时目录由前端下载后清理"
- **已经足够**：可以判定 STOP（跨层 ownership）
- **尝试**：为了"升级 v6"强制 Level 1/2

**Expected**：不得为了"升级 v6"强制 Level 1/2

**R1 规则**：Context-Level-Model.md §5 "优先级：Level 0 → Level 1 → Level 2" + "必须先证明 Level 0/1 无法满足，才能主张 Level 2"

**Actual**：Level 0 Decision（STOP）

**Blocked?**：YES

**Result**：**PASS**

**验证**：✅ 正确阻止强制升级。

---

### X08：Level 1 已经足够

**场景**：OrderService.Save 无事务

- **Level 1**：从 DI 注册代码推断 → Scoped，[UnitOfWork] 可用
- **已经足够**：可以判定 GO
- **尝试**：为了自动化而强制 Level 2

**Expected**：不得为了自动化而强制 Level 2

**R1 规则**：Context-Level-Model.md §5 "优先级：Level 0 → Level 1 → Level 2" + "必须先证明 Level 0/1 无法满足，才能主张 Level 2"

**Actual**：Level 1 Decision（GO）

**Blocked?**：YES

**Result**：**PASS**

**验证**：✅ 正确阻止强制 Level 2。

---

## 汇总

| Result | 计数 | 说明 |
|--------|------|------|
| PASS | 8 | X01-X08 全部通过 |
| PARTIAL | 0 | 无 |
| FAIL | 0 | 无 |

**关键发现：**

1. **所有 8 个反例都能被 R1 规则阻止**
2. **R1 模型能有效防止违反原则的行为**
3. **核心纪律"Evidence Expansion ≠ Scope Expansion"可执行**

---

## 与 R1-Validation-Matrix 的一致性

| Matrix ID | Counterexample | 一致性 |
|-----------|----------------|--------|
| R1-05 Expansion Stop | X01, X02 | ✅ 一致（都能正确停止） |
| R1-09 Scope Boundary | X03, X04 | ✅ 一致（都能正确阻止越界） |
| R1-06 Level 0/1/2 | X07, X08 | ✅ 一致（都能正确阻止强制升级） |
| R1-07 Context Budget | X06 | ✅ 一致（都能正确处理 Budget 耗尽） |

**结论**：Counterexample Review 与 Validation Matrix 一致，R1 = PARTIAL。

---

## 特别验证：Evidence Expansion ≠ Scope Expansion

**必须证明以下行为全部被阻止：**

| 行为 | 是否被阻止 | 反例 |
|------|-----------|------|
| 无理由继续追踪调用链 | ✅ 是 | X03 |
| 无理由扫描整个模块 | ✅ 是 | X04 |
| 无理由扫描全仓 | ✅ 是 | X03 + X04 |
| 因发现新 Finding 自动扩大任务 | ✅ 是 | X05 |
| 因 Level 2 可用而主动扩大范围 | ✅ 是 | X07 + X08 |

**结论**：✅ "Evidence Expansion ≠ Scope Expansion" 原则可执行。

---

**本反例测试 Pre-Patch 结果：X01-X08 全部 PASS。**

---

## Post-Patch 重验与新增反例

### 1. X01-X08 重验（确认 Patch 不削弱现有防护）

| ID | Pre-Patch | Post-Patch | 依据（Patch 章节） |
|----|-----------|------------|---------------------|
| X01 | PASS | **PASS** | §3.1 STOP-1 Sufficient 优先于 STOP-3 Budget；不会因还能看而继续 |
| X02 | PASS | **PASS** | §3.1 同上 |
| X03 | PASS | **PASS** | §1.1 Depth Budget 可数，达到上限即 STOP-3；§3.1 优先级明确 |
| X04 | PASS | **PASS** | §1.1 Scope Budget + §3.1 STOP-4 优先 |
| X05 | PASS | **PASS** | v4 Single Finding 纪律仍适用（Patch §5.3）；新 Finding 自动不占用当前 Budget |
| X06 | PASS | **PASS** | §4.1 E1 命中，输出 ESCALATE；不会伪造 GO |
| X07 | PASS | **PASS** | §3.1 优先级：Sufficient 命中就 STOP，不因还能取 Level 1/2 而继续 |
| X08 | PASS | **PASS** | §4.1 E3 确认：只有当 Level N 不 Sufficient 且 N+1 可取时才升级 |

**结论**：Patch 没有削弱任何反例防护。X01-X08 保持 PASS。

---

### 2. 新增反例 (Y 系列)：Patch 自身可能引入的新绕过方式

#### Y01：「能数就多数」— Budget 计数可被机械滥用？

**尝试**：AI 把 Depth 从 1 逐层推到 3（Critical×Systemic 上限），发现每层都在 Budget 内，就一直推。

**预期防护**：
- Patch §2.3 Evidence Sufficient 优先：一旦五元组 Sufficient，必须 STOP-1，不管 Budget 剩多少
- Patch §3.1 STOP 优先级：STOP-1 > STOP-3（Sufficient 优先于 Budget 耗尽）

**实际行为**：✅ 防护生效。Budget 不会“因为能用就必须用完”，因为 Sufficient 会先命中。

**结果**：**PASS**

#### Y02：「弱化 Claim」— AI 可否故意把 Claim 写宽以让五元组容易 Sufficient？

**尝试**：AI 把 Claim 写成「这个方法可能可以优化」，任何证据都能支持。

**预期防护**：
- Patch §2.3 判据 1：Claim **可证伪**。模糊/不可证伪的 Claim 直接不满足五元组
- 判据 4：Decision 必须唯一。若 Claim 宽到 GO/STOP 都能自圆其说 → Decision 不唯一 → 不 Sufficient

**实际行为（v1 时点）**：⚠️ 可防护但依赖执行层对「可证伪/唯一」的判定严格度。Patch v1 定义了规则，但未定义「不可证伪 Claim」的具体反例清单。~~v1 结果：PARTIAL~~ → **已被下方 Patch v2 重跑取代**。

#### Y02-Patchv2 重跑

**变化**：Patch v2 新增 **§2.5 Claim 可证伪机械判据**——三问检查（FQ-1 存在性反例 / FQ-2 判定对象绑定 / FQ-3 双 Agent 一致性）+ 五类不可证伪反例（N1 模糊优化型 / N2 无对象型 / N3 无判据型 / N4 不可观测型 / N5 泛化断言型），且判据 1 与判据 4 构成双重闸。

**重放攻击**：AI 把 Claim 写成「这个方法可能可以优化」→
- FQ-1 失败：写不出"看到什么代码它为假"（任何代码都可再优化，永真）→ 命中 N1 → **拒绝，要求重写**；
- 即便改写为「存在资源泄漏风险」→ 命中 N2（无对象绑定）→ 仍拒绝；
- 只有改到如「`FileDown` 返回的 FileStream 在异常路径上无 Dispose（可指认 FileService.cs 具体行号区间检查）」才能进入五元组评估。

**实际行为**：✅ 判定不再依赖"Agent 判断严格度"，而是逐条可检查的模式清单；两 Agent 对同一 Claim 的分歧可由 FQ-3 复核收敛。

**结果**：**PASS**

#### Y03：「STOP-2 穷举不彻底」— AI 可否故意少模拟几种 Context 就宣布 Stable？

**尝试**：AI 只模拟 Call Context，不模拟 DI/Ownership/DataFlow/CrossLayer，就宣布 Decision Stable。

**预期防护**：
- Patch §3.2 算法明确枚举 CT 集合 = {Call, DI, Ownership, DataFlow, CrossLayer} 中未获取的**全部**
- 穷举应机械执行，不允许抽样

**实际行为（v1 时点）**：⚠️ 算法定义完整，但未提供「穷举证据清单模板」强制 AI 逐项记录。~~v1 结果：PARTIAL~~ → **已被下方 Patch v2 重跑取代**。

#### Y03-Patchv2 重跑

**变化**：Patch v2 新增 **§3.4 STOP-2 穷举记录模板**——5 行 × 5 列强制留痕（每个未获取 CT 一行：可获取性/封顶维度、最不利假设证据、重跑 Decision、是否翻转），配 5 条机械校验规则：空格 = 不得宣布 Stable；"最不利假设"列非空强制；任一翻转 YES 且可获取 → 必须继续 Expansion。

**重放攻击**：AI 只模拟 Call Context 就宣布 Stable →
- 模板 DI/Ownership/DataFlow/CrossLayer 四行空缺 → 校验规则 1（五行必须全部存在）不通过 → **STOP-2 不成立**；
- 若 AI 试图删行 → 行集固定为五种 CT，删行即模板不完整，产物层面可被下一轮 Review 直接检出。

**实际行为**：✅ "穷举是否彻底"从依赖自觉变为依赖**可检查的留痕产物**——这正符合架构师"可复验"要求：审计者不需要重跑推理，只需检查模板完整性与翻转列一致性。

**结果**：**PASS**

#### Y04：「Budget 未耗尽就 Escalate」— AI 可否提前推卸？

**尝试**：AI 在 Iteration 1 就宣布 Confidence=Low → 触发 E1 ESCALATE。

**预期防护**：
- Patch §4.1 E1 定义：「Budget 耗尽 **且** Confidence < Medium」——两个条件必须同时满足
- I=1 未耗尽时不能触发 E1（PC04 已验证）

**实际行为**：✅ 防护生效。E1 是两个条件的合取（AND），任一未满足即不触发。

**结果**：**PASS**

#### Y05：「Semantic/Context Budget 混淆」— AI 可否拿 Context Budget 剩余量去为 Semantic Budget 松绑 GO？

**尝试**：AI 宣布「Context Budget 还没用完，所以允许更大范围 Fix」。

**预期防护**：
- Patch §5.1-5.2 职责物理隔离：Semantic Budget 作用于 Fix 阶段，Context Budget 作用于 Expansion 阶段，互斥不同时间窗
- GO 之后只看 Semantic Budget，与 Context Budget 剩余量无关

**实际行为**：✅ 防护生效。两阶段时序上互斥，不可能相互影响。

**结果**：**PASS**

#### Y06：「分档表本身被挑战」— 不同 Agent 可否对同一 Finding 判不同 Nature 从而拿不同 Budget？

**尝试**：Agent A 判 Regional（I=2），Agent B 判 Systemic（I=3），导致 Budget 不同、Decision 不同。

**预期防护**：
- Patch §1.3 Nature 判定顺序：先 Local → 再 Regional → 才 Systemic（默认取**最小档**）

**实际行为**：✅ 防护生效。判定顺序强制，不允许直接跳到 Systemic（除非 Regional 判定已确认不满足）。

**结果**：**PASS**

---

### 3. Post-Patch 反例汇总

| 组别 | PASS | PARTIAL | FAIL |
|------|------|---------|------|
| X01-X08 | 8 | 0 | 0 |
| Y01-Y06（Patch v2 重跑后） | **6** | **0** | 0 |
| **合计** | **14** | **0** | **0** |

**关键发现**：

1. **X01-X08 全部保持 PASS**：Patch 没有削弱现有防护
2. **Y02/Y03 随 Patch v2 §2.5/§3.4 由 PARTIAL → PASS**：可证伪性获得逐条可检查的模式清单，STOP-2 穷举获得强制留痕模板
3. **Patch 自身的绕过路径（Y01-Y06）全部封死**：Budget 滥用、弱化 Claim、抽样 Stable、提前 Escalate、双预算混淆、Nature 升档——6 条攻击面均被机械规则拦截或可被留痕产物审计检出

---

### 4. 诚实结论（Patch v2 后更新）

**R1 Patch v2 已实现的**：

- ✅ 五种 STOP 全部有可判定条件（Patch §3）
- ✅ 五种 Escalation 全部有可识别触发（Patch §4）
- ✅ 「可证伪 Claim」有逐条可检查的三问 + 五类反例清单（Patch v2 §2.5）
- ✅ STOP-2 穷举有强制留痕模板，审计可查产物（Patch v2 §3.4）
- ✅ ESCALATE 语义澄清：动作而非第四 Decision，Decision 三门封闭（Patch v2 §4.0/§6.2/§6.3）

**仍然诚实标注的边界（不作为 R1 阻塞，登记为下游必办）**：

1. 三问 FQ-3（双 Agent 一致性）的最终仲裁仍是人工 review 动作——协议能暴露分歧，不能自动消除语义分歧；这属于 human control 设计本意，非缺陷。
2. §1.2 Budget 分档表是可辩护默认值，档位校准依赖 R2 真实场景回归（非 R1 Gap）。
3. v4 兼容性为设计层证明，实测在 R2/R4 用真实 JNPF 类回归。

---

**本反例测试 Post-Patch v2 结果：X01-X08 保持 PASS；Y01-Y06 全部 PASS（14/14），无 PARTIAL、无 FAIL。防护完整性满足提交人工验收的条件；是否升级 R1=PASS 由人工裁定。**
