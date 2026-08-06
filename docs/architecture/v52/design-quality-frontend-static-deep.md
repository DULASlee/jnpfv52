# 前端深度静态扫描 — 死代码 / 重复依赖 / CSS 健康 / Bundle 构成

> **日期**：2026-08-06  
> **范围**：`jnpf-web-vue3`（`src` 1566 文件 + `package.json` + `pnpm-lock.yaml`）  
> **父文档**：[`design-quality-diagnostics.md`](design-quality-diagnostics.md)  
> **姊妹篇**：[`design-quality-frontend-ct-report.md`](design-quality-frontend-ct-report.md)（Vue 反模式 / 复杂度 CT）  
> **证据目录**：`.claude/evidence/frontend-ct/`  
> **定位**：本文覆盖 CT 报告**未触及的四个前端独有静态维度**（D4-D7）。CT 报告看「代码怎么写错了」，本文看「什么代码根本不该还在 / 什么依赖在重复膨胀 / 样式健康度」  
> **编写规范**：[`../ARCHITECTURE_DOC_RULES.md`](../ARCHITECTURE_DOC_RULES.md)

---

## 0. 四维度定位

| 维度 | 工具 | 性质 | 一句话结论 |
|------|------|------|-----------|
| **D6 死代码** | Knip | 静态 | **104 个未用文件 + 14 个未用依赖 + 214 未用导出 + 212 未用类型** |
| **D5 重复依赖** | pnpm-lock 解析 + package.json 比对 | 静态 | **4 类功能重叠库共存 + 196 个包带多版本** |
| **D7 CSS 健康** | stylelint 全量 | 静态 | **205 文件 / 2270 error**（86% 是空行格式，可自动修） |
| **D4 Bundle 构成** | dist 实际文件大小 | 构建时 | **JS 14.8MB，vendor-common 单块 7.8MB 占 55%（未分包肿瘤）** |

> **重要**：这四个维度是 vue-mess-detector **完全不覆盖**的前端独有切面。CT 报告查的是「Vue 反模式与复杂度」；本文查的是「资产库存健康度」。两者互补。

---

## 1. D6 死代码（Knip）

### 1.1 命令与数据源

```bash
cd jnpf-web-vue3
npx knip --no-exit-code --reporter symbols
```

> ⚠️ **可信度声明**：Knip 报告了一处 `vite.config.ts` 解析错误（`Cannot read properties of undefined (reading 'split')`）。这会导致**少量漏报**（少报死代码），但**不会误报**——已列出的 104 个文件仍是高置信度的死代码。完整死代码量应 ≥ 104。

### 1.2 汇总

| 类别 | 数量 |
|------|-----:|
| 未使用**文件** | **104** |
| 未使用 **dependencies**（package.json 里装了但从没 import） | **14** |
| 未使用 **devDependencies** | **12** |
| 未使用**导出**（export 了但没人 import） | **214** |
| 未使用**导出类型**（interface/type 导出但没人用） | **212** |
| 未使用 enum 成员 | 4 |
| 重复导出 | 2 |

### 1.3 未使用文件按目录分布

| 目录 | 死文件数 | 读法 |
|------|--------:|------|
| `src/components` | **49** | 半数组件库是死代码——低代码平台迭代遗留 |
| `src/locales` | **22** | 多语言文件未接驳 |
| `src/core` | 14 | AI/编译器子模块遗留 |
| `src/hooks` | 5 | 未用 composable |
| `src/views` | 5 | 死页面 |
| `src/layouts` | 3 | — |
| `build/script` | 2 | 构建脚本遗留 |
| `src/directives` | 2 | — |

**死组件示例（整目录死的）：** `Authority`、`CardList`、`ClickOutSide`、`CodeEditor`、`Cropper`、`Excel`、`FlowChart`、`Markdown`、`Page`、`Time`、`Verify`、`VirtualScroll` —— 这些是 `index.ts` + 组件 + 类型 + props 整套都未被引用。

### 1.4 未使用 dependencies（可直接 `pnpm remove`）

