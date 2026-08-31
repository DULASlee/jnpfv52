# JNPF v5.2 后端表结构重构 — 最终完成冲刺 v3.1

## 任务ID: PHASE-32-FINAL-CLOSURE-20260831
## 完成时间: 2026-08-31T18:55:00

## 执行概要
- 需求: JNPF v5.2 后端 BASE_SIGNATURE / BASE_SIGNATURE_USER 两张表添加主键约束
- 方案: M32-01 (单列 PK on f_id) + M32-02 (复合 PK on f_signature_id, f_user_id)
- 完成阶段: WAVE 0~5 全部完成

## 关键技术决策

### 决策1: M32-02 复合主键 vs 代理主键
- 选项A (复合): (f_signature_id, f_user_id) — 保留关联表业务语义
- 选项B (代理): 新增 f_id 单列主键 — 不反映业务关系
- **结论**: Chief Architect 批准 Option A
- **理由**: 关联表使用复合主键是正确建模选择

### 决策2: ALTER COLUMN NOT NULL (M32-02 前置条件)
- **问题**: f_signature_id 和 f_user_id 在 DB 层是 NULLABLE
- **风险**: SQL Server 要求 PK 列必须 NOT NULL
- **触发**: sqlcmd 执行 M32-02 时报错 — Chief Architect 立即授权修复
- **方案**: `ALTER TABLE base_signature_user ALTER COLUMN f_signature_id NVARCHAR(50) NOT NULL` + 同 f_user_id
- **前提**: 两表均为空表（0行），无任何数据风险
- **避免策略**: 未来 DDL 设计应在 phase-32/migration.sql 中包含列类型 + NULLABILITY 声明

### 决策3: SqlSugar [Navigate] 兼容性
- SignatureUserEntity 使用 `[Navigate(NavigationType.OneToMany, nameof(SignatureId))]`
- 依赖父表 Id 列作为 FK 目标，与子表 PK 结构无关
- **验证**: 复合 PK 不影响 [Navigate] 行为 — FK 列是 SignatureId 而非 PK 列

## 已执行迁移（实际验证）

| 迁移 | SQL | 状态 |
|---|---|---|
| M32-01 | PK_base_signature on f_id | ✅ ACTUALLY_FIXED |
| M32-02 前置 | ALTER COLUMN f_signature_id NOT NULL | ✅ ACTUALLY_FIXED |
| M32-02 前置 | ALTER COLUMN f_user_id NOT NULL | ✅ ACTUALLY_FIXED |
| M32-02 | PK_base_signature_user on (f_signature_id, f_user_id) | ✅ ACTUALLY_FIXED |

## 发现 Bug 及根因

### Bug1: 架构测试 JNPF.Tests.Architecture 存在预有失败
- **问题**: `SugarTable_Mappings_ShouldBe_Unique` 失败 — BASE_AI_Call_LOG 重复映射
- **根因**: AiCallLogEntity 在两个不同路径下有重复定义
  - `backend/application/JNPF.API.Entry/Entities/AiCallLogEntity.cs`
  - `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiCallLogEntity.cs`
- **影响**: 1 个测试失败，92 个通过（与本次重构无关）
- **状态**: 预有（与 phase-32 无关）

### Bug2: 后端 localhost:5000 未运行
- **问题**: 无法执行 API 运行时冒烟测试
- **根因**: backend 未启动
- **影响**: Stage D (API smoke) 被阻塞，架构分析和 ORM 验证已完成
- **避免策略**: 下次重构开始前应先确认 backend 运行状态或提前启动

## 踩坑记录

### 坑1: 迁移 SQL 未包含列 NULLABILITY
- phase-32/migration.sql 中 M32-02 只写了 ADD CONSTRAINT，没有预判列类型
- 导致执行时遇到 SQL Server PK 约束的 NOT NULL 要求而报错
- **避免**: DDL 迁移脚本必须同时声明列类型 + NULLABILITY + PK，三者缺一不可

