# Admin 头像下拉系统切换 + 菜单 bug 修复 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 admin 头像下拉菜单增加"切换系统"入口（复用 SystemTriggerDrawer），并定位修复 admin 登录后菜单内容混合的 bug。

**Architecture:** 方案 A — `user-dropdown` emit `switch-system` 事件，`header` 监听后调用已注册的 `openSystemTriggerDrawer`。Part B 用 data-driven-debug 抓运行时 `menuList` 定位根因后再修。

**Tech Stack:** Vue 3（选项式 API `defineComponent`）+ Ant Design Vue + TypeScript + Playwright 1.61 + jnpf-api.mjs + SqlSugar/SQL Server

**Spec:** `docs/superpowers/specs/2026-07-07-admin-system-switch-menu-design.md`

**关键事实（已验证）:**
- 数据库 `base_system` 有 2 个启用系统：`devDemoSystem`(功能演示) / `mainSystem`(开发平台)
- admin 当前在 `mainSystem`
- `SystemTriggerDrawer` 已注册在 `header/index.vue:50`，触发：`openSystemTriggerDrawer(true, { list: getUserInfo.systemIds })`
- 后端 :5000 当前**不可用**（Task 1 先恢复）

---

## File Structure

| 文件 | 责任 | 改动 |
|---|---|---|
| `jnpf-web-vue3/src/layouts/default/header/components/user-dropdown/index.vue` | 头像下拉菜单，新增"切换系统"MenuItem + emit 事件 + data-testid | Modify |
| `jnpf-web-vue3/src/layouts/default/header/index.vue` | 顶栏布局，UserDropDown 加 `@switch-system` 监听 | Modify（1 行） |
| `e2e/playwright.config.ts` | Playwright 配置，扩展 testMatch 支持 admin 目录 | Modify |
| `package.json` | 加 `e2e:admin:system-switch` 脚本 | Modify |
| `e2e/admin/01-system-switch.spec.ts` | E2E：头像下拉切换系统验证 | Create |
| 后端 `OAuthService.cs` 或相关（Part B） | admin 菜单加载逻辑修复 | Modify（待 Task 6 定位） |

---

## Task 1: 启动开发环境并确认基线

**Files:** 无（环境准备）

- [ ] **Step 1: 启动全栈开发环境**

```bash
powershell -ExecutionPolicy Bypass -File D:\JNPF-v52\start-dev.ps1
```

在**单独终端**运行（它会持续运行热重载）。等待输出 `前端 :3100` + `后端 :5000` 就绪。

- [ ] **Step 2: 验证后端 :5000 LISTEN**

```bash
netstat -ano | grep ":5000" | grep LISTEN
```

Expected: 至少一行 TCP LISTENING（PID 非 0）。

- [ ] **Step 3: 验证前端 :3100 LISTEN**

```bash
netstat -ano | grep ":3100" | grep LISTEN
```

Expected: 至少一行 TCP LISTENING。

- [ ] **Step 4: 验证后端 API 可用（admin 登录 + CurrentUser 200）**

```bash
node scripts/jnpf-api.mjs login --json
```

Expected: JSON 输出含 `token` 字段（非空）。

```bash
node scripts/jnpf-api.mjs GET "/api/oauth/CurrentUser?type=Web&systemCode=mainSystem"
```

Expected: JSON 输出含 `userInfo` + `menuList`，**HTTP 200**（不再 fetch failed）。

> 若 CurrentUser 仍失败 → 切 Debugger（`/data-driven-debug` 或 dispatch `jnpf-debugger`），不继续后续任务。

---

## Task 2: 扩展 Playwright 配置支持 admin 目录

**Files:**
- Modify: `e2e/playwright.config.ts:7`
- Modify: `package.json:11-19`（scripts）

- [ ] **Step 1: 改 testMatch 匹配所有 spec**

修改 `e2e/playwright.config.ts` 第 7 行：

