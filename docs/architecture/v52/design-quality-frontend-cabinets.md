# 前端专科五柜 — 执行快照

> **日期**：2026-08-06  
> **决策**：[`design-quality-frontend-tooling-adr.md`](design-quality-frontend-tooling-adr.md)  
> **工程**：`jnpf-web-vue3`  
> **证据**：`.claude/evidence/frontend-ct/`

---

## 怎么跑

| 柜 | 命令 | 主要产物 |
|----|------|----------|
| 1 架构 | `pnpm quality:arch` / `pnpm quality:knip` | `cab1-depcruise-err.txt` · `cab1-knip.txt` |
| 2 复杂度 | `pnpm quality:complexity` | `cab2-sonarjs.json` · `cab2-sonarjs-top.json` |
| 3 组件 | `pnpm quality:components` | `cab3-component-meta.json` · `cab3-unused-vue-sample.json` |
| 4 内存 | `pnpm quality:memory`（dry）/ `:run` | `cab4-fuite-runbook.json` |
| 5 ADR | （文档） | `design-quality-frontend-tooling-adr.md` |

配置文件：`.dependency-cruiser.cjs` · `knip.json` · `.eslintrc.complexity.cjs` · `scripts/quality/cab*.cjs`

---

## 本轮数字（勿当 CI 红线）

| 指标 | 值 | 解读 |
|------|----|------|
| depcruise 违例 | 1174（854 err / 320 warn） | 环依赖为主；先拆枢纽 barrel |
| 环涉及模块 | 314 unique | Top：`Jnpf/index.ts`、路由、`dynamicModel`、流程解析、Studio 聊天 |
| Knip unused files | 592 | **动态路由假阳多**；先抽样再删 |
| SonarJS 扫描 | 223 热点文件 | 认知 CC Top：`Form.vue` 140、`list/index` 119、`AiChatPanel` 85 |
| 组件 meta 抽样 | 3 组件 | BasicForm 43 props；契约文档化入口 |
| fuite | runbook only | 本轮 `:3100` 未起；有 Chrome 时可 `--run` |

---

## 整改建议顺序（业务优先）

1. **环枢纽**：`components/Jnpf/index.ts`、router ↔ store ↔ axios（架构柜）  
2. **在线开发列表/表单**：`dynamicModel/list/*`（复杂度柜 × 业务核心度高）  
3. **Studio SSE/聊天**：`AiChatPanel` / `PipelineSSEPanel`（复杂度 + 内存柜）  
4. **未用组件**：仅对 Knip 抽样做人工确认后删除（组件柜）
