# Context Level Model — Level 0/1/2 定义

> **版本**：v6.0-R1-draft | **日期**：2026-08-28 | **状态**：R1 设计规格（待人工验收）  
> **基于**：V6-Context-Model.md + Context-Expansion-Rules.md  
> **纪律**：不修改 v4 已冻结协议；不实现 Level 2 工具；不审计 JNPF 新类

> ⚠️ **操作性规则已更新（2026-08-28 Patch v2 横幅）**：**唯一操作源 = `R1-Operationalization-Patch.md` v2**。
> - 本文件所有 **"成本 > 收益"停止条件**（§2.8/§3.8/§4.8）与 **§6 升级决策流程中"成本合理?"分支**已废止，替换为：升级与否 = ① §2.3 五元组是否缺口在该 Context Type；② §1.2 分档 Budget 是否有余量；③ §4.1 E3 是否命中（剩余取证全部依赖 Level 2 → STOP-5/Escalation，决策冻结 NEED EVIDENCE）。
> - Level 0/1/2 定义、可信度分级（与 Patch §2.2 Confidence 判据一致）、"先证伪低 Level 才主张高 Level"优先级纪律**继续有效**。
> - 本文件 JSON 输出中的 `cost: Low/Medium/High` 字段为 Pre-Patch 遗留表述，R2 契约以 Patch §6 为准。

---

## 1. 核心原则

### 1.1 优先级

**Level 0 → Level 1 → Level 2**

- **Level 0（人工）**：成本最高，但最灵活
- **Level 1（静态信息）**：成本中等，利用已有静态信息
- **Level 2（工具）**：成本最低（一旦实现），但需开发

### 1.2 升级条件

**必须先证明 Level 0/1 无法满足，才能主张 Level 2。**

---

## 2. Level 0 — 人工提供

### 2.1 定义

**Level 0 = 人工描述跨类上下文。**

### 2.2 输入

- 人工提供的调用链描述
- 人工提供的 DI 注入关系
- 人工提供的 ownership 链
- 人工提供的数据规模估计

### 2.3 输出

```json
{
  "level": "Level0",
  "context_type": "Call/DI/Ownership/DataFlow/CrossLayer",
  "description": "人工描述的上下文",
  "evidence": "人工提供的证据",
  "confidence": "Medium",
  "cost": "High"
}
```

### 2.4 可信度

- **Medium**：依赖人工描述的准确性
- **风险**：人工可能遗漏或错误描述

### 2.5 成本

- **High**：需要人工介入，时间成本高
- **适用**：验证阶段，成本可接受

### 2.6 适用场景

- 验证 v6 决策模型本身
- 快速原型验证
- 无法从静态信息推断的场景

### 2.7 升级条件

**Level 0 → Level 1：**

- 人工描述成本过高
- 人工描述不可靠（多次错误）
- 需要从静态信息验证

### 2.8 降级/停止条件

**Level 0 → STOP：**

- 人工无法提供可靠描述
- 人工描述与静态信息矛盾
- 成本 > 收益

### 2.9 示例

**示例：FileService.DownloadAll 临时目录 ownership**

```json
{
  "level": "Level0",
  "context_type": "Ownership",
  "description": "临时目录由前端下载后清理",
  "evidence": "人工描述",
  "confidence": "Medium",
  "cost": "High",
  "decision": "STOP（跨层 ownership，不能局部修复）"
}
```

---

## 3. Level 1 — 静态信息

### 3.1 定义

**Level 1 = 利用已有静态信息进行有限上下文扩展。**

### 3.2 输入

- 接口签名（方法参数/返回类型）
- DI 注册代码（Startup.cs / Program.cs）
- 项目结构（.csproj 依赖关系）
- 代码注释/文档

### 3.3 输出

```json
{
  "level": "Level1",
  "context_type": "Call/DI/Ownership/DataFlow/CrossLayer",
  "inferred_from": "接口签名/DI注册代码/项目结构",
  "evidence": "推断的证据",
  "confidence": "High",
  "cost": "Medium"
}
```

### 3.4 可信度

- **High**：基于静态信息推断，可验证
- **风险**：推断可能不完整（如虚方法/接口调用）

### 3.5 成本

- **Medium**：需要分析静态信息，但不需要工具
- **适用**：大部分跨类场景

### 3.6 适用场景

- 从接口签名推断调用关系
- 从 DI 注册代码推断生命周期
- 从项目结构推断依赖关系

### 3.7 升级条件

**Level 1 → Level 2：**

- 静态信息无法提供足够证据
- 需要运行时证据（如实际调用次数/数据规模）
- 需要工具辅助（如 Roslyn call-graph）

### 3.8 降级/停止条件

**Level 1 → Level 0：**

- 静态信息不足以推断
- 需要人工确认

**Level 1 → STOP：**

- 静态信息矛盾
- 成本 > 收益

### 3.9 示例

**示例 1：从接口签名推断调用关系**

```json
{
  "level": "Level1",
  "context_type": "Call",
  "inferred_from": "接口签名 IFileManager.DownloadFileByType returns FileStreamResult",
  "evidence": "FileService.FileDown 调用 IFileManager.DownloadFileByType，返回 FileStreamResult",
  "confidence": "High",
  "cost": "Medium",
  "decision": "GO（using var fs 正确管理）"
}
```

