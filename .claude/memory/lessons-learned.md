# 踩坑记录与最佳实践

> 团队共享，提交到 Git。避免同一个坑踩两次。

---

## 2026-07-08 | Node 3+4 代码质量自审 — 9 维度系统性改进清单

**审查范围**：Node 3（SaNineViewCompiler / PreAnalysisModel / SystemDesignLockedCompletenessGate / IrSchemaValidator / TemplateContextBuilder / DeveloperSkillService）+ Node 4（LlmCircuitBreaker / LlmJsonFixer / LlmTokenEstimator / LlmGatewayService 重构）。

**审查维度**：业务逻辑完整性 / 日志 / 性能 / 数据一致性 / 内存泄漏 / 并发 / 高可用 / 高内聚 / 高扩展。

### 发现 1：日志覆盖严重不足 🔴 高优先级

**现象**：Node 3 六个文件中仅 `DeveloperSkillService` 注入 `ILogger`，其余五个文件零日志。Node 4 中仅 `LlmCircuitBreaker` 和 `LlmGatewayService` 有日志，`LlmJsonFixer` 修复成功/失败均静默。

**后果**：
- `SaNineViewCompiler` 编译失败无可追溯审计日志，只能靠返回值中的 `Stopwatch` 字段
- `SystemDesignLockedCompletenessGate` 校验失败原因仅存于返回值字符串，调用方丢弃则证据永久丢失
- `IrSchemaValidator` 校验失败仅抛异常，无法定位"第几次 LLM 重试后仍失败"
- `LlmJsonFixer` 修复成功时无迹可查，无法统计 LLM 输出质量退化趋势

**改进铁律**：
- 所有 `ITransient` 服务 MUST 注入 `ILogger<T>`（非可选的）
- Gate/Validator 失败 MUST `LogWarning`；编译/物化操作 MUST `LogInformation("开始/完成 {Entity} 耗时{Elapsed}ms")` 
- 吞异常/降级分支 MUST `LogWarning(ex, "降级原因")` 让运维可观测

---

### 发现 2：异常处理哲学不统一 🔴 高优先级

**现象**：同一 InteAssistant 模块内四种异常风格共存：
| 文件 | 风格 | 示例 |
|------|------|------|
| `SaNineViewCompiler` | 裸 CLR 异常 | `throw new InvalidOperationException("无业务事件")` |
| `IrSchemaValidator` | 业务友好异常 | `throw Oops.Bah("JSON解析失败")` |
| `SystemDesignLockedCompletenessGate` | 返回值错误码 | `return SkillValidationResult.Fail("...")` |
| `DeveloperSkillService` | 静默吞异常 | `catch { /* 容错 */ }` |

**后果**：
- 调用方 `catch (AppFriendlyException)` 抓不到 `InvalidOperationException` → 500 堆栈泄露到前端
- 静默吞异常的 `DeveloperSkillService.WriteModuleSkeleton` 让调用方误以为模块骨架已生成成功
- Gate 用返回值 vs Validator 用异常 — 同一个 pipeline 里两种错误传播方式让编排代码复杂化

**改进铁律**：
- 全模块统一：业务可恢复错误 → `throw Oops.Bah("人类可读原因")`，系统不可恢复 → `throw Oops.Oh("...")`
- **禁止裸 `catch { }`** — 最小 `catch (JsonException)`；必须吞异常时用 `catch (Exception ex) when (ex is not OutOfMemoryException)` + `LogWarning(ex, "非阻断降级")`
- Gate 保持返回 `SkillValidationResult`（聚合多规则结果），但内部单个断言失败应 `LogWarning` 而非仅拼字符串

---

### 发现 3：FK 推导逻辑三处重复 🟡 中优先级

**现象**：`EndsWith("Id")` + `GuessRefEntity` 逻辑出现在三个位置：
1. `SaNineViewCompiler.CompileCommandQuery` (line ~232)
2. `SaNineViewCompiler.CompileDataModel` (line ~312)
3. `PreAnalysisModel.MapRelations` (line ~141)

**后果**：修改 FK 推导规则需改三处，已出现语义不一致——`MapRelations` 硬编码 `ToField = "id"`，而 `CompileDataModel` 不硬编码。未来任何一处遗漏修改即产生 bug。

