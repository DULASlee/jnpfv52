# Backend Structural Audit — Refactoring Exclusion / Freeze List

**日期**：2026-08-25 ｜ 原则：没有行为保护，就不能因为静态指标漂亮而直接重构

## 1. 冻结清单（Freeze）

| 类别 | 对象 | 理由 |
|------|------|------|
| D1 已销账代码 | `ListSuperQueryInputRewriter` / `FieldBindDefaultValueHelpers` / `FlowFormDataMapper.ApplyMapRules` / `ImportFirstVerifyHelpers.ValidateBatchUnique` / `GetConditionQueryClauseAppender` | 已完成等价证明（122 特征 + 签名契约 + 路由 zero-diff）；变更须另立战役 |
| JNPF009 台账冻结 | **111 个 A 类方法**（maxComplexity 锁定） | 基线机制：只许下降；重构须按 D1 五步协议逐方法销账 |
| 序列化契约 | `ConditionalType/WhereType` 枚举数值、匿名对象 `{Key,Value}` 形态、`JsonToConditionalModels` | 跨层反序列化硬契约（IC-01/03） |
| 数据权限契约 | NotIn 尾部 `"null"`+空串 IsNot、首条 whereType 序列、`-` 键豁免 | Q7/Q10/Q11 保真（L0） |
| 租户契约 | `ITenantFilter` 12 文件挂靠语义（EntityBase/TenantEntityBase/DataBaseManager/TenantManager） | S2 抽象前提（P0-3） |
| 第三方集成边界 | infrastructure/*（OAuth/EventBus/WebSocket）、framework 框架层 | 非本审计范围 |
| 无充分保护的核心逻辑 | 路径 B（GetConditionAsync/GetDataConditionAsync，零特征） | **先补保护再谈重构**（P0-2 解除条件） |
| 并行战役隔离 | DLL 化 v2.3、`.agents/`、session 文件 | 提交边界三查 |

## 2. 例外情形（允许打破冻结的唯一路径）

- 按 D1 五步协议（特征金标准前置 → 拆分 → 等价 → 销账 → 独立提交）且经人工批准的新战役
- F3 流程：发现冻结对象存在行为/边界疑问 → 停止、登记、报裁，不自行处置

## 3. 与候选登记的关系

P0/P1/P2/P3 全部候选均受本清单约束；无行为保护的候选（如 IC-04 脚本生成）即使 CC 高也**不进入重构队列**，先补特征。
