# Universal .NET Class Refactoring Architect — Design Baseline (v1.0)

**Status**：✅ BASELINE LOCKED（2026-08-30）
**Author**：Chief Architect
**上位**：Generic Class Refactor Expert Skill v6.0；L2 类级螺旋 SOP v2.0
**性质**：本轮设计决策的冻结记录。下一步将基于本基线编写完整设计 Spec。
**目的**：在编写 Spec 前，明确「这个 Agent 是什么、不是什么、要遵守哪些不可违反的原则」，避免在实现细节中走偏。

---

## 0. 摘要（TL;DR）

把项目里已有的 `generic-class-refactor-expert` v6.0 + L2 SOP + Golden Examples，**重构并抽离**为一个**可独立发布**的用户级 Qoder Subagent：

- **正式名称**：**Universal .NET Class Refactoring Architect**（不再叫 "Generic Class Refactor Expert"）
- **形态**：Agent Package（不是单个 Prompt）
- **架构**：Universal Core（与项目无关）+ 可插拔 Project Profile（JNPF 为第一个）
- **运行模式**：三档（Audit / Verify / Execute），默认开启 Audit + Verify，Execute 需显式授权
- **工具权限**：Read / Grep / Glob / Bash（含 `dotnet build` / `dotnet test` / BDN）；**不含 Write / Commit**
- **发布渠道**：GitHub（从第一天就按可发布标准设计）

---

## 1. 名称升级理由（决策记录）

v6.0 沉淀下来的能力，已经远超"refactoring skill"层级：

```
理解 → 建模 → Finding → 风险分级 → 方案 → 最小修改 → 验证 → 回归 → Golden Example → 沉淀
```

这是一个完整的**专家执行协议**，而非简单的"找坏味道-改代码"。名称升级是为了在发布后让外部用户**一眼理解 Agent 的层级与定位**，避免与一般 lint / refactor tool 混淆。

---

## 2. 核心架构：Universal Core + Project Profile（双层）

```
┌─────────────────────────────────────────────────┐
│  Universal .NET Class Refactoring Architect     │
│  (Reasoning / SOP / Gate / Mode)                │
└────────────┬────────────────────────────────────┘
             │
   ┌─────────┼──────────┬──────────────┐
   ▼         ▼          ▼              ▼
Universal  Project   Evidence       Knowledge
Core       Profile   Pack           References
（与项目    （JNPF/   （Golden       （18 个 references
 无关）    ABP/EF/   Examples/      不消失，作为
           ...）    Cases）         知识资产外挂）
```

**Universal Core 不知道以下任何项目特异性词汇**：

```
❌ TenantId / Multi-tenancy (项目实现细节)
❌ SqlSugar / EF Core / Dapper (具体 ORM)
❌ IRepository / DbContext (项目接口)
❌ Foundation.Domain.* / JNPF.* / ABP.* (项目命名空间)
❌ JNPF-specific 测试基线 / 内部文档路径
```

**Project Profile（独立文件）允许出现的**：

```
✅ ORM = SqlSugar 5.x
✅ Repository = IRepository<T>
✅ Multi-tenancy = TenantId (string) in all entities
✅ Convention = PascalCase + lower_snake DB
✅ ...
```

**未来扩展**：JNPF Profile / ABP Profile / EF-Core-only Profile / SqlSugar-Standalone Profile，互不污染。

---

## 3. 三档 Mode（默认分层开启）

| Mode        | 能力                                              | 默认       | 授权方式 |
| ----------- | ------------------------------------------------- | ---------- | -------- |
| **Audit**   | Read / Search / 分析 / 报告                          | ✅ 默认开启 | 自动     |
| **Verify**  | Audit + `dotnet build` / `dotnet test` / BDN 跑验证 | ✅ 默认开启 | 自动     |
| **Execute** | Verify + Write / Diff / Commit                     | ⛔ 默认关闭 | 显式授权（用户在请求中明确写 `EXECUTE MODE` 或 `apply refactor`） |

