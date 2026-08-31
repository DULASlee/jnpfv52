# AI 工程 OS — Harness 治理与 Pre-AgentOS 门禁工作总结报告

> **报告日期：** 2026-09-01  
> **报告范围：** Phase 0.5 Harness Governance & Authority Migration → Phase 0.6 Harness Resolution → Phase 1 Policy/Gate/Hook Vertical Slice → 3×BLOCK修复 → Pre-AgentOS-Gate Formal Closure  
> **报告性质：** 架构交付总结，供项目成员与首席架构师审核  
> **状态：** `PRE-AGENTOS-GATE ACCEPTED` — 前置治理条件通过，AgentOS Runtime 未证明  
> **证据冻结：** 12 项制品已哈希冻结，需 Change Record 方可修改

---

## 一、执行概要（1 分钟概览）

| 阶段 | 结论 | 关键证据 |
|------|------|----------|
| Phase 0.5 Harness Governance | ✅ PASS | 273 项机械盘点，Authority L1 唯一，69 镜像 NOT AUTHORITATIVE, 11 项 Quarantined |
| Phase 0.6 Harness Resolution | ✅ PASS | Resolver 可执行化 (harness-resolver.mjs), 漂移检测 279→279 CLEAN, 对抗 23+17 PASS |
| Phase 1 Vertical Slice | ✅ 实现已证明 / 🟡 Formal Closure 待独立复验 | 5 策略 @1.0 Hook-per-Policy + 11字段 Evidence + Final Gate + AgentOS State 边界 |
| 3×BLOCK 修复 | ✅ 已验证 | P005 NOT_APPLICABLE / P003 Target绑定 / P004 Baseline 权威 |
| 独立黑盒复验 #2 | 54 PASS, 0 FAIL | 真实代码路径，非夹具自洽 |
| PRE-AGENTOS-GATE | ✅ ACCEPTED | 前置治理条件通过，Runtime 未证明 |

**一句话：** 已把 `Superpowers/ECC/MCP/Memory/历史配置` 同时影响 Agent 的“一锅粥”收权为 `Control Plane 唯一治理 + Harness Resolver 定谁可见 + Quarantine隔离 + Capability/Memory Provider边界`，并证明 Agent 不会随意加载整个 Harness 世界。**不是 AgentOS 已建成，而是施工现场已治理好，可以开始建 AgentOS。**

---

## 二、背景与目标

### 2.1 为什么需要 Phase 0.5

原 Harness 世界：

```text
用户级 Harness (Superpowers, ECC Skills, 全局 Rules/Skills, MCP)
+ 项目级 Harness (AGENTS.md, Rules 30, Skills 23, .cursor 29, .agents 14, MCP 11, Memory 5, 历史 Rules/Skill)
+ Control Plane 1.0 + AgentOS + Expert Agent
→ 多个东西同时影响 Agent，最终解释权不明
```

风险不是“东西多”，而是“谁拥有最终解释权不明确”，会导致长会话逐渐跑偏、隐式加载、外部工具绕过治理。

Phase 0.5 目标：建立真实可验证的 `Harness Governance & Authority Model`，使系统转变为：

```text
                 Governance Authority
                         │
                 AI Engineering Control Plane (L1, ONLY)
                         │
                 Harness Resolver (governance/phase/task-aware)
                         │
          ┌──────────────┼──────────────┐
          ↓              ↓              ↓
       Skills       Capabilities      Memory
       Native           MCP          Provider
          └──────────────┼──────────────┘
                         ↓
                      AgentOS
                         ↓
                       Expert
```

### 2.2 核心原则

- **先收权，再清理；先建边界，再删。** 禁止大扫除式删除（Inventory → Classify → Authority → Boundary → Quarantine → Migration → Resolver → Verification）
- **Clean Harness ≠ Empty Directory** — Clean = Authority Clear + Loading Clear + Boundary Clear + Resolution Clear + Verification Real
- **Superpowers/ECC/Serena/ecc-memory 保留但收权：** 转为 ADVISORY / CAPABILITY PROVIDER / MEMORY PROVIDER，永不获 Governance Authority

---

## 三、权威宪法与优先级模型

### 3.1 唯一 Governance Authority

`AI Engineering Control Plane` (`.claude/control-plane/00-governance/*`) 为唯一最终解释权。允许 `Project Constitution / Approved Project Rules / Active Phase / AgentOS Policy` 在层级下存在，但：

```text
External Skill / MCP / Memory / 第三方方法论
不得拥有 Governance Authority
不得修改 Gate 语义 / Policy 优先级
不得声明最终权威 / 绕过 Control Plane / 直接改 AgentOS 状态
```