```
@iconify/iconify      @logicflow/core        @logicflow/extension
@microsoft/fetch-event-source   @vue/runtime-core   @vueuse/shared
cropperjs             dompurify             （共 14 项，见证据文件）
```

> **注意**：Knip 对动态 import / 反射注册可能漏判。`@microsoft/fetch-event-source`、`dompurify` 等若经动态加载需人工复核（标【待源码验证】）。但 `@logicflow/core` + `@logicflow/extension` 与 D6 死文件 `FlowChart/*` 对应——**FlowChart 组件已死，其依赖逻辑图库亦可移除**，这是一条完整的死代码链。

### 1.5 本节关键代码路径索引

| 路径 | 用途 |
|------|------|
| `npx knip --no-exit-code --reporter symbols` | Knip 全量输出（命令复现，证据未持久化） |
| `jnpf-web-vue3/package.json` | 未用依赖清单源 |
| `src/components/FlowChart/` | 死组件 + 死依赖（@logicflow）联动案例 |

---

## 2. D5 重复依赖（bundle 膨胀根因）

### 2.1 功能重叠库共存（架构决策债）

`package.json` 里存在 **4 类功能重叠的库同时被安装**：

| 功能域 | 共存库 | 问题 |
|--------|--------|------|
| **图表** | `echarts` + `echarts-stat` + `highcharts` + `highcharts-vue` | 两套图表库并存，echarts(~1MB) + highcharts(~800KB) 双重体积 |
| **代码编辑器** | `codemirror@5` + `monaco-editor@0.38` | 两套编辑器，monaco 单独约 4MB |
| **Markdown/富文本** | `marked` + `showdown` + `tinymce` + `vditor` | 4 个富文本/MD 方案并存 |
| **拖拽** | `sortablejs` + `vuedraggable` + `vue3-draggable-resizable` | vuedraggable 本就是 sortablejs 的封装 |

**读法**：这不是「误装」，而是低代码平台长期演进中不同模块各自选型、从未收敛。**每收敛一类可省 0.5-4MB bundle**。需结合 D6 死代码判断：若某套库的使用方已死，可直接移除整套。

### 2.2 多版本包（pnpm-lock 实测）

```bash
# 解析 pnpm-lock.yaml (lockfileVersion 6.0) 的 packages 段，统计同名多版本
# 结果已存：.claude/evidence/frontend-ct/d5-multiversion.txt（196 个多版本包）
```

| 指标 | 数值 |
|------|-----:|
| lock 中去重包总数 | 1466 |
| **带多版本的包** | **196** |

**多版本 Top（≥3 版本）：**

| 包 | 版本数 | 影响 |
|----|------:|------|
| `type-fest` | **8** | 类型工具库，纯类型不进 bundle，但污染 node_modules |
| `file-type` | 7 | 进 bundle，文件类型检测重复 |
| `commander` | 6 | CLI 工具，多为 devDep |
| `minimatch` | 6 | glob 匹配 |
| `chalk` / `execa` / `get-stream` / `supports-color` / `ansi-styles` | 各 5 | 终端工具链碎片化 |

**关键发现（进 bundle 的）：**

- **`@vue/compiler-*`（sfc/dom/ssr/core/shared）有 3.3.4 和 3.4.5 两套** —— Vue 编译器版本不统一，主依赖锁 `vue@3.3.4` 但某依赖拉入了 3.4.5
- **`axios` 有 0.26.1 和 1.6.5 两版** —— HTTP 客户端两份进 bundle
- **`sortablejs` 1.14.0 和 1.15.1** —— 与 D5.1 拖拽重叠叠加
- **`postcss` 5.2.18 和 8.4.33** —— 旧 postcss5 被某遗留依赖拖入

### 2.3 本节关键代码路径索引

| 路径 | 用途 |
|------|------|
| `jnpf-web-vue3/package.json` | 重叠库清单源 |
| `jnpf-web-vue3/pnpm-lock.yaml` | 多版本证据（lockfileVersion 6.0） |
| `pnpm why <pkg>` | 追溯多版本来源（复现命令见 §5） |

