# R1 Validation Matrix — 10 项验收矩阵

> **版本**：v6.0-R1-Validation | **日期**：2026-08-28 | **状态**：🟢 R1 = PASS（2026-08-28 人工验收；随 R1 冻结，F-R1-①）  
> **核心原则**：Definition ≠ Executability | 规则必须能推出唯一、可重复的决策

---

## 验收矩阵

| ID | 验收能力 | 输入 | 规则 | Expected | Actual | Ambiguous? | Evidence | Result |
|----|----------|------|------|----------|--------|------------|----------|--------|
| R1-01 | Context Model 概念定义 | 5 种 Context Type 定义 | V6-Context-Model.md §2 | 每种 Context Type 有明确的输入/输出/证据字段 | 5 种 Context Type 都有定义，但部分字段描述抽象（如 Data-flow 的 "data_volume" 无度量标准） | NO | V6-Context-Model.md §2 | **PARTIAL** |
| R1-02 | Context Unit 最小必要上下文 | Finding + 当前类证据 | V6-Context-Model.md §3 | 能判断一个 Context Unit 是否"必要" | 判断标准清晰（3 个问题），但缺少具体度量（如"成本合理"无阈值） | NO | V6-Context-Model.md §3 | **PARTIAL** |
| R1-03 | Context Dependency 依赖关系 | Finding + 缺失证据 | V6-Context-Model.md §4 | 能识别缺失哪种上下文 | 依赖关系图清晰，但缺少"如何判断直接影响判定"的操作规则 | NO | V6-Context-Model.md §4 | **PARTIAL** |
| R1-04 | Expansion Trigger 触发条件 | Finding + 当前类证据 + 缺失上下文 | Context-Expansion-Rules.md §3 | 能判断是否触发 Expansion | 3 个条件清晰，但"直接影响判定"无操作定义 | NO | Context-Expansion-Rules.md §3 | **PARTIAL** |
| R1-05 | Expansion Stop 终止条件 | Expansion 进行中状态 | Context-Expansion-Rules.md §4 | 能判断何时停止 | 5 个条件清晰，但"已获得足够证据"无操作定义 | NO | Context-Expansion-Rules.md §4 | **PARTIAL** |
| R1-06 | Level 0/1/2 升级纪律 | 当前 Level + 证据充分性 | Context-Level-Model.md §5 | 能判断是否升级 | 升级条件清晰，但"成本合理"无阈值 | NO | Context-Level-Model.md §5 | **PARTIAL** |
| R1-07 | Context Budget 可操作性 | Context Expansion 成本 + Finding 收益 | Context-Budget.md §3 | 能度量成本/收益，判断是否 STOP | **Budget 单位未定义**（Time/Complexity/Accuracy 是维度，不是单位）；成本 > 收益无操作规则 | NO | Context-Budget.md §3 | **FAIL** |
| R1-08 | Decision Re-entry 重新进入 | Expansion 后获得的新证据 | Context-Expansion-Rules.md §7 | 能重新进入 GO/STOP/NEED | 流程清晰，但"新证据是否充分"无操作定义 | NO | Context-Expansion-Rules.md §7 | **PARTIAL** |
| R1-09 | Scope Boundary 范围边界 | Expansion 范围 | Context-Expansion-Rules.md §8 | 能防止全仓扫描 | 限制"最多 1 层"清晰，但"间接调用链"定义模糊 | NO | Context-Expansion-Rules.md §8 | **PARTIAL** |
| R1-10 | v4 Compatibility 兼容性 | v4 核心纪律 | V6-Context-Model.md §1.3 + Context-Expansion-Rules.md §6 | v6 不弱化 v4 | 继承关系清晰，但 Semantic Budget vs Context Budget 职责边界未证明 | NO | V6-Context-Model.md §1.3 | **PARTIAL** |

---

## 汇总

| Result | 计数 | 说明 |
|--------|------|------|
| PASS | 0 | 无完全通过项 |
| PARTIAL | 9 | 概念定义清晰，但缺少操作规则/度量标准 |
| FAIL | 1 | R1-07 Context Budget 不可操作 |

**关键发现：**

1. **所有验收项都"概念定义清晰"**，但**都缺少操作规则/度量标准**
2. **R1-07 Context Budget 是唯一的 FAIL**：Budget 单位未定义，成本 > 收益无操作规则
3. **核心问题**：规则是"原则"，不是"可执行规则"

---

## 详细分析

### R1-01 Context Model — PARTIAL

