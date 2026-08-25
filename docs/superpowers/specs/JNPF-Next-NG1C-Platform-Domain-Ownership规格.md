# JNPF-Next NG-1C — Platform Domain Ownership Proof 设计规格 v1.0

> ⛔ **STATUS: SUSPENDED / ROUTE CONVERGED（2026-08-26 人工裁决）**
>
> **NG-1C 不再作为「157 表 × 20 列 Ownership 大矩阵」独立工程执行。** 该路线已由项目主路线收敛取代：
> - 唯一总基线：`docs/architecture/JNPF平台整体结构基线.md`（v0.2）——PHASE 0–8 主路线；
> - Capability Map 已在基线 §二 形成（五类 A–E + 核心闭环），替代大矩阵的产出目的；
> - 本规格的**方法论资产保留参考**：六态表级归属、三权判定（Lifecycle/Decision/Contract Owner）、反证两问、PASS 完整性铁律——若未来某能力进入 PHASE 5+ 需要深度 Ownership 取证时按需引用；
> - 本规格与配套计划**不得被任何 Agent 当作待执行任务启动**。NG 编号系列退役为历史证据链。

**日期**：2026-08-26 ｜ **裁决依据**：G0 Final Review 人工终审 PASS + NG-1 规格 §0A.9
**状态**：规格与配套计划已批准落盘；**C0–C4 执行待本规格+计划审核通过后另行批准启动**
**性质**：只读审计——**六零约束**：ZERO BUSINESS CODE / ZERO DB CHANGE / ZERO DATA CHANGE / ZERO DEPLOYMENT / ZERO MICROSERVICE / ZERO ASPIRE ARCHITECTURE

---

## 0. 人工批准裁决记录（APPROVE，2026-08-26）

### 0.1 批准范围

| # | 项 | 状态 |
|---|---|------|
| ① | G0 = PASS 回写（NG-1 规格 §0A.9 + G0-Final-Review 终态） | ✅ 已执行 |
| ② | 本规格落盘 | ✅ |
| ③ | 《NG-1C实施计划-v1.0》落盘 | ✅ |
| ④ | NG-0 `domain-candidates.md`（D1–D12）标记 `HISTORICAL-CANDIDATE / SUPERSEDED`——保留历史架构决策证据价值，**不得删除**，但不得再作输入边界 | ✅ 已执行 |
| ⑤ | 157 张 P0/P1 表进入 Ownership Proof | ✅ 批准（执行待审核） |
| ⑥ | 16 个候选能力仅作为 Hypothesis | ✅ |

### 0.2 方法论附加约束（与批准同时生效，优先于本规格正文任何冲突表述）

| # | 约束 |
|---|------|
| ⑦ | **禁止「一表一 Domain」强制归属**——NG-1A/1B 已证明一张表可能是共享基础设施、聚合根的一部分、运行时支撑表或跨能力基础设施；强制一表一域 = 重新制造刚被纠正的「表 → 领域」错误 |
| ⑧ | 表级归属允许 `SHARED_INFRA / CROSS_DOMAIN` 等非领域状态（§4.2 六态） |
| ⑨ | **禁止预设任何 Candidate 的 PASS/BLOCK**——历史 Anti-Service 清单候选项必须逐项反证验证；本规格正文出现的任何倾向性表述均为待验证假设，不构成结论 |
| ⑩ | **PASS 必须拥有 Candidate 级完整证据**——抽样只用于发现问题，不得作为 PASS 依据 |

### 0.3 明确不批准（本阶段冻结）

立即启动 C0（待两文件审核）/ Domain 拆分 / 微服务实现 / Aspire 架构 / 数据库改造 / 迁移。

---

## 1. 目标边界

> 对 157 张 P0/P1 平台资产完成**主能力归属分析**（六态），为每个能力候选建立三权证据
> （Lifecycle / Decision / Contract Owner）+ 事务/查询耦合画像 + 反证两问回答，
> 产出 **Domain Candidate Matrix** 与域级三态裁决（PASS / REFINE / BLOCK）。
>
> **不拆域、不做微服务设计、不碰 Aspire、不预设任何结论。**

NG-1C 回答的问题：「**这个东西是不是领域？**」
NG-1C 拒绝回答的问题：「这个表应该塞进哪个领域？」

---

## 2. 输入基线（全部可复算）

### 2.1 范围内（157 张）