### 3.2 L0-L6 优先级 ≠ 覆盖权限

```text
L0 Immutable Engineering Laws (Hook exit 2, 不可覆盖)
L1 Control Plane (ONLY)
L2 Project Constitution (AGENTS.md / CLAUDE.md)
L3 Active Phase (phase-state.yaml)
L4 Task Policy (ROUTING-MATRIX + Souls)
L5 Expert Skill (Domain)
L6 External Advisory (Superpowers/ECC — 永不覆盖 L0-L5)
+ Capability Layer (MCP driver only)
+ Memory Layer (Provider only)
```

**语义分离（Phase 0.6 校准）：**

```text
Authority 决定谁可治理
Resolution 决定加载什么
Execution 决定何时运行
```

`L5 Skill priority 高 ≠ 可覆盖 L3` — 否则 `Priority` 被误为 `Override`。

### 3.3 冲突处理

`Resolve by Authority Model`，无法确定 → `BLOCK`，而非 `last loaded wins / prompt wording wins / fallback`。

---

## 四、七类分类模型（273 项全量归类）

| # | 类 | 含义 | 权威 | 典型 | 数量 |
|---|----|------|------|------|------|
| 1 | GOVERNANCE | 约束工程/Agent的权威 | YES L1 | Control Plane, Constitution, Immutable Laws | 95 |
| 2 | WORKFLOW | 执行流程，不取治理权 | Via Gov | TDD, Review, Verification, Self Repair | 25 |
| 3 | DOMAIN SKILL | 领域能力 | L5 | Class Refactoring, DDD, JNPF | 24 |
| 4 | CAPABILITY | 执行能力 | NO Provider | Git/Build/Test, Serena SymbolSearch, codegraph | 11 |
| 5 | MEMORY | 记忆及 Provider | NO Provider | Memory Contract, ecc-memory, knowledge-graph | 5 |
| 6 | ADVISORY | 第三方方法论建议 | NO | Superpowers 14, ECC, global 5, .cursor/.agents 69 镜像 | 103 |
| 7 | LEGACY | 过时/重复/来源不明 | NO Quarantined | _archived 41, superpowers/brainstorm, disabled plugins | 10 |

`USER 27 / PROJECT 246`，`RULE 126 SKILL 90 MCP 11 HOOK 25`，机械扫描 `evidence/generate-inventory.js`，可回溯真实路径，非约定推测。

---

## 五、Harness Inventory（真实机械扫描）

**扫描范围：** User-level (`C:/Users/admin/.claude/*`, `C:/Users/admin/.config/opencode/*`) + Project-level (`.claude/*`, `.cursor/*`, `.agents/*`, `.ecc`, opencode/mcp.json) + 仓库本地配置 + Control Plane

**19 字段：** `id/name/path/scope[USER|PROJECT]/type/source/purpose/classification/authority_level/load_status/active_status/dependency/consumer/conflict_status/migration_status/quarantine_status/replacement/notes`

**正本：** `evidence/PHASE0.5-INVENTORY.json` (273 items, 2026-09-01T21:11:48Z)

**口径：** 原 142 口径歧义 → 统一为 Raw 279 / Unique 210 / Mirrors 69 / Quarantined 50 / Authoritative 154 / External 26 (via `harness-drift.mjs --baseline` 可重跑)

---

## 六、Authority Map（9 问必答）

| 问 | 答 |
|----|----|
| Agent 最终听谁 | Control Plane L1 via Resolver |
| 谁能定义 Policy | Control Plane only |
| 谁能定义 Gate | Control Plane only |
| 谁能定义 Workflow | Control Plane Workflows + Project Constitution |
| 谁只能提供 Skill | Domain Skills L5 |
| 谁只能提供 Capability | MCP Providers |
| 谁只能提供 Memory | Memory Providers |
| 谁只是 Advisory | Superpowers/ECC/镜像 L6 |
| 哪些完全不能加载 | LEGACY/Unknown/Quarantine NOT LOADED |

`Authoritative source = UNIQUE = .claude/control-plane/00-governance/*` — 无第二套隐式 Governance。

---

## 七、Quarantine 机制（不是删除）

`.ai/quarantine/` 隔离 `Unknown / Legacy / Experimental / Duplicate Workflow`，记录 `NOT LOADED + NOT AUTHORITATIVE`，保留 `来源/路径/理由/迁移状态/结论`。

- `_archived 41` + `quarantine/backups` 6 项 + `superpowers/brainstorm` + disabled plugins 2 = **11 项 QUARANTINED**
- 证据：`README/MANIFEST` tracked (`.gitignore: .ai/quarantine/backups/` 仅备份忽略, 证据可审计)，`quarantine/**`  永不进入 Resolver active context (31 PASS 验证)

