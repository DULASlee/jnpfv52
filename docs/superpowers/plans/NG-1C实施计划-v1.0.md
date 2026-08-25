# NG-1C Platform Domain Ownership 实施计划 v1.0

> ⛔ **STATUS: SUSPENDED / ROUTE CONVERGED（2026-08-26 人工裁决）——C0–C4 永不启动**
>
> NG-1C 大矩阵路线已终止，由《JNPF 平台整体结构基线》v0.2 的 **PHASE 主路线**取代（当前位于 PHASE 3 数据架构设计入口）。本计划仅作方法论存档；六态/三权/PASS 完整性等方法论已沉淀至总基线引用体系。**任何 Agent 不得执行本计划。**

**日期**：2026-08-26 ｜ **裁决依据**：G0=PASS（NG-1 规格 §0A.9）+ 人工批准意见（方法论约束 ⑦–⑩）
**规格**：`docs/superpowers/specs/JNPF-Next-NG1C-Platform-Domain-Ownership规格.md`
**状态**：⏸ **已落盘待审核——审核通过前不得启动 C0**
**性质**：只读审计（六零约束：ZERO BUSINESS CODE / ZERO DB CHANGE / ZERO DATA CHANGE / ZERO DEPLOYMENT / ZERO MICROSERVICE / ZERO ASPIRE ARCHITECTURE）

---

## 1. 目标

对 157 张 P0/P1 表完成六态主能力归属分析 + 16 个候选能力（HYPOTHESIS ONLY）的三权取证与域级三态裁决。

> 回答「这个东西是不是领域？」；拒绝回答「这个表应该塞进哪个领域？」。
> 执行顺序铁律：先机械映射，再取 Ownership 证据，最后才做三态 Domain 裁决——禁止人工先"凭经验画领域"。

## 2. 表级六态（替代「一表一候选」硬门，裁决约束 ⑦⑧）

| 状态 | 判据 | 后续 |
|---|---|---|
| PRIMARY_DOMAIN | 生命周期+契约明确归属单一候选 | 进入该候选聚合证据池 |
| SHARED_INFRA | 多候选共同依赖基础数据，无单一业务属主 | Anti-Service 评估通道 |
| CROSS_DOMAIN | ≥2 候选存在真实写决策权竞争 | 双方证据登记 → 无法裁决则 CONFLICT |
| FRAMEWORK | 迁移/事务/调度框架自建表（SchemaVersions/undo_log 形态） | 登记基础设施，不参与域裁决 |
| DEFERRED | sa_\* 13 张（人工裁决第 6 条延后） | 零裁决 |
| CONFLICT | 证据冲突且三权规则无法消解 | **Human Gate，禁止猜测** |

覆盖门：157/157 每张表获得六态之一 + 证据；**不要求存在 PRIMARY_DOMAIN**（SHARED_INFRA/CROSS_DOMAIN/FRAMEWORK 本身即有效结论）。

## 3. Candidate Capability Set v0（HYPOTHESIS ONLY, NOT A DOMAIN DECISION）

```text
Identity / Organization / Authorization / Tenant / Dictionary / WorkflowRuntime /
FormRuntime / DataModeling / AppMetadata / File / Message / Audit / EventInfra /
Scheduler / Integration / AIStudio  ＋ SpecialInfra（兜底）/ DEFERRED（sa_*）
```

- C4 裁决前不携带任何预判结论；候选可被合并/拆分/增删；
- Anti-Service 历史候选项（Identity/Tenant/Authorization/Dictionary/Form Metadata/交叉表）必须反证验证——不得预设 BLOCK，也不得预设 PASS（裁决约束 ⑨）。

## 4. Matrix Schema

21 列（Table / AssetClass / BusinessCapability / LifecycleOwner / WriteOwner / DecisionOwner / ContractOwner / OwnerEvidenceGrade / ReadConsumers / TenantScope / TransactionBoundary / CrossDomainWrites / CrossDomainReads / PermissionDependency / RuntimeDependency / TemplateDependency / MigrationDependency / Evidence / Confidence / OwnershipState），列 × 证据源映射见规格 §4.1。所有证据强制 file:line 或「全库 0 引用」式缺位证明。

## 5. 取证分层（含 PASS 完整性铁律，裁决约束 ⑩）

```text
A 类 ≈35–40 张核心聚合表：全链六通道（创建/写/读/删/事务/权限）file:line
B 类其余 P0/P1：access-map 底稿 + 抽验（发现问题用）
⚠ 铁律：任何候选 PASS 前，必须对其全部 PRIMARY_DOMAIN 表补齐 Candidate 级完整取证
        ——抽样不得作为 PASS 依据
```

工具链：Serena `find_referencing_symbols` / `find_symbol`（单符号精确查）、Codebase-Memory `trace_path`（多跳调用链）、Grep 带 path/glob（文本）、只读 sqlcmd（DB 事实）、沿用 ng1b `gen-*.ps1` 可复算脚本模式（**禁止新增 .mjs 业务脚本**，符合仓库规则一）。

## 6. 阶段计划（C0 → C4）

| 阶段 | 动作 | 产出 | 复算方式 |
|---|---|---|---|
| **C0 冻结** | 建 `ng1c-domain-ownership/` 目录；固化六态 schema + Hypothesis v0 + gen-\* 脚本骨架 | 目录 + schema + 脚本骨架 | 脚本 dry-run |
| **C1 机械映射** | provenance-matrix.csv JOIN 候选目录 → 157 表六态初标；红旗检查（TemplateDependency 非空等） | `table-capability-map.csv` | gen-capability-map.ps1 重跑一致 |
| **C2 三权取证** | A 类全链；B 类底稿+抽验；跨模块写逐条 file:line；CONFLICT 升级登记 | `ownership-evidence.tsv` + `conflict-register.md` | 证据行均含 file:line |
| **C3 依赖画像** | 跨域读写矩阵、ACID 清单、权限/租户耦合汇总 | `capability-dependency-profile.md` | 由 C1/C2 聚合生成 |
| **C4 域级裁决** | 反证两问 + 域级三态 + PASS 候选完整取证补齐 + Anti-Service §3.2 六项核销 | `domain-ownership-matrix.csv` + `NG-1C-Final-Review.md` | DoD 逐条核对 |

每阶段完成即暂停自查（节点纪律）；**C4 完成后 STOP 等待人工裁决**。

## 7. 禁止项

1. 六零约束全程有效；
2. 不进入 NG-1D/1E（Transaction/Query/Migration 正式裁决）；
3. 不预设任何候选三态；不把 Anti-Service 清单当免检通行证；
4. 不复活 D1–D12（SUPERSEDED）；PX 零猜测；sa_\* 零裁决；
5. 不修改 `provenance-matrix.csv` 等 NG-1B 产出物（只读 JOIN）；
6. 完成后 STOP，裁决权在人工。

## 8. DoD

同规格 §9 八条：157/157 六态覆盖、PASS 完整取证、BLOCK/REFINE 反证记录与解除条件、DEFERRED 零裁决、Anti-Service 六项核销、六零合规、Final Review 提交、STOP。

## 9. STOP 条件

- **现在**：本计划落盘即 STOP——待人工审核本规格+计划后另行批准 C0；
- **将来**：C4 完成提交 Final Review 后 STOP——PASS/REFINE/BLOCK 裁决权在人工；
- 无论何种结果：Domain Ownership 正式裁决 / Microservice / Aspire Architecture / S2 保持 BLOCKED。
