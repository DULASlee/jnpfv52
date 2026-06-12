# CT Scan 1.2: 目录结构分析报告

> 扫描日期: 2026-06-08
> 扫描范围: jnpf-web-vue3 / jnpf-web-datascreen / jnpf-app-vue3
> 分析方法: 完整目录树 + 文件计数 + 类型分布

---

## 一、项目规模对比

| 指标 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 |
|---|---|---|---|
| **源文件总数** (不含 node_modules/dist) | ~1,585 | 474 | 1,180 |
| **Vue 组件数** | 725 | 131 | 412 |
| **TS/JS 逻辑文件** | 523 (.ts + .tsx + .js) | 75 (.js) | 203 (.js + .ts) |
| **样式文件** | 58 (.less + .css) | 10 (.scss + .css) | 38 (.scss + .css) |
| **图片资源** | 192 (.png + .gif + .svg) | 248 (.png + .jpg + .gif + .svg) | 294 (.png + .gif + .svg + .jpg) |
| **构建产物 (dist)** | 1,654 | — | — |

---

## 二、jnpf-web-vue3 目录详解

### 2.1 顶层目录

```
jnpf-web-vue3/
├── build/          19 files   — 构建脚本、Vite插件、主题配置
├── deploy/          1 file    — Nginx配置
├── public/        155 files   — 静态资源 (emoji GIF、图标字体)
├── src/         1,337 files   — 源代码
├── types/           8 files   — TypeScript 类型声明
├── .editorconfig
├── .env / .env.development / .env.production / .env.test
├── .eslintrc.js / .eslintignore
├── .prettierignore / prettier.config.js
├── .stylelintignore / stylelint.config.js
├── Dockerfile
├── index.html
├── package.json
├── postcss.config.js
├── tsconfig.json
├── vite.config.ts
└── windi.config.ts
```

### 2.2 src/ 目录结构 (1,337 文件)

| 目录 | 文件数 | 占比 | 职责 |
|---|---|---|---|
| `components/` | 618 | 46.2% | 全局组件 (~50个子目录) |
| `views/` | 344 | 25.7% | 业务页面 |
| `assets/` | 73 | 5.5% | 图标/图片/SVG |
| `api/` | 69 | 5.2% | API 服务模块 |
| `layouts/` | 57 | 4.3% | 布局组件 |
| `hooks/` | 41 | 3.1% | 组合式函数 |
| `utils/` | 35 | 2.6% | 工具函数 |
| `design/` | 23 | 1.7% | 设计系统 (Less变量/主题/过渡) |
| `locales/` | 21 | 1.6% | 国际化 |
| `router/` | 13 | 1.0% | 路由配置/守卫 |
| `store/` | 11 | 0.8% | Pinia 状态 |
| `logics/` | 9 | 0.7% | 应用初始化/错误处理 |
| `enums/` | 8 | 0.6% | TS 枚举 |
| `directives/` | 7 | 0.5% | 自定义指令 |
| `settings/` | 6 | 0.4% | 项目配置 |

### 2.3 views/ 业务模块分布 (344 文件)

| 模块 | 文件数 | 占比 | 说明 |
|---|---|---|---|
| `extend/` | 83 | 24.1% | 扩展功能 (最大模块) |
| `system/` | 51 | 14.8% | 系统管理 |
| `workFlow/` | 45 | 13.1% | 工作流 |
| `basic/` | 40 | 11.6% | 基础数据 |
| `permission/` | 31 | 9.0% | 权限管理 |
| `systemData/` | 28 | 8.1% | 系统数据 |
| `msgCenter/` | 21 | 6.1% | 消息中心 |
| `common/` | 21 | 6.1% | 公共页面 |
| `onlineDev/` | 18 | 5.2% | 在线开发 |
| `generator/` | 6 | 1.7% | 代码生成 |

### 2.4 components/ 核心组件 (前15)

| 组件 | 文件数 | 说明 |
|---|---|---|
| `Jnpf/` | 140 | **JNPF 核心组件库** (表格/表单/弹窗等) |
| `FormGenerator/` | 78 | 表单生成器 |
| `VisualPortal/` | 64 | 可视化门户 |
| `Table/` | 42 | 表格组件 |
| `FlowProcess/` | 22 | 流程设计器 |
| `IntegrateProcess/` | 20 | 集成流程 |
| `Form/` | 19 | 表单组件 |
| `SimpleMenu/` | 15 | 简易菜单 |
| `Modal/` | 14 | 弹窗组件 |
| `ColumnDesign/` | 14 | 列设计 |
| `Upload/` | 13 | 上传组件 |
| `Preview/` | 12 | 预览组件 |
| `CommonModal/` | 11 | 通用弹窗 |
| `BillRule/` | 10 | 单据规则 |
| `DataGrid/` | 10 | 数据表格 |

