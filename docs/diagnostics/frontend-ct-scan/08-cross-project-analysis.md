# CT Scan 4: 跨项目分析与反模式诊断

> 扫描日期: 2026-06-08
> 扫描范围: 三项目横向对比 + 代码审查
> 数据来源: Day 1-3 全部扫描结果

---

## 一、跨项目代码共享分析

### 1.1 共享程度: 零

三个项目之间**没有任何共享代码**。无 monorepo tooling、无共享包、无 Git submodule、无 npm workspace。

### 1.2 独立实现的相同功能

| 功能 | jnpf-web-vue3 实现 | jnpf-web-datascreen 实现 | jnpf-app-vue3 实现 | 代码行数浪费 |
|---|---|---|---|---|
| **HTTP 封装** | VAxios 类 (utils/http/axios/) ~500行 | axios.js ~96行 | request.js ~116行 | ~700行重复 |
| **Token 管理** | Persistent 加密双层缓存 ~200行 | localStorage.getItem("token") ~3行 | uni.setStorageSync ~30行 | ~230行重复 |
| **密码加密** | MD5 → AES-ECB (cipher.ts) | — | MD5 → AES-ECB (login page 内联) | ~50行重复 |
| **权限检查** | hasBtnP/hasColumnP/hasFormP + v-auth 指令 ~80行 | — | hasBtnP/hasP/hasFormP (libs/permission.js) ~143行 | ~80行重复 |
| **日期格式化** | dayjs 1.11.7 | dayjs 1.10.6 | — (手动) | 版本不一致 |
| **加密** | crypto-js 4.1.1 → cipher.ts | crypto-js 4.1.1 → crypto.js (不同实现) | crypto-js 4.2.0 | 三份不同封装 |

### 1.3 共享可行性评估

| 候选共享模块 | 技术可行性 | 工作量 | 价值 |
|---|---|---|---|
| HTTP 请求封装 | 中 (平台API差异) | 3天 | 高 |
| 加密工具 | **高** (纯算法) | 1天 | **高** |
| Token 管理 | 中 (存储API差异) | 2天 | 高 |
| 权限检查 | **高** (纯逻辑) | 1天 | **高** |
| 日期工具 | **高** | 0.5天 | 中 |
| 常量/枚举 | **高** | 0.5天 | 中 |

---

## 二、jnpf-web-datascreen 专项分析

### 2.1 架构全景

datascreen 是一个**混合式**项目:
- **独立 SPA** (端口 3102): 大屏管理后台 (列表/编辑器/查看器)
- **UMD 库** (lib 模式): 可嵌入第三方页面的 `<avue-data>` 组件
- **CDN 依赖**: ECharts/jQuery/html2canvas 通过全局脚本加载

### 2.2 特殊架构模式

**组件工厂模式 (echart 自动发现):**
```javascript
const modules = import.meta.globEager('./packages/**/*.vue')
// 自动注册为 avue-echart-{name} 全局组件
```

**配置驱动架构:**
- `public/config.js` (~2400行) 是所有组件的配置中心
- `baseList` 数组定义每个图表的默认属性/样式/数据
- 组件 + 配置面板 + 配置数据 = 完整的可视化组件

**代理转发模式:**
```javascript
// axios.js — 将跨域请求包装为服务端代理
if (config.headers.proxy) {
  config.url = '/visual/proxy'
  config.method = 'post'
  config.data = { url, method, headers, params }
}
```

### 2.3 严重问题

| 问题 | 严重度 | 说明 |
|---|---|---|
| 零认证零授权 | P0 | 安全红线 #4 |
| `public/config.js` ~2400行 | P0 | 巨型配置文件, 无法维护 |
| jQuery + Vue3 混用 | P1 | 安全红线 #6 |
| `import.meta.globEager` 废弃 | P2 | Vite API 兼容性 |
| CDN 目录混乱 | P1 | 多版本并存 (axios 1.0.0+1.3.6, vuex 2.4.1+3.1.1) |
| 无 TypeScript | P1 | 纯 JS, 2400行 config 无类型保护 |
| window 全局状态 | P1 | 无封装, 任意修改 |

### 2.4 与 web-vue3 的架构差距

datascreen 与 web-vue3 的架构成熟度差距约 **3 年**:
- web-vue3: Pinia + TypeScript + VAxios + 8层守卫 + 加密存储
- datascreen: window 全局 + 纯 JS + Axios 0.19 + 零守卫 + 明文存储

---

## 三、jnpf-app-vue3 专项分析

### 3.1 多平台复杂度

UniApp 的"一次编写多端运行"导致:
- **条件编译散落**: `#ifdef VUE3` / `#ifdef H5` / `#ifdef APP-PLUS` / `#ifdef MP-WEIXIN` 遍布代码
- **隐式代码路径**: 同一个文件在不同平台编译出不同代码
- **存储抽象泄露**: `uni.setStorageSync` 在 H5=localStorage, APP=SQLite, MP=wx.setStorageSync
- **HTTP 抽象泄露**: `uni.request` vs web-vue3 的 Axios

### 3.2 包管理异常

- `package.json` 仅声明 2 个依赖 (crypto-js, sass)
- 实际使用 50+ 依赖全部隐式 (由 HBuilder X IDE 或 UniApp CLI 注入)
- 无法在标准 CI 中 `pnpm install && pnpm build`
- 构建完全依赖 HBuilder X IDE

### 3.3 Vue 2 兼容包袱

`main.js` 中保留完整的 `#ifndef VUE3` 分支 (Vue 2 代码), 增加认知负担和维护成本。由于 `manifest.json` 已声明 `vueVersion: "3"`, Vue 2 分支为死代码。