| 分类 | 张数 | Provenance | 说明 |
|------|-----:|-----------|------|
| P0 PLATFORM_CORE | 146 | PROVEN 128 / PARTIAL 18（缺位均已证明）/ UNKNOWN 0 | G0 收口产物 |
| P1 LOWCODE_RUNTIME | 11 | PROVEN 11 | 100% |

### 2.2 排除集合（132 张，禁止进入任何分析）

P2 PRODUCT_TEMPLATE=48 ｜ P3 DEMO_APPLICATION=25 ｜ P4 CUSTOMER_APPLICATION=5 ｜ P6 LEGACY=47 ｜ P7 ORPHAN=1 ｜ PX UNKNOWN=6（PX 保持 BLOCKED，零猜测）。

### 2.3 输入文件

| 文件 | 用途 |
|---|---|
| `ng1b-provenance/provenance-matrix.csv`（289×26） | 表级基线：asset_class / write_owner / read_consumers / api_exposed / ui_menu / runtime_required / startup_impact 等 |
| `ng1b-provenance/_access-map.tsv` `_entity-modules.tsv` `_api-services.tsv`（160 服务类）`_creation-sources.tsv` `_seed-map.tsv` | 写读 / 模块 / API / 创建源底稿 |
| `.claude/evidence/jnpf-next-architecture/db-fks.tsv` `db-index-stats.tsv` | 物理 FK / 索引事实 |
| ng1a `platform-asset-classification.csv` | P0–PX 分类基线 |

### 2.4 特殊资产处理

| 资产 | 处理 |
|---|---|
| sa_\* 13 张 | 进矩阵但 **OwnershipState = DEFERRED**（延续 §0A.7 人工裁决第 6 条），不计入域级成败统计，零裁决 |
| SchemaVersions / undo_log | FRAMEWORK 运行时基础设施（§0A.8.4），预期形态为 FRAMEWORK 态；真删实验仍归 Platform Independence Proof |
| base_file = P6 LEGACY 的事实 | 如实登记「File 能力当前有职责无核心表」——这是 NG-1C 应暴露的结论之一，不得为凑域而复活该表 |
| mt\* 5 张动态表 | P4 排除；但其与 P1 base_visual_\* 元数据的注册关系属于 FormRuntime/DataModeling 候选的分析对象（只分析元数据侧） |
| NG-0 D1–D12 | SUPERSEDED，仅历史线索（见 §0.1 第④条） |

---

## 3. Candidate Capability Set v0

```text
STATUS = HYPOTHESIS ONLY
NOT    = A DOMAIN DECISION
用途   = C1 机械映射的靶子清单；每个候选在 C4 裁决前不携带任何预判结论
```

v0 清单（16 项，源自前缀族 × `_api-services.tsv` × 菜单实测的机械归纳，仅为「提问的名字」）：

```text
Identity / Organization / Authorization / Tenant / Dictionary /
WorkflowRuntime / FormRuntime / DataModeling / AppMetadata / File /
Message / Audit / EventInfra / Scheduler / Integration / AIStudio
另设 SpecialInfra 兜底桶承接 Framework/杂项；DEFERRED 承接 sa_*
```

规则：
- 候选不是答案——C4 可能合并、拆分、增删候选；
- 历史 Anti-Service 清单（NG-1 规格 §3.2：Identity/Tenant/Authorization/Dictionary/Dynamic Form Metadata/交叉事务表）中的候选项**必须逐项反证验证**，不得预设 BLOCK；
- 同样不得预设 PASS——包括看起来最自洽的候选。

---

## 4. Domain Candidate Matrix

### 4.1 Schema（21 列 × 证据源映射）

