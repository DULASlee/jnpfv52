# 需求分析子链铁律（Requirement Analysis Iron Law）

> **定位：** JNPF v5.2 项目**宪法级**铁律，与实现完整性 / 全链条冲刺 / 三元组 / Business First 铁律并列，凌驾于架构红线之上。
> **诞生背景：** 2026-07-12 — AI 反复偏离开发方案；2026-07-17 切换为阶段 A-B-C，旧 25–33 废止归档。
> **生效日期：** 2026-07-12 · **永久生效，无例外，无豁免，无时效**
> **Cursor 镜像：** `.cursor/rules/req-analysis-iron-law.mdc`（alwaysApply: true）

---

## 核心宣言

**一切编码以阶段 A-B-C 为唯一施工依据；旧 25–33 号已废止（见 `docs/AI原生开发/旧方案归档（弃用）/`）。**

需求分析子链是全链条的起点，其正确性决定下游所有阶段。AI MUST 按子链逐阶段推进，按功能点验收，禁止擅自修改关键业务方法，禁止用 mjs 脚本替代 xUnit。

**未经变更申请（CR）擅自修改关键业务方法 = 最严重的越权行为。**

---

## 七条禁令（违反任一 = 立即停工）

> 标注 ✅ = 机器可检测（guard-write.mjs L10 / JNPF.Analyzers / xUnit 守卫）
> 标注 📋 = 流程约束（节点审批 + 人审）

### 禁令一：禁止新增 .mjs 脚本做 E2E/冒烟测试 ✅

**铁律：**
- **全面禁止新增 `.mjs` 文件**，除 `.claude/hooks/` 和 `.cursor/hooks/`（hook 基础设施）外。
- 现有 `scripts/*.mjs` 和 `tests/api/*.mjs` **冻结**，逐步迁移到 xUnit（后端）或 Vitest `.ts`（前端）。
- 新测试一律用：后端 `dotnet test`（xUnit，`backend/tests/`）；前端 `pnpm test:unit`（Vitest `.ts`）。
- **判断标准：** 新建文件的扩展名是 `.mjs` 且不在 hooks 目录 → 违规。

### 禁令二：严格保证各子链数据一致性 ✅

**铁律：**
- IR 事件流（`ai_ir_events`）= Write Model（唯一写入真理）。
- `ai_entity_field` = 字段消费唯一源；下游（Architect/Db/Ui/Developer/Tester）**禁止** parse IR JSON 当字段源。
- `sa_*` 九表 / `sa_quality_score` / `sa_assumptions` / `sa_consistency` = 投影，禁止后阶段手改补洞。
- 三元组 `(tenantId, projectId, pipelineId)` MUST 完整、独立、可分离。
- **判断标准：** 同一关注点存在两个互不等价的业务数据源 → 违规（禁令二第二源）。

### 禁令三：必须按子链逐阶段推进 ✅

**铁律：**
- 强制顺序：`门控(SG0) → 需求分析(SG1-2) → 架构设计(SG3) → 总体设计(SG4) → 开发(SG5) → 测试(SG6) → debug修复 → 沙箱部署(SG7)`。
- **禁止跳过任一 SG**；未通过当前 SG，不得进入下一阶段施工或宣称完成。
- 全链冒烟测试（W3）**置后**——所有 SG + 所有功能点验收完成后才允许联调。
- **判断标准：** 当前 SG 的功能点未全部验收 → 禁止跑下一 SG 或全链。

### 禁令四：必须以阶段 A-B-C 为唯一计划编码 📋

**铁律：**
- [`1、阶段A.md`](../../docs/AI原生开发/1、多用户多任务并行/1、阶段A.md) — PM 三方法 + DTO
- [`2、阶段B.md`](../../docs/AI原生开发/1、多用户多任务并行/2、阶段B.md) — 4 步编排器 + 门控
- [`3、阶段C.md`](../../docs/AI原生开发/1、多用户多任务并行/3、阶段C.md) — 端到端 + 真流式 + 删旧流程
- 旧 25–33 号 → [`旧方案归档（弃用）`](../../docs/AI原生开发/旧方案归档（弃用）/README.md)，**禁止**再作施工或验收依据
- 编码 MUST 对照阶段 A/B/C 对应章节；偏离 = 先修订现行文档再改代码
- **判断标准：** 能否指出本次修改对照阶段 A/B/C 哪一节？指不出 → 停工先读文档

