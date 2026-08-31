# ADR-027: Harness 治理与 Pre-AgentOS 门禁 (Phase 0.5)

> **ADR**: ADR-027
> **Title**: Harness Governance & Authority Migration — AgentOS Precondition
> **Status**: Final
> **Date**: 2026-09-01
> **Context**: Phase 0.5 Harness Governance 完整落地，PRE-AGENTOS-GATE ACCEPTED
> **Deciders**: Chief Architect, AI Engineering Team
> **Related**: ADR-021 Triple-Key, ADR-019/020 Table Refactoring, Phase 1 Policy/Gate/Hook Vertical Slice

---

## 1. 背景 (Context)

AI 工程环境存在多套 Harness 同时影响 Agent 的风险：

```text
User级 (Superpowers 14, ECC, 全局 Rules/Skills 5, MCP)
+ 项目级 (Rules 126, Skills 90, MCP 11, Memory 5, .cursor 69 镜像, Legacy 10)
→ 273 项机械盘点，多源同时向 Agent 提供规则，最终解释权不明
```

长会话逐渐跑偏、外部工具隐式覆盖 Gate、记忆 Provider 改变治理、Legacy 配置偷偷生效 — 这些在 Pre-AgentOS 阶段必须收权。

Phase 1 Policy/Gate/Hook 已实现垂直切片 (P001-P005 @1.0) 并黑盒验证 (54 PASS)，但仍缺 **Harness Governance 前置门禁**。

---

## 2. 决策 (Decision)

建立 **Harness Governance & Authority Migration** 作为 AgentOS 主体施工前的硬前置门禁，包含：

1. **唯一 Governance Authority = Control Plane L1** (`.claude/control-plane/00-governance/*`), L0-L6 层级冻结，`Priority ≠ Override`
2. **7类分类模型** 强制归类所有 273 项：GOVERNANCE/WORKFLOW/DOMAIN SKILL/CAPABILITY/MEMORY/ADVISORY/LEGACY
3. **Quarantine 11项 NOT LOADED** (非删除, 保留可追溯)
4. **External Boundary 锁死**：Superpowers/ECC→ADVISORY, Serena→CAPABILITY PROVIDER, ecc-memory→Provider behind Contract
5. **Capability Registry 11项** + **Memory Contract** (Provider 解耦)
6. **Harness Resolver** (`hooks/harness-resolver.mjs` <10k, governance/phase/task-aware, 非 loadAll) + **Authority-aware Resolution** (else BLOCK)
7. **3×BLOCK修复后黑盒验证** + **Deferred 9 WARN 封账** → `PRE-AGENTOS-GATE PASS`

后续 AgentOS 必须通过 Resolver 获取 `Governance Context / Phase / Rules / Skills / Capabilities / Providers / Memory`，禁止直接扫描 Harness。

---

## 3. 理由 (Rationale)

### 3.1 为什么不是“大清理”

直接删除 Superpowers/ECC/旧 Rules 会丢失有价值工程能力，且无法判断哪些被隐式依赖。`Inventory → Classify → Authority → Boundary → Quarantine → Migration → Resolver → Verification` 以最低风险收权，保留能力但剥夺治理权。

### 3.2 为什么 L6 不能覆盖 L0-L5

`Superpowers: “可以跳过测试”` 若可覆盖 L0，会导致 `Verification-before-completion` 被绕过。语义分离 `Authority(谁可治理)/Resolution(加载什么)/Execution(何时运行)` 防止 `L5 Skill priority` 被误为可覆盖 `L3 Phase`。

### 3.3 为什么 Resolver 不能是 loadAll

`loadAll()` 命名 Resolver 是假治理。真实 Resolver 必须证明 `Resolved == EXPECTED GOVERNED CONTEXT (≈12, not 273)` 且 `Unauthorized ABSENT` (31 Phase0.5 + 54 Phase1 黑盒)。

