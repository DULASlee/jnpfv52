# Admin 头像下拉菜单系统切换 + 菜单内容修复 — 设计文档

- **日期**: 2026-07-07
- **分支**: frontend-architecture-refactor
- **任务级别**: A 级（标准 — 2 文件核心改动 + 1 项 bug 调研）
- **状态**: 待用户审阅

---

## 1. 背景与目标

### 1.1 用户诉求

> "检查 admin 账号，菜单异常问题，最好是在系统框架页 admin 头像关联的下拉菜单为系统用户关联演示系统/开发系统的切换。"

拆解为两件事：

1. **功能缺失**：admin 头像下拉菜单（`user-dropdown/index.vue`）当前没有"切换系统"入口，希望增加"功能演示 ↔ 开发平台"的切换。
2. **菜单 bug**：admin 正常登录后，左侧菜单内容/结构不对（菜单项缺失 / 显示了不该显示的 / 两个系统的菜单混在一起）。

### 1.2 目标

- **Part A**：在 admin 头像下拉菜单增加"切换系统"项，复用现有 `SystemTriggerDrawer` 抽屉完成切换。
- **Part B**：定位并修复 admin 登录后菜单内容异常的根因（根因待 Build 阶段运行时数据确认）。

### 1.3 非目标

- 不改变现有 header 顶栏的系统切换图标行为（保留为额外入口）。
- 不修改后端 `setMajor` API、`SystemTriggerDrawer` 组件内部逻辑。
- 不调整系统主数据（`base_system` 表）。

---

## 2. 现状分析（已验证事实）

### 2.1 数据库系统主数据（`base_system` 表）

| F_ID | F_FULL_NAME | F_EN_CODE | F_ENABLED_MARK | F_SORT_CODE |
|---|---|---|---|---|
| devDemoSystem | 功能演示 | devDemoSystem | 1 | 0 |
| mainSystem | 开发平台 | mainSystem | 1 | 100 |

- 共 2 个启用系统。
- admin 当前所在系统：`mainSystem`（JWT payload `ZxSystemId=mainSystem`）。

> 用户口中的"演示系统" = `devDemoSystem`（功能演示）；"开发系统" = `mainSystem`（开发平台）。

### 2.2 前端组件事实

| 组件 | 路径 | 当前行为 |
|---|---|---|
| **头像下拉菜单** | `jnpf-web-vue3/src/layouts/default/header/components/user-dropdown/index.vue` | 菜单项：个人资料 / 反馈 / 关于 / 声明 / 锁屏 / 退出。**无系统切换入口** |
| **顶栏切换图标** | `jnpf-web-vue3/src/layouts/default/header/index.vue:32-40` | 独立图标按钮，`v-if="getUserInfo.systemIds && getUserInfo.systemIds.length > 1 && !getJnpfAppId()"` |
| **切换抽屉** | `jnpf-web-vue3/src/layouts/default/header/components/SystemTriggerDrawer.vue` | 接收 `{ list: systemIds }`，选中调 `setMajor({ majorId, majorType: 'System' })`，成功后 `router.replace('/') + location.reload()` |
| **切换 API** | `setMajor` → `PUT /api/permission/Users/Current/major` | 已存在，无需改动 |

### 2.3 后端 `systemIds` 来源（`OAuthService.cs:387-401`）

`userInfo.systemIds` 来自 `base_system` 表所有 `EnabledMark=1` 且未被租户忽略的系统，**非用户专属**。对 admin（超管）= 全部 2 个系统。

### 2.4 后端 :5000 当前不可用

**关键运行时事实**：brainstorming 阶段检测到后端服务进程已退出（`:5000` 无 LISTEN），仅剩 19+ 个 dotnet 编译进程。因此 Part B 的菜单 bug 无法在此阶段抓取运行时数据，根因未定。

---

## 3. Part A — 头像下拉菜单系统切换（详细设计）

### 3.1 架构：方案 A（emit 事件，props-down/events-up）

