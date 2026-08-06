# JNPF AI 错题本

> **加载方式：** 每次编码前 Grep 关键词；犯错误后立即追加。
> **编号规则：** M001-M999 连续，不可重用。重编号在本文件末尾记录映射。

---

## Before You Code（每次写代码前过一遍）

这些是从错误中提炼的**重复模式**。每条背后都有实际犯错记录。

| # | 铁律 | 来源 |
|---|------|------|
| **0** | **先钉死本节点业务功能（①）再动手**：编码/验收前必须说清「本阶段业务能力是什么、用户操作、完整产物」；禁止用表头/文案/待确认节/单测绿顶替业务功能正确完整 | **M035** · 业务优先铁律 · 30号§0.6① |
| **0b** | **S/A 先走 ADF**：架构先行 → 设计模式先行 → 接口契约先行 → 再实现；每阶段等「继续」；B 级须声明豁免 | ADF 三先行 · `architecture-design-interface-first` |
| **0d** | **会话收尾必归档**：有代码变更或调试闭环后 MUST 更新 CURRENT-FOCUS + progress-registry session_log + mistake-log `## 今日` + session-summaries；禁止只靠 Chat 记忆 | **M036–M038** · episodic-memory-automation |
| 1 | **验证三路径**：改了防御代码 → 正向/异常/缺失全测，不能只测修的那条 | M030 |
| 2 | **改 prompt = 改代码**：改完逐条对照原始 spec 审计，不能凭"感觉对了" | M031 |
| 3 | **先抓包再分析源码**：前端无响应 → Playwright `page.on('response')` → 看实际返回体 | M011 |
| 4 | **不跳过 brainstorming**：无论任务多小，MUST 走 S1。输入详尽≠豁免流程 | M009, M024 |
| 5 | **声称完成 = Gate Function 5 步**：IDENTIFY→RUN→READ→VERIFY→CLAIM，缺一不可 | M010 |
| 6 | **零占位符**：禁止 TODO implement / NotImplementedException 糊弄编译；hooks+pre-commit 硬拦 | engineering-laws Law4 · L11 |

---

## 2026-07-18

| 日期 | 类别 | 症状 | 根因 | 修复 | 关键词 |
|------|------|------|------|------|--------|
| 2026-07-18 | 工具链 | hook 自动：设计 Skill 编排（157 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +153 |
| 2026-07-18 | 工具链 | hook 自动：设计 Skill 编排（156 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +152 |
| 2026-07-18 | 工具链 | hook 自动：设计 Skill 编排（155 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +151 |
| 2026-07-18 | 工具链 | hook 自动：设计 Skill 编排（154 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +150 |
| 2026-07-18 | 工具链 | hook 自动：设计 Skill 编排（152 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +148 |
| 2026-07-18 | 工具链 | hook 自动：设计 Skill 编排（152 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +148 |
| 2026-07-18 | 工具链 | hook 自动：设计 Skill 编排（151 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +147 |
| 2026-07-18 | 工具链 | hook 自动：设计 Skill 编排（150 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +146 |
| 2026-07-18 | 工具链 | hook 自动：设计 Skill 编排（149 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +145 |
| 2026-07-18 | 工具链 | hook 自动：设计 Skill 编排（148 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +144 |
| 2026-07-18 | 工具链 | hook 自动：设计 Skill 编排（147 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +143 |
| 2026-07-18 | 工具链 | hook 自动：设计 Skill 编排（145 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +141 |
| 2026-07-18 | 工具链 | hook 自动：设计 Skill 编排（143 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +139 |
| 2026-07-18 | 工具链 | hook 自动：设计 Skill 编排（142 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +138 |
| 2026-07-18 | 工具链 | hook 自动：设计 Skill 编排（141 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +137 |
| 2026-07-18 | 编排器 | 第2轮答完「继续」重弹第1轮澄清 | HasPending 先于已答轮次；ClarificationAnswered 未按 stage/轮次合并 | 重排续跑；pendingRound>answeredCount 才 replay | `澄清轮次回退`, `stale pending`, `ClarificationAnswered` |
| 2026-07-18 | 空指针 | Round2 写回骨架后编排器崩溃 | ExtractRequirementText(null)；Requirement 片段尚未投影 | 可空安全 + 事件/骨架 summary 兜底 | `NullReferenceException`, `Requirement fragment` |
| 2026-07-18 | 前端 UX | 门控通过后误弹 IR-0 骨架审阅 | needsConfirmation 见 skeleton draft；骨架审阅应 PM 内部自动 Stabilize | gatePassed 后隐藏 IrSkeletonConfirmCard | `骨架审阅`, `IR-0`, `gatePassed` |
| 2026-07-18 | 前端 | 下载提示「未返回正式渲染版」 | API 返回 PascalCase `Rendered`；前端只读 `rendered` | unwrapStudioApi + spec-content 统一下载 | `Rendered`, `PascalCase` |
| 2026-07-18 | 门控 | 上传文档 GATE_JSON_ERR 0分 | Prompt 含 `true或false` 非法 JSON；ExtractJson 弱 | LlmJsonFixer + 合法示例 + 重试 | `GATE_JSON_ERR`, `SemanticFitness` |
| 2026-07-18 | 交付物 | 02 可能是 PM raw 文本 | 步骤④曾 silent 回退 raw | requireFormal + 封面/CTA 校验 | `02-requirement-spec`, `RequirementDocumentRenderer` |
| 2026-07-18 | 前端 | 预览弹窗不打开 | `v-model:open` 误用于 Ant Design Vue 3.2 | 改为 `v-model:visible` | `IrRequirementSpecConfirmCard` |
| 2026-07-18 | 工具链 | start-dev.ps1 卡死 [1/8] 无输出 | WMI 挂起 + Get-CimInstance 无超时 | `-OperationTimeoutSec 10` → CimException → name-only fallback | `WMI 挂起`, `Get-CimInstance`, `start-dev 卡死` |
| 2026-07-18 | 工具链 | Bash 下 jnpf-api.mjs 全部 404 | Git Bash 把 `/api/...` 参数改写成 `C:/Program Files/Git/api/...` | `MSYS_NO_PATHCONV=1` 或用 PowerShell | `MSYS_NO_PATHCONV`, `路径转换` |

