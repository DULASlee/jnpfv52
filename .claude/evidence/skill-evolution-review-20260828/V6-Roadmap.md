# V6 Roadmap — R1 → R4 路线图

> 只设计路线，不执行。基于 Capability Evolution Matrix 和 Context Expansion Model 推导。
>
> 🟢 **状态更新（2026-08-28 人工验收）**：R1 = **PASS 并冻结**（F-R1-①：除 R2 真实执行证据证明 R1 缺陷外，R1 交付物不再修订）。R2 已解锁排期但**须先经 `R2-Design-and-Validation-Specification.md` 人工批准**方可实施；R2 = R1 Contract 的 Consumer（F-R1-③）。R2 的验证问题是"Agent 是否实际按规则行动"（F-R1-②），不是再验规则可执行性。

## 路线图总览

```
R1 Context Model
    ↓
R2 Context Acquisition (Level 0/1)
    ↓
R3 Automated Evidence (Level 2)
    ↓
R4 Decision Integration
```

---

## R1 — Context Model

### 目标
定义跨类上下文模型与证据结构。

### 交付物
- `Cross-Class-Context-Model.md`：五种上下文类型（调用/DI/Ownership/数据流/跨层）的形式化定义
- `Context-Evidence-Structure.md`：每种上下文类型的证据字段定义
- `Context-Expansion-Rules.md`：触发条件、终止条件、成本评估规则

### 验收标准
- 五种上下文类型有明确定义
- 每种上下文类型有证据字段清单
- Context Expansion 规则可执行（不模糊）

### 依赖
- 无（纯设计，不依赖工具）

### 预计工作量
- 1-2 天

---

## R2 — Context Acquisition (Level 0/1)

### 目标
实现 Level 0/1 的可验证上下文获取。

### 交付物
- `Level-0-Context-Template.md`：人工提供上下文的标准模板
- `Level-1-Context-Acquisition.md`：从静态信息（接口签名/DI 注册/项目结构）推断上下文的规则
- `Level-0-1-Validation.md`：Level 0/1 上下文正确性验证方法

### 验收标准
- Level 0 模板可用于 3 个真实场景（FileService DownloadAll / OrderService UnitOfWork / ScheduleService N+1）
- Level 1 可从接口签名推断调用关系
- Level 1 可从 DI 注册代码推断生命周期
- Level 0/1 上下文正确性可验证（不靠猜）

### 依赖
- R1（Context Model）

### 预计工作量
- 2-3 天

---

## R3 — Automated Evidence (Level 2)

### 目标
研究/实现必要的 Roslyn / Call Graph / DI 自动取证。

### 交付物
- `Level-2-Requirements.md`：Level 2 必须支持的上下文类型与证据字段
- `Roslyn-CallGraph-Spec.md`：Roslyn call-graph 的输出格式与精度要求
- `Roslyn-DI-Graph-Spec.md`：Roslyn DI-registration 的输出格式
- `Level-2-Tool-Prototype`（可选）：最小可行工具原型（不要求生产级）

### 验收标准
- Level 2 需求明确（不模糊）
- Roslyn call-graph 可输出 caller/callee 关系（精度要求：方法级，非类级）
- Roslyn DI-graph 可输出 injection/lifetime 关系
- Level 2 工具原型可在 1 个真实场景跑通（如 FileService DownloadAll）

### 依赖
- R2（Level 0/1 已验证）

### 预计工作量
- 5-10 天（Roslyn 工具开发复杂度高）

### 风险
- Roslyn MSBuildWorkspace 加载大型解决方案可能失败
- Call graph 精度可能不足（如虚方法/接口调用）
- 工具开发成本可能超过收益

### 缓解
- 先做最小可行原型，不追求生产级
- 如果 Level 2 成本过高，可停留在 Level 0/1

---

## R4 — Decision Integration

### 目标
把跨类证据真正接入 Finding → GO/STOP/NEED 决策。

### 交付物
- `V6-Decision-Integration.md`：跨类证据如何接入 GO/STOP/NEED 的具体规则
- `V6-Finding-Template.md`：v6 Finding 模板（含 Context Expansion 字段）
- `V6-Golden-Examples.md`：3 个跨类 Golden Examples（STOP→GO / STOP→NEED / STOP→STOP）
- `V6-Evaluation-Cases.md`：能力型 Evaluation Cases（见 Evaluation Strategy）

### 验收标准
- 跨类证据可接入 GO/STOP/NEED 决策（有明确规则）
- v6 Finding 模板包含 Context Expansion 字段
- 3 个跨类 Golden Examples 可复现
- Evaluation Cases 可执行（不模糊）

### 依赖
- R3（Level 2 工具已实现，或 Level 0/1 已验证）

### 预计工作量
- 2-3 天

---

## 路线图风险与缓解

| 风险 | 缓解措施 |
|------|----------|
| **R3 成本过高** | 先做最小可行原型，不追求生产级；如果成本过高，可停留在 Level 0/1 |
| **Level 2 精度不足** | 明确精度要求（方法级，非类级）；如果精度不足，可标记 NEED EVIDENCE |
| **路线图机械执行** | 每阶段结束做验收，不满足则停止，不机械推进 |
| **弱化 v4 纪律** | 每阶段明确继承 v4 核心纪律，不替代 |

---

## 路线图推荐顺序

**推荐：R1 → R2 → (R3 可选) → R4**

理由：
- R1/R2 是基础，必须先做
- R3 成本高，可选（如果 Level 0/1 已能满足大部分场景）
- R4 是集成，必须在 R1/R2 之后

**如果 R3 成本过高，可停在 R2：**
- Level 0/1 已可验证 v6 决策模型
- Level 2 可留作未来工作
- v6 可定性为"v6.0-Level-0-1"，不追求完整 Level 2

---

## 路线图与 Capability Evolution Matrix 的对应

| Roadmap | Capability Evolution Matrix |
|---------|---------------------------|
| R1 Context Model | #9 Call graph / #10 DI / #12 Cross-class / #13 Cross-layer / #14 Context expansion |
| R2 Context Acquisition | #9/#10/#12/#13/#14 的 Level 0/1 实现 |
| R3 Automated Evidence | #9/#10 的 Level 2 实现 |
| R4 Decision Integration | #5 GO/STOP/NEED 的跨类扩展 |

---

## 总结

V6 Roadmap = **R1 Context Model → R2 Context Acquisition → R3 Automated Evidence → R4 Decision Integration**

- R1/R2 是基础，必须先做
- R3 可选（成本高，可停在 Level 0/1）
- R4 是集成，必须在 R1/R2 之后
- 每阶段结束做验收，不满足则停止
- 明确继承 v4 核心纪律，不弱化安全边界
