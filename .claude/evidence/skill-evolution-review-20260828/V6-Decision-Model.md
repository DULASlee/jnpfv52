# V6 Decision Model — 继承 v4 GO/STOP/NEED 并扩展跨类判定

> v6 不能因为拥有更强的取证能力，就弱化 v4 已经建立的安全边界。

> ⚠️ **2026-08-28 Patch v2 横幅**：三门封闭原则与本文全部一致（Patch §4.0 已把 ESCALATE 明确为非 Decision 的动作，Decision 恒为 GO/STOP/NEED EVIDENCE）。§7 风险表中"成本 > 收益则 STOP"缓解措施已由 Patch §1.2 Budget 计数 + §3 STOP-1~5 取代；三门语义、DecisionChange 路径、"Expansion 后 GO 条件更严格"纪律继续有效。

## 1. 核心原则

### 继承 v4 三门判定

v6 **完全继承** v4 的 GO/STOP/NEED 三门判定逻辑：

- **GO (Allow Modify)**：Finding evidenced ∧ Contract violation ∧ single-point boundary ∧ gates pass ∧ regression path exists ∧ no Contract expansion
- **STOP (Must Stop)**：sufficient evidence to decide NOT to do now（10 种 disjunctive 条件）
- **NEED EVIDENCE**：insufficient evidence to decide（可能真实存在，但证据不足）

### v6 新增：Context Expansion 后的重新判定

v6 唯一新增的是：**当当前类证据不足时，允许通过 Context Expansion 获取跨类证据，然后重新进入三门判定。**

```
v4 协议：当前类证据 → GO/STOP/NEED
v6 协议：当前类证据 → (若不足) → Context Expansion → 跨类证据 → GO/STOP/NEED
```

---

## 2. 跨类 Finding 的判定逻辑

### 问题：一个跨类 Finding 是否仍然属于 STOP？

**答案：不一定。取决于 Context Expansion 后获得的证据。**

### 三种可能的判定路径

#### 路径 A：STOP → Context Expansion → GO

**条件：**
- 初始判定为 STOP（因为跨类边界，不能局部修复）
- Context Expansion 后获得证据：该 Finding 实际上可以在当前类安全修复
- 修复不违反 Contract、不引入跨模块依赖、可回归

**示例：**
```
当前类：OrderService
Finding：Save 方法无事务（多步 DB 操作）
初始判定：STOP（跨类边界，需确认 UnitOfWork 是否可行）
Context Expansion：
  - Level 1：检查 DI 注册 → OrderService 是 Scoped
  - Level 1：检查 [UnitOfWork] 是否可用 → JNPF.DatabaseAccessor 提供
  - 结论：可以在 OrderService 内部加 [UnitOfWork]
Context Expansion 后判定：GO（+1 using +2 [UnitOfWork]，Semantic Budget 内）
```

#### 路径 B：STOP → Context Expansion → NEED EVIDENCE

**条件：**
- 初始判定为 STOP（因为跨类边界）
- Context Expansion 后获得证据：该 Finding 可能真实存在，但需要运行时证据
- 运行时证据无法获取（环境受限/成本过高）

**示例：**
```
当前类：ScheduleService.Delete
Finding：N+1 循环查询（foreach 内 ToListAsync）
初始判定：STOP（跨类边界，需确认实际调用次数）
Context Expansion：
  - Level 1：检查循环条件 → 无法确定数据规模
  - Level 2：需要 Roslyn call-graph + runtime profiling → 未实现
  - 结论：无法获取必要证据
Context Expansion 后判定：NEED EVIDENCE（Level 2 未实现，冻结待运行时证据）
```

#### 路径 C：STOP → Context Expansion → STOP（仍 STOP）

**条件：**
- 初始判定为 STOP（因为跨类边界）
- Context Expansion 后获得证据：该 Finding 确实需要跨类修复，不能局部处理
- 修复违反 Contract、引入跨模块依赖、或成本过高

**示例：**
```
当前类：FileService.DownloadAll
Finding：临时目录未清理（R-03）
初始判定：STOP（跨层 ownership，不能局部修复）
Context Expansion：
  - Level 0：人工描述 → "临时目录由前端下载后清理"
  - 结论：ownership 跨层，不能在 FileService 内部修复
Context Expansion 后判定：STOP（跨层 ownership，正确拒绝局部修）
```

---

## 3. v6 Decision Model 判定流程

```
当前类 Finding
    ↓
当前类证据是否充分？
    ├─ YES → 直接 GO/STOP/NEED（v4 协议）
    └─ NO  → 判断是否触发 Context Expansion
                ↓
            触发条件满足？（Finding 真实 + 证据不足 + 上下文直接影响判定）
                ├─ NO  → 直接 NEED EVIDENCE 或 STOP
                └─ YES → Context Expansion
                            ↓
                        获取跨类证据（Level 0/1/2）
                            ↓
                        跨类证据是否充分？
                            ├─ YES → 重新判定 GO/STOP/NEED
                            │           ├─ GO → 修复（遵守 Semantic Budget）
                            │           ├─ STOP → 记录理由（跨类边界/成本过高）
                            │           └─ NEED EVIDENCE → 冻结待补充
                            └─ NO  → NEED EVIDENCE（Level 2 未实现/无法获取）
```

