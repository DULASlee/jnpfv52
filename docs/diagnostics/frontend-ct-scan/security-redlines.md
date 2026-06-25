# 前端 CT 扫描 — 安全红线记录

> 记录日期: 2026-06-08
> 严重级别: P0 = 必须立即修复 / P1 = 本迭代修复 / P2 = 计划修复
> 状态: 全部待修复 (扫描阶段不中断)

---

## 红线 1: 加密密钥硬编码 (P0 — 严重)

**位置:** 三个项目源码

| 项目 | 文件 | 密钥类型 | 值 |
|---|---|---|---|
| jnpf-web-vue3 | `src/utils/cipher.ts` | AES Key | `'EY8WePvjM5GGwQzn'` |
| jnpf-web-vue3 | `src/utils/cache/storageCache.ts` | 存储加密 Key | `'_11111000001111@'` |
| jnpf-web-vue3 | `src/utils/cache/storageCache.ts` | 存储加密 IV | `'@11111000001111_'` |
| jnpf-web-datascreen | `src/utils/crypto.js` | AES Key | `"EY8WePvjM5GGwQzn"` |
| jnpf-web-datascreen | `src/utils/crypto.js` | DES Key | `"jMVCBsFGDQr1USHo"` |
| jnpf-app-vue3 | `utils/define.js` | AES CipherKey | `'EY8WePvjM5GGwQzn'` |

**影响:** 前端加密形同虚设。任何可访问前端源码/构建产物的人均可解密传输数据。AES Key `'EY8WePvjM5GGwQzn'` 在三项目中完全相同, 一旦泄露影响全部产品线。

**修复建议:**
- 密钥应从后端 API 动态获取 (如 `/api/oauth/getPublicKey`)
- 或通过环境变量注入 (`.env` + `VITE_CIPHER_KEY`)
- 至少每个部署实例使用不同密钥

---

## 红线 2: axios 0.19.0 已知 CVE (P0 — 严重)

**位置:** `jnpf-web-datascreen/package.json:18` — `"axios": "0.19.0"` (精确锁定, 无 ^)

**CVE:**
- CVE-2023-45857 — SSRF via absolute URL in baseURL (CVSS 6.5)
- CVE-2020-28168 — ReDoS in trim method (CVSS 5.9)
- 2019年8月发布，5年未更新

**修复:** 升级到 `axios ^1.7.0` (注意 breaking changes: 0.x → 1.x API 变更)

---

## 红线 3: CDN 脚本无 SRI 完整性校验 (P0 — 严重)

**位置:** `jnpf-web-datascreen/index.html`

加载了 10+ 个全局脚本，**全部缺少 `integrity` 属性:**

```html
<script src="/cdn/echarts/5.4.0/echarts.min.js"></script>
<script src="/cdn/jquery.min.js"></script>
<script src="/cdn/html2canvas/html2canvas.min.js"></script>
<script src="/cdn/staticfile/FileSaver.min.js"></script>
<script src="/cdn/staticfile/xlsx.full.min.js"></script>
<script src="/cdn/staticfile/jszip.min.js"></script>
<script src="/cdn/qrious.min.js"></script>
<script src="/cdn/echarts-wordcloud.min.js"></script>
<script src="/cdn/echarts-gl.min.js"></script>
```

**影响:** CDN 被劫持时，攻击者可注入任意恶意代码，窃取 token/用户数据/执行 XSS。

**修复:** 添加 `integrity="sha384-..."` 属性，或迁移到 npm 依赖 (tree-shakeable + 自动 SRI via lockfile)。

---

## 红线 4: datascreen 零认证零授权 (P0 — 严重)

**位置:** `jnpf-web-datascreen`

- 无登录页面、无认证流程
- 无路由守卫 (zero navigation guards)
- Token 通过 URL 查询参数明文传递: `?token=xxx`
- 编辑器路由 `/build` 无任何访问控制
- 屏幕密码为客户端校验 (数据已加载, 密码仅在 UI 层阻止渲染)

**影响:** 任何人知晓 URL 可访问大屏编辑器和所有已发布屏幕。URL 中的 token 被浏览器历史/日志/Referer header 泄露。

**修复:** 集成统一认证 (与 web-vue3 共享 token 验证), 添加路由守卫。

