# Scenario RB-01
## 你的任务（Executor · 盲测）
你是执行"通用类级专家重构 Context 协议"的 Agent。对下方 Finding 执行完整协议并产出 trace 文件。

**必须遵守的唯一规则源**：`D:/JNPF-v52/.claude/evidence/skill-evolution-review-20260828/R1-Operationalization-Patch.md`（v2，§1 Budget 分档表 / §2 五元组+可证伪三问 / §3 五种 STOP+穷举模板 / §4 Escalation / §6 契约）。先读该文件再动手。

**代码访问范围**：只允许读取本场景目录 `baseline/` 下的文件（等同冻结仓库）。禁止读其他任何仓库路径。工具：Read / Grep /（可用时）Serena。禁止全仓拖网。

**输出**：一份 JSON trace，schema 见 `../Level-0-1-Validation.md` §2（r2-trace/1），写到指定输出路径。final.decision 只允许 GO / STOP / NEED_EVIDENCE 三值。每轮必须有 stop_check（STOP4→5→1→2→3 顺序）。
## Finding
类：`JNPF.Extend/OrderService.cs`（baseline 内，Pre-Fix 历史基线）
方法 `Save`（:198 起）连续执行多条 `Deleteable/Insertable/Updateable...ExecuteCommandAsync()`（:226-:237），**无事务包裹**。若中途一步失败，已执行步骤不回滚 → 订单/明细/应收出现部分成功不一致。
风险等级：High。

## 设计文档摘录（未经验证，仅供参考方向）
> 旧设计示意写道："从 DI 注册看，OrderService 应已通过 `services.AddScoped<OrderService>()` 注册为 Scoped。"

## Trace 输出路径
`D:/JNPF-v52/.claude/evidence/skill-evolution-review-20260828/r2/traces/RB-01/run-{N}.json`（N 由调度者给定）
