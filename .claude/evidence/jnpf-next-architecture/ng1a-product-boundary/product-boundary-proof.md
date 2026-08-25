# NG-1A 产出物 5 — Product Boundary Proof（G0 初步结论）

**日期**：2026-08-26 ｜ **分类体系**：P0-PX v2（§0A.6 升级裁决）｜ **性质**：初步结论（**非 G0 最终裁决**，等待人工裁决）｜ **依据**：289 表四证据链实测

---

## 1. 核心问题回答

> **如果把所有 Demo、示例、模板、历史业务表从数据库中拿掉，JNPF 这个低代码平台本身究竟还剩下哪些数据？**

**实测答案（v2 口径）：159 张**（P0 PLATFORM_CORE 148 + P1 LOWCODE_RUNTIME 11）= 289 张的 **55.0%**。

```text
289 tables
│
├── P0 PLATFORM_CORE 148（51.2%）  ✅ 平台自身（144 功能 + 4 基础设施）→ 进入 NG 设计
├── P1 LOWCODE_RUNTIME 11（3.8%）  ✅ 低代码运行时元数据 → 进入 NG 设计
│
├── P2 PRODUCT_TEMPLATE 48（16.6%） ❌ 产品模板内容 → 独立模板包，不进 Platform Core
├── P3 DEMO_APPLICATION 25（8.7%）  ❌ 官方演示应用 → 可删除/隔离（ext_* P2 候选待证）
├── P4 CUSTOMER_APPLICATION 5（1.7%）❌ 用户动态业务表 → 不进平台核心
├── P6 LEGACY 45（15.6%）           ❌ 历史遗留（WM/WH 39 + 6）→ 归档/迁移/清理
├── P7 ORPHAN 1（0.3%）             ❌ 彻底孤儿 → 隔离
└── PX UNKNOWN 6（2.1%）            ⏸ 人工裁决（报表 4 + 租户词表 2）→ BLOCKED
```

## 2. 关键证明结果

| # | 命题 | 结果 | 证据 |
|---|------|------|------|
| P1 | WM/WH 是「Warehouse Domain」？ | **证伪** → P6 LEGACY（历史污染样本） | 39 张：实体 0 + 引用 0 + init 39/39 + 行数据真实 |
| P2 | ext_* 是「平台 Order 领域」？ | **证伪** → P3 DEMO（P2 模板候选待证） | 12 子域演示服务 + 业务语义非平台功能 + §0A.6.4 四种可能排除①④ |
| P3 | 「数据库有 Order/Product/Warehouse 表 ⇒ 平台有这些领域」？ | **证伪**（硬规则 4 + §0A.6.1 Template ≠ Platform Domain） | 130 张业务表全部为非平台资产 |
| P4 | WFORM_* 是平台功能表？ | **证伪** → P2 PRODUCT_TEMPLATE | 48 张唯一引用 = 备份清单数组（DataBaseService.cs L485） |
| P5 | 无实体映射 ⇒ 无代码？ | **证伪**（方法修正） | AI_/SA_ 25+ 张用 Dapper/双端（backend+sa-service 196 处）——「无 SugarTable 实体」≠「无代码引用」 |
| P6 | AI/SA Studio 表是平台核心？ | **成立** → P0 PLATFORM_CORE | 无 init（代码自建）+ 双端活跃读写 |
| P7 | 平台核心数据规模远小于 289？ | **成立** | 159/289 = 55.0%（去掉 45% 非平台资产） |

## 3. G0 状态（2026-08-26 人工裁决）

```text
G0 = Product Boundary Proof
├── 289 表 P0-PX 全覆盖 ✅（0 表无分类；P5/P8 当前 0 张）
├── 二维分类（PlatformRole × AssetLifecycle）全表输出 ✅
├── 硬规则 1-8 全部遵守 ✅
├── 四证据链可追溯 ✅（file:line 级样本见 asset-provenance-map.md §3）
└── 人工裁决（§0A.7）：PASS-PENDING-PROVENANCE（不最终 PASS）
```

**裁决记录（§0A.7，2026-08-26）**：NG-1A 验收通过；G0 不最终 PASS，登记 **`PASS-PENDING-PROVENANCE`**。已完成「平台资产 vs 非平台资产」第一轮边界证明（289 → 159/130/6），但尚未完成全部 Provenance Proof → **不得进入 Domain Ownership Proof、不得恢复 D12**。已批准启动 NG-1B Provenance Matrix（只读审计），完成后 G0 Final Review 仅允许 **PASS/REFINE/BLOCK**。

**NG-1B 聚焦（§0A.7）**：
- 289 表 × 14 维（DB Object / Creation Source / Code Owner / Write Owner / Read Consumers / API / UI-Menu / Template / Demo / Runtime / Startup / Product / Lifecycle / Provenance）
- 每表终态：`PROVEN` / `PARTIAL` / `UNKNOWN`（证据驱动，非主观判断）
- 优先追踪 ext_* / WFORM_* / WM_* / WH_* / base_* / sa_*
- 禁止项：不恢复 D12、不微服务设计、不删孤儿表（仅 Provenance + 处置建议）、PX 不猜测归属、不真删生产库

**遗留待裁决（NG-1B 完成后连同 G0 Final 一并提交）**：
1. ext_* 19 张 P2/P3 细分（OrderService 四种可能 §0A.6.4）
2. PX UNKNOWN 6 张处置（报表 4 张 P0/P8？租户词表 2 张 P0/P7？）
3. 130 张非平台资产归档策略（ARCHIVE/DELETE/MIGRATE/DEMO/LEGACY 建议随 NG-1B 产出）

## 4. 对 NG 架构设计的直接含义

- 新架构**不应围绕旧库 289 张表设计**，只围绕 **159 张 P0/P1 平台资产**；
- 第一批 D12 Candidate Slice（ext_* Order）**不再具备 Architecture Slice 资格**（演示/模板资产，§0A.6.1），但其「真实链验证方法」（路径 B 权限、三表 Join、事务扫描）可作为方法论迁移到平台资产切片（如 flow_ 流程域）；
- 微服务拆分工作量与边界按 159 张重新评估（原 289 张口径作废）；
- 非平台资产 130 张的「模板/演示/遗留」身份建立后，**平台领域模型不受订单/商品/库存等业务语义污染**。
