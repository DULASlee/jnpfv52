# JNPF 主线正式确认（v1.0）——NG-1C SUPERSEDED，PHASE 0–8 为唯一执行路线

> **本文件由项目总基线《JNPF 平台整体结构基线》v0.2 (2026-08-26) 升级确认。**
> **NG-1C「Platform Domain Ownership Proof」已 SUPERSEDED，不再作为工程执行路线。**

---

## 1. NG-1C 状态：SUPERSEDED

| 项目 | 状态 | 依据 |
|------|------|------|
| NG-1C 大矩阵 | ⛔ SUSPENDED / ROUTE CONVERGED | `NG-1C实施计划-v1.0.md` 第1行 |
| NG-1C 作为独立工程 | ❌ 已终止 | `JNPF平台整体结构基线.md` §一③ |
| NG-1C 方法论 | ↓ 降级为参考方法 | 其 "三权取证框架" 已沉淀至总基线引用体系 |
| 任何 Agent 执行 NG-1C | ❌ 严禁 | `NG-1C实施计划-v1.0.md`：`任何 Agent 不得执行本计划` |

**NG-1C 已产生的证据全部保留**（289 表资产分类、Platform/Demo/Templates/Legacy 区分、Provenance、Access Map 等），作为历史侦察档案，不再延伸新编号，不再作为设计基础。

---

## 2. 确认的主线：PHASE 0–8

```text
PHASE 0  项目基线          ✅ 已送审（本文件）
       ↓
PHASE 1  平台资产识别      ✅ 已完成（289→157平台+132非平台）
       ↓
PHASE 2  平台能力地图      ✅ 已完成（A–E 五类 + 核心闭环）
       ↓
PHASE 3  数据架构设计      ▶ 当前入口 —— 产出《JNPF Next 数据架构与数据库设计规范 v0.1》
       ↓                     （产出后 STOP，人类批准才进入实施）
PHASE 4  模块架构设计      ⏸ 待 PHASE 3 输出 + 人工审批
       ↓
PHASE 5  首个 Vertical Slice ⏸ Identity/File 待裁决
       ↓
PHASE 6  模块化单体建设    ⏸ 按 Module 逐个建设 + 架构测试强制边界
       ↓
PHASE 7  数据迁移          ⏸ 双写/校验/切流；沙盘先行
       ↓
PHASE 8  服务化演进        ⏸ 仅具备独立运行价值的模块逐步抽离；Aspire 承载
```

**每个 PHASE 的 Gate 规则**：产出规格或实施计划 → 提交人类审批 → 批准后才进入实施 → 实施完成提交证据 → 下一个 PHASE。

**AI 不得自行跳相或自行决定架构边界**（`JNPF平台整体结构基线.md` §五、⑥.2）。

---

## 3. 五否定原则（永久生效，继承 §3.2）

**不得从以下信息反推 Domain：**

| 信息 | 错误推导 | 正确姿势 |
|------|----------|----------|
| 表名 | `ext_*` = Order Domain / `WFORM_*` = Workflow Domain | 表是资产盘点单位，沿抽象阶梯向上：Tables → Assets → Capabilities → Architecture |
| 代码 | 某 Service 即为核心领域 | 代码→Ownership→Transaction Boundary→Module Boundary |
| 菜单 | 存在即为业务核心 | 菜单 ≠ 数据所有权 |
| Entity | 存在即为写 owner | Entity ≠ Write Owner |
| 有真实数据 | 必须立即物理隔离 | 有真实数据 → Archive/Compatibility → 随后处理 |

---

## 4. 当前位置与下一交付物

| 维度 | 状态 | 下一交付物 |
|------|------|-----------|
| 项目基线 | ✅ v0.2 现行 | — |
| 位置 | PHASE 3：数据架构设计入口 | 产出《JNPF Next 数据架构与数据库设计规范 v0.1》 |
| 关键决策 | 待人工审批（PHASE 3 Gate） | 1）数据库主键策略 2）租户列契约 3）外键分级策略 4）首个 Vertical Slice 选域 |

**所有设计文档中的结论必须携带状态标记**：`[KNOWN]` / `[COMPUTED]` / `[INFERRED]` / `[HYPOTHESIS]` / `[UNKNOWN]` / `NEEDS HUMAN DECISION`（`JNPF平台整体结构基线.md` §六·6.3）。

---

## 5. 人机分工（坚持）

- **人类**：目标 / 边界 / 架构裁决 / Gate / 验收标准
- **AI**：侦察 / 设计草案 / 实现 / 测试 / 验证 / 报告

**任何重大架构变化**：AI 不得自行决定 → 提交证据 → 人类裁决 → 固化规格 → 执行。

---

## 6. 六零约束（各 PHASE 进入实施时由该 PHASE 规格 redefine）

ZERO BUSINESS CODE / ZERO DB CHANGE / ZERO DATA CHANGE / ZERO DEPLOYMENT / ZERO MICROSERVICE / ZERO ASPIRE ARCHITECTURE

---

## 七、已归档的前序工作（证据链，不再延伸）

| 编号 | 工作 | 状态 | 角色 |
|------|------|------|------|
| NG-0 | 只读设计，零代码 | ✅ COMPLETE/APPROVED | 侦察档案 |
| NG-1 | Domain & Data Ownership Proof + D12 Slice 证伪 | ▶ APPROVED（有条件） | 侦察档案 → NG-1A 触发 |
| NG-1A | Platform Product Boundary Audit | ✅ 完成（6 产出物） | 证据链 |
| NG-1B | Provenance Matrix（289×14维） | ✅ 完成 | 证据链 → PHASE 1 归档 |
| NG-1C | Platform Domain Ownership Proof | ⛔ SUSPENDED / ROUTE CONVERGED | **SUPERSEDED，归档仅存证据** |

---

**结论**：项目已从「考古和归类」转段准备进入「设计和建设」阶段。唯一执行路线是 PHASE 0–8，NG-1C 作为独立工程已永久停止。下一份唯一交付物是《JNPF Next 数据架构与数据库设计规范 v0.1》及其实施计划，产出后由人类审批后方可进入 PHASE 4。