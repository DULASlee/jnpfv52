# Context Expansion Rules — 上下文扩展规则

> **版本**：v6.0-R1-draft | **日期**：2026-08-28 | **状态**：R1 设计规格（待人工验收）  
> **基于**：V6-Context-Model.md  
> **纪律**：不修改 v4 已冻结协议；不实现 Level 2 工具；不审计 JNPF 新类

> ⚠️ **操作性规则已更新（2026-08-28 Patch v2 横幅）**：**唯一操作源 = `R1-Operationalization-Patch.md` v2**。本文件中旧判据按下表映射，冲突处一律以 Patch 为准：
>
> | 本文件旧条款 | Patch v2 取代条款 |
> |--------------|-------------------|
> | §1.2 判断标准第 3 问"成本是否合理" | §1.2 Risk×Nature 分档 Budget（可数上限），不再问"合理" |
> | §3.1/§3.3 触发条件第 3 条"直接影响判定" | §2.3 五元组判据 4"Decision 唯一"（给定证据可推出唯一 Decision 才算"直接影响"） |
> | §4.1 终止条件 ①"已获得足够证据" | STOP-1（§2.3 五判据 + §2.5 可证伪检查） |
> | §4.1 终止条件 ②"达到 Level 上限" | STOP-5 / E3（§4.1，决策态冻结 NEED EVIDENCE） |
> | §4.1 终止条件 ③"成本超过收益" | **已删除**——被 STOP-2 穷举算法（§3.2/§3.4）+ STOP-3 分档上限取代，主观成本评估不再作为停止依据 |
> | §4.1 终止条件 ④"跨模块边界" | STOP-4（§3.1，含同时命中记录规则） |
> | §4.1 终止条件 ⑤"无法获取" | STOP-5 / E1-E3 |
> | §5.1 输出字段 `ContextExpansionCost` | §6.2 Result 契约 `budget_consumed` 四维计数 |
> | §7 "成本失控→成本>收益则STOP" | Budget 计数封顶 + Escalation（成本失控由可数维度防堵） |
> | §8 Q6-Q10 中涉及"成本合理"的表述 | Patch §1.2/§2.3/§4.0 |

---

## 1. 核心原则

### 1.1 Evidence Expansion ≠ Scope Expansion

**Context Expansion 的唯一目的是获取解决当前 Finding 所必需的最小跨类证据，而不是无限制扫描整个解决方案。**

### 1.2 最小必要上下文原则

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

---

## 3. Context Expansion 触发条件

### 3.1 必须同时满足以下三个条件

1. **Finding 真实存在**（不是 False Positive）
2. **当前类证据不足**（无法安全判定 GO/STOP/NEED）
3. **缺失的上下文直接影响判定**（不是"可能有用"）

### 3.2 禁止触发的情况

- Finding 本身是 False Positive
- 当前类证据已足够判定 STOP
- 缺失的上下文不影响核心判定
- 仅为"completeness"而扩展（过度工程）

### 3.3 触发判定算法

```python
def should_trigger_expansion(finding, current_class_evidence, missing_context):
    # 条件 1：Finding 真实存在
    if finding.is_false_positive:
        return False
    
    # 条件 2：当前类证据不足
    if current_class_evidence.is_sufficient():
        return False
    
    # 条件 3：缺失的上下文直接影响判定
    if not missing_context.directly_impacts_decision():
        return False
    
    return True
```

---

## 4. Context Expansion 终止条件

### 4.1 必须停止的情况

**以下任一条件满足时，必须停止 Context Expansion：**

1. **已获得足够证据** → 重新进入 GO/STOP/NEED 判定
2. **达到 Level 上限** → 若 Level 0/1 无法满足，标记 NEED EVIDENCE（Level 2 未实现）
3. **成本超过收益** → 上下文获取成本 > Finding 修复收益 → STOP
4. **跨模块边界** → 涉及跨模块依赖 → STOP（避免跨模块传染）
5. **无法获取** → 上下文不可获取（如运行时行为） → NEED EVIDENCE

### 4.2 终止判定算法

```python
def should_stop_expansion(context_obtained, level_reached, cost, benefit, cross_module, evidence_available):
    # 条件 1：已获得足够证据
    if context_obtained and context_obtained.is_sufficient():
        return True, "已获得足够证据"
    
    # 条件 2：达到 Level 上限
    if level_reached >= MAX_LEVEL and not context_obtained:
        return True, "达到 Level 上限"
    
    # 条件 3：成本超过收益
    if cost > benefit:
        return True, "成本超过收益"
    
    # 条件 4：跨模块边界
    if cross_module:
        return True, "跨模块边界"
    
    # 条件 5：无法获取
    if not evidence_available:
        return True, "无法获取可靠证据"
    
    return False, ""
```

---

## 5. Context Expansion 输出字段

### 5.1 必须附加的字段

v6 Finding 必须附加以下字段（如果触发 Context Expansion）：

