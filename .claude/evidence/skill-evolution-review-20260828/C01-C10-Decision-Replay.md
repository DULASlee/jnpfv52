# C01-C10 Decision Replay — 决策回放验证

> **版本**：v6.0-R1-Validation | **日期**：2026-08-28 | **状态**：🟢 R1=PASS 验收证据（2026-08-28 人工验收；本文件随 R1 冻结，F-R1-①）  
> **核心原则**：不得重新设计案例来迎合规则 | 必须识别 Ambiguous 情况

---

## Decision Replay 表

| Case | Initial State | Missing Context | Trigger | Context Type | Level | Expansion | Stop | Final Decision | Expected | Actual | Ambiguous? | Result |
|------|---------------|-----------------|---------|--------------|-------|------------|------|----------------|----------|--------|------------|--------|
| C01 | EmailService.Delete catch 丢栈 | 无（当前类证据充分） | NO | — | — | 不触发 | — | GO | GO | GO | NO | **PASS** |
| C02 | FileService.FileDown 调用 IFileManager.DownloadFileByType | IFileManager.DownloadFileByType 返回什么？ownership 如何交接？ | YES | Call | Level 1 | 从接口签名推断 → 返回 FileStreamResult | 已获得足够证据 | GO | GO | GO | NO | **PASS** |
| C03 | OrderService.Save 多步 DB 操作无事务 | OrderService 的 DI 生命周期？[UnitOfWork] 是否可用？ | YES | DI | Level 1 | 从 DI 注册代码推断 → Scoped，[UnitOfWork] 可用 | 已获得足够证据 | GO | GO | GO | NO | **PASS** |
| C04 | FileService.DownloadAll 临时目录未清理 | 谁消费这个临时目录？何时结束？ | YES | Ownership | Level 0 | 人工描述 → "临时目录由前端下载后清理" | 已获得足够证据 | STOP | STOP | STOP | NO | **PASS** |
| C05 | OrderService.Save 无事务（同 C03） | 同 C03 | YES | DI | Level 1 | 同 C03 | 同 C03 | GO | GO | GO | NO | **PASS** |
| C06 | FileService.DownloadAll 临时目录未清理（同 C04） | 同 C04 | YES | Ownership | Level 0 | 同 C04 | 同 C04 | STOP | STOP | STOP | NO | **PASS** |
| C07 | ScheduleService.Delete N+1 | 实际调用次数？数据规模？ | YES | Data-flow | Level 1 → Level 2 | Level 1 无法确定 → 需要 Level 2 | Level 2 未实现 | NEED EVIDENCE | NEED EVIDENCE | NEED EVIDENCE | NO | **PASS** |
| C08 | OrderService（JNPF.Extend）调用 WorkflowService（JNPF.WorkFlow） | WorkflowService.CreateTask 的事务边界？ | YES | Cross-layer | Level 1 | 尝试获取 WorkflowService 上下文 | 跨模块边界 | STOP | STOP | STOP | NO | **PASS** |
| C09 | FileService.DownloadAll 临时目录未清理（Medium Finding） | 同 C04 | YES | Ownership | Level 0 | 同 C04 | 成本 > 收益 | STOP | STOP | STOP | **YES** | **PARTIAL** |
| C10 | 任意 Finding | — | YES | 任意 | 任意 | 限制扩展范围：最多 1 层 | 超过范围 | STOP | STOP | STOP | NO | **PASS** |

---

## 详细分析

### C01：当前类证据足够 → 不 Expansion

**Initial State**：EmailService.Delete catch 丢栈

```csharp
catch (Exception) { _db.RollbackTran(); throw Oops.Oh(COM1002); }
```

**Missing Context**：无（当前类证据充分）

**Trigger**：NO（当前类证据充分）

**Expansion**：不触发

**Final Decision**：GO（修复为 `catch (Exception ex) { ... throw new AppFriendlyException(..., ex); }`）

**Expected**：GO

**Actual**：GO

**Ambiguous?**：NO

**Result**：**PASS**

**验证**：✅ 正确识别"当前类证据充分"，不触发 Expansion。

---

### C02：当前类证据不足 → 请求 Call Context

**Initial State**：FileService.FileDown 调用 IFileManager.DownloadFileByType

```csharp
var fileStreamResult = await _fileManager.DownloadFileByType(systemFilePath, fileName);
using var fs = fileStreamResult.FileStream;
```

**Missing Context**：IFileManager.DownloadFileByType 返回什么？ownership 如何交接？

**Trigger**：YES（当前类证据不足）

**Context Type**：Call

**Level**：Level 1