### M036 | 「继续」澄清轮次回退：重播过期 pending 题

- **症状**：第2轮已作答并说「继续」，系统弹出「第 1 轮澄清题等待作答」
- **根因**：续跑判据先走 HasPendingClarification 重推 in-progress 片段（可能是第1轮 stale 题），后判已答轮次；CountClarificationAnswered 未按 requirement stage 合并多轮 ClarificationAnswered
- **修复**：LoadRequirementClarificationAnswerStateAsync 合并全部轮次；仅 pendingRound > answeredCount 才 ReturnPending；「继续」优先续跑步骤③
- **铁律**：澄清续跑 MUST 比较 ClarificationSet.round 与已答轮次，禁止 replay 已答轮
- **日期**：2026-07-18 | **关键词**：`澄清轮次`, `继续`, `stale pending`, `ClarificationAnswered`

### M037 | Requirement 片段未投影时 ExtractRequirementText 空指针

- **症状**：日志「Round 2 PM 完善已写回骨架」后立即 RequirementAnalysis 编排器失败 NullReferenceException
- **根因**：续跑步骤③时 snapshot 无 Requirement stable/draft 片段，ExtractRequirementText 未判 null；?? ResolveUserRequirementAsync 来不及执行
- **修复**：ExtractRequirementText(IrSnapshotFragment?) 可空；ResolveEnhancedRequirementTextAsync 链式兜底（fragment→事件→骨架 summary→上下文）
- **铁律**：IR 续跑判据不得假设 snapshot.Find(Requirement) 非空；事件流与骨架 summary 为合法兜底源
- **日期**：2026-07-18 | **关键词**：`NullReferenceException`, `Requirement fragment`, `IrProjectionEngine`

### M038 | 门控通过后仍向用户展示 IR-0 骨架审阅

- **症状**：编排器崩溃或骨架 patch 后，对话区出现「IR-0 骨架审阅 · 待确认」
- **根因**：usePmSkill.needsConfirmation 在 skeleton stabilityState=draft 时为 true；新 PM 流程内编排器应自动 Stabilize，用户不参与 HITL
- **修复**：AiChatPanel showSkeletonConfirm 增加 !gatePassed 条件
- **铁律**：PM 新 4 步流程中骨架确认是编排器内部动作，禁止要求用户 confirm-skeleton
- **日期**：2026-07-18 | **关键词**：`IR-0`, `骨架审阅`, `gatePassed`, `StabilizeSkeletonAsync`

### M039 | 说明书预览弹窗 v-model:open 无效

- **症状**：点击「预览全文」无反应
- **根因**：Ant Design Vue 3.2 Modal 使用 `visible` 非 `open`
- **修复**：IrRequirementSpecConfirmCard 改为 `v-model:visible`
- **日期**：2026-07-18

### M040 | 02 落盘 raw PM 文本非正式渲染版