```
user-dropdown/index.vue                  header/index.vue
┌──────────────────────┐                ┌──────────────────────────┐
│ MenuItem: 切换系统    │ ── emit ──▶   │ @switch-system =         │
│  (key=switchSystem)  │  'switch-     │   openSystemTriggerDrawer │
│                      │   system'     │     (true, {list})        │
└──────────────────────┘                │                          │
                                        │ <SystemTriggerDrawer />  │
                                        │   (单实例，已存在)        │
                                        └──────────────────────────┘
```

**选择理由**：
- 符合 Vue 单向数据流，最小改动（2 文件 ~15 行）。
- `SystemTriggerDrawer` 维持单实例注册在 `header`，无组件冗余。
- 不引入新 composable（YAGNI — 仅 2 处调用点）。

### 3.2 组件改动

#### `user-dropdown/index.vue`

1. 模板：在 `<MenuItem key="profile">` 之后插入：
   ```vue
   <MenuItem
     key="switchSystem"
     :text="t('layout.header.systemChange')"
     icon="icon-ym icon-ym-systemToggle"
     v-if="getUserInfo.systemIds && getUserInfo.systemIds.length > 1 && !getJnpfAppId()"
   />
   ```
2. `setup()`：
   - 引入 `getJnpfAppId`（从 `/@/utils/jnpf`）。
   - `handleMenuClick` 增加：`if (e.key === 'switchSystem') return emit('switch-system');`
   - 组件 `emits: ['switch-system']` 声明。
   - return 增加 `getJnpfAppId`。

#### `header/index.vue`

1. 模板：`<UserDropDown>` 加监听：
   ```vue
   <UserDropDown :theme="getHeaderTheme" @switch-system="openSystemTriggerDrawer(true, { list: getUserInfo.systemIds })" />
   ```

**仅此 2 处改动**。`SystemTriggerDrawer` 注册、`openSystemTriggerDrawer`、`getUserInfo` 均已存在于 header，无需新增。

### 3.3 数据流

```
admin 点头像下拉"切换系统"
  → user-dropdown emit('switch-system')
  → header 调 openSystemTriggerDrawer(true, { list: systemIds })
  → SystemTriggerDrawer 打开，显示 [功能演示, 开发平台]，当前系统打勾
  → admin 选另一个系统
  → selectItem() 调 setMajor({ majorId, majorType: 'System' })
  → 成功 → router.replace('/') → setTimeout 50ms → location.reload()
  → 重新拉 CurrentUser → menuList 按新 systemId 加载
```

### 3.4 显示条件一致性

头像下拉的"切换系统"项与 header 顶栏图标使用**完全相同**的 `v-if` 条件：
`systemIds.length > 1 && !getJnpfAppId()`

- 只有 1 个系统或独立应用模式下，两处入口同时隐藏。
- 保证行为统一，不会出现"图标在但菜单项不在"的困惑。

### 3.5 错误处理

复用 `SystemTriggerDrawer.selectItem` 现有逻辑：
- `setMajor` 失败 → `.catch(() => changeLoading(false))`，抽屉保持打开（已实现）。
- 切换中 loading 状态由 `changeLoading(true)` 控制（已实现）。

**不新增错误处理代码**（现有已覆盖）。

---

## 4. Part B — admin 菜单内容 bug 调研与修复

### 4.1 现象

admin 正常登录后，左侧菜单内容/结构不对（用户描述：菜单项缺失 / 两个系统菜单混在一起）。

### 4.2 嫌疑代码路径（`[INFERRED, MED]` — 非根因断言）

`OAuthService.GetCurrentUser` 的 **admin 超管分支**（line 503-540）：

- **line 508**：长条件判断 — 当 `currSysId` 不在 `systemIds` 内 / `currSysId` 为空 / `menuList` 为空 / `currSysId` 对应系统被禁用，**任一满足**即触发系统重置。
- **line 515**：PC 端默认选 `mainSystem` 作为 currentSystem。
- **line 532**：`menuList = GetUserModuleListByIds(type, string.Empty, ...)` —— **传空 `sysId`** 加载菜单。

`[INFERRED, MED]` 空 `sysId` 可能导致 `GetUserModuleListByIds` 不过滤系统，混合"功能演示"与"开发平台"两个系统的菜单。**但这是源码分析推断，未经运行时验证**。对比：正常路径（line 405）传入具体 `sysId`。

