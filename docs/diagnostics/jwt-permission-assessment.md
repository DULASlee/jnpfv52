# JWT 权限校验评估报告

> 评估日期：2026-06-07
> 文件：`application/JNPF.API.Entry/Handlers/JwtHandler.cs`

---

## 当前状态

**权限校验被完全绕过。** `CheckAuthorzieAsync` 方法（第 54-79 行）对所有已认证用户无条件返回 `true`。

### 代码现状

```csharp
// 第 74-78 行
//var permissionList = await App.GetService<ISysMenuService>().GetLoginPermissionList(userManager.UserId);
//return permissionList.Contains(routeName);
return true;  // ← 所有已认证用户都通过
```

### 问题

1. **ISysMenuService 接口不存在** — 代码库中未找到该接口定义，仅有注释中的引用
2. **权限校验被注释** — 第 74-78 行的权限查询和校验逻辑被注释
3. **无条件返回 true** — 任何持有有效 JWT Token 的用户可访问所有 API 端点

### 现有保护

| 保护层 | 状态 | 说明 |
|---|---|---|
| JWT Token 验证 | ✅ 生效 | `JWTEncryption.AutoRefreshToken` + Token 有效性校验 |
| Refresh Token 防护 | ✅ 生效 | 基类 `AppAuthorizeHandler` 阻止 refresh token 当 access token 用 |
| Administrator 短路 | ✅ 生效 | 管理员跳过权限检查（第 57-58 行） |
| 端点级权限校验 | ❌ 缺失 | 所有已认证用户可访问所有端点 |
| 路由级权限校验 | ❌ 缺失 | 无路由-权限映射检查 |

---

## 修复方案评估

### 方案 A：恢复 ISysMenuService + 权限校验（推荐）

**前提条件：**
- 需要先创建 `ISysMenuService` 接口和实现
- 需要权限数据表（`SYS_MENU` 或类似表）已有数据
- 需要确认权限数据的缓存策略

**实现步骤：**
1. 在 `JNPF.Systems.Interfaces` 中创建 `ISysMenuService` 接口
2. 在 `JNPF.Systems` 中实现该接口，查询 `SYS_MENU` 表获取用户权限列表
3. 取消注释 `JwtHandler.cs` 第 74-78 行
4. 添加 Redis 缓存（TTL 5 分钟）

**风险：** 如果权限数据不完整或格式不匹配，会导致大量 403

### 方案 B：渐进式恢复（稳妥）

**策略：** 先恢复核心模块权限，其他模块临时白名单

1. 创建 `ISysMenuService` 接口和实现
2. 在 `JwtHandler` 中添加白名单配置：
   ```json
   {
     "Auth": {
       "AllowAnonymousPaths": [
         "/api/oauth/*",
         "/health",
         "/swagger/*"
       ]
     }
   }
   ```
3. 先只对 `/api/system/*`、`/api/workflow/*` 启用权限校验
4. 其他模块临时加入白名单
5. 逐步收紧白名单

### 方案 C：仅修复架构缺陷，不恢复权限（不推荐）

仅修复代码结构问题（如 Service Locator 模式），但保持 `return true`。**不推荐** — 违反铁律一（带病不可重构）。

---

## 建议

**采用方案 B（渐进式恢复）**，理由：
1. 符合铁律一 — 先修复安全漏洞，再进行架构重构
2. 降低风险 — 不会一次性导致大量 403
3. 可控 — 通过白名单逐步收紧

**实施时间：** 阶段 0 第 2 周 Day 8（0.5 天）

**前置条件：**
- 确认 `SYS_MENU` 表有权限数据
- 确认权限数据格式与路由格式匹配（`module:controller:action`）

---

## 影响评估

| 影响项 | 评估 |
|---|---|
| 现有功能 | 无影响（当前所有用户已能访问所有端点） |
| 性能 | 轻微影响（每次请求增加一次权限查询，有缓存后可忽略） |
| 安全 | 显著提升（从"全开放"变为"按权限访问"） |
| 开发体验 | 需要确保开发环境的管理员账号权限正确 |
