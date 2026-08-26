# 通用类级重构专家 Skill 实施计划 v4.0 — 衔接 MASTER/L1/L2 + AI Agent 执行协议

> **版本**：v4.0｜**日期**：2026-08-27｜**状态**：待审核（与规格 v4.0 同步）  
> **上位**：《通用类级重构专家Skill规格-v4.0》《L2-类级螺旋专家级重构方案-v2.0》《MASTER 总体实施计划/设计规格》《L1-表级螺旋执行手册-v1.0》  
> **性质**：实施计划（Plan），按 superpowers 子代理驱动执行。  
> **硬约束**：与 S0–S1 “零业务代码修改”铁律冲突时以上位为准；L11/L12 hooks 仍生效；本文不产生业务代码，仅产出计划与门控产物。

---

## 0. 目标与非目标

### 目标

- 将 v3.0“十大深水区技术清单”升级为 **v4.0 证据驱动的通用类级重构专家 Skill**，补齐 P0 证据门槛、风险矩阵、复杂度预算、Benchmark Gate、过度优化禁令、AI 执行/验收协议。
- 使 Skill 可稳定用于 JNPF / ASP.NET Core / 通用 .NET 的任意类（Repository/Service/Domain/Infrastructure/Middleware/Worker/SDK）。
- 首批 3–5 个聚合完成 P0→重构→验证闭环，产出可复用的 Skill Core 案例库。

### 非目标

- 不在 S0–S1 未双 PASS 前启动 T2.0 表级螺旋首批 5 张表的写操作（MASTER 硬约束）。
- 不引入 Outbox/消息队列/Saga（L2 阶段禁止）。
- 不为 JNPF 创造专用抽象（Generic .NET First）。

---

## 1. 文件结构

```
docs/superpowers/
├── specs/
│   ├── 通用类级重构专家Skill规格-v4.0.md          # 本次新增（Spec）
│   ├── JNPF后端类级代码审计扫描清单-v1.1.md        # 已有，16维度底座
│   ├── JNPF后端类级代码审计扫描设计规格-v1.0.md    # 已有，扫描架构
│   └── MASTER-JNPF后端重构与Aspire微服务化总体设计规格.md
├── plans/
│   ├── 通用类级重构专家Skill实施计划-v4.0.md       # 本文件（Plan）
│   ├── L2-类级螺旋专家级重构方案-v2.0.md           # 已有，L2 SOP
│   ├── L1-表级螺旋执行手册-v1.0.md                 # 已有，表级输入
│   └── MASTER-JNPF后端重构与Aspire微服务化总体实施计划.md
└── evidence/
    └── class-refactor-expert-v40/
        ├── P0-Evidence-Pack-template/               # 计划阶段产出模板
        ├── Risk-Matrix-template.xlsx/md
        ├── Performance-Change-Gate-checklist.md
        └── Benchmark-template/
```

---

## 2. 与上位的时序衔接（关键）

```
S0–S1（只读，已双PASS，T0.3 考卷待补表单/审批写入授权）
  │ 产出：平台资产清单 v1 / 数据责任映射 v1 / Legacy Registry v1 / L1 排序清单 / 行为考卷 30 条基线
  ↓
v4.0 Skill 规格+计划审核（本文阶段，仍为只读，不改业务代码）
  ↓
T1.3 后 STOP 点（人工三态裁决 PASS/REFINE/BLOCK，获准后才可 T2）
  ↓
L1 表级螺旋首批 5 张表（按 gen-l1-order.ps1，表事实卡驱动，产出 表-类-事务矩阵）
  ↓
v4.0 Skill 首批聚合试点（3–5 个最独立聚合，见 §3）
  ↓
L2 全量类级螺旋（按聚合逐批，每聚合 P0→P→验证→回看）
  ↓
L3 模块级螺旋（以 L2 聚合边界为输入）
```

**门控语义（复用 MASTER）**：

- S0 与 S1 双 PASS 前不得启动 T2.0（L1 手册 §0.5）。
- L1 表事实卡未覆盖聚合所涉表 → 该聚合的 L2 不得启动。
- v4.0 P0 未完成 → 该类的 P1..P10 不得启动（L11 硬拦）。

---

## 3. 首批试点选型（与 L2 v2.0 §5 一致，优先级从低耦合到高耦合）

| 批次 | 聚合候选（示例） | 特征 | 目的 |
|------|----------------|------|------|
| Pilot-1 | 日志 / 文件元数据 / 系统配置 | 依赖最少、无事务外溢、表-类关系清晰 | 验证 P0 流程与门控，沉淀模板 |
| Pilot-2 | 用户/租户/权限（只做读路径与边界显式化） | 核心但可从读侧切入 | 验证依赖治理与多租户铁律 |
| Pilot-3 | 工作流/动态表单（选一个子聚合，如表单定义） | 耦合最高 | 验证复杂度预算与热路径门控 |