### 3.4 三套 UI 库共存

```
vk-uview-ui (90+ components) — 第三方社区库
uni-ui (47 modules)            — DCloud 官方
Jnpf (49 components)           — JNPF 自研
```

超过 186 个 UI 组件共存，部分功能重叠，包体积难以控制。

---

## 四、反模式清单

### 4.1 架构反模式

| 反模式 | 位置 | 说明 |
|---|---|---|
| **God Config** | datascreen: public/config.js (~2400行) | 单体配置文件, 不易分割 |
| **全局可变状态** | datascreen: window.$glob, window.$website | 无封装, 无类型, 无约束 |
| **隐式依赖** | app: package.json 不完整 | 构建依赖未声明 |
| **双锁文件** | 三项目 | 包管理器不统一 |
| **Vue 2 死代码** | app: main.js | 条件编译保留 Vue 2 分支 |
| **Store 耦合 Router** | web-vue3: userStore 直接调用 router.push() | 违反单向数据流 |
| **Getter 副作用** | web-vue3: getUserInfo getter 回退 localStorage | 不纯的 getter |
| **重复状态存储** | web-vue3: backMenuList === backRouterList | 相同数据存两次 |
| **废弃 API** | datascreen: import.meta.globEager | Vite 2.x API |

### 4.2 代码反模式

| 反模式 | 位置 | 说明 |
|---|---|---|
| **硬编码密钥** | 三项目 | AES Key `'EY8WePvjM5GGwQzn'` |
| **硬编码URL/端口** | app: define.js | localhost:5000 写死 |
| **拼写错误** | app: base.js:96 | `data.dictionaryList.fliter()` (应为 `filter`) |
| **eval() 动态执行** | app: jnpf.js | new Function() 执行动态脚本 |
| **客户端密码校验** | datascreen: container.vue | 屏幕密码仅 UI 层拦截, 数据已加载 |
| **无错误边界** | datascreen, app | 无全局 errorHandler |
| **console.log 残留** | 待扫描 | 生产代码中可能残留 |

### 4.3 性能反模式

| 反模式 | 位置 | 说明 |
|---|---|---|
| **全量 CDN 加载** | datascreen: index.html | ECharts+jQuery+html2canvas 全量加载 |
| **monaco-editor 全量** | datascreen | ~5MB, 仅一处使用 |
| **四编辑器共存** | web-vue3 | Monaco+CodeMirror+TinyMCE+Vditor |
| **双图表库** | web-vue3 | ECharts+Highcharts |
| **全局组件注册** | web-vue3 | 618 组件全注册, 无 tree-shake |
| **无路由懒加载** | datascreen | 所有路由同步导入 |
| **emoji GIF 打包** | app | 200 个 GIF 表情内嵌 |

### 4.4 安全反模式

| 反模式 | 说明 |
|---|---|
| Token 明文存储 (datascreen, app) | localStorage 无加密 |
| Token 在 URL 中传递 (datascreen) | 浏览器历史/日志/Referer 泄露 |
| 无 CSP 头 | 三个项目均未配置 Content-Security-Policy |
| 无 SRI 校验 (datascreen) | CDN 脚本无 integrity 属性 |
| 客户端加密密钥 | 硬编码 AES 密钥, 前端加密形同虚设 |

---

## 五、跨项目共性问题

### 5.1 三个项目共享的问题

1. **均无测试**: 0% 测试覆盖率
2. **均无 Token 刷新**: 过期直接踢出
3. **加密密钥相同**: `'EY8WePvjM5GGwQzn'` 三项目共享
4. **API 契约隐式耦合**: 依赖后端特定 code 值 (600/601/602) 但无文档
5. **无 API 类型生成**: 后端 OpenAPI/Swagger 未用于前端类型生成

### 5.2 缺乏统一的前端平台

- 无组件库共享机制
- 无工具函数共享包
- 无统一 lint 配置
- 无统一构建配置
- 无统一部署流程

---

## 六、技术债务量化估算

| 类别 | 项目 | 估计工作量 |
|---|---|---|
| 安全红线修复 (P0) | datascreen | 3天 |
| 安全红线修复 (P0) | app | 1天 |
| TypeScript 迁移 | datascreen | 10天 |
| TypeScript 迁移 | app | 15天 |
| 测试基线建设 | web-vue3 | 5天 |
| 测试基线建设 | datascreen | 3天 |
| 测试基线建设 | app | 5天 |
| 共享包抽取 | 跨项目 | 15天 |
| CI/CD 完善 | 跨项目 | 5天 |
| Lint/Format 补齐 | datascreen, app | 3天 |
| 代码清理 (Vue2死代码等) | app | 2天 |
| **合计** | | **67天** |

---

## 七、重构优先级矩阵

```
高价值 + 低成本 → Phase 1 立即执行
├── 删除双锁文件
├── 修复 CI Lint 门禁
├── 删除 Vue 2 死代码 (app)
├── 补全 app package.json
├── 为 datascreen+app 添加 ESLint/Prettier基础配置

高价值 + 高成本 → Phase 2 重点投入
├── 抽取共享工具包 (@jnpf/shared)
├── datascreen 安全加固 (认证+授权)
├── app TypeScript 迁移
├── web-vue3 编辑器统一 (Monaco/CodeMirror/TinyMCE → 保留1-2个)

低价值 + 低成本 → Phase 3 随Sprint推进
├── 修复拼写错误 (fliter → filter)
├── 清理 console.log 残留
├── 升级 Prettier 废弃配置

低价值 + 高成本 → 观察后决定
├── datascreen jQuery 移除 (如无实际使用)
├── web-vue3 Highcharts → ECharts 迁移 (如Highcharts无特殊功能)
```
