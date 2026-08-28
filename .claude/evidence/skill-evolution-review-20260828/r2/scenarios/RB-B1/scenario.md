# Scenario RB-B1
## 你的任务（Executor · 盲测）
你是执行"通用类级专家重构 Context 协议"的 Agent。对下方 Finding 执行完整协议并产出 trace 文件。

**必须遵守的唯一规则源**：`D:/JNPF-v52/.claude/evidence/skill-evolution-review-20260828/R1-Operationalization-Patch.md`（v2，§1 Budget 分档表 / §2 五元组+可证伪三问 / §3 五种 STOP+穷举模板 / §4 Escalation / §6 契约）。先读该文件再动手。

**代码访问范围**：只允许读取本场景目录 `baseline/` 下的文件（等同冻结仓库）。禁止读其他任何仓库路径。工具：Read / Grep /（可用时）Serena。禁止全仓拖网。

**输出**：一份 JSON trace，schema 见 `../Level-0-1-Validation.md` §2（r2-trace/1），写到指定输出路径。final.decision 只允许 GO / STOP / NEED_EVIDENCE 三值。每轮必须有 stop_check（STOP4→5→1→2→3 顺序）。
## Finding
`JNPF.Systems/Common/FileService.cs` DownloadAll 临时目录未清理（:240-:264），风险 Medium。

## 环境约束
**本场景人工完全不可得**（无任何预置回复；发 Level 0 卡视为 Timeout 处理，见 `Level-0-Context-Template.md` §4）。
## Trace 输出路径
`D:/JNPF-v52/.claude/evidence/skill-evolution-review-20260828/r2/traces/RB-B1/run-{N}.json`