- **症状**：下载/预览内容是 PM 草稿，非九步+附录正式说明书
- **根因**：步骤④曾直接落盘 specText；refresh 无 RequirementSpecRendered 门禁
- **修复**：BuildConfirmSpecMarkdownAsync(requireFormal) + spec-content/refresh-spec API
- **日期**：2026-07-18

### M041 | 门控 GATE_JSON_ERR 误报需求不合格

- **症状**：上传文档 → 0 分 + 评估格式异常
- **根因**：SemanticFitness Prompt 含非法 JSON 占位；ExtractJson 未用 LlmJsonFixer
- **修复**：合法 JSON 示例 + LlmJsonFixer + 重试 + 输入截断
- **日期**：2026-07-18

### M042 | 下载「服务端未返回正式渲染版」

- **症状**：refresh-spec 成功但前端报未返回正式版
- **根因**：JNPF API PascalCase `Rendered`；前端只判断 `rendered`
- **修复**：unwrapStudioApi 双兼容；下载改走 spec-content
- **日期**：2026-07-18

### M043 | start-dev.ps1 卡死在 [1/8]：WMI 查询无超时保护

- **症状**：start-dev.ps1 输出 `[1/8] Cleaning...` 后无任何反应（>3 分钟）
- **根因**：Layer 2 僵尸扫描 `Get-CimInstance Win32_Process` 在 WMI provider 挂起时无限阻塞；脚本对 CIM **报错**有 try/catch fallback，但对 CIM **挂起**无超时（Get-CimInstance 默认不超时）。对照数据：netstat 91ms 正常，CIM >3min 不返回
- **修复**：`Get-CimInstance -OperationTimeoutSec 10`（实测挂起时 5.1s 抛 CimException → 走已有 name-only fallback）
- **铁律**：脚本内任何 WMI/CIM/外部 IPC 查询 MUST 带操作超时；「服务 Running」≠「查询会返回」
- **日期**：2026-07-18 | **关键词**：`Get-CimInstance`, `WMI 挂起`, `OperationTimeoutSec`, `start-dev 卡死`

### M044 | Git Bash 路径转换把 `/api/...` 参数改写成 Windows 路径 → 404

- **症状**：`node scripts/jnpf-api.mjs GET /api/oauth/CurrentUser` 在 Bash 工具下返回 404 空体；同一函数 Node 内直调 200
- **根因**：MSYS/Git Bash 自动路径转换把 argv 中 `/api/oauth/CurrentUser` 改写为 `C:/Program Files/Git/api/oauth/CurrentUser`，实际请求了不存在的 URL；与后端/token 无关
- **修复**：Bash 下加 `MSYS_NO_PATHCONV=1` 前缀，或改用 PowerShell 运行 jnpf-api.mjs（项目主 shell，无此问题）
- **铁律**：Git Bash 里给 CLI 传 `/xxx` 开头的非文件参数 MUST 加 `MSYS_NO_PATHCONV=1`；404+空体先查实际发出的 URL，再查服务端
- **日期**：2026-07-18 | **关键词**：`MSYS_NO_PATHCONV`, `Git Bash 路径转换`, `jnpf-api 404`

---


### M045 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 141 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 20260718144858
- **日期**：2026-07-18 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +137`

### M046 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 142 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 20260718144904
- **日期**：2026-07-18 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +138`

### M047 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 143 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 167fac99-d1fe-4012-a00a-554e1aac6c0b
- **日期**：2026-07-18 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +139`

### M048 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 145 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 20260718145147
- **日期**：2026-07-18 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +141`

### M049 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 147 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 20260718145204
- **日期**：2026-07-18 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +143`

### M050 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 148 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 167fac99-d1fe-4012-a00a-554e1aac6c0b
- **日期**：2026-07-18 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +144`

### M051 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 149 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 167fac99-d1fe-4012-a00a-554e1aac6c0b
- **日期**：2026-07-18 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +145`

### M052 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 150 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 167fac99-d1fe-4012-a00a-554e1aac6c0b
- **日期**：2026-07-18 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +146`

### M053 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 151 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 20260718145418
- **日期**：2026-07-18 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +147`

### M054 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 152 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 20260718145427
- **日期**：2026-07-18 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +148`

### M055 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 152 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 20260718145428
- **日期**：2026-07-18 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +148`

### M056 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 154 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 167fac99-d1fe-4012-a00a-554e1aac6c0b
- **日期**：2026-07-18 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +150`

### M057 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 155 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 167fac99-d1fe-4012-a00a-554e1aac6c0b
- **日期**：2026-07-18 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +151`

### M058 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 156 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 167fac99-d1fe-4012-a00a-554e1aac6c0b
- **日期**：2026-07-18 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +152`