### 2.5 文件类型分布

| 扩展名 | 数量 | 占比 |
|---|---|---|
| `.vue` | 725 | 45.7% |
| `.ts` | 514 | 32.4% |
| `.gif` | 102 | 6.4% (表情包) |
| `.png` | 90 | 5.7% |
| `.less` | 41 | 2.6% |
| `.css` | 17 | 1.1% |
| `.svg` | 10 | 0.6% |
| `.tsx` | 9 | 0.6% |
| 其他 | 77 | 4.9% |

---

## 三、jnpf-web-datascreen 目录详解

### 3.1 顶层目录

```
jnpf-web-datascreen/
├── deploy/          1 file    — Nginx配置
├── public/        270 files   — 静态资源 + CDN库文件
├── src/           198 files   — 源代码
├── vite/            5 files   — Vite 插件配置
├── Dockerfile
├── index.html
├── lib.config.js               — UMD 库构建配置
├── package.json
├── pnpm-lock.yaml
├── vite.config.js
└── yarn.lock                   — 双锁文件!
```

### 3.2 src/ 目录结构 (198 文件)

| 目录 | 文件数 | 职责 |
|---|---|---|
| `page/` | ~80 | 页面组件 (编辑器/查看器/列表/分组) |
| `echart/packages/` | 34 | 图表组件 (bar/line/pie/map/...) |
| `option/components/` | 33 | 图表属性配置面板 |
| `api/` | 9 | API 接口模块 |
| `components/` | 6 | 共享组件 (code/fullscreen/imgTabs) |
| `styles/` | 4 | SCSS 样式 |
| `theme/` | 3 | 主题文件 |
| `utils/` | 3 | 工具函数 (含硬编码密钥!) |
| `icons/` | 23 | SVG 图标 |
| `mixins/` | 1 | Vue mixin |

### 3.3 关键架构文件

| 文件 | 大小 | 职责 |
|---|---|---|
| `src/index.js` | — | 库入口 + 全局注册函数 |
| `src/main.js` | — | App 启动引导 |
| `src/App.vue` | — | 根组件 (仅 `<router-view />`) |
| `src/router.js` | — | 路由实例 (动态添加路由) |
| `src/axios.js` | — | HTTP 封装 |
| `public/config.js` | ~2,400行 | **巨型组件目录配置** |
| `public/components.js` | — | 自定义组件定义 |
| `public/view.html` | — | 独立查看器 (加载 UMD 构建) |
| `public/swiper.html` | — | 轮播查看器 |

### 3.4 文件类型分布

| 扩展名 | 数量 | 占比 |
|---|---|---|
| `.png` | 195 | 41.1% |
| `.vue` | 131 | 27.6% |
| `.js` | 75 | 15.8% |
| `.svg` | 25 | 5.3% |
| `.jpg` | 18 | 3.8% |
| `.gif` | 10 | 2.1% |
| `.css` | 6 | 1.3% |
| `.scss` | 4 | 0.8% |
| 其他 | 10 | 2.1% |

**关键发现:** `.vue` 仅占 27.6%，远低于 jnpf-web-vue3 的 45.7%。大量逻辑内嵌在 `.js` 文件和 `public/config.js` 中。

---

## 四、jnpf-app-vue3 目录详解

### 4.1 顶层目录

```
jnpf-app-vue3/
├── api/            19 files   — API 接口模块
├── assets/          4 files   — 图标字体/SCSS
├── components/    130 files   — 组件 (Jnpf 49 + 其他 8 + ly-tree)
├── harmony-configs/ 5 files   — 鸿蒙OS 构建配置
├── libs/            5 files   — 核心库 (chat/permission/file/...)
├── locale/          6 files   — i18n (zh-Hans/zh-Hant/en)
├── pages/         440 files   — 页面 (12 主包 + 6 分包)
├── scripts/         3 files   — 辅助脚本
├── static/         28 files   — 静态资源
├── store/           5 files   — Pinia stores (4 modules)
├── uni_modules/   517 files   — UniApp 模块 (vk-uview-ui + uni-ui + 其他)
├── utils/           6 files   — 工具函数
├── App.vue                     — 应用根组件 (onLaunch 逻辑)
├── main.js                     — 入口 (Vue2/Vue3 条件编译)
├── manifest.json               — 多平台配置
├── pages.json                  — 路由/分包/TabBar
├── uni.scss                    — 全局 SCSS 变量
└── vite.config.js              — Vite 配置
```

### 4.2 pages/ 详细结构 (440 文件)

