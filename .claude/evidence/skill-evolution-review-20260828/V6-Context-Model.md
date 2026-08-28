# V6 Context Model — 核心概念模型

> **版本**：v6.0-R1-draft | **日期**：2026-08-28 | **状态**：R1 设计规格（待人工验收）  
> **基于**：v4.0 CALIBRATED + v5.0 增量 + v6.0-alpha Target  
> **纪律**：不修改 v4 已冻结协议；不实现 Level 2 工具；不审计 JNPF 新类

> ⚠️ **操作性规则已更新（2026-08-28 Patch v2 横幅）**：**唯一操作源 = `R1-Operationalization-Patch.md` v2**。本文件为概念模型，以下条款的操作性部分被取代：
> - **§3.2 第 3 问**（成本合理）与 **§9 Q3** → Patch §1.2 分档 Budget；
> - **§6.1 终止条件**（5 条旧表述，含"成本超过收益"）→ Patch §3.1 STOP-1~5 优先级序列；
> - **§5.3 示例 2（DownloadAll 禁止触发 Expansion）已作废**：该示例用"即使获取证据也只能判定 STOP"预判结论，而结论恰恰依赖缺失的 Ownership Context，构成循环论证。Patch 后的 C09 重放（Decision Replay 文件 Post-Patch 节）正确路径为：触发 Expansion → Iteration 1 → STOP-1 五元组命中 → STOP。本文件保留旧示例仅作 Pre-Patch 历史记录。
>
> §1/§2/§4（五种 Context Type、Context Unit/Dependency 概念结构）与 §7 核心纪律继续有效，不受 Patch 影响。

---

## 1. 核心定义

### 1.1 什么是 Context？

**Context = 解决当前 Finding 所必需的最小跨类证据集合。**

- **不是**：全仓扫描结果
- **不是**：所有可能的调用关系
- **而是**：直接影响当前 Finding 的 GO/STOP/NEED 判定的关键证据

### 1.2 为什么需要 Context？

v4/v5 整条协议是**单类视野**，对以下问题结构性无法回答：

- **跨类 ownership**：A 调 B，B 返回 IDisposable，谁释放？
- **跨类 DI 生命周期**：Singleton 注 Scoped？
- **跨类数据量传播**：B 返回全量，A 有无截断？
- **跨层边界**：Service → Controller → 前端，事务边界在哪？

这些问题**只看一个类的代码无法回答**，必须知道类之间的关系。

### 1.3 Context 与 v4 的关系

- **继承**：v4 的 P0 取证 / 16 维 Finding / GO/STOP/NEED / Semantic Budget / Convergence 全部继承
- **扩展**：新增 Context Expansion 机制，允许获取跨类证据
- **纪律**：Context Expansion 不替代 P0，不弱化 GO/STOP/NEED 三门判定

---

## 2. Context Type（五种上下文类型）

### 2.1 Call Context（调用上下文）

**定义**：当前类的方法调用了哪些其他类的方法？被哪些其他类调用？

**典型场景**：
- A 调 B，B 返回 IDisposable，谁释放？
- A 调 B，B 内部有事务，A 是否参与？

**证据字段**：
```json
{
  "context_type": "Call",
  "caller_method": "FileService.DownloadAll",
  "callee_methods": ["IFileManager.DownloadFileByType", "IFileManager.CopyFile"],
  "call_chain": ["FileService.DownloadAll → IFileManager.DownloadFileByType → FileStreamResult"],
  "evidence_source": "Level 0/1/2"
}
```

### 2.2 DI Context（依赖注入上下文）

**定义**：当前类注入了哪些接口/服务？它们的生命周期是什么？

**典型场景**：
- Singleton 注 Scoped？
- Scoped 注 Singleton？

**证据字段**：
```json
{
  "context_type": "DI",
  "current_class": "OrderService",
  "current_lifetime": "Scoped",
  "injected_services": [
    {"interface": "IFileManager", "lifetime": "Scoped"},
    {"interface": "IUserManager", "lifetime": "Scoped"}
  ],
  "evidence_source": "Level 0/1"
}
```

