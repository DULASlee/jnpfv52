# Design: frontend-arch-code-quality

> **编写规范**：[`docs/architecture/ARCHITECTURE_DOC_RULES.md`](../../docs/architecture/ARCHITECTURE_DOC_RULES.md)  
> **父提案**：[`proposal.md`](./proposal.md)  
> **定位**：P1 架构 + P2 设计模式 + P3 接口契约（禁止方法体，遵循 ADF 三先行）

---

## 1. 目标层边界（解除基础设施互咬）

### 1.1 现状违例（实测）

核心病根环（`utils/http/axios` 静态 import store）：

```
utils/http/axios/index.ts:19   import { useUserStoreWithOut } from '/@/store/modules/user'   ← 静态，环根因
utils/http/axios/index.ts:16   import { useErrorLogStoreWithOut } from '/@/store/modules/errorLog'
store/modules/user.ts:9        import { loginApi, getUserInfo, doLogout } from '/@/api/basic/user'
api/basic/user.ts              import { defHttp } from '/@/utils/http/axios'                  ← 闭环
```

> **关键纠正**：`router/routes/basic.ts` 的 `component: () => import('/@/views/...')` **已是动态 import**（懒加载），**不是环根因**。环根因是 HTTP 层对 store 的**静态反向依赖**。

### 1.2 目标拓扑

```
┌─────────────────────────────────────────────┐
│  views / components (表现层)                 │  依赖 store + api + composables
├─────────────────────────────────────────────┤
│  store (状态层)                              │  依赖 api
├─────────────────────────────────────────────┤
│  api (数据层)                                │  依赖 utils/http/axios
├─────────────────────────────────────────────┤
│  utils/http/axios (HTTP 基础设施)            │  只依赖 IUserContext 抽象 ★ 不依赖 store
│  router / hooks (基础设施)                   │
└─────────────────────────────────────────────┘
```

**依赖反转**：HTTP 层不再 import store，改为通过 `IUserContext` 接口获取 token/error 处理回调。接口由 store 实现，在 app 启动时注入。

### 1.3 failure_boundary

- **token 获取**：axios 拦截器需要 token，现状直接 `useUserStoreWithOut().getToken`。改为 `userContext.getToken()`，store 注入实现。
- **错误处理**：401 跳登录、错误日志记录，现状 import store。改为 `userContext.onUnauthorized()` / `errorContext.log(err)` 回调注入。
- **渐进**：先抽 axios 的 2 个 store 引用（user/errorLog），环数立降。剩余 hooks→store 等逐步处理。

---

## 2. Bundle 分包模式

### 2.1 现状

`vendor-common` 7.8MB 占 JS 55%——一个未分包巨块。结合 D5（echarts 1MB + antd 0.7MB + monaco ~4MB + tinymce），这些大库很可能全堆在 vendor-common。

### 2.2 手动分包策略（Vite manualChunks）

```ts
// vite.config.ts build.rollupOptions.output.manualChunks
{
  echarts: ['echarts', 'echarts-stat'],
  'vendor-antd': ['ant-design-vue', '@ant-design/icons-vue'],
  monaco: ['monaco-editor'],           // 按需: 仅代码编辑器页面加载
  editor: ['codemirror'],
  richText: ['tinymce', 'vditor'],
  charts2: ['highcharts', 'highcharts-vue'],  // 待 D5 收敛后移除
}
```

### 2.3 路由级懒加载验证

`router/routes/basic.ts` 已用动态 import——确认全部路由均为 `() => import()`，无静态 `import Foo from '@/views'`（静态会让该 view 进主 chunk）。

### 2.4 failure_boundary

- **不引入新工具**：用 Vite 内置 `manualChunks` + 既有 `rollup-plugin-visualizer` 验证，不换构建工具。
- **回归**：分包后逐路由验证（Playwright 关键路径），确认无 chunk 加载 404。

---

## 3. god 组件拆解模式

### 3.1 目标组件