### 禁令五：以功能点验收为标准，逐点验收到沙箱部署 ✅

**铁律：**
- 从门控到沙箱部署的**每一个功能点**都必须逐个验收，验收完成才能进入下一功能点。
- 一个功能点完成 = **三者齐全**：
  1. **xUnit 守卫测试绿**（确定性核心有对应单测，`dotnet test` 通过）
  2. **业务证据**（IR 事件 / 数据快照 / 产物内容摘录，非"测试通过"一句话）
  3. **用户审批**（用户明确"通过/继续"，沉默 ≠ 审批）
- 全部功能点（门控→沙箱）验收完成后，**才允许**联调测试（W3 全链冒烟）。
- **判断标准：** 说"这个功能点完成了"时，xUnit 路径、业务证据、审批记录三者在哪？缺一 → 未完成。

### 禁令六：禁止擅自修改关键业务方法 ✅

**铁律：**
- 修改下方"关键业务方法保护清单"中的任一方法前，**MUST 先提交变更申请（CR）**。
- CR 流程：在 `.claude/change-requests/` 写 CR 文件（目标方法、修改原因、对照决策、影响评估）→ 提交用户审批 → 批准后在 `workflow-state.json` 标记 `cr-approved`。
- **未经 CR 审批修改关键方法 = 越权**，guard-write.mjs L10 会阻断。
- 纯格式/注释修改用 `// cr-safe: <理由>` 行内豁免。
- **判断标准：** 我要改的这个方法在保护清单里吗？在 → 先写 CR。

### 禁令七：禁止复活 25 号废止清单中的模块 ✅

**铁律：**
- 以下已被 25 号 v2.1 明确废止，**禁止再实现**：
  - `ScannerValidator` 独立模块 + `sa_scanner_validation` 表
  - 确定性出题引擎 + 固定 Q1–Q9
  - `cascadeUpdate` + `sa_event_dependencies` + BFS 增量依赖图
  - 5 张 `sa_ddd_*` 表 + 5 个 DDD 增强器
  - 编排器 `_llm.ChatAsync` 冒充 PM 出题
  - 普通 `SINGLE` 澄清题（非矩阵行内）
  - 每轮投影 + 每轮 Materializer（仅 Round 3）
- **判断标准：** 新代码/新表名命中上述任一 → 违规。

---

## 关键业务方法保护清单（禁令六的对象）

> 来源：阶段 A-B-C 关键路径 + 保护清单源码锚点。
> 路径前缀：`backend/modularity/inteAssistant/JNPF.InteAssistant/`

| 文件 | 受保护方法 | 保护要点 | 来源 |
|------|-----------|----------|------|
| `Skills/PmSkillService.cs` | `GenerateClarificationAsync` / `ReviewSpecAsync` / `AmendProposeAsync` / `ApplyAmendmentAsync` | 出题/终评/Amend 是 PM 专家闭环核心；31 §4.1 契约签名 | 31 §4.1 |
| `Skills/RequirementAnalysisOrchestrator.cs` | `RunAsync` / `GenerateRoundClarificationAsync` | 编排器只编排，禁止 `_llm.ChatAsync` 代问 | 31 P0.2 |
| `Skills/AnalystSkillService.cs` | `FinalizeAsync` | 步骤⑤工程保障（投影+门禁+物化+渲染） | 阶段 C §改动6 |
| `Skills/SkillsApiService.cs` | `ConfirmRequirementSpecAsync` | confirm 校验 `pm≥85 ∨ force` | 31 P0.5 |
| `Skills/DesignSkillOrchestrator.cs` | `GetStatusAsync` | `canRunDesign` 含 pm 门禁 | 31 §2 |
| `Gates/AnalysisFinalizedGate.cs` | Finalize 门禁 | 禁止开逃逸通道 | 25 §3.1 |
| `Gates/QualityScoreCalculator.cs` | 工程质量分 | 工程分 ≠ PM 业务分 | 25 决策9 |
| `Gates/ConsistencyChecker.cs` | 一致性检查 | Round3 一次性执行 | 28 §5 |

