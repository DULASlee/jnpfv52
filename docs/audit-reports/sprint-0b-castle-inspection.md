# Sprint 0-B 城堡巡检报告

**巡检日期：** 2026-06-12
**巡检范围：** Sprint 0-B AI 基础设施地桩全量交付物（10 项地桩 + 8 项门禁）
**巡检人：** Mavis（架构审计代理）

---

## 巡检摘要

| 评级 | 数量 | 处置 |
|---|---|---|
| **严重** | 2 | 阶段一启动前必须修复 |
| **高** | 4 | 排入阶段一技术债 backlog |
| **中** | 5 | 阶段二前修复 |
| **低** | 3 | 按需处理 |

---

## 巡检发现

### F-001 | 严重 | LlmGatewayService 无 Provider 路由

**描述：** `LlmGatewayService` 为 stub 实现，`ChatAsync` 和 `HealthCheckAsync` 均返回硬编码值。Phase 2 对接真实 SDK 时需关注 provider 切换逻辑的位置——当前未定义 `provider` 参数的路由规则（OpenAI vs 国产模型选择）。

**影响：** 五阶段流水线编译阶段（compiling）依赖 LLM 调用，stub 无法验证 prompt 实际效果。

**建议修复：** Phase 2 在 `ChatAsync` 签名中显式增加 `provider` 枚举参数，并在 `LlmGatewayService` 内部实现基于 `IConfiguration` 的 provider 工厂。

**关联地桩：** #9

---

### F-002 | 严重 | WriteCallLog 吞异常无法追踪日志丢失

**描述：** `LlmGatewayService.WriteCallLog()` 方法内 `catch { }` 完全吞没异常。在日志写入失败时（如数据库连接中断、租户上下文丢失），没有任何告警渠道。

**影响：** AI 调用日志（`BASE_AI_CALL_LOG`）是 Evals 和审计的唯一数据源。静默丢失日志意味着无法统计 token 使用量、无法追踪错误调用、无法审计。

**建议修复：** 至少通过 Serilog `Log.Warning(ex, "AI call log write failed")` 记录失败。Phase 2 可引入 outbox 模式（利用 Day 3 已验证的 OutboxSqlServerPoC）。

**关联地桩：** #1, #9

---

### F-003 | 高 | KnowledgeGraphStore BFS 无循环图防护

**描述：** `QueryNeighborsAsync()` 虽然使用了 `visited` set 防止无限循环，但 **不检测边所引用的节点是否真实存在**。如果一条边指向不存在的节点 ID，查询会静默返回空结果而非报错。

**影响：** Foundry → Studio 知识传递时，若 KnowledgePatch 包含悬挂边，图谱查询会出现"部分缺失"现象，难以排查。

**建议修复：** `AddEdgeAsync` 时增加 FK 校验（检查 `sourceNodeId` 和 `targetNodeId` 是否存在），或通过数据库外键约束强制执行。

**关联地桩：** #3

---

### F-004 | 高 | FounderGuardMiddleware 无 Whitelist 机制

**描述：** 当前中间件仅基于 Phase 配置返回 404/403。如果 Phase 3 需要白名单 IP 或特定用户绕过认证（如健康检查端点），当前设计无法支持。

**影响：** Phase 3 切换为 403 后，Docker Compose health check 可能因 `/health` 端点路径匹配 `/api/founder/*` 而被误拦截。实际上 `/health` 不在 `/api/founder/*` 路径下——但需确认没有其他 `/api/founder/health` 路径。

**建议修复：** 在中间件中增加 `WhitelistPaths` 配置项，默认包含 `/health`。路径匹配逻辑从 `StartsWithSegments` 改为精确前缀匹配 + 白名单例外。

**关联地桩：** #5

---

### F-005 | 高 | KnowledgePatchService 签名密钥默认值硬编码

**描述：** `KnowledgePatchService` 第 43 行使用 `"jnpf-default-signing-key"` 作为 `SignatureKeyConfig` 的 fallback 值。此默认密钥硬编码在源码中，若部署环境未配置 `KnowledgePatch:SignatureKey`，所有签名校验实际使用此已知密钥。

