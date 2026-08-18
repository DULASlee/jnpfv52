# Node 3+4 代码质量修复计划

基于自审查发现的 11 个问题，逐一修复。

---

## 🔴 高优先级（5 项）

### Fix-1: 日志注入 — SaNineViewCompiler
**文件**: `Sa\SaNineViewCompiler.cs`
- 添加 `ILogger<SaNineViewCompiler>` 字段 + 构造函数注入
- `Compile()` 方法：`LogInformation("SA九步编译开始 events={Count}", ...)` / `LogInformation("完成 耗时{Elapsed}ms")`
- `Compile()` 方法：异常分支 `LogWarning("编译失败: {Message}")`（在 throw 前）
- `CollectAssumptions()`：`LogDebug` 记录每条约定的推导细节
- `IsComplex()` 等关键分支：`LogDebug`

### Fix-2: 日志注入 — SystemDesignLockedCompletenessGate
**文件**: `Ir\SystemDesignLockedCompletenessGate.cs`
- 添加 `ILogger<SystemDesignLockedCompletenessGate>` 字段 + 构造函数注入
- 每个 `Fail(...)` 返回前：`LogWarning("R{N} 校验失败: {Message}")`
- `catch` 块：改裸 `catch` → `catch (Exception ex)` + `LogWarning(ex, "Skeleton解析失败")`
- 检测到 skeleton 缺失时：`LogDebug("跳过跨层校验，Skeleton 不存在")`

### Fix-3: 日志注入 — IrSchemaValidator
**文件**: `Ir\IrSchemaValidator.cs`
- 添加 `ILogger<IrSchemaValidator>` 字段 + 构造函数注入
- `Validate()` 入口：`LogDebug("Schema校验 eventType={EventType} payloadLen={Len}")`
- 空 payload 跳过时：`LogDebug`（非关键事件允许空 payload）
- 每个 `throw Oops.Bah` 前：`LogWarning("校验失败 eventType={EventType}: {Message}")`
- JsonException catch 中保留 `ex` 为 `Oops.Bah` 的 innerException

### Fix-4: 异常统一 — SaNineViewCompiler + DeveloperSkillService
- **SaNineViewCompiler line 38**: `throw new InvalidOperationException(...)` → `throw Oops.Bah(...)`
- **DeveloperSkillService line 97-98**: `catch { /* 容错 */ }` → `catch (JsonException ex) { _logger.LogWarning(ex, "SkeletonPayload 解析容错"); }`
- **IrSchemaValidator**: `JsonException` catch 中传 innerException：`throw Oops.Bah("...", ex)`（如果 Oops.Bah 支持）
- **TemplateContextBuilder**: 4 处裸 catch → `catch (JsonException) { /* 跳过非关键字段 */ }`

### Fix-5: SQL DDL 标识符转义 — DeveloperSkillService
**文件**: `Skills\DeveloperSkillService.cs` 的 `GenerateSqlFromTables`
- 列名包裹 `[{columnName}]`，表名包裹 `[{tableName}]`
- SQL 关键字检测（`Order`、`Group`、`User` 等保留字强制转义）

---

## 🟡 中优先级（5 项）

### Fix-6: FK 推导去重 — 新建 FkResolver
**新建文件**: `Sa\EntityRelationInferenceService.cs`（静态工具类）
- 提取 `GuessRefEntity(string fieldName, IReadOnlyList<PreAnalysisEntityDraft> entities)` 逻辑
- 提取 `EndsWith("Id")` + FK 推导逻辑
- `SaNineViewCompiler`（2 处）和 `PreAnalysisModel.MapRelations`（1 处）改用统一方法
- 确保 `ToField` 不再硬编码 `"id"`（从目标实体的主键列名推断）

### Fix-7: LlmJsonFixer 截断标记 — ParseResponse 通信
**文件**: `LlmGatewayService.cs`
- `ParseResponse` 方法签名改为返回 `(ChatCompletionResponse response, bool jsonWasFixed)`
- 调用方（`ChatAsync`）在 jsonWasFixed=true 时设置 `fallbackReason = "json_auto_fixed"`
- 调用方（`ChatAsync`）在 jsonWasFixed=true 时追加 audit log 标记

