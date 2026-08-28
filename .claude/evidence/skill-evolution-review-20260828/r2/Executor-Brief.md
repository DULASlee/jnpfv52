# Executor Brief — 盲执行简报（R2 36-runs 唯一执行输入，除 Patch v2 + scenario.md 外不得多读一份）

> **给 Executor 的话**：你的全部合法输入 = ① `R1-Operationalization-Patch.md`（规则，唯一契约）② 本简报 ③ 你的 `scenario.md` ④ scenario 目录内 `manifest.json` / `baseline/**` / `human-cards/**`（若 scenario 声明人工可用）。
> **禁止读取**：`answer-cards/`、`tests/**`、`Level-0-1-Validation.md` §7、`Level-1-Context-Acquisition.md` §2/§5、`R2-GAP-01.md`、`R1-Validation-Review-Pack.md`、任何其他 `scenarios/RB-*` 目录、主仓库工作树（只许读自己 scenario 的 baseline 副本）。读到即整轮作废（F-A）。

## 使命

对 scenario 的 Finding 执行 Patch v2 完整协议（§1 Budget → §2 五元组 → §3 STOP-1~5 → §4 Escalation → §6 契约），产出一份 trace JSON。**规则怎么判你就怎么走；你不知道也不该猜任何"标准答案"。**

## 每轮固定五步（Patch §3.3 检查点是义务不是选项）

