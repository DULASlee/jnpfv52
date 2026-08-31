# ADR-020: R2-COMP 独立 AI 验证作为主要验证机制

**状态:** Final
**日期:** 2026-08-30
**阶段:** Phase 8 / P8-A.6 / 验证机制升级

---

## 背景

JNPF Phase 8 Table Refactoring Expert Skill 的生产应用需要稳定可靠的验证机制。原计划以 R1（人类盲审）作为主要验证手段，但面临以下问题：

1. **规模化矛盾** — 274 张生产表全部人工盲审需要数十人日
2. **一致性矛盾** — 不同人类评审员之间可能存在判断差异
3. **可重复性矛盾** — 人类评审难以 100% 复现
4. **可复用矛盾** — AI Skill 验证后无法直接服务于其他 AI 验证场景

---

## 决策内容

**R2-COMP（独立 AI 专家 Comparative Validation）作为 Skill 验证的主要机制。**

```
R1（人类盲审）= High-Risk Governance 角色（保留历史证据）
R2-COMP（独立 AI 验证）= PRIMARY Skill 验证机制

R2-COMP 标准：
  - 10 张测试表（Round 1: 5 张普通 + Round 2: 5 张对抗性）
  - 8 metrics（维度一致性 / 发现一致性 / 风险一致性 / 等）
  - 4 safety gates（Hard Gate FN / P0/P1 / Scope / Closure）
  - Stop Rule（5 criteria 全部满足时停止）
  - 跨家族 AI（如 DeepSeek + Judge）避免自偏好
```

---

## 理由

### 1. 解决了规模化和可重复性问题

```
R1 人工盲审：
  - 单次：5/5 表，1 人日
  - 全量（274 表）：~55 人日
  - 可重复性：低（人类判断波动）

R2-COMP 独立 AI：
  - 单次：10 表，自动化执行
  - 可重复性：100%（同一输入产生相同判断）
  - 可审计：完整证据链
```

### 2. 8 metrics + 4 safety gates 设计完整

| Metric | 阈值 | v1.0 实际 |
|--------|------|---------|
| 1. Dimension Agreement | ≥ 0.75 | 70/70 = 100% |
| 2. Finding Agreement | ≥ 0.60 | ~97% |
| 3. Risk Agreement | EXACT/ADJACENT | 10/10 EXACT |
| 4. Hard Gate Agreement | 0 CRITICAL | 0 ✅ |
| 5. Action Agreement | EXACT/EQUIV | 10/10 ✅ |
| 6. Closure Agreement | MATCH/SEMANTIC | 10/10 MATCH |
| 7. Evidence Sufficiency | AGREE | 10/10 ✅ |
| 8. Scope Agreement | AGREE | 10/10 ✅ |

| Safety Gate | 阈值 | 实际 |
|-------------|------|------|
| S1 Hard Gate FN | 0 | 0 ✅ |
| S2 P0/P1 Error | 0 | 0 ✅ |
| S3 Scope Error | 0 | 0 ✅ |
| S4 Closure Error (MAJOR) | 0 | 0 ✅ |

### 3. 跨家族 AI 验证避免自偏好

```
普通 A/B Test：模型 vs 自己
R2-COMP：模型 vs 跨家族 AI（如 DeepSeek, Judge mimo）
→ 避免"模型对自家输出更宽容"的自偏好偏差
```

### 4. Stop Rule 避免无限验证

```
IF all 5 criteria 满足：
  P0/P1 Error = 0
  Hard Gate FN = 0
  Scope Error = 0
  TABLE CLOSED Error = 0
  No systemic defect pattern
THEN R2-COMP PASS, stop validation
```

### 5. R1 仍然保留作为治理层

R1 不取消，但降级为：
- Hard Gate 争议解决
- P0/P1 决策仲裁
- 核心架构演进的人工把关
- 历史证据保留

---

## 备选方案

| 方案 | 优点 | 缺点 | 为何不选 |
|---|---|---|---|
| 仅 R1 人工盲审 | 直观、人类判断 | 不可规模化、不可重复、~55 人日 | 规模化矛盾 |
| 仅 R2-COMP，无 R1 | 自动化、高效率 | 无人类把关、争议无仲裁 | 高风险场景需要人类 |
| **R2-COMP 主 + R1 治（本决策）** | 自动化 + 人工把关 | 需双层流程 | ✅ 选择此项 |
| 第三方咨询机构 | 独立专业 | 成本高、周期长 | 战略时机不对 |

---

## 后果

### 正面

- **效率提升 10x+** — R2-COMP 自动执行，AI Skill 验证时间从人日级降到分钟级
- **可重复性 100%** — 相同输入产生相同判断
- **可审计** — 完整证据链支持外部审计
- **可复用** — 验证模式可应用于未来其他 Skill

### 负面

- **跨家族 AI 依赖** — 需要可用的独立 AI 模型（DeepSeek, Judge 等）
- **R1 资源需求降低但未消失** — High-Risk 治理层仍需 R1 介入
- **Stop Rule 阈值需精心设计** — 阈值过高会漏判，过低会过度验证

### 风险缓解

- Stop Rule 设计参考 Industry Standard（如 Anthropic Claude Eval 框架）
- 跨家族 AI 选择需 Chief Architect 审批（避免被污染的"独立"AI）
- R2-COMP 结果由 Chief Architect 最终签字

---

## 验证结果

```
Phase 8 R2-COMP 执行结果：

Round 1（Normal Production Stability）：
  - base_message: R2/REFACTOR (2 idx)
  - ext_product_goods: R2/REFACTOR (3 idx)
  - base_advanced_query_scheme: R0/R1/NO-CHANGE
  - base_file: R3+/DEFERRED (HG#4 borderline)
  - flow_template_json: R2/REFACTOR (3 idx)
  - 1 RUBRIC DIFFERENCE (base_message HG#4, non-blocking)
  → 5/5 PASS

Round 2（Adversarial/Boundary Stability）：
  - sa_business_process: R3+/DEFERRED (FK hub)
  - sa_decision_table: R3+/DEFERRED (FK leaf)
  - WM_BillDetail: R3+/DEFERRED (legacy)
  - base_msg_account: R3+/DEFERRED (sensitive credentials)
  - base_visual_filter: R3+/DEFERRED (dynamic)
  → 5/5 PASS, 0 disagreements

Combined:
  - 10/10 tables PASS
  - 4/4 safety gates PASS
  - 0 P0/P1 errors
  - 0 Hard Gate FN
  - 0 Scope errors
  - 0 Closure errors
  - Stop Rule TRIGGERED

详见 p8-a/r2/CROSS-ROUND-CUMULATIVE-AND-GATE-DECISION.md
```

---

## 相关 ADR

- ADR-019: Table Refactoring Expert Skill v1.0 冻结决策（v1.0 验证结果基于本决策）
- ADR-021: Triple-Key Iron Law（R2-COMP 验证覆盖此规则）
- ADR-022: NO-CHANGE 主动判断原则（R2-COMP 验证了此规则）

## 相关资产

- `docs/universal/Phase-8/p8-a/r2/R2-MASTER-PLAN.md` — R2-COMP 框架设计
- `docs/universal/Phase-8/p8-a/r2/R2-COMPARISON-PROTOCOL.md` — 8 metrics + 4 safety gates 详细定义
- `docs/universal/Phase-8/p8-a/r2/CROSS-ROUND-CUMULATIVE-AND-GATE-DECISION.md` — 验证结果