### Fix-8: CircuitBreaker.IsOpen → CheckAndTransition 重命名
**影响文件**:
- `Llm\LlmCircuitBreaker.cs`: 接口 `ILlmCircuitBreaker` 方法 `IsOpen` → `CheckAndTransition`
- `Llm\LlmCircuitBreaker.cs`: 实现方法重命名
- `LlmGatewayService.cs`: 2 处调用（line 168, 467）→ `CheckAndTransition`
- 接口 XML 文档更新说明"此方法有副作用：可能将 Open 转换为 HalfOpen"

### Fix-9: 异步边界修正
- **SystemDesignLockedCompletenessGate**: `ValidateAsync` 中 `_ = ct;` 删除，在循环或关键路径前插入 `ct.ThrowIfCancellationRequested()`
- **DeveloperSkillService.ReasonAsync**: 暂不改 `IAsyncEnumerable` 声明（接口契约），但将内部 `File.WriteAllText` → `await File.WriteAllTextAsync(...)` — 需验证方法签名支持 async

### Fix-10: CompletenessGate 假三元组
**文件**: `Ir\SystemDesignLockedCompletenessGate.cs`
- 不改变三元组来源（架构限制），但添加 `LogWarning` 当使用占位三元组时：
  `"SystemDesignLockedCompletenessGate 使用占位三元组 (gate/gate/gate)，Project可能租户感知不准确"`
- 同时更新 XML 文档说明适用边界

---

## 🟢 低优先级（2 项）

### Fix-11: 硬编码常量抽取
- **SaNineViewCompiler**: 抽取 `SaCompilerDefaults` 静态类（`DefaultSystemName = "业务系统"`、SQL 类型映射表、置信度阈值 `0.6m`/`0.7m`）
- **TemplateContextBuilder**: 抽取 `Ir2CodegenDefaults`（`FallbackBusName = "请假申请"`→`"BusinessEntity"` 并改为英文默认值以免领域污染）
- **DeveloperSkillService**: `"2.0.0-p9s2"` → `SkillVersion` 常量

### Fix-12: CircuitBreaker 定期清理
**文件**: `Llm\LlmCircuitBreaker.cs`
- 添加 `DateTime _lastCleanup = DateTime.UtcNow`
- 在 `GetOrAdd` 时检查：距上次清理 > 1h → 扫描并移除 `OpenSince` 超过 24h 且不在 `HalfOpen` 的 entry
- 使用 `_entries.TryRemove` 安全清理

---

## 实施顺序

| 序号 | 修复项 | 风险 | 预计改动行数 |
|------|--------|------|-------------|
| 1 | Fix-1 SaNineViewCompiler 日志 | 低 | +20 |
| 2 | Fix-2 SystemDesignLockedCompletenessGate 日志 | 低 | +25 |
| 3 | Fix-3 IrSchemaValidator 日志 | 低 | +20 |
| 4 | Fix-4 异常统一 | 低 | ~10 处修改 |
| 5 | Fix-5 SQL DDL 转义 | 低 | ~15 |
| 6 | Fix-6 FK 去重 | **中** | +80 新文件 + 3 处调用替换 |
| 7 | Fix-7 JSON fixer 标记 | 中 | ~20 修改 |
| 8 | Fix-8 IsOpen 重命名 | 低 | 4 处修改 |
| 9 | Fix-9 异步修正 | 低 | ~10 |
| 10 | Fix-10 假三元组日志 | 低 | +5 |
| 11 | Fix-11 常量抽取 | 低 | +40 |
| 12 | Fix-12 CB 清理 | 低 | +25 |
| 13 | `dotnet build` + `pnpm test:api` 回归 | — | — |

## 验收标准

1. `dotnet build backend/` → InteAssistant 项目 0 错误 0 警告
2. `E2E_PIPELINE_ID=311 pnpm test:api` 回归通过
3. 新日志输出可见（运行时验证）
4. 无硬编码中文在基础设施代码中（SaNineViewCompiler 常量类除外）