### 坑2: 文档版本号与实际执行状态不一致
- JNPF-Final-Refactoring-Matrix-vFinal.json 状态标记为 "FINAL_CLOSURE"，但实际未执行
- 导致误以为迁移已完成
- **避免**: 文档状态应区分 "READY_TO_EXECUTE" vs "ACTUALLY_EXECUTED"

## 变更文件

| 文件 | 操作 |
|---|:---|
| backend/database/final-refactor/JNPF-Final-Refactoring-Matrix-vFinal.json | 更新 (metadata + execution_evidence + ACTUALLY_FIXED) |
| backend/database/final-refactor/JNPF-Table-Refactoring-Final-Report.md | 更新 (执行时间 + corrective step + 实际验证结果) |

## 验证结果

| 检查项 | 结果 |
|:---|:---|
| dotnet build JNPF.Systems.csproj | PASS (0 errors) |
| dotnet test Architecture (排除预有失败) | PASS (92/92) |
| sqlcmd preflight (live) | PASS |
| sqlcmd migration (live) | PASS |
| sqlcmd postflight (live) | PASS |

## 风险与建议

1. **FR-009/FR-010** (BASE_IM_CONTENT / BASE_IM_REPLY): 实体标注为 tenant-aware 但数据全为 NULL — 数据质量问题，非 schema 问题，需先修复数据再评估索引
2. **17 个 False Positive** 已通过 Chief Architect 授权正式关闭，文档已存档
3. **7 个 Deferred 项** 有明确触发条件，下次生产数据充足时重新评估

## 质量门通过记录

| 门 | 结果 | 时间 |
|:---|:---|:---|
| Chief Architect M32-01 授权 | PASS | 2026-08-31 |
| Chief Architect M32-02 Option A 授权 | PASS | 2026-08-31 |
| Chief Architect 15项 Tenant Index Defer 授权 | PASS | 2026-08-31 |
| Chief Architect 17项 False Positive 关闭授权 | PASS | 2026-08-31 |
| Live DB Migration Execution | PASS | 2026-08-31T18:50 |
| Postflight PK 验证 | PASS | 2026-08-31T18:52 |
| Regression (build + test) | PASS | 2026-08-31T18:55 |

## 最终验收结论
- **2026-08-31T19:20:00**: Chief Architect 给出 FINAL ACCEPTANCE APPROVED
- 状态: CLOSED

## Knowledge Consolidation Mode（2026-08-31T19:25:00）

建立 JNPF Engineering Knowledge System 6+N 架构：

### 节点 1: Project Charter ✅
- 位置: `docs/architecture/v52/database-modernization/JNPF-Table-Refactoring-Charter.md`
- 内容: Why/What/Not What/Objectives/Done Criteria/Current Status

### 节点 2: Final Matrix（机器可读事实源）✅
- 位置: `backend/database/final-refactor/JNPF-Final-Refactoring-Matrix-vFinal.json`
- ACTUALLY_FIXED=2, DEFERRED=7, FALSE_POSITIVE=17, NO_CHANGE=10

### 节点 3: Final Acceptance Report
- 位置: `backend/database/final-refactor/JNPF-Table-Refactoring-Final-Acceptance.md`

### 节点 4: Final Validation Package
- 位置: `backend/database/final-refactor/final-validation/`
- 包含: final-schema-diff.json, final-test-results.json, final-runtime-validation.json, final-regression.json, final-rollback-status.json

### 节点 5: Evidence Archive（按批次归档）
- Phase-8 batches (batch-29~31) → `docs/universal/Phase-8/p8-c/`
- M32-01/M32-02 SQL → `backend/database/phase-32/`
- final-refactor/ → 最终交付物

### 节点 6: Session Key Points（本文件）
- 机器可读: 否（但 .claude/memory/ 对所有 AI Agent 可访问）
- 用途: 跨会话传递技术决策、Bug 分析、踩坑记录

---

# AI工程OS Harness治理与Pre-AgentOS门禁 — 2026-09-01 (Phase 0.5/0.6/Phase1 Vertical Slice → PRE-AGENTOS-GATE)

**任务ID:** PHASE0.5-HARNESS-20260901
**完成时间:** 2026-09-01T21:30:00
**状态:** PRE-AGENTOS-GATE ACCEPTED (前置治理通过, AgentOS NOT YET)