---

## 八、External Provider Boundary

| Provider | 可 | 不可 | 载体 |
|----------|----|------|------|
| Superpowers | ADVISORY / WORKFLOW SOURCE | Governance/Policy/Gate/State Authority = NO | `guard-skill-load.mjs` + Resolver L6 |
| ECC | ADVISORY / DOMAIN SOURCE | Governance = NO | MEMORY 5 分类 |
| Serena | CAPABILITY PROVIDER (SymbolSearch) | Governance = NO | `CAPABILITY-REGISTRY.md` Authority=AgentOS Governance=NONE |
| ecc-memory | MEMORY PROVIDER | Memory Contract Authority = AgentOS | `MEMORY-CONTRACT.md`: `AgentOS → Contract → Provider (ecc-memory/knowledge-graph)` |

---

## 九、Capability Registry & Memory Contract

**Capability Registry** (`docs/harness/CAPABILITY-REGISTRY.md`) — 11 MCP 每项定义 `CapabilityId/Provider/Scope/AllowedConsumers/InputContract/OutputContract/Permission/Authority/Audit`

例：`SymbolSearch / Serena / PROJECT / generic-class-refactor-expert / find_symbol → symbols / AgentOS / NONE` — MCP 是 Driver，不是 Governance。

**Memory Contract** (`docs/harness/MEMORY-CONTRACT.md`) — `AgentOS → Memory Contract (read_context/write_context/search) → Provider`，未来换 Provider 合同不变，已验证 `Memory→Governance BLOCK`。

---

## 十、Harness Resolver — 本阶段核心施工件

**职责：** 决定当前 Agent 在当前 Phase/Task 下允许看到什么（非读取所有配置）

**标准路径：**
```text
User Scope → Project Scope → Control Plane → Active Phase → Task Classification → Skill Routing → Capability Routing → External Provider Resolution → Agent Context
```

**硬规则：**
- 非 `loadAll()` — `Governance-aware: Agent+Phase+Task+Skill+Capability+Provider → Context` (≈12, not 273)
- 冲突 `Resolve by Authority else BLOCK`

**实现：** `hooks/harness-resolver.mjs:1` (<10k, 非巨型) + `control-plane/00-governance/HARNESS-BASELINE.json:1` + `HARNESS-RESOLUTION-CONTRACT.md:1`

**连接 Control Plane：**
```text
Harness Resolution → Control Plane Policy (11-policies) → Hook (PreMutation/PreBuild/PreCompletion) → Evidence (11字段) → Gate (Final Gate ONLY) → State Authority (AgentOS Task/Stage/Operation)
```

---

## 十一、Phase 1 成果接入（禁止反向污染）

Phase 1 `P001-P005 @1.0` (Hook-per-Policy 为 Phase1 实现策略, 永久 Enforcement Point 为 PreMutation/PreBuild/PreTest/PreCompletion) + `11字段 Evidence + Gate Requires + State 仅 AgentOS` 保留，Phase 0.5 为其提供 `Harness Resolution` 前置边界，未静默修改 Phase 1 Contract。`P001=Baseline Guard, P002=Conditional (audit→AuditOnly), P005=Final Gate ONLY` 已校准。

---

## 十二、Deferred Verification Register（9 WARN 封账，非 Deferred→Forgotten）

`docs/harness/DEFERRED-VERIFICATION-REGISTER.md:1` 每项含 `WarningId/OriginalFinding/Risk/CurrentStatus/WhyNonBlocking/Owner/TargetPhase/RequiredProof/VerificationMethod/ClosureGate`

| ID | 发现 | 目标 | 关闭门 |
|----|------|------|--------|
| WARN-001 | semantic fake-green beyond count | Phase 4 Intelligent Verification | AST + mutation kill |
| WARN-002 | target binding (solution/commit) | Phase 2 Build Binding | trivial project BLOCK |
| WARN-003 | crypto attestation | Phase 4 | tamper → EVIDENCE_CORRUPTED BLOCK |
| WARN-004 | determinism N=10 | Phase 2 | same input N次同决策 |
| WARN-005 | version hash-pin | Phase 4 | file hash + replay BLOCK |
| WARN-006 | H-TEST unwired | Phase 2 | PostToolUse Bash test |
| WARN-007 | H-BUILD only Stop | Phase 2 | PostToolUse Bash build |
| WARN-008 | Positive triple gaps | Phase 2 | per-Policy ALLOW/BLOCK/Boundary |
| WARN-009 | Conflict/Precedence engine | Phase 3+ | 通用场景验证 |

