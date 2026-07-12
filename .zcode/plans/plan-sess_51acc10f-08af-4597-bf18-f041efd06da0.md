# Development Phase (开发阶段) SG3 质量修复计划

## 概况
对 Development 阶段（DeveloperSkill → Sandbox → ArchGuard → Tester → Deploy）进行 SG3 质量审查，修复 10 个缺陷（3 P0 + 4 P1 + 3 P2），新增 4 个测试用例。

---

## P0 修复（阻断/严重 — 3 项）

### H2: `EntityDesignRepository.PersistAsync` 非原子软删+插入
- **文件**: `EntityDesignRepository.cs:71-80`
- **问题**: 软删除和插入之间无事务保护，中途崩溃导致数据丢失或孤儿行
- **修复**: 用 `_db.Ado.BeginTran()` / `CommitTran()` / `RollbackTran()` 包裹两个操作，加 try-catch

### H3: `SkillHarness` tokenConsumed 永远为 0
- **文件**: `SkillHarness.cs:141,198,209`
- **问题**: `tokenConsumed` 初始化后永不更新，写入 DB 和返回给调用方始终为 0，误导性数据
- **修复**: 当前架构中 Skill 为确定性执行（零 LLM），token 跟踪尚未实现。添加明确注释说明状态，避免产生误导

### H4: `DeploySkillService` 硬编码凭据写入 IR 事件
- **文件**: `DeploySkillService.cs:130`
- **问题**: `defaultCredentials = new { username = "admin", password = "admin123" }` 写入 DeploymentVerified IR 事件 payload，安全风险
- **修复**: 从 payload 中移除 `defaultCredentials`——凭据不应出现在 IR 事件存储中

---

## P1 修复（高风险/中等 — 4 项）

### M2: `DeveloperSkillService` 语法校验错误仅打日志不传播
- **文件**: `DeveloperSkillService.cs:155-160`
- **问题**: `CodegenSyntaxValidator.EnsureValidSyntax` 抛出的异常被 catch 后仅 `LogWarning`，语法非法的代码仍被写入 workspace
- **修复**: 收集所有语法错误，在实体循环结束后统一抛出 `InvalidOperationException`（含全部错误摘要），阻止非法代码落盘

### M4: `TesterSkillInputBuilder` 4 处静默 JSON catch
- **文件**: `TesterSkillInputBuilder.cs:127,163,224,242`
- **问题**: 4 个 `catch { // ignore }` 静默吞噬 JSON 解析异常，难以排查数据问题
- **修复**: 每处加 `_logger.LogWarning(ex, "上下文说明")`，但由于这是 static 类，改为接受 `ILogger` 参数或使用 `Console.Error.WriteLine`。为此需将类从 static 改为实例方法，构造函数注入 `ILogger<TesterSkillInputBuilder>`

### M5: `ParseConfirmedFieldsFromFormPage` 硬编码领域字段
- **文件**: `TesterSkillInputBuilder.cs:159`
- **问题**: `fieldId is "reason" or "days" or "status"` 是请假领域硬编码，不通用
- **修复**: 改为从字段元数据派生 `Required` 判定——若字段在 FormPageIR 中标记了 `required: true` 则 Required=true，否则默认 false；移除硬编码领域逻辑

### M7: `DeveloperSkillOrchestrator` 硬编码 FragmentVersion
- **文件**: `DeveloperSkillOrchestrator.cs:171,199`
- **问题**: `FragmentVersion = 2` 和 `FragmentVersion = 3` 硬编码，新增事件类型时容易遗漏
- **修复**: 从已有快照中动态计算版本号（同 FragmentId 的最大 version + 1），或提取为命名常量

---

## P2 修复（低风险/代码质量 — 3 项）

### L1: 冗余 `await Task.CompletedTask;`
- **文件**: `DeveloperSkillService.cs:231` + `TesterSkillService.cs:120`
- **修复**: 删除两处无意义的 `await Task.CompletedTask;`

### L5: `DeveloperSkillService` 硬编码 channel="A/B"
- **文件**: `DeveloperSkillService.cs:218`
- **修复**: 改为 `"stable"`（更语义化）或从 `CodegenProfile` 派生

### L9: `TestCaseDeriver` 仅处理 int 类型
- **文件**: `TestCaseDeriver.cs:77`
- **问题**: `fields.Where(f => f.Type == "int")` 仅对 int 字段生成边界值/类型错误用例，decimal/DateTime/bool 等类型被忽略
- **修复**: 扩展类型映射表支持 `decimal`/`long`/`double`(数值边界)、`DateTime`(格式错误)、`bool`(非布尔值)、`Guid`(格式错误)

---

## 新增测试（PhaseB 测试套件 — 4 项）

### T11: EntityDesignRepository 事务原子性测试
- **文件**: 新建 `IrPhase4EntityDesignTests.cs` 或追加到现有 `IrPhase4DeveloperTests.cs`
- **验证点**: 
  1. 正常 Persist → 旧数据软删除 + 新数据插入
  2. 模拟插入失败 → 软删除被回滚（数据完整性）
- **实现**: SQLite 内存库 + 两轮 Persist + 中断注入

### T12: DeploySkillService 凭据安全性测试
- **文件**: `IrPhase4OrchestratorTests.cs` 追加
- **验证点**: `DeploymentVerified` payload 不含 `defaultCredentials` 字段

### T13: DeveloperSkill 语法错误传播测试
- **文件**: `IrPhase4DeveloperTests.cs` 追加
- **验证点**: 包含语法错误的模板渲染 → `ReasonAsync` 抛出异常（非静默忽略）

### T14: TestCaseDeriver 多类型覆盖测试
- **文件**: `IrPhase4TesterTests.cs` 追加
- **验证点**: decimal/DateTime/bool/Guid 类型字段均生成对应测试用例

---

## 执行顺序
1. P2（L1, L5, L9）— 快速低风险修复
2. P1（M2, M4, M5, M7）— 中等修复
3. P0（H2, H3, H4）— 关键修复
4. 新测试（T11-T14）
5. `dotnet build` + `dotnet build -c Release` → 0 错误
6. PhaseB 全量测试 → 全部通过
7. 注册新测试到 `TestRunner.cs`