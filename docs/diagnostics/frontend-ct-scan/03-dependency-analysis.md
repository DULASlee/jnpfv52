# CT Scan 1.3: 依赖树分析报告

> 扫描日期: 2026-06-08
> 扫描范围: jnpf-web-vue3 / jnpf-web-datascreen / jnpf-app-vue3
> 分析维度: 版本健康度 / 安全漏洞 / 包体积 / 跨项目共享

---

## 一、版本健康度总览

### 1.1 jnpf-web-vue3

| 状态 | 数量 | 代表包 |
|---|---|---|
| 🔴 严重过时 (major落后2+) | 2 | tinymce 5.10.7 (当前7.x), codemirror 5.65.12 (当前6.x) |
| 🟡 过时 (major落后1) | 5 | vue-tsc 1.6.5 (当前2.x), @typescript-eslint/* 5.x (当前8.x), eslint 8.37 (当前9.x) |
| 🟢 最新 | 多数 | vue 3.3.4, pinia 2.1.3, ant-design-vue 3.2.20 |

**精确锁定 (无 ^/~):**
- `vue: 3.3.4` — 精确锁定 (刻意)
- `@vue/runtime-core: 3.3.4` — 精确锁定
- `@vue/shared: 3.3.4` — 精确锁定
- `vue-simple-uploader: 1.0.0` — 精确锁定
- `lint-staged: 13.2.0` — 精确锁定

**安全扫描:**
- `terser 5.14.2` 作为 dependency (非 devDep!) — 应移至 devDependencies
- ImageSharp 漏洞已修复 (后端, 见 security/imagesharp-upgrade.md)
- 前端 npm audit 待执行 (需 `pnpm audit`)

### 1.2 jnpf-web-datascreen

| 状态 | 数量 | 代表包 |
|---|---|---|
| 🔴 严重过时 (安全风险) | 3 | **axios 0.19.0** (2019年, 大量CVE), dayjs 1.10.6, sass 1.37.5 |
| 🟡 过时 | 5 | vite 4.4.6 (当前6.x), vue-router 4.1.5, @vitejs/plugin-vue 4.2.3 |
| 🟢 最新 | 少数 | vue 3.4.27, element-plus 2.7.5 |

**特别关注:**
- **axios 0.19.0**: 发布于 2019年8月，存在已知 SSRF 和 ReDoS 漏洞 (CVE-2023-45857, CVE-2020-28168)
- **sass 1.37.5**: 嵌入式版本，当前 1.77+
- CDN 脚本版本硬编码在 index.html，无法享受 npm audit 保护

### 1.3 jnpf-app-vue3

| 状态 | 数量 | 代表包 |
|---|---|---|
| 🔴 严重问题 | N/A | package.json 仅声明 2 个依赖, 无法评估真实依赖树 |
| 🟡 隐式依赖 | 50+ | vk-uview-ui, uni-ui, pinia, vue-i18n 等未声明 |

---

## 二、安全隐患

### 2.1 已知漏洞 (评估)

| 包 | 版本 | 已知CVE | 严重度 | 项目 |
|---|---|---|---|---|
| axios | 0.19.0 | CVE-2023-45857 (SSRF), CVE-2020-28168 (ReDoS) | **高** | datascreen |
| sass (embedded) | 1.37.5 | 多个 embedded 相关漏洞已修复 | 中 | datascreen |
| tinymce | 5.10.7 | 5.x 已停止安全更新 | 中 | web-vue3 |

### 2.2 硬编码密钥 (严重)

**文件:** `jnpf-web-datascreen/src/utils/crypto.js`

```javascript
// 行内硬编码 - 任何人可读取
const aesKey = "EY8WePvjM5GGwQzn"
const desKey = "jMVCBsFGDQr1USHo"
```

**影响:** AES 和 DES 加密密钥暴露在源码中，前端加密形同虚设。任何可访问前端构建产物的人均可解密传输数据。

**修复建议:** 密钥应来自后端 API 或环境变量，前端不应持有加密密钥。

### 2.3 CDN 脚本完整性

jnpf-web-datascreen 的 index.html 通过 `<script>` 加载 10+ 个外部脚本，**均未使用 `integrity` 属性** (Subresource Integrity)。CDN 被劫持时，可注入任意恶意代码。

---

## 三、包体积分析

### 3.1 依赖体积估算 (未压缩)

| 项目 | dependencies 数 | devDependencies 数 | 估算 node_modules |
|---|---|---|---|
| jnpf-web-vue3 | 54 | 66 | ~800MB+ |
| jnpf-web-datascreen | 22 | 8 | ~400MB |
| jnpf-app-vue3 | **2** (声明) | 0 | ~500MB (uni_modules) |

### 3.2 体积大户 (jnpf-web-vue3)

| 包 | 估算大小 | 说明 |
|---|---|---|
| monaco-editor | ~15MB | 代码编辑器, 已做 manual chunk |
| tinymce | ~8MB | 富文本编辑器, 已做 manual chunk |
| echarts | ~6MB | 图表库 |
| ant-design-vue | ~5MB | UI 框架, 已做 manual chunk |
| @logicflow/core + extension | ~3MB | 流程图 |
| highcharts | ~2MB | 图表库 (与 ECharts 功能重叠!) |
| @fullcalendar/* | ~2MB | 日历 |

**体积优化措施 (已实施):**
- Manual chunk 拆分: vendor-vue, vendor-antd, vendor-tinymce, vendor-monaco, vendor-codemirror
- CDN 模式可选 (bootcdn, 默认关闭)
- Gzip/Brotli 压缩 (build only)
- 图片压缩 (vite-plugin-imagemin)
- PWA 可选 (默认关闭)

**冗余问题:**
- **ECharts + Highcharts 并存** (~8MB 图表库双份) — 应评估是否可统一
- **Monaco + CodeMirror + TinyMCE + Vditor** (~30MB 编辑器四份) — 应评估使用场景
- `terser` 错误放置在 `dependencies` 而非 `devDependencies`

### 3.3 jnpf-web-datascreen 体积隐患

- ECharts 通过 CDN 全局加载 (~3MB, 无 tree-shaking, 全部 echarts+echarts-gl+echarts-wordcloud)
- Monaco Editor 0.34.1 (~5MB)
- jQuery (~87KB) 在现代 Vue3 项目中完全冗余

---

## 四、跨项目依赖共享: 零

### 4.1 可共享却各自独立维护的代码

| 功能 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 | 共享潜力 |
|---|---|---|---|---|
| HTTP 封装 | VAxios (utils/http/axios/) | axios.js (src/axios.js) | request.js (utils/request.js) | **高** |
| 加密工具 | crypto-js 4.1.1 | crypto-js 4.1.1 + 硬编码密钥 | crypto-js 4.2.0 | **高** |
| 权限检查 | permission store + 指令 | — | libs/permission.js | 中 |
| Token 管理 | utils/auth/ | localStorage | uni Storage | 中 |
| 国际化 | 21 files | 1 file (仅配置) | 6 files (691 keys × 3) | 低 (内容不同) |
| WebSocket | reconnecting-websocket | — | chat.js (uni.connectSocket) | 低 (平台差异) |

### 4.2 依赖版本不一致

| 包 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 |
|---|---|---|---|
| vue | 3.3.4 (pinned) | 3.4.27 | UniApp Vue 3 |
| vue-router | 4.2.1 | 4.1.5 | — (pages.json) |
| pinia | 2.1.3 | — (store2) | ✅ (隐式) |
| axios | 1.4.0 | 0.19.0 | — (uni.request) |
| crypto-js | 4.1.1 | 4.1.1 | 4.2.0 |
| dayjs | 1.11.7 | 1.10.6 | — |
| vue-i18n | 9.2.2 | 9.1.9 | ✅ (隐式) |

**结论:** 即使使用相同包，版本也不一致。Vue 主版本差异 (3.3.4 vs 3.4.27) 可能在共享组件时产生兼容性问题。

---

## 五、包管理器分析

| 项目 | 锁文件 | 问题 |
|---|---|---|
| jnpf-web-vue3 | pnpm-lock.yaml | ✅ 干净 |
| jnpf-web-datascreen | pnpm-lock.yaml + yarn.lock | ❌ 双锁文件 |
| jnpf-app-vue3 | pnpm-lock.yaml + package-lock.json | ❌ 双锁文件 |

**风险:** 双锁文件意味着不同开发者可能使用不同包管理器，导致 `node_modules` 结构不一致，产生"我机器上能跑"类问题。

---

## 六、修复优先级

### P0 (本周)
1. **datascreen: 移除硬编码密钥** — 安全红线
2. **datascreen: 升级 axios 0.19.0 → 1.x** — CVE 修复
3. **datascreen: CDN 脚本添加 integrity 属性**

### P1 (本月)
4. **app: 补全 package.json** — 声明所有实际依赖
5. **datascreen + app: 统一包管理器为 pnpm** — 删除多余锁文件
6. **web-vue3: 移动 terser 到 devDependencies**
7. **web-vue3: 评估 ECharts+Highcharts 双图表库必要性**

### P2 (长期)
8. **建立跨项目共享包** — 抽取 HTTP/加密/权限到 `@jnpf/shared`
9. **统一依赖版本** — 通过 workspace 或 renovate 管理
10. **datascreen: ECharts 从 CDN 迁移到 npm** — 实现 tree-shaking
