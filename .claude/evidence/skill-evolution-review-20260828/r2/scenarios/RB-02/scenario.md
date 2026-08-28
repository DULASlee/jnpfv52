# Scenario RB-02
## 你的任务（Executor · 盲测）
你是执行"通用类级专家重构 Context 协议"的 Agent。对下方 Finding 执行完整协议并产出 trace 文件。

**必须遵守的唯一规则源**：`D:/JNPF-v52/.claude/evidence/skill-evolution-review-20260828/R1-Operationalization-Patch.md`（v2，§1 Budget 分档表 / §2 五元组+可证伪三问 / §3 五种 STOP+穷举模板 / §4 Escalation / §6 契约）。先读该文件再动手。

**代码访问范围**：只允许读取本场景目录 `baseline/` 下的文件（等同冻结仓库）。禁止读其他任何仓库路径。工具：Read / Grep /（可用时）Serena。禁止全仓拖网。

**输出**：一份 JSON trace，schema 见 `../Level-0-1-Validation.md` §2（r2-trace/1），写到指定输出路径。final.decision 只允许 GO / STOP / NEED_EVIDENCE 三值。每轮必须有 stop_check（STOP4→5→1→2→3 顺序）。
## Finding
类：`JNPF.Systems/Common/FileService.cs`，方法 `DownloadAll`（:240-:264）在 `TemporaryFile/{随机名}` 创建临时目录并打包 zip，方法内无任何清理路径。
风险等级：Medium。

## 可用人工上下文（Level 0）
本场景人工可用。按 `Level-0-Context-Template.md` 发卡；模拟回复预置于 `human-cards/HR-01.json`（引用时必须使用其中原文 snippet，不得转述升格）。

## Trace 输出路径
`D:/JNPF-v52/.claude/evidence/skill-evolution-review-20260828/r2/traces/RB-02/run-{N}.json`
