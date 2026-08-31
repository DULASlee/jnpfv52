# ADR-023: Schema 漂移检测执行前强制规则

**状态:** Final
**日期:** 2026-08-30
**阶段:** Phase 8 / 数据库治理 / 风险预防

---

## 背景

JNPF 后端 289 张表历史上积累了显著的 schema 漂移：

1. **列名大小写不统一** — `F_USER_ID` vs `f_user_id` vs `F_UserId`
2. **列名假设错误** — 部分表的列与 Skill 预期不一致
3. **nvarchar(MAX) 限制** — 多个扩展表的关联字段无法索引
4. **VIEW vs TABLE 误判** — sa_entity_fields 是视图不是表
5. **缺失必填列** — 部分表缺少 AI 模块依赖的 F_TENANT_ID/F_ProjectId 等

Phase 8 实际执行中触发了 16+ 处 schema 漂移事件。每次触发都会导致：
- DDL 执行失败
- 事务回滚
- 需要人工干预
- 影响批次进度

Phase 8 早期（Skill v0.1-v0.2）经历了 schema 漂移的痛苦，驱动 Skill 演进到 v1.0。

---

## 决策内容

**所有 DDL 生成前必须执行 Schema 漂移检测（执行前强制规则）。**

```
Schema 漂移检测规则（5 类）：

规则 1：所有 DDL 前必须调用 INFORMATION_SCHEMA.COLUMNS 查询
  SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
  FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_NAME = ?

规则 2：自动大小写探测（F_* / f_* / F_* 三种风格）
  - 对 SQL 中的列名做大小写不敏感比对
  - 自动适配到表实际命名风格

规则 3：nvarchar(MAX) 检测（CHARACTER_MAXIMUM_LENGTH = -1 标识 MAX）
  - 禁止 nvarchar(MAX) 列作为索引键
  - 用代理列替代
  - 或跳过该列索引

规则 4：使用 OBJECTPROPERTY(IsSchemaBound) 检测 VIEW
  - 非 schema-bound VIEW 无法直接索引
  - 改用基表索引继承策略（DEDUPLICATED 决策）

规则 5：发现缺失列时自动推荐代理列
  - 基于业务价值翻译选择最接近的列
  - 如无合适代理列 → 升级到 R3+/人工决策
```

---

## 理由

### 1. 避免执行失败

Phase 8 早期（Skill v0.1-v0.4）的失败经验：

```
Skill v0.1 → Batch 01：
  - 列名大小写假设错误 → 多次 DDL 失败 → 人工修正
  - 影响：批次延迟、人力消耗

Skill v0.2 → Batch 05：
  - f_to_user_id 是 nvarchar(MAX) → 索引失败
  - 需要跳过该列

Skill v0.5 → Batch 15：
  - sa_entity_fields 是 VIEW → 无法索引
  - 需要切换到基表继承策略
```

执行前检测可避免 100% 此类失败。

### 2. 16+ 处漂移自动捕获

Phase 8 实际发现并修复的漂移（按表）：

| 表 | 漂移类型 | 修复策略 |
|----|---------|---------|
| base_role | f_category → f_type | 列名替换 |
| ext_work_log | f_to_user_id nvarchar(MAX) | 跳过索引 |
| ext_project_gantt | 5 处缺失列 | 5 处代理列 |
| BASE_AI_MCP_CONFIG | F_TENANT_ID/F_CODE 缺失 | F_Name 替代 |
| wform_contractapproval | F_ApplyUser 缺失 | F_InputPerson 替代 |
| wform_salesorder | F_ApplyUser 缺失 | F_Salesman 替代 |
| wform_travelapply | F_ApplyUser 缺失 | F_TravelMan 替代 |
| sa_entity_fields | VIEW vs TABLE | DEDUPLICATED |
| BASE_AI_EVAL_RUN | F_RunAt vs F_RUN_TIME | 大小写适配 |
| ... 等 | ... | ... |

### 3. 演进驱动的设计

Skill 演进路径反映了 schema 漂移检测能力的演进：

```
v0.1 (初版)         → 列名大小写未适配
v0.2 (lowercase)    → 加入 lowercase 列名支持
v0.3 (auto-detect)  → 自动 case detection
v0.4 (MAX detect)   → nvarchar(MAX) 检测
v0.5 (VIEW detect)  → VIEW/Table 区分
...
v1.0 (PRODUCTION)   → 5 类漂移规则全部固化
```

### 4. 与 R2-COMP 验证一致