> 具体聚合以 L1 的“表-类-事务”矩阵产出为准，此处仅为选型策略。

---

## 4. 分阶段实施（5 阶段，均为小批次可演示）

### Phase 0 — Skill 规格与门控固化（1–2 天，零业务代码）

- [ ] **Step 0.1** 审核并定版 `通用类级重构专家Skill规格-v4.0.md`（本文上位）
- [ ] **Step 0.2** 定版 `Performance Change Gate checklist` + `Complexity Budget` 量表 + `P0-Evidence-Pack` 模板（见 §6）
- [ ] **Step 0.3** 在 `guard-write` L11/L12 中登记 P0 缺失阻断规则（仅配置，不改业务）
- [ ] **Step 0.4** 提交审核，获得“通过”后进入 Phase 1

**产出**：Spec v4.0 + Plan v4.0 + 门控模板（本阶段即交付物）。

### Phase 1 — P0 证据能力拉通（2–3 天，仍为只读/快照）

- [ ] **Step 1.1** 为 Pilot-1 聚合生成 `P0-Code-Facts.md`（Roslyn + dependency-scan + 扫描清单 I/L/H）
- [ ] **Step 1.2** 采集 `P0.2 运行时事实`（至少 2 项：dotnet-counters + Benchmark 基线或压测）
- [ ] **Step 1.3** 生成 `Risk / Impact Matrix` 并定级（Critical/High/Medium/Low）
- [ ] **Step 1.4** 归档到 `.claude/evidence/class-refactor-expert-v40/pilot-1/P0-Evidence-Pack/`

**产出**：首个聚合的完整 P0 包（作为后续所有聚合的样板）。

### Phase 2 — 单类单维度试点重构（3–5 天，首个预授权改动）

> 选 Pilot-1 中**风险最高且预授权**的一项（如：静态集合无界增长 / Service 直接操作 DB / IDispoable 缺失），单类单维度。

- [ ] **Step 2.1** 按规格 §4 执行最小复杂度方案（禁止直接上 Span/池化等）
- [ ] **Step 2.2** 单测≥1 + 行为考卷 + 架构测试（依赖环 0）
- [ ] **Step 2.3** 若涉及性能 → 执行 Benchmark 对比，未达收益阈值则回退
- [ ] **Step 2.4** 提交 `git commit`（单类单提交，可回退）+ 更新边界图

**产出**：首个“P0→改→验”闭环案例 + `Final Report`。

### Phase 3 — 3–5 聚合批量验证（1–2 周）

- [ ] **Step 3.1** 复制 Phase 1–2 流程到 Pilot-2/3（每聚合独立分支/提交）
- [ ] **Step 3.2** 沉淀 `Skill Core 案例库`（含反例：何时**不**用 ValueTask/WeakEvent/Record/Strategy）
- [ ] **Step 3.3** 收敛扫描清单误报规则（白名单）与检测正则

**产出**：3–5 聚合的批量证据 + 案例库。

### Phase 4 — Skill 固化与推广（持续）

- [ ] **Step 4.1** 将 P0 模板、Risk 矩阵、Gate 检查清单固化为 Skill 的 `references/` 与 `scripts/`
- [ ] **Step 4.2** 接入 CI：`dotnet build -p:CI_BUILD=true` + `Architecture Tests` + `arch-module-dependency-scan -Gate` + `test-hooks` + `characterization` 全绿
- [ ] **Step 4.3** 发布 Skill 版本（v4.x），后续 L2 全量按此 Skill 执行

---

## 5. AI Agent 执行协议（可控性，写入 Skill 的硬约束）

### 5.1 粒度

- **一个 Chat = 一个可演示结果**（如“Pilot-1 的 P0 包”或“单类×单维度的修复+验证”）。
- 禁止一次处理多类、多维度全量铺开。

### 5.2 单步协议（每步必含）

```
改前快照 → 最小改动（复杂度预算内） → 单测/考卷 → Benchmark（若涉性能） → 架构测试 → 回看（更新边界/风险/遗留） → 人话汇报
```

### 5.3 禁止项（硬拦）

| 禁止 | 判定 | 后果 |
|------|------|------|
| 无 P0 直接改 | P0-Evidence-Pack 缺失 | L11 阻断写入 |
| 无 Benchmark 上高级技术 | Performance Change Gate 7 问未答 | 阻断 |
| 全量弱引用化 | Weak Event 滥用 | 审核打回 |
| string TenantId 用 ConditionalWeakTable | 选型错 | 打回 |
| HttpClient 自建/自 Dispose | 未用 IHttpClientFactory | 打回 |
| .Result → .GetAwaiter().GetResult() 视为已修复 | 仍同步阻塞 | 打回 |
| 机械 ValueTask | 未同时满足三条件 | 打回 |
| ConcurrentDictionary 非原子操作 | 未用原子 API | 打回 |
| 异常类爆炸 | > 10 业务异常类 | 打回 |
| 2 分支上 Strategy/Factory/Scan | 复杂度预算超 | 打回 |
| 日志含 PII/高基数 | 隐私/成本违规 | 打回 |

