# JNPF 后端重构与 Aspire 微服务化总体实施计划

**版本**：v1.0 ｜ **状态**：MASTER / LONG-LIVED ｜ **日期**：2026-08-26
**上位规格**：《MASTER-JNPF后端重构与Aspire微服务化总体设计规格》（P1–P6 / 八禁 / PHASE 定义以该规格为准）
**纪律**：以后所有 Task、Slice、Incident、Refine 均从本计划**派生**，不再新增主线编号。

---

## ⚡ 路线记忆卡（与总规格 §0 同卡，任何 Agent 每次开工前必读）

> **我们不是重新设计 JNPF。我们是在现有 JNPF 上进行拆分和优化。**
> `Baseline → Platform Boundary → Modularization → Physical Decomposition → Contract → Aspire → Microservices → Data Isolation`
> 不重新设计数据库 ｜ 不预设 Domain ｜ 不预设微服务数量 ｜ 不因 Aspire 改变架构边界 ｜ 不删除未知/遗留数据 ｜ 不让 Demo/Template/Customer Data 污染判断 ｜ 先模块化再服务化 ｜ 数据库隔离最后按证据决定 ｜ AI 执行，人类裁决 ｜ 每阶段 PASS/REFINE/BLOCK 后停止

---

## 1. 主线路线图（以后只看这一张图）

```text
                    JNPF Backend Refactoring
                              │
                              ▼
                    ┌──────────────────┐
                    │ PHASE 0 Baseline │
                    └────────┬─────────┘
                             ▼
                ┌─────────────────────────┐
                │ PHASE 1 Platform        │
                │ Boundary / Asset        │
                │ Classification          │
                └────────────┬────────────┘
                             ▼
                ┌─────────────────────────┐
                │ PHASE 2 Modularization  │
                │ Module & Dependency Map │
                └────────────┬────────────┘
                             ▼
                ┌─────────────────────────┐
                │ PHASE 3 Physical        │
                │ DLL / NuGet / Project   │
                │ Decomposition           │
                └────────────┬────────────┘
                             ▼
                ┌─────────────────────────┐
                │ PHASE 4 Contract        │
                │ Decoupling              │
                └────────────┬────────────┘
                             ▼
                ┌─────────────────────────┐
                │ PHASE 5 Aspire          │
                │ Enablement              │
                └────────────┬────────────┘
                             ▼
                ┌─────────────────────────┐
                │ PHASE 6 Progressive     │
                │ Microservice Extraction │
                └────────────┬────────────┘
                             ▼
                ┌─────────────────────────┐
                │ PHASE 7 Data            │
                │ Physical Isolation      │
                └─────────────────────────┘
```

---

## 2. 当前位置判定（诚实盘点，2026-08-26）

```text
PHASE 0  Baseline            ◑ 部分完成（Build/Test/复杂度基线已有；Runtime/Compatibility/Legacy Registry 未定稿归档）
PHASE 1  Platform Boundary   ◕ 主体已完成（NG-1A/1B：289→157/132 + Provenance），待收口为 Platform Asset Inventory 定稿
PHASE 2~7                    ⏸ 未启动（既有研究文档仅为待复核输入，见总规格 §10）

▶ 当前任务 = PHASE 0 收口 ∥ PHASE 1 收口 → 完成后进入 PHASE 2
```

---

## 3. 第一批工作包（Task 提案——**批准后生效，未批准不动工**）

| # | Task | 内容 | 验证方式 |
|---|---|---|---|
| T0.1 | Build/Test Baseline 定稿 | `dotnet build -c Release`（backend）、`dotnet test backend/zx_lowcode_netcore.sln`、`node scripts/verify-toolchain.mjs`、`node scripts/test-hooks.mjs` 结果归档为基线快照 | 全绿 + evidence 归档 |
| T0.2 | Runtime & Compatibility Baseline | `start-dev.ps1` 冒烟 + `jnpf-api.mjs GET /api/oauth/CurrentUser` + `E2E_PIPELINE_ID=311 pnpm test:api` 快照；关键 API 行为清单登记 | 冒烟全绿 + 快照存档 |
| T0.3 | Legacy Compatibility Registry 初版 | 由 `legacy-compatibility-map.md` 升格为正式登记册（KEEP/REDEFINE/DEPRECATE/REMOVE 四态） | 覆盖率核对 |
| T1.1 | Platform Asset Inventory 定稿 | 合并 ng1a `platform-asset-classification.csv` + ng1b `provenance-matrix.csv` → 单一定稿清单（157 进入 / 132 处置建议冻结表）；**零删除零修改 DB** | 行数复算 289=157+132；三态统计可复算 |
| T1.2 | PHASE 1 Final Review | 提交 Inventory + 处置冻结表 → 三态裁决 → STOP | 人工签收 |