### M059 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 157 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 167fac99-d1fe-4012-a00a-554e1aac6c0b
- **日期**：2026-07-18 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +153`

## 2026-07-19

| 日期 | 类别 | 症状 | 根因 | 修复 | 关键词 |
|------|------|------|------|------|--------|
| 2026-07-19 | 工具链 | hook 自动：设计 Skill 编排（165 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +161 |
| 2026-07-19 | 工具链 | hook 自动：设计 Skill 编排（163 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +159 |
| 2026-07-19 | 工具链 | hook 自动：设计 Skill 编排（162 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +158 |
| 2026-07-19 | 工具链 | hook 自动：设计 Skill 编排（161 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +157 |
| 2026-07-19 | 工具链 | hook 自动：设计 Skill 编排（160 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +156 |
| 2026-07-19 | 工具链 | hook 自动：设计 Skill 编排（159 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +155 |
| 2026-07-19 | 工具链 | hook 自动：设计 Skill 编排（158 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +154 |

### M060 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 158 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: d9fb7ff6-ca1c-4379-9c50-b0312a486446
- **日期**：2026-07-19 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +154`

### M061 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 159 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 72dcf581-5f3a-461a-a110-b8a7f784092f
- **日期**：2026-07-19 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +155`

### M062 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 160 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 061bdc77-a564-4b5c-b2a8-af54e25edf4d
- **日期**：2026-07-19 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +156`

### M063 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 161 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 0e398890-36e7-4eed-97df-6fe6dc4d5b27
- **日期**：2026-07-19 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +157`

### M064 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 162 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: fed47cdd-c1bf-40bb-95c9-1fbe1e254553
- **日期**：2026-07-19 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +158`

### M065 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 163 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: ef3d3ee0-4639-4584-828d-1680dd326602
- **日期**：2026-07-19 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +159`

### M066 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 165 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 340ba3fc-c099-4383-bbfc-f64e30bd0523
- **日期**：2026-07-19 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +161`

## 2026-08-06

| 日期 | 类别 | 症状 | 根因 | 修复 | 关键词 |
|------|------|------|------|------|--------|
| 2026-08-06 | 工具链 | hook 自动：设计 Skill 编排（181 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +177 |
| 2026-08-06 | 工具链 | hook 自动：设计 Skill 编排（180 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +176 |
| 2026-08-06 | 工具链 | hook 自动：设计 Skill 编排（179 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +175 |
| 2026-08-06 | 工具链 | hook 自动：设计 Skill 编排（175 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +171 |
| 2026-08-06 | 工具链 | hook 自动：设计 Skill 编排（172 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +168 |
| 2026-08-06 | 工具链 | hook 自动：设计 Skill 编排（172 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +168 |
| 2026-08-06 | 工具链 | hook 自动：设计 Skill 编排（170 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +166 |
| 2026-08-06 | 工具链 | hook 自动：设计 Skill 编排（169 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +165 |
| 2026-08-06 | 工具链 | hook 自动：设计 Skill 编排（168 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +164 |
| 2026-08-06 | 工具链 | hook 自动：设计 Skill 编排（167 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +163 |
| 2026-08-06 | 工具链 | hook 自动：设计 Skill 编排（166 文件） | 见 session-digest | 见 AUTO summary / digest | knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +162 |

### M067 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 166 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 4ea6c159-9f34-4df9-af9a-ab4378e89c78
- **日期**：2026-08-06 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +162`

### M068 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 167 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 4ea6c159-9f34-4df9-af9a-ab4378e89c78
- **日期**：2026-08-06 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +163`

### M069 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 168 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 4ea6c159-9f34-4df9-af9a-ab4378e89c78
- **日期**：2026-08-06 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +164`

### M070 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 169 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 4ea6c159-9f34-4df9-af9a-ab4378e89c78
- **日期**：2026-08-06 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +165`

### M071 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 170 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 4ea6c159-9f34-4df9-af9a-ab4378e89c78
- **日期**：2026-08-06 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +166`

### M072 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 172 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 4ea6c159-9f34-4df9-af9a-ab4378e89c78
- **日期**：2026-08-06 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +168`

### M073 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 172 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 4ea6c159-9f34-4df9-af9a-ab4378e89c78
- **日期**：2026-08-06 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +168`

### M074 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 175 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 4ea6c159-9f34-4df9-af9a-ab4378e89c78
- **日期**：2026-08-06 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +171`