### 5.4 失败与回退

- 考卷变红 → 立即 `git revert` 到最近提交，记录根因，不得带病前进。
- Benchmark 未达阈值 → 回退方案，改选更低复杂度方案。
- 命中不可逆清单（删类/改公共签名/改事务边界）→ 立即 STOP 等人工。

### 5.5 人话汇报（Boss 模式， ≤10 行）

```
【状态】已修好/卡住/等你批
【人话】发生了什么+现在怎样+你点头后会怎样（各一句，无类名）
【你怎么验】命令一行+产物路径
【要你做】继续/通过/打回/重开（单选）
（可选）详情：.claude/change-requests/CR-*.md
```

---

## 6. 关键模板（Phase 0 产出，随本计划一并定版）

### 6.1 Performance Change Gate 7 问（必填）

```
1. 当前性能是多少？（基线）
2. 热点在哪里？（P0.2）
3. Allocation 是多少？
4. GC 影响是多少？
5. 优化后是多少？
6. 复杂度增加多少？
7. 是否值得？（收益 > 成本 ? go : no-go）
```

### 6.2 Complexity Budget 量表（示例）

| 方案 | 新增行数 | 新增生命周期/池化点 | 维护成本 | 收益 | 决策 |
|------|----------|-------------------|----------|------|------|
| 简单 if | +0 | 0 | 低 | 低 | 2分支时 go |
| 字典映射 | +10 | 0 | 低 | 中 | 3–5分支时 go |
| Strategy+DI | +80 | +3 | 中 | 中 | >5分支且需扩展时 go |
| Plugin Scan | +200 | +5 | 高 | 高 | 仅开放平台时 go |

### 6.3 P0 缺失阻断清单（L11 规则草案）

```
- P0-Evidence-Pack 缺失 → 阻断 backend/**/*.cs 写入
- P0.2 运行时事实 <2 项且动用 P6 → 阻断
- Benchmark 报告缺失且动用 Span/ValueTask/ArrayPool → 阻断
- 风险矩阵未定级 → 阻断
```

---

## 7. 验收标准

### 7.1 本计划验收（Phase 0）

| 项 | 标准 |
|----|------|
| Spec v4.0 | 18 节审核意见全量回应，无遗漏（见 Spec §0–§9） |
| Plan v4.0 | 与 MASTER/L1/L2 时序无冲突，门控可执行 |
| 模板 | P0 包、Risk 矩阵、Gate 7 问、Complexity Budget 可直接使用 |
| 零业务代码 | `git diff` 无 backend 改动（仅 docs/.claude 计划类） |

### 7.2 首批试点验收（Phase 2）

| 项 | 证据 |
|----|------|
| 行为不变 | `tests/characterization` CI 全绿 |
| 风险闭环 | Risk Matrix 对应项 closed |
| 性能门 | BDN 基线 vs 优化后（若涉） |
| 架构门 | `dotnet test --filter Architecture` 通过 |
| 测试门 | 单测≥3 覆盖核心规则 |

---

## 8. 风险与缓解

| 风险 | 缓解 |
|------|------|
| 误把工具当目标（过度优化） | Performance Change Gate + Complexity Budget 双硬拦 |
| Agent 一次改多类失控 | 一个 Chat 一类一维，小切片+可回退提交 |
| 考卷未覆盖导致回归 | 先补考卷再重构（T0.3 待授权项优先） |
| 扫描误报导致错误重构 | 人工抽样 + 白名单，N 维度 Critical 必人审 |

---

## 9. 下一步（待你审核）

- [ ] **你审核**：Spec v4.0 + Plan v4.0（本文）— 重点看 §0 定性、§3 P0、§4 纠正清单、§5 执行协议是否符合你的 18 节意见。
- [ ] **你决策**：`通过 / 打回 / 重开`（老板模式）。
- [ ] **通过后**：进入 Phase 0 Step 0.2–0.3 产出模板并固化为 Skill references，再以 Pilot-1 启动 P0 取证（仍为只读，不改业务代码）。

---

## 10. 版本历史

| 版本 | 日期 | 变更 |
|------|------|------|
| v4.0 | 2026-08-27 | 首版，由 v3.0 审核意见升级：新增 P0/Risk/Complexity/Benchmark/回退等 AI 可控性设计，纠正 10 类 .NET 技术误用，确立 Generic First 与 Boss 汇报 |

