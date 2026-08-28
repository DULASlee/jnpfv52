# v5.0 → v6.0 Target — 基于仓库证据的目标澄清

> 证据源：commit `b3b8acde`（v6.0-alpha，2026-08-28 05:05）实 diff、`references/Cross-Class-Context-Rule.md` 全文、commit message 自述。
> 纪律：区分"仓库已存在的官方目标"与"工程师推测应做"；区分 目标 vs 已实现。

## 0. v6.0 在仓库里到底改了什么（实测足迹）

`git show b3b8acde --stat`：
```
SKILL.md                                  | 28 ++++-
references/Cross-Class-Context-Rule.md    | 140 ++++++ (新)
2 files changed, 163 insertions(+), 5 deletions(-)
```
实 diff：SKILL.md 加 **「### Step 2.5: Cross-Class Analysis (D11)」** + 扫描表 D11 行 + 1 条 Reference。commit message 自述关键三行：
```
- New: references/Cross-Class-Context-Rule.md (D11, 3 check items)
- SKILL.md: v5.0→v6.0, +D11 in scan table, +Step 2.5 cross-class analysis
- Cross-class context: Level 0 (manual) for alpha phase
```
> **"alpha phase" + "Level 0 (manual)" 是仓库自带的定性，不是我推测。** 即 v6.0 目前只是个 alpha 桩，D11 只能靠人工喂调用链。

## 1. v6.0 为什么存在（仓库可证，非"更强"这种空话）

`Cross-Class-Context-Rule.md` L8/L15 明确：
> v5.0 的 D1~D10 都在**单类内部**排查。D11 看的是**类与类之间的边界**……这些问题**只看一个类的代码无法回答**，必须知道类之间的关系。

即 v6.0 的存在理由 = v4/v5 的整条执行协议都是**单类视野**，对以下三类跨类问题结构性无法回答：
- 11.1 资源/Stream 跨类 ownership（A 调 B，B 返回 IDisposable，谁释放？）
- 11.2 DI 生命周期跨层（Singleton 注 Scoped？）
- 11.3 数据量跨方法传播（B 返回全量，A 有无截断？）

## 2. v5 → v6 目标能力表（含"已实现程度"）

| # | v5.0 当前能力 | 当前限制 | v6.0 要解决什么 | 目标能力 | 仓库实现状态 |
|---|---------------|----------|-----------------|----------|--------------|
| G1 | D1~D10 单类排查 | 无法回答跨类 ownership/DI/数据量 | 引入第 11 维跨类生命周期 | D11 三维检查清单 + 输出字段 | `PARTIALLY_IMPLEMENTED`（规则/清单已写，执行依赖人工上下文） |
| G2 | 跨类上下文靠人脑 | Level 0 人工描述、Level 1 文本，成本高不可复现 | 自动化跨类取证 | Level 2：Roslyn call-graph.json / di-registration.json | `NOT_FOUND_IN_REPOSITORY`（reference 明文"第二期工具开发"，仅有会话内未落仓的 R1 分析器雏形） |
| G3 | 决策系统单类闭环 | 单类 GO/STOP 无法覆盖"跨类才能定性"的 Finding | 让 D11 Finding 也能进 GO/STOP/NEED 门 | 跨类证据→决策门贯通 | `PROPOSED`（SKILL 只加"若上下文可用则分析"，未定义跨类门控） |

## 3. 状态汇总（严禁把"准备做"写成"已具备"）

| 状态标签 | 归属 |
|----------|------|
| `IMPLEMENTED` | 无（v6.0 无任何端到端可复现能力落地） |
| `PARTIALLY_IMPLEMENTED` | D11 规则文本 + 3 检查项 + 输出字段（Level 0/1 可手工跑） |
| `PROPOSED` | 跨类 Finding 纳入决策门 |
| `NOT_FOUND_IN_REPOSITORY` | Level 2 自动 call-graph/DI-graph 工具；v6.0 专属规格/计划文档；R1→R4 路线图文档（全仓搜 `JnpfAnalyzer`/`Correctness Gate`/`v6.0 R1` 零命中，仅存在于会话 chat） |

## 4. 特别调查：v5→v6 是"继续加规则"还是"升级成专家级决策/执行系统"？

- **仓库字面证据**：v6.0-alpha 只加了一维 D11 + 一张规则表 → 表面仍是"加规则"（同 v5 的 C/D 套路）。
- **目标语义证据**：D11 想解锁的 Level 2 是"把 Skill 从**单类人工判断**升级为**解决方案级自动取证决策**"——若真做，属**架构级能力升级**（引入工具子系统）。
- **裁决**：**意图是架构升级，现状仍是规则桩。** 仓库里**没有任何已实现的执行系统/工具**支撑该升级；把它当"已经是专家级执行系统"属超前定性。
