# Golden Example #1 — Confirmed Low-Cost Exception Context Fix（v4.0 冻结）

> **提交**：`e45f724a` `backend/modularity/extend/JNPF.Extend/EmailService.cs:145` `Delete` `catch (Exception) → catch (Exception ex) + InnerException`  
> **类型**：Confirmed Low-Cost Exception Context Fix（非代码冻结，冻结决策范式）  
> **价值**：证明 AI 知道何时该修、修多少、何时必须不修

## 冻结的不是代码，是范式

```
发现多个问题（11 Findings）→ 不全部修改
  → 识别真正 Contract violation（F-03 异常吞栈，High）
  → 过门控（Risk P1 / Gate 非性能免 BDN / Budget 低成本）
  → 缩小 Fix Boundary（单类单点：Delete catch 一处）
  → 单点修改（2+2 行，错误码/行为不变，仅保留 InnerException）
  → 验证（Build 0 错 + 行为不变 + 可观测增强）
  → 提交（单类单提交，无扩散）
```

## 本例通过的 6 要素（Evidence → Modify）

| 要素 | 本例证据 |
|------|----------|
| 1 Evidence 确认 | `EmailService.cs:145` `catch(Exception){ throw Oh(COM1002) }` 有文件:行号，试点 Pack 已量化 |
| 2 Contract | 异常体系 E/E2，可观测 Contract violation，已定级 High P1 |
| 3 单点边界 | `Delete` 单点 catch 一处，无需跨类 |
| 4 门控通过 | Risk P1 / Gate 非性能 / Budget 低成本 |
| 5 回归路径 | build + 行为特征（错误码不变）+ InnerException 可检 |
| 6 不扩 Contract | 错误码 COM1002、状态码、签名均不变 |

## 本例拒绝的 10 项（Evidence → Stop）

其余 10 Findings 全部命中 Stop（如 F-02 仓储收敛需跨类、F-04 需性能证据、F-01 仅 4 分支不升 Strategy 等），均记为“暂不实施”，未批量修。

## 自检对照（供未来 Agent 使用）

- 是否先取证再定级？ **是**（Pilot-2 Pack → Risk）
- 是否 Finding≠Fix？ **是**（11→1）
- 是否高级技术受控？ **是**（三安全阀全否）
- 是否最小修改？ **是**（2+2）
- 是否行为保持？ **是**（COM1002 同）
- 是否保留诊断？ **是**（InnerException）
- 是否可回归？ **是**（Build 0 错）
- 是否范围纪律？ **是**（单类单 Finding 单提交）

## 复用指引

本范式可复用于数据库、并发、缓存、异步、资源生命周期等完全不同问题。复用时对照 6 要素与 10 拒绝条件，任一不满足即 Stop，不得以“经验”补全。

## 引用

- 证据链：`../../evidence/class-refactor-expert-v40/pilot-email/P0-Evidence-Pack.md` F-03 + `../../evidence/class-refactor-expert-v40/first-refactor-email-f03/Evidence-Pack.md`
- 规格：`../../../docs/superpowers/specs/通用类级重构专家Skill规格-v4.0.md` §4.6/§5
- 提交：`e45f724a`
