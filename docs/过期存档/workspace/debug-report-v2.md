# Admin CurrentUser NRE — 根因定位与修复 (2026-07-08)

> 更正 `debug_report.md`（commit e897b473）的错误根因。本文件为 data-driven 确认的真实根因。

## 根因（data-driven 确认）

`UserManager.UserOrigin` 属性（`UserManager.cs:294`）：

```csharp
public string UserOrigin
{
    get => _httpContext?.Request.Headers["jnpf-origin"] ?? "pc";   // ← bug
}
```

**执行链**：
1. 请求不带 `jnpf-origin` header（curl / 浏览器默认不设）
2. `Headers["jnpf-origin"]` 返回 `StringValues.Empty`（**struct，非 null**）
3. `?? "pc"` **不触发**（StringValues 是值类型，永不为 null）
4. `StringValues.Empty` 隐式转 `string` → **null**（`StringValues.Value` 在无值时为 null）
5. `UserOrigin` 返回 null
6. `GetUserInfo` `UserOrigin.Equals("pc")` → **`null.Equals` → NRE**
7. 传播到 `OAuthService.GetCurrentUser` → 前端 `code:500` → **菜单加载失败**

## 诊断过程（data-driven，逐步收窄）

环境稳定后（后端连续运行 6.5h，无抖动），逐步加 log 收窄：

| 步骤 | log 发现 | 排除/收窄 |
|---|---|---|
| dbc.log | `IsMultiTenant=False` | 排除 ITenantFilter（不附加） |
| sysconfig.log | `sysConfigInfo found=True`（tokenTimeout 存在） | 排除 SysConfig null |
| data.log | `data found=True`（admin 查到），organizeName=null | 排除 UserEntity 查询返回 null |
| step.log | GetSubsidiary / GetSubordinates 都执行完 | 排除这两个方法 |
| step.log + user.log | step 写到 "before User.SystemId"，但 **user.log 空**（User getter 没执行） | NRE 在 `User.SystemId` **之前** |
| 拆 UserOrigin log | **`UserOrigin=[] len=null`** | **定位 UserOrigin 返回 null** |

关键转折：user.log 空这一矛盾，证明 NRE 不在 User getter，而在三元运算符 `UserOrigin.Equals("pc") ? ...` 的 condition（先求值）。

## 修复

`UserManager.cs:294` UserOrigin 属性 — 显式 `IsNullOrEmpty` 处理 `StringValues.Empty`：

```csharp
public string UserOrigin
{
    get
    {
        var origin = _httpContext?.Request.Headers["jnpf-origin"].ToString();
        return string.IsNullOrEmpty(origin) ? "pc" : origin;
    }
}
```

## 验证

```
GET /api/oauth/CurrentUser?type=Web&systemCode=mainSystem
→ code=200 msg=操作成功
→ userName=管理员, systemId=mainSystem, systemIds=2, menuList=11
集成测试 scripts/test-admin-bypass.mjs: 4/4 PASS
```

## 更正：debug_report.md 的 ITenantFilter 根因是错的

`debug_report.md`（e897b473）写"ITenantFilter 附加 `TenantId=='0'` 排除 admin（tenant NULL）→ NRE"。这是 **`[INFERRED, post-hoc]` 当成 `[KNOWN]`**——基于 SQL 模拟（`WHERE tenant='0'` 排除 NULL）而非运行时验证。实际 `IsMultiTenant=False`，ITenantFilter 根本不附加，与 NRE 无关。

数据修复（`UPDATE admin tenant='0'`）当时让 CurrentUser 200 是**环境抖动假象**（后端 port ready 0s，curl 连残留实例），不是 ITenantFilter 修复。

## 方案 A（ITenantFilter 超管豁免）保留

`AdminBypassGuard` + `SqlSugarDbContextProvider` / `ColumnIsolationStrategy` 两处 `!IsAdministrator()` 条件。与本次 NRE **无关**（IsMultiTenant=False），但逻辑正确（超管应豁免），保留作为防御性改动（未来 IsMultiTenant=True 时生效）。

## 附带修复（工程师代码 build bug）

`EntityDesignProjector.cs` 缺 `using JNPF.InteAssistant.Skills;`（IrSnapshot 所在命名空间），导致 build CS0246 错误。临时加 using 解锁 build（P1 阻塞）。该文件是工程师 untracked 半成品，需通知工程师。
