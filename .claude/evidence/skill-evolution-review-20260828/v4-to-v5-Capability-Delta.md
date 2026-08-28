# v4.0 → v5.0 Capability Delta — 基于 Git 与仓库证据

> 证据源：commit `093b4e11`（v5.0，2026-08-28 04:53）实 diff、`git show --stat`、SKILL.md v5.0 段、3 个 reference 文件、commit message 自述。
> 纪律：区分 A 真正新增能力 / B 能力强化 / C 规则细化 / D 文档结构变化；不把 C/D 冒充 A。

## 0. v5.0 在仓库里到底改了什么（实测足迹）

`git show 093b4e11 --stat`：
```
SKILL.md                                  | 40 +++---
references/Data-Volume-Sensitivity-Rule.md | 73 ++++++ (新)
references/Impact-Assessment-Rule.md       | 89 ++++++ (新)
references/ORM-Behavior-Quick-Reference.md | 75 ++++++ (新)
4 files changed, 265 insertions(+), 12 deletions(-)
```
SKILL.md 实 diff：新增一节 **「### v5.0 Additions (P2/P3/P4) — After Findings are identified, apply the following post-processing」**，把 P2/P3/P4 定义为**在 Findings 识别之后叠加的后处理步**，分别挂到既有维度 D3/D6/D8、D5、D4/D5/D10；并加 changelog 表 + 3 条 References。**没有新增任何决策架构、门控或执行系统。**

## 1. v4.0 基线能力模型（对照基准，取 v4.0 规格 + 校准评审实证）

| 能力域 | v4.0 状态 | 核心机制（v4.0 里"能做什么"） | 证据 |
|--------|-----------|------------------------------|------|
| Evidence | 完整 | P0 五维取证先行，缺 P0 则 P1..P10 全封锁 | 规格 §2、SKILL P0.1-0.5 |
| Ownership | 完整 | 资源生命周期四问 Create→consume→end→who owns→dispose | Resource-Lifetime-Ownership-Rule.md、Golden#2/3 |
| Finding | 完整 | 16 维排查，Finding≠Fix，发现不自动改码 | 规格 §1.3、三安全阀 |
| Risk | 完整 | C/H/M/L 分级 + JNPF N1-N4 Critical | Risk-Matrix-template.md |
| GO/STOP/NEED | 完整(M1校准) | 三态门控，NEED EVIDENCE≠STOP 显式化 | 校准 M1、91e90cdb |
| Semantic Budget | 完整(M2校准) | 最小语义变更预算（非物理行数） | 校准 M2、OrderService CS0246 案例 |
| Verification | 完整 | characterization/Benchmark/Arch test，Deferred/Env-Blocked 诚实记录 | Golden#4 Deferred、F-P1 Need-Evidence |
| Golden Example | 完整 | 4 个跨技术性质样本(Exception/Resource×2/Transaction) | Golden-Example-01..04 |
| Convergence | 完整(M3校准) | Class-level 收敛停止规则显式化 | 校准 M3 |
| Calibration | 完整 | 六问决策质量复盘 + Decision Replay | v4-calibration/*.md、91e90cdb |

> 结论：**v4.0 已是一个"证据驱动的类级诊断与重构决策系统"**，不是清单。这是判断 v5 是否"升级"的标尺。

## 2. v4 → v5 能力差异表

| # | v4.0 能力 | v5.0 新增/改变 | 为什么需要 | 解决 v4 什么问题 | 证据 | 已实现? | 已验证? | 类别 |
|---|-----------|----------------|----------|------------------|------|---------|---------|------|
| P2 | D3/D6/D8 各维内自行判断 | 查框架默认行为表(SqlSugar/EF/Dapper)核对安全性 | ORM 默认行为易被忽略 | v4 靠人记忆框架语义，漏检(如乐观锁) | ORM-Behavior-Quick-Reference.md + SKILL diff | 规则已实现 | **仅 commit 自述 F1+0.05(GS-14)，仓库无评测工件** | **C 规则细化 + D 新文档**（挂在既有维，非新能力域） |
| P3 | D5 只判 N+1 形态 | 5 种数据量模式 + DataVolume/Reason/Decision 字段 | 形态对但规模未知会误判严重度 | v4 无法给"量纲"，NEED_EVIDENCE 粗糙 | Data-Volume-Sensitivity-Rule.md | 规则已实现 | 声称 3 项 NEED_EVIDENCE 加量纲，无独立复算工件 | **C 规则细化 + D 新文档** |
| P4 | D4/D5/D10 按表面严重度 | 数据源追溯(HARDCODED/REFLECTION 降级) + SeverityOriginal/Adjusted | 硬编码源不该高估 | v4 严重度不区分数据来源 | Impact-Assessment-Rule.md | 规则已实现 | 声称消除 1 FP/3 严重度精确化，无复算工件 | **C 规则细化 + D 新文档** |

## 3. 定性裁决：v5.0 是"真新能力"还是"规则增强"？

- **A 类（真正新增能力域）：0。** v5.0 未新增任何 v4 不存在的能力域，也未新增决策门控/执行系统。
- **B 类（能力强化）：弱。** 提升的是既有维度的**检出准确性与严重度精度**，但机制是"多查一张表/多带几个字段"，不改决策骨架。
- **C 类（规则细化）：主。** P2/P3/P4 本质是把 v4 已能覆盖的判断"显式化、表单化"。
- **D 类（文档/结构）：主。** 全部改动 = SKILL.md 增段 + 3 reference 文件；**无 v5.0 规格、无 v5.0 计划、无 v5.0 Golden、无 v5.0 评测集**（`git ls-files` 仅命中 v4.0 规格/计划）。

> **一句话：v4→v5 = 在冻结的 v4 决策系统之上"加了三张后处理查表 + 字段规范"，属 C/D，非 A/B 的架构升级。**

## 4. 验证状态诚实标注

| 项 | 状态 |
|----|------|
| P2/P3/P4 规则文本 | `IMPLEMENTED`（SKILL.md + reference 存在） |
| v5.0 "F1 0.900→0.950 / +1 TP / -1 FP" | `NOT_FOUND_IN_REPOSITORY`（commit message 与 changelog 声称，但全仓无 Golden Set / 无标注评测集 / 无复算脚本；Golden-Example-01..04 是重构范例非评测集） |
| v5.0 专属规格/计划文档 | `NOT_FOUND_IN_REPOSITORY` |