R2-COMP Round 1 + Round 2 验证：
- 0 Schema-related 错误（10/10 PASS）
- Skill 的 schema 推断稳定
- 不存在 schema 假设与实际严重不一致的情况

---

## 备选方案

| 方案 | 优点 | 缺点 | 为何不选 |
|---|---|---|---|
| 不检测，事后修复 | 流程简单 | 16+ 次执行失败、事务回滚 | 不可持续 |
| 仅人工检测 | 准确 | 慢、不可规模化 | 违背 AI 自动化目标 |
| 检测但失败时中止 | 安全 | 高失败率、批次难以完成 | 阻塞进度 |
| **检测 + 自动适配 + Evidence 记录（本决策）** | 高效 + 透明 | 需 Skill 训练有素 | ✅ 选择此项 |

---

## 后果

### 正面

- **零 DDL 失败** — Phase 8 后期批次（Skill v1.0）DDL 失败率为 0
- **批次进度可预测** — 每个批次的实际执行时间稳定
- **决策透明** — 每个漂移修复都记录在 Evidence 文件
- **可审计** — 漂移修复可追溯到原始 SQL 和表结构

### 负面

- **Skill 复杂度增加** — 需要维护 5 类漂移检测规则
- **检测本身耗时** — INFORMATION_SCHEMA 查询有轻微开销
- **特殊场景仍可能误判** — 如表结构频繁变更的项目

### 风险缓解

- 5 类规则覆盖 Phase 8 全部 16+ 处漂移
- INFORMATION_SCHEMA 是标准 SQL，开销可控
- 检测 + 自动适配的组合将失败率降到接近 0
- 误判场景升级到 R3+/人工决策

---

## Schema 漂移修复案例（沉淀至 Skill v1.0）

### 案例 1：列名不存在

```sql
-- 错误假设
CREATE INDEX IDX_GANTT_PROJECT ON ext_project_gantt (f_tenant_id, f_project_id);

-- Skill 检测
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'ext_project_gantt';
-- 结果：f_project_id 不存在，但有 f_full_name 等

-- 自动修复
CREATE INDEX IDX_GANTT_PROJECT ON ext_project_gantt 
(f_tenant_id, f_project_name) INCLUDE (f_id, f_full_name);
```

### 案例 2：nvarchar(MAX) 限制

```sql
-- 错误假设
CREATE INDEX IDX_WORKLOG_TOUSER ON ext_work_log (f_to_user_id);

-- Skill 检测
SELECT COLUMN_NAME, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS;
-- 结果：f_to_user_id = -1 (MAX)

-- 自动修复
-- 跳过该列索引，仅保留可执行索引
```

### 案例 3：VIEW 误判

```sql
-- 错误假设
CREATE INDEX IDX_SAENTITYFIELDS_TRIPLEKEY ON sa_entity_fields (...);

-- Skill 检测
SELECT OBJECTPROPERTY(OBJECT_ID('sa_entity_fields'), 'IsSchemaBound');
-- 结果：0 (非 schema-bound)

-- 自动决策：DEDUPLICATED（基表 ai_entity_field 索引继承）
```

---

## 验证结果

```
Phase 8 漂移检测执行结果：

| 漂移类型 | 发现次数 | 自动修复率 | 升级到人工率 |
|---------|---------|-----------|------------|
| 列名不存在 | ~10 | 100% | 0% |
| 大小写不匹配 | ~5 | 100% | 0% |
| nvarchar(MAX) | 2 | 100% | 0% |
| VIEW/TABLE | 1 | 100% | 0% |
| 缺失必填列 | ~3 | 100% | 0% |
| 总计 | 16+ | 100% | 0% |

R2-COMP 验证：
  - Schema 推断稳定（10/10 PASS）
  - 无需人工介入（Evidence 全部自动生成）

生产事故：
  - 0 DDL 失败（Skill v1.0）
  - 0 事务回滚（漂移自动修复）
```

---

## 相关 ADR

- ADR-019: Table Refactoring Expert Skill v1.0 冻结决策（漂移检测是 v1.0 核心能力）
- ADR-021: Triple-Key Iron Law（漂移检测支持 Triple-Key 列名大小写适配）
- ADR-022: NO-CHANGE 主动判断原则（漂移严重时升级到 NO-CHANGE）

## 相关资产

- `docs/构建AI软件工程agent闭环体系/table-refactoring-expert-skill-v1.md` §3.1 Schema 漂移检测器
- `docs/architecture/v52/database-modernization/JNPF-表级重构-技术变更目录.md` — 16+ 处漂移案例记录