**问题**：5 种 Context Type 都有定义，但部分字段描述抽象。

**示例**：Data-flow Context 的 `data_volume` 字段定义为"未知（需运行时证据）"，但没有定义如何度量"未知"。

**影响**：无法判断 Data-flow Context 是否"必要"。

### R1-02 Context Unit — PARTIAL

**问题**：判断标准清晰（3 个问题），但"成本合理"无阈值。

**示例**：什么情况下"成本合理"？5 分钟？30 分钟？1 小时？

**影响**：无法判断一个 Context Unit 是否"必要"。

### R1-03 Context Dependency — PARTIAL

**问题**：依赖关系图清晰，但"如何判断直接影响判定"无操作规则。

**示例**：如何判断"缺失的上下文直接影响判定"？是主观判断，还是有客观标准？

**影响**：无法判断是否应该触发 Expansion。

### R1-04 Expansion Trigger — PARTIAL

**问题**：3 个条件清晰，但"直接影响判定"无操作定义。

**示例**：同 R1-03。

**影响**：无法判断是否应该触发 Expansion。

### R1-05 Expansion Stop — PARTIAL

**问题**：5 个条件清晰，但"已获得足够证据"无操作定义。

**示例**：什么情况下"已获得足够证据"？是主观判断，还是有客观标准？

**影响**：无法判断何时停止 Expansion。

### R1-06 Level 0/1/2 — PARTIAL

**问题**：升级条件清晰，但"成本合理"无阈值。

**示例**：同 R1-02。

**影响**：无法判断是否应该升级。

### R1-07 Context Budget — FAIL

**问题**：Budget 单位未定义，成本 > 收益无操作规则。

**示例**：
- Time Budget 的单位是什么？分钟？小时？
- Complexity Budget 的单位是什么？层数？调用次数？
- Accuracy Budget 的单位是什么？可信度百分比？
- 成本 > 收益如何度量？

**影响**：Context Budget 是抽象概念，不可操作。

### R1-08 Decision Re-entry — PARTIAL

**问题**：流程清晰，但"新证据是否充分"无操作定义。

**示例**：同 R1-05。

**影响**：无法判断重新进入后的决策。

### R1-09 Scope Boundary — PARTIAL

**问题**：限制"最多 1 层"清晰，但"间接调用链"定义模糊。

**示例**：什么是"间接调用链"？A → B → C 是间接吗？A → B → C → D 呢？

**影响**：无法判断是否超出范围。

### R1-10 v4 Compatibility — PARTIAL

**问题**：继承关系清晰，但 Semantic Budget vs Context Budget 职责边界未证明。

**示例**：Semantic Budget 限制代码修改，Context Budget 限制证据获取。两者如何协调？是否有冲突场景？

**影响**：无法证明 v6 不弱化 v4。

---

## 结论

**R1 = PARTIAL**

**核心问题**：规则是"原则"，不是"可执行规则"。

**Gap 清单**：

| Gap | 影响 | 是否阻塞 R2 | 最小修复建议 |
|-----|------|-------------|--------------|
| R1-07 Context Budget 不可操作 | 无法判断成本 > 收益 | 是 | 定义 Budget 单位 + 成本/收益度量规则 |
| R1-01~06, 08~09 缺少操作规则 | 无法稳定推出唯一决策 | 否 | 定义操作规则（阈值/标准） |
| R1-10 v4 兼容性未证明 | 无法证明 v6 不弱化 v4 | 否 | 证明 Semantic Budget vs Context Budget 职责边界 |

**是否阻塞 R2**：

- **R1-07 阻塞 R2**：Context Budget 不可操作，R2 无法实现 Level 0/1 acquisition
- **其他 Gap 不阻塞 R2**：可以在 R2 中逐步完善操作规则

---

**本矩阵 Pre-Patch 状态：R1 = PARTIAL（0 PASS / 9 PARTIAL / 1 FAIL）。**

---

## Post-Patch 重评估（2026-08-28，基于 R1-Operationalization-Patch.md）

### 更新后的验收矩阵