### M075 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 179 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 4ea6c159-9f34-4df9-af9a-ab4378e89c78
- **日期**：2026-08-06 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +175`

### M076 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 180 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 4ea6c159-9f34-4df9-af9a-ab4378e89c78
- **日期**：2026-08-06 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +176`

### M077 | 设计 Skill 编排（hook 自动归档）

- **症状**：stop hook 检测到 181 个代码文件变更
- **根因**：机器归档快照（语义根因待人工可选补全）
- **修复**：见 `.claude/memory/session-digest/latest.json` 与 AUTO summary
- **变更**：`.ai-memory/knowledge-graph.json`, `.claude/.session-init-lock.json`, `.claude/.skill-load-state.json`, `.cursor/hooks.json`, `.cursor/hooks/episodic-session-start.mjs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Dto/Skills/SkillDtos.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Entity/AiProjectEntity.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant.Entitys/Ir/IrEventTypes.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`, `backend/modularity/inteAssistant/JNPF.InteAssistant/Infrastructure/Background/BackgroundTaskRunner.cs` …
- **hook-auto-archive**: 4ea6c159-9f34-4df9-af9a-ab4378e89c78
- **日期**：2026-08-06 | **关键词**：`knowledge-graph.json, .session-init-lock.json, .skill-load-state.json, hooks.json +177`
## 一、方法论（最贵——每条都导致 ≥1 小时浪费）

### M035 | 未钉死业务功能就开发/验收：用纠偏项冒充四大支柱①（方向性错误）

- **症状（2026-07-11 · SG2 纠偏）**：用户要求对照四大支柱把 SG2 做扎实。我把「02 表头非空」「DDD 待确认节」当成这一节点的业务核心，并据此勾选支柱 **① 业务符合**，向用户提交「SG2 待审批」。用户当场指出：**① 的第一要点是业务功能实现的正确和完整**——我连本节点业务核心都没抓住。
- **根因（方向性）**：
  1. **开发目标未明确就动手**——未先用一句话说清 SG2 业务能力本体（25 §1.2/§1.3：三轮需求分析 → Round3 Finalize → 可交开发的说明书 + ai_entity_field + sa_* + 下游契约），却把局部缺陷修复当成「做完了业务」。
  2. **用手段/表象顶替目的**——表头、待确认节、xUnit 绿、test:api 绿都是 **质量/支撑**，不是 **① 业务功能本身**；把 E1/E2 纠偏项升格为支柱①过关证据。
  3. **脱离已写入项目的铁律**——业务优先铁律（开工前三问）与 30 号 §0.6 ① 已明文规定「做对的事」；我口头引用四支柱，实际验收仍滑向「文件好看 + 测试绿」。
- **正确工作方向与核心（反复告诫）**：
  1. **任何 SG/阶段开工前 MUST 先回答**：本节点要交付的**业务功能**是什么？用户在界面上完成什么操作？完整业务产物清单是什么？（对照该阶段设计文档，如 SG2→25/27/28，而非只对照自己刚改的 bug 列表）
  2. **四大支柱① 不可被顶替**：① = 业务功能正确完整；②数据；③清旧；④单测。缺①或用②③④冒充① = **假绿，项目全毁**。
  3. **纠偏项 ≠ 节点完成**：修空壳表头/待确认/假绿，只证明「去掉了一个假绿点」，**绝不**等于「本阶段业务功能已正确完整实现」。
  4. **每轮自检**：准备勾选①或声称「本节点可审批」前，强制对照设计文档业务目标清单逐条有证据；说不清业务能力本体 → **禁止编码、禁止勾选、禁止进入下一阶段**。
- **关联铁律**：`.claude/rules/business-first-iron-law.md` · `.cursor/rules/business-first-iron-law.mdc` · 30号计划 §0.6 支柱① · 实现完整性铁律（禁令五）
- **日期**：2026-07-11 | **关键词**：`四大支柱①`, `业务功能正确完整`, `开发目标不明`, `脱离业务`, `纠偏冒充完成`, `SG2`, `Business First`

### M033 | 工具确认死循环 + 搜索不穷尽：丢失业务目标

### M034 | SaMaterializer 写入值与 DB 列类型/CHECK 不一致
- **症状**：Analyst Round3 物化失败 — `CK_sa_scope_status`（写 `COMPILED`）、`nvarchar→bit`（校验列写 `"PASS"` 字符串）、SqlSugar 匿名类型 Insertable（`class,new()`）
- **根因**：代码假设列语义（COMPILED / 详细 PASS 文案）与真实 DDL（CHECK=PASS|FAIL|PENDING；校验列为 BIT）脱节；QualityScore/Assumptions 用匿名类型 Insertable
- **修复**：`validation_status='PASS'`；BIT 列改 `bool`；`SaQualityScoreRow`/`SaAssumptionRow` 具体类型；`EntityDesignRepository` 避免 Storageable 匿名 WhereColumns
- **铁律**：写 sa_* / SqlSugar Insertable 前先查 INFORMATION_SCHEMA 列类型与 CHECK；禁止匿名类型 Insertable

