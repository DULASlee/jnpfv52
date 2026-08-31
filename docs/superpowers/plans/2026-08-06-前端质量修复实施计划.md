# 前端质量迭代修复与优化 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 前端先挂五柜 advisory，再断环枢纽（含 axios 依赖反转），再单文件降认知复杂度，再补 Studio/SSE 深路径内存，最后 Bundle/god 大拆与 a11y。

**Architecture:** F0 工具链报告化；F1 枢纽制拆环（首选 IUserContext）；F2 单文件 Extract/composable；F3 登录态 fuite + R6；F4 Bundle + god 先测后拆 + a11y。不合并三前端，不盲删 Knip 文件。

**Tech Stack:** Vue3 · Vite · dependency-cruiser · Knip · eslint-plugin-sonarjs · vue-component-meta · fuite · Vitest · `pnpm type-check`

**Spec（唯一设计源）:** [`../specs/2026-08-06-frontend-quality-remediation-design.md`](../specs/2026-08-06-frontend-quality-remediation-design.md)

**波次映射（历史草稿 → 本计划）:** 原 F1→F0′ · F2→F1 · F3b→F2/F4b · F3a/F3c/F4→F4 · F5→Task R

**Backend plan (separate):** [`2026-08-06-backend-quality-remediation-plan.md`](2026-08-06-backend-quality-remediation-plan.md)

## Baseline inventory（五柜 · 2026-08-06）

| 柜 | 关键数字 |
|----|----------|
| 架构 depcruise | 1124 违例（854 err / 270 warn） |
| Knip unused | 592（vue 443，假阳多） |
| SonarJS 认知命中 | 126；Top dynamicForm **146** |
| fuite 入口 | 无泄漏（-87.5 kB）；Studio 未覆盖 |
| CT/静态/运行时对照 | madge ~182 环；vendor-common ~7.8MB；axe critical/serious |

## Global Constraints

- 禁止批量删除 Knip/depcruise 命中文件（整目录高置信 + 路由/菜单复核除外）。  
- 禁止无表征大拆 Studio/在线开发页。  
- SSE/Timer 遵守 R6（保存句柄、onUnmounted 清理、重连上限、`buildEventSourceUrl`）。  
- `pnpm type-check`（禁止裸全量 vue-tsc）。  
- 一 Chat 一个可演示波次。  
- 包管理：pnpm 8。  
- 不在本计划改后端 `.cs`。  
- **勾选只在本文件**。

---

## File map

| 路径 | 职责 |
|------|------|
| `jnpf-web-vue3/package.json` `quality:*` | F0 |
| `.dependency-cruiser.cjs` | F0/F1 规则姿态 |
| `src/utils/http/axios/index.ts` · `IUserContext.ts` · `main.ts` | F1 DIP |
| `src/components/Jnpf/index.ts` 等 | F1 枢纽 |
| `dynamicForm/index.vue` 或 `dynamicModel/list/*` | F2 |
| `AiChatPanel.vue` / `PipelineSSEPanel.vue` · cab4 scenario | F3 / F4c |
| `vite.config.ts` · visualizer | F4a |
| `ColumnDesign/Main.vue` | F4b（或 F2 候选） |
| `index.html` · nprogress | F4d a11y |
| `.claude/evidence/frontend-ct/` | 证据 |

---

### Task 0: 开工与证据冻结

**Files:**
- Read: `docs/superpowers/specs/2026-08-06-frontend-quality-remediation-design.md`
- Read: `.claude/evidence/frontend-ct/cabinets-full-run-summary.json`

- [ ] **Step 1:** 确认口令指向 F0 / F0′ / F1 / F2 / F3 / F4  
- [ ] **Step 2:** 记下本波次基线数字（环数 / CC / fuite / vendor 体积）  

---

### Task 1: F0 — quality 进日常/CI advisory

**Files:**
- Modify: `docs/toolchain/SETUP.md`（或现有 CI 工作流）
- Keep: `jnpf-web-vue3/package.json` scripts

