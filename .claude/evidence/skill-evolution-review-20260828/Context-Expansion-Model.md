# Context Expansion Model — v6 核心设计

> ⚠️ **已被取代（2026-08-28 Patch v2 横幅）**：本文件为 18:20 早期草稿，规则正文已由 `Context-Expansion-Rules.md` 承接，操作性判据最终由 `R1-Operationalization-Patch.md` v2 唯一化（含"成本>收益"删除，见该文件横幅映射表）。本文件仅作设计演进历史保留，**不得作为判据引用源**。

> v6 的核心挑战：当一个 Finding 无法仅凭当前类安全判断时，如何**有纪律地**获取最小必要上下文，而不是无限制扫描整个解决方案。

## 1. 核心原则

### Evidence Expansion ≠ Scope Expansion

```
❌ 错误：发现一个潜在问题 → 扫描整个解决方案
✅ 正确：发现一个潜在问题 → 判断缺失哪一种上下文 → 只扩展必要上下文 → 重新判断
```

### 最小必要上下文原则（Minimal Necessary Context Principle）

**只获取解决当前 Finding 所必需的最小跨类上下文。**

判断标准：
- 该上下文是否直接影响当前 Finding 的 GO/STOP/NEED 判定？
- 如果不获取，能否安全地标记为 NEED EVIDENCE 并停止？
- 获取该上下文的成本（时间/复杂度/准确性）是否合理？

---

## 2. Context Expansion 决策流程

```
当前类 Finding
    ↓
当前类证据是否充分？
    ├─ YES → 直接 GO/STOP/NEED（v4 协议）
    └─ NO  → 判断缺失哪一种上下文
                ↓
            缺失的上下文类型：
                ├─ 调用关系（caller/callee）
                ├─ DI 关系（injection/lifetime）
                ├─ Ownership 关系（resource flow）
                ├─ 数据流关系（data propagation）
                └─ 跨层关系（layer boundary）
                ↓
            该上下文是否可通过 Level 0/1 获取？
                ├─ YES → 获取 Level 0/1 上下文 → 重新判断
                └─ NO  → 该上下文是否必须依赖 Level 2？
                            ├─ YES → 标记 NEED EVIDENCE（Level 2 未实现）
                            └─ NO  → 标记 STOP（无法获取必要证据）
```

---

## 3. 五种上下文类型与获取方式

| 上下文类型 | 典型场景 | Level 0（人工） | Level 1（静态信息） | Level 2（工具） |
|------------|----------|-----------------|---------------------|-----------------|
| **调用关系** | A 调 B，B 返回 IDisposable，谁释放？ | 人工描述调用链 | 从接口签名推断 | Roslyn call-graph |
| **DI 关系** | Singleton 注 Scoped？ | 人工描述注入链 | 从 DI 注册代码推断 | Roslyn DI-registration |
| **Ownership 关系** | 资源跨类传递，谁负责释放？ | 人工描述 ownership 链 | 从返回类型/参数推断 | Roslyn data-flow |
| **数据流关系** | B 返回全量，A 有无截断？ | 人工描述数据规模 | 从查询条件推断 | Roslyn query-analysis |
| **跨层关系** | Service 调 Repository，事务边界？ | 人工描述层边界 | 从项目结构推断 | Roslyn project-dependency |

---

## 4. Context Expansion 触发条件

**只有当以下三个条件同时满足时，才允许 Context Expansion：**

1. **Finding 真实存在**（不是 False Positive）
2. **当前类证据不足**（无法安全判定 GO/STOP/NEED）
3. **缺失的上下文直接影响判定**（不是"可能有用"）

**禁止以下情况触发 Context Expansion：**
- Finding 本身是 False Positive
- 当前类证据已足够判定 STOP
- 缺失的上下文不影响核心判定
- 仅为" completeness"而扩展（过度工程）

---

## 5. Context Expansion 终止条件

**以下任一条件满足时，必须停止 Context Expansion：**

1. **已获得足够证据** → 重新进入 GO/STOP/NEED 判定
2. **达到 Level 上限** → 若 Level 0/1 无法满足，标记 NEED EVIDENCE（Level 2 未实现）
3. **成本超过收益** → 上下文获取成本 > Finding 修复收益 → STOP
4. **跨模块边界** → 涉及跨模块依赖 → STOP（避免跨模块传染）
5. **无法获取** → 上下文不可获取（如运行时行为） → NEED EVIDENCE

---

## 6. Context Expansion 与 v4 纪律的兼容性