---

## 红线 5: localStorage 明文存储 Token (P1 — 高)

**位置:**
- `jnpf-web-datascreen`: `localStorage.setItem("token", token)` 明文
- `jnpf-app-vue3`: `uni.setStorageSync('token', token)` 明文

**对比:** jnpf-web-vue3 使用 AES 加密 localStorage (key `'_11111000001111@'`), 但仍然硬编码。

**影响:** XSS 攻击可读取明文 token。物理访问设备可提取 token。

---

## 红线 6: jQuery 在 Vue 3 项目中 (P1 — 高)

**位置:** `jnpf-web-datascreen/index.html:14` — `<script src="/cdn/jquery.min.js"></script>`

在现代 Vue 3 + Vite 项目中加载 jQuery (~87KB) 是完全冗余的。可能原因: 遗留代码或第三方依赖。jQuery 的全局 `$` 可能与浏览器 DevTools 或浏览器扩展冲突。

**修复:** 移除 jQuery 依赖, 用 Vue 响应式或原生 DOM API 替代。

---

## 红线 7: 双锁文件导致依赖不确定性 (P1 — 中)

**位置:**
- `jnpf-web-datascreen/`: `pnpm-lock.yaml` + `yarn.lock`
- `jnpf-app-vue3/`: `pnpm-lock.yaml` + `package-lock.json`

不同开发者使用不同包管理器会导致 `node_modules` 结构不一致, 产生"我机器上能跑"问题。

**修复:** 删除多余锁文件, 在 `package.json` 中设置 `"packageManager": "pnpm@8.x"`, 添加 `.npmrc` 强制包管理器。

---

## 红线 8: 依赖声明严重缺失 (P1 — 中)

**位置:** `jnpf-app-vue3/package.json` — 仅声明 `crypto-js` 和 `sass` 两个依赖

实际使用的 50+ 依赖 (vue, pinia, vue-i18n, vk-uview-ui, uni-ui, 等) 全部隐式依赖。`pnpm` 的严格模式下这些依赖不可用。

**修复:** 补全所有实际使用的依赖到 package.json。

---

## 红线 9: `import.meta.globEager` 已废弃 (P2 — 低)

**位置:** `jnpf-web-datascreen/src/echart/index.js` 和 `src/components/index.js`

`import.meta.globEager` 是 Vite 2.x API, Vite 3+ 已废弃并移除。当前 Vite 4.4.6 仍支持但会在未来版本移除。

**修复:** 迁移到 `import.meta.glob('**/*.vue', { eager: true })`。

---

## 红线 10: eval() 用于动态权限 (P2 — 中)

**位置:** `jnpf-app-vue3/utils/jnpf.js` — `getScriptFunc(str)` 使用 `eval()` 执行按钮启用/禁用脚本

```javascript
getScriptFunc(str) {
    // #ifdef MP
    return false;  // 小程序禁用
    // #endif
    return new Function('return ' + str)();  // 等价于 eval
}
```

**影响:** 如果后端返回的脚本字符串被篡改, 可执行任意代码。虽然当前小程序已禁用, H5/APP 仍存在风险。

**修复:** 使用沙箱解释器或预定义函数映射替代动态 eval。

---

## 总结

| 红线 | 项目 | 严重度 | 类型 |
|---|---|---|---|
| 1. 加密密钥硬编码 | 三项目 | P0 | 密钥泄露 |
| 2. axios 0.19.0 CVE | datascreen | P0 | 已知漏洞 |
| 3. CDN 脚本无 SRI | datascreen | P0 | 供应链攻击 |
| 4. 零认证零授权 | datascreen | P0 | 未授权访问 |
| 5. Token 明文存储 | datascreen, app | P1 | 凭证泄露 |
| 6. jQuery + Vue3 混用 | datascreen | P1 | 安全/性能 |
| 7. 双锁文件 | datascreen, app | P1 | 供应链 |
| 8. 依赖声明缺失 | app | P1 | 构建可靠性 |
| 9. globEager 废弃 | datascreen | P2 | 兼容性 |
| 10. eval() 动态执行 | app | P2 | 代码注入 |

**修复计划:** 等全部 15 份扫描报告完成 + 架构师系统性分析后, 统一安排修复优先级与分阶段计划。