## 执行概要
- **需求:** 建立 Harness Governance & Authority Model，使 Superpowers/ECC/MCP/Memory 从“同时影响 Agent”收权为 Control Plane 唯一治理 + Resolver 定可见 + Quarantine 隔离
- **路径:** Inventory(273机械)→Classify(7类)→Authority L1唯一→Boundary→Quarantine 11项→Capability Registry 11→Memory Contract→Resolver(<10k)→Deferred 9 WARN→黑盒31+54+回归44→3×BLOCK修复→独立复验→Gate PASS
- **完成阶段:** Phase 0.5/0.6 PASS, Phase1 Implementation APPROVED (black-box verified), Closure Repair VERIFIED, PRE-AGENTOS-GATE ACCEPTED

## 关键技术决策
### 决策1: 不做大扫除，采用收权式四步法
- **选项A (大扫除):** 删除 Superpowers/ECC/旧 Rules/MCP — 丢失能力，隐式依赖断裂
- **选项B (收权):** Inventory→Classify→Quarantine→Migration/Retire — 保留能力但剥夺治理权
- **结论:** Chief Architect 批准 Option B (先收权再清理)
- **理由:** Clean Harness ≠ Empty Directory，Clean = Authority/Loading/Boundary/Resolution Clear

### 决策2: Hook-per-Policy 仅为 Phase1 策略
- **问题:** 30 Policies = 30 Hooks 会爆炸
- **决策:** Phase1 5策略用 Hook-per-Policy 便于验证，永久 Enforcement Point 为 PreMutation/PreBuild/PreTest/PreCompletion (Policy Engine 在这些点求值)
- **避免:** 未来不冻结为每 Policy 一 Hook

### 决策3: Evidence 11字段结构化 + Gate Final ONLY + State仅AgentOS
- **坑:** log.push 假证据、P005 污染中间阶段、State在Policy引擎直接改
- **修复:** Evidence 含 EvidenceType/Actor/Task/Stage/Policy/Action/Before/After/Tool/Result/Timestamp/Integrity+version，Gate Requires `type=REAL_BUILD & exit0`，P005 仅 Completion 阶段 (6阶段 NOT_APPLICABLE)，State 仅 AgentOS Transition

## 已执行治理 (实际验证)
| 阶段 | 证据 | 状态 |
|------|------|------|
| Inventory 273 | evidence/PHASE0.5-INVENTORY.json | ✅机械扫描 |
| Authority L1唯一 | PHASE0.5-AUTHORITY.json UNIQUE | ✅ |
| Resolver | hooks/harness-resolver.mjs 8.2k | ✅ 54黑盒 |
| Quarantine 11 | .ai/quarantine NOT LOADED | ✅ 31对抗 |
| Capability 11 | CAPABILITY-REGISTRY.md | ✅ |
| Memory Contract | MEMORY-CONTRACT.md | ✅ |
| 3×BLOCK修复 | P005/P003/P004 各组 | ✅ 3/3 BLOCK + 3/3 ALLOW |
| Drift | HARNESS-BASELINE 279 raw CLEAN | ✅ |
| Regression | test-hooks 44/44 | ✅ |

## 发现 Bug 及根因
### Bug1: P005 无NOT_APPLICABLE污染中间阶段
- **问题:** P005 Completion Gate 在 Discovery/Build/Test 等中间 Stop 也 BLOCK
- **根因:** 无阶段隔离，always BLOCK
- **修复:** 增加 COMPLETION_STAGE/intent 隔离，非完成阶段 NOT_APPLICABLE (policy-005-completion-evidence.mjs:14-34)

### Bug2: P003 全局diff满足任意文件
- **问题:** OtherFile.cs 变更可满足 FlowCommentService.cs 的 Mutation
- **根因:** `git diff --stat` 全局 + Actor/Task 未强制
- **修复:** 文件级 Target绑定 + Workspace边界 + 5-field Evidence (policy-003-mutation-evidence.mjs:40-87)

