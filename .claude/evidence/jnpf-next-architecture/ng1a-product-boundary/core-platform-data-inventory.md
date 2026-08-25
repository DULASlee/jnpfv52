# NG-1A 产出物 2 — Core Platform Data Inventory（平台核心数据清单）

**日期**：2026-08-26 ｜ **方法**：ProductAssetClass P0-PX 判定 v2（§0A.6 升级裁决：P0-PX 十类 + PlatformRole×AssetLifecycle 二维；实体映射 + init 脚本 + 代码引用 + DB 元数据四证据链）｜ **完整明细**：`platform-asset-classification.csv`

## 0. 总览

| ProductAssetClass | 表数 | 占比 | PlatformRole × Lifecycle | 是否进入 NG 架构设计 |
|-------------------|-----|------|--------------------------|---------------------|
| **P0 PLATFORM_CORE** | 148 | 51.2% | CORE × MANDATORY | ✅ 唯一允许进入领域/Ownership 分析 |
| **P1 LOWCODE_RUNTIME** | 11 | 3.8% | RUNTIME × MANDATORY | ✅ 唯一允许进入领域/Ownership 分析 |
| P2 PRODUCT_TEMPLATE | 48 | 16.6% | PRODUCT_CONTENT × TEMPLATE | ❌ 独立模板包，不进 Platform Core |
| P3 DEMO_APPLICATION | 25 | 8.7% | PRODUCT_CONTENT × DEMO | ❌ 可删除/隔离，不进新架构 |
| P4 CUSTOMER_APPLICATION | 5 | 1.7% | EXTERNAL × CUSTOMER_GENERATED | ❌ 不进入平台核心 |
| P5 TEST_FIXTURE | 0 | 0% | — | ❌ 清理或隔离 |
| P6 LEGACY | 45 | 15.6% | LEGACY × LEGACY | ❌ 归档/迁移/清理 |
| P7 ORPHAN | 1 | 0.3% | LEGACY × ORPHAN | ❌ 隔离 |
| P8 EXTERNAL | 0 | 0% | — | ❌ 不进入平台核心 |
| PX UNKNOWN | 6 | 2.1% | UNKNOWN × UNKNOWN | ⏸ BLOCKED 直至证明 |

> **回答 NG-1 核心问题**：把 Demo/模板/历史/客户表拿掉后，平台本身 = **159 张**（P0 148 + P1 11）= 289 的 **55.0%**。旧口径「155+4」作废（基础设施 4 张已并入 P0）。**289 → 159，规模缩小 45%**，非平台资产 130 张不迁移、不进架构设计。

## 1. P0 PLATFORM_CORE（148 张）——平台自身不可缺数据

= 旧 CORE_PLATFORM 144 张 + 基础设施 4 张（§0A.6 升级：基础设施属「平台自身必须存在的数据」并入 P0）。

按前缀族（全部有实体映射 + init 脚本 + 活跃代码引用）：

| 族 | 张数 | 内容 | 对应 NG-0 候选域 |
|----|-----|------|-----------------|
| base_ | 98 | 用户/角色/组织/岗位/权限/菜单/按钮/列/字典/门户/消息/IM/通知/日志/日程/打印/省市/数据接口/系统配置/三方同步等 | D1-D3, D6-D9, D11 |
| flow_ | 18 | 流程引擎（表单/模板/任务/候选人/委托/事件日志/可见性） | D4 |
| sa_ | 13 | Studio 需求分析九步产物表（assumptions/consistency/dfd/er/pspec/state_machine/ui/scope 等） | D10 |
| ai_ | 8 | AI 管道核心（entity_field/projects/ir_events/route_table/skill_runs/seed_templates 等） | D10 |
| zx_ | 3 | 租户注册（zx_sys_db 等，NG-0 [KNOWN] 证据） | D2 |
| inte_ | 2 | 智能助手附件/交付物 | D10 |
| eval_ | 1 | 评估指标 | D10 |
| sys_ | 1 | 差异日志（SYS_DIFF_LOG） | D9 |
| 基础设施 | 4 | UNDO_LOG / SCHEMAVERSIONS / PROCESSED_EVENT / SYS_EVENT_OUTBOX_MESSAGE | 框架层 |

**关键证据**：AI_/SA_/BASE_AI_/BASE_IR_/BASE_KNOWLEDGE_/BASE_SANDBOX/BASE_INTEGRATE_* 等约 25 张**不在 init 脚本**（代码 CodeFirst/迁移自建）+ 代码引用活跃 → 平台运行时自建自管 = 核心证据。sa_* 13 张被 backend（Dapper）+ sa-service（Node，196 处引用）双端读写。

## 2. P1 LOWCODE_RUNTIME（11 张）——低代码运行时元数据

| 族 | 张数 | 内容 |
|----|-----|------|
| BASE_VISUAL_DEV / _LINK / _RELEASE | 3 | 在线开发项目定义与发布 |
| BLADE_VISUAL_* | 8 | 数据大屏在线配置（category/component/config/db/glob/map/record） |

> 说明：MT_* 动态业务表**不属**此列——它们是「用户通过平台创建的业务数据」→ P4 CUSTOMER_APPLICATION（排除）。P1 仅保留「运行时自身元数据」。

## 3. 非平台资产（130 张）——不迁移、不进 NG 设计

| 类 | 张数 | 关键证据 |
|----|-----|---------|
| P2 PRODUCT_TEMPLATE | 48 | WFORM_* 示例表单（OA 模板：合同/差旅/报销/付款/出入库等），无业务代码，唯一引用 = DataBaseService.cs L485 备份清单 |
| P3 DEMO_APPLICATION | 25 | ext_* 19（12 子域演示服务）+ demo_* 3 + WFORM 演示 3；ext_* 登记 P2-template-candidate 待 Provenance Matrix 细分 |
| P4 CUSTOMER_APPLICATION | 5 | MT_* 数字后缀动态表（用户在线开发创建，init 打包历史用户数据） |
| P6 LEGACY | 45 | WM/WH 39（行数据真实但代码零引用）+ BAK/BASE_FILE/KG_*2/STUDENT/DOMAIN_MODEL |
| P7 ORPHAN | 1 | BASE_VISUAL_FILTER（无 init 无实体 0 引用，彻底孤儿） |
| PX UNKNOWN | 6 | 报表 4 张（独立前端 jnpf-web-datareport）+ 租户词表 2 张 |

详见 `demo-sample-legacy-registry.md`。**全部不删除、不修改**（硬规则 8）。

## 4. 与第一批 Ownership Matrix 的衔接

- 第一批 `ownership-matrix-v1.csv` 的六维矩阵**仅对 159 张 P0/P1 平台资产继续有效**；
- 96 张 UNKNOWN 裁决表经本批重分类后：42 孤儿 → P6 LEGACY；54 TBD → 逐表落位（TBD-Base 31 → P0 或 P6；TBD-Job/Integration/Platform → P0）；
- 硬规则 2 遵守：P2/P3/P4/P6/P7 不得参与 Domain Ownership Proof。