**改进要求**：抽取 `EntityRelationInferenceService` 或静态 `FkResolver` 作为单一真实源。

---

### 发现 4：同步/异步边界不一致 🟡 中优先级

**现象**：
- `SystemDesignLockedCompletenessGate.ValidateAsync` 声明 `Task<SkillValidationResult>` 但纯同步，`CancellationToken` 被 `_ = ct;` 丢弃
- `DeveloperSkillService.ReasonAsync` 声明 `IAsyncEnumerable` 但内部 `File.WriteAllText` 同步阻塞线程
- `TemplateContextBuilder.BuildFromSampleJson` 同步 `File.ReadAllText`

**后果**：假异步方法阻塞线程池线程，真异步调用方被欺骗以为不阻塞。`IAsyncEnumerable` 部分枚举（`break`）时文件写入半途而废。

**改进铁律**：
- 纯同步方法不要声明 `Task`/`IAsyncEnumerable` 返回类型
- 有 I/O 的异步方法用 `ConfigureAwait(false)` + `*Async` 后缀 API
- `CancellationToken` 要么真在循环/IO前 `ThrowIfCancellationRequested()`，要么别接受该参数

---

### 发现 5：基础设施代码含硬编码业务术语 🟢 低优先级

**现象**：
- `TemplateContextBuilder`：`BusName ?? "请假申请"`
- `SaNineViewCompiler`：`"用户"`、`"AD目录"`、`"标准业务处理"`
- SQL 类型映射：魔法值 `"NVARCHAR(255)"`、`"BIGINT"`、`"DECIMAL(18,2)"`、`0.6m` 无数值常量

**后果**：跨行业复用时出现"请假申请"鬼影数据；修改默认类型精度需全文搜索魔法数字。

**改进要求**：抽取常量类 `SaDefaults`/`SqlTypeDefaults`；业务术语默认值由配置或调用方传入，不硬编码。

---

### 发现 6：LlmJsonFixer 激进修复可能损坏合法 JSON 🟡 中优先级

**现象**：`FixTruncatedString` 在检测到未闭合引号时无条件追加 `"}]`。若截断点在嵌套对象中间层（如 `{"a": {"b": "未闭合`），追加 `"}]` 只闭合了内层字符串和外层对象，缺失中间层。

**后果**：可生成语法合法但语义错误的 JSON（缺字段），下游解析成功但读到不完整数据。

**改进要求**：截断修复成功后标记 `fallbackReason = "json_truncation_suspected"` 并在解析后做 schema 校验。

---

### 发现 7：CircuitBreaker._entries 无清理机制 🟢 低优先级

**现象**：`ConcurrentDictionary<string, CircuitEntry>` 只增不减。Provider 下线/重命名后旧 entry 永久残留（~100 bytes/entry）。

**后果**：长期运行微量内存泄漏。对生命周期短的 provider code 无实际影响。

**改进要求**：定期（每小时）扫描移除超过 24h 无调用的 entry，或换用 `MemoryCache` 带滑动过期。

---

### 发现 8：SystemDesignLockedCompletenessGate 假三元组 🟡 中优先级

**现象**：`TenantId = "gate"`, `ProjectId = "gate"`, `PipelineId = "gate"` — 注释承认"Gate 被调用时没有三元组上下文"，但违反宪法级 Triple-Key Iron Law (R12)。

**后果**：若 `EntityDesignProjector.Project` 内部按租户做 DB 查询，假 tenantId 查不到数据——当前恰好未触发，属于定时炸弹。

**改进要求**：Gate 应从调用方接收真实三元组；若短期做不到，至少 assert 式验证 Projector 不依赖租户隔离。

---

### 发现 9：SQL DDL 生成无标识符转义 🔴 高优先级

**现象**：`DeveloperSkillService.GenerateSqlFromTables` 用 `StringBuilder` 手工拼接 SQL，列名/表名不转义。

**后果**：遇到 `Order`、`Group`、`User` 等保留字列名生成非法 DDL，执行即报错。

**改进要求**：至少加方括号 `[{columnName}]`（SQL Server 方言），或直接用 SqlSugar `DbMaintenance` API 生成。

---

### 发现 10：C# `fixed` 关键字陷阱 ✅ 已修复

