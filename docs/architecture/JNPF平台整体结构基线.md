# JNPF 平台整体结构基线 v0.2

> ⚠️ **STATUS: REPOSITIONED（2026-08-26 战略修正——专家终审采纳）**
>
> 本文件**不再是项目最高层路线文档**。最高技术路线已移交两份 MASTER 文档：
> - `docs/architecture/MASTER-JNPF后端重构与Aspire微服务化总体设计规格.md` —— 六原则 P1–P6（其中 **P1 不重新设计 JNPF、P2 不预先重新设计数据库** 直接取代本文「重建 JNPF Next」战略表述）
> - `docs/architecture/MASTER-JNPF后端重构与Aspire微服务化总体实施计划.md` —— PHASE 0–7 主路线（取代本文 §四 PHASE 0–8 表；原拟 PHASE 3《JNPF Next 数据架构与数据库设计规范》**未启动即取消**，违反 P2）
>
> 本文保留价值：**§二 能力地图、§三 资产边界与证伪链 = PHASE 1 Platform Boundary 的既成事实输入**；§五 H1–H5 并入新主规格复审。下文其余表述按此横幅理解。

> **正式定义**：JNPF Next Project Baseline & Architecture Direction —— ~~项目唯一总基线~~（v0.2 时的定义，已被上述 MASTER 文档体系取代）。
> **只承担四件事**：A 我们是谁 ｜ B 我们有什么 ｜ C 我们已经证明了什么 ｜ D 我们接下来按什么顺序建设。
> **明确不承担**（塞进来就会无限膨胀，一律进下游规格）：数据库具体设计 →《数据架构与数据库设计规范》；Capability→Module 落法 →《平台能力与模块架构规格》；首个编码模块 →《Vertical Slice 规格》；AI 工作方式 →《AI 工程执行规范》。
>
> **事实与蓝图分离声明（v0.2 核心修正）**：§一~§三、§四为**事实基线（FACT BASELINE）**——全部来自只读审计，可复算；§五为**架构方向（ARCHITECTURE DIRECTION / WORKING HYPOTHESIS）**——是当前最佳假设，**不是最终架构裁决**。任何 AI 工程师不得把 §五理解为"架构已定，直接开始 Modular Monolith 编码"。

**版本历史**

| 版本 | 日期 | 变更 |
|---|---|---|
| v0.1 | 2026-08-26 | 收束 NG-0 / NG-1A / NG-1B 成果；提出能力地图与 7 步发现管线；取代前版《JNPF 下一代总体架构蓝图》 |
| v0.2 | 2026-08-26 | 升级为项目唯一总基线：①事实基线与架构方向分离；②PHASE 0–8 项目主路线取代 NG 编号路线；③正式终止 NG-1C「157 表 × 20 列大矩阵」；④确立"少而稳定"文档体系 |

---

## 0. 文档体系总图（少而稳定，禁止再增殖 NG-x 系列分析文档）

```text
                    人类总裁决 / 项目总纲
                           │
                           ▼
          《JNPF 平台整体结构基线》（本文件）
                 【唯一总基线】
                           │
          ┌────────────────┼────────────────┐
          ▼                ▼                ▼
   数据库设计规范     平台能力/模块规格     AI工程执行规范
   DB Design          Capability Spec      Coding/Execution
          │                │                │
          ▼                ▼                ▼
      数据库实施       模块化单体建设       AI Agent 执行
```

核心文件清单（目标 ≤5 份常驻）：

| # | 文件 | 回答的问题 | 状态 |
|---|---|---|---|
| ① | 《JNPF 平台整体结构基线》（本文件） | 我们是什么、有什么、证明了什么、按什么顺序建 | **v0.2 现行** |
| ② | 《JNPF Next 数据架构与数据库设计规范 v0.1》 | Next 的数据应该怎么重新设计 | ⏭ 下一份（PHASE 3） |
| ③ | 《JNPF Next 平台能力与模块架构规格 v0.1》 | Capability → Module → Public Contract → Internal Data 怎么落 | 排队（PHASE 4） |
| ④ | 《JNPF Next 首个 Vertical Slice 规格 v0.1》（Identity 或 File，待裁决） | 第一个真正写代码的模块怎么建 | 排队（PHASE 5） |
| ⑤ | 《JNPF AI 工程执行规范 v0.1》 | AI 工程师该怎么工作（Human-in-the-loop 分工） | 排队（可并行起草） |