| 字段 | 类型 | 说明 |
|------|------|------|
| `ContextExpansionTriggered` | bool | 是否触发上下文扩展 |
| `MissingContextType` | enum | 缺失的上下文类型（Call/DI/Ownership/DataFlow/CrossLayer） |
| `ContextLevel` | enum | 使用的上下文级别（Level0/Level1/Level2） |
| `ContextObtained` | bool | 是否成功获取上下文 |
| `ContextSource` | string | 上下文来源（人工描述/静态分析/工具） |
| `ContextExpansionCost` | enum | 上下文获取成本（Low/Medium/High） |
| `DecisionAfterExpansion` | enum | 上下文扩展后的判定（GO/STOP/NEED） |
| `DecisionRationale` | string | 判定理由（为什么扩展后仍 STOP 或 GO） |

### 5.2 示例

```json
{
  "finding": "OrderService.Save 无事务",
  "initial_decision": "STOP",
  "context_expansion_triggered": true,
  "missing_context_type": "DI",
  "context_level": "Level1",
  "context_obtained": true,
  "context_source": "DI 注册代码推断",
  "context_expansion_cost": "Low",
  "decision_after_expansion": "GO",
  "decision_rationale": "跨类证据支持在当前类修复，Semantic Budget 内（+1 using +2 [UnitOfWork]）"
}
```

---

## 6. Context Expansion 与 v4 纪律的兼容性

### 6.1 继承 v4 核心纪律

| v4 纪律 | v6 Context Expansion 如何继承 |
|---------|------------------------------|
| **P0 先行** | Context Expansion 不替代 P0，而是在 P0 之后补充跨类证据 |
| **Finding ≠ Fix** | Context Expansion 是为了更好判定，不是为了自动修复 |
| **GO/STOP/NEED 三门** | Context Expansion 后仍必须进入三门判定，不能跳过 |
| **Semantic Budget** | Context Expansion 获取的证据必须纳入 Semantic Budget 评估 |
| **Single commit** | Context Expansion 不引入额外提交，仍遵守单提交原则 |
| **Convergence** | Context Expansion 有终止条件，不能无限扩展 |

### 6.2 防止 v6 过度扩张

**v6 不能因为拥有更强的取证能力，就变成"什么都要查、什么都要改"。**

必须遵守：
- **Context Expansion 是手段，不是目的**
- **目标仍是 GO/STOP/NEED 判定，不是"获取所有上下文"**
- **如果 Context Expansion 成本过高，应直接 STOP 或 NEED EVIDENCE**

---

## 7. Context Expansion 风险与缓解

| 风险 | 缓解措施 |
|------|----------|
| **过度扩展** | 明确触发条件 + 终止条件 |
| **成本失控** | ContextExpansionCost 字段 + 成本 > 收益则 STOP |
| **跨模块传染** | 跨模块边界 → STOP |
| **弱化 v4 纪律** | 明确继承 v4 核心纪律，Context Expansion 不替代 P0 |
| **误判 GO** | Context Expansion 后仍必须进入三门判定，不能跳过 |

---

## 8. 回答核心问题（Q6-Q10）

### Q6：Level 0 → Level 1 → Level 2 的升级依据是什么？

**升级依据：**

1. **Level 0 → Level 1**：当人工描述成本过高或不可靠时，尝试从静态信息推断
2. **Level 1 → Level 2**：当静态信息无法提供足够证据时，需要工具辅助

**升级条件：**

- Level N 无法提供足够证据
- Level N+1 的成本合理
- Level N+1 的证据可信度更高

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

### Q8：如何防止 Context Expansion 演变成"全仓扫描"？

**防止措施：**

1. **明确触发条件**：只有三个条件同时满足才允许触发
2. **明确终止条件**：五个条件任一满足必须停止
3. **限制扩展范围**：最多扩展到直接调用者/被调用者（1 层）
4. **成本评估**：ContextExpansionCost 字段 + 成本 > 收益则 STOP
5. **跨模块边界**：涉及跨模块依赖 → STOP

### Q9：如何处理跨层 ownership 问题？

**处理原则：**

- **跨层 ownership = STOP**（不能局部修复）
- **不能因为 v6 有更强的取证能力，就试图在单层修复跨层问题**

**示例：**

- **Finding**：FileService.DownloadAll 临时目录未清理
- **Context Expansion**：Level 0 → 人工描述"临时目录由前端下载后清理"
- **判定**：STOP（跨层 ownership，不能局部修复）

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

---

## 9. 总结

Context Expansion Rules = **触发条件 + 终止条件 + 输出字段 + 与 v4 兼容性 + 风险缓解**

- **触发条件**：Finding 真实 + 证据不足 + 上下文直接影响判定
- **终止条件**：已获得足够证据 / 达到 Level 上限 / 成本 > 收益 / 跨模块边界 / 无法获取
- **输出字段**：8 个必须附加字段
- **与 v4 兼容性**：继承 v4 核心纪律，不弱化安全边界
- **风险缓解**：明确触发/终止条件 + 成本评估 + 跨模块边界

---

**本规格待人工验收。验收通过后，才能进入 R2 Context Acquisition 设计阶段。**