- [ ] **Step 1:** 本地跑通 `quality:arch` / `knip` / `complexity` / `components`  
- [ ] **Step 2:** 写入 SETUP/CI 为 **advisory**（失败不挡合并，直至 F1 约定升严）  
- [ ] **Step 3:** 更新 `cabinets-full-run-summary.json` 时间戳  

```powershell
cd D:\JNPF-v52\jnpf-web-vue3
pnpm quality:arch
pnpm quality:knip
pnpm quality:complexity
pnpm quality:components
```

---

### Task 1b: F0′ — 高置信死目录 / dedupe（可选）

> 默认不阻塞 F1。仅在口令点名 F0′ 时做。

**Files:**
- Knip 报告抽样；整目录死组件（Authority/Cropper/Excel/FlowChart/Markdown 等需再核对）
- `package.json`（`pnpm remove` / `dedupe`）

- [ ] **Step 1:** 对照动态路由/菜单，列出**可删整目录**清单（禁止一次清 592）  
- [ ] **Step 2:** 逐目录引用复核后删除；散文件低置信跳过  
- [ ] **Step 3:** 复核后 `pnpm remove` 确认未用依赖（动态加载如 EventSource/dompurify 慎删）  
- [ ] **Step 4:** `pnpm dedupe`（优先纯类型多版本）  
- [ ] **Step 5:** `pnpm type-check` + `pnpm build` 绿；节点审批  

```powershell
cd D:\JNPF-v52\jnpf-web-vue3
pnpm type-check
pnpm build
```

---

### Task 2: F1 — 环依赖枢纽 + axios DIP

**Files:**
- Create: `src/utils/http/IUserContext.ts`（及 ErrorContext，见 Spec §6.1）
- Modify: `src/utils/http/axios/index.ts` · `src/main.ts`
- Optional: `components/Jnpf/index.ts`、router 等 Top 枢纽（本波次合计改动枢纽 ≤3）
- Optional: `.dependency-cruiser.cjs`

- [ ] **Step 1:** 列出本波次枢纽（**必须含 axios↔store**）+ 当前环命中次数  
- [ ] **Step 2:** 新建接口；axios 改用注入 `getToken` / `onUnauthorized` / `log`  
- [ ] **Step 3:** `main.ts` mount 前注入；删除 axios 对 store 的静态 import  
- [ ] **Step 4:** 可选同步拆 1–2 个 barrel 枢纽  
- [ ] **Step 5:** `pnpm quality:arch` 对比下降；可选 `npx madge --circular` 对照  
- [ ] **Step 6:** `pnpm type-check` 绿；登录冒烟  
- [ ] **Step 7:** 节点审批（演示：登录 + 目标页无白屏）  

```powershell
cd D:\JNPF-v52\jnpf-web-vue3
pnpm quality:arch
pnpm type-check
node D:\JNPF-v52\scripts\lib\jnpf-auth.mjs --json
```

**演示:** 登录 → 打开工作流/Studio → 无白屏；环数报告可对比下降。

---

### Task 3: F2 — 单文件认知复杂度下降

**Files:**
- **只选一个**：`workFlowForm/dynamicForm/index.vue` **或** `dynamicModel/list/Form.vue` **或** `ColumnDesign/Main.vue`

- [ ] **Step 1:** 记录该文件 SonarJS / 认知 CC  
- [ ] **Step 2:** 页面金丝雀或组件测表征关键行为（红→绿）  
- [ ] **Step 3:** Extract 函数或 composable；行为不变；净化 computed 副作用  
- [ ] **Step 4:** `pnpm quality:complexity` 确认该文件 CC 下降  
- [ ] **Step 5:** 节点审批  

```powershell
cd D:\JNPF-v52\jnpf-web-vue3
pnpm quality:complexity
pnpm type-check
```

---

### Task 4: F3 — Studio/SSE 深路径内存

