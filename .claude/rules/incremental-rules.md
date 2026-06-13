# 增量规则 — 跨会话记忆、健康验证、安全知识库

> **触发条件**：当涉及增量开发、模块迭代、版本升级、安全相关任务、会话结束前，Read 本文件。
> **主文件引用**：CLAUDE.md § 增量规则（1 句摘要 + 触发条件）

---

## 跨会话记忆使用规范

- 每次会话开始时，MUST 读取项目内 `.claude/memory/` 下的文件了解团队共享上下文
- 同时参考 auto-memory（系统自动维护的个人记忆）
- 两者分工：`.claude/memory/` = 团队共享知识（提交到 Git），auto-memory = AI 个人笔记
- 每次会话结束前，MUST 将以下内容写入 `.claude/memory/`：
  - 重要技术决策 → `decisions.md`
  - 未解决的问题 → `pending-issues.md`
  - 踩坑记录 → `lessons-learned.md`

---

## 禁止推脱补充

> 补充 Law 1 中未覆盖的具体行为规范。

当发现错误但无法在当前会话快速修复时（≥ 15 分钟），MUST：
1. 明确告知人类："我发现了一个需要单独处理的问题"
2. 给出具体代码级修复方案（不是"建议排查"）
3. 写入 `.claude/memory/pending-issues.md`，包含：问题描述、复现步骤、修复方案、影响评估
4. 绝对不允许发现错误后沉默或跳过

---

## 项目健康验证

> 补充 R5，仅验证已启用且被修改的项目。

每次代码修改后，被修改的项目 MUST 能编译通过：
- 前端（jnpf-web-vue3）：`vue-tsc --noEmit` 通过
- 后端（已启用的 Entry）：`dotnet build` 通过
- DataV（jnpf-web-datascreen）：如被修改则需验证
- UniApp（jnpf-app-vue3）：如被修改则需验证
- **OA（禁用）、IoT/MES（未创建）：不验证，与 R5 一致**

---

## 安全知识库

- 处理安全相关任务时，MUST 先查阅 `.claude/knowledge/` 下的相关文件
- 不确定时明确说"我不确定，请安全团队审核"

---

## 前端 UI 品味提升规范

> 已安装 5 个前端设计技能（frontend-design / ui-ux-pro-max / taste-skill / frontend-design-pro / bencium-controlled-ux-designer），通过 `jnpf-ui-enhance` 桥接技能在框架内使用。

WHEN 修改自定义页面（非 .vm 生成页面）的视觉样式 => Read `.claude/skills/jnpf-ui-enhance/SKILL.md`

**使用原则：**
- **组件骨架不动**：BasicTable / BasicForm / BasicPopup / jnpf-content-wrapper 的用法不可更改
- **皮肤层可提升**：颜色、间距、阴影、字体层级、hover 效果、加载动画
- **生成页面禁止改**：.vm 模板输出的页面不属于增强范围
- **渐进式增强**：默认用 Level 1（微调），用户明确要求时再用 Level 2/3
- **设计技能仅提供方向**：具体实现必须符合 `jnpf-frontend-rules.md` 的组件选择表和 `jnpf-taste-blueprint.md` 的骨架决策树