---

## 3. D7 CSS 健康（stylelint 全量）

### 3.1 命令与数据源

```bash
cd jnpf-web-vue3
npx stylelint "**/*.{vue,less,postcss,css,scss}" --formatter compact
```

### 3.2 汇总

| 指标 | 数值 |
|------|-----:|
| 涉及文件 | **205** |
| error | **2270** |
| warning | 7 |

### 3.3 规则命中分布

| 规则 | 次数 | 性质 |
|------|-----:|------|
| `rule-empty-line-before` | **1957** | 纯格式（86%），`stylelint --fix` 可一键修 |
| `alpha-value-notation` | 90 | 透明度写法 |
| `color-function-notation` | 80 | 颜色函数写法 |
| `import-notation` | 33 | @import 写法 |
| `color-hex-length` | 12 | hex 缩写 |
| `length-zero-no-unit` | 11 | `0px` → `0` |
| `declaration-block-no-duplicate-properties` | **11** | **重复属性（真 bug 源）** |
| `declaration-block-no-redundant-longhand-properties` | 10 | 可合并简写 |
| `no-duplicate-selectors` | **6** | **重复选择器（真冗余）** |
| 其余 | ~60 | 散布 |

**读法**：**86% 是空行格式债**，可 `pnpm lint:stylelint` 一键自动修复，零风险。真正需要人工看的 CSS 病症约 **300 处**（去格式后），其中 `no-duplicate-properties`(11) 和 `no-duplicate-selectors`(6) 是潜在渲染冲突源。

### 3.4 本节关键代码路径索引

| 路径 | 用途 |
|------|------|
| `npx stylelint "**/*.{vue,less,postcss,css,scss}" --formatter compact` | 全量违规明细（命令复现） |
| `jnpf-web-vue3/.stylelintrc` | 规则配置源 |

---

## 4. D4 Bundle 构成（构建时）

### 4.1 方法与诚实声明

| 项 | 状态 |
|----|------|
| `rollup-plugin-visualizer` stats.html | ⚠️ 已生成但 **version 2 格式无 size 字段**（701 chunks、0 个节点带 size），无法从中提取体积 |
| **改用 dist 实际文件大小** | ✅ 可靠——`vite build` 产物 `dist/` 的真实字节是 bundle 体积的**地面真相** |

> **方法纠错**：visualizer 的 stats.html 在本仓配置下只产结构树、不带体积（疑似 `rollup-plugin-visualizer` 与 vite4 的兼容问题）。改用 `du` 直接量 dist 文件，更准。证据：`.claude/evidence/frontend-ct/d4-bundle-stats.txt`。

### 4.2 总体积

| 指标 | 数值 |
|------|------|
| dist 总产物 | **36 MB** |
| JS（parsed） | **14.81 MB / 700 文件** |
| CSS | 1.16 MB |

### 4.3 头号肿瘤：`vendor-common` 7.8MB（占 JS 55%）

```
Top5 JS chunk (parsed):
  7985 KB  55.2%  vendor-common-634b552f.js   ← 头号肿瘤，未分包的巨型 vendor
  1529 KB  10.6%  index-416ad06d.js           ← 主入口
  1011 KB   7.0%  vendor-echarts-0853b75c.js  ← 图表库
   752 KB   5.2%  vendor-antd-383bb4a7.js     ← UI 库
   144 KB   1.0%  vendor-vue-0a5bbc88.js      ← 框架
```

**读法**：一个 `vendor-common` chunk 吃掉 55% 的 JS 体积，且**没有进一步分包**。这意味着任何页面首次加载都要拉这 7.8MB（gzip 后仍约 2-3MB）。结合 D5.1 的「echarts+highcharts 双图表库」「monaco 4MB」——这些大块头很可能全堆在 vendor-common 里未做路由级懒加载切分。

### 4.4 vendor 分包聚合