**影响：** 安全降级——签名形同虚设，任何持有源码的人都可以伪造 KnowledgePatch 包。

**建议修复：** 移除默认值，改为启动时强制检查配置项是否存在。若缺失，`ConfigureServices` 阶段抛出 `InvalidOperationException("KnowledgePatch:SignatureKey is required")`。

**关联地桩：** #10

---

### F-006 | 高 | AiPromptTemplateService GetActiveByName 无版本冲突处理

**描述：** `GetActiveByName(name)` 使用 `.FirstAsync()` 期望每个 name 只有一个激活版本。但 `Create` 中的唯一性约束仅为 `(Name, Version)`，允许同一 Name 的多个版本同时标记 `IsActive=1`。当出现多版本激活时，`FirstAsync` 仅返回一条，另一条被静默忽略。

**影响：** 五阶段流水线加载 prompt 模板时可能获取到非预期的版本，导致 A/B 测试结果不可信。

**建议修复：** `Create` 中增加逻辑：当新模板 `IsActive=1` 时，自动将同 Name 的旧模板 `IsActive` 置为 0。或在 `GetActiveByName` 中使用 `.ToListAsync()` 并返回多条（让调用方决定）。

**关联地桩：** #6

---

### F-007 | 中 | ir-to-schema 无字段顺序保留

**描述：** `irToSchema()` 生成的 Schema JSON 中，字段属性顺序依赖于 `Object.keys()` / 对象字面量顺序（V8 引擎保证插入顺序）。但 `fieldToSchemaField()` 中通过条件追加 `span`、`labelWidth` 等可选属性，可能导致与原 Schema 的字段顺序不一致。

**影响：** Round-trip diff 中可能出现属性顺序差异（非功能性，但影响 diff 可读性）。

**建议修复：** 显式维护属性输出顺序（例如 `__vModel__` → `__config__` → `placeholder` → `on`），确保 round-trip diff 仅含语义差异。

**关联地桩：** #8

---

### F-008 | 中 | `@jnpf-gen:insert-point` 占位符无结构化标记

**描述：** 当前 insert-point 是纯文本注释 `// @jnpf-gen:insert-point`，无作用域标记（如 `@jnpf-gen:insert-point imports` vs `@jnpf-gen:insert-point methods`）。LLM 生成代码时可能将 import 语句插入到 methods 区域。

**影响：** Phase 2 LLM 生成代码合并时，插入位置可能不精确，需后处理修正。

**建议修复：** 扩展为结构化占位符：`@jnpf-gen:insert-point:imports` / `@jnpf-gen:insert-point:methods` / `@jnpf-gen:insert-point:template` 等。在 `types.ts` 中定义 `InsertPoint` 枚举，供编译器和 LLM 共用。

**关联地桩：** #8

---

### F-009 | 中 | AiCallLogService 列表查询无 Token 统计聚合

**描述：** `GetList` 仅返回单条记录的 `promptTokens` 和 `completionTokens`，无聚合查询（如按 model 统计总 token 消耗）。Evals 和成本监控面板需要聚合能力。

**影响：** Phase 2 监控面板需要额外开发聚合 API 或在前端做客户端聚合（后者性能差）。

**建议修复：** 新增 `GET api/InteAssistant/AiCallLog/Stats` 端点，按 model、时间范围分组统计 `SUM(PromptTokens)` 和 `SUM(CompletionTokens)`。

**关联地桩：** #1

---

### F-010 | 中 | PipelineMessage 表无索引声明

**描述：** `BASE_AI_PIPELINE_MESSAGE` 表未声明 `F_PIPELINE_ID` 或 `F_SEQUENCE` 的数据库索引。流水线消息按 `(PipelineId, Sequence)` 排序是高频查询模式，无索引将导致全表扫描。

**影响：** 流水线消息量增大后（每次 LLM 交互数条消息），查询性能线性下降。

**建议修复：** 在 Entity 中通过 `[SugarIndex]` 或 `IndexAttribute` 声明联合索引 `(F_PIPELINE_ID, F_SEQUENCE)`。或在 DbUp migration 脚本中显式 `CREATE INDEX`。

