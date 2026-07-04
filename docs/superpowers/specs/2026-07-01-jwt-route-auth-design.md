# S1 — JwtHandler 路由授权补全（策略 C）

> **日期**：2026-07-01
> **优先级**：P0 安全债
> **策略**：C (GradualEnforcement) → 第二阶段切 A (StrictEnforcement)

---

## 1. 问题诊断

`JwtHandler.CheckAuthorzieAsync` 第 88-95 行：

```csharp
var permissionGroups = await GetCachedPermissionGroupsAsync(...);
if (permissionGroups.Count == 0) return false;  // 无权限组→拒绝
return true;  // ← BUG: 有任意权限组即全放行，不做路由匹配
```

**根因**：`GetPermissionByUserId` 返回的是用户所在的权限组 ID 列表（非授权资源列表）。只要用户属于任意权限组，所有 `[SecurityDefine]` 声明的路由都无条件放行。

---

## 2. 修复设计

### 2.1 三层判断

```
管理员 / 白名单 / [AllowAnonymous] → 直接放行
  ↓
提取端点 SecurityDefineAttribute.ResourceId
  ├─ ResourceId 存在 → 严格匹配用户授权资源
  └─ ResourceId 不存在 + 无 AllowAnonymous
      ├─ GradualEnforcement (C): 放行 + Warning 日志
      └─ StrictEnforcement (A): 403
```

### 2.2 策略开关

```csharp
enum RouteAuthPolicy
{
    GradualEnforcement,  // 策略 C — 无声明放行+日志
    StrictEnforcement    // 策略 A — 无声明返回 403
}
```

配置键：`Auth:RoutePolicy`，默认值 `GradualEnforcement`。

### 2.3 授权资源缓存

- Key: `jnpf:authorized:resources:{tenantId}:{userId}`
- Value: `HashSet<string>` — 所有已授权的 module code
- Source: `BASE_AUTHORIZE` WHERE `ObjectId IN (roles + userId)` AND `ItemType = "system"`
- TTL: 5 min

### 2.4 管理员缓存

管理员标记也进 Redis：`jnpf:user:isadmin:{tenantId}:{userId}`，TTL 5min，避免每次请求查库。

---

## 3. 文件变更

| 文件 | 动作 | 行数 |
|------|------|------|
| `JwtHandler.cs` | 重写 `CheckAuthorzieAsync` + `GetCachedAuthorizedResourcesAsync` | ~100 |

---

## 4. 验收标准

- [ ] 有 `[SecurityDefine]` + 有权限 → 200
- [ ] 有 `[SecurityDefine]` + 无权限 → 403
- [ ] `[AllowAnonymous]` → 200
- [ ] 无属性声明 → 200 + Warning 日志（策略 C）
- [ ] 管理员 → 200
- [ ] 白名单路径 → 200
- [ ] 无权限组 → 403
