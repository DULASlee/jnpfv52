# 前端运行时扫描 — 可访问性（D8）+ 渲染性能（D9）

> **日期**：2026-08-06  
> **范围**：`jnpf-web-vue3` dev server（`:3100`）+ backend（`:5000`），真实浏览器渲染  
> **父文档**：[`design-quality-diagnostics.md`](design-quality-diagnostics.md)  
> **姊妹篇**：[`design-quality-frontend-ct-report.md`](design-quality-frontend-ct-report.md)（静态 Vue 反模式）· [`design-quality-frontend-static-deep.md`](design-quality-frontend-static-deep.md)（静态死代码/依赖/CSS/Bundle）  
> **证据目录**：`.claude/evidence/frontend-ct/`  
> **定位**：前两份报告是**静态扫描**（看源码），本文是**运行时扫描**（看真实 DOM 渲染 + 浏览器性能）——这是静态分析永远看不到的两个维度

---

## 0. 两维度定位

| 维度 | 工具 | 性质 | 一句话结论 |
|------|------|------|-----------|
| **D8 可访问性** | axe-core（via Playwright） | 运行时 DOM | **3 critical + 5 serious**，跨 3 页面；叠加静态预扫的 293 处键盘不可达 = a11y 系统性缺失 |
| **D9 渲染性能** | PerformanceObserver（LCP/长任务/堆） | 运行时 | LCP 健康（max 1020ms）；但 home/workStation 各有 ≥2 个超 100ms 长任务阻塞主线程 |

> **为什么运行时和静态都要做**：静态预扫发现 293 处 `@click` 绑在 div 上（源码层）；运行时 axe 发现实际渲染后还有 `image-alt`/`color-contrast`/`aria-required-children`（DOM 层）。两者互补，单做任一都会漏。

---

## 1. D8 可访问性（axe-core 运行时）

### 1.1 命令与数据源

```bash
cd e2e
JNPF_WEB_URL=http://127.0.0.1:3100 npx playwright test admin/d8-a11y-scan.spec.ts
# 证据：.claude/evidence/frontend-ct/d8-axe-{login,authed-home,authed-workStation}.json
```

> **扫描覆盖**：登录页（无需登录）+ 首页 home + 工作台 workStation（UI 登录后）。规则集：wcag2a / wcag2aa / wcag21a / wcag21aa。

### 1.2 跨页面违规汇总（8 类，按严重度）

| 规则 | 严重度 | 总处数 | 出现页面 | 含义 |
|------|--------|------:|---------|------|
| `aria-roles` | 🔴 critical | 2 | login | `role="bar"`/`role="spinner"` 不是合法 ARIA 角色 |
| `aria-required-children` | 🔴 critical | 2 | home, workStation | ARIA 容器缺少必需子角色（如 tablist 缺 tab） |
| `image-alt` | 🔴 critical | 4 | home, workStation | 图片无 alt 文本（WCAG 1.1.1） |
| `html-lang-valid` | 🟠 serious | 3 | 全部 3 页面 | `<html lang="zh_CN">` 下划线应为连字符 `zh-CN` |
| `aria-tab-name` | 🟠 serious | 3 | home, workStation | Tab 节点无可访问名称 |
| `color-contrast` | 🟠 serious | 3 | home, workStation | 文字/背景对比度不足（WCAG 1.4.3） |
| `scrollable-region-focusable` | 🟠 serious | 1 | home | 可滚动区域键盘不可达 |
| `meta-viewport` | 🟡 moderate | 3 | 全部 3 页面 | `user-scalable=0` 禁用缩放（WCAG 1.4.4） |

### 1.3 静态预扫补充（运行时 axe 看不到的源码层）

> 来源：`.claude/evidence/frontend-ct/d8-static-prescan.txt`

| WCAG | 反模式 | 处数 |
|------|--------|-----:|
| 2.1.1 键盘 | `@click` 绑在 div/span/i/em/p/li（键盘不可达） | **293** |
| 1.1.1 非文本 | `<img>` 无 alt | 75 |
| 4.1.2 名称 | a11y 基础设施密度：`aria-label` **0 处**、`tabindex` **0 处**、`role` 仅 3 处 | — |

### 1.4 一个"运行时实测"的 a11y 旁证

