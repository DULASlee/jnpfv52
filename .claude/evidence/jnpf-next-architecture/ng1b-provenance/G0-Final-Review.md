# G0 Final Review（NG-1B Provenance Matrix + REFINE 收口完成后）

**日期**：2026-08-26 ｜ **依据**：NG-1 规格 §0A.7（G0 终审三态 PASS / REFINE / BLOCK）+ §0A.8（REFINE 裁决新增方法论）
**输入**：`provenance-matrix.csv`（289 × 26 列，REFINE 收口后重生成）+ `provenance-matrix-report.md` + `ng1a-product-boundary/` 全部产出物
**性质**：只读审计（零业务代码修改 / 零数据库修改 / 零删除）

---

## 裁决结论（终态：✅ PASS —— 2026-08-26 人工终审）

> ### ✅ G0 = PASS（最终裁决已下达，NG-1C UNLOCKED）
>
> 人工终审批准：**NG-1B Provenance Matrix 收口通过，G0 = PASS**。
>
> **PASS 语义（裁决原文，永久有效）**：
> G0 PASS 仅表示 Provenance / Platform Boundary 已达到进入 Domain Ownership Proof
> 的证据门槛，**不代表已经证明任何微服务边界，也不代表现有模块/Service/表前缀就是领域边界**。
> Provenance 证明的是「这张表是什么资产、从哪里来、谁在使用」，
> 不是「这张表应该成为一个领域」。Domain Ownership 必须在 NG-1C 从 P0/P1 资产重新推导。
>
> 历史记录（保留）：REFINE 收口执行完毕——**P0 集合 UNKNOWN 清零（3 → 0）**，
> 24 张边界表的 Provenance Proof 全部收口；本轮未进入 Domain Ownership、
> 未恢复 D12、未进行任何微服务设计或实现。

---

## 1. REFINE 执行对照（人工裁决 7 条逐条核销）

| # | 裁决指令 | 执行结果 | 证据 |
|---|---------|---------|------|
| 1 | 补齐 P0/P1 剩余 PARTIAL/UNKNOWN 的 Creation Source、Code Owner、Write Path、Read Consumer、Runtime/Startup Impact 证据 | ✅ P0 UNKNOWN 3→0；P0/P1 剩余 18 张 PARTIAL 全部缺位已证明（file:line 或全库 0 引用扫描） | `provenance-matrix-report.md` §2.2/2.3/2.5 |
| 2 | SchemaVersions / undo_log 必须取得源码级 CodeFirst/Runtime 创建证据 | ✅ SchemaVersions = DbUp journal（`Program.cs` L17-22，dbup-core 5.0.87 默认 journal，2 行=2 脚本吻合）；undo_log = Seata-AT schema（`主库脚本.sql` L4235-4246），0 引用=惰性预留 | `_framework-evidence.tsv`（新增） |
| 3 | BASE_SANDBOX / BASE_USER_DEVICE / SYS_PROCESSED_EVENT 等逐表 file:line 访问证据 | ✅ BASE_SANDBOX：读 L18/19/33/36 + 写缺失证明（SandboxManager 纯内存）；BASE_USER_DEVICE：读 IMHandler L486 + 写缺失证明；SYS_PROCESSED_EVENT：写 L51-55 + 读 L35-37；SYS_EVENT_OUTBOX_MESSAGE：写 L20-29 + 读 L34-43 + API DeadLetterService | report §2.2/2.3/2.5 |
| 4 | zx_sys_db / zx_system_db 归属区分（运行时基础设施 vs 历史遗留） | ✅ zx_system_db = P0 平台运行时基础设施（实体 SystemDbEntity L9 + 读 ConfigController L235）；zx_sys_db = P6 LEGACY（无实体无访问 5 行，遗留副本） | CSV 降级 + report §2.4 |
| 5 | sa_* 保持独立登记，暂不做下一代 Domain Ownership 裁决 | ✅ 13 张独立登记为 dapper-first 形态（PARTIAL 正常形态归档），未做任何归属裁决 | report §2.5 |
| 6 | 确认四分类规则（Dapper-first / Template≠Domain / Demo / Legacy） | ✅ 正式写入 NG-1 规格 §0A.8.3，并新增 §0A.8.4 框架表规则 | 规格文档 §0A.8 |
| 7 | 禁止扩大范围 | ✅ 零业务代码修改 / 零数据库修改 / 零微服务设计 / 未恢复 D12 / 未进入 Domain Ownership | 本表 + 只读审计声明 |

**重新生成产出物**：`provenance-matrix.csv` ✅（PROVEN 161 / PARTIAL 75 / UNKNOWN 53）｜ `provenance-matrix-report.md` ✅ ｜ 本文件 `G0-Final-Review.md` ✅

---

## 2. 铁证汇总（全部可复算）

