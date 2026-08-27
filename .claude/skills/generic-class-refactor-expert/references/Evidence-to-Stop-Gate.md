# Evidence → Stop 拒绝条件（v4.0 冻结，10 项负能力）+ 防幻觉原则

> **来源**：Golden Example 复盘冻结检查 Q2  
> **门控语义**：命中任一 = 必须 Stop，不得进入 Modify  
> **核心原则**：`不知道 → Stop，而不是让 Agent 用经验补全答案`（防幻觉）

| # | 拒绝条件 | 说明 | 示例 |
|---|----------|------|------|
| 1 | 只有猜测，无充分 Evidence | P0 证据缺失或仅推断，无文件:行号/量化影响 | “好像会泄漏”无证据 |
| 2 | 仅 Capability 缺失，未证明属通用 Contract | 能力可有可无，未映射到扫描清单的 Contract violation | “少个接口”但无边界违规 |
| 3 | 仅 Test gap | 缺单测/覆盖，但无缺陷 Evidence | “无单测”本身不自动触发重构 |
| 4 | Finding 属 `Not a defect` | 误报/白名单（如 `[NonAction]` 权限误报） | N3 对 `[NonAction]` 的误报 |
| 5 | 需扩大公共 Contract | 需改签名/新增公共方法/配置/错误码 | 改接口需新增参数 |
| 6 | 需引入新架构能力 | 需新增 Outbox/队列/Saga/新中间件 | L2 阶段禁止 |
| 7 | 需池化/缓存/并发/异步等高级优化但无性能证据 | Gate 7 问未通过 | ValueTask/Span 无 BDN |
| 8 | 修改边界无法保持单点 | 牵连多类/多文件，无法单提交 | 仓储收敛需改 5 类 |
| 9 | 会牵连其他类/模块而无法维持边界 | 跨模块传染，需大范围改 | 跨聚合改 |
| 10 | 回归行为无法被可靠验证 | 无 build/单测/特征考卷/运行证据路径 | 无法回归 |

**防幻觉原则**（宪法级）：
- `Evidence 不足 → Decision = Stop`，禁止 Agent 用“经验”“最佳实践”“我认为”补全。
- `Finding 多 ≠ 批量修`：每 Finding 独立过门控，单次仅允许一个 Fix Boundary。
- `高级优化必须有证据`：无 P0.2 + BDN 证据，Span/Pool/ValueTask 等一律 Stop.

**STOP vs NEED EVIDENCE（M2 calibrated）**：
- **STOP** = 有足够证据决定“当前不应做”（如 F-L3 ownership 跨层）。
- **NEED EVIDENCE** = 问题可能真实但证据不足以决定做/不做（如 F-P1 N+1 无实测），冻结为 `NEED EVIDENCE / BLOCKED`，禁止因压力强行 GO，也禁止无重决策直接转为 STOP。

**Class-Level Convergence（M3 calibrated）**：
- 当剩余 Finding 满足 `风险收益递减 + 需跨类/架构级变化 + 证据不足 + 局部修改风险上升` 时，主动宣布 `Class-level convergence`，结束当前类局部重构。这不是偷懒，是专家完成准则。

**记录要求**：Stop/Need-Evidence 时必须在 P0-Evidence-Pack 的 Decision 中写明命中的拒绝项编号及依据，形成可审计链。
