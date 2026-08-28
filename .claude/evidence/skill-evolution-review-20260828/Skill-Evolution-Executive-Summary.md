# Skill Evolution Executive Summary（6 问，全部仓库可证）

> 只依据 Git + SKILL.md + references + 规格/校准文档。不含设计、不含推测性补写。

### 1. v4.0 是什么？
一个**证据驱动的 .NET 类级诊断与重构决策系统**（非技术清单）：P0 五维取证先行 → 16 维 Finding → Risk 分级 → GO/STOP/NEED 三门 → 最小语义预算 → 单提交 → 验证 → 收敛。有正式规格+计划文档（`通用类级重构专家Skill规格-v4.0.md` / 计划），4 个跨技术性质 Golden（Exception/Resource×2/Transaction），并经六问 Calibration Review 落地 M1/M2/M3。**v4.0 = 唯一成熟且有证据链的版本，已在 `81bc1dce` 冻结。**

### 2. v4 → v5 增加了什么？
新增 **P2 ORM 行为查表 / P3 数据量敏感度 / P4 影响面(数据源追溯)** 三个**后处理步** + 3 张 reference。全部挂在 v4 既有维度(D3-D10)之上，"在 Findings 识别后叠加"。commit `093b4e11`，265 行改动，全在 SKILL.md 增段 + 3 新文档。

### 3. 这些变化中哪些是真正的新能力？
**0 个真正新能力域。** 净性质是 **C 规则细化 + D 文档新增**（把 v4 已能覆盖的判断表单化、给严重度量纲）。未新增能力域、门控或执行系统。→ **把 v5.0 称作一次"版本升级"存在拔高，它更像 v4.0 的一次增量校准。**

### 4. v5.0 当前已达到什么程度？
- 规则文本：`IMPLEMENTED`（在仓库里）。
- 声称的验证 "F1 0.900→0.950 / +1 TP / -1 FP"：`NOT_FOUND_IN_REPOSITORY` —— 全仓无标注 Golden Set、无评测脚本、无复算证据（Golden-Example-01..04 是重构范例，不是评测集）。
- **结论：v5.0 = 规则已写、验证未证实。** 无 v5.0 规格/计划文档。

### 5. v5 → v6 为什么要升级？
v4/v5 整条协议是**单类视野**；`Cross-Class-Context-Rule.md` 明确 D1~D10"只看一个类的代码无法回答"跨类 ownership/DI 生命周期/数据量传播。D11 = 把分析视野扩到**类与类之间的边界**。

### 6. v6.0 最终要解决的核心问题是什么？
让 Skill 能基于**跨类证据**下判断，其完整形态 = 用 **Level 2 自动化 call-graph/DI-graph（Roslyn）** 取代 Level 0 人工喂链 —— 即把 Skill 从"单类人工判断"升级为"解决方案级自动取证决策"。**这才是 v6.0 的真实目标（架构级），不是"再加一维"。**

---

## 核心结论（对"到底有没有真 4→5→6 演进"的正面回答）

- **Git 上确有 `v4.0 → v5.0 → v6.0-alpha` 三次版本标记**，但三者**不是同一量级的东西**：
  - v4.0 = 成熟决策系统（有规格/计划/Golden/校准/冻结）。
  - v5.0 = v4 的**规则增量 + 文档**（C/D 类），验证声称**仓库无凭**。
  - v6.0 = **alpha 规则桩**，定义性能力(Level 2 工具)**未落仓**，仅 Level 0 手工可用。
- **用户担忧成立**：所谓"5.0/6.0"很大程度上是**把若干轮校准、JNPF 实战、规则沉淀在口头上顺延成了版本号**，而非三段同质的能力跃迁。**版本历史需要重新校准**（建议：把 v5.0 定性为 v4.x 校准或"v5.0-rc，验证待补"，把 v6.0 明确标注为"v6.0-alpha，能力未实现，依赖 R1 工具"），否则在"自己没定义清楚的版本"上继续堆 6.0 = 高风险。

## 证据索引
- Commits：`e45f724a` `81bc1dce` `91e90cdb` `093b4e11`(v5.0) `b3b8acde`(v6.0-alpha, HEAD)
- 文件：`.claude/skills/generic-class-refactor-expert/SKILL.md` + `references/{ORM-Behavior,Data-Volume-Sensitivity,Impact-Assessment,Cross-Class-Context}-*.md`
- 文档：`docs/superpowers/specs/通用类级重构专家Skill规格-v4.0.md`、`plans/…v4.0.md`、`.claude/evidence/class-refactor-expert-v40/v4-calibration/*`
- `git ls-files`：v5/v6 无规格/计划；全仓搜 `JnpfAnalyzer/Correctness Gate/v6.0 R1` = 0 命中。
