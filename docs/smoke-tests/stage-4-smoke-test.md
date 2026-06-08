# Stage 4 Smoke Test Report

**Date:** 2026-06-07
**Stage:** 4 — Repository/Provider Refactoring
**Tester:** Claude Code (automated browser testing)

---

## L1 Compile Verification

| Project | Command | Result |
|---|---|---|
| JNPF.API.Entry | `dotnet build application/JNPF.API.Entry/JNPF.API.Entry.csproj` | PASS (0 errors) |

## L2 Startup Verification

| Service | Port | Status |
|---|---|---|
| Backend API | :5000 | LISTENING (PID 25232) |
| Frontend PC | :3100 | LISTENING (PID 25104) |
| DataV | :3102 | LISTENING (PID 3148) |
| UniApp H5 | :3800 | LISTENING (PID 25204) |

## L3 Health Check

| Endpoint | Result |
|---|---|
| `GET /health` | Healthy |

## L4 Browser Smoke Test

### 4.1 PC Frontend (jnpf-web-vue3)

| Step | Action | Result |
|---|---|---|
| 1 | Navigate to http://localhost:3100 | Login page loaded ("面包树科技快速开发平台") |
| 2 | Login with admin/000000 | SUCCESS, redirected to /home |
| 3 | Console errors after login | 0 errors |
| 4 | Home page data | Dashboard loaded: 访问数, 成交额, 下载数, 成交数 |
| 5 | User identity | "管理员" displayed correctly |
| 6 | Navigate: 系统管理 → 应用菜单 | Page loaded at /system/menu |
| 7 | 应用菜单 list data | 2 rows: 功能演示 (devDemoSystem), 开发平台 (mainSystem) |
| 8 | Console errors on 应用菜单 | 0 errors |

### 4.2 DataV Frontend (jnpf-web-datascreen)

| Step | Action | Result |
|---|---|---|
| 1 | Navigate to http://localhost:3102/DataV/ (with token) | Page loaded |
| 2 | Sidebar navigation | 9 items: 大屏管理, 大屏分类, 数据源管理, 组件库, 全局变量, 数据集管理, 静态资源, 地图管理, 工具箱 |
| 3 | Template list | 66 templates loaded, pagination working |
| 4 | Console errors | 0 errors, 2 warnings |

### 4.3 UniApp H5 Frontend (jnpf-app-vue3)

| Step | Action | Result |
|---|---|---|
| 1 | Navigate to http://localhost:3800 | Login page loaded (V3.6.0) |
| 2 | Initial console | 1 error: "登录过期,请重新登录" (expected — no token) |
| 3 | Login with admin/000000 | SUCCESS, redirected to /pages/index/index |
| 4 | Home page content | "Baobab快速开发平台", 销售指数, 公告通知, 进行中的项目 |
| 5 | Bottom navigation | 首页, 协同, 应用, 消息, 我的 |
| 6 | Console errors after login | 0 new errors |

---

## Summary

| Level | Status |
|---|---|
| L1 Compile | PASS |
| L2 Startup | PASS (4/4 services) |
| L3 Health | PASS |
| L4 Browser | PASS (3/3 frontends) |

**Conclusion:** Stage 4 refactoring verified. All services start, all frontends render correctly, no console errors during normal operation.