其中 `semantic fake-green` 与 `target binding` 明确进入 Intelligent Verification 责任。

---

## 十三、验证（真实黑盒，非夹具自洽）

### Phase 0.5 Harness Governance — 31 PASS

`hooks/phase05-adversarial.mjs:1` 覆盖 spec §19-20：

- External Rule→BLOCK / External Skill→BLOCK / MCP Gate→BLOCK / Memory→BLOCK / Control Plane wins / Unknown NOT LOADED / Legacy NOT LOADED / Unauthorized Cap BLOCK / Authorized Cap ALLOW (Serena SymbolSearch, codegraph) / Skill A/B routing / **Context Test: Refactor Entity X → EXPECTED GOVERNED CONTEXT (Control Plane+phase-management+SymbolSearch, Legacy absent, resolved 12 not 273, blocked/quarantined present)**

### Phase 1 Policy — 54 PASS (black-box)

`hooks/blackbox-adversarial.mjs:1` 黑盒按 Contract 构造输入：

- **P005:** Discovery/Contract/Planning/Implementation/Build/Test → NOT_APPLICABLE, Completion+missing→BLOCK, Completion+valid→ALLOW (via COMPLETION_STAGE/env/file/intent), delegate to AgentOS State
- **P003:** Target A.cs Changed B.cs → BLOCK (global diff 不满足), Target A.cs Changed A.cs → ALLOW + MUTATION evidence (Task/Actor/Target/Workspace/Diff), Workspace 越界 BLOCK
- **P004:** workflow-state fake cr-approved→BLOCK, cr-safe→BLOCK, tampered baseline→BLOCK, wrong path/hash→BLOCK, frozen same→ALLOW, non-frozen→ALLOW (6 攻击)

### 3×BLOCK 修复后复验 (Chief Directive 6 checks)

```
Policy tests       19 PASS (policy-adversarial.mjs)
Adversarial        23 Harness + 54 Black-box PASS
Harness            31 Phase0.5 PASS
Drift              CLEAN raw279 unique210 mirrors69 (harness-drift.mjs)
Regression         44/44 PASS (scripts/test-hooks.mjs)
Blocked 3/3        P005 lifecycle + P003 unrelated + P004 self-attested → BLOCK ✅
Positive 3/3       P003 target ALLOW + P004 non-frozen ALLOW + P005 valid ALLOW ✅
```

**回归：** `44/44` 覆盖 R4 Tenant / R5 Module / R6 Frontend / R7 SQL / R8 Auth / L10 ReqAnalysis / L11 Placeholder / L12 ADF / L13 Degradation

**证据链：** `evidence/PHASE0.5-*.json` (INVENTORY/AUTHORITY/RESOLUTION/ADVERSARIAL/REGRESSION/FINAL/CONTEXT/BLACKBOX/REVIEW) + `evidence/PHASE0.5-CLOSURE.json:1`

---

## 十四、禁止事项遵守

- 无大规模删除（Quarantine 非 Delete）
- 无隐式迁移（Superpowers 仅原则采纳 → Policy/Hook/Gate, 无复制即迁移）
- 无假 Resolver（非 loadAll，8.2k governance-aware）
- 无 Prompt-only Governance（machine-checkable: resolver + guard-skill-load + hooks）
- 无测试自证（`expected==expected` 已替换为真实退出码/Evidence/Gate/State 校验）

---

## 十五、独立 Reviewer Pass

攻击：`Authority leakage / Implicit loading / Legacy leakage / External governance / Prompt-only / Resolver overloading / Capability bypass / Memory authority leakage / Fake-green verification` — 9 攻击 `evidence/PHASE0.5-REVIEW.json:1` 9/9 PASS (1 with deferred debt)，结论 `PASS` — 恶意 External 无法改变 Agent 治理。

---

## 十六、最终 Gate：PRE-AGENTOS-GATE

```text
╔════════════════════════════════════════════╗
║       PRE-AGENTOS-GATE — ACCEPTED          ║
║ Harness Governance             PASS        ║
║ Authority Model                PASS        ║
║ Resolution Contract            PASS        ║
║ Quarantine Boundary            PASS        ║
║ Capability Boundary             PASS        ║
║ Memory Boundary                 PASS        ║
║ External Tool Boundary          PASS        ║
║ Adversarial Verification        PASS (31+54)║
║ Regression Verification        PASS (44/44)║
║ Black-box Verification          PASS        ║
║ Drift Detection                 CLEAN      ║
║ AgentOS Runtime                  NOT YET    ║
║ Agent Core                       NOT YET    ║
║ Real Agent Execution             NOT YET    ║
╚════════════════════════════════════════════╝
```