**Expansion**：从接口签名推断 → 返回 FileStreamResult

**Stop**：已获得足够证据

**Final Decision**：GO（using var fs 正确管理）

**Expected**：GO

**Actual**：GO

**Ambiguous?**：NO

**Result**：**PASS**

**验证**：✅ 正确识别缺失 Call Context，Level 1 推断成功，重新判定 GO。

---

### C03：当前类证据不足 → 请求 DI Context

**Initial State**：OrderService.Save 多步 DB 操作无事务

```csharp
public async Task Save(string id, OrderCrInput input) {
    // 多步 DB 操作
    await _repository.AsSugarClient().Insertable(orderEntryList).ExecuteCommandAsync();
}
```

**Missing Context**：OrderService 的 DI 生命周期？[UnitOfWork] 是否可用？

**Trigger**：YES（当前类证据不足）

**Context Type**：DI

**Level**：Level 1

**Expansion**：从 DI 注册代码推断 → Scoped，[UnitOfWork] 可用

**Stop**：已获得足够证据

**Final Decision**：GO（+1 using +2 [UnitOfWork]）

**Expected**：GO

**Actual**：GO

**Ambiguous?**：NO

**Result**：**PASS**

**验证**：✅ 正确识别缺失 DI Context，Level 1 推断成功，重新判定 GO。

---

### C04：当前类证据不足 → 请求 Ownership Context

**Initial State**：FileService.DownloadAll 临时目录未清理

```csharp
string directoryPath = Path.Combine(App.GetConfig<AppOptions>("JNPF_App", true).SystemPath, "TemporaryFile", fileName);
await _fileManager.CopyFile(filePath, Path.Combine(directoryPath, item.fileName));
```

**Missing Context**：谁消费这个临时目录？何时结束？

**Trigger**：YES（当前类证据不足）

**Context Type**：Ownership

**Level**：Level 0

**Expansion**：人工描述 → "临时目录由前端下载后清理"

**Stop**：已获得足够证据

**Final Decision**：STOP（跨层 ownership，不能局部修复）

**Expected**：STOP

**Actual**：STOP

**Ambiguous?**：NO

**Result**：**PASS**

**验证**：✅ 正确识别缺失 Ownership Context，Level 0 获取成功，重新判定 STOP。

---

### C05：Expansion 后 → GO

**同 C03**。

**Result**：**PASS**

---

### C06：Expansion 后 → STOP

**同 C04**。

**Result**：**PASS**

---

### C07：Expansion 后 → NEED EVIDENCE

**Initial State**：ScheduleService.Delete N+1

```csharp
foreach (var item in pushList) {
    var users = await _repository.AsSugarClient().Queryable<ScheduleUser>().Where(...).ToListAsync();
}
```

**Missing Context**：实际调用次数？数据规模？

**Trigger**：YES（当前类证据不足）

**Context Type**：Data-flow

**Level**：Level 1 → Level 2

**Expansion**：Level 1 无法确定 → 需要 Level 2

**Stop**：Level 2 未实现

**Final Decision**：NEED EVIDENCE（Level 2 未实现，冻结待运行时证据）

**Expected**：NEED EVIDENCE

**Actual**：NEED EVIDENCE

**Ambiguous?**：NO

**Result**：**PASS**

**验证**：✅ 正确识别缺失 Data-flow Context，Level 1 无法满足，Level 2 未实现，判定 NEED EVIDENCE。

---

### C08：Expansion 达到边界 → STOP

**Initial State**：OrderService（JNPF.Extend）调用 WorkflowService（JNPF.WorkFlow）

```csharp
// OrderService（JNPF.Extend）调用 WorkflowService（JNPF.WorkFlow）
await _workflowService.CreateTask(...);
```

**Missing Context**：WorkflowService.CreateTask 的事务边界？

**Trigger**：YES（当前类证据不足）

**Context Type**：Cross-layer

**Level**：Level 1

**Expansion**：尝试获取 WorkflowService 上下文

**Stop**：跨模块边界（JNPF.Extend → JNPF.WorkFlow）

**Final Decision**：STOP（跨模块边界，避免跨模块传染）

**Expected**：STOP

**Actual**：STOP

**Ambiguous?**：NO

**Result**：**PASS**

**验证**：✅ 正确识别跨模块边界，触发 Stop 条件。

---

### C09：Expansion 成本超过收益 → STOP

**Initial State**：FileService.DownloadAll 临时目录未清理（Medium Finding）

**Missing Context**：同 C04

**Trigger**：YES（当前类证据不足）

**Context Type**：Ownership

**Level**：Level 0

**Expansion**：同 C04