---

## 4. v6 Decision Model 输出字段

v6 Finding 必须附加以下字段：

| 字段 | 类型 | 说明 |
|------|------|------|
| `InitialDecision` | enum | 初始判定（GO/STOP/NEED） |
| `ContextExpansionTriggered` | bool | 是否触发上下文扩展 |
| `ContextLevel` | enum | 使用的上下文级别（Level0/Level1/Level2） |
| `ContextObtained` | bool | 是否成功获取上下文 |
| `FinalDecision` | enum | 最终判定（GO/STOP/NEED） |
| `DecisionChange` | enum | 判定变化（Unchanged/STOP→GO/STOP→NEED/STOP→STOP） |
| `DecisionRationale` | string | 判定理由（为什么最终这样判定） |

---

## 5. v6 Decision Model 与 v4 纪律的兼容性

### 继承 v4 核心纪律

| v4 纪律 | v6 Decision Model 如何继承 |
|---------|---------------------------|
| **GO 六要素** | Context Expansion 后 GO 仍需满足六要素 |
| **STOP 十要素** | Context Expansion 后 STOP 仍需满足十要素之一 |
| **NEED EVIDENCE 语义** | Context Expansion 后 NEED EVIDENCE 仍表示"证据不足" |
| **Finding ≠ Fix** | Context Expansion 是为了更好判定，不是为了自动修复 |
| **Semantic Budget** | Context Expansion 后 GO 仍需遵守 Semantic Budget |
| **Single commit** | Context Expansion 不引入额外提交 |

### 防止 v6 弱化安全边界

**v6 不能因为拥有更强的取证能力，就把原本的 STOP 轻易转为 GO。**

必须遵守：
- **Context Expansion 后 GO 的条件更严格**（需要跨类证据支持）
- **Context Expansion 后 STOP 的理由更充分**（需要说明为什么跨类证据仍不支持 GO）
- **Context Expansion 后 NEED EVIDENCE 的冻结更诚实**（需要说明缺什么证据）

---

## 6. v6 Decision Model 示例

### 示例 1：STOP → GO（OrderService UnitOfWork）

```
当前类：OrderService
Finding：Save 方法无事务
初始判定：STOP（跨类边界，需确认 UnitOfWork 是否可行）
Context Expansion：
  - Level 1：DI 注册 → OrderService 是 Scoped
  - Level 1：[UnitOfWork] 可用 → JNPF.DatabaseAccessor 提供
  - 结论：可以在 OrderService 内部加 [UnitOfWork]
Final Decision：GO
Decision Change：STOP → GO
Decision Rationale：跨类证据支持在当前类修复，Semantic Budget 内（+1 using +2 [UnitOfWork]）
```

### 示例 2：STOP → NEED EVIDENCE（ScheduleService N+1）

```
当前类：ScheduleService.Delete
Finding：N+1 循环查询
初始判定：STOP（跨类边界，需确认实际调用次数）
Context Expansion：
  - Level 1：循环条件 → 无法确定数据规模
  - Level 2：需要 Roslyn call-graph + runtime profiling → 未实现
  - 结论：无法获取必要证据
Final Decision：NEED EVIDENCE
Decision Change：STOP → NEED EVIDENCE
Decision Rationale：Level 2 未实现，无法获取运行时证据，冻结待补充
```

### 示例 3：STOP → STOP（FileService DownloadAll）

```
当前类：FileService.DownloadAll
Finding：临时目录未清理
初始判定：STOP（跨层 ownership，不能局部修复）
Context Expansion：
  - Level 0：人工描述 → "临时目录由前端下载后清理"
  - 结论：ownership 跨层，不能在 FileService 内部修复
Final Decision：STOP
Decision Change：STOP → STOP（Unchanged）
Decision Rationale：跨层 ownership，正确拒绝局部修，避免跨层传染
```

---

## 7. v6 Decision Model 风险与缓解

| 风险 | 缓解措施 |
|------|----------|
| **STOP 轻易转 GO** | Context Expansion 后 GO 的条件更严格（需跨类证据） |
| **Context Expansion 成本失控** | ContextExpansionCost 字段 + 成本 > 收益则 STOP |
| **弱化 v4 安全边界** | 明确继承 v4 三门判定，Context Expansion 不替代 |
| **误判 NEED EVIDENCE** | 必须说明缺什么证据，不能模糊冻结 |

---

## 8. 总结

v6 Decision Model = **v4 三门判定 + Context Expansion 后的重新判定**

- **继承**：GO/STOP/NEED 三门判定逻辑完全继承 v4
- **扩展**：允许通过 Context Expansion 获取跨类证据，然后重新判定
- **纪律**：Context Expansion 后 GO 的条件更严格，STOP 的理由更充分，NEED EVIDENCE 的冻结更诚实
- **目标**：让 Skill 能基于跨类证据下判断，但不弱化 v4 已经建立的安全边界