| ID | 验收能力 | Pre-Patch | Post-Patch | 关闭依据（Patch 章节） |
|----|----------|-----------|------------|----------------------|
| R1-01 | Context Model 概念定义 | PARTIAL | **PASS** | §1.2 Nature 三档判定 + §2.1 五元组字段可回溯 |
| R1-02 | Context Unit 最小必要上下文 | PARTIAL | **PASS** | §2.3 Evidence Sufficient 五判据 + §2.5 可证伪三问/五类反例（v2）可执行 |
| R1-03 | Context Dependency 依赖关系 | PARTIAL | **PASS** | §1.3 Nature 判定顺序 + §3.2 STOP-2 穷举算法 + §3.4 留痕模板（v2） |
| R1-04 | Expansion Trigger 触发条件 | PARTIAL | **PASS** | §2.3 判据 4「Decision 唯一」替代「直接影响判定」主观表述 |
| R1-05 | Expansion Stop 终止条件 | PARTIAL | **PASS** | §3.1 五种 STOP 优先级序列全部可判定 |
| R1-06 | Level 0/1/2 升级纪律 | PARTIAL | **PASS** | §3.1 STOP-4/5 优先 + §4.1 E3 明确 Level 依赖 |
| R1-07 | Context Budget 可操作性 | **FAIL** | **PASS** | §1.1-1.2 五维 Budget 全部可数（modules/layers/artifacts/rounds） |
| R1-08 | Decision Re-entry 重新进入 | PARTIAL | **PASS** | §2.3 判据 5 + §4 Escalation 三种出口 |
| R1-09 | Scope Boundary 范围边界 | PARTIAL | **PASS** | §1.2 三档 Nature 明确 S=0/1/2 上限 |
| R1-10 | v4 Compatibility 兼容性 | PARTIAL | **PASS** | §5.1-5.4 四概念职责物理隔离证明 |

### Post-Patch 汇总

| Result | 计数 |
|--------|------|
| **PASS** | **10** |
| PARTIAL | 0 |
| FAIL | 0 |

### 为什么这次不是「伪精确」

- ❌ **没有**引入「30 分钟」「70%」这类估不准的数字
- ✅ 所有 Budget 维度**能数**：模块 / 层 / 工件 / 轮次
- ✅ 所有 STOP 判据**能判**：Claim 可证伪 / 证据可回溯 / Decision 单值 / 穷举翻转检查
- ✅ 所有 Escalation 触发**能识别**：Confidence 档位 + Budget 计数 + 冲突比对

### 诚实标注：Post-Patch 结论的有效边界（v2 更新）

1. **二次 Replay 已完成**：`C01-C10-Decision-Replay.md` Post-Patch v2 重放 20/20 PASS（含 C09 重跑 + PC/NC/EC）；`R1-Counterexample-Review.md` Post-Patch v2 重验 14/14 PASS（X01-X08 保持 + Y01-Y06 全过，Y02/Y03 已随 Patch v2 §2.5/§3.4 关闭）。本矩阵 10/10 结论的下游依赖已解除，无回退触发条件。
2. **代码锚点已实证**：C09 证据 `FileService.cs:240-264 无 finally 清理` 于 2026-08-28 经真实代码 Read 复核（原引用 240-258 行号已修正），满足 §2.3 判据 2"证据可回溯"。
3. **v4 兼容性仍是「设计层证明」**：§5 的四概念职责隔离是概念层证明，实测需要在 R2 落地时用真实 JNPF 类回归——已登记 R2 必办。
4. **Budget 分档表本身可辩护**：§1.2 中「Low × Systemic 不允许 Expansion」这类规则是**可争论的默认设置**，不是不可改的物理规律。R1 通过不代表这套分档永远不变；调档属于 Skill Maintenance 范畴，不属于 R1 Gap。

---

## Post-Patch 最终状态

> 🟢 **2026-08-28 人工验收裁定：R1 = PASS**（首席架构师批准，同时解锁 R2 排期）。
> **PASS 限定**：通过对象 = R1 Context Model 的设计与操作化契约；非整个 Skill 完成；非 R2 免验证实施。
> **R1 冻结（F-R1-①）**：本矩阵与 R1 全部交付物冻结，除 R2 真实执行证据证明 R1 缺陷外不再修订；调分档 = Skill Maintenance 走人工裁定，不是实现层自由。
> **Consumer 纪律（F-R1-③）**：R1 = Contract，R2 = Consumer。实现困难 → Implementation Gap 记录 → 违反 Contract 则 R2 停止 → 人工决定是否演进 R1。

**状态轨迹**：PARTIAL（0/9/1）→ Patch v2 全绿（10 项资格 / Replay 20/20 / Counterexample 14/14 / 锚点实证）→ **人工验收 PASS**。

人工验收全文见 `R1-Validation-Review-Pack.md` §10。