---

- **症状**：用户要求推进 26/27/28 需求分析子链实现，我却在 serena/知识图谱工具确认上消耗了十几个轮次，反复问用户、每次只搜一个配置位置就回头问，业务目标（写代码）一行没动
- **根因（三重）**：
  1. **工具确认变成拖延实现的借口**——每被回答一次，就追问更细的工具细节，形成"问→答→再问"死循环，回避了真正的编码任务
  2. **搜索不穷尽**——每轮只查一个配置文件，查不到就问用户，而非一次 bash 把所有可能位置（~/.claude.json / .zcode/v2/config.json / 项目 .mcp.json / .cursor/mcp.json / Claude Desktop / VS Code settings）全 grep 完。正确做法：**第一次就该 `for f in 所有位置; grep; done` 一把扫尽**
  3. **丢失业务优先级**——工具是手段不是目的。serena 配置完成、重启即可加载；知识图谱查不到就该直接说明并继续推进，而不是让工具问题阻塞整个任务
- **规则（三条）**：
  1. **搜索一把扫尽**：找配置/符号/文件时，第一次就用一条命令覆盖所有合理位置，不要"查一个问一次"
  2. **工具不阻断业务**：工具查不到 → 明确报告现状 → 继续推进核心任务，禁止用工具确认拖延编码
  3. **每轮自检**：回答用户前问自己"这轮有推进业务目标吗？"——如果一整轮都在问工具/查配置而无任何代码产出，说明已偏离
- **日期**：2026-07-09 | **关键词**：`工具死循环`, `搜索不穷尽`, `丢失业务目标`, `一把扫尽`, `MCP配置`

### M030 | 验证不完整：只测"修的那条路"

- **症状**：Q3-security 修复后只验了缺失路径，正向/漏洞路径被架构师追问才补
- **根因**：本能只验自己改过的那条路径，忽略防御代码影响多条路径
- **规则**：改了 if/switch/guard → 所有分支全测
- **日期**：2026-06-26 | **关键词**：`三路径`, `正向/异常/缺失`

### M032 | import type 导入运行时值 → ReferenceError

- **症状**：IrObservatoryPanel IR-3 Tab 不渲染，Console 报 `ReferenceError: IR3_RELEVANT_EVENT_TYPES is not defined`
- **根因**：`useIrObservatory.ts:11` 将 `IR3_RELEVANT_EVENT_TYPES` 和 `IR3_FRAGMENT_TYPES` 放在 `import type` 块中。`import type` 在编译时被擦除，运行时无法访问这些常量。而代码在 `new Set(IR3_RELEVANT_EVENT_TYPES)` 和 `IR3_FRAGMENT_TYPES.includes()` 中将它们作为运行时值使用
- **修复**：将两个常量从 `import type { ... }` 移到独立的 `import { ... }` 语句
- **日期**：2026-07-04 | **关键词**：`import type`, `ReferenceError`, `运行时值`, `类型擦除`, `Vite`

### M031 | Prompt 审计：凭感觉不逐条对照

- **症状**：论断纪律改版后用户亲自对照 spec 发现缺了两条核心规则
- **根因**：把 prompt 修改当"写文章"而非"改代码"，没有 diff 和回测
- **规则**：改完 MUST 逐条对照原始 spec，标注每条的"已覆盖/已删除/已修改"
- **日期**：2026-06-26 | **关键词**：`spec审计`, `逐条对照`, `prompt工程`

### M011 | 源码分析替代不了网络抓包

- **症状**：SSE 源码正确但仍无 AI 回复，花 4 小时反复改代码无效
- **根因**：一直看源码猜测，从未抓网络响应体。最终 Playwright `page.on('response')` 发现 `/events` 返回 `{"code":600,"msg":"登录过期"}`——HTTP 层就失败了
- **规则**：前端无响应 → 先抓包看实际返回，再分析源码
- **日期**：2026-06-21 | **关键词**：`网络抓包`, `Playwright`, `SSE`, `调试方法`

### M009 | 跳过 brainstorming 直接编码

