---
name: writing-skills
description: 创建、修订或验证 .cursor/skills/ 下的 SKILL.md 时使用。技能编写即流程文档的 TDD。
scope: JNPF-v52
---

# Writing Skills — 编写技能

## 核心原则

**写技能 = 对流程文档做 TDD。** 先观察 Agent 无技能时的失败行为，再写技能，再验证 Agent 遵守。

前置：理解 `test-driven-development` 的 RED-GREEN-REFACTOR 循环。

## 技能是什么

- ✅ 可复用的技术模式、工具用法、检查清单
- ❌ 一次性解决问题的叙事

## 本项目技能位置

```
.cursor/skills/<skill-name>/SKILL.md   # 相对于仓库根目录
```

## SKILL.md 格式

```markdown
---
name: skill-name          # 与目录名一致
description: 一句话说明何时触发（Agent 靠此匹配）
scope: JNPF-v52           # 必填；跨项目复用时改为对应项目标识
tech-stack: [dotnet, pnpm] # 可选；含技术栈绑定命令时必填
---

# 标题 — 中文副标题

## 适用场景
...

## 工作流 / checklist
...

## 铁律
...
```

## 编写流程（TDD 映射）

| TDD | 技能编写 |
|-----|----------|
| 写失败测试 | 无技能时跑 Agent，记录其错误/捷径 |
| 写实现 | 撰写 SKILL.md |
| 测试通过 | 有技能时 Agent 遵守 checklist |
| 重构 | 堵漏洞，保持简洁 |

## JNPF 适配要点

- 路径使用仓库根**相对路径**；禁止 `liu202505v2` 或硬编码绝对盘符路径
- 技术栈相关命令（如 `dotnet build`）须在 frontmatter 标注 `scope` / `tech-stack`（见 `using-git-worktrees`）
- 构建命令：`dotnet build`（backend）、`pnpm build`（前端）
- 与 `toolchain-division.mdc` 职责一致：Superpowers 管开发推进，OpenSpec 管知识库
- 中文为主，与现有 `brainstorming`、`executing-plans` 风格一致

## 修订后验证

```powershell
node scripts/verify-toolchain.mjs
```

确认技能目录有 `SKILL.md`、frontmatter 完整、核心技能未缺失。

## 禁止

- ❌ 在技能中驱动 `/opsx:apply` 日常编码
- ❌ 与插件缓存同名但内容冲突而不注明「以项目版为准」
- ❌ description 空泛（「帮助用户」）— 必须写**何时**触发
