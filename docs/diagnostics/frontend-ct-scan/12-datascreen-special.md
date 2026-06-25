# 12 — jnpf-web-datascreen 专项深度分析

> 扫描日期：2026-06-08
> 扫描范围：jnpf-web-datascreen (数字大屏项目)

---

## 项目定位

datascreen 是一个**混合式**项目，同时支持两种运行模式：

| 模式 | 入口 | 产物 | 用途 |
|---|---|---|---|
| **独立 SPA** | `index.html` → `src/main.js` | `dist/` (Vite build) | 大屏管理后台 (列表/编辑器/查看器) |
| **UMD 嵌入库** | `src/page/index.js` | `public/lib/index.umd.js` | 可嵌入第三方页面的 `<avue-data>` 组件 |

---

## 一、架构全景

### 1.1 技术栈概览

| 维度 | 详情 |
|---|---|
| 框架 | Vue 3.2.47 (Options API 100%) |
| UI 库 | Element Plus 2.3.3 |
| 状态管理 | **无** — 使用 `window.$glob` + `window.$website` 全局对象 |
| 路由 | Vue Router 4，16 条路由 (4 静态 + 12 动态) |
| HTTP | Axios 0.19.0 (⚠️ CVE-2023-45857) |
| 可视化 | ECharts 5.4.0 + DataV + echarts-wordcloud + echarts-gl |
| 构建 | Vite 4.4.6 |
| CSS | SCSS (sass 1.37.5) |
| 语言 | JavaScript (无 TypeScript) |

### 1.2 与 web-vue3 的架构成熟度差距

datascreen 与 web-vue3 的架构成熟度差距约为 **3 年**：

| 维度 | web-vue3 | datascreen |
|---|---|---|
| 状态管理 | Pinia (类型安全) | `window.$glob` (无类型、无封装) |
| HTTP 封装 | VAxios (5层管道、CancelToken、重试) | Axios 0.19 单例 (无取消、无重试) |
| 路由守卫 | 8 层守卫链 | 0 层 (仅 Token 存在检查) |
| Token 存储 | AES 加密 localStorage | sessionStorage 明文 |
| 权限 | 3 级 (按钮/列/表单) | 无 |
| TypeScript | ✅ (放宽) | ❌ |
| 测试 | ❌ | ❌ |
| Lint | ESLint + Prettier | ❌ |

---

## 二、数据大屏设计器 — 核心差异化能力

### 2.1 设计器架构

datascreen 包含一个完整的**拖拽式大屏设计器**，这是其核心价值：

```
设计器架构:
├── 组件面板 (左侧)
│   ├── 图表组件 (bar/line/pie/scatter/funnel/gauge/radar/map/wordCloud/pictorialBar)
│   ├── 展示组件 (table/text/data/datetime/flop/progress/img/svg/html/iframe)
│   ├── 媒体组件 (video/audio/swiper)
│   └── 容器组件 (group/tabs/borderBox/decoration/rectangle)
├── 画布区域 (中央)
│   ├── container.vue — 拖拽画布
│   ├── vue3-sketch-ruler — 标尺辅助
│   └── CSS transform: scale() — 屏幕适配
├── 属性面板 (右侧)
│   ├── 样式配置
│   ├── 数据源配置 (8 种数据源类型)
│   └── 动画设置
└── 图层管理
    └── 组件层级/显示隐藏/锁定
```

### 2.2 组件工厂模式

```javascript
// src/echart/index.js — 自动发现 ECharts 图表组件
const modules = import.meta.globEager('./packages/**/*.vue')
// 自动注册为 avue-echart-{name} 全局组件

// src/components/index.js — 自动发现展示组件
const modules = import.meta.globEager('./components/**/*.vue')
```

**风险：** `import.meta.globEager` 是 Vite 2.x API，Vite 3+ 已废弃。

### 2.3 配置驱动架构

`public/config.js` (~2,400 行) 是所有组件的配置中心：

```javascript
// 每个图表组件的默认属性/样式/数据配置
const baseList = [
  {
    name: '柱状图',
    type: 'bar',
    icon: 'icon-bar',
    option: { /* ECharts 默认配置 */ },
    style: { /* CSS 默认样式 */ },
    data: { /* 数据源默认配置 */ }
  },
  // ... 35+ 组件配置
]
```

### 2.4 8 种数据源类型

| 数据源 | 说明 |
|---|---|
| 静态数据 | JSON 编辑器直接编写 |
| SQL 查询 | 后端执行 SQL → 返回结果 |
| API 接口 | HTTP 请求 → JSON 响应 |
| 字典数据 | 系统字典 |
| 动态数据 | WebSocket 推送 |
| 大屏变量 | 全局变量共享 |
| 数据集 | 预定义数据集 |
| 脚本 | JavaScript 自定义处理 |

### 2.5 屏幕适配方案

```css
/* 1920×1080 基准 → 等比缩放 */
.screen-container {
  width: 1920px;
  height: 1080px;
  transform: scale(var(--scale));
  transform-origin: 0 0;
}
```

JS 动态计算 `--scale = min(containerWidth/1920, containerHeight/1080)`。

---

## 三、严重问题清单

### 3.1 安全红线 (P0)

| # | 问题 | 详情 |
|---|---|---|
| 1 | **零认证零授权** | 无登录页、无路由守卫、无权限检查。Token 通过 URL `?token=xxx` 明文传递 |
| 2 | **axios 0.19.0 CVE** | CVE-2023-45857 (SSRF) + CVE-2020-28168 (ReDoS)，5 年未更新 |
| 3 | **CDN 脚本无 SRI** | 10+ 全局脚本无 integrity 属性，供应链攻击面 |
| 4 | **postMessage 域验证缺失** | `window.addEventListener('message', ...)` 接受 `'*'` 来源 |

