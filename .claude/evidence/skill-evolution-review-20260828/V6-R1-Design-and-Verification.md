# V6 R1 Design and Verification — R1 设计与验证

> **版本**：v6.0-R1-draft | **日期**：2026-08-28 | **状态**：R1 设计规格（待人工验收）  
> **基于**：V6-Context-Model.md + Context-Expansion-Rules.md + Context-Level-Model.md + Context-Budget.md  
> **纪律**：不修改 v4 已冻结协议；不实现 Level 2 工具；不审计 JNPF 新类

> ⚠️ **操作性规则已更新（2026-08-28 Patch v2 横幅）**：**唯一操作源 = `R1-Operationalization-Patch.md` v2**。本文件是 R1 概念设计与案例清单，以下被 Patch 取代：
> - **Q3"成本是否合理"、Q5 终止条件③"成本超过收益"、Q8 防全仓第4条"成本评估"、Q6"成本合理"** → Patch §1.2 分档 Budget + §3.1 STOP-1~5。
> - **C09（§3.2 与 §3.3 详细）**原表述"成本>收益 → STOP"**作废**：C09 现由 STOP-1 五元组 Sufficient 判定（见 Decision Replay 文件 Post-Patch 节）。案例本身不重设计，仅停止依据从主观成本评估改为可判五元组——符合架构师"不重新设计案例迎合规则"要求。
> - **§4.3 验收指标**（≥90% 等百分比）保留为 R2+ 量化目标参考，**不作为 R1 PASS 判据**（R1 用 Decision Replay + Counterexample 定性验收）。
>
> §3.2 C01-C10 案例的**场景定义**继续有效（是验证输入），仅"预期行为的停止依据"表述随 Patch 更新。

---

## 1. R1 设计总结

### 1.1 R1 交付物

| # | 交付物 | 状态 | 说明 |
|---|--------|------|------|
| 1 | V6-Context-Model.md | ✅ 完成 | 核心概念模型（5 种 Context Type + Context Unit + Context Dependency + Trigger/Stop Conditions） |
| 2 | Context-Expansion-Rules.md | ✅ 完成 | 扩展规则（触发/终止条件 + 输出字段 + 与 v4 兼容性） |
| 3 | Context-Level-Model.md | ✅ 完成 | Level 0/1/2 定义（输入/输出/可信度/成本/升级/降级） |
| 4 | Context-Budget.md | ✅ 完成 | Context Budget 定义（时间/复杂度/准确性 + 成本 > 收益判定） |
| 5 | V6-R1-Design-and-Verification.md | ✅ 完成 | 本规格（10 个核心问题回答 + 10 个抽象验证案例） |

### 1.2 R1 核心设计

**v6.0 = Context-Aware Class Refactoring**

- **核心升级**：从单类证据判断 → 在必要时进行受纪律约束的最小跨类 Context Expansion → 基于扩展证据重新进入 GO/STOP/NEED 决策
- **核心纪律**：Evidence Expansion ≠ Scope Expansion
- **核心机制**：Context Model + Context Expansion Rules + Context Level Model + Context Budget

---

## 2. 回答 10 个核心问题（Q1-Q10）

### Q1：什么时候一个 Finding 有资格请求跨类 Context？

**当且仅当以下三个条件同时满足时：**

1. Finding 真实存在（不是 False Positive）
2. 当前类证据不足（无法安全判定 GO/STOP/NEED）
3. 缺失的上下文直接影响判定（不是"可能有用"）

**详见**：Context-Expansion-Rules.md §3

### Q2：请求哪一种 Context？

**根据 Finding 的技术性质选择：**

- **跨类 ownership** → Ownership Context
- **跨类 DI 生命周期** → DI Context
- **跨类调用链** → Call Context
- **跨类数据量传播** → Data-flow Context
- **跨层边界** → Cross-layer Context

**详见**：V6-Context-Model.md §2

### Q3：为什么这个 Context 是"必要"的，而不是"有用但非必要"？