**为什么默认不开启 Execute**：

1. 第一个版本是**可信专家分析 Agent**，不是自动重构 Agent（让用户先验证 Agent 的判断准确性）
2. Write 权限会引入"幻觉即代码修改"风险，必须人工闸门
3. 经验沉淀（Golden Examples）只能来自人工评审过的修复，而非 Agent 自动 commit

---

## 4. 工具权限清单

```
✅ Read
✅ Grep
✅ Glob
✅ Bash（仅限）
   - dotnet build / dotnet test
   - 静态分析（Roslyn analyzer / SonarQube CLI）
   - BDN（ BenchmarkDotNet）
   - dotnet-counters / dotnet-trace / dotnet-gcmon
   - git read-only 命令（git log / git diff / git show）
   - 文件系统只读操作

❌ Write
❌ Edit / StrReplace
❌ Bash（含写操作的命令）
   - 任何形式的 `git commit` / `git push` / `git checkout` 写操作
   - 任何文件覆盖 / 移动 / 删除
   - 任何安装 / 全局修改命令
```

---

## 5. 知识分层原则（最容易被违反，必须守住）

**Prompt 负责"怎么思考和怎么工作"，Reference 负责"知道什么"。**

```
Agent System Prompt
├── 角色定义 / Workflow / 输出格式
├── Universal Rules（不可违反的硬门）
├── Finding Taxonomy（16 维度）
├── Risk Model
├── Verification Gates
└── Knowledge References（外挂，不进 system prompt）
    ├── Resource Lifetime
    ├── Async / Concurrency
    ├── Exception Semantics
    ├── Performance
    ├── Security
    ├── Observability
    ├── Modern C# (Span/ArrayPool/ValueTask/...)
    ├── ORM Behavior (lookup table)
    ├── Data Volume Sensitivity
    ├── Impact Assessment
    ├── Cross-Class Context (D11)
    └── Golden Examples 1-4
```

**铁律**：精简 Prompt ≠ 删除知识。**18 个 references 全部保留**，但作为**外挂知识资产**而非塞进 system prompt。

---

## 6. Agent Package 形态（待评审候选结构）

```
universal-dotnet-refactor-expert/
├── README.md                # 5 分钟上手
├── LICENSE                  # MIT / Apache 2.0（待定）
├── AGENTS.md                # Agent 自己如何被 Qoder 加载
│
├── agent/
│   ├── system.md            # 主 system prompt
│   ├── protocol.md          # 执行协议（P0→P10）
│   ├── modes/
│   │   ├── audit.md
│   │   ├── verify.md
│   │   └── execute.md
│   └── profiles/
│       ├── universal-dotnet.md
│       ├── jnpf.md
│       └── ...
│
├── knowledge/
│   ├── architecture/
│   ├── performance/
│   ├── concurrency/
│   ├── resources/
│   ├── security/
│   ├── exceptions/
│   └── ...
│
├── golden-examples/
│   ├── exception-preserve.md
│   ├── resource-lifetime-upload.md
│   ├── resource-lifetime-download.md
│   └── unit-of-work-boundary.md
│
├── validators/              # 可选：自带的校验脚本
│
└── docs/
    ├── architecture.md
    ├── methodology.md
    └── contribution.md
```

> ⚠ **这是候选结构，不是冻结结构**。最终结构由下一步"3 个架构方案比较"决定。

---

## 7. 能力不压缩原则（宪法级）

**绝对不能因为精简而压缩类级重构专家的能力**。

本 Agent 必须继承 v6.0 的全量方法论：