**场景**：`LlmJsonFixer.TryFix` 返回 `(string? Fixed, bool WasFixed)` tuple，解构时写 `var (fixed, wasFixed) = ...` 触发 CS1525（`fixed` 是 C# 关键字）。

**教训**：Tuple 解构变量名不能使用 C# 关键字（`fixed`、`string`、`int`、`class` 等）。改用 `fixedJson` 解决。

---

### 发现 11：`IsOpen()` 存在隐性状态变更 🟡 中优先级

**现象**：`LlmCircuitBreaker.IsOpen()` 内部调用 `EvaluateAndTransition()` 会将 Open→HalfOpen 转换。方法名暗示纯查询，实际是查询+变更。

**后果**：调用方在日志/监控循环中多次调 `IsOpen()` + `GetState()` 可能观察到不一致的状态。

**改进要求**：要么重命名为 `CheckAndTransition()` 揭示副作用，要么将转换逻辑移到 `RecordSuccess`/`RecordFailure` 的入口处。

---

### 跨节点总结：当前最薄弱的三项

1. **日志** — 半数以上文件零日志，生产故障排查靠猜
2. **异常处理统一性** — 四种风格共存，调用方心智负担重
3. **重复代码** — FK 推导三处重复，改一处漏两处 = bug

---

## 2026-06-05 | dotnet build 的 NU5026 pack 错误

**场景**：hook 中执行 `dotnet build --no-restore` 触发 NU5026
**后果**：编译失败，hook 误判为代码错误
**教训**：
- `--no-restore` 跳过包恢复但不跳过 pack，XML 文档文件缺失会报 NU5026
- 解决方案：加 `-p:IsPackable=false` 跳过 pack 步骤
- 参考：`.claude/hooks/guard-finish.mjs`

---

## 2026-06-05 | Windows 下 execSync 的 ETIMEDOUT 与 DLL 锁定

**场景**：后端服务运行时，hook 中 dotnet build 因 DLL 锁定阻塞直至超时
**后果**：60 秒超时后报 ETIMEDOUT，hook 误判为编译失败
**教训**：
- DLL 锁定 ≠ 代码错误，应降级为警告
- 超时应设为 30 秒（增量编译正常 5-15 秒，超过 30 秒大概率是锁定）
- 在 catch 中同时检测 `'is being used by another process'` 和 `ETIMEDOUT`

---

## 2026-06-05 | Node.js hook 脚本 Windows 兼容性

**场景**：`readFileSync('/dev/stdin')` 在 Windows 下不工作
**后果**：hook 无法读取 Claude Code 传入的 stdin JSON
**教训**：
- 必须用异步方式读取 stdin：`for await (const chunk of process.stdin)`
- 不能用 `readFileSync('/dev/stdin')`，Windows 没有 `/dev/stdin`
- `execSync` 的 `stdio: ['ignore', 'ignore', 'pipe']` 可捕获 stderr

---

## 2026-06-05 | npm view 在国内网络可能挂起

**场景**：guard-deps hook 中执行 `npm view lodash version` 查询版本
**后果**：某些包在国内网络下挂起 30-120 秒，阻塞 AI 工作流
**教训**：
- 必须设置 3 秒熔断：`execSync(..., { timeout: 3000 })`
- 查询失败时降级为跳过校验，不阻断

---

## 2026-06-05 | Hook 测试命令被自身 Hook 拦截

**场景**：用 Bash 工具测试 guard-bash hook，测试命令字符串中包含 `rmdir /s /q` 和 `npm install`
**后果**：guard-bash 和 guard-deps hook 拦截了测试命令本身
**教训**：
- 测试危险命令拦截 hook 时，必须将测试脚本写入文件再执行
- `node test-file.js` 命令不包含危险模式，不会被拦截
- 测试数据中的危险模式会被 hook 的 stdin 解析正确拦截（这是预期行为）

---

## 2026-06-05 | 多子项目结构下 node_modules 路径

**场景**：format-and-lint hook 硬编码 `./node_modules/.bin/`
**后果**：根目录无 node_modules，prettier/eslint 找不到
**教训**：
- JNPF 是多子项目结构（jnpf-web-vue3、jnpf-app-vue3 各有独立 node_modules）
- 必须从被编辑文件路径向上动态查找最近的 node_modules
- `findProjectRoot()` 函数实现：从文件路径向上遍历，检查 `node_modules/.bin` 是否存在