| 组件 | CC | 行数 | 拆解策略 |
|------|---:|-----:|---------|
| `ColumnDesign/Main.vue` | 96 | 1256 | 按「列类型」抽 composable + 子组件 |
| `AiChatPanel.vue`(studio) | 200 | 2682 | 按「消息流/SSE/输入/工具栏」四域拆 |

### 3.2 Main.vue 拆解

现状：CC 96，8 处 computed 副作用。内部混合列设计的数据计算 + UI 编排 + 字段联动。

模式：Extract Composable

```
Main.vue (编排, 目标 CC < 30)
  ├─ useColumnDesignData(props)     // 数据计算 → 从 computed 副作用净化
  ├─ useColumnFieldLinkage()        // 字段联动逻辑
  ├─ ColumnTypeSelector.vue         // 列类型选择 UI
  └─ ColumnList.vue                 // 列表渲染
```

**computed 副作用净化规则**：所有 `computed` 内的 `let`/`if`/赋值移到 `watch` 或纯函数；computed 只做纯计算。

### 3.3 不变量

- **UI 行为不变**：拆解前后，列设计器的交互（增删列、改类型、联动）完全一致。
- **先测后拆**：用 `@vue/test-utils` + Vitest 写组件测（渲染快照 + 关键交互），绿后再拆。

---

## 4. P3 接口契约（签名级）

### 4.1 IUserContext（HTTP 层依赖反转）

```ts
// src/utils/http/IUserContext.ts
export interface IUserContext {
  getToken(): string | null;
  onUnauthorized(): void;  // 401 时跳登录
}
export interface IErrorContext {
  log(error: AxiosError): void;
}
// app 启动注入: setHttpDependencies({ userContext: useUserStore(), errorContext: useErrorLogStore() })
```

### 4.2 axios 改造点

`src/utils/http/axios/index.ts`:
- 删除：`import { useUserStoreWithOut }` / `import { useErrorLogStoreWithOut }`
- 新增：从注入的 `IUserContext`/`IErrorContext` 取 token 和错误回调
- 注入时机：`main.ts` app mount 前

---

## 5. a11y 修正模式

### 5.1 全页面级（3 处，极小改动）

| 问题 | 修正 | 文件 |
|------|------|------|
| `lang="zh_CN"` | → `lang="zh-CN"` | `index.html` |
| `user-scalable=0` | → `user-scalable=1`（或删除） | `index.html` viewport |
| `role="bar"/"spinner"` | → 合法值（`role="progressbar"`/移除） | nprogress 样式 |

### 5.2 键盘可达（293 处 @click 绑 div，横切）

模式：`<div @click>` → `<button @click>`（或 `<div role="button" tabindex="0" @keydown.enter>`）。

批量策略：先 lint 规则（eslint-plugin-vue 自定义）拦截新增，存量分模块逐个改（views/ 优先于 components/）。

---

## 6. 整改排序（与 tasks.md 对齐）

| 阶段 | 内容 | 前置依赖 |
|------|------|---------|
| F1 | 清死代码(104文件+14依赖) + pnpm dedupe | 无（先清让分析更准） |
| F2 | axios 抽 IUserContext，切断核心环 | F1（死代码清完） |
| F3 | vendor-common 手动分包 + god 组件补测拆解 | F2（环断才能有效分包） |
| F4 | a11y 全页面修正 + computed 副作用清理 + stylelint --fix | F3 |

**铁律**：F3 god 组件拆解「先补组件测（红→绿）→ 再 extract（绿保绿）」。

---

## 7. 风险与缓解

| 风险 | 缓解 |
|------|------|
| 删死代码误删动态引用 | Knip 已知有动态 import 盲区；逐文件 `grep -r "文件名"` 确认无引用再删；先删整目录死组件（高置信） |
| axios 改造破坏登录/token | Playwright 登录 spec 回归 + token 注入冒烟 |
| 分包导致 chunk 404 | 逐路由 Playwright 验证；先在 dev 模式分包验证再 prod |
| 拆 god 组件破坏 UI | 组件测快照 + 关键交互断言；一次只拆一个 composable |
| 重叠库替换影响图表 | 先做使用方盘点（`grep highcharts`），确认能下掉再 remove |