**Stop**：成本 > 收益

**Final Decision**：STOP

**Expected**：STOP

**Actual**：STOP

**Ambiguous?**：**YES**

**问题**：如何判断"成本 > 收益"？

- **成本**：Level 0 人工描述，成本 = Medium
- **收益**：Medium Finding，收益 = Low
- **判断**：Medium > Low → 成本 > 收益 → STOP

**但是**：

- 什么是"Medium"成本？什么是"Low"收益？没有度量标准。
- 如果人工描述只需要 1 分钟，成本是否还是 Medium？
- 如果 Medium Finding 实际上是高频调用，收益是否还是 Low？

**Result**：**PARTIAL**

**验证**：⚠️ 能推出 STOP，但"成本 > 收益"判断依赖主观评估，无操作规则。

---

### C10：Expansion 不得演化为全仓扫描

**Initial State**：任意 Finding

**Missing Context**：—

**Trigger**：YES

**Context Type**：任意

**Level**：任意

**Expansion**：限制扩展范围：最多 1 层

**Stop**：超过范围

**Final Decision**：STOP

**Expected**：STOP

**Actual**：STOP

**Ambiguous?**：NO

**Result**：**PASS**

**验证**：✅ 正确限制扩展范围，防止全仓扫描。

---

## 汇总

| Result | 计数 | 说明 |
|--------|------|------|
| PASS | 9 | C01-C08, C10 |
| PARTIAL | 1 | C09（成本 > 收益判断依赖主观评估） |
| FAIL | 0 | 无 |

**关键发现：**

1. **C01-C08, C10 都能推出唯一、可重复的决策**
2. **C09 是唯一的 PARTIAL**：成本 > 收益判断依赖主观评估，无操作规则
3. **核心问题**：Context Budget 不可操作（与 R1-Validation-Matrix 的 R1-07 FAIL 一致）

---

## 与 R1-Validation-Matrix 的一致性

| Matrix ID | Case | 一致性 |
|-----------|------|--------|
| R1-07 Context Budget | C09 | ✅ 一致（都是 Context Budget 不可操作） |
| 其他 Matrix ID | C01-C08, C10 | ✅ 一致（都是 PARTIAL，概念清晰但缺少操作规则） |

**结论**：Decision Replay 与 Validation Matrix 一致，R1 = PARTIAL。

---

**本回放 Pre-Patch 结果：R1 = PARTIAL（9 PASS / 1 PARTIAL / 0 FAIL）。**

---

## Post-Patch 重放（基于 R1-Operationalization-Patch.md）

### C01-C08, C10 重跑结果（无变化）

上述九个案例本身不依赖主观判据，Patch 后重跑结果与 Pre-Patch 一致（均 PASS）。不再展开。

---

### C09 Post-Patch 重放（重点改造案例）

**场景**：FileService.DownloadAll 临时目录未清理（R-03，Risk=Medium）

#### Step 1—Nature 判定（Patch §1.3）

- 先看 Local：不成立（创建临时目录的使用方在本类外）
- 再看 Regional：成立（FileService 直接调用 IFileManager 接口，接口在 JNPF.Common.Core，边界清晰）
- → **Nature = Regional**，不升 Systemic

#### Step 2—Budget 分档（Patch §1.2 Medium×Regional）

**D=2, A=4, I=1, S=1**（Depth / Artifact / Iteration / Scope）

#### Step 3—Expansion Iteration 1

获取 Ownership Context（Level 0）：

| 五元组 | 内容 |
|--------|------|
| Claim | 「临时目录由前端下载后消费，本类不能局部释放」 |
| Evidence | 人工描述 + **FileService.cs:240-264 无 finally 清理（v2 实证：2026-08-28 Read 真实代码复核，方法体 240-264，临时目录 244 行创建，无任何清理路径；zip 经 263 行 URL 交由 `/api/File/Download`（同文件 271 行）下游消费——原引用 240-258 系行号错误，已修正）** |
| Impact | 若 Claim 成立→跨层 ownership→本类不能修→STOP |
| Confidence | **Medium**（Level 0 人工，按 Patch §2.2） |
| Decision | **STOP** |

#### Step 4—五元组检查（Patch §2.3）

1. Claim 可证伪？✅（按 Patch v2 §2.5 三问：FQ-1 反例存在——若本类有 finally 清理则 Claim 为假，**2026-08-28 Read FileService.cs:240-264 实证为无**；FQ-2 对象绑定——FileService.DownloadAll 的 TemporaryFile 目录生命周期；FQ-3 一致性——行号区间可双 Agent 复核。未命中 N1-N5 任何反例类型）
2. Evidence 可回溯？✅（file:line + 人工描述）
3. Impact 链完整？✅（成立→STOP；不成立→需重新判定）
4. Decision 唯一？✅（五元组已直接对应 STOP）
5. Confidence ≥ Medium？✅（Medium）