```ts
// 原：  testMatch: 'studio/**/*.spec.ts',
    testMatch: '**/*.spec.ts',
```

- [ ] **Step 2: 加 e2e:admin:system-switch 脚本**

在 `package.json` 的 `scripts` 里（`e2e:studio` 行之后）加：

```json
    "e2e:admin:system-switch": "playwright test -c e2e/playwright.config.ts e2e/admin/01-system-switch.spec.ts",
```

注意：JSON 不允许尾逗号，加在前一行末尾加逗号。

- [ ] **Step 3: 跑现有 studio 测试确认未破坏**

```bash
pnpm e2e:studio:layout
```

Expected: 测试正常发现并运行（`studio/02-*.spec.ts` 被匹配）。若报"no tests found"说明 testMatch 改错。

- [ ] **Step 4: Commit**

```bash
git add e2e/playwright.config.ts package.json
git commit -m "chore(e2e): testMatch 扩展支持 admin 目录 + e2e:admin:system-switch 脚本

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 3: Part A — user-dropdown 加"切换系统"MenuItem + emit

**Files:**
- Modify: `jnpf-web-vue3/src/layouts/default/header/components/user-dropdown/index.vue`

- [ ] **Step 1: 加 import getJnpfAppId**

在文件第 41 行（`import { useGo } from '/@/hooks/web/usePage';` 之后）加：

```ts
  import { getJnpfAppId } from '/@/utils/jnpf';
```

- [ ] **Step 2: 模板加"切换系统"MenuItem**

在 `<MenuItem key="profile" ... />`（第 11 行）**之后**插入：

```vue
        <MenuItem
          key="switchSystem"
          :text="t('layout.header.systemChange')"
          icon="icon-ym icon-ym-systemToggle"
          data-testid="user-dropdown-switch-system"
          v-if="getUserInfo.systemIds && getUserInfo.systemIds.length > 1 && !getJnpfAppId()"
        />
```

- [ ] **Step 3: 加 emits 声明**

在 `props: { theme: ... }`（第 54-56 行）**之后**加：

```ts
    emits: ['switch-system'],