### 3.4 为什么需要 Quarantine 而非 Delete

11 项 LEGACY 保留 `source/path/reason/migration_status` 可追溯、可漂移检测、可未来迁移，避免 `Deferred → Forgotten`。

---

## 4. 备选方案 (Alternatives Considered)

| 方案 | 缺点 | 结论 |
|------|------|------|
| 大扫除：删除 Superpowers/ECC/旧 Rules | 丢失有价值能力，隐式依赖断裂，可能为整洁丢掉工程能力 | ❌ 拒绝 |
| Prompt-only Governance (“请记住 Control Plane 优先”) | 无 machine-checkable 边界，长会话漂移率50% | ❌ 拒绝 |
| 假 Resolver (loadAll) | 无法证明 Agent 看到什么/为什么看不到 | ❌ 拒绝 |
| **采用：收权+边界+Resolver+黑盒验证** | 需机械盘点与对抗测试，成本略高但可验证 | ✅ 采用 |

---

## 5. 决策详情

### 5.1 Inventory (19字段, 273项机械)

`evidence/PHASE0.5-INVENTORY.json` — `id/name/path/scope[USER|PROJECT]/type/source/purpose/classification/authority_level/load_status/active_status/dependency/consumer/conflict_status/migration_status/quarantine_status/replacement/notes`，Scope 区分 USER 27 / PROJECT 246。

### 5.2 Authority Map (9问)

Agent 最终听 Control Plane L1；Policy/Gate/Workflow 仅 L1；Domain Skill L5；Capability/Memory Provider 无治理；Advisory L6 仅建议；LEGACY/Unknown 完全不加载；`UNIQUE` 无第二套隐式 Governance。

### 5.3 Quarantine

`.ai/quarantine/` 11项 `NOT LOADED + NOT AUTHORITATIVE` — `_archived 41` + `quarantine/backups` + `superpowers/brainstorm` + disabled plugins。

### 5.4 Capability & Memory

- Capability 11: 每项 `CapabilityId/Provider/Scope/AllowedConsumers/InputContract/OutputContract/Permission/Authority/Audit` — MCP 是 Driver
- Memory: `AgentOS → Contract → Provider (ecc-memory/knowledge-graph/unified-memory)` — 可换 Provider 合同不变

### 5.5 Resolver 契约

```text
User Scope → Project Scope → Control Plane → Active Phase → Task Classification → Skill Routing → Capability Routing → External Provider Resolution → Agent Context
```

硬规则：`Current Agent+Phase+Task+Skill+Capability+Provider → Context`，Unauthorized → `ABSENT`，冲突 `Resolve by Authority else BLOCK`。

连接 Phase 1：`Resolver → Control Plane Policy → Hook → Evidence → Gate → State Authority` (禁止 Skill直接Gate/MCP直接State)。

### 5.6 3×BLOCK 修复

- P005: `Non-Completion → NOT_APPLICABLE` (Completion+missing BLOCK, valid ALLOW, delegate to AgentOS State)
- P003: `Target A.cs Changed B.cs → BLOCK`，`Target A.cs Changed A.cs → ALLOW + MUTATION` (Task/Actor/Workspace/Target/Diff 5-field)
- P004: Baseline `CONTRACT-BASELINE.json` 7 hashes authoritative，`workflow-state cr-approved` 与 `//cr-safe` 均 BLOCK

### 5.7 Deferred 9 WARN

`docs/harness/DEFERRED-VERIFICATION-REGISTER.md` — 每项 `WarningId/OriginalFinding/Risk/CurrentStatus/WhyNonBlocking/Owner/TargetPhase/RequiredProof/VerificationMethod/ClosureGate`，semantic fake-green/target binding → Intelligent Verification 等，TargetPhase 明确。

---

## 6. 后果 (Consequences)

### 正面