### 2.3 Ownership / Resource Context（资源所有权上下文）

**定义**：资源（Stream/IDisposable/byte[]）在类之间如何传递？谁负责释放？

**典型场景**：
- B 返回 FileStream，A 是否 using？
- A 把 Stream 传给 C，ownership 是否明确交接？

**证据字段**：
```json
{
  "context_type": "Ownership",
  "resource_type": "FileStream",
  "producer": "IFileManager.DownloadFileByType",
  "consumer": "FileService.FileDown",
  "ownership_chain": ["IFileManager.DownloadFileByType → FileStreamResult → FileService.FileDown → using var fs"],
  "disposal_responsibility": "调用方（FileService）",
  "evidence_source": "Level 0/1"
}
```

### 2.4 Data-flow / Data-volume Context（数据流/数据量上下文）

**定义**：数据在类之间如何传播？是否有数量限制？

**典型场景**：
- B 返回全量数据，A 有无截断？
- A 调 B 100 次，每次返回 1 条，是否 N+1？

**证据字段**：
```json
{
  "context_type": "DataFlow",
  "producer_method": "ScheduleRepository.GetScheduleUsers",
  "return_type": "List<ScheduleUser>",
  "data_volume": "未知（需运行时证据）",
  "consumer_method": "ScheduleService.Delete",
  "loop_count": "foreach（未知次数）",
  "evidence_source": "Level 0/1/2"
}
```

### 2.5 Cross-layer Context（跨层上下文）

**定义**：当前类处于哪一层？调用链跨越了哪些层？

**典型场景**：
- Service → Controller → 前端，ownership 在哪层结束？
- Service → Repository，事务边界在哪层？

**证据字段**：
```json
{
  "context_type": "CrossLayer",
  "current_layer": "Service",
  "call_chain": ["Controller → Service → Repository"],
  "layer_boundaries": ["Controller（HTTP）→ Service（业务）→ Repository（数据）"],
  "transaction_boundary": "Service（[UnitOfWork]）",
  "evidence_source": "Level 0/1"
}
```

---

## 3. Context Unit（最小必要上下文）

### 3.1 定义

**Context Unit = 解决当前 Finding 所必需的最小证据单元。**

- **不是**：所有调用关系
- **而是**：直接影响当前 Finding 判定的关键证据

### 3.2 判断标准

**一个 Context Unit 是否"必要"，取决于以下三个问题：**

1. **该证据是否直接影响当前 Finding 的 GO/STOP/NEED 判定？**
   - YES → 必要
   - NO → 非必要

2. **如果不获取该证据，能否安全地标记为 NEED EVIDENCE 并停止？**
   - YES → 非必要（可以冻结）
   - NO → 必要

3. **获取该证据的成本是否合理？**
   - YES → 必要
   - NO → 非必要（成本 > 收益）

### 3.3 示例

**示例 1：FileService.DownloadAll 临时目录未清理（R-03）**

- **Finding**：临时目录未清理
- **当前类证据**：创建目录 + CopyFile，无 finally 清理
- **缺失证据**：谁消费这个临时目录？何时结束？
- **Context Unit**：
  ```json
  {
    "context_type": "Ownership",
    "question": "谁消费临时目录？何时结束？",
    "evidence": "人工描述 → 临时目录由前端下载后清理",
    "impact": "ownership 跨层，不能在 FileService 内部修复",
    "decision": "STOP"
  }
  ```
- **判定**：该 Context Unit 是**必要**的（直接影响 STOP 判定）

**示例 2：OrderService.Save 无事务（M-04）**

- **Finding**：Save 方法无事务（多步 DB 操作）
- **当前类证据**：多步 Queryable/Insertable
- **缺失证据**：OrderService 的 DI 生命周期？[UnitOfWork] 是否可用？
- **Context Unit**：
  ```json
  {
    "context_type": "DI",
    "question": "OrderService 的 DI 生命周期？[UnitOfWork] 是否可用？",
    "evidence": "Level 1 → OrderService 是 Scoped，[UnitOfWork] 可用",
    "impact": "可以在 OrderService 内部加 [UnitOfWork]",
    "decision": "GO"
  }
  ```