**示例 2：从 DI 注册代码推断生命周期**

```json
{
  "level": "Level1",
  "context_type": "DI",
  "inferred_from": "Startup.cs: services.AddScoped<OrderService>()",
  "evidence": "OrderService 是 Scoped",
  "confidence": "High",
  "cost": "Medium",
  "decision": "GO（[UnitOfWork] 可用）"
}
```

---

## 4. Level 2 — 工具辅助

### 4.1 定义

**Level 2 = 工具辅助自动取证（Roslyn / Call Graph / DI graph）。**

### 4.2 输入

- Roslyn SemanticModel
- Call graph（caller/callee 关系）
- DI-registration graph（injection/lifetime 关系）
- Data-flow analysis（数据流分析）

### 4.3 输出

```json
{
  "level": "Level2",
  "context_type": "Call/DI/Ownership/DataFlow/CrossLayer",
  "tool": "Roslyn call-graph/DI-graph/data-flow",
  "evidence": "工具分析的证据",
  "confidence": "Very High",
  "cost": "Low（一旦实现）"
}
```

### 4.4 可信度

- **Very High**：基于工具分析，可复现
- **风险**：工具精度可能不足（如虚方法/接口调用）

### 4.5 成本

- **Low（一旦实现）**：自动化取证，边际成本低
- **开发成本**：High（需要开发 Roslyn 工具）

### 4.6 适用场景

- 大规模解决方案
- 需要运行时证据（如实际调用次数/数据规模）
- 需要高精度证据

### 4.7 升级条件

**Level 2 是最高级别，无升级。**

### 4.8 降级/停止条件

**Level 2 → Level 1：**

- 工具精度不足
- 工具无法分析（如动态调用）

**Level 2 → STOP：**

- 工具无法提供证据
- 成本 > 收益

### 4.9 示例

**示例：ScheduleService.Delete N+1**

```json
{
  "level": "Level2",
  "context_type": "DataFlow",
  "tool": "Roslyn call-graph + runtime profiling",
  "evidence": "实际调用次数 = 100，每次返回 10 条",
  "confidence": "Very High",
  "cost": "Low（一旦实现）",
  "decision": "GO（确认 N+1 真实存在，需修复）"
}
```

**注意**：Level 2 工具当前 `NOT_FOUND_IN_REPOSITORY`，本规格不实现 Level 2 工具。

---

## 5. Level 对比

| 维度 | Level 0（人工） | Level 1（静态信息） | Level 2（工具） |
|------|----------------|---------------------|-----------------|
| **输入** | 人工描述 | 接口签名/DI 注册/项目结构 | Roslyn SemanticModel/Call graph |
| **输出** | 人工证据 | 推断证据 | 工具分析证据 |
| **可信度** | Medium | High | Very High |
| **成本** | High | Medium | Low（一旦实现） |
| **适用场景** | 验证阶段 | 大部分跨类场景 | 大规模解决方案 |
| **升级条件** | 成本过高/不可靠 | 静态信息不足 | — |
| **降级/停止条件** | 无法提供/矛盾 | 信息不足/矛盾 | 精度不足/无法分析 |

---

## 6. Level 升级决策流程

```
当前 Level N 无法提供足够证据？
    ├─ NO  → 继续使用 Level N
    └─ YES → Level N+1 可用？
                ├─ YES → Level N+1 成本合理？
                │           ├─ YES → 升级到 Level N+1
                │           └─ NO  → STOP（成本 > 收益）
                └─ NO  → STOP（Level N+1 不可用）
```

---

## 7. Level 与 Context Expansion 的关系

### 7.1 Level 0/1 可验证 v6 决策模型

**Level 0/1 已可验证 v6 决策模型，不需要 Level 2。**

- **Level 0**：人工描述跨类上下文，验证 Context Expansion 触发/终止条件
- **Level 1**：从静态信息推断跨类上下文，验证 Context Expansion 决策流程

### 7.2 Level 2 是未来工作

**Level 2 是 v6 的完整形态，但当前不实现。**

- **原因**：开发成本高，精度风险
- **替代方案**：Level 0/1 已可验证 v6 决策模型
- **未来工作**：如果 Level 0/1 成本过高，可考虑实现 Level 2

### 7.3 v6 可定性为"v6.0-Level-0-1"

**如果 R3 成本过高，可停在 Level 0/1：**

- Level 0/1 已可验证 v6 决策模型
- Level 2 可留作未来工作
- v6 可定性为"v6.0-Level-0-1"，不追求完整 Level 2

---

## 8. 总结

Context Level Model = **Level 0/1/2 定义 + 优先级 + 升级/降级条件 + 对比**

- **Level 0**：人工提供，成本高，灵活
- **Level 1**：静态信息推断，成本中等，大部分场景可用
- **Level 2**：工具辅助，成本低（一旦实现），但需开发
- **优先级**：Level 0 → Level 1 → Level 2
- **升级条件**：Level N 无法提供足够证据 + Level N+1 成本合理
- **降级/停止条件**：Level N 无法提供/矛盾 + 成本 > 收益

**当前建议**：先实现 Level 0/1，验证 v6 决策模型；Level 2 留作未来工作。

---

**本规格待人工验收。验收通过后，才能进入 R2 Context Acquisition 设计阶段。**