→ **STOP-1 Evidence Sufficient 命中**，不进入后续 STOP-2/3。

#### Step 5—与 Pre-Patch 对比

| 项 | Pre-Patch | Post-Patch |
|----|-----------|------------|
| 停止依据 | 「成本 > 收益」（主观） | 「五元组 Sufficient」（可判） |
| 不同人能否得到相同结果 | ❌ 不一定 | ✅ 能 |
| Ambiguous | YES | **NO** |
| Result | PARTIAL | **PASS** |

---

### 新增 Positive Control Cases (PC)：应该「继续」获取 Context

#### PC01：现有证据不完整，不能进入 STOP-1

**场景**：OrderService.Save 多步 DB 无事务（Risk=High, Nature=Regional）

- Budget：D=2, A=6, I=2, S=1
- Iteration 1 只获取到「OrderService = Scoped」
- 五元组检查：Claim 「需要 [UnitOfWork]」 → Impact 需要确认 [UnitOfWork] 可用 → 未取 → **Sufficient 不成立**
- **预期行为**：**不**触发 STOP-1，继续 Expansion
- **实际行为**：✅ Iteration 2 取 [UnitOfWork] 属性定义 → Sufficient 命中 → GO
- **结果**：**PASS**（Patch 能推动「继续获取」，不依赖直觉）

#### PC02：Claim 存在但未验证，不能直接 STOP

**场景**：一个 Finding 人工口头描述「这个服务不写文件」，但无代码证据

- 五元组检查：Evidence = 仅人工描述（1 条 Medium），Impact 无法反向验证→ **不 Sufficient**
- **预期行为**：继续 Expansion（若 Budget 允许）或 Escalate（若 Budget 耗尽，Decision 冻结为 NEED EVIDENCE + 交人动作，Patch v2 §4.0）
- **实际行为**：✅ Budget 内 → 取代码验证 → 确认不写 → Sufficient → GO
- **结果**：**PASS**

#### PC03：STOP-2 穷举检查发现未穷举完全

**场景**：一个 Ownership Finding，Budget 内还可获取 Call Context

- Iteration 1 取了 Ownership（Level 0）→ 五元组未完全 Sufficient（Confidence 仅 Medium）
- STOP-2 穷举：未取的 Call Context 可能翻转 Decision（若发现中间层已释放→本层不需释放→GO）
- **预期行为**：不触发 STOP-2，继续 Expansion
- **实际行为**：✅ Iteration 2 取 Call Context → 发现中间层未释放 → STOP-1 Sufficient → STOP
- **结果**：**PASS**（Patch 能发现「还能拿新证据且可能翻转」，不提前停止）

#### PC04：Budget 未耗尽时不得提前 Escalation

**场景**：Regional + High Risk，Budget I=2，当前 I=1

- **预期行为**：若证据不足，必须继续 Expansion，不得 Escalate（提前推卸，Y04 同源）
- **实际行为**：✅ 规则确认：Patch §4.1 E1 需同时满足 Budget 耗尽 + Confidence < Medium；本例 I=1<2 → 不能触发 E1
- **结果**：**PASS**

---

### 新增 Negative Control Cases (NC)：应该「停止」获取 Context

#### NC01：STOP-2 Decision Stable 自动触发

**场景**：ScheduleService N+1，Budget D=2, A=4, I=1

- Iteration 1 取 DataFlow Context→ Level 1 确认循环存在 + 全量查询
- 但未取到「实际调用次数」（需 Level 2/运行时）
- STOP-2 穷举：剩余可取 Context = {Call, DI, Ownership, CrossLayer}（DataFlow 已取）
- 对每一个模拟「最不利于 NEED EVIDENCE 的证据」：即使 Call Context 发现调用方循环不多次、即使 DI 发现单例，都**不能把 NEED EVIDENCE 翻为 GO**（GO 需要「确实存在 N+1」的硬证据，静态推断不到）
- **预期行为**：Decision Stable 命中 → STOP-2 → NEED EVIDENCE（不 Expansion）
- **实际行为**：✅
- **结果**：**PASS**（不主观判「成本>收益」，而是穷举翻转可能）

#### NC02：STOP-4 边界自动触发

**场景**：Regional 但 Budget 已耗到 S=1 上限，发现实际要跨入另一个 .csproj