```

- [ ] **Step 4: setup 接收 emit + handleMenuClick 处理**

把 `setup() {`（第 57 行）改为：

```ts
    setup(props, { emit }) {
```

在 `handleMenuClick` 函数（第 71 行）的 `if (e.key === 'about') ...` 行**之后**加：

```ts
        if (e.key === 'switchSystem') return emit('switch-system');
```

- [ ] **Step 5: return 加 getJnpfAppId**

在 return 对象（第 95-104 行）里加一行（`getUseLockPage,` 之后）：

```ts
        getJnpfAppId,
```

- [ ] **Step 6: type-check**

```bash
cd jnpf-web-vue3 && pnpm type-check
```

Expected: `0 errors`（vue-tsc 退出码 0）。若报 `getJnpfAppId` 未导出 → 检查 `/@/utils/jnpf` 是否有该导出（`header/index.vue:93` 已用同样 import，应可用）。

- [ ] **Step 7: Commit**

```bash
git add jnpf-web-vue3/src/layouts/default/header/components/user-dropdown/index.vue
git commit -m "feat(header): user-dropdown 加\"切换系统\"MenuItem + emit switch-system

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 4: Part A — header 监听 @switch-system 打开抽屉

**Files:**
- Modify: `jnpf-web-vue3/src/layouts/default/header/index.vue:46`

- [ ] **Step 1: UserDropDown 加 @switch-system 监听**

把第 46 行：

```vue
      <UserDropDown :theme="getHeaderTheme" />
```

改为：

```vue
      <UserDropDown :theme="getHeaderTheme" @switch-system="openSystemTriggerDrawer(true, { list: getUserInfo.systemIds })" />
```

> `openSystemTriggerDrawer` 与 `getUserInfo` 均已在 header setup return（line 203, 257, 264），无需新增。

- [ ] **Step 2: type-check**

```bash
cd jnpf-web-vue3 && pnpm type-check
```

Expected: `0 errors`。

- [ ] **Step 3: Commit**

```bash
git add jnpf-web-vue3/src/layouts/default/header/index.vue
git commit -m "feat(header): UserDropDown 监听 switch-system 打开 SystemTriggerDrawer

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 5: Part A — Playwright E2E 验证 + E1 截图

**Files:**
- Create: `e2e/admin/01-system-switch.spec.ts`

- [ ] **Step 1: 新建 E2E 测试文件**

创建 `e2e/admin/01-system-switch.spec.ts`：

```ts
import { test, expect } from '@playwright/test';
import { loginAsAdmin } from '../helpers/login';

/**
 * 验证 admin 头像下拉菜单含"切换系统"入口，点击后打开 SystemTriggerDrawer 抽屉。
 * Supreme Iron Law E1/E2/E3 证据。
 */
test('admin 头像下拉含"切换系统"且打开抽屉', async ({ page }) => {
  await loginAsAdmin(page);

  // 点 admin 头像展开下拉
  const trigger = page.locator('.header-user-dropdown').first();
  await trigger.waitFor({ state: 'visible', timeout: 15_000 });
  await trigger.click();

  // 找到"切换系统"菜单项（data-testid 优先，文案 fallback）
  const switchItem = page
    .getByTestId('user-dropdown-switch-system')
    .or(page.getByRole('menuitem', { name: /切换系统|系统切换/ }))
    .first();
  await expect(switchItem).toBeVisible({ timeout: 10_000 });

  // E1 截图：下拉菜单展开（含切换系统项）
  await page.screenshot({
    path: '.claude/evidence/admin-system-switch-dropdown.png',
    fullPage: false,
  });

  // 点击切换系统
  await switchItem.click();

  // 验证抽屉打开（SystemTriggerDrawer title="切换应用"）
  const drawer = page
    .locator('.portal-toggle-drawer')
    .or(page.getByText('切换应用'))
    .first();
  await expect(drawer).toBeVisible({ timeout: 10_000 });

  // E1 截图：抽屉展开（显示功能演示/开发平台）
  await page.screenshot({
    path: '.claude/evidence/admin-system-switch-drawer.png',
    fullPage: false,
  });
});
```

- [ ] **Step 2: 跑 E2E**

```bash
pnpm e2e:admin:system-switch
```

Expected: `1 passed`。失败时检查 `.claude/evidence/playwright-report.json` + trace。

- [ ] **Step 3: 确认 E1 截图产出**

```bash
ls -la .claude/evidence/admin-system-switch-dropdown.png .claude/evidence/admin-system-switch-drawer.png
```

Expected: 两个文件均 > 5KB（Supreme Iron Law 要求）。

- [ ] **Step 4: Commit**

```bash
git add e2e/admin/01-system-switch.spec.ts .claude/evidence/admin-system-switch-dropdown.png .claude/evidence/admin-system-switch-drawer.png
git commit -m "test(e2e): admin 头像下拉系统切换 E2E + E1 截图证据

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 6: Part B — 抓 admin 菜单运行时数据（data-driven 调研）

**Files:**
- Create: `workspace/debug_report.md`

> 遵循 S5：禁止靠源码猜测根因，抓运行时数据。

- [ ] **Step 1: 抓 admin 的 CurrentUser 完整响应**

```bash
node scripts/jnpf-api.mjs login --json > /tmp/admin-login.json
TOKEN=$(python -c "import json;print(json.load(open('/tmp/admin-login.json'))['token'])")
node scripts/jnpf-api.mjs GET "/api/oauth/CurrentUser?type=Web&systemCode=mainSystem" > /tmp/admin-currentuser.json 2>&1 || true
```

> 若 jnpf-api.mjs GET 仍 fetch failed，改用 curl：
> `curl -s -H "Authorization: Bearer $TOKEN" "http://localhost:5000/api/oauth/CurrentUser?type=Web&systemCode=mainSystem" > /tmp/admin-currentuser.json`

- [ ] **Step 2: 提取 menuList 分析**

```bash
python -c "
import json
d = json.load(open('/tmp/admin-currentuser.json'))
ui = d.get('userInfo', {})
ml = d.get('menuList', [])
print('systemId:', ui.get('systemId'))
print('systemIds:', [(s.get('id'), s.get('name'), s.get('currentSystem')) for s in ui.get('systemIds', [])])
print('menuList top-level count:', len(ml))
print('menuList top-level items:', [(m.get('id'), m.get('fullName'), m.get('urlAddress')) for m in ml])
"
```

Expected: 输出 admin 当前 systemId、systemIds 列表、menuList 顶层菜单项。

- [ ] **Step 3: 对比预期**

预期（admin 在 mainSystem）：
- `systemId` = `mainSystem` 的 id
- `menuList` 仅含 `mainSystem`（开发平台）的菜单

判定依据：
- 若 menuList 顶层混入了属于 `devDemoSystem` 的菜单 → 证实"两系统菜单混合"，嫌疑指向 `OAuthService.cs:532` 空 sysId
- 若 menuList 为空 → 转向 `GetUserModuleListByIds` 菜单查询逻辑或菜单主数据
- 若 menuList 仅 mainSystem 但用户仍报错 → 可能是前端渲染/缓存问题，转前端

- [ ] **Step 4: 直查数据库确认菜单归属（交叉验证）**

```python
# 在 PowerShell 里跑（参考 spec 2.1 节连接串）
$cs = "Server=(local)\SQLEXPRESS;Database=ZXAF_V1_DevTest1;User Id=sa;Password=1qazxsw2;TrustServerCertificate=True"
# 查 menuList 里某个可疑菜单 id 的 SystemId 归属：
# SELECT F_ID, F_FULL_NAME, F_SYSTEM_ID FROM BASE_MODULE WHERE F_ID IN ('<可疑id1>', '<可疑id2>')
```

- [ ] **Step 5: 写 debug_report**

创建 `workspace/debug_report.md`，记录：
- 症状：admin 登录后菜单内容/结构异常
- 抓取的 menuList 实际内容（Step 2 输出）
- 对比预期（Step 3 判定）
- DB 交叉验证（Step 4）
- **根因结论**（基于数据，非源码推断）
- 建议修复点（具体文件:行号 + 改法）

- [ ] **Step 6: Commit 调研产出**

```bash
git add workspace/debug_report.md
git commit -m "docs(debug): admin 菜单内容 bug 根因调研报告 (data-driven)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 7: Part B — 根因修复（基于 Task 6 debug_report）

**Files:** 由 Task 6 debug_report 指明（最可能：`backend/modularity/oauth/JNPF.OAuth/OAuthService.cs`）

> **重要**：以下代码是**最可能根因（line 532 空 sysId）的修复参考**。实际修复**必须以 Task 6 debug_report 的根因结论为准**。若实际根因不同，据实际根因修复并在本步骤注明偏差。

- [ ] **Step 1: 读 debug_report 确认根因**

```bash
cat workspace/debug_report.md
```

确认根因文件:行号。若与下方"参考修复"不一致 → 按实际根因走，跳到 Step 4。

- [ ] **Step 2: 参考修复（若根因 = line 532 空 sysId 导致菜单混合）**

`OAuthService.cs` admin 分支 line 532 当前：

```csharp
loginOutput.menuList = (await _moduleService.GetUserModuleListByIds(type, string.Empty, noContainsMIdList, noContainsMUrlList)).ToTree("-1");
```

改为传当前系统 id（与正常路径 line 405 一致）：

```csharp
var adminSysId = loginOutput.userInfo.systemId;
loginOutput.menuList = (await _moduleService.GetUserModuleListByIds(type, adminSysId, noContainsMIdList, noContainsMUrlList)).ToTree("-1");
```

> 注意：`loginOutput.userInfo.systemId` 在 line 516/519 已被设为 `defaultItem.id`（mainSystem）。此处复用该值，保证菜单按当前系统过滤。

- [ ] **Step 3: 后端编译**

```bash
cd backend && dotnet build
```

Expected: `0 errors`。

- [ ] **Step 4: 验证修复（重抓 menuList）**

后端热重载后，重跑 Task 6 Step 1-2，确认：
- menuList 不再混合两个系统
- 仅含当前 systemId 的菜单

- [ ] **Step 5: Part A + Part B 联合验证**

切换系统（用 Task 5 的 E2E 流程或手动）→ 确认切换后菜单正确刷新为新系统的菜单。

- [ ] **Step 6: Commit**

```bash
git add backend/modularity/oauth/JNPF.OAuth/OAuthService.cs
git commit -m "fix(oauth): admin 菜单加载按当前 systemId 过滤 (修两系统菜单混合)

根因见 workspace/debug_report.md

Co-Authored-By: Claude <noreply@anthropic.com>"
```

> 若根因不在 OAuthService.cs，`git add` 改为实际修改的文件。

---

## Task 8: 全量验证 + Push

**Files:** 无（验证 + 推送）

- [ ] **Step 1: 前端类型检查**

```bash
cd jnpf-web-vue3 && pnpm type-check
```

Expected: `0 errors`。

- [ ] **Step 2: 后端编译**

```bash
cd backend && dotnet build
```

Expected: `0 errors`。

- [ ] **Step 3: Part A E2E 复跑**

```bash
pnpm e2e:admin:system-switch
```

Expected: `1 passed`。

- [ ] **Step 4: 现有 studio 测试回归**

```bash
pnpm e2e:studio:layout
```

Expected: 通过（未破坏现有）。

- [ ] **Step 5: 工作树状态确认**

```bash
git status --short
```

Expected: 仅 `test-results/`（gitignored）或空。

- [ ] **Step 6: Push**

```bash
git push origin frontend-architecture-refactor
```

Expected: 推送成功（local HEAD == remote HEAD）。

---

## 验收标准（对齐 spec §7）

- [ ] E1 截图：`.claude/evidence/admin-system-switch-dropdown.png` + `admin-system-switch-drawer.png`
- [ ] E2 操作路径：登录 → 头像 → 切换系统 → 抽屉（记录于 E2E spec）
- [ ] E3 实际输出：抽屉显示"功能演示/开发平台"，切换后菜单刷新
- [ ] admin menuList 不再混合两系统（Task 6/7 数据验证）
- [ ] header 顶栏原切换图标未受影响
- [ ] `pnpm type-check` + `dotnet build` + E2E 全绿
- [ ] 已 push 到 `origin/frontend-architecture-refactor`

---

## Self-Review 记录

**Spec coverage:**
- §3.2 user-dropdown MenuItem + emit → Task 3 ✅
- §3.2 header @switch-system → Task 4 ✅
- §3.4 显示条件（systemIds>1）→ Task 3 Step 2 v-if ✅
- §4 Part B data-driven 调研 → Task 6 ✅
- §4 Part B 修复 → Task 7 ✅
- §6 测试（E1 截图）→ Task 5 ✅

**Placeholder scan:** Task 7 标注"参考修复 + 以实际根因为准"——非 placeholder，给了具体代码且明确处理根因不同的回退路径。其余步骤均有完整代码/命令。

**Type consistency:** `emit('switch-system')`（Task 3）↔ `@switch-system`（Task 4）↔ `switch-system` 事件名一致 ✅。`getJnpfAppId` 导入路径 `/@/utils/jnpf` 与 header 一致 ✅。