**判断标准：**

1. 该证据是否直接影响当前 Finding 的 GO/STOP/NEED 判定？
2. 如果不获取该证据，能否安全地标记为 NEED EVIDENCE 并停止？
3. 获取该证据的成本是否合理？

**三个问题都 YES → 必要；任一 NO → 非必要。**

**详见**：V6-Context-Model.md §3

### Q4：最多扩展到哪里？

**最多扩展到：**

- **直接调用者/被调用者**（1 层）
- **直接注入的依赖**（1 层）
- **直接的资源生产者/消费者**（1 层）

**禁止扩展到：**

- 跨模块边界
- 间接调用链（除非直接影响判定）
- 全仓扫描

**详见**：V6-Context-Model.md §4

### Q5：什么时候必须停止 Expansion？

**以下任一条件满足时：**

1. 已获得足够证据
2. 达到 Level 上限（Level 0/1 无法满足）
3. 成本超过收益
4. 跨模块边界
5. 无法获取可靠证据

**详见**：Context-Expansion-Rules.md §4

### Q6：Level 0 → Level 1 → Level 2 的升级依据是什么？

**升级依据：**

1. **Level 0 → Level 1**：当人工描述成本过高或不可靠时，尝试从静态信息推断
2. **Level 1 → Level 2**：当静态信息无法提供足够证据时，需要工具辅助

**升级条件：**

- Level N 无法提供足够证据
- Level N+1 的成本合理
- Level N+1 的证据可信度更高

**详见**：Context-Level-Model.md §5

### Q7：Expansion 得到的新证据如何重新进入 v4 的 GO / STOP / NEED 门控？

**重新进入流程：**

```
Context Expansion 获得新证据
    ↓
新证据是否充分？
    ├─ YES → 重新判定 GO/STOP/NEED（使用 v4 三门判定逻辑）
    └─ NO  → 标记 NEED EVIDENCE（冻结待补充）
```

**判定逻辑：**

- **GO**：新证据支持在当前类修复 + 满足 v4 GO 六要素
- **STOP**：新证据不支持修复 + 满足 v4 STOP 十要素之一
- **NEED EVIDENCE**：新证据仍不足 + 无法获取更多证据

**详见**：Context-Expansion-Rules.md §7

### Q8：如何防止 Context Expansion 演变成"全仓扫描"？

**防止措施：**

1. **明确触发条件**：只有三个条件同时满足才允许触发
2. **明确终止条件**：五个条件任一满足必须停止
3. **限制扩展范围**：最多扩展到直接调用者/被调用者（1 层）
4. **成本评估**：Context Budget + 成本 > 收益则 STOP
5. **跨模块边界**：涉及跨模块依赖 → STOP

**详见**：Context-Expansion-Rules.md §8 + Context-Budget.md §3

### Q9：如何处理跨层 ownership 问题？

**处理原则：**

- **跨层 ownership = STOP**（不能局部修复）
- **不能因为 v6 有更强的取证能力，就试图在单层修复跨层问题**

**示例：**

- **Finding**：FileService.DownloadAll 临时目录未清理
- **Context Expansion**：Level 0 → 人工描述"临时目录由前端下载后清理"
- **判定**：STOP（跨层 ownership，不能局部修复）

**详见**：Context-Expansion-Rules.md §9

### Q10：如何证明 Expansion 后的 GO 比 Expansion 前的 GO 更有证据基础？

**证明方式：**

1. **记录 Expansion 前的证据**：当前类证据
2. **记录 Expansion 后的证据**：当前类证据 + 跨类证据
3. **对比证据充分性**：Expansion 后的证据更充分（覆盖了跨类边界）
4. **记录判定理由**：DecisionRationale 字段说明为什么 Expansion 后判定 GO

**示例：**

- **Expansion 前**：OrderService.Save 多步 DB 操作，无事务 → STOP（跨类边界）
- **Expansion 后**：OrderService 是 Scoped + [UnitOfWork] 可用 → GO（跨类证据支持）
- **证明**：Expansion 后获得了 DI 生命周期 + [UnitOfWork] 可用性证据，覆盖了跨类边界