### 4.3 Build 阶段调研步骤（data-driven-debug）

> 遵循 S5：禁止靠源码猜测定位，必须抓运行时数据。

1. `start-dev.ps1` 拉起后端（确认 `:5000` 恢复 LISTEN）。
2. `node scripts/jnpf-api.mjs login --json` 拿 token。
3. `node scripts/jnpf-api.mjs GET "/api/oauth/CurrentUser?type=Web&systemCode=mainSystem"` 抓 admin 实际 `menuList`。
4. 对比：
   - 预期：仅 `mainSystem` 的菜单（开发平台菜单树）。
   - 实际：是否混入了 `devDemoSystem` 的菜单项？
5. 若证实混合 → 在 `GetUserModuleListByIds` 加诊断日志或查其 SQL，定位为何空 `sysId` 不过滤。
6. 定位根因后，**单一变量修复**（只改根因点，不顺手重构）。
7. 复跑确认菜单正确 + Part A 切换功能正常。

### 4.4 Part B 修复范围（待根因确认后填充）

- **不在本 spec 范围内预先写死修复方案**（根因未定，写死方案 = 猜测）。
- 调研产出根因后，在 `workspace/debug_report.md` 记录，再补充修复方案到 plan.md。

---

## 5. 影响面

| 文件 | 改动类型 | 行数估计 |
|---|---|---|
| `jnpf-web-vue3/src/layouts/default/header/components/user-dropdown/index.vue` | 修改（加 MenuItem + emit + getJnpfAppId） | +8 |
| `jnpf-web-vue3/src/layouts/default/header/index.vue` | 修改（UserDropDown 加 @switch-system） | +1 |
| 后端 `OAuthService.cs` 或相关 | **待 Part B 调研定位** | TBD |
| i18n 文件 | 复用 `layout.header.systemChange`，可能无需新增 | 0 |

**架构红线检查**：
- R3（Codegen Boundary）：改的是手写 `.vue`，非生成代码 ✅
- R5（Module Boundary）：仅 jnpf-web-vue3 + oauth 模块，无禁用模块 ✅
- R6（前端内存安全）：本改动不涉及 SSE/Timer/EventSource ✅
- R8（API Permission）：复用已授权 API，不新增端点 ✅

---

## 6. 测试计划

### 6.1 Part A 验证（Playwright E2E — Supreme Iron Law E1/E2/E3）

1. 启动 `start-dev.ps1`（前端 :3100 + 后端 :5000）。
2. Playwright 登录 admin → 进入框架页。
3. 点击 admin 头像 → 截图 E1：下拉菜单显示"切换系统"项。
4. 点击"切换系统" → 截图：抽屉滑出，显示"功能演示/开发平台"，开发平台打勾。
5. 选择"功能演示" → 验证页面 reload，菜单切换为功能演示菜单。
6. 再次切换回"开发平台" → 验证可逆。

**选择器**：优先 `data-testid` 或 `getByRole`，避免 CSS class。

### 6.2 Part B 验证

- 抓 admin 登录后 `menuList` → 确认仅含当前系统的菜单。
- 切换系统后菜单刷新正确。

### 6.3 回归

- header 顶栏切换图标仍正常显示和工作（未破坏）。
- 普通用户（非 admin）登录不受影响。

---

## 7. 验收标准

- [ ] E1：头像下拉菜单含"切换系统"截图
- [ ] E2：操作路径（头像 → 切换系统 → 抽屉 → 选系统 → reload）
- [ ] E3：切换后菜单实际更新（描述实际 UI）
- [ ] admin 登录后 `menuList` 不再混合两个系统
- [ ] header 顶栏原切换图标未受影响
- [ ] `pnpm type-check` 0 error
- [ ] 后端改动（如有）`dotnet build` 0 error

---

## 8. 开放问题

1. **Part B 是否独立任务**：本 spec 默认并入同任务第二节。若根因复杂（涉及菜单加载深层逻辑），可拆为独立 spec。
2. **显示条件**：头像下拉"切换系统"项是否应与 header 图标同条件（`systemIds.length > 1`），还是无条件显示？本 spec 取前者（一致）。