- **症状**：多次小修复直接 Edit→Build→Claim，未走 S1
- **根因**："太简单不需要设计"的错觉，违反 S1 铁律
- **规则**：编码前 MUST `superpowers:brainstorming`，即使输出只有 3 行
- **日期**：2026-06-21 | **关键词**：`brainstorming`, `S1`, `流程`

### M010 | 声称完成但未执行 Gate Function

- **症状**：多次声称"✅ 完成"，但未执行 5 步验证
- **根因**：把"编译 0 error"当作完整验证，缺少 E2E 证据
- **规则**：声称完成前 MUST `superpowers:verification-before-completion`
- **日期**：2026-06-21 | **关键词**：`Gate Function`, `S2`, `E2E`

### M024 | 跳过 Phase 抬头声明

- **症状**：SA 门控施工全程未输出 Phase 抬头
- **根因**：施工手册极详尽 → 误判"设计已定直接执行"。手册是输入，流程是纪律，不冲突
- **规则**：无论输入多详细，逐 Phase 输出抬头声明
- **日期**：2026-06-23 | **关键词**：`Phase抬头`, `流程违规`, `七阶段流水线`

### M008 | 删除文件前未对比内容

- **症状**：直接删除 4 个用户级 Hook 文件，用户质疑
- **根因**：跳过对比步骤，假设同名 = 功能重叠
- **规则**：删除前 MUST Read → 对比 → 输出分析 → 获确认
- **日期**：2026-06-20 | **关键词**：`文件删除`, `对比`

---

## 二、C# 语言陷阱

### 模式：API 名记错 / 语法边界不清

| 编号 | 症状 | 根因 | 修复 | 日期 | 关键词 |
|------|------|------|------|------|--------|
| M001 | `volatile long` CS0677 | C# 不允许 volatile 修饰 64 位值类型 | `Volatile.Read/Write` | 06-20 | `volatile`, `CS0677` |
| M021 | `SingleProducer` 不存在 | .NET 8 属性名是 `SingleWriter` | 改用 `SingleWriter` | 06-22 | `BoundedChannelOptions`, `Channel` |
| M022 | using 写在方法体内 | C# 只允许文件级/namespace 级 using | 完全限定名替代 | 06-22 | `using directive`, `Program.cs` |
| M023 | `??` 类型不匹配 CS0019 | `ReadOnlyCollection<string>` vs `string[]` | 三元表达式 + 显式转型 | 06-22 | `??`, `类型不匹配` |
| M025 | `$"""` + JSON 大括号 CS9006 | `{{` 转义链超限 | `$$"""` 双美元 | 06-23 | `raw string`, `$$`, `CS9006` |
| M026 | `System.Text.Json` 不认字符串枚举 | 默认按数值反序列化枚举 | `JsonStringEnumConverter` | 06-23 | `enum`, `JsonException` |
| M028 | `List<T> = new()` 使 null 检查失效 | 反序列化用默认值而非 null | 额外检测 `Count == 0` | 06-23 | `record`, `init`, `default` |
| M029 | `new` 关键字不能替代 virtual | `new` 是隐藏不是重写，CLR 分派到基类 | 构造函数注入 Fake | 06-23 | `new vs virtual`, `vtable` |

---

## 三、JNPF 框架专属陷阱

### 模式：框架约定被 .NET 直觉覆盖

| 编号 | 症状 | 根因 | 修复 | 日期 | 关键词 |
|------|------|------|------|------|--------|
| M002 | throw `UnauthorizedAccessException` → HTTP 500 | JNPF 统一响应要求 `Oops.Bah()` | `throw Oops.Bah("msg")` | 06-20 | `Oops.Bah`, `RESTfulResult` |
| M004 | `res.pipelineId` 为 undefined | JNPF 包装为 `{ code, data: {...} }` | `const data = res?.data \|\| res` | 06-20 | `RESTfulResult`, `data 包装` |
| M020 | 更新后 CreateTime 被重置 | Mapster `Adapt()` 全量映射覆盖审计字段 | 先查原始实体再 Adapt | 06-21 | `Mapster`, `Adapt`, `Trap 2` |
| M005 | PipelineEntity 落库无 TenantId | 只传给 engine 未写入 entity 初始化器 | 显式赋值 `TenantId` | 06-20 | `TenantId`, `多租户` |
| M006 | SSE /events 无租户校验 | 直接从 `_sseChannels` 取 Channel | 校验 pipeline 归属当前租户 | 06-20 | `SSE`, `租户隔离` |
| M019 | FormData 上传 403 | axios 拦截器不处理 FormData，缺 `X-Tenant-Id` | 显式携带 `X-Tenant-Id` | 06-21 | `FormData`, `X-Tenant-Id` |

---

## 四、前端陷阱