**详见**：Context-Expansion-Rules.md §10

---

## 3. R1 最小验证集（10 个抽象案例）

### 3.1 验证目标

**验证 v6 Context Model 本身是否自洽，不是增加 Golden Corpus。**

### 3.2 验证案例清单

| # | 场景 | 技术性质 | 预期行为 | 验证目标 |
|---|------|----------|----------|----------|
| C01 | 当前类证据足够 → 不 Expansion | 任意 | 直接 GO/STOP/NEED | 验证不触发 Expansion |
| C02 | 当前类证据不足 → 请求 Call Context | Call | 触发 Expansion → Level 0/1 | 验证 Call Context 请求 |
| C03 | 当前类证据不足 → 请求 DI Context | DI | 触发 Expansion → Level 1 | 验证 DI Context 请求 |
| C04 | 当前类证据不足 → 请求 Ownership Context | Ownership | 触发 Expansion → Level 0 | 验证 Ownership Context 请求 |
| C05 | Expansion 后 → GO | UnitOfWork | STOP → Expansion → GO | 验证 STOP → GO 路径 |
| C06 | Expansion 后 → STOP | Cross-layer ownership | STOP → Expansion → STOP | 验证 STOP → STOP 路径 |
| C07 | Expansion 后 → NEED EVIDENCE | N+1 | STOP → Expansion → NEED EVIDENCE | 验证 STOP → NEED EVIDENCE 路径 |
| C08 | Expansion 达到边界 → STOP | Cross-module | STOP → Expansion → STOP | 验证跨模块边界 |
| C09 | Expansion 成本超过收益 → STOP | Medium Finding | STOP → Expansion → STOP | 验证成本 > 收益 |
| C10 | Expansion 不得演化为全仓扫描 | 任意 | STOP → Expansion → STOP | 验证全仓扫描禁止 |

### 3.3 验证案例详细设计

#### C01：当前类证据足够 → 不 Expansion

**场景**：EmailService.Delete catch 丢栈（Golden #1）

**当前类证据**：
```csharp
catch (Exception) { _db.RollbackTran(); throw Oops.Oh(COM1002); }
```

**预期行为**：
- 当前类证据充分（catch 丢栈）
- 不触发 Context Expansion
- 直接判定 GO（修复为 `catch (Exception ex) { ... throw new AppFriendlyException(..., ex); }`）

**验证目标**：验证不触发 Expansion

---

#### C02：当前类证据不足 → 请求 Call Context

**场景**：FileService.FileDown 调用 IFileManager.DownloadFileByType

**当前类证据**：
```csharp
var fileStreamResult = await _fileManager.DownloadFileByType(systemFilePath, fileName);
using var fs = fileStreamResult.FileStream;
```

**缺失证据**：IFileManager.DownloadFileByType 返回什么？ownership 如何交接？

**预期行为**：
- 当前类证据不足（不知道 IFileManager.DownloadFileByType 返回什么）
- 触发 Context Expansion → 请求 Call Context
- Level 1 → 从接口签名推断 → IFileManager.DownloadFileByType 返回 FileStreamResult
- 重新判定 GO（using var fs 正确管理）

**验证目标**：验证 Call Context 请求

---

#### C03：当前类证据不足 → 请求 DI Context

**场景**：OrderService.Save 无事务

**当前类证据**：
```csharp
public async Task Save(string id, OrderCrInput input) {
    // 多步 DB 操作
    await _repository.AsSugarClient().Insertable(orderEntryList).ExecuteCommandAsync();
}
```

**缺失证据**：OrderService 的 DI 生命周期？[UnitOfWork] 是否可用？

**预期行为**：
- 当前类证据不足（不知道 OrderService 的 DI 生命周期）
- 触发 Context Expansion → 请求 DI Context
- Level 1 → 从 DI 注册代码推断 → OrderService 是 Scoped，[UnitOfWork] 可用
- 重新判定 GO（+1 using +2 [UnitOfWork]）

