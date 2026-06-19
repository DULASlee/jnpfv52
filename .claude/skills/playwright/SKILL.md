---
name: playwright
description: 浏览器端到端验证技能。当需要打开浏览器、对前端变更做 E2E 验证、产出 Supreme Iron Law 要求的 E1 截图证据时触发。JNPF 前端验收的唯一证据来源。
---

# Playwright E2E 验证技能

> **Supreme Iron Law 强制使用本技能。** 任何前端实质性变更（.vue/.ts/.tsx/.js/.jsx）MUST 产出 E1/E2/E3 三项证据，否则 `guard-finish.mjs` BLOCK。

## 能力边界

- ✅ 打开浏览器访问 JNPF 前端（默认 `http://localhost:3100`）
- ✅ 登录（admin / 123456 seed 数据）
- ✅ 操作页面并截图至 `.claude/evidence/`
- ✅ 描述实际 UI 状态（E3 证据）
- ❌ 不用于后端测试、不用于性能基准

---

## 前置检查

每次使用前，验证 dev server 在跑：

```bash
# 检查 3100 端口（前端）
curl -s -o /dev/null -w "%{http_code}" http://localhost:3100 || echo "DOWN"
# 期望：200 或 302。返回 000 / DOWN → 先跑 /start-dev
```

若端口不通 → 停止，提示用户执行 `/start-dev`，**不要自己起 dev server**（违反 CLAUDE.md 铁律）。

---

## 三种使用模式

### 模式 A：临时一次性验证（最常用）

当只是要"打开浏览器看一眼并截图"，**不要**写 spec 文件，直接用 Playwright API 跑一次性脚本：

```bash
node -e "
const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  await page.goto('http://localhost:3100');
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: '.claude/evidence/page-home.png', fullPage: false });
  console.log('E1 截图已产出: .claude/evidence/page-home.png');
  await browser.close();
})();
"
```

**登录 + 操作示例：**

```bash
node -e "
const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  await page.goto('http://localhost:3100/#/login');
  await page.fill('input[placeholder*=\"账号\"]', 'admin');
  await page.fill('input[type=\"password\"]', '123456');
  await page.click('button:has-text(\"登录\")');
  await page.waitForURL('**/workStation/**', { timeout: 15000 }).catch(() => {});
  await page.screenshot({ path: '.claude/evidence/login-success.png' });
  console.log('E1 截图已产出: .claude/evidence/login-success.png');
  await browser.close();
})();
"
```

### 模式 B：可复用 spec（回归测试用）

当某个流程需要反复验证（如 PR 回归），写 spec 到 `e2e/` 目录：

```typescript
// e2e/smoke.spec.ts
import { test, expect } from '@playwright/test';

test('登录页可访问', async ({ page }) => {
  await page.goto('/#/login');
  await expect(page.locator('button:has-text("登录")')).toBeVisible();
  await page.screenshot({ path: '.claude/evidence/smoke-login.png' });
});
```

运行：`npx playwright test e2e/smoke.spec.ts`

### 模式 C：有头调试（看实际渲染）

```bash
# 加 headed 参数弹出浏览器窗口
node -e "const{chromium}=require('playwright');(async()=>{const b=await chromium.launch({headless:false});const p=await b.newPage();await p.goto('http://localhost:3100');await p.waitForTimeout(5000);await b.close();})()"
```

---

## E1/E2/E3 证据产出规范（Supreme Iron Law）

每次前端验证 MUST 产出三项，缺一即判定未通过验收：

| 证据 | 产出方式 | 示例 |
|---|---|---|
| **E1 截图** | `page.screenshot({ path: '.claude/evidence/<场景>.png' })` | `.claude/evidence/login-success.png` |
| **E2 操作路径** | 在 Step 7 报告中文字描述步骤 | "打开 /login → 输入 admin/123456 → 点登录 → 跳转 /workStation" |
| **E3 实际输出** | 在 Step 7 报告中描述浏览器实际看到的 | "页面显示工作台仪表盘，左上角显示用户名 admin" |

**截图命名规范：** `<场景>-<状态>.png`，如 `user-list-after-delete.png`、`form-validation-error.png`。

---

## 常见陷阱

1. **选择器失效** → JNPF 用 Ant Design Vue，优先用 `:has-text()`、`[placeholder]`，避免脆弱的 `nth-child`
2. **登录态丢失** → 每个 spec 用 `test.use({ storageState })` 或重新登录
3. **dev server 没起** → 必须先 `/start-dev`，本技能不自动启停
4. **截图为空（0 字节）** → `guard-finish.mjs` 会因文件 <5KB 拒绝；确保 `waitForLoadState` 后再截图
5. **路由 hash 模式** → JNPF 前端是 hash 路由，URL 是 `http://localhost:3100/#/login`，不是 `/login`

---

## 验证本技能可用

```bash
# 确认 playwright + 浏览器就绪
node -e "const{chromium}=require('playwright');chromium.launch().then(b=>{console.log('OK: chromium 可启动');return b.close();}).catch(e=>{console.error('FAIL:',e.message);process.exit(1);})"
```

预期输出：`OK: chromium 可启动`。失败 → `npx playwright install chromium`。