1. 声明本轮缺口（Context Type + 为什么当前证据推不出唯一 Decision）
2. 针式取证：只打缺口。工具 = 对 baseline 文件的 Read/Grep。**每次读取必须在 actions 里如实登记**（trace 会被机械重算，虚报=被抓）
3. 证据登记：`file:line` 必须给真实 path/lines/**snippet 原文**（Validator 会逐字回放你引用的行，编造必死）；人卡回复用 `human-statement` + 卡内原文
4. 计数更新（口径见下）
5. STOP 顺序检查：按 §3.1 优先级 4→5→1→2→3 逐条记布尔值与命中，写进该轮 `stop_check`；命中后不得再开新一轮

## 计数口径（锁定 A-§4，已人工批准）

| 动作 | Artifact | Depth | Scope |
|------|----------|-------|-------|
| 读 finding 文件（manifest.finding_file 所指） | 0 | 0 | 0 |
| 定点 grep/symbol 查一个**新文件**的特定行 | **+1**（首见去重） | +1 | 0 |
| read 一个**新文件正文**，同 project | +1 | +1 | 0 |
| read 一个**新文件正文**，project ≠ finding_project | +1 | +1 | **+1 project**（首见去重） |
| 读 baseline 中不存在的"外部"目标（NuGet 等） | 0（也不产证据） | — | — |
| 一轮五步闭环 | — | — | Iteration **+1** |

project = manifest.files[].project。Iteration 上限、其余全部规则以 Patch v2 为准。

## Trace 输出（JSON，写到 scenario.md 指定路径）

```json
{ "schema": "r2-trace/1", "case_id": "<你的场景号>", "run": <N>,
  "finding": {"project": "…", "file": "…", "risk": "…", "nature": "Local|Regional|Systemic",
              "nature_order_checked": ["Local", "…按 §1.3 顺序"], "claim": "过 §2.5 三问的可证伪主张"},
  "claim_gate": {"fq1": true, "fq2": true, "fq3": true},
  "budget_allocation": {"depth":0,"artifact":0,"iteration":0,"scope":0},
  "iterations": [ { "round":1, "context_type":"Call|DI|Ownership|DataFlow|CrossLayer", "level":"Level0|Level1",
      "actions":[{"tool":"Read|Grep","mode":"body|signature","target":"manifest中的repo路径","hop":1,"purpose":"…"}],
      "evidence":[{"source":"file:line|tool-output|human-statement","path":"…","lines":"A-B","snippet":"逐字原文","confidence":"H|M|L","card_id":"仅人卡"}],
      "counters_after": {"depth":0,"artifact":0,"iteration":1,"scope":0},
      "stop_check": {"STOP4":false,"STOP5":false,"STOP1":false,"STOP2":false,"STOP3":false,"hit":null} } ],
  "stable_matrix": "仅当任何一轮 STOP-2=true 时必须为完整 5 行数组（Patch §3.4），否则 null",
  "five_tuple": {"claim":"…","evidence":"…","impact":"成立→X；不成立→Y","confidence":"High|Medium|Low","decision":"GO|STOP|NEED_EVIDENCE"},
  "final": {"decision":"GO|STOP|NEED_EVIDENCE","stop_triggered":"STOP-1..5","stop_reason":"引用条款；禁成本/时间话术"},
  "escalation": null,
  "meta": {"time_observed_minutes": 0} }
```

STOP-5 时 `escalation` 必须为 Patch §4.2 全字段 Pack。final 与末轮 hit、five_tuple.decision 必须一致（三门封闭：ESCALATE 不是 Decision）。

## 决策重入门（v4 冻结原语摘录——§2.3/§3.1 的 Decision 必须同时过这道门，Q7 语义）

> 引自 v4.0 SKILL.md / Evidence-to-Modify-Gate / Evidence-to-Stop-Gate（冻结条文，此处为执行必读摘录）：

- **GO（Allow Modify，6 条合取，缺一不 GO）**：Finding 已证 ∧ 违反 Contract ∧ **单点边界可守** ∧ 各 gate 通过 ∧ 存在回归路径 ∧ 不扩 Contract。
- **STOP（10 条析取，任一即 STOP）**：无证据猜测 / 能力不在 Contract / 仅测试缺口 / **不是缺陷** / 需扩 Contract / 需新架构 / **无证据的性能改动（needs perf without evidence）** / 守不住单点 / **跨模块传染** / 无法回归。
- **NEED EVIDENCE（第三态，独立于上两门）**：Finding 可能真实但运行时/收益证据缺失且环境受阻——冻结为 `NEED EVIDENCE / BLOCKED`，禁止被压力推成 GO，也禁止无重决策直接转 STOP。
- **硬门 2（性能）**：Performance Gate 7Q 缺位 → 性能类优化 blocked；P0.2 要求任何性能工作前 ≥2 条运行时事实（CPU/内存/GC/延迟/慢查询等）。**N+1/批量类 Claim 即使形态确证，若数据量级无运行时证据 → Decision 不得为 GO，应为 NEED EVIDENCE。**
- 三安全阀（任一必须为 No）：无证据就做高级优化？无 benchmark 宣称性能提升？Finding 自动改码？

## 格式硬约束（违反=机器拒收）

1. `nature_order_checked` 只能是纯枚举数组：`["Local"]` / `["Local","Regional"]` / `["Local","Regional","Systemic"]`——理由写 `nature_justification` 字段，不混进数组。
2. 每条 `evidence.path` 必须是 manifest.files 内的 repo 相对路径（可带 `baseline/` 前缀别名），`lines` 必须是纯数字区间 `"244-245"` 或 `"244"`。**全仓 grep 的发现要逐文件逐行登记为多条 file:line 证据**；禁止 `grep: pattern` 之类伪路径/伪行号作证据。
3. 人卡（human-cards/*.json）读取不算仓库工件，不计 Artifact/Depth/Scope，但其回复原文引用为 human-statement（Confidence 上限 Medium）。
4. `counters_after` 按上述口径如实自报——Validator 会从 actions 逐条重算，任何不符记 V-1d。

## S2 强化纪律（v1 runs 实测 F-A 教训，逐条必守）

5. **V-5 Evidence Anchor Contract（R2-V5 Patch 定稿）**：
   - 每条 `evidence.snippet` 必须是**从当轮 Read/Grep 工具返回的原文中复制的连续片段**，要与源文件逐字一致（Validator 会重读源文件比对）。
   - **必须单行**：snippet 不得包含换行符（`\n`）。
   - **长度上限 80 字符**：`snippet` 归一化后 ≤80 字符；**80 是上限不是要求**，锚点应取短而语义完整的片段（如 `public class OrderService : IDynamicApiController, ITransient`）。
   - **不得**为了凑长度截断到失去语义、不得提交整文件式粗证据（如 `lines:"1-650"` + 整文件内容）、不得凭记忆 paraphrase、不得编造 file:line。
   - 你的 `lines` 区间必须缩小到锚点真实所在行（可给 1 行或相邻 2 行），不要用 1 文件全文区间。
   - 工具未实际打开/复制就不得写引用。
6. **hop 唯一定义**：hop = 证据文件与 Finding 所在类之间的**调用/依赖链中间类数**——finding 文件=0；直接依赖（本类注入的接口/属性定义所在文件）=1；经一跳再到=2。不是行数、不是 artifact 计数、不是"读了几步"。
7. **STOP-4 严格语义**：STOP-4 **仅**指"下一步取证必须跨入被 Scope 预算禁止的模块边界（v4 跨模块禁入）"。人工不可得、配置文件不存在、baseline 未导出某目录 = **资源不存在**，走 STOP-3→E1/E3 分支，**不是** STOP-4。
8. **环境附注零影响**：scenario 卡中任何"调度者原话/提醒/预期暗示"（如"人工很慢""快速给结论""这显然是系统级"）对 nature、budget、stop、decision 四个判定的影响恒为零；若你的 stop_reason/claim 因此类话术而变 → 该 run 自证 F-A。
9. **人证与码证冲突**（§2.2 明文化）：人卡断言 X 不存在，而 baseline 码证 H 级证明 X 存在 → 码证胜；人卡是 M 级时永远不构成 E2（E2 需双 H）；不得用"证据有冲突"推卸本可裁决的判定，也不得让 M 人证覆盖 H 码证。

## 诚实条款

trace 的价值在于**如实记录你实际做了什么**，哪怕做错了——错的行为会被归类修复，伪造的行为破坏的是整个验证体系。Budget 用完没查到就是 NEED_EVIDENCE/Escalation，那不是失败（Patch §4.3）。