| 集合 | 张数 | 定性 | 证据强度 |
|------|-----:|------|---------|
| ext_* | 19 | **P3 DEMO_APPLICATION**（非 Order Domain，D12 切片证伪） | 铁证：菜单实测 62 条 extend.* 中 48 条 Demo 菜单 + 前端 17 个演示目录 + Seed 测试数据 |
| WFORM_* | 48 | **P2 PRODUCT_TEMPLATE**（OA 模板族） | 铁证：48 张实测 0 行 + generator.flowForm 入口 + IsSysTable 清单 |
| WM_*/WH_* | 42 | **P6 LEGACY**（含真实客户数据，禁止删除） | 铁证：WM_BillDetail 1629 行 / WM_Material 739 行 / WH_BasicData 208 行 |
| sa_* | 13 | **P1 LOWCODE_RUNTIME**（dapper-first 独立登记） | 铁证：backend Dapper 写 L760/L200/L415/L312 + 读 L37/L67/L34/L41 + sa-service 13 文件 |
| 框架表 | 2 | **P0 FRAMEWORK**（SchemaVersions / undo_log） | 源码级：DbUp journal + Seata-AT 预留 schema |
| 事件基础设施 | 2 | **P0 PROVEN**（SYS_EVENT_OUTBOX_MESSAGE / SYS_PROCESSED_EVENT） | file:line 写读 + API 暴露 |
| 降级 | 2 | **P6 LEGACY**（zx_sys_db / PROCESSED_EVENT） | 无访问/命名不一致废止副本 |
| P1 | 11 | 全部 LOWCODE_RUNTIME | **100% PROVEN** |
| PX | 6 | **保持 UNKNOWN，零猜测** | ✓ 纪律遵守：2 PARTIAL / 4 UNKNOWN |

**三态统计（脚本驱动，可复算）**：PROVEN 161 / PARTIAL 75 / UNKNOWN 53，覆盖 289 / 289。
**P0/P1 PROVEN 率**：139 / 157 = **88.5%**；**P0 UNKNOWN = 0** ✅

**禁止项核查（§0A.7 + 本轮裁决第 7 条逐条）**：

| 禁止项 | 状态 |
|--------|------|
| 不进入 Domain Ownership Proof | ✓ 未进入 |
| 不恢复 D12 | ✓ 未恢复（维持证伪暂停状态） |
| 不进行微服务设计 | ✓ 未进行 |
| 不修改数据库 | ✓ 零修改（只读查询） |
| 不修改业务代码 | ✓ 零修改 |
| 不删除 42 张孤儿表 | ✓ 零删除（仅 ARCHIVE 建议） |
| PX UNKNOWN 不得猜测归属 | ✓ 零猜测 |

---

## 3. 方法论原则（本轮正式入规）

五否定原则 + 七分支资产谱系 + 四分类规则 + 战略顺序锁定，已写入 NG-1 规格 **§0A.8**，为 NG 架构方法论的永久原则：

```text
数据库表
   ├── Platform Core Data（P0）├── Platform Runtime Data（P1）
   ├── Product Template（P2）├── Demo / Sample（P3）
   ├── Customer Business Data（P4）├── Legacy（P6，可归档禁删除）
   └── Unknown（PX，无证据不进架构决策）

战略顺序：Provenance → Platform Boundary → Domain Ownership → Data Ownership
        → Transaction Boundary → Query Boundary → Migration Proof → 微服务裁决
```

**本轮最大成果**：之前「Order 微服务切片」被 Provenance 证伪（ext_* = Demo 资产）——不是失败，而是 NG-1 最有价值的发现，直接避免「看到表 → 画微服务 → 拆数据库」的不可控工作量。

---

## 4. 人工裁决清单（随 STOP 一并提交）

| # | 裁决项 | 工程师建议 | 状态 |
|---|--------|-----------|------|
| 1 | G0 最终裁决 | **PASS**（REFINE 收口证据已闭合：P0 UNKNOWN=0，P0/P1 88.5% PROVEN，18 张 PARTIAL 缺位全部已证明） | ✅ **PASS（2026-08-26 人工终审批准）** |
| 2 | ext_* 19 张处置 | P3 Demo 包化：从平台默认初始化剥离为独立 Demo 安装包 | 非本批执行 |
| 3 | WFORM_* 48 张处置 | P2 模板包化：独立模板包 | 非本批执行 |
| 4 | WM_*/WH_* 42 张处置 | ARCHIVE（含真实数据，禁止删除） | 非本批执行 |
| 5 | zx_sys_db / PROCESSED_EVENT | ARCHIVE（遗留副本） | 已降级 P6，删除禁令有效 |
| 6 | sa_* 13 张归属 | 下一代 Domain Ownership 阶段再裁决（dapper-first vs Node 双端） | 延后 |
| 7 | P0/P1 18 张 PARTIAL | 接受「已证明缺位」归档（不阻塞 G0 PASS） | ✅ 已确认归档 |
| 8 | 下一阶段 | **NG-1C — Platform Domain Ownership Proof**（规格+计划已批准落盘；C0 待两文件审核后启动） | 🔓 UNLOCKED |

---

## 5. STOP 声明与状态快照

本批（NG-1B Provenance Matrix + REFINE 收口）到此完成。**停止并等待人工裁决。**

| 状态项 | 值 |
|--------|-----|
| G0 | ✅ **PASS**（2026-08-26 人工终审） |
| NG-1B | ✅ **PASS** |
| NG-1C Platform Domain Ownership | 🔓 **UNLOCKED**（规格+计划落盘，C0 待审核后启动） |
| Domain Ownership 裁决 | 🔒 等 NG-1C Proof |
| Microservice Design | 🔒 **BLOCKED** |
| Aspire Architecture | 🔒 **BLOCKED** |
| D12 Order Slice | **暂停（已证伪）**，不恢复 |
| S2 | 🔒 **BLOCKED** |

**PASS 附带锁定（裁决原文）**：从现在起禁止「表 → Domain → Microservice」直接推导；推导链必须为 Asset Provenance → Platform Boundary → Business Capability → Domain Ownership → Transaction Boundary → Query Boundary → Migration Boundary → Service Boundary，任何一步无证据不得跳步。Aspire 仍只是未来实现/编排工具，不参与 Domain Boundary 判断。
