# NG-0 Final Review — JNPF Next Generation 可行性与总体架构设计

**日期**：2026-08-25 ｜ **结论**：**NG-0 完成（只读设计，零代码）**——建议 **APPROVE**（进入 NG-1 原型）或 REFINE（人工指定补充范围）；不自动进入 NG-1

## 一、交付清单（全部落盘）

| 类 | 数量 | 文件 |
|----|:----:|------|
| 规格 | 5 | 数据库架构 / 领域与模块边界 / 数据访问架构 / 租户权限架构 / Aspire与运行架构 |
| 证据 | 10 | database-inventory / relationship-map / query-hotspots / data-ownership-map / domain-candidates / modular-monolith-vs-microservices / aspire-evaluation / legacy-compatibility-map / migration-strategy / cost-and-risk-estimate |
| 计划 | 1 | JNPF-Next-NG0-架构设计计划 |
| 数据底账 | 8 | db-*.tsv（289 表全量/索引/FK/类型/审计/前缀聚类） |

## 二、核心发现（数据驱动）

| 维度 | 发现 | 裁决 |
|------|------|------|
| 数据库 | 289 表/6134 列；**FK 仅 14**（275 表零外键=隐式关系单体）；租户列**三风格**（219 表）；主键 nvarchar 77%；JSON 大字段 161 表；**无 base_tenant（租户注册在 zx_sys_db）** | 不补 FK，改显式关系注册表；租户列统一为单一契约（W8） |
| 领域 | 12 候选域（D1-D12）**不沿用项目目录**（inteAssistant 116 文件跨 3 域） | Modular Monolith 程序集划分 + 架构测试强制 |
| 事务 | 全部候选域**单库事务** | 形态 A（Modular Monolith）首选；微服务为远期（证据否决第一形态） |
| 权限 | 三连查每查询执行；条件注入四段链；双路径 76 特征已锁定 | 权限评估 API + 快照；33+43 特征为等价基线 |
| Legacy 资产 | Q1-Q11/E1-E4/E-PB1~4 全量裁决 | KEEP 7 / REDEFINE 11 / REMOVE 6 / DEPRECATE 3（含表级） |
| Aspire | 工具层定位 | 采用（编排+遥测）；Broker 不引入（出箱表+进程内总线） |
| 迁移 | 8 波次（W1 字典/File → W8 租户统一） | 沙盘先行（D12 Order——路径 B 唯一消费者）；总量 65-100 人周 |

## 三、确定性自评

[KNOWN] HIGH：DB 元数据全部实测（289 表/6134 列/租户列分布/FK）；查询热点代码审计。
[INFERRED] MED-HIGH：12 域聚类（三角验证）；[MED]：单库事务边界（NG-1 补 TransactionScope 扫描）。
[GUESS] LOW-MED：工作量估算（NG-1 后校准）。

## 四、边界遵守声明

- ✅ 只读：零业务代码/零数据库变更/零 API 变更/零 UniApp 变更/零迁移/零部署；
- ✅ 分析顺序合规（业务能力→领域→ownership→事务→DB→模块→API→单体→选择性服务→Aspire）；
- ✅ 未做微服务设计（形态 C 冻结）；未机械按文件数生成任务；未修任何 Legacy 怪异；
- ✅ P0-C DEFERRED / S2 BLOCKED / P1 DEFERRED 状态保持。

## 五、闸门（人工裁决）

```text
NG-0 ──► REJECT（回 Legacy 路线）
    ├──► REFINE（补充设计：人工指定范围）
    └──► APPROVE（进入 NG-1：原型阶段——另行计划+批准）
```

**NG-0 已 APPROVE；NG-1 已于 2026-08-26 获人工有条件批准**（范围 = Domain & Data Ownership Proof + D12 Slice 证伪；BOUNDARY-PROOF 闸门 + 反证机制 + PASS/REFINE/BLOCK 三态已写入 NG-1 规格 §0/§2.4/§5 与 D12 计划 §4/§4.1）。

**2026-08-26 架构纠偏裁决**：第一批实测（WM/WH 42 孤儿表 + ext_* 12 子域打包）触发 **NG-1A：Platform Product Boundary Audit**（ProductAssetClass 十类 + G0 闸门，见 NG-1 规格 §0A）；**D12 Order Slice ⏸ SUSPENDED**（改名 Candidate Slice）。状态表：

```text
D1 ✅ CLOSED ｜ Backend S1 Audit ✅ CLOSED ｜ P0-A ✅ APPROVED ｜ P0-B ✅ APPROVED/BASELINED
P0-C ⏸ DEFERRED ｜ S2 🔒 BLOCKED ｜ P1 🔒 BLOCKED ｜ NG-0 ✅ COMPLETE/APPROVED ｜ NG-1 ▶ APPROVED（有条件）
NG-1 第一批 ✅ 完成 ｜ D12 Candidate Slice ⏸ SUSPENDED ｜ NG-1A ▶ 启动（G0 闸门前置）｜ NG-2 ⏸ 未启动
```

**2026-08-26 NG-1A 完成 + 资产模型升级 + G0 条件 PASS 裁决**（规格 §0A.6/§0A.7）：
- 分类体系升级为 **P0-PX 十类 + 二维（PlatformRole × AssetLifecycle）**；Template ≠ Platform Domain；OrderService 四种可能建模；
- 289 表全分类实测：**平台资产 159（P0 148 + P1 11）/ 非平台 130（P2 48 + P3 25 + P4 5 + P6 45 + P7 1）/ PX UNKNOWN 6**；
- G0 人工裁决：**`PASS-PENDING-PROVENANCE`**（不最终 PASS）；批准启动 **NG-1B Provenance Matrix**（289 表 × 14 维，每表 PROVEN/PARTIAL/UNKNOWN）；
- 状态表：

```text
NG-1A ✅ 完成（6 产出物）｜ G0 ⏳ PASS-PENDING-PROVENANCE ｜ NG-1B ▶ 启动（Provenance Matrix）
D12 Candidate Slice ⏸ SUSPENDED（禁恢复）｜ Domain Ownership Proof 🔒 未解锁 ｜ NG-2 ⏸ 未启动
```

**2026-08-26 NG-1B Provenance Matrix 完成 + G0 Final Review = REFINE（待人工裁决）**：
- 289 表 × 14 维全覆盖，三态脚本驱动：**PROVEN 157 / PARTIAL 77 / UNKNOWN 55**（P0/P1 核心 PROVEN 率 84.9%，P1 100%）；
- 核心定性铁证：ext_* 19 = P3 Demo / WFORM_* 48 = P2 模板 / WM_WH_* 42 = P6 LEGACY（WM_BillDetail 1629 行实测）/ sa_* 13 = P1 / PX 6 零猜测；
- 未闭合：P0 24 张边界表（21 PARTIAL + 3 UNKNOWN）→ 建议 REFINE 窄范围补强（框架证据 2 + 访问证据 ≤10 + zx_sys_db 降级裁决 + 判定原则 3 项确认）；
- 状态表：

```text
NG-1B ✅ 完成（3 产出物：matrix.csv / report / G0-Final-Review）｜ G0 ⏳ PASS-PENDING-PROVENANCE（建议 REFINE）
D12 Candidate Slice ⏸ SUSPENDED（禁恢复）｜ Domain Ownership Proof 🔒 未解锁 ｜ NG-1C ⏸ 未批准（Platform Independence Proof 候选）
```

## 六、提交

- 新增：5 规格 + 1 计划 + 10 证据 + 8 数据底账（`.claude/evidence/jnpf-next-architecture/`）；
- 零代码改动；独立提交可整体 revert。