| 列 | 证据源（已有） | 新增采集 |
|---|---|---|
| Table | provenance-matrix.csv | — |
| AssetClass | 同上（P0/P1） | — |
| BusinessCapability | §3 目录映射 | CONFLICT 人工归位 |
| LifecycleOwner | _creation-sources + F_DeleteMark 软删扫描 | 归档/清除路径（谁 DELETE/purge） |
| WriteOwner | _access-map.tsv write_owner | 按 §5 三优先级复核；跨模块写行逐条 file:line |
| DecisionOwner | Service 业务规则定位（校验/状态迁移） | Serena 符号引用抽验调用链 |
| ContractOwner | _entity-modules.tsv（实体所在模块）+ Migration 脚本归属 + DTO | — |
| OwnerEvidenceGrade | 综合 | file:line / 模块级 / 推断 三档 |
| ReadConsumers | _access-map read_consumers | Codebase-Memory trace_path 补多跳 |
| TenantScope | provenance tenant_style + ITenantFilter 挂靠点 | — |
| TransactionBoundary | [Transactional]/useTran/TransactionScope 扫描 | 聚合操作级归组 |
| CrossDomainWrites | writer 模块 ≠ ContractOwner 模块的行 | 逐条裁决合法共享 vs 侵权写 |
| CrossDomainReads | read_consumers 聚合 | — |
| PermissionDependency | [SecurityDefine] 清单 + GetCondition 双路径消费点 | — |
| RuntimeDependency | runtime_required/startup_impact | — |
| TemplateDependency | template/demo 列（P0/P1 出现非空 = 红旗上报） | — |
| MigrationDependency | db-fks.tsv + 逻辑引用扫描 | — |
| Evidence | file:line 清单 | — |
| Confidence | HIGH≥80% / MED 50–80% / LOW<50%（assertion-discipline） | — |
| OwnershipState | §4.2 六态 | — |

所有证据强制 **file:line 或「全库 0 引用」式缺位证明**——与 NG-1B 同一纪律：缺位的实证结果 ≠ 未扫描。

### 4.2 表级主能力归属状态（六态——替代「一表一候选」硬门）

```text
Table
 ├── PRIMARY_DOMAIN   生命周期+契约明确归属单一候选 → 进入该候选聚合证据池
 ├── SHARED_INFRA     多候选共同依赖的基础数据，无单一业务属主 → Anti-Service 评估通道
 ├── CROSS_DOMAIN     ≥2 候选对其存在真实写决策权竞争 → 双方证据登记 → 无法裁决 → CONFLICT
 ├── FRAMEWORK        迁移/事务/调度等框架自建表 → 直接登记基础设施，不参与域裁决
 ├── DEFERRED         sa_* 13 张（人工裁决延后）→ 零裁决
 └── CONFLICT         证据冲突且三权规则无法消解 → Human Gate，禁止猜测
```

**覆盖门（替代旧「每张恰好归 1 候选」）**：157/157 每张表必须获得六态之一的显式状态 + 证据；**不要求**每张表都有 PRIMARY_DOMAIN。SHARED_INFRA / CROSS_DOMAIN / FRAMEWORK 态本身就是有效产出——这正是「这东西是不是领域」的证据形态。

### 4.3 域级三态裁决（对候选能力，不对表）

```text
PASS   ：PRIMARY_DOMAIN 表构成清晰聚合
        + 单一 LifecycleOwner 且 Decision/Contract Owner 同域
        + 事务边界可陈述（ACID 清单）
        + 跨域依赖全部走读模型/契约（无侵权写）
        + 可定义稳定 Contract
        + Candidate 级完整取证（§7 铁律）

REFINE ：候选边界存在但不满足 PASS 任一项
        （三权分裂 / 跨域读过重 / 租户权限耦合深）
        → 必须写出解除条件（如「权限快照上线后重评」）

BLOCK  ：Shared Infrastructure / 无独立 Ownership / 全局强依赖 / 无法独立迁移
        → BLOCK 本身就是架构结论，不是失败
        → 必须写出「为何是基础设施而非领域」的反证记录
```

**反证两问（每个候选必答，写入证据文档）**：

1. 把这个候选从平台核心拿走，平台是否仍可作为低代码开发平台运行（启动 → 登录 → 建应用 → 建模 → 表单 → 发布 → 运行）？
2. 把它保留在平台核心，它是否实际只是 Shared Infrastructure 而非 Business Domain？

最终结构预期形态（仅为方向示意，非结论）：Platform Kernel（Identity/Tenant/Authz/Metadata Runtime/Dynamic Data Runtime/Workflow Runtime/File Runtime/Event-Job Infrastructure）+ Platform Business Domains（由四重证明产生）。**哪些进 Kernel、哪些成 Domain，由本轮证据决定，不由本段预设。**

---

## 5. 三权判定规则（继承 NG-1 规格 §2.2，强化版）

```text
Owner ≠ IRepository caller
Owner ≠ Controller
Owner ≠ Service class（类名出现 ≠ 所有权）
Owner ≠ Folder / 模块目录
Owner ≠ Table prefix
Owner ≠ Entity 存在
```