| 包 | 页面数 | 说明 |
|---|---|---|
| **主包** | | |
| pages/index/ | 4 | 首页/消息/工作流/申请/我的 (5 Tab) |
| pages/login/ | 4 | 登录/SSO/扫码/其他登录 |
| pages/launch/ | 3 | 启动/政策/引导 (APP only) |
| pages/formShortLink/ | 2 | 表单外链 |
| **分包-门户** | | |
| pages/portal/ | 3 | 应用门户/小程序门户/扫码门户 |
| **分包-消息** | | |
| pages/message/ | 5 | 联系人/用户详情/站内信/IM |
| **分包-工作流** | | |
| pages/workFlow/ | 18 | 流程待办/文档/日程/文件预览/评论 |
| **分包-公共** | | |
| pages/commonPage/ | 2 | 常用菜单/收藏流程 |
| **分包-申请** | | |
| pages/apply/ | 13 | 报表日志/动态模型/订单/定位/外部链接 |
| **分包-我的** | | |
| pages/my/ | 13 | 设置/扫码结果/修改密码/个人资料/委托代理 |

### 4.3 uni_modules/ (517 文件, 47个模块)

**核心UI框架:**
- `vk-uview-ui/` — 90+ 组件 (主要 UI 库)
- `uni-ui/` — 47 个 uni-* 模块 (官方组件库)

**功能性模块:**
- `mescroll-uni/` — 下拉刷新/上拉加载
- `mp-html/` — 富文本渲染
- `qiun-data-charts/` — 图表 (uCharts)
- `lsj-upload/` — 文件上传
- `jnpf-exitApp/` — 退出应用

### 4.4 components/Jnpf/ (49 个组件)

JNPF 移动端组件库，覆盖表单控件全集:
`Alert`, `AreaSelect`, `AutoComplete`, `Barcode`, `Button`, `Calculate`, `Cascader`, `Checkbox`, `ColorPicker`, `DatePicker`, `DateRange`, `DepSelect`, `Divider`, `Editor`, `GroupSelect`, `GroupTitle`, `Input`, `InputNumber`, `Link`, `Location`, `NumberRange`, `OpenData`, `OrganizeSelect`, `Parser`, `PopupAttr`, `PopupSelect`, `PosSelect`, `Qrcode`, `Radio`, `Rate`, `RelationForm`, `RelationFormAttr`, `RoleSelect`, `Select`, `Sign`, `Signature`, `Slider`, `Steps`, `Switch`, `Text`, `Textarea`, `TimePicker`, `TimeRange`, `TreeSelect`, `UploadFile`, `UploadFileComment`, `UploadImg`, `UserSelect`, `UsersSelect`

### 4.5 文件类型分布

| 扩展名 | 数量 | 占比 |
|---|---|---|
| `.vue` | 412 | 34.9% |
| `.js` | 201 | 17.0% |
| `.gif` | 200 | 17.0% (聊天表情) |
| `.md` | 105 | 8.9% (uni_modules文档) |
| `.json` | 98 | 8.3% |
| `.png` | 91 | 7.7% |
| `.scss` | 27 | 2.3% |
| `.css` | 11 | 0.9% |
| `.ts` | **2** | **0.17%** |

---

## 五、结构健康度评估

### jnpf-web-vue3: 良好 (B+)
- ✅ 清晰的目录分层 (components/views/api/hooks/utils/store/router)
- ✅ TypeScript 覆盖率高 (42.4% .ts + .tsx)
- ✅ 完整的工程化配置 (lint/format/git hooks)
- ⚠️ components/ 目录过大 (618文件, 46%), 组件发现困难
- ⚠️ views/extend/ 膨胀 (83文件, 24%), 需评估是否可拆分

### jnpf-web-datascreen: 需改进 (D)
- ❌ `public/config.js` ~2,400行巨型配置文件，难以维护
- ❌ 组件自动发现依赖 `import.meta.globEager` (Vite已废弃API)
- ❌ 源码与构建产物混合 (`public/lib/` 存放 UMD 构建)
- ❌ CDN 库存放在 `public/cdn/` (应通过 npm 管理)
- ⚠️ 34个图表组件 + 33个配置面板 = 高度耦合的插件架构

### jnpf-app-vue3: 需改进 (C-)
- ❌ `uni_modules/` 517文件 (44%), 大量第三方代码内嵌
- ❌ 200个 GIF 表情包 (17%), 应动态加载
- ❌ package.json 不完整, 90%依赖未声明
- ❌ 条件编译 (`#ifdef`) 散落各处, 代码路径难以追踪
- ⚠️ Vue 2 兼容代码保留 (增加认知负担)

---

## 六、结构改进建议 (概要)

1. **三项目统一目录规范**: 制定统一的 src/ 一级目录结构标准
2. **datascreen 紧急拆分**: `public/config.js` 拆分为独立模块
3. **app 依赖显式化**: 所有实际使用依赖必须出现在 package.json
4. **静态资源管理**: CDN 依赖统一迁移到 npm, 表情包延迟加载
5. **组件索引**: 为 jnpf-web-vue3 的 618 个组件建立 barrel export