**验证目标**：验证 DI Context 请求

---

#### C04：当前类证据不足 → 请求 Ownership Context

**场景**：FileService.DownloadAll 临时目录未清理

**当前类证据**：
```csharp
string directoryPath = Path.Combine(App.GetConfig<AppOptions>("JNPF_App", true).SystemPath, "TemporaryFile", fileName);
await _fileManager.CopyFile(filePath, Path.Combine(directoryPath, item.fileName));
```

**缺失证据**：谁消费这个临时目录？何时结束？

**预期行为**：
- 当前类证据不足（不知道谁消费临时目录）
- 触发 Context Expansion → 请求 Ownership Context
- Level 0 → 人工描述 → "临时目录由前端下载后清理"
- 重新判定 STOP（跨层 ownership，不能局部修复）

**验证目标**：验证 Ownership Context 请求

---

#### C05：Expansion 后 → GO

**场景**：OrderService.Save 无事务（同 C03）

**预期行为**：
- 初始判定：STOP（跨类边界）
- Context Expansion：Level 1 → OrderService 是 Scoped，[UnitOfWork] 可用
- 重新判定：GO（+1 using +2 [UnitOfWork]）

**验证目标**：验证 STOP → GO 路径

---

#### C06：Expansion 后 → STOP

**场景**：FileService.DownloadAll 临时目录未清理（同 C04）

**预期行为**：
- 初始判定：STOP（跨层 ownership）
- Context Expansion：Level 0 → 人工描述"临时目录由前端下载后清理"
- 重新判定：STOP（跨层 ownership，不能局部修复）

**验证目标**：验证 STOP → STOP 路径

---

#### C07：Expansion 后 → NEED EVIDENCE

**场景**：ScheduleService.Delete N+1

**当前类证据**：
```csharp
foreach (var item in pushList) {
    var users = await _repository.AsSugarClient().Queryable<ScheduleUser>().Where(...).ToListAsync();
}
```

**缺失证据**：实际调用次数？数据规模？

**预期行为**：
- 初始判定：STOP（跨类边界）
- Context Expansion：Level 1 → 无法确定数据规模
- 需要 Level 2（Roslyn call-graph + runtime profiling）→ Level 2 未实现
- 重新判定：NEED EVIDENCE（Level 2 未实现，冻结待运行时证据）

**验证目标**：验证 STOP → NEED EVIDENCE 路径

---

#### C08：Expansion 达到边界 → STOP

**场景**：跨模块调用

**当前类证据**：
```csharp
// OrderService（JNPF.Extend）调用 WorkflowService（JNPF.WorkFlow）
await _workflowService.CreateTask(...);
```

**缺失证据**：WorkflowService.CreateTask 的事务边界？

**预期行为**：
- 初始判定：STOP（跨类边界）
- Context Expansion：尝试获取 WorkflowService 上下文
- 达到边界：跨模块边界（JNPF.Extend → JNPF.WorkFlow）
- 重新判定：STOP（跨模块边界，避免跨模块传染）

**验证目标**：验证跨模块边界

---

#### C09：Expansion 成本超过收益 → STOP

**场景**：FileService.DownloadAll 临时目录未清理（Medium Finding）

**预期行为**：
- 初始判定：STOP（跨层 ownership）
- Context Expansion：Level 0 → 人工描述 ownership 链
- 成本评估：Cost=Medium（Level 0，人工描述）, Benefit=Low（Medium Finding）
- 成本 > 收益 → STOP（即使获取证据，也只能判定 STOP）

**验证目标**：验证成本 > 收益

---

#### C10：Expansion 不得演化为全仓扫描

**场景**：任意 Finding

**预期行为**：
- 触发 Context Expansion
- 限制扩展范围：最多 1 层（直接调用者/被调用者）
- 禁止扩展到：跨模块边界 / 间接调用链 / 全仓扫描
- 如果超过范围 → STOP