- ✅ P0 5 维证据（code/runtime/arch/test/risk）
- ✅ Finding Taxonomy 16 维度（D1-D16）
- ✅ Evidence → Modify / Stop / Need-Evidence 三态门控
- ✅ Performance Change Gate（7Q）
- ✅ ORM Behavior 框架默认行为查询
- ✅ Data Volume Sensitivity 评估
- ✅ Impact Assessment（HARDCODED / REFLECTION 严重度调整）
- ✅ Cross-Class Lifecycle Analysis（D11）
- ✅ Risk Matrix 模板
- ✅ Golden Examples 1-4 作为决策范式
- ✅ Class-Level Convergence Rule（M3）
- ✅ Observability Boundary（无 PII / 高基数 / 租户上下文）
- ✅ Semantic Change Budget（M1）

**任何被砍掉的能力，都必须记录在 `docs/superpowers/specs/2026-08-30-*-trade-off.md` 中并说明理由**。

---

## 8. 不在本轮决策范围（明确划线）

以下议题**不在本设计基线内**，留待后续 Spec 阶段讨论：

1. Universal Core 的具体 System Prompt 文本（属于"3 个方案 + 设计"阶段）
2. Project Profile 的具体内容（如 JNPF Profile 的字段映射）
3. Agent Package 的最终目录结构（待"3 个方案比较"决定）
4. License 选择（MIT vs Apache 2.0 vs 商业）
5. CI/CD 与发布流程
6. Execute Mode 的具体授权协议（如何让用户"明确开启"）
7. 与现有 `.claude/skills/generic-class-refactor-expert` 的最终共存策略（共存 / 取代 / 重写迁移）

---

## 9. 下一步动作（严格分阶段）

按 brainstorming 技能要求：

```
[本轮完成]
   ✅ 设计基线锁定（本文件）

[下一步]
   → 提出 3 个 Agent 封装架构方案
      - 方案 A：Prompt-Centric
      - 方案 B：Knowledge + SOP + Profile
      - 方案 C：Layered Expert Agent
   → 从 9 个维度严格比较：
      1. 通用性
      2. AI 执行可靠性
      3. 上下文效率
      4. 可维护性
      5. Qoder 兼容性
      6. 未来公开发布可行性
      7. JNPF 适配成本
      8. 知识资产复用
      9. 权限隔离
   → 选定最终架构

[之后]
   → 写入完整设计 Spec（含 Profile / Mode / Knowledge / Workflow）
   → 自审 + 交用户审
   → writing-plans 阶段
```

---

## 10. 决策记录表（Decision Log）

| # | 决策 | 状态 | 备注 |
|---|------|------|------|
| D-01 | Agent 名称：Universal .NET Class Refactoring Architect | ✅ LOCKED | 替代 Generic Class Refactor Expert |
| D-02 | 双层架构：Universal Core + Project Profile | ✅ LOCKED | Universal Core 不可知 JNPF |
| D-03 | Universal Core 词汇黑名单（10 个 JNPF 特异项） | ✅ LOCKED | 见 §2 |
| D-04 | 三档 Mode：Audit / Verify / Execute | ✅ LOCKED | 默认 Audit + Verify |
| D-05 | 工具权限：Read/Grep/Glob/Bash（验证用）；无 Write | ✅ LOCKED | 不含 git commit |
| D-06 | 知识分层：Prompt 不吸收 Reference | ✅ LOCKED | 18 个 references 外挂 |
| D-07 | 能力不压缩：v6.0 全量能力继承 | ✅ LOCKED | 任何裁剪需说明 |
| D-08 | Agent Package 形态：候选结构（不冻结） | 🟡 CANDIDATE | 待"3 个方案"决定 |
| D-09 | 发布渠道：GitHub | ✅ LOCKED | 可发布标准设计 |
| D-10 | 首个 Project Profile = JNPF | ✅ LOCKED | 其他 Profile 后续 |
| D-11 | 本轮不做的事（7 项） | ✅ LOCKED | 见 §8 |

---

## 11. 版本历史

| 版本 | 日期       | 变更                          |
| ---- | ---------- | ----------------------------- |
| v1.0 | 2026-08-30 | 设计基线冻结（D-01 ~ D-11） |