NG-0 / NG-1A / NG-1B 全部证据文档**保留为历史证据链**（侦察档案），不再延伸新编号。**禁止把研究过程误认为项目目标。**

---

## 一、我们是谁（Identity）

### 1.1 JNPF 是什么

JNPF 是一个低代码开发平台：用户通过可视化建模（数据模型 / 表单 / 流程 / 页面）创建业务应用，平台提供身份、租户、授权、字典等基座能力，并以运行时引擎承载这些应用的执行。旧后端停留在 .NET Core 5 时代，架构耦合严重，289 张表 / 6134 列中**外键仅 14 个（275 表零 FK = 隐式关系单体）**，租户列三风格并存，关系高度隐式。

### 1.2 JNPF Next 是什么

```text
旧 JNPF 单体  ──(仅作为业务知识 / 行为兼容性 / 迁移参考)──▶  JNPF Next
                                                                  ├── 现代 .NET + Aspire（工具层）
                                                                  ├── 重新设计的数据架构
                                                                  ├── 更清晰的平台内核
                                                                  └── 新 UniApp 前端
```

我们**不是在重构旧 JNPF**，而是以旧系统为业务知识与兼容性参考，重新建设下一代平台。旧系统的四个正式作用：Behavior Reference（行为参考）/ Compatibility Baseline（兼容基线）/ Data Migration Source（迁移源）/ Business Knowledge Base（业务知识库）。Next 不复制造 Legacy 的实现怪癖。

### 1.3 战略红线

**微服务只是最后一个可能的架构结果，不是当前任务。** 当前任务是先把平台真实结构还原出来（已完成），再按 §四 主路线建设。Aspire 是开发编排 / 可观测性工具层，不参与架构边界判断。

---

## 二、我们有什么（事实基线：平台能力地图）

> 来源：D1–D12 十维分析 + NG-0 五规格十证据 + NG-1A/1B 资产审计。核心原则：**表是资产盘点单位，不是领域单位**，必须沿抽象阶梯向上：

```text
Tables(289) → Assets(平台/模板/Demo/遗留/孤儿/未知) → Platform Capabilities
           → Capability Relationships(数据/事务/权限/租户/运行时) → Platform Architecture
```

### 2.1 五类系统能力结构（A–E）

**A–D 是平台自身；E 不是平台 Domain（是产品交付物）。**

#### A. 平台基础能力（Platform Foundation）
| 能力 | 说明 |
|------|------|
| 身份（用户/组织/角色/岗位/组） | 全域依赖根，最先 API 化 |
| 租户（注册/连接配置） | 注册表 `zx_system_db`/`zx_sys_config`；连接级切库语义保留 |
| 授权（菜单/按钮/列/数据权限） | 数据权限「条件生产」是核心；GetCondition 双路径为迁移基线 |
| 字典与公共数据 | 最易 API 化的共享读 |
| 基础配置 / 调度 | 平台运行基础设施 |

#### B. 低代码设计态（Low-Code Authoring）
| 能力 | 说明 |
|------|------|
| 数据建模 / 表单设计器 / 页面设计器 / 流程设计器 | 元数据定义态 |
| 集成设计器 | **[HYPOTHESIS]** 旧系统无清晰集成域，属前瞻能力 |

#### C. 低代码运行时（Low-Code Runtime）——未来最值得研究的部分
| 能力 | 说明 |
|------|------|
| 表单运行时（含 mt\* 动态表） | 最大迁移面 |
| 流程运行时 | 天然事件源 |
| 数据运行时 | 运行时数据 API（GetListQuerySql 链） |
| 权限运行时 | GetCondition 双路径 |
| 应用运行时 | 应用运行载体 |

#### D. 平台服务（Platform Services）
| 能力 | 说明 |
|------|------|
| 文件 | 无跨域 Join，独立化最强候选（注意：旧 `base_file` 已判 P6 LEGACY——File 能力在旧库「有职责无核心表」，Next 需全新设计） |
| 消息通知 | 已有 `SYS_EVENT_OUTBOX_MESSAGE` 事件化种子 |
| 审计/日志 | 写放大，独立存储候选 |
| 事件出箱/总线 | 出箱表已是挂靠点 |
| 报表 | 读模型候选（CQRS 读侧） |
| AI（InteAssistant） | 旧系统中域自治度最高（sa\* 有 FK + IR 事件表）；仅依赖身份/授权读 |

