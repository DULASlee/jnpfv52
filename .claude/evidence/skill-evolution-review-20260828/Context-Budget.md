# Context Budget — 上下文获取预算

> **版本**：v6.0-R1-draft | **日期**：2026-08-28 | **状态**：R1 设计规格（待人工验收）  
> **基于**：V6-Context-Model.md + Context-Expansion-Rules.md + Context-Level-Model.md  
> **纪律**：不修改 v4 已冻结协议；不实现 Level 2 工具；不审计 JNPF 新类

> ⚠️ **操作性规则已废止（2026-08-28 Patch v2 横幅）**：本文件 **§2 三维度（Time/Complexity/Accuracy 枚举）、§3 成本>收益判定、§4.2 判定流程、§5 输出字段、§7.3 示例** 中的操作性规则**已被 `R1-Operationalization-Patch.md` v2 §1（五维可数 Budget + Risk×Nature 分档）取代，不再作为判据使用**。
> 保留价值仅限：§1 概念定义（Context Budget ≠ Semantic Budget）与"防止全仓扫描"的设计动机。
> **唯一操作源 = R1-Operationalization-Patch.md v2。** 旧"30 分钟/可信度百分比"式度量被架构师裁定为伪精确，已删除效力。旧成本>收益停止条件的替代判据映射见 `Context-Expansion-Rules.md` 横幅。

---

## 1. 核心定义

### 1.1 什么是 Context Budget？

**Context Budget = 获取额外证据的预算。**

- **不是**：修改代码的预算（那是 Semantic Budget）
- **而是**：获取跨类上下文的预算（时间/复杂度/准确性）

### 1.2 Context Budget vs Semantic Budget

| 维度 | Context Budget | Semantic Budget |
|------|----------------|-----------------|
| **定义** | 获取额外证据的预算 | 修改代码的预算 |
| **目的** | 获取解决 Finding 所必需的跨类证据 | 限制代码修改的范围 |
| **度量** | 时间/复杂度/准确性 | 语义范围/物理 diff/依赖扩展 |
| **阶段** | Context Expansion 阶段 | Fix 阶段 |
| **v4 对应** | 无（v6 新增） | Semantic Budget（v4 M2 校准） |

### 1.3 为什么需要 Context Budget？

**防止 Context Expansion 演变成"全仓扫描"。**

- **没有 Context Budget**：可能无限制获取上下文，成本失控
- **有 Context Budget**：明确获取上下文的边界，成本 > 收益则 STOP

---

## 2. Context Budget 三维度

### 2.1 时间预算（Time Budget）

**定义**：获取上下文所花费的时间。

**度量**：
- **Low**：< 5 分钟
- **Medium**：5-30 分钟
- **High**：> 30 分钟

**限制**：
- **Level 0**：High（人工描述成本高）
- **Level 1**：Medium（静态信息分析成本中等）
- **Level 2**：Low（工具自动化，一旦实现）

### 2.2 复杂度预算（Complexity Budget）

**定义**：获取上下文的复杂度。

**度量**：
- **Low**：直接调用者/被调用者（1 层）
- **Medium**：间接调用链（2 层）
- **High**：全仓扫描（> 2 层）

**限制**：
- **最多扩展到**：直接调用者/被调用者（1 层）
- **禁止扩展到**：跨模块边界 / 间接调用链（除非直接影响判定）

### 2.3 准确性预算（Accuracy Budget）

**定义**：获取上下文的准确性。

**度量**：
- **High**：工具分析（Level 2）
- **Medium**：静态信息推断（Level 1）
- **Low**：人工描述（Level 0）

**限制**：
- **Level 0**：可信度 Medium，需验证
- **Level 1**：可信度 High，可验证
- **Level 2**：可信度 Very High，可复现

---

## 3. Context Budget 判定规则

### 3.1 成本 > 收益 → STOP

**如果 Context Expansion 成本 > Finding 修复收益，必须 STOP。**

**判定算法：**

```python
def should_stop_by_budget(context_cost, finding_benefit):
    # 成本 > 收益
    if context_cost > finding_benefit:
        return True, "成本超过收益"
    
    return False, ""
```

### 3.2 成本评估

**Context Cost = Time Budget + Complexity Budget + Accuracy Budget**

**示例：**

- **Level 0**：Time=High + Complexity=Low + Accuracy=Medium → Cost=Medium
- **Level 1**：Time=Medium + Complexity=Low + Accuracy=High → Cost=Low
- **Level 2**：Time=Low + Complexity=Low + Accuracy=Very High → Cost=Very Low

### 3.3 收益评估

**Finding Benefit = 修复收益（避免的风险/提升的质量）**

**示例：**

- **Critical Finding**：Benefit=High（避免严重风险）
- **High Finding**：Benefit=Medium（提升质量）
- **Medium/Low Finding**：Benefit=Low（边际收益）

### 3.4 成本 > 收益示例

