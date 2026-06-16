# Cross-Session Memory

> 跨会话记忆使用规范。触发条件：会话开始 / 会话结束。

---

## 会话开始

每次会话开始时，MUST 读取项目内 `.claude/memory/` 下的文件了解团队共享上下文，同时参考 auto-memory（系统自动维护的个人记忆）。

**两者分工：**
- `.claude/memory/` = 团队共享知识（提交到 Git）
- auto-memory = AI 个人笔记

## 会话结束

每次会话结束前，MUST 将以下内容写入 `.claude/memory/`：

| 内容类型 | 目标文件 |
|---|---|
| 重要技术决策 | `decisions.md` |
| 未解决的问题 | `pending-issues.md` |
| 踩坑记录 | `lessons-learned.md` |

## 安全知识库

处理安全相关任务时，MUST 先查阅 `.claude/knowledge/` 下的相关文件。不确定时明确说"我不确定，请安全团队审核"。
