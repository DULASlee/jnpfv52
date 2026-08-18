# 前端专科五柜 — 执行快照

> **日期**：2026-08-06（**全量复跑完成**）  
> **决策**：[`design-quality-frontend-tooling-adr.md`](design-quality-frontend-tooling-adr.md)  
> **工程**：`jnpf-web-vue3`  
> **证据**：`.claude/evidence/frontend-ct/` · 汇总 `cabinets-full-run-summary.json`  
> **整改总纲（前端）**：[`../../superpowers/specs/2026-08-06-frontend-quality-remediation-design.md`](../../superpowers/specs/2026-08-06-frontend-quality-remediation-design.md) · [`../../superpowers/plans/2026-08-06-frontend-quality-remediation-plan.md`](../../superpowers/plans/2026-08-06-frontend-quality-remediation-plan.md)  
> **后端专册**：[`../../superpowers/specs/2026-08-06-backend-quality-remediation-design.md`](../../superpowers/specs/2026-08-06-backend-quality-remediation-design.md)

---

## 完成状态（本轮）

| 柜 | 状态 | 范围 | 关键数字 |
|----|------|------|----------|
| 1 架构 | ✅ 全量 | `src` depcruise + Knip files | 违例 **1124**（854 err / 270 warn）；Knip unused **592** |
| 2 复杂度 | ✅ 全量 | SonarJS 全 `src` | **1376** 文件；认知复杂度命中 **126**；Top：`dynamicForm/index.vue` CC **146** |
| 3 组件 | ✅ 全量 | 全部 `.vue` meta + Knip 未用清单 | **787/787** ok；未用 `.vue` **443**（假阳需人工） |
| 4 内存 | ✅ 真测 | fuite ×5 迭代 + heapsnapshot | URL `index.html`；**Leak detected: No**；内存变化 **-87.5 kB** |
| 5 ADR | ✅ 文档 | 选型落盘 | [`design-quality-frontend-tooling-adr.md`](design-quality-frontend-tooling-adr.md) |

---

## 怎么跑

| 柜 | 命令 | 主要产物 |
|----|------|----------|
| 1 架构 | `pnpm quality:arch` / `pnpm quality:knip` | `cab1-depcruise-err.txt` · `cab1-knip.txt` · `cab1-summary.json` |
| 2 复杂度 | `pnpm quality:complexity` | `cab2-sonarjs.json` · `cab2-sonarjs-top.json` |
| 3 组件 | `pnpm quality:components` | `cab3-component-meta.json` · `cab3-unused-vue-full.json` |
| 4 内存 | `pnpm quality:memory:run`（需 `:3100` + Chrome） | `cab4-fuite.json` · `cab4-fuite-summary.json` |
| 5 ADR | （文档） | `design-quality-frontend-tooling-adr.md` |

配置：`.dependency-cruiser.cjs` · `knip.json` · `.eslintrc.complexity.cjs` · `scripts/quality/cab*.cjs`

柜4 说明：裸 `/` 可能 404；场景用 `http://127.0.0.1:3100/index.html` + `cab4-fuite-scenario-def.cjs`（`domcontentloaded` / 自定义 idle，避免 Vite networkidle 超时）。本轮为入口页 reload 泄漏基线，**非**登录后 Studio SSE 深路径。

---

## 整改建议顺序（业务优先）

1. **环枢纽**：`components/Jnpf/index.ts`、router ↔ store ↔ axios（架构柜）  
2. **在线开发 / 流程表单**：`dynamicModel/list/*`、`workFlowForm/dynamicForm`（复杂度柜）  
3. **Studio SSE/聊天**：`AiChatPanel` / `PipelineSSEPanel`（复杂度 + 后续柜4深路径）  
4. **未用组件**：仅对 Knip `.vue` 清单人工确认后删除（组件柜）