扫描过程中发现：**登录按钮文字是"登 录"（中间有空格）**，导致 `getByRole('button', {name: /登录/})` 匹配失败。这是 ant-design 中文字间距渲染。但对屏幕阅读器而言，这种"看似有文字但语义断裂"的按钮，正是 **WCAG 4.1.2** 的灰色地带——自动化工具都难以稳定定位，辅助技术用户更易困惑。

---

## 2. D9 渲染性能（PerformanceObserver 运行时）

### 2.1 命令与数据源

```bash
cd e2e
JNPF_WEB_URL=http://127.0.0.1:3100 npx playwright test admin/d9-render-perf.spec.ts
# 证据：.claude/evidence/frontend-ct/d9-perf-results.json
```

### 2.2 三路由性能明细

| 路由 | LCP (ms) | TTFB | DOM 交互 | 长任务数 | 超100ms | 长任务总时 | 堆 (MB) |
|------|---------:|-----:|--------:|--------:|-------:|----------:|--------:|
| login | 76 🟢 | 7 | 44 | 2 | **1** | 182 | 30 |
| home | 1020 🟢 | 10 | 33 | 2 | **2** | 276 | 30 |
| workStation | 932 🟢 | 6 | 26 | 3 | **3** | 375 | 30 |

> **LCP 阈值**：好 <2500 / 需改进 2500-4000 / 慢 >4000。三路由全部 🟢 健康。

### 2.3 读法

- **好消息**：LCP 和 TTFB 都健康（本地 dev server，无网络延迟）。JS 堆 30MB 合理（未泄漏迹象）。
- **关注点**：home 和 workStation 各有 2-3 个**超 100ms 的长任务**阻塞主线程。虽然不致命，但影响交互响应（INP）。结合 CT 报告的 `vendor-common` 7.8MB 巨块——长任务很可能来自这个未分包 vendor 的初始化解析。
- **局限**：这是 dev 模式（无压缩、有 HMR 开销），生产 LCP 会更优；但长任务的"数量和模式"在 dev/prod 一致（都是 JS 解析+组件挂载）。

---

## 3. 第二批整改优先级

| 波次 | 目标 | 收益 | 工作量 | 依据 |
|------|------|------|--------|------|
| **R-W1** | 修 3 处全页面违规：`html lang="zh_CN"→"zh-CN"`、`meta viewport` 去 `user-scalable=0` | 全站 a11y 基线提升 | 极小（2 行） | D8 |
| **R-W2** | 修 critical：`role="bar"/"spinner"` 改合法值、图片加 alt、tablist 补子角色 | 消除阻断性 a11y 缺陷 | 小 | D8 |
| **R-W3** | 治本：293 处 `@click` 绑 div → 改 `<button>` 或加 `role/tabindex` | 键盘可访问性 | 中（横切） | D8 静态 |
| R-W4 | 排查 home/workStation 的长任务来源（疑似 vendor-common 初始化） | 改善 INP | 中 | D9 + D4 |

---

## 4. 复现命令

```bash
cd D:\JNPF-v52\e2e

# 前置：dev server :3100 + backend :5000 已起（start-dev.ps1）

# D8 a11y
JNPF_WEB_URL=http://127.0.0.1:3100 npx playwright test admin/d8-a11y-scan.spec.ts --reporter=list

# D9 性能
JNPF_WEB_URL=http://127.0.0.1:3100 npx playwright test admin/d9-render-perf.spec.ts --reporter=list

# 注意：登录按钮文字是"登 录"(带空格)，选择器需用 /登\s*录/
```

---

## 5. 本节关键路径索引

| 路径 | 用途 |
|------|------|
| `.claude/evidence/frontend-ct/d8-axe-{login,authed-home,authed-workStation}.json` | axe 运行时违规明细 |
| `.claude/evidence/frontend-ct/d8-static-prescan.txt` | 静态 a11y 预扫 |
| `.claude/evidence/frontend-ct/d9-perf-results.json` | 渲染性能数据 |
| `e2e/admin/d8-a11y-scan.spec.ts` | D8 扫描脚本 |
| `e2e/admin/d9-render-perf.spec.ts` | D9 采集脚本 |
| `e2e/helpers/login.ts` | 登录 helper（"登 录"空格问题） |
