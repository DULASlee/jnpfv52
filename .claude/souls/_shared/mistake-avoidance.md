# 错题本避坑（全角色共享 — 编码前强制加载）

> **数据源：** `.claude/memory/mistake-log.md`（31 条历史错误）
> **本文件为角色加载指针。完整错题本见源文件。每次编码前 MUST Grep 关键词。**

## Before You Code（五条铁律——每条背后 ≥2 次实际犯错）

1. **验证三路径** — 改了防御代码 → 正向/异常/缺失全测，不能只测修的那条路（M030）
2. **改 prompt = 改代码** — 改完逐条对照原始 spec 审计，不能凭"感觉对了"（M031）
3. **先抓包再分析源码** — 前端无响应 → Playwright `page.on('response')` → 看实际返回体（M011）
4. **不跳过 brainstorming** — 无论任务多小，MUST 走 S1。输入详尽≠豁免流程（M009, M024）
5. **声称完成 = Gate Function 5 步** — IDENTIFY→RUN→READ→VERIFY→CLAIM，缺一不可（M010）

## 角色避坑速查

| 角色 | 高频错误类型 | 编码前 Grep 关键词 |
|------|-------------|-------------------|
| Architect | [FRAME] 当 [KNOWN]；虚构 JNPF 能力 | `架构`, `V3.0`, `方向修正` |
| Planner | 低估复杂度；忽略模块边界 | `流程`, `Phase`, `brainstorming` |
| Coder | C# 语法陷阱；JNPF 框架约定；前端链路断裂 | `volatile`, `Oops.Bah`, `RESTfulResult`, `Mapster`, `fetch`, `Bearer` |
| Tester | 声称通过但未实际运行；验证不完整 | `三路径`, `Gate Function`, `E2E`, `验证` |
| Reviewer | 谄媚放过 Critical；未检查 Hook 覆盖 | `反谄媚`, `BLOCK`, `hook_audit` |
| Reporter | 美化未完成项；虚构数据 | `post-hoc`, `事后归因`, `编造` |
| Orchestrator | 跳过质量门；降低任务级别 | `流程`, `PHASE_HALT`, `Gate` |

## 错误后协议

发现任何 bug/错误/失败后 MUST：

1. **匹配已有模式** — Grep `mistake-log.md` 搜索当前症状关键词
2. **命中？** → 引用编号（如 `见 M020`），检查 recurrence_count，按已有修复方案处理
3. **新错误？** → 立即追加到 `mistake-log.md`，格式：`M{序号} | {类别} | {一句话症状} | {根因} | {修复} | {关键词}`
4. **编号** — 查 `mistake-log.md` 末尾最大编号 +1

## 错题本位置

```
.claude/memory/mistake-log.md   ← 主文件（31 条，按类别分组）
```
