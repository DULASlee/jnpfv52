# Debug Report — admin 菜单异常（CurrentUser NRE）

- **日期**: 2026-07-07
- **任务**: Plan Part B — admin 登录后菜单内容/结构异常
- **调试方法**: data-driven-debug（运行时数据定位，非源码猜测）

## 症状

admin 登录后菜单异常。前端调 `GET /api/oauth/CurrentUser` 拿菜单数据。

## 抓取的运行时数据

### 1. 直接复现（curl）

```
GET /api/oauth/CurrentUser?type=Web&systemCode=mainSystem
→ HTTP 200, code: 500, msg: "Object reference not set to an instance of an object."
→ 耗时 9.89s（异常路径，慢）
```

### 2. NRE Stack Trace（error-20260707.json）

```
System.NullReferenceException: Object reference not set to an instance of an object.
   at JNPF.Common.Core.Manager.UserManager.GetUserInfo() in User/UserManager.cs:line 421
   at JNPF.OAuth.OAuthService.GetCurrentUser(...) in OAuthService.cs:line 348
```

**根因点**: `UserManager.GetUserInfo():421`

```csharp
var currSysId = UserOrigin.Equals("pc") ? User.SystemId : User.AppSystemId;
//                                        ^^^^^^^^^^^^ User 为 null → NRE
```

### 3. `User` 属性为何 null（UserManager.cs:66-73）

```csharp
public UserEntity User {
    get {
        if (_userEntity == null) _userEntity = _repository.GetSingle(u => u.Id == UserId);
        return _userEntity;   // GetSingle 查不到 → null
    }
}
```

`_repository.GetSingle` 走 SqlSugar Queryable，自动附加 `ITenantFilter`。

### 4. ITenantFilter 触发条件（SqlSugarDbContextProvider.cs:37-46）

```csharp
if (_tenantContext.IsMultiTenant && !_tenantContext.IsDefaultTenant() && IsolationType == 1) {
    context.QueryFilter.AddTableFilter<ITenantFilter>(it => it.TenantId == fieldValue);
}
```

`IsDefaultTenant()`（TenantContext.cs:155）：
```csharp
return string.IsNullOrEmpty(TenantId) || TenantId == "default";
// "0" 既非空也非 "default" → 返回 false → 附加过滤
```

### 5. admin 数据库实际数据

```
BASE_USER: id=349057407209541, account=admin, F_TENANT_ID = NULL  ← 数据不一致
JWT:       TenantId = "0"
```

SQL 模拟验证：
- `WHERE F_ID='349057407209541'`（无过滤）→ 1 行（admin 存在）
- `WHERE F_ID='349057407209541' AND F_TENANT_ID='0'`（带过滤）→ **0 行（admin 被排除）**

`NULL == '0'` 在 SQL 中为 false → admin 被 ITenantFilter 排除。

## 根因（基于数据，非推断）

1. admin JWT `TenantId="0"`
2. `"0"` 不是默认租户（`IsDefaultTenant` 只认空 / `"default"`）→ 附加 `AddTableFilter<ITenantFilter>(it => it.TenantId == "0")`
3. admin 在 `BASE_USER.F_TENANT_ID = NULL`（与 JWT 不一致）
4. `NULL == "0"` 为 false → admin 被过滤掉
5. `_repository.GetSingle(UserId)` 返回 null → `User` null
6. `UserManager.GetUserInfo():421` `User.SystemId` 抛 NRE
7. `OAuthService.GetCurrentUser():348` 传播 → 前端 `code:500` → **菜单加载失败 = "菜单异常"**

## 修复

### 已实施：数据修复（P0 解锁）

```sql
UPDATE BASE_USER SET F_TENANT_ID = '0' WHERE F_ACCOUNT = 'admin';
-- UPDATE rows: 1，admin tenant 现为 '0'（与 JWT 一致）
```

### 验证

```
GET /api/oauth/CurrentUser?type=Web&systemCode=mainSystem
→ HTTP 200, code: 200, msg: 操作成功, 耗时 0.228s
→ userName: 管理员, menuList: 4 项（devDemoSystem 菜单）
```

NRE 消失，CurrentUser 恢复。

## 次要发现

- admin 默认 `systemId = devDemoSystem`（功能演示），非 mainSystem（开发平台）。由 `BASE_USER.F_SYSTEM_ID` 决定。**Part A 的系统切换功能可让 admin 手动切换**，无需强行改默认。
- 菜单**未混合两个系统**（原假设 line 532 空 sysId 不成立）— menuList 仅含 devDemoSystem 的 4 项菜单。

## 建议的后续（P2，不在本次范围）

数据修复是 dev 环境的临时解锁。**代码层健壮性改进**：`ITenantFilter` 对超管（`IsAdministrator`）豁免，或 `UserManager.User` 查询对超管用 `DisableGlobalFilter("TenantFilter")`（带 `// r4-safe:` 豁免注释）。需评估对 R4 多租户隔离的影响，建议单独任务处理。

## pre-existing type-check 错误（Bug Discovery 上报）

`pnpm type-check` 全量 FAIL（EXIT=2），但错误全部在 `src/views/studio/components/chat/__tests__/MessageBubble.browser.test.ts`（vitest browser 测试 API 误用：`expect.element`/`getByText`/`getByRole` 不存在于类型），**与本次 Part A 改动无关**。Part A 的两个文件（user-dropdown/header）type-check 无报错。此为 P2 pre-existing 问题。
