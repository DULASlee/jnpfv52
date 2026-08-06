# Tasks: frontend-arch-code-quality

> **实施计划**（对照 [`design.md`](./design.md)）· **铁律**：F3 god 组件「先补组件测(红→绿) → 再 extract(绿保绿)」  
> **验收**：每阶段 `pnpm lint` + `pnpm type-check` + `pnpm test:unit` 全绿；扫描指标改善

---

## F1 — 清死代码 + 收敛依赖（让后续分析更准）

- [ ] 逐文件 `grep -r "文件名"` 复核 Knip 104 死文件，确认无动态引用（重点关注非整目录死的散文件）
- [ ] 删除 12 个整目录死组件（Authority/Cropper/Excel/FlowChart/Markdown/Page/Time/Verify 等高置信）
- [ ] 删除散布死文件（locales 22 个 / core 14 个 / hooks 5 个）
- [ ] `pnpm remove` 14 个未用依赖（先复核 `@microsoft/fetch-event-source`/`dompurify` 是否动态加载）
- [ ] 删除死链依赖 `@logicflow/core` + `@logicflow/extension`（FlowChart 已死）
- [ ] `pnpm dedupe` 收敛多版本包（先收纯类型：type-fest 8 版本 → 1）
- [ ] **验收**：`npx knip` 未用文件 < 10；`pnpm build` 无报错；bundle 体积下降

## F2 — 打破核心环（axios 依赖反转）

> 源码：`src/utils/http/axios/index.ts:16,19` 静态 import store

- [ ] 新建 `src/utils/http/IUserContext.ts` + `IErrorContext.ts`（设计 §4.1）
- [ ] axios 拦截器改用注入的 `IUserContext.getToken()` 替代 `useUserStoreWithOut().getToken`
- [ ] axios 401 处理改用 `IUserContext.onUnauthorized()` 替代 store 调用
- [ ] axios 错误日志改用 `IErrorContext.log()` 替代 `useErrorLogStoreWithOut`
- [ ] `src/main.ts`：app mount 前注入 `{ userContext, errorContext }`
- [ ] 删除 `axios/index.ts` 对 store 的 2 个 import
- [ ] **验收**：`npx madge --circular` 环数 < 80（从 182）；Playwright 登录 spec 绿；token 冒烟绿

## F3 — 拆 Bundle + god 组件

### F3a vendor-common 分包
- [ ] `vite.config.ts` 配置 `manualChunks`（echarts/antd/monaco/editor/richText，设计 §2.2）
- [ ] 逐路由 Playwright 验证无 chunk 404
- [ ] **验收**：首屏 chunk < 2MB（从 vendor-common 7.8MB 拆出）；`rollup-plugin-visualizer` 复跑确认分块

### F3b ColumnDesign/Main.vue (CC96)
- [ ] **先补组件测**：`useColumnDesign` 渲染 + 增删列 + 改类型 + 联动 → 红→绿
- [ ] extract `useColumnDesignData` composable（净化 computed 副作用）
- [ ] extract `useColumnFieldLinkage` composable
- [ ] extract `ColumnTypeSelector.vue` + `ColumnList.vue` 子组件
- [ ] 清 8 处 computed 副作用（let/if/赋值移到 watch 或纯函数）
- [ ] **验收**：CC < 30；组件测绿；UI 行为不变

### F3c AiChatPanel.vue (2682行/CC200)
- [ ] **先补组件测**：消息流渲染 + SSE 接收 + 输入发送 → 红→绿
- [ ] 按「消息流/SSE/输入/工具栏」四域拆 composable + 子组件
- [ ] **验收**：CC < 30；文件 < 800 行；组件测绿

## F4 — a11y + CSS + 类型收敛

- [ ] **a11y 全页面**（3 处）：`index.html` lang `zh_CN→zh-CN`；viewport 去 `user-scalable=0`；nprogress `role="bar"→"progressbar"`
- [ ] **a11y 治本**：自定义 eslint 规则拦截新增 `<div @click>`；存量分批改 button/role（views 优先）
- [ ] **CSS 自动修**：`pnpm lint:stylelint --fix` 清 1957 空行格式
- [ ] **CSS 真病症**：人工修 duplicate-properties(11) + duplicate-selectors(6)
- [ ] **computed 副作用**：清 261 处（先 ColumnDesign/Main 已在 F3b 清 8 处，其余分批）
- [ ] **类型收敛**：`views/` any 从 1024 处逐步收敛（优先拆解过的组件）
- [ ] **验收**：axe critical=0；stylelint error < 50；VMD computedSideEffects < 50

## F5 — 收尾

- [ ] 更新 `CLAUDE.md` / `AGENTS.md`：记录 IUserContext 注入约定 + eslint a11y 规则
- [ ] 复跑九维度扫描，更新三份 design-quality 报告数据
- [ ] 复跑 D8/D9 Playwright，更新 runtime 报告
- [ ] 全链冒烟：`pnpm type-check` + `pnpm test:unit` + `pnpm build` + Playwright 关键路径
