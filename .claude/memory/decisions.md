# 技术决策记录

> 团队共享，提交到 Git。记录重要技术决策。
> 格式：日期 | 决策 | 理由 | 影响范围

---

## 2026-06-05 | Hooks 基础设施架构决策

**背景**：实施 AI 编程工程化第二阶段 hooks 机制
**决策**：
- hooks 格式采用官方嵌套格式 `{matcher, hooks: [{type, command}]}`，非扁平格式
- settings.json 采用合并策略（保留 env + permissions），非覆盖
- guard-finish 采用增量编译（只编译 JNPF.API.Entry.csproj），非全量 sln 编译
- guard-deps 设置 3 秒熔断机制，防止 npm view 挂起

**理由**：
- 扁平格式被 Claude Code JSON Schema 校验拒绝（实测验证）
- 覆盖 settings.json 会丢失 API 连接配置和权限，导致项目瘫痪
- 全量 sln 编译需要 5-10 分钟，不适合 Stop hook（应 ≤30 秒）
- npm view 在国内网络某些包会挂起 30-120 秒

**影响范围**：`.claude/settings.json`, `.claude/hooks/*.mjs`, `CLAUDE.md`

---

## 2026-06-05 | format-and-lint 动态 node_modules 查找

**背景**：架构师原方案硬编码 `./node_modules/.bin/`，但 JNPF 是多子项目结构
**决策**：从被编辑文件路径向上查找最近的 `node_modules/.bin/` 目录
**理由**：jnpf-web-vue3 和 jnpf-app-vue3 各有独立 node_modules，根目录没有
**影响范围**：`.claude/hooks/format-and-lint.mjs`

---

## 2026-06-05 | guard-finish 超时降级策略

**背景**：后端服务运行时 DLL 被锁定，build 会阻塞超时
**决策**：30 秒超时 + ETIMEDOUT 降级为警告（不阻断停止）
**理由**：DLL 锁定是服务运行的正常状态，非代码错误
**影响范围**：`.claude/hooks/guard-finish.mjs`

---

## 2026-06-05 | guard-bash 危险命令拦截器设计

**背景**：防止 AI 误执行高危命令导致数据丢失
**决策**：PreToolUse Bash hook，exit 2 硬阻断，覆盖 Windows/Linux/DB/Git/安全 5 类
**理由**：
- Windows 专用规则：rmdir /s /q、del /s /q、Remove-Item -Recurse -Force
- exit 2 = 不重试，比 JSON block 更可靠
- 16 条正则规则，3 秒超时
**影响范围**：`.claude/hooks/guard-bash.mjs`, `.claude/settings.json`

---

## 2026-06-05 | collect-summary 会话变更摘要

**背景**：AI 会话结束时自动收集变更摘要，便于人类审阅
**决策**：Stop hook，收集未提交变更（git diff），保存为 Markdown
**理由**：
- 使用 `git diff --name-only`（未提交变更）而非 `git diff --name-only HEAD~1 HEAD`（已提交变更）
- 静默失败不阻断停止流程
- 分类：后端/前端/配置/Hooks/文档/其他
**影响范围**：`.claude/hooks/collect-summary.mjs`, `.claude/memory/session-summaries/`

---

## 2026-06-05 | Memory 双路径分工

**背景**：存在两个记忆目录
**决策**：
- `C:\Users\admin\.claude\projects\D--JNPF-v52\memory\` = Claude auto-memory（AI 个人笔记，系统自动维护）
- `项目根/.claude/memory/` = 团队共享知识（提交到 Git，人工可审阅）

**理由**：两者职责不同，不应合并
**影响范围**：CLAUDE.md 跨会话记忆使用规范
## 2026-06-14 | Phase 3 Week 1 完成

**F-7.1-7.3 交付：**
- Alova 请求封装 (request.ts + httpEnum.ts)
- createEntityApi 通用 CRUD + createEntityStore Pinia 模板
- 3 个 EJS 页面模板 (list/form/detail)，全部 wot-design-uni

**后端 CreateScope 清零：**
- Sprint 4-5: 11 处 + Day 2: 2 处构造函数泄漏 + Day 3: 4 处 IServiceProvider→IServiceScopeFactory
- App.GetService 业务层已清零
- framework 层 25 处判定为框架基础设施，不消化

**硬性约束：** 零 eval/Function，全部 @jnpf-generated 标记

**提交：** 371956e

---

## 2026-08-28 | Skill v6.0 终局判定 + ECC 技能选型

**决策**：
1. **R1 冻结、v6.0 版本 DEFERRED 关闭**：R1 Context Model=PASS&FROZEN，V-5 Anchor Contract Patch 后仍无法稳定获得 exact snippet，按架构师终局裁决 STOP→P1/Execution Capability→DEFERRED；未进第三轮修复。**影响**：SKILL.md/references/生产代码零改动，F-R=0。
2. **A-§4 计数锁定**：定点 grep 免 Scope 但计 Artifact/Depth/Iteration；broad discovery=scope expansion。**影响**：R2/R3 计数口径唯一化，R1 分档表不动。
3. **答案卡码证修正（v2→v3）**：Directory≠Zip（CreateFromDirectory 后目录不再被引用）⇒ "目录打包后可局部清理"为合法 GO 终态。**影响**：GO/STOP/NEED 三态均规则内可推导，非迁就行为。
4. **ECC 仅装 7 技能，三可选不装**：production-audit/agent-architecture-audit/rules-distill/skill-stocktake/skill-scout/prompt-optimizer/dotnet-patterns 装；token-budget-advisor（伪精确）/parallel-execution-optimizer（重复）/strategic-compact（运行时错配）不装。
5. **ECC vault 与 .claude/memory 并存**：前者跨 harness 共享（unreviewed、不入 git），后者项目知识库（随仓库共享）。

**commit**：227adc28