### 3.2 架构问题 (P1)

| # | 问题 | 详情 |
|---|---|---|
| 5 | **全局可变状态** | `window.$glob` + `window.$website` 无封装、无类型、无约束 |
| 6 | **巨型配置文件** | `public/config.js` ~2,400 行，不可维护 |
| 7 | **jQuery + Vue 3 混用** | `<script src="/cdn/jquery.min.js">` 在现代 Vue 3 项目中完全冗余 |
| 8 | **CDN 目录混乱** | 多版本并存：axios 1.0.0+1.3.6, vuex 2.4.1+3.1.1 |
| 9 | **Vue 2 生态残留** | element-ui 2.15.0, vue-router 3.0.1, vuex 2.x/3.x 在 CDN 目录中 |
| 10 | **废弃 API** | `import.meta.globEager` (Vite 2.x), 未来 Vite 版本可能移除 |

### 3.3 工程问题 (P2)

| # | 问题 | 详情 |
|---|---|---|
| 11 | **无 TypeScript** | 纯 JS，2400 行 config 无类型保护 |
| 12 | **无 ESLint/Prettier** | 代码风格不统一 |
| 13 | **无测试** | 0% 覆盖率 |
| 14 | **无 Git Hooks** | 低质量提交可能进入仓库 |
| 15 | **双锁文件** | `pnpm-lock.yaml` + `yarn.lock` 并存 |
| 16 | **无 CI/CD** | 无前端专属构建流水线 |
| 17 | **CDN 与 npm 重复** | ECharts/Vue/VueRouter 同时通过 CDN 和 npm 加载 |
| 18 | **客户端屏幕密码** | 密码仅 UI 层拦截，数据已通过 API 加载到浏览器 |

---

## 四、CDN 依赖审计

```
public/cdn/
├── animate/3.5.1/          # 动画库 (CSS)
├── avue/3.2.16/            # AVUE 框架
├── axios/1.0.0/            # ⚠️ 旧版本
├── axios/1.3.6/            # ⚠️ 重复
├── echarts/5.4.0/          # ECharts
├── element-plus/2.3.3/     # Element Plus (当前使用)
├── element-ui/2.15.0/      # ❌ Vue 2 Element UI (冗余)
├── html2canvas/            # 截图
├── iconfont/               # 图标
├── staticfile/             # FileSaver/XLSX/JSZip
├── vue/3.2.47/             # Vue 3
├── vue-router/3.0.1/       # ❌ Vue Router 3 (Vue 2, 冗余)
├── vuex/2.4.1/             # ❌ Vuex 2 (Vue 2, 冗余)
└── vuex/3.1.1/             # ❌ Vuex 3 (Vue 2, 冗余)
```

**清理建议：**
- 删除 `element-ui/`, `vue-router/3.0.1/`, `vuex/` → 节省 ~5MB
- 删除 `axios/1.0.0/` → 保留 1.3.6
- 将 ECharts 从 CDN 迁移到 npm → tree-shaking → 体积减少 60%

---

## 五、代码特征

### 5.1 100% Options API

```javascript
// 所有组件使用 Vue 2 风格的 Options API
export default {
  name: 'ChartBar',
  props: { ... },
  data() { return { ... } },
  mounted() { ... },
  methods: { ... }
}
```

无 `<script setup>`、无 Composition API。迁移到 Vue 3 现代写法的工作量不小。

### 5.2 文件规模分布

| 范围 | 文件数 |
|---|---|
| < 100 行 | 305 |
| 100-500 行 | 142 |
| 500-1000 行 | 19 |
| 1000+ 行 | 8 |

最大文件：`public/config.js` (2,400 行)、`src/views/builder/container.vue` (1,400 行)。

### 5.3 console.log 残留

`src/views/code-tip.vue` 包含 18 处 `console.log`，生产环境中可能泄露调试信息。

---

## 六、与 web-vue3 集成现状

当前 datascreen 与 web-vue3 的集成方式：

1. **跳转方式**：web-vue3 菜单点击 → `window.open(datascreenUrl + '?token=' + currentToken)`
2. **Token 传递**：URL 查询参数明文传递
3. **无 SSO**：两个项目独立认证，无单点登录

---

## 七、重构路线图建议

### Phase 1: 止血 (1 周)
- 添加路由守卫 + Token 验证
- 升级 axios 0.19.0 → 1.7.x
- 清理 CDN 冗余文件
- postMessage 添加 origin 白名单

### Phase 2: 架构升级 (2-3 周)
- 引入 Pinia 替代 `window.$glob`
- 拆分 `public/config.js` 为模块
- ECharts CDN → npm (tree-shaking)
- 添加 ESLint + Prettier
- 移除 jQuery 依赖

### Phase 3: 现代化 (3-4 周)
- TypeScript 迁移 (206 文件)
- Options API → Composition API
- `import.meta.globEager` → `import.meta.glob`
- 添加单元测试 + E2E

---

## 关键发现

| # | 发现 | 严重度 |
|---|---|---|
| 1 | 零认证零授权 — 任何知晓 URL 者可访问设计器和屏幕 | P0 |
| 2 | axios 0.19.0 (2019) 含已知 CVE，5 年未更新 | P0 |
| 3 | 设计器是核心差异化能力，但代码质量拖后腿 | — |
| 4 | CDN 目录含大量 Vue 2 生态冗余文件 | P1 |
| 5 | 2,400 行 `config.js` 是定时炸弹 | P1 |
| 6 | 与 web-vue3 集成方式不安全 (Token 在 URL 中) | P1 |