**示例 1：成本 < 收益 → 允许 Expansion**

- **Finding**：OrderService.Save 无事务（Critical）
- **Context Expansion**：Level 1 → DI 生命周期 + [UnitOfWork] 可用性
- **成本**：Low（Level 1，静态信息推断）
- **收益**：High（避免事务风险）
- **判定**：成本 < 收益 → 允许 Expansion → GO

**示例 2：成本 > 收益 → STOP**

- **Finding**：FileService.DownloadAll 临时目录未清理（Medium）
- **Context Expansion**：Level 0 → 人工描述 ownership 链
- **成本**：Medium（Level 0，人工描述）
- **收益**：Low（Medium Finding，边际收益）
- **判定**：成本 > 收益 → STOP（即使获取证据，也只能判定 STOP）

---

## 4. Context Budget 与 Context Expansion 的关系

### 4.1 Context Budget 是 Context Expansion 的约束

**Context Expansion 必须在 Context Budget 内进行。**

- **Context Expansion**：获取跨类上下文的流程
- **Context Budget**：限制 Context Expansion 的边界

### 4.2 Context Budget 判定流程

```
Context Expansion 触发
    ↓
评估 Context Cost（Time + Complexity + Accuracy）
    ↓
评估 Finding Benefit
    ↓
成本 > 收益？
    ├─ YES → STOP（成本超过收益）
    └─ NO  → 继续 Context Expansion
                ↓
            获取上下文（Level 0/1/2）
                ↓
            重新判定 GO/STOP/NEED
```

---

## 5. Context Budget 输出字段

### 5.1 必须附加的字段

v6 Finding 必须附加以下字段（如果触发 Context Expansion）：

| 字段 | 类型 | 说明 |
|------|------|------|
| `ContextBudgetTime` | enum | 时间预算（Low/Medium/High） |
| `ContextBudgetComplexity` | enum | 复杂度预算（Low/Medium/High） |
| `ContextBudgetAccuracy` | enum | 准确性预算（Low/Medium/High） |
| `ContextCost` | enum | 总成本（Very Low/Low/Medium/High/Very High） |
| `FindingBenefit` | enum | 修复收益（Low/Medium/High） |
| `BudgetExceeded` | bool | 是否超出预算 |
| `BudgetDecision` | enum | 预算判定（ALLOW/STOP） |

### 5.2 示例

```json
{
  "finding": "OrderService.Save 无事务",
  "context_budget_time": "Medium",
  "context_budget_complexity": "Low",
  "context_budget_accuracy": "High",
  "context_cost": "Low",
  "finding_benefit": "High",
  "budget_exceeded": false,
  "budget_decision": "ALLOW"
}
```

---

## 6. Context Budget 风险与缓解

| 风险 | 缓解措施 |
|------|----------|
| **成本失控** | Context Budget 三维度评估 + 成本 > 收益则 STOP |
| **收益高估** | Finding Benefit 严格评估（Critical/High/Medium/Low） |
| **预算绕过** | BudgetExceeded 字段 + 必须记录预算判定 |
| **弱化 v4 纪律** | Context Budget 不替代 Semantic Budget，两者独立 |

---

## 7. Context Budget 与 v4 Semantic Budget 的关系

### 7.1 两者独立

- **Context Budget**：获取额外证据的预算（Context Expansion 阶段）
- **Semantic Budget**：修改代码的预算（Fix 阶段）

### 7.2 两者不冲突

- **Context Budget**：限制获取上下文的边界
- **Semantic Budget**：限制代码修改的范围

**Context Expansion 后 GO 仍需遵守 Semantic Budget。**

### 7.3 示例

**示例：OrderService.Save 无事务**

- **Context Budget**：Level 1 → DI 生命周期 + [UnitOfWork] 可用性 → Cost=Low, Benefit=High → ALLOW
- **Semantic Budget**：+1 using +2 [UnitOfWork] → Semantic Scope=Low, Physical Diff=3 lines, Dependency Expansion=None → ALLOW
- **最终判定**：GO（Context Budget ALLOW + Semantic Budget ALLOW）

---

## 8. 总结

> ⚠️ 本总结描述的是 Pre-Patch 旧模型，操作性判据已废止（见文件头横幅）。现行模型：Scope/Depth/Artifact/Iteration 四维可数 Budget + Time 仅参考，判停走 STOP-1~5，见 Patch v2 §1/§3。

Context Budget = **时间预算 + 复杂度预算 + 准确性预算 + 成本 > 收益判定**

- **三维度**：Time / Complexity / Accuracy
- **判定规则**：成本 > 收益 → STOP
- **输出字段**：7 个必须附加字段
- **与 v4 关系**：Context Budget 不替代 Semantic Budget，两者独立

**核心纪律**：Context Expansion 必须在 Context Budget 内进行，成本 > 收益则 STOP。

---

**本规格待人工验收。验收通过后，才能进入 R2 Context Acquisition 设计阶段。**