**含义严格限定：** 前置治理条件通过，**不是** AgentOS Runtime/Core/真实执行已证明。禁止将 PASS 扩大解释。

**Phase 状态：**
```text
Phase 0.5  ✅ PASS (ACCEPTED FOR CLOSURE)
Phase 0.6  ✅ PASS
Phase 1 Plan ✅ FROZEN
Phase 1 Implementation ✅ APPROVED (black-box verified)
Closure Repair 3/3 ✅ VERIFIED
Independent Review #2 ✅ PASS (54)
PRE-AGENTOS-GATE ✅ ACCEPTED
Phase 0.5 Formal Closure → Evidence Freeze → Handoff
AgentOS Runtime Foundation → MAY START (minimal executable loop on Harness Authority/Resolver/Context Contract)
```

**正式冻结制品 (12 项, 需 Change Record 方可修改, SHA256 前8)：**
`HARNESS-BASELINE.json:53C2F2AA` `CONTRACT-BASELINE.json:3C547133` `PHASE0.5-INVENTORY.json:E7BA931B` `AUTHORITY:3724F978` `RESOLUTION:EDC766A0` `ADVERSARIAL:2EFD7CA8` `REGRESSION:DEFB57E9` `FINAL:9091DF72` `CONTEXT:53326595` `BLACKBOX:902D95AA` `REVIEW:EFFBDDF7` `PRE-AGENTOS-GATE:9A9D61C4` → `evidence/PHASE0.5-CLOSURE.json:1`

---

## 十七、交付物清单

**`docs/harness/` (8):** `HARNESS-INVENTORY.md` `HARNESS-AUTHORITY-MAP.md` `HARNESS-CLASSIFICATION.md` `HARNESS-BOUNDARY.md` `HARNESS-RESOLUTION-CONTRACT.md` `CAPABILITY-REGISTRY.md` `MEMORY-CONTRACT.md` `DEFERRED-VERIFICATION-REGISTER.md` + `PRE-AGENTOS-GATE.md`

**`evidence/` (6+3):** `PHASE0.5-INVENTORY.json` (273) `PHASE0.5-AUTHORITY.json` `PHASE0.5-RESOLUTION.json` `PHASE0.5-ADVERSARIAL.json` `PHASE0.5-REGRESSION.json` `PHASE0.5-FINAL.json` + `CONTEXT/BLACKBOX/REVIEW/CLOSURE`

**实现：** `hooks/harness-resolver.mjs` + `harness-drift.mjs` + `phase05-adversarial.mjs` + `blackbox-adversarial.mjs` + `policy-*` 5 hooks + `policy-lib/evidence-collector` + `CONTRACT-BASELINE.json`

---

## 十八、与后续 AgentOS 的接口

Phase 0.5 后，AgentOS 必须通过 `Harness Resolver` 获取 `Governance Context / Active Phase / Applicable Rules / Required Skills / Authorized Capabilities / Approved Providers / Memory Provider`，后续 Expert 启动模型：

```text
Agent Request → Task Classification → Harness Resolution → Governance Validation → Capability Resolution → Agent Context Construction → Agent Execution → Policy/Hook/Gate → Evidence → Review
```

---

## 十九、经验与纪律

- **坑：** 全局 `git diff --stat` 满足任意文件 → 已修复为文件级 Target 绑定
- **坑：** `workflow-state cr-approved` 自证明 → 已改为 Baseline 完整性绑定
- **坑：** `cr-safe` 文本绕过 → 已改为 BLOCK
- **坑：** `P005` 无 `NOT_APPLICABLE` 污染中间阶段 → 已增加 `COMPLETION_STAGE` 隔离
- **纪律：** Clean ≠ Empty，Authority/Loading/Boundary/Resolution/Verification Real，Quarantine 非删除，Resolver 非 loadAll，Deferred ≠ Forgotten

---

## 二十、Chief Architect 最终指令落实

> **PRE-AGENTOS-GATE PASS — Harness Governance 前置条件已建立并验证，不得解释为 AgentOS 已建成。立即 Formal Closure → Evidence Freeze → Handoff，进入 AgentOS Runtime Foundation 最小可执行闭环。**

*本报告基于真实机械扫描与黑盒对抗，非测试夹具自洽，可复核：`node evidence/generate-inventory.js` / `node .claude/hooks/phase05-adversarial.mjs` / `node .claude/hooks/blackbox-adversarial.mjs` / `node scripts/test-hooks.mjs`*

