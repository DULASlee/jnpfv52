# Phase 1 Policy Vertical Slice — Design (2026-09-01)

> **Status:** Draft → Chief Architect approved 5 policies; Approach A recommended
> **Upstream:** Phase 0.6 PASS — Harness Resolver + Drift + Adversarial 23 PASS
> **Next:** `writing-plans` → `.claude/control-plane/11-policies/` implementation

---

## 1. 需求摘要

- **来源:** Chief Architect Phase 0.6 裁决，Phase 1 仅做 5 条硬策略的 `Policy → Hook → Evidence → Gate → State` 垂直闭环
- **业务价值:** 基建债（标注）— 防止 AI 工程行为“假绿/跳过构建/无证据完成/破坏契约”，首次让 OS **阻止错误行为** 而非仅告知
  - 无直接用户操作，但映射到 `JNPF Build/Test/Completion` 业务保障
  - 验收: 故意削弱断言 → BLOCK；声称 Build 太慢 Skip → BLOCK
- **技术约束:**
  - MUST 复用 Harness Resolver (Authority/Resolution/Execution 分离)
  - MUST NOT 把 `harness-resolver.mjs` 堆成巨型脚本（分层）
  - MUST 保持 Control Plane 1.0 44/44 回归
  - MUST 遵循 `.mjs` 仅 `hooks/` 允许新增（req-analysis 铁律 L10c）
  - Backend `dotnet build`, Frontend `pnpm test:unit` 证据来源不变
- **歧义点:** “Mutation Must Be Evidenced” 的最小证据集是 `Before/After/Diff/Actor/Task` 五元组，需明确由 git diff + hook 采集，非 Agent 自述

---

## 2. 架构目标 — Governance Execution

```
Harness Resolver (0.6)
       ↓
Policy (declarative, 5 items)
       ↓
Policy Evaluator (pure function)
       ↓
Enforcement Hook (PreToolUse/Stop, exit 2 BLOCK)
       ↓
Runtime Evidence (.claude/control-plane/09-evidence/)
       ↓
Gate (GATE-COMPLETION, reads Evidence)
       ↓
AgentOS State Transition (Completion = BLOCK until Evidence complete)
```

已完成 `Governance Input Normalization`，本阶段完成 `Governance Execution`。

---

## 3. 方案对比

### 方案 A — Hook-per-Policy 垂直切片 (推荐)

- **描述:** 每个 Policy 对应一个独立 hook 文件 (+ 共享 `policy-lib.mjs` + `evidence-collector.mjs`), Gate `GATE-COMPLETION.md` 聚合 5 策略结果。5 hooks + 1 gate + 1 lib，无中央引擎。
  - `policy-001-no-fake-green.mjs` (PreToolUse Write/Edit — 检测 `assert weaken / skip test / mock 替代`)
  - `policy-002-real-build.mjs` (Stop — 无 `dotnet build` exit 0 证据则 BLOCK)
  - `policy-003-mutation-evidence.mjs` (PreToolUse Write/Edit — 无 Before/After/Diff 则 BLOCK)
  - `policy-004-contract-preservation.mjs` (PreToolUse Write — Frozen Contract 变更检测)
  - `policy-005-completion-evidence.mjs` (Stop — Build/Test/Review/Evidence 四件套缺一则 BLOCK)
- **优点:** 最小增量、每策略独立可测、排序显式、避免巨型脚本、符合 “先打通 5 条链” 目标
- **缺点:** 策略 >10 时 hook 数量膨胀，排序与依赖需人工维护
- **失效边界:** 当策略数 >10 或需 Structural/Semantic/Authority Drift 时，Hook-per-Policy 维护成本指数增长，需迁移至中央引擎 (方案 B)；若 hook 排序未定义，会出现 “Policy-002 依赖 Build 证据但 Policy-005 先执行” 的时序竞态
- **预估工作量:** 2–3 天
- **红线检查:** R1,R4,R7,R8 不触发；L10c (hooks/ 允许 .mjs) 合规；B0 基建债标注