### 模式：axios/Vite 代理链路断裂

| 编号 | 症状 | 根因 | 修复 | 日期 | 关键词 |
|------|------|------|------|------|--------|
| M003 | `fetch()` POST 未达后端 | fetch 不走 axios 拦截器链（baseURL/token/代理） | 业务 POST 改用 `defHttp.post()` | 06-20 | `fetch`, `defHttp`, `Vite` |
| M007 | buildFetchSseUrl + fetch 仍失败 | 虽然 URL 对了，但 fetch 仍不走 axios | Step1 用 defHttp, Step2 用 fetch | 06-20 | `buildFetchSseUrl`, `SSE 两步` |
| M012 | `Bearer Bearer` 双重前缀 | `getToken()` 自带 "Bearer "，代码又拼接一次 | `token.startsWith('Bearer ') ? token : \`Bearer ${token}\`` | 06-21 | `getToken`, `双重前缀` |
| M017 | 28 处 `as string` 类型断言 | `getToken()` 未标注返回类型 | 加 `string \| null` 返回类型 | 06-21 | `as string`, `TypeScript` |
| M018 | 纯附件消息被 handleSend 守卫拦截 | `if (!content) return` 早返回，附件代码不可达 | 有附件时即使无文字也继续 | 06-21 | `handleSend`, `附件`, `早返回` |

---

## 五、边界条件 / 配置

| 编号 | 症状 | 根因 | 修复 | 日期 | 关键词 |
|------|------|------|------|------|--------|
| M014 | AttachmentProcessor 处理音视频 | 未做格式过滤 | `IsAudioVideoFile()` 跳过 | 06-21 | `音视频`, `格式过滤` |
| M015 | 文件格式白名单过严 (28种) | 保守策略，未考虑文档分析场景 | 扩展到 58 种 | 06-21 | `AllowUploadFileType`, `白名单` |
| M016 | Markdown 被 D1800 拦截 | 白名单逐一列举遗漏 .md | 补充 md 扩展名 | 06-21 | `Markdown`, `白名单遗漏` |
| M013 | Pipeline 步骤间重复下载图片 | 步骤间无数据共享机制 | `ConcurrentDictionary` 缓存 | 06-21 | `重复下载`, `缓存` |

---

## 六、测试陷阱

| 编号 | 症状 | 根因 | 修复 | 日期 | 关键词 |
|------|------|------|------|------|--------|
| M027 | Moq mock 不命中，测试假绿 | `CancellationToken` + 重载 + Moq 匹配失效 | Fake 显式接口实现替代 Moq | 06-23 | `Moq`, `CancellationToken`, `Fake` |
| M034 | 测试运行在过期二进制上导致假失败 | 上次会话修改的测试文件未重新编译，`dotnet test --no-build` 使用旧 DLL，T14 断言 Status=failed 实为过期二进制 | 验证前必须 `dotnet build` 确认最新；DLL 被 testhost 锁定时 `taskkill //f //im dotnet.exe` 再重建 | 07-10 | `stale binary`, `--no-build`, `testhost 锁定`, `重建验证` |

---

## 七、架构 / 研究（无代码缺陷，仅记录决策上下文）

| 编号 | 内容 | 结论 | 日期 |
|------|------|------|------|
| M013-R | Open Code Review 对 JNPF 适用性评估 | OCR 不含 C# 规则，不能替代 code-reviewer 子代理 | 06-24 |
| M014-R | CodeGraph 部署 + 21 Hook 审计 | 发现 guard-finish 内存泄漏 + CodeGraph 无限递归 + 规则分层协议 | 06-25 |
| M015-R | V3.0 涅槃重构 | 自建状态机 → Claude Code 原生 Agent；7 soul + 3 脚本 + 7 Hook | 06-26 |

---

## 编号映射（旧→新）

```
旧 M013 (后端Pipeline)  → M013 (保留)
旧 M013 (研究OCR)      → M013-R
旧 M014 (后端格式过滤) → M014 (保留)
旧 M014 (基础设施审计)  → M014-R
旧 M015 (配置白名单)   → M015 (保留)
旧 M015 (V3.0架构)     → M015-R
其余编号未变。
```

---

## 错误类型分布

```
方法论:   7 ███████
C# 语法:  8 ████████
JNPF 框架: 6 ██████
前端:     5 █████
边界/配置: 4 ████
测试:     2 ██
```

> **解读**：C# 语法错误虽然最多，但都是"查文档即可"的一次性错误。方法论错误只占 7/32（22%），但每条都导致 ≥1 小时浪费——**方法论是 ROI 最高的改进方向**。