#### E. 产品包装（Product Packaging —— 非平台 Domain）
| 类别 | 对应资产 |
|------|---------|
| 模板包 Template Pack | WFORM_\* 48 = P2 |
| 演示包 Demo Pack | ext_\* 19 = P3 |
| 归档 Archive | WM_\*/WH_\* 42 = P6；P4 客户表 5 张（真实数据禁删） |

### 2.2 低代码核心闭环（JNPF Next 真正要重新架构的核心）

```text
           ┌──────────────┐
           │  Application │
           └──────┬───────┘
                  │
         ┌────────▼────────┐
         │    Metadata     │  B 设计态：Form / Model / Workflow / UI
         └────────┬────────┘
                  │
         ┌────────▼────────┐
         │     Runtime     │  C 运行时
         └────────┬────────┘
                 │
       ┌─────────┼─────────┐
       │         │         │
     Data      Workflow   Permission
       └─────────┼─────────┘
                  │
            Tenant / Identity（A 基座）
```

---

## 三、我们已经证明了什么（证伪链 + 资产边界，全部可复算）

### 3.1 资产边界结论

- 289 张表全部获得 P0–PX 分类，零遗漏；
- **157 张平台资产（P0 146 + P1 11，55%）进入 Next 设计；132 张非平台资产不现代化**（模板 48 / Demo 25 / 客户 5 / 遗留 47+ / 孤儿 1 / 未知 6）；
- Provenance Matrix（289×26）：P0 UNKNOWN 清零，P0/P1 PROVEN 率 88.5%；
- sa_\* 13 张 = P1 特殊基础设施（dapper-first），归属裁决延后。

### 3.2 已钉死的证伪链（本项目最有价值的资产）

```text
Order / ext_*        ──❌ 不是平台 Domain（实为 Demo）
WFORM_*              ──❌ 不能等同 Workflow Domain（实为产品模板）
zx_sys_db            ──❌ 不能等同 Tenant Core（实为遗留副本；真身 zx_system_db）
base_file            ──❌ 不能因名字叫 File 就认定 File Core Domain（实为 P6）
WM_*/WH_* 有真实数据  ──❌ 不构成 Warehouse Domain（实为 P6 遗留，归档禁删）
289 Tables           ──❌ 不能直接转换成 Domains / Microservices
```

由此沉淀的五否定原则（对所有后续 Agent 永久生效）：**表存在 ≠ 领域存在；代码存在 ≠ 核心能力存在；菜单存在 ≠ 核心数据存在；Entity 存在 ≠ Write Owner 存在；有真实数据 ≠ 属于平台核心。**

### 3.3 沉淀纪律

1. Next 数据库重写仅针对 157 张平台表；132 张严禁拖入现代化；
2. 不沿用 modularity 项目目录作为模块/领域边界；
3. PX 零猜测；不确定结论必须标记 `UNKNOWN / HYPOTHESIS / NEEDS HUMAN DECISION`。

---

## 四、我们接下来按什么顺序建设（项目主路线 PHASE 0–8）

> **v0.2 关键变更：项目主路线改用 PHASE 编号，NG 编号路线退役**（NG-0/1A/1B 成为 PHASE 0–1 的已归档侦察证据；NG-1C 大矩阵终止——其"三权取证框架"降级为未来需要时的参考方法，不再作为独立工程执行）。

```text
PHASE 0  项目基线          ✅ 本文件 v0.2
        ↓
PHASE 1  平台资产识别      ✅ 289 → 157/132（NG-1A/1B 证据链归档）
        ↓
PHASE 2  平台能力地图      ✅ 本文件 §二（A–E 五类 + 核心闭环）
        ↓
PHASE 3  数据架构设计      ▶ 当前位置 ——《JNPF Next 数据架构与数据库设计规范 v0.1》+ 实施计划
        ↓                     （产出后 STOP，人类批准才进实施）
PHASE 4  模块架构设计      ⏸ 《平台能力与模块架构规格》：Capability → Module → Public Contract → Internal Data
        ↓
PHASE 5  首个 Vertical Slice ⏸ Identity 或 File（§七待裁决）；第一份真正写代码的规格
        ↓
PHASE 6  模块化单体建设    ⏸ 按 Module 逐个建设 + 架构测试强制边界
        ↓
PHASE 7  数据迁移          ⏸ 双写/校验/切流；沙盘先行
        ↓
PHASE 8  服务化演进        ⏸ 仅对通过四重证明（Ownership+Transaction+Query+Migration）的能力抽离独立服务；Aspire 届时承载
```

每个 PHASE 的 Gate 规则：**产出规格或实施计划 → 提交人类审批 → 批准后才进入实施 → 实施完成提交证据 → 下一个 PHASE。** AI 不得自行跳相。