### 方案 B — 中央 Policy Engine (YAML 驱动)

- **描述:** `policy-engine.mjs` (150 行) 加载 `policies.yaml` (5 策略声明式), 暴露 `evaluate(policyId, context)` 纯函数；hooks 均为 thin wrapper `policy-engine.evaluate(...)`。支持未来 30 策略与 Structural/Semantic Drift。
- **优点:** 声明式、可扩展、单点审计、易加新 Policy
- **缺点:** 为 5 策略过度设计、单点故障、首版复杂度高、易违反 “不要堆成巨型脚本” 警告、测试需 mock engine
- **失效边界:** Engine >250 行或 YAML 需 DSL 表达 “Before/After/Diff” 时，声明式反而比代码难读；调试需穿透 engine 层，定位比独立 hook 慢 2 倍
- **预估工作量:** 4–5 天
- **红线检查:** 同 A，但需额外 L10c 豁免说明 (engine 在 hooks/)

### 方案 C — 不做 / 零代码 (复用现有 hooks)

- **描述:** 不新增 Policy，复用 `guard-write`, `guard-finish`, `placeholder-scan`, `harness-adversarial` 现有能力，靠 code-reviewer 人工检查假绿/跳过构建
- **优点:** 零开发、零风险、不增加 Harness 复杂度
- **缺点:** 无法自动阻止 “削弱断言→假绿”“Build 太慢→Skip” 真实已发生问题，Governance Execution 仍为 0
- **失效边界:** 首个真实假绿事故发生即失效；Chief Architect 已明确要求 5 策略闭环，不做 = Phase 1 未启动
- **预估工作量:** 0 天
- **红线检查:** 无新增

### 推荐方案

- **选择:** 方案 A — Hook-per-Policy 垂直切片
- **理由:** 与 Chief Architect “先打通 5 条链，而非建设 30 个空接口” 完全对齐；最小可验证增量；每条 Policy 可单独做 adversarial 攻击测试 (削弱断言/跳过构建) 直接验证 `BLOCK`，最快证明 OS 具备执行能力；未来 10+ 策略时再演进为 B，演进路径清晰
- **风险:** Hook 排序与证据时效 (Build 证据过期)；`harness-resolver.mjs` 被误堆叠
- **缓解:** 在 `HARNESS-RESOLUTION.yaml` 声明 `resolver 不得堆叠`；Gate 按 `Build → Test → Evidence → Completion` 显式排序；证据带 30min TTL

---

## 4. 详细设计 (方案 A)

### 4.1 目录与文件

```
.claude/
  control-plane/
    11-policies/                      # 新增 Policy 域
      POLICY-001-NO-FAKE-GREEN.md
      POLICY-002-REAL-BUILD.md
      POLICY-003-MUTATION-EVIDENCE.md
      POLICY-004-CONTRACT-PRESERVATION.md
      POLICY-005-COMPLETION-EVIDENCE.md
      POLICIES-INDEX.md               # 5 策略总览 + 垂直切片图
    05-gates/
      GATE-COMPLETION.md              # 升级: 5 策略聚合门
    02-rules/
      POLICY-DEFINITIONS.md           # 机器可读策略定义 (供 hook 读取)
  hooks/
    policy-lib.mjs                    # 共享纯函数 (evaluate, evidence I/O)
    policy-001-no-fake-green.mjs      # PreToolUse Write/Edit
    policy-002-real-build.mjs         # Stop
    policy-003-mutation-evidence.mjs  # PreToolUse Write/Edit
    policy-004-contract-preservation.mjs # PreToolUse Write
    policy-005-completion-evidence.mjs   # Stop
    evidence-collector.mjs            # 追加 .claude/control-plane/09-evidence/
```

### 4.2 策略定义 (Rule → Machine Policy)