**关联地桩：** #4

---

### F-011 | 中 | Studio 视图骨架硬编码 stub 数据

**描述：** `src/views/studio/index.vue` 中的 `handleTestHealth` 函数返回硬编码的 `{ isHealthy: true, provider: 'stub', latencyMs: <random> }`。与 `LlmGatewayService.HealthCheckAsync()` 的 API 路径尚未对齐（当前无路由注册）。

**影响：** Phase 2 联调时需要额外对接工作。

**建议修复：** 在 Phase 2 为 `LlmGatewayService` 注册一个内部 API 端点（如 `GET /api/InteAssistant/LlmGateway/Health`），或在 `AiCallLogService` 中增加健康检查代理方法。

**关联地桩：** #7, #9

---

### F-012 | 低 | ai/gateway/types.ts 缺少 KnowledgePatch 类型

**描述：** 前端 AI 类型定义中无 `KnowledgeIncrementPackage` 对应的 TypeScript 接口。Phase 2 前端知识图谱管理页面需要此类型。

**影响：** 前端需要从后端 C# DTO 手动复制类型定义。

**建议修复：** 在 `types.ts` 中追加 `KnowledgeIncrementPackage` 和 `KnowledgeNode` / `KnowledgeEdge` 类型。

**关联地桩：** #7, #10

---

### F-013 | 低 | 后端 DTO 使用 camelCase 命名，与 C# PascalCase 属性不一致

**描述：** InteAssistant DTO 遵循 JNPF 前端约定使用 camelCase（如 `promptTokens`、`latencyMs`），但 C# Entity 和 Service 内部使用 PascalCase。这没有问题——但新增 DTO 的字段命名缺少统一文档指引。

**影响：** 后续新增 DTO 时可能混用 camelCase / PascalCase / snake_case。

**建议修复：** 在 `CLAUDE.md` 或 `docs/conventions/` 中明确：前端交互 DTO 字段使用 camelCase（对齐 JSON），Entity 属性使用 PascalCase + `[SugarColumn(ColumnName = "F_XXX")]`。

**关联：** 整体代码规范

---

### F-014 | 低 | Sprint 0-B 无性能基线

**描述：** 全部 8 项门禁均为功能验证（表存在 / 接口定义 / 编译通过），无性能指标（如 `LlmGatewayService.ChatAsync` 的 stub 响应时间、KnowledgeGraphStore BFS 查询延迟）。

**影响：** Phase 2 引入真实 LLM SDK 和知识图谱数据后，无基准对比点，难以判断性能回退。

**建议修复：** Phase 2 启动前运行一次基线性能快照：`LlmGatewayService.HealthCheckAsync()` 延迟、`KnowledgeGraphStore.QueryNeighborsAsync()` 在 100 节点图中的耗时、`AiCallLogService.GetList()` 分页查询耗时。

**关联：** 整体质量保障

---

## 处置优先级

| 优先级 | 编号 | 阶段一前 | 阶段一 | 阶段二 |
|---|---|---|---|---|
| 立即 | F-001, F-002 | ✅ 修复 | — | — |
| 本周 | F-005 | ✅ 修复 | — | — |
| 本周 | F-003, F-004, F-006 | ✅ 修复 | — | — |
| 中期 | F-007, F-008, F-010 | — | ✅ 修复 | — |
| 待定 | F-009, F-011 | — | — | ✅ 修复 |
| 记录 | F-012, F-013, F-014 | — | — | 按需 |

---

## 附录：巡检方法

- **静态分析：** 审查所有 Sprint 0-B 新增 .cs 和 .ts 文件
- **架构一致性：** 对照 CLAUDE.md Architecture Redlines (R1-R5) 和 JNPF Expert Traps (1-14)
- **安全审查：** 检查硬编码密钥、吞异常、签名校验降级
- **数据完整性：** 检查 FK 约束、唯一性约束、索引声明
- **管线完整性：** Round-trip Schema → IR → Schema 验证

---

*本报告由架构审计代理自动生成，人工审核后归档。*