## 4. Task 卡模板（九段强制格式）

```text
Task-{Phase}.{序号}-{短名}
├── Objective        ：一句话目标
├── Scope            ：改什么/不改什么（含"不碰"清单）
├── Preconditions    ：前置 Gate/依赖
├── Evidence         ：开工前事实依据（file:line / 命令输出）
├── Implementation   ：步骤
├── Verification     ：验证命令与预期
├── Regression       ：回归面（哪些测试必须仍绿）
├── Artifacts        ：产出物路径（evidence 目录）
└── Human Gate       ：停点与裁决人
```

---

## 5. 各 PHASE 执行卡（Entry / Exit / Verification / Gate）

| Phase | Entry 条件 | Exit 条件（全部满足才三态收口） | 验证手段（映射仓库工具链） |
|---|---|---|---|
| 0 Baseline | 本计划获批 | 六类基线齐备并归档：Architecture / Build / Test / Runtime / Compatibility / Legacy Registry | `dotnet build -c Release`、`dotnet test`、CI gate 本地复跑、toolchain/hooks 校验、API 冒烟快照 |
| 1 Platform Boundary | T1.1 完成 | Platform Asset Inventory 定稿 + 132 张处置冻结表签收；**这不是 Domain Design** | CSV 行数复算（289=157+132）；Provenance 三态统计重跑一致 |
| 2 Modularization | PHASE 1 PASS | Module Map + Dependency Map（Project/Namespace/Service/Repository/Entity/API/Event/Dependency/Transaction/Permission/Tenant 十一维）；模块候选集获人类批准 | `scripts/arch-module-dependency-scan.ps1` 只读扫描 + Serena/Codegraph 调用链取证；零业务代码修改 |
| 3 Physical Decomposition | Module Map 批准 | 逻辑模块→物理 DLL/NuGet/Project 分波完成；九项治理清零或显式豁免登记 | 每波次 `dotnet build /p:CI_BUILD=true` 零 error JNPF\*、ARCH tests 绿、循环依赖扫描通过 |
| 4 Contract Decoupling | PHASE 3 PASS | 跨模块引用仅经 Public Contract（Interface/API/Event）；消费者依赖能力不依赖实现 | 跨模块直引扫描=0（豁免表除外）；回归测试全绿 |
| 5 Aspire Enablement | PHASE 4 PASS | AppHost 统一运行/配置/依赖/观测入口可用；**第一目标不是拆服务** | AppHost 一键起、dashboard/telemetry 可见、现有 E2E 冒烟不回退 |
| 6 Progressive Service Extraction | PHASE 5 稳定运行 | 仅达标模块过 Service Candidate Gate → Module→Service→Aspire Resource；**未过者留 Monolith（合法结论）** | Gate 十条件逐项证据：Ownership/Contract/Runtime/配置/权限/Tenant/Transaction/Cross-domain Query/Failure Boundary/Migration |
| 7 Data Isolation | 服务化稳定 | Shared DB→Logical Ownership→Migration→Shadow Verification→Independent DB→Cutover；十问全答（总规格 §4） | 影子校验报告 + 回滚方案 + 人类 cutover 批准 |

---

## 6. 停机与升级规则（何时必须 STOP）

1. 任一阶段收口为 REFINE/BLOCK —— STOP 等人工；
2. 收到与八禁冲突的需求 —— 立即停止并请求人工裁决；
3. 出现跨 PHASE 越界冲动（如 PHASE 2 期间想拆 DLL、PHASE 5 期间想拆服务）—— 停止，登记为下阶段候选；
4. 任何涉及数据删除 / 数据库拆分 / Domain 重命名 / 微服务数量的决策 —— Evidence→Recommendation→Human Decision→Freeze→Execution，AI 不得自行决定。

## 7. 风险登记（Top 4）

| 风险 | 对策 |
|---|---|
| 分布式单体（拆了服务共享 DB） | P5 顺序铁律 + PHASE 7 前禁独立 DB |
| 兼容性回归（重构破坏行为） | PHASE 0 Compatibility Baseline 为每波次回归底座；特征测试先行 |
| 双轨期配置漂移 | PHASE 5 起 Aspire 统一配置源；旧 Configurations 冻结只读 |
| AI 自作主张越权 | P6 + 八禁 + 每 Task Human Gate |

## 8. 修订控制

- 主线路线图、PHASE 定义、Gate 条件：仅人类裁决可修订；
- Task 级内容：经所属 PHASE 的 Human Gate 修订；
- 所有修订记录版本史，禁止静默改写。

## 9. 版本历史

| 版本 | 日期 | 变更 |
|---|---|---|
| v1.0 | 2026-08-26 | 首版。PHASE 0–7 执行卡、第一批工作包提案（T0.1–T1.2）、Task 九段模板、停机规则 |
