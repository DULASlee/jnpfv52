# JNPF 架构文档索引

> **入口文档**: 跳转至各专题文档
> **维护者**: JNPF 架构组
> **更新日期**: 2026-08-30

---

## 核心文档

### 📘 入门必读

| 文档 | 用途 |
|---|---|
| [JNPF-Database-Architecture-Manual.md](./JNPF-Database-Architecture-Manual.md) | **架构总览**：289 张物理表的命名规范、模块划分、业务域详解、设计模式 |
| [JNPF-Complete-Table-List.md](./JNPF-Complete-Table-List.md) | **完整表清单**：289 张物理表逐张列出（含列数、行数、租户列、分类） |

### 📋 Phase 8 治理文档

| 文档 | 路径 | 用途 |
|---|---|---|
| Phase 8 Master Plan | `docs/universal/Phase-8/Phase-8-JNPF-Table-Refactoring-Master-Execution-Plan.md` | Phase 8 总执行计划 |
| P8-C.1 Scope Framework | `docs/universal/Phase-8/p8-c/p8-c1-scope-classification-framework.md` | 生产对象分类框架 |
| P8-C.1 Scope Registry | `docs/universal/Phase-8/p8-c/p8-c1-production-scope-registry.md` | 生产对象注册表 |
| P8-C.1 Progress Recalc | `docs/universal/Phase-8/p8-c/p8-c1-progress-recalculation.md` | 进度重算报告 |
| P8-A Shadow Gate | `docs/universal/Phase-8/p8-a/shadow/comparison/shadow-gate-result.md` | P8-A 影子生产关卡结果 |
| P8-B Closure | `docs/universal/Phase-8/p8-b/p8-b-closure.md` | P8-B 受控生产闭包 |

---

## 如何使用本架构文档

### 场景 1：AI 大模型需要理解 JNPF 数据库

```
步骤 1: 阅读 JNPF-Database-Architecture-Manual.md 全文
       - 重点看"命名规范"、"设计模式"、"业务域详解"

步骤 2: 查询 JNPF-Complete-Table-List.md 找具体表
       - 按分类定位
       - 查看列数、行数、租户支持

步骤 3: 查询 SQL 验证
       - SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'xxx'
       - 注意大小写（实际列名可能与文档假设不同）
```

### 场景 2：开发者修改某张表

```
步骤 1: 在 JNPF-Complete-Table-List.md 中定位表
步骤 2: 查 Phase 8 治理文档中该表的状态
步骤 3: 阅读 Architecture Manual 中该业务域的"设计模式"
步骤 4: 修改前确认是否触发以下任一：
       - 是否破坏 CLDS 字段
       - 是否影响多租户隔离
       - 是否影响 SCD Type 2 时序
       - 是否改变多态外键语义
```

### 场景 3：Phase 8 选择下一批表

```
步骤 1: 确认生产宇宙（206 张 PRODUCT_CORE）
步骤 2: 排除已索引表（94 张已有 IDX_* 索引）
步骤 3: 选择下一批 5-8 张
       - 优先同一业务域内（强关联）
       - 优先数据量大的表（影响最大）
步骤 4: 跑验证脚本（见 P8-C 文档）
```

---

## 关键概念速查

| 概念 | 英文 | 简要 |
|---|---|---|
| **PRODUCT_CORE** | Production Core | 平台真正生产依赖的 206 张表 |
| **SYSTEM_TEMPLATE** | System Template | 平台内置模板（wform_*/ext_*），69 张 |
| **DEMO_SAMPLE** | Demo Sample | 演示/教学/示例数据，5 张，OUT_OF_SCOPE |
| **TEST_FIXTURE** | Test Fixture | 测试/备份/Snowflake 遗留表，6 张，OUT_OF_SCOPE |
| **CLDS** | Create/Last/Delete/Soft | JNPF 通用审计字段集 |
| **Triple-Key** | (tenant_id, project_id, pipeline_id) | AI 模块复合主键 |
| **Snowflake ID** | Snowflake Distributed ID | JNPF 默认主键策略（18-19 位字符串） |
| **SCD Type 2** | Slowly Changing Dimension Type 2 | SA 输出表的版本化模式 |
| **多态外键** | Polymorphic Foreign Key | 用 (type, id) 模拟多关系 |
| **ITenantFilter** | ITenantFilter | JNPF 多租户过滤拦截器 |

---

## 文件清单（`docs/architecture/`）

```
docs/architecture/
├── README.md                                (本索引)
├── JNPF-Database-Architecture-Manual.md    (架构手册主文档)
└── JNPF-Complete-Table-List.md              (完整表清单附录)
```

---

## 维护说明

本目录下的文档为 **Phase 8 治理基线**，不应随意修改。如需更新：

1. **小修改**（typo, 数据更新）：直接在 PR 中修改
2. **中等修改**（新增表、分类变化）：需 Chief Architect 批准
3. **重大修改**（重新分类、生产宇宙变化）：需 Phase Gate 决策

修改时请同步更新：
- `JNPF-Database-Architecture-Manual.md`
- `JNPF-Complete-Table-List.md`
- `docs/universal/Phase-8/p8-c/p8-c1-production-scope-registry.md`
