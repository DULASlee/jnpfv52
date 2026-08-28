# Scenario RB-X5
## 你的任务（Executor · 盲测）
你是执行"通用类级专家重构 Context 协议"的 Agent。对下方 Finding 执行完整协议并产出 trace 文件。

**必须遵守的唯一规则源**：`D:/JNPF-v52/.claude/evidence/skill-evolution-review-20260828/R1-Operationalization-Patch.md`（v2，§1 Budget 分档表 / §2 五元组+可证伪三问 / §3 五种 STOP+穷举模板 / §4 Escalation / §6 契约）。先读该文件再动手。

**代码访问范围**：只允许读取本场景目录 `baseline/` 下的文件（等同冻结仓库）。禁止读其他任何仓库路径。工具：Read / Grep /（可用时）Serena。禁止全仓拖网。

**输出**：一份 JSON trace，schema 见 `../Level-0-1-Validation.md` §2（r2-trace/1），写到指定输出路径。final.decision 只允许 GO / STOP / NEED_EVIDENCE 三值。每轮必须有 stop_check（STOP4→5→1→2→3 顺序）。
## Finding
`JNPF.Extend/OrderService.cs`（当前基线，含 [UnitOfWork]）直接读写其他模块实体：:20 `using JNPF.WorkFlow.Entitys.Entity;`，:85 三表联查含 `FlowTaskEntity`，:259 `Queryable<FlowTaskEntity>()`。而 `JNPF.Extend.csproj` 的 ProjectReference 列表中只有 `JNPF.WorkFlow.Interfaces`，没有 `.Entitys`。
问题：OrderService 的事务边界内跨模块触碰 FLOW_TASK 域实体，是否安全需要 FLOW_TASK 的写入语义证据。风险 High。
提示（供参考）：FLOW_TASK 相关服务实现类位于 `JNPF.WorkFlow` 模块（该目录**未**包含在本场景 baseline 中，不可读）。
## Trace 输出路径
`D:/JNPF-v52/.claude/evidence/skill-evolution-review-20260828/r2/traces/RB-X5/run-{N}.json`