### Bug3: P004 自签 cr-approved/cr-safe 绕过
- **问题:** workflow-state Fake-CR 与 //cr-safe 文本可绕过 frozen
- **根因:** 基准非权威，文本标记可注入
- **修复:** CONTRACT-BASELINE.json 7 hashes 权威，workflow-state 与 cr-safe 均 IGNORED (policy-004-contract-preservation.mjs:23-68)

## 踩坑记录
### 坑1: Raw 273 vs Unique 210 口径歧义
- 原 142 口径手工推测 → 统一为 Raw/Unique/Mirrors/Quarantined/Authoritative 机械口径 via harness-drift.mjs --baseline

### 坑2: .gitignore 证据被忽略
- `.ai/quarantine/` 整个忽略导致 MANIFEST 不可审计 → 改为 `.quarantine/backups/` 仅备份忽略，证据 tracked

### 坑3: Evidence transient 被计入漂移
- 09-evidence/*.json 瞬态导致 Drift 误报 277→279 → harness-drift.mjs:58 排除 09-evidence

### 坑4: 中文标题通过CLI传递乱码
- `ecc memory save --title 中文` 经 GBK 控制台乱码 → 改用 --body-file UTF8 文件，标题用英文或确保 UTF8

## 变更文件
| 文件 | 操作 |
|------|------|
| docs/architecture/AI工程OS-Harness治理与Pre-AgentOS门禁工作总结报告-20260901.md | 新建 19KB |
| docs/adr/ADR-027-Harness治理与Pre-AgentOS门禁.md | 新建 9KB Final |
| docs/harness/8件 (Inventory/Authority/Classification/Boundary/Resolution/Capability/Memory/Deferred) + PRE-AGENTOS-GATE.md | 新建 |
| evidence/6+3 JSON + PHASE0.5-CLOSURE.json | 新建 |
| .claude/control-plane/00-governance/CONTRACT-BASELINE.json | 新建 7 hashes |
| .claude/hooks/harness-resolver.mjs / harness-drift.mjs / phase05-adversarial.mjs / blackbox-adversarial.mjs / policy-*.mjs 5 + lib | 新建/修复 |
| .claude/hooks/policy-005/003/004 3×BLOCK修复 | 修复 |

## 验证结果
| 检查项 | 结果 |
|--------|------|
| 机械 Inventory 273 | PASS |
| Harness对抗 31 | PASS |
| Policy黑盒 54 | PASS |
| Policy对抗 19 + Harness 23 | PASS |
| 回归 44/44 | PASS |
| Drift CLEAN 279 | PASS |
| 3×BLOCK 3/3 + 3/3 Positive | PASS |

## 风险与建议
1. 9 WARN (semantic fake-green/target binding/crypto/determinism/version等) 已在 DEFERRED-REGISTER 封账，TargetPhase 明确 (Phase2/4)，不得遗忘
2. Resolver <10k 需保持不堆叠，后续 AgentOS 不得把新 Runtime 逻辑塞入 resolver
3. Phase 1 保持 READY FOR FORMAL CLOSURE 未扩张，AgentOS 需基于 Harness Contract 建最小可执行闭环

## 质量门通过记录
| 门 | 结果 | 时间 |
|----|------|------|
| Phase 0.5 PASS | PASS | 2026-09-01 |
| Phase 0.6 PASS | PASS | 2026-09-01 |
| Phase1 Implementation APPROVED | PASS | 2026-09-01 |
| Closure Repair 3/3 VERIFIED | PASS | 2026-09-01 |
| Independent Review #2 54 PASS | PASS | 2026-09-01 |
| PRE-AGENTOS-GATE ACCEPTED | PASS | 2026-09-01 |
| Evidence Freeze 12项哈希 | PASS | 2026-09-01 |

## 最终验收结论
- **PRE-AGENTOS-GATE PASS** — 前置治理条件通过 (Authority/Resolution/Quarantine/Capability/Memory/External 均 PASS, 黑盒+回归+漂移全绿)，**不是** AgentOS Runtime 已建成
- 下一阶段: AgentOS Runtime Foundation 最小可执行闭环 (Handoff: evidence/PHASE0.5-CLOSURE.json)