- **判定**：该 Context Unit 是**必要**的（直接影响 GO 判定）

---

## 4. Context Dependency（上下文依赖）

### 4.1 定义

**Context Dependency = 当前 Finding 缺少什么证据？为什么该证据会影响当前 Finding 的 GO/STOP/NEED 判定？**

### 4.2 依赖关系图

```
当前 Finding
    ↓
当前类证据是否充分？
    ├─ YES → 直接 GO/STOP/NEED（v4 协议）
    └─ NO  → 判断缺失哪一种上下文
                ↓
            缺失的上下文类型：
                ├─ Call Context
                ├─ DI Context
                ├─ Ownership Context
                ├─ Data-flow Context
                └─ Cross-layer Context
                ↓
            该上下文是否可通过 Level 0/1 获取？
                ├─ YES → 获取 Level 0/1 上下文 → 重新判断
                └─ NO  → 该上下文是否必须依赖 Level 2？
                            ├─ YES → 标记 NEED EVIDENCE（Level 2 未实现）
                            └─ NO  → 标记 STOP（无法获取必要证据）
```

### 4.3 依赖示例

**示例：ScheduleService.Delete N+1（R-08）**

- **Finding**：foreach 内 ToListAsync（N+1 形态）
- **当前类证据**：foreach 内查询 ScheduleUser
- **缺失证据**：实际调用次数？数据规模？
- **Context Dependency**：
  ```json
  {
    "finding": "ScheduleService.Delete N+1",
    "missing_evidence": "实际调用次数 + 数据规模",
    "context_type": "DataFlow",
    "impact": "无法判定 N+1 是否真实存在（可能只有 1 次循环）",
    "level_required": "Level 2（Roslyn call-graph + runtime profiling）",
    "decision": "NEED EVIDENCE（Level 2 未实现）"
  }
  ```

---

## 5. Context Expansion Trigger（上下文扩展触发条件）

### 5.1 触发条件

**只有当以下三个条件同时满足时，才允许 Context Expansion：**

1. **Finding 真实存在**（不是 False Positive）
2. **当前类证据不足**（无法安全判定 GO/STOP/NEED）
3. **缺失的上下文直接影响判定**（不是"可能有用"）

### 5.2 禁止触发的情况

- Finding 本身是 False Positive
- 当前类证据已足够判定 STOP
- 缺失的上下文不影响核心判定
- 仅为"completeness"而扩展（过度工程）

### 5.3 触发示例

**示例 1：允许触发**

- **Finding**：OrderService.Save 无事务
- **当前类证据**：多步 DB 操作
- **缺失证据**：DI 生命周期 + [UnitOfWork] 可用性
- **影响**：直接影响 GO/STOP 判定
- **判定**：允许触发 Context Expansion

**示例 2：禁止触发**

- **Finding**：FileService.DownloadAll 临时目录未清理
- **当前类证据**：创建目录 + CopyFile，无 finally 清理
- **缺失证据**：谁消费临时目录？
- **影响**：即使获取证据，也只能判定 STOP（跨层 ownership）
- **判定**：禁止触发 Context Expansion（当前类证据已足够判定 STOP）

---

## 6. Context Expansion Stop Conditions（上下文扩展终止条件）

### 6.1 终止条件

**以下任一条件满足时，必须停止 Context Expansion：**

1. **已获得足够证据** → 重新进入 GO/STOP/NEED 判定
2. **达到 Level 上限** → 若 Level 0/1 无法满足，标记 NEED EVIDENCE（Level 2 未实现）
3. **成本超过收益** → 上下文获取成本 > Finding 修复收益 → STOP
4. **跨模块边界** → 涉及跨模块依赖 → STOP（避免跨模块传染）
5. **无法获取** → 上下文不可获取（如运行时行为） → NEED EVIDENCE

### 6.2 终止示例

**示例 1：已获得足够证据**