---

## 五、架构方向（ARCHITECTURE DIRECTION / WORKING HYPOTHESIS —— 非最终架构裁决）

> **本节全部内容是当前最佳工作假设，不是已完成的最终架构裁决。** 它们将在 PHASE 4/5 用模块架构与切片实测证据确认或推翻。任何 AI 工程师不得据此直接开始编码。

| # | 方向假设 | 当前依据 | 确认时机 |
|---|---|---|---|
| H1 | v1 采用 Modular Monolith（单库 + 域内程序集隔离 + 架构测试强制） | 各候选域事务均落单库事务内；无跨库事务需求 | PHASE 4 模块架构证据 |
| H2 | 四类形态：CORE MODULE / OPTIONAL MODULE / FUTURE SERVICE / INFRASTRUCTURE | 能力地图 A–E 的承载方式草案 | PHASE 4 |
| H3 | File / AI / Integration 为远期服务化候选 | 边界清晰度初判 | PHASE 5–8 切片实测 |
| H4 | Aspire = 开发编排 / 服务发现 / 可观测性工具层，先单体的演化杠杆，不预设微服务数量 | NG-0 Aspire 规格 | 持续有效约束 |
| H5 | 微服务只是最后可能结果 | 证伪链 §3.2 | PHASE 8 前**冻结一切拆分讨论** |

---

## 六、Agent 硬规则（Human-in-the-loop Agentic Engineering）

### 6.1 五否定推导禁令（继承 §3.2，永久生效）

**不从表名 / Service 名称 / Demo / 模板 / 历史数据反推 Domain。**
正确顺序：`Platform Capability → Data Responsibility → Data Model → Transaction Boundary → Module Boundary`。

### 6.2 人机分工

```text
人类：目标 / 边界 / 架构裁决 / Gate / 验收标准
AI  ：侦察 / 设计草案 / 实现 / 测试 / 验证 / 报告

任何重大架构变化：
AI 不得自行决定 → 提交证据 → 人类裁决 → 固化规格 → 执行
```

### 6.3 不确定性标记纪律

所有设计文档中的结论必须携带状态标记：`[KNOWN]`（有证据）/ `[COMPUTED]`（可复算推导）/ `[INFERRED]`（推断，给置信度）/ `[HYPOTHESIS]`（待验证假设）/ `[UNKNOWN]`（未知，零猜测）/ `NEEDS HUMAN DECISION`（升级人工）。

### 6.4 六零约束延续

侦察与设计阶段维持：ZERO BUSINESS CODE / ZERO DB CHANGE / ZERO DATA CHANGE / ZERO DEPLOYMENT / ZERO MICROSERVICE / ZERO ASPIRE ARCHITECTURE（各 PHASE 进入实施时由该 PHASE 规格 redefine）。

---

## 七、待决策事项（NEEDS HUMAN DECISION）

| # | 决策项 | 选项 / 建议 | 阻塞的 Phase |
|---|---|---|---|
| 1 | 数据库主键策略 | 字符串 ID 兼容 vs 雪花/GUID | PHASE 3 |
| 2 | 租户列契约 | 统一单一 `tenant_id`（三风格并存现状） | PHASE 3 |
| 3 | 外键分级策略 | 平台核心补 FK / 动态运行时保持隐式 | PHASE 3 |
| 4 | 首个 Vertical Slice 选域 | 建议 Identity（全域依赖根）或 File（边界最清晰） | PHASE 5 |
| 5 | 集成能力定位 | 本期定义契约 vs 留待 v2 | PHASE 3/4 |
| 6 | 132 张非平台资产处置节奏 | 模板包 / Demo 包 / 归档库优先级 | PHASE 6/7 |
| 7 | 本基线 v0.2 审批 | 批准后 PHASE 3 启动 | PHASE 3 |

---

## 八、总结

本项目已完成从「考古和归类」到「设计和建设」的转段准备：战略上锁定「以旧 JNPF 为知识参考重建 JNPF Next」；证据上完成 289 表资产边界与来源证明并证伪「表→领域→微服务」错误推导链；结构上还原出五类能力地图与低代码核心闭环；路线上以 PHASE 0–8 取代无限延伸的分析编号，当前位置 **PHASE 3（数据架构设计）入口**。下一份唯一交付物：《JNPF Next 数据架构与数据库设计规范 v0.1》及其实施计划——从平台能力正向推导数据模型，产出后 STOP 等待人类批准。