| Policy | Trigger | Condition | Enforcement | Evidence |
|--------|---------|-----------|-------------|----------|
| P001 No Fake Green | PreToolUse Write/Edit on `*.cs`, `*.ts`, `*.vue` test | 检测 `Assert.*Equal` 弱化、删除 `test`、新增 `Mock` 替代真实调用、`skip` | BLOCK exit 2 | `evidence/p001-fake-green.json` |
| P002 Real Build | Stop (pre-completion) | `dotnet build` (backend) 或 `pnpm build` (frontend) 无 exit 0 且 TTL <30min | BLOCK | `evidence/build-evidence.json` (exit code, timestamp, log tail) |
| P003 Mutation Evidence | PreToolUse Write/Edit | 写文件但无 Before/After/Diff/Actor/Task 五元组 (git diff + workflow-state) | BLOCK | `evidence/mutation-{hash}.json` |
| P004 Contract Preservation | PreToolUse Write on frozen contract (`08-phase-contracts/*`, `L0-LAWS.md`) | 变更 frozen 文件无 `cr-approved` | BLOCK | `evidence/contract-guard.json` |
| P005 Completion Gate | Stop | Build+Test+Review+Evidence 四件套缺一 | BLOCK | `evidence/completion-gate.json` |

### 4.3 数据流 (以 P001 为例)

```
Agent Edit `FooService.cs` weaken assert
  → hook policy-001-no-fake-green.mjs (PreToolUse)
  → policy-lib.evaluate(P001, { file, oldContent, newContent })
  → detect: assert count ↓ or skip added
  → BLOCK exit 2 + write evidence/p001-fake-green.json
  → AgentOS State: Edit REJECTED
```

### 4.4 钩子注册 (settings.json)

```json
"PreToolUse": [
  { "matcher": "Write|Edit|MultiEdit", "command": "node .claude/hooks/policy-001-no-fake-green.mjs" },
  { "matcher": "Write|Edit|MultiEdit", "command": "node .claude/hooks/policy-003-mutation-evidence.mjs" },
  { "matcher": "Write|Edit|MultiEdit", "command": "node .claude/hooks/policy-004-contract-preservation.mjs" }
],
"Stop": [
  { "command": "node .claude/hooks/policy-002-real-build.mjs" },
  { "command": "node .claude/hooks/policy-005-completion-evidence.mjs" }
]
```

### 4.5 Evidence 格式

```json
{ "policy": "P001", "result": "BLOCK", "reason": "assert weakened: 5→2", "file": "FooService.cs", "actor": "agent", "task": "P1", "timestamp": "2026-09-01T..." }
```

### 4.6 测试 (Adversarial)

- `harness-adversarial.mjs` 扩展: 注入 fake-weak-assert, skip-build, no-evidence-mutation, break-frozen-contract 四类攻击，期望全部 BLOCK
- 回归: `test-hooks.mjs` 44/44 保持

### 4.7 非目标

- 不做 30 策略框架、不做 Structural/Semantic Drift (Phase 2)、不改 `harness-resolver.mjs` 逻辑、不迁移 Superpowers 方法

---

## 5. 影响评估

- **变更类型:** Governance Execution (Policy/Hook/Gate/Evidence)
- **探索深度:** 2 级 (Control Plane 1.0 + Harness Governance 0.6)
- **涉及符号:** 5 policies + 5 hooks + 1 gate + 1 baseline
- **是否截断:** 否
- **风险:** Hook 数量膨胀 → 演进为 Engine；Build 证据 TTL 过期导致误 BLOCK → 缓解 TTL 30min + 显式 `pnpm build` 重跑提示

---

## 6. 自检

- [x] 无 TBD/TODO/placeholder
- [x] 架构与 5 策略需求一致
- [x] 单计划聚焦垂直切片，非巨型框架
- [x] 无二义 (P003 五元组、P002 TTL、P004 cr-approved 判定均显式)