### 继承 v4 核心纪律

| v4 纪律 | v6 Context Expansion 如何继承 |
|---------|------------------------------|
| **P0 先行** | Context Expansion 不替代 P0，而是在 P0 之后补充跨类证据 |
| **Finding ≠ Fix** | Context Expansion 是为了更好判定，不是为了自动修复 |
| **GO/STOP/NEED 三门** | Context Expansion 后仍必须进入三门判定，不能跳过 |
| **Semantic Budget** | Context Expansion 获取的证据必须纳入 Semantic Budget 评估 |
| **Single commit** | Context Expansion 不引入额外提交，仍遵守单提交原则 |
| **Convergence** | Context Expansion 有终止条件，不能无限扩展 |

### 防止 v6 过度扩张

**v6 不能因为拥有更强的取证能力，就变成"什么都要查、什么都要改"。**

必须遵守：
- **Context Expansion 是手段，不是目的**
- **目标仍是 GO/STOP/NEED 判定，不是"获取所有上下文"**
- **如果 Context Expansion 成本过高，应直接 STOP 或 NEED EVIDENCE**

---

## 7. Context Expansion 输出字段

v6 Finding 必须附加以下字段（如果触发 Context Expansion）：

| 字段 | 类型 | 说明 |
|------|------|------|
| `ContextExpansionTriggered` | bool | 是否触发上下文扩展 |
| `MissingContextType` | enum | 缺失的上下文类型（CallGraph/DI/Ownership/DataFlow/CrossLayer） |
| `ContextLevel` | enum | 使用的上下文级别（Level0/Level1/Level2） |
| `ContextObtained` | bool | 是否成功获取上下文 |
| `ContextSource` | string | 上下文来源（人工描述/静态分析/工具） |
| `ContextExpansionCost` | enum | 上下文获取成本（Low/Medium/High） |
| `DecisionAfterExpansion` | enum | 上下文扩展后的判定（GO/STOP/NEED） |
| `DecisionRationale` | string | 判定理由（为什么扩展后仍 STOP 或 GO） |

---

## 8. Context Expansion 示例

### 示例 1：跨类 Ownership（Level 0）

```
当前类：FileService.DownloadAll
Finding：临时目录未清理（R-03）
当前类证据：创建目录 + CopyFile，无 finally 清理
缺失上下文：谁消费这个临时目录？何时结束？
Level 0 上下文：人工描述 → "临时目录由前端下载后清理"
Context Expansion 后判定：STOP（跨层 ownership，不能局部修复）
```

### 示例 2：DI 生命周期（Level 1）

```
当前类：OrderService
Finding：注入 IFileManager（Scoped），但 OrderService 是 Singleton？
当前类证据：OrderService 注入 IFileManager
缺失上下文：IFileManager 实际注册生命周期？
Level 1 上下文：从 DI 注册代码推断 → IFileManager 是 Scoped
Context Expansion 后判定：NEED EVIDENCE（需确认 OrderService 实际生命周期）
```

### 示例 3：Call Graph（Level 2 未实现）

```
当前类：ScheduleService.Delete
Finding：N+1 循环查询（R-08）
当前类证据：foreach 内 ToListAsync
缺失上下文：实际调用次数？数据规模？
Level 2 上下文：需要 Roslyn call-graph + runtime profiling
Context Expansion 后判定：NEED EVIDENCE（Level 2 未实现，无法获取必要证据）
```

---

## 9. Context Expansion 风险与缓解

| 风险 | 缓解措施 |
|------|----------|
| **过度扩展** | 明确触发条件 + 终止条件 |
| **成本失控** | ContextExpansionCost 字段 + 成本 > 收益则 STOP |
| **跨模块传染** | 跨模块边界 → STOP |
| **弱化 v4 纪律** | 明确继承 v4 核心纪律，Context Expansion 不替代 P0 |
| **误判 GO** | Context Expansion 后仍必须进入三门判定，不能跳过 |

---

## 10. Context Expansion 与 Level 0/1/2 的关系

- **Level 0（人工）**：验证 v6 决策模型本身，成本最高但最灵活
- **Level 1（静态信息）**：利用已有静态信息（接口签名/DI 注册/项目结构），成本中等
- **Level 2（工具）**：自动化取证（Roslyn call-graph/DI-graph），成本最低但需开发

**优先级：Level 0 → Level 1 → Level 2**

**必须先证明 Level 0/1 无法满足，才能主张 Level 2。**