**Files:**
- `cab4-fuite-scenario-def.cjs`（登录后 URL/步骤）
- 必要时 `PipelineSSEPanel.vue` / `AiChatPanel.vue`（**仅 R6 泄漏点**，不大拆）

- [ ] **Step 1:** 保证 `:3100` + `:5000`；拿到可复用登录态策略（禁手点登录脚本优先）  
- [ ] **Step 2:** 扩展 fuite 场景到 Studio 路由  
- [ ] **Step 3:** `pnpm quality:memory:run`；产物 `cab4-fuite*.json`  
- [ ] **Step 4:** 若检出泄漏 → 按 R6 修并重跑  
- [ ] **Step 5:** 节点审批  

```powershell
cd D:\JNPF-v52\jnpf-web-vue3
$env:PUPPETEER_EXECUTABLE_PATH='C:\Program Files\Google\Chrome\Application\chrome.exe'
pnpm quality:memory:run
```

---

### Task 5: F4a — vendor-common 分包

**Files:**
- Modify: `vite.config.ts` `manualChunks`（Spec §5.2）
- Verify: rollup-plugin-visualizer

- [ ] **Step 1:** 配置 echarts / antd / monaco / editor / richText 等分块  
- [ ] **Step 2:** prod build + visualizer；记录首屏/vendor 体积变化  
- [ ] **Step 3:** 关键路由验证无 chunk 404  
- [ ] **Step 4:** 节点审批  

```powershell
cd D:\JNPF-v52\jnpf-web-vue3
pnpm build
```

---

### Task 5b: F4b/F4c — god 组件大拆

> 与 F4a 可同里程碑、不同 Chat。F2 已拆过的文件跳过。

**Files:**
- `ColumnDesign/Main.vue` 和/或 `AiChatPanel.vue`
- 新建 composable / 子组件

- [ ] **Step 1:** **先补组件测**（渲染 + 关键交互）红→绿  
- [ ] **Step 2:** 按 Spec §5.1 一次抽一个 composable/子组件  
- [ ] **Step 3:** 目标：编排层 CC 明显下降；AiChatPanel 行数分段下降  
- [ ] **Step 4:** `pnpm test:unit` + `pnpm type-check` 绿  
- [ ] **Step 5:** 节点审批（UI 行为不变）  

---

### Task 5c: F4d — a11y + CSS 收敛

**Files:**
- `index.html` · nprogress  
- 可选：eslint 规则拦新增 `<div @click>`  
- stylelint

- [ ] **Step 1:** 修全页面三处（lang / viewport / role）  
- [ ] **Step 2:** `pnpm lint:stylelint --fix`（格式空行）；真病症 duplicate 人工修  
- [ ] **Step 3:** （可选）eslint 拦新增 div@click；存量不本轮清零  
- [ ] **Step 4:** 节点审批  

---

### Task 6: R — 季度五柜再生

- [ ] **Step 1:** 全量重跑 quality:*  
- [ ] **Step 2:** 更新 `design-quality-frontend-cabinets.md` 与 CT/runtime 关键数字  
- [ ] **Step 3:** 评估 `no-circular` 是否可对枢纽升 error  
- [ ] **Step 4:** 关键路径：`pnpm type-check` + `pnpm test:unit` + `pnpm build`  

---

## 顺序

```text
Task0 → Task1(F0) → (可选 Task1b F0′) → Task2(F1) → Task3(F2) → Task4(F3)
      → Task5(F4a) / Task5b / Task5c → Task6(R)
```

F1 与后端计划可并行（不同 Chat/worktree），但前端 Chat 内仍单波次。

## 完成定义

- [ ] F0 advisory 已挂  
- [ ] F1 axios 环切断 + 枢纽环数可对比下降  
- [ ] F2 或 F3 或 F4 至少一波用户「通过」  
- [ ] `pnpm type-check` 绿  
- [ ] 证据在 `.claude/evidence/frontend-ct/`  