**验证目标**：验证全仓扫描禁止

---

## 4. R1 验收标准

### 4.1 核心验收：Context Expansion Decision Quality

**给定一个 Finding，Skill 是否能够正确回答：**

1. "我是否需要扩展上下文？"
2. "需要什么上下文？"
3. "扩展到什么程度？"
4. "什么时候停止？"
5. "拿到证据后如何重新决策？"

### 4.2 验收案例

**10 个抽象验证案例（C01-C10）全部通过 = R1 验收通过。**

### 4.3 验收指标

| 指标 | 目标 |
|------|------|
| **Correct Decision Rate** | ≥ 90%（10 个案例中 ≥ 9 个正确） |
| **False Positive Rate** | ≤ 5%（不误触发 Expansion） |
| **False Negative Rate** | ≤ 5%（不漏触发 Expansion） |
| **Context Expansion Accuracy** | ≥ 85%（Expansion 后判定正确） |
| **Convergence Rate** | ≥ 95%（不该继续时停止） |

---

## 5. R1 与 v4/v5 的关系

### 5.1 继承 v4 核心纪律

- **P0 先行**：Context Expansion 不替代 P0
- **Finding ≠ Fix**：Context Expansion 是为了更好判定，不是为了自动修复
- **GO/STOP/NEED 三门**：Context Expansion 后仍必须进入三门判定
- **Semantic Budget**：Context Expansion 获取的证据必须纳入 Semantic Budget 评估
- **Single commit**：Context Expansion 不引入额外提交
- **Convergence**：Context Expansion 有终止条件，不能无限扩展

### 5.2 v5 是增量规则细化

- **P2/P3/P4**：后处理查表 + 字段规范
- **不影响 v6 设计**：v6 是在 v4 基础上的架构升级，不是 v5 的延续

### 5.3 v6 是架构升级

- **核心升级**：从单类证据判断 → Context-Aware Class Refactoring
- **核心机制**：Context Model + Context Expansion Rules + Context Level Model + Context Budget
- **核心纪律**：Evidence Expansion ≠ Scope Expansion

---

## 6. R1 风险与缓解

| 风险 | 缓解措施 |
|------|----------|
| **Context Expansion 过度** | 明确触发/终止条件 + Context Budget |
| **弱化 v4 纪律** | 明确继承 v4 核心纪律 |
| **Level 2 成本过高** | 先实现 Level 0/1，Level 2 留作未来工作 |
| **验证案例不足** | 10 个抽象案例覆盖所有核心场景 |

---

## 7. R1 下一步

### 7.1 R1 完成后

**R1 完成后 → 提交 R1 Review Pack → 暂停 → 等人工批准。**

### 7.2 人工批准后

**人工批准后 → 进入 R2 Context Acquisition（Level 0/1 实现）。**

### 7.3 禁止事项

- ❌ 不进入 R2/R3/R4（未经批准）
- ❌ 不修改 JNPF 生产代码
- ❌ 不审计 JNPF 新类
- ❌ 不 Fix 任何 Finding
- ❌ 不开发 JnpfAnalyzer
- ❌ 不开发 Roslyn 自动分析工具
- ❌ 不搭建数据库/性能测试环境
- ❌ 不增加 Golden Example
- ❌ 不修改 MASTER/L1/L2

---

## 8. 总结

V6 R1 Design and Verification = **5 个交付物 + 10 个核心问题回答 + 10 个抽象验证案例 + 验收标准**

- **交付物**：V6-Context-Model / Context-Expansion-Rules / Context-Level-Model / Context-Budget / V6-R1-Design-and-Verification
- **核心问题**：Q1-Q10 全部回答
- **验证案例**：C01-C10 覆盖所有核心场景
- **验收标准**：Context Expansion Decision Quality

**R1 完成后 → 提交 R1 Review Pack → 暂停 → 等人工批准。**

---

**本规格待人工验收。验收通过后，才能进入 R2 Context Acquisition 设计阶段。**