- **预期行为**：不进入 STOP-1/2 判断，直接 STOP-4（边界优先）
- **实际行为**：✅ Patch §3.1 优先级序列确认：STOP-4 > STOP-5 > STOP-1 > STOP-2 > STOP-3
- **结果**：**PASS**

#### NC03：已有 Sufficient 证据不得为了「多看一点」而继续

**场景**：OrderService.Save 已取得完整五元组 + STOP-1 命中，但发现 Budget 未耗尽

- **预期行为**：**必须** STOP。不得以「既然还能看」为由继续
- **实际行为**：✅ Patch §3.1 STOP-1 优先于 STOP-3，Sufficient 命中即停
- **结果**：**PASS**（防止 v6 「有能力就无限制使用」）

#### NC04：新 Finding 不得自动并入当前 Expansion

**场景**：Expansion Iteration 1 发现 Delete 方法也有事务问题

- **预期行为**：新 Finding 写入 Future Evaluation Candidate，**不**并入当前 Expansion（不消耗当前 Budget）
- **实际行为**：✅ Patch §3.1 未提供「新 Finding 入口」，且 v4 Single Finding 纪律仍适用
- **结果**：**PASS**

---

### 新增 Escalation Cases (EC)：Budget 耗尽 → 交人

#### EC01：E1 BudgetExhausted 触发

**场景**：Critical 安全 Finding，Nature=Systemic，Budget D=3, A=10, I=3, S=2；实际需 D=4 才能判断

- Iteration 3 后，Depth=3 已耗尽，Confidence 仍为 Medium- (证据不完整)
- **预期行为**：E1 命中 → STOP-5 + Escalation Pack；Decision 冻结 NEED EVIDENCE（不自行 GO 也不自行 STOP，Patch v2 §4.0）
- **实际行为**：✅ 输出 Escalation Pack（`finding_decision_record=NEED_EVIDENCE`），包含建议是否扩 Budget
- **结果**：**PASS**

#### EC02：E2 EvidenceConflict 触发

**场景**：Level 1 推断 Scoped，但人工描述实际运行时被提升为 Singleton

- 两条证据都是 High（一个是代码，一个是人提供），但指向不同 Decision
- **预期行为**：E2 命中 → STOP-5 + Escalation Pack（Decision 冻结 NEED EVIDENCE），不自行裁决哪条可信
- **实际行为**：✅
- **结果**：**PASS**

#### EC03：E5 DecisionUnstable 触发

**场景**：同一 Finding 连续 2 次 STOP-2 穷举，一次推出 STOP，一次推出 GO（因为某个模拟假设差异）

- **预期行为**：E5 命中 → STOP-5 + Escalation Pack（Decision 冻结 NEED EVIDENCE），不强行选一个
- **实际行为**：✅
- **结果**：**PASS**

---

## Post-Patch 汇总

| Result | 计数 | 明细 |
|--------|------|------|
| PASS | 20 | C01-C08, C10, C09重跑, PC01-PC04, NC01-NC04, EC01-EC03 |
| PARTIAL | 0 | — |
| FAIL | 0 | — |

**关键发现**：

1. **C09 重跑从 PARTIAL → PASS**：不再依赖主观「成本>收益」，而是五元组 Sufficient 判定
2. **PC 系列验证了「什么时候应该继续」**：Patch 不仅能阻止错误（X01-X08），也能推动正确（PC01-PC04）
3. **NC 系列验证了「什么时候应该停止」**：STOP-2 穷举 + STOP-4 优先 + STOP-1 不贪多
4. **EC 系列验证了「什么时候必须交人」**：E1/E2/E5 三种 Escalation 可自动触发

## 与 Pre-Patch 对比

| 项 | Pre-Patch | Post-Patch |
|----|-----------|------------|
| 能阻止错误行为（Negative） | ✅ X01-X08 | ✅ 保持 |
| 能指导正确行为（Positive） | ❌ 不具备 | ✅ PC01-PC04 |
| 能自动识别 Escalation | ❌ 不具备 | ✅ EC01-EC03 |
| C09 主观判断 | ❌ PARTIAL | ✅ PASS |

**本回放 Post-Patch（v2）结果：重放 20/20 PASS（C01-C10 含 C09 重跑 + PC01-04 + NC01-04 + EC01-03）。**

**R1 状态声明**：Decision Replay 侧证据链满足"可执行、可复验、可停止、可升级"四项要求。**2026-08-28 首席架构师人工验收已裁定 R1 = PASS**（见 `R1-Validation-Review-Pack.md` §10），本回放文件作为 PASS 验收证据随 R1 冻结（F-R1-①）。