- **Finding**：OrderService.Save 无事务
- **Context Expansion**：Level 1 → OrderService 是 Scoped，[UnitOfWork] 可用
- **判定**：已获得足够证据 → 重新判定 GO

**示例 2：达到 Level 上限**

- **Finding**：ScheduleService.Delete N+1
- **Context Expansion**：Level 1 → 无法确定数据规模
- **判定**：需要 Level 2（Roslyn call-graph + runtime profiling）→ 标记 NEED EVIDENCE

**示例 3：成本超过收益**

- **Finding**：FileService.DownloadAll 临时目录未清理
- **Context Expansion**：Level 0 → 人工描述"临时目录由前端下载后清理"
- **判定**：即使获取证据，也只能判定 STOP（跨层 ownership）→ 成本 > 收益 → STOP

---

## 7. Evidence Expansion ≠ Scope Expansion（核心纪律）

### 7.1 定义

**Evidence Expansion = 获取解决当前 Finding 所必需的最小跨类证据。**

**Scope Expansion = 无限制扫描整个解决方案。**

### 7.2 纪律

- ❌ **错误**：发现一个潜在问题 → 扫描整个解决方案
- ✅ **正确**：发现一个潜在问题 → 判断缺失哪一种上下文 → 只扩展必要上下文 → 重新判断

### 7.3 防止过度扩张

**Context Expansion 是手段，不是目的。**

- **目标**：GO/STOP/NEED 判定
- **不是**："获取所有上下文"

**如果 Context Expansion 成本过高，应直接 STOP 或 NEED EVIDENCE。**

---

## 8. 总结

V6 Context Model = **五种上下文类型 + 最小必要上下文原则 + 触发/终止条件 + 核心纪律**

- **Context Type**：Call / DI / Ownership / Data-flow / Cross-layer
- **Context Unit**：解决当前 Finding 所必需的最小证据单元
- **Context Dependency**：当前 Finding 缺少什么证据？为什么该证据会影响判定？
- **Context Expansion Trigger**：Finding 真实 + 证据不足 + 上下文直接影响判定
- **Context Expansion Stop Conditions**：已获得足够证据 / 达到 Level 上限 / 成本 > 收益 / 跨模块边界 / 无法获取
- **核心纪律**：Evidence Expansion ≠ Scope Expansion

---

## 9. 回答核心问题（Q1-Q5）

### Q1：什么时候一个 Finding 有资格请求跨类 Context？

**当且仅当以下三个条件同时满足时：**

1. Finding 真实存在（不是 False Positive）
2. 当前类证据不足（无法安全判定 GO/STOP/NEED）
3. 缺失的上下文直接影响判定（不是"可能有用"）

### Q2：请求哪一种 Context？

**根据 Finding 的技术性质选择：**

- **跨类 ownership** → Ownership Context
- **跨类 DI 生命周期** → DI Context
- **跨类调用链** → Call Context
- **跨类数据量传播** → Data-flow Context
- **跨层边界** → Cross-layer Context

### Q3：为什么这个 Context 是"必要"的，而不是"有用但非必要"？

**判断标准：**

1. 该证据是否直接影响当前 Finding 的 GO/STOP/NEED 判定？
2. 如果不获取该证据，能否安全地标记为 NEED EVIDENCE 并停止？
3. 获取该证据的成本是否合理？

**三个问题都 YES → 必要；任一 NO → 非必要。**

### Q4：最多扩展到哪里？

**最多扩展到：**

- **直接调用者/被调用者**（1 层）
- **直接注入的依赖**（1 层）
- **直接的资源生产者/消费者**（1 层）

**禁止扩展到：**

- 跨模块边界
- 间接调用链（除非直接影响判定）
- 全仓扫描

### Q5：什么时候必须停止 Expansion？

**以下任一条件满足时：**

1. 已获得足够证据
2. 达到 Level 上限（Level 0/1 无法满足）
3. 成本超过收益
4. 跨模块边界
5. 无法获取可靠证据

---

**本规格待人工验收。验收通过后，才能进入 R2 Context Acquisition 设计阶段。**