---

## 变更申请流程（CR — Change Request）

```
1. AI 识别需要修改保护清单中的方法
2. 在 .claude/change-requests/ 新建 CR-{YYYYMMDD}-{NN}.md
   内容：目标方法 | 修改原因 | 对照哪条决策 | 影响评估 | 回滚方案
3. 向用户提交 CR，等待审批
4. 用户批准 → 在 .claude/workflow-state.json 写入 "cr-approved": "CR-XXXXXXXX-NN"
5. guard-write.mjs L10a 检查：关键文件写入时有对应已批准 CR → 放行
6. 无 CR 或 CR 未批准 → exit 2 阻断写入
7. 纯格式/注释修改：写内容含 // cr-safe: <理由> → 豁免放行
```

---

## 节点审批门禁（继承 implementation-integrity + 阶段 C）

每个功能点完成后**必须暂停**，提交：

```
## 功能点：[名称] 验收

### ① 业务实现说明
- 对照 25-32 哪条决策：
- 做了什么：
- 关键代码 file:line：

### ② xUnit 证据
- 测试路径：backend/tests/.../XxxTests.cs
- dotnet test 输出：通过 N/N

### ③ 业务证据
- IR 事件 / 数据快照 / 产物内容摘录：

### ④ 七禁令自检
- 禁令一(mjs)：✓/✗
- 禁令二(数据一致性)：✓/✗
- 禁令三(逐阶段)：✓/✗
- 禁令四(对照文档)：✓/✗
- 禁令五(功能点验收)：✓/✗
- 禁令六(CR审批)：✓/✗ — 本次是否改了保护清单方法？CR 编号？
- 禁令七(废止模块)：✓/✗

### 待审批
```

**未经用户明确"通过/继续/下一步"，不得进入下一功能点。**

---

## 自检触发点

| 时刻 | 检查 |
|------|------|
| **新建 .mjs 文件时** | 为什么不用 xUnit/Vitest .ts？（禁令一） |
| **修改字段读取源时** | 是否引入了第二源？（禁令二） |
| **想跳到下一阶段时** | 当前 SG 功能点全部验收了？（禁令三/五） |
| **动手编码前** | 对照了哪个文档的哪条决策？（禁令四） |
| **想说"功能点完成"时** | xUnit 绿 + 业务证据 + 审批 三者齐全？（禁令五） |
| **修改关键方法前** | 写 CR 了吗？（禁令六） |
| **新建类/表时** | 命中废止清单了吗？（禁令七） |

---

## 与现有铁律的关系

| 现有铁律 | 本铁律补充 |
|----------|-----------|
| 实现完整性铁律 | 补充：关键方法修改需 CR 审批（禁令六）；验收以功能点为单位（禁令五） |
| 全链条冲刺铁律 | 补充：逐 SG 推进的**具体顺序**（禁令三）；mjs 禁令（禁令一） |
| 三元组铁律 R12 | 补充：数据一致性不仅三元组，还有字段唯一源（禁令二） |
| Business First | 补充：以 25 号为总纲（禁令四） |

---

## 关联文档

- `CLAUDE.md` — §需求分析子链铁律摘要（须同步添加）
- `.claude/rules/implementation-integrity-iron-law.md` — 实现完整性（并列宪法级）
- `.claude/rules/fullchain-sprint-iron-law.md` — 全链条冲刺（并列宪法级）
- `docs/AI原生开发/1、多用户多任务并行/1、阶段A.md` — 阶段 A
- `docs/AI原生开发/1、多用户多任务并行/2、阶段B.md` — 阶段 B
- `docs/AI原生开发/1、多用户多任务并行/3、阶段C.md` — 阶段 C
- `docs/AI原生开发/旧方案归档（弃用）/` — 旧 25–33（废止）
- `AGENTS.md` — 需求分析子链铁律摘要（须同步添加）