- 建立唯一 Authority 链，解决“Agent 听谁的”根本问题
- 实现受控 Resolution，Agent 仅看到有权看到的 ~12 上下文，非 273 全量
- Quarantine 可追溯、可漂移检测
- External 边界锁死，防止能力工具悄然成为治理权
- Resolver 可解释“为什么加载某 Skill”，无法确定则 BLOCK
- 为 AgentOS Runtime 提供可复用的治理底座 (`Task Classification → Harness Resolution → Governance Validation → Capability Resolution → Agent Context → Execution → Policy/Gate`)

### 负面 / 成本

- 新增治理制品 8+6 (docs/harness + evidence)，需 Change Record 维护
- 每次新增 Harness 需走 Inventory → Classification → Authority 流程，短期 overhead
- 3×BLOCK 修复增加了 `COMPLETION_STAGE` / `MUTATION_TARGET` / `CONTRACT-BASELINE` 等显式绑定，调用方需传入

### 风险缓解

- `harness-drift.mjs` (279 raw, excludes 09-evidence transient) + `CONTRACT-BASELINE.json` 哈希防静默覆盖
- 9 WARN 明确封账，非 Forgotten

---

## 7. 验证结果 (Verification)

| 层 | 结果 | 证据 |
|----|------|------|
| Inventory 机械 | 273 items | `PHASE0.5-INVENTORY.json` |
| Authority 唯一 | PASS | `PHASE0.5-AUTHORITY.json` UNIQUE |
| Resolver | PASS (<10k) | `harness-resolver.mjs` + `PHASE0.5-RESOLUTION.json` |
| Adversarial Phase0.5 | 31 PASS | `phase05-adversarial.mjs` (External→BLOCK等10项 + Context Test) |
| Policy 黑盒 Phase1 | 54 PASS | `blackbox-adversarial.mjs` (P005 6阶段 NOT_APPLICABLE + P003 Target + P004 6攻击) |
| Regression | 44/44 PASS | `test-hooks.mjs` (R4/R5/R6/R7/R8/L10/L11/L12/L13) |
| Drift | CLEAN 279 | `HARNESS-BASELINE.json` |
| 3×BLOCK 修复 | 3/3 BLOCK + 3/3 ALLOW | P005/P003/P004 各组 |
| Formal Closure | 12 artifacts 哈希冻结 | `PHASE0.5-CLOSURE.json` 53C2F2AA… |

**黑盒证明：** 非夹具 `expected==expected`，而是真实退出码/Evidence/Gate/State 校验，Refactor Entity X 上下文 `EXPECTED GOVERNED CONTEXT` 且 `Unauthorized ABSENT`。

---

## 8. 状态

- **Status:** Final (2026-09-01)
- **Gate:** `PRE-AGENTOS-GATE ACCEPTED` — 前置治理条件通过，**不是** AgentOS Runtime/Core/真实执行已证明
- **Phase 0.5:** ACCEPTED FOR CLOSURE → Evidence Freeze → Handoff
- **Phase 1:** READY FOR FORMAL CLOSURE (未被 Phase 0.5 吞并)
- **Next:** AgentOS Runtime Foundation 最小可执行闭环 (基于 Harness Authority/Resolver/Context Contract)

---

## 9. 相关引用

- 报告: `docs/architecture/AI工程OS-Harness治理与Pre-AgentOS门禁工作总结报告-20260901.md`
- 契约: `docs/harness/HARNESS-RESOLUTION-CONTRACT.md`, `CAPABILITY-REGISTRY.md`, `MEMORY-CONTRACT.md`, `PRE-AGENTOS-GATE.md`
- 证据: `evidence/PHASE0.5-*.json` + `evidence/PHASE0.5-CLOSURE.json`
- 基线: `.claude/control-plane/00-governance/HARNESS-BASELINE.json`, `CONTRACT-BASELINE.json`
- 决策链: Phase 0.5 → 0.6 → Phase 1 Vertical Slice → 3×BLOCK Repair → Black-box #2