| vendor chunk | 体积 | 评估 |
|--------------|------|------|
| `vendor-common` | **7.8 MB** | 🔴 未拆分巨块，需排查内含哪些库并按路由懒加载 |
| `vendor-echarts` | 1.0 MB | 🟡 可按需引入 echarts 模块瘦身 |
| `vendor-antd` | 0.7 MB | 🟡 ant-design-vue 全量引入，可改按需 |
| `vendor-vue` | 0.1 MB | 🟢 正常 |

### 4.5 本节关键代码路径索引

| 路径 | 用途 |
|------|------|
| `.claude/evidence/frontend-ct/d4-bundle-stats.txt` | dist 实际体积完整统计 |
| `jnpf-web-vue3/build/vite/plugin/visualizer.ts` | visualizer 配置（stats.html 无 size 的原因） |
| `jnpf-web-vue3/dist/static/js/` | 真实 chunk 产物 |

---

## 5. 复现命令清单

```bash
cd D:\JNPF-v52\jnpf-web-vue3
EVD=../.claude/evidence/frontend-ct

# D6 死代码（原始输出大，结论已提炼进本文 §1；按需重跑）
npx knip --no-exit-code --reporter symbols

# D5 多版本依赖 → 结果已存 $EVD/d5-multiversion.txt（解析 pnpm-lock.yaml v6.0 packages 段）
pnpm why axios          # 追溯单个包的多版本来源

# D7 CSS（原始输出大，结论已提炼进本文 §3；按需重跑）
npx stylelint "**/*.{vue,less,postcss,css,scss}" --formatter compact

# D4 Bundle → 结果已存 $EVD/d4-bundle-stats.txt（需 ~5min + 8GB 内存）
REPORT=true npx vite build --mode production
```

---

## 6. 第一批静态扫描的整改优先级

| 波次 | 目标 | 收益 | 工作量 | 风险 |
|------|------|------|--------|------|
| **S-W1** | **拆 D4 `vendor-common`（7.8MB）**：排查内含库，按路由懒加载切分 | 省 3-5MB 首屏 JS | 中 | 中（需回归测试关键路由） |
| **S-W2** | 清 D6 死代码：104 文件 + 14 依赖 | 减小仓库 + bundle | 小 | 低（Knip 高置信，但需复核动态 import） |
| **S-W3** | 收敛 D5 重叠库：先定「图表用 echarts 还是 highcharts」「编辑器用 monaco 还是 codemirror」 | 省 1-4MB bundle | 中（需业务确认） | 中（替换有回归风险） |
| **S-W4** | `pnpm dedupe` 收敛 196 多版本包中的纯类型/工具类（type-fest 等） | node_modules 瘦身 | 小 | 低 |
| **S-W5** | `pnpm lint:stylelint` 自动修 1957 空行格式 | CSS 风格统一 | 极小（脚本） | 极低 |
| S-W6 | 人工修 D7 真病症（duplicate-properties 11 + duplicate-selectors 6） | 消除渲染冲突源 | 小 | 低 |

**排序逻辑**：S-W1（删死代码）零风险且让后续所有分析更准；S-W2（重叠库）收益最大但需业务决策；S-W4（CSS 格式）纯自动化放最后。

---

## 7. 与既有报告的关系

| 本文维度 | 既有报告覆盖? | 关系 |
|---------|:-----------:|------|
| D6 死代码 | ❌ | 全新——CT 报告只看「写错的代码」，不看「死代码」 |
| D5 重复依赖 | ❌ | 全新——CT 报告无依赖分析 |
| D7 CSS | 🟡 部分 | CT 报告有 `globalStyle`/`repeatedCss`（Vue 反模式角度）；本文是 stylelint（纯 CSS 语法角度） |
| D4 Bundle | ❌ | 全新 |

> 既有 `design-quality-diagnostics.md` §4.1 已列出「死代码（后端 fan-in=0）」方法——**本文 D6 是其前端等价物**（Knip 取代 fan-in 分析，因前端 ES module 与后端 DI 语义不同）。