按以下优先级判定，冲突且无法消解时降级 CONFLICT（禁止猜测）：

1. **生命周期权**：谁创建/归档/删除（创建与删除路径是强信号）；
2. **决策权**：谁决定数据内容变更（业务规则归属），而非谁执行写语句；
3. **契约权**：谁定义 schema/校验/版本（如 ai_entity_field = 字段唯一源）。

---

## 6. 与既有裁决的关系

| 既有资产 | 在 NG-1C 中的地位 |
|---|---|
| Anti-Service/Shared-Core 清单（NG-1 规格 §3.2 六项） | 输入线索之一；C4 逐项核销（一致 / 修订+理由）；**不得预设其结论正确** |
| 战略顺序锁定（§0A.8.5） | 不变：Ownership → Transaction → Query → Migration → 微服务裁决 |
| D12 Order Slice | 维持证伪暂停；NG-1C 不恢复 |
| sa_\* 裁决第 6 条 | DEFERRED 延续 |
| Aspire 定位（§7） | 不变：工具层，不参与边界判断 |
| NG-0 D1–D12 | SUPERSEDED；仅历史线索 |

---

## 7. 取证分层与完整性铁律

```text
A 类（约 35–40 张核心聚合表：base_user / base_authorize / base_module* /
      flow_task / flow_task_operator / base_visual_dev / ai_ir_events /
      ai_entity_field / base_dictionary_data / base_sys_log /
      SYS_EVENT_OUTBOX_MESSAGE 等高频读写与聚合根表）
      → 全链六通道取证（创建/写/读/删/事务/权限），全部 file:line

B 类（其余 P0/P1）
      → access-map 底稿 + 抽验（目的：发现问题）

⚠ PASS 完整性铁律：任何候选最终 PASS 前，必须对其全部 PRIMARY_DOMAIN 表补齐
   Candidate 级完整 Ownership Proof。「抽样未发现跨域写」永远不得作为 PASS 依据
   （低代码平台动态面大，抽样盲区致命）。
```

---

## 8. 禁止项

1. 六零约束全程有效（ZERO BUSINESS CODE / DB / DATA / DEPLOYMENT / MICROSERVICE / ASPIRE）;
2. 禁止进入 Transaction/Query/Migration Boundary 的正式裁决（那是 NG-1D/1E）；
3. 禁止把候选清单当答案、把 Anti-Service 清单当免检通行证；
4. 禁止复活 D1–D12 或任何 SUPERSEDED 假设；
5. PX UNKNOWN 零猜测；sa_\* 零裁决；
6. 完成后 STOP，裁决权在人工。

---

## 9. DoD（验收硬条件）

1. 157/157 表获得六态之一的显式状态 + 证据；CONFLICT=0 或全部升级 Human Gate 登记；
2. 每个候选完成三权 + 反证两问 + 事务/跨域读写画像；
3. 每个 PASS 候选满足 §4.3 全部要件且为 Candidate 级完整取证（非抽样）；
4. 每个 BLOCK/REFINE 候选写出反证记录 / 解除条件；
5. sa_\* 13 张保持 DEFERRED 零裁决；
6. Anti-Service §3.2 六项逐条核销（一致 / 修订+理由）；
7. 全程六零合规（可由 git status + 只读查询日志复核）；
8. 提交 `NG-1C-Final-Review.md` 后 STOP。

---

## 10. 产出物（目录 `.claude/evidence/jnpf-next-architecture/ng1c-domain-ownership/`）

| # | 文件 | 说明 |
|---|---|---|
| 1 | `table-capability-map.csv` | 157 表 × 六态 × 候选映射（脚本可复算） |
| 2 | `ownership-evidence.tsv` | 三权 + 六通道证据（file:line） |
| 3 | `capability-dependency-profile.md` | 跨域读写矩阵 / ACID 清单 / 权限租户耦合 |
| 4 | `domain-ownership-matrix.csv` | 域级三态 + 反证两问 + 解除条件 |
| 5 | `conflict-register.md` | CONFLICT / Human Gate 登记表 |
| 6 | `NG-1C-Final-Review.md` | 终审提交物（PASS/REFINE/BLOCK 三态建议 + STOP 声明） |

配套实施计划：`docs/superpowers/plans/NG-1C实施计划-v1.0.md`（C0–C4；**本规格与计划审核通过前不得启动 C0**）。
