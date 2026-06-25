---
name: start-dev
description: 启动 JNPF 开发环境（前端+后端+端口验证）。当用户要启动开发环境、跑项目、启动开发服务器、或问如何启动系统时触发。
---

# Start Dev Environment

按正确顺序启动后端 + 前端服务，并验证各端口就绪。

> **环境前置条件：** 见 CLAUDE.md "Build & Run" 章节。需要 .NET 8 SDK、Node.js 18+、pnpm、SQL Server 连接。

## 执行步骤

### Step 1: 检查依赖

运行以下命令验证开发环境就绪：

```bash
dotnet --version          # 预期：8.0.x
node --version            # 预期：v18+
pnpm --version            # 预期：8+
```

如果任何命令失败 → 停止，报告缺失的依赖。

### Step 2: 检查数据库连接

读取后端配置确认数据库连接可用：

```bash
# 读取连接字符串（不泄露密码）
grep -A 2 "ConnectionStrings" backend/application/JNPF.API.Entry/Configurations/database.json | head -5
```

如果连接字符串指向 `localhost` 或 `127.0.0.1` → 提醒用户确认 SQL Server 已启动。

### Step 3: 使用统一脚本启动开发环境

> **铁律：** 启动开发环境只能通过统一脚本，禁止直接执行 `npm run dev` / `dotnet run` / `dotnet watch`。

```powershell
powershell -ExecutionPolicy Bypass -File D:\JNPF-v52\start-dev.ps1
```

脚本会自动：
1. 清理旧进程
2. 启动前端（:3100）
3. 启动后端（:5000）热重载

### Step 4: 验证端口就绪

等待脚本输出确认各服务就绪：

- 后端 API：`:5000` — 预期输出 `Now listening on: http://localhost:5000`
- PC 前端：`:3100` — 预期 Vite 输出 `Local: http://localhost:3100/`

如果 30 秒内未启动 → 报告错误日志。

### Step 5: 输出启动状态表

```
## 开发环境启动状态

| 服务 | 端口 | 状态 | 访问地址 |
|------|------|------|---------|
| 后端 API | 5000 | ✅ Running | http://localhost:5000 |
| PC 前端 | 3100 | ✅ Running | http://localhost:3100 |

### 默认账号
- 管理员：admin / 123456

### 下一步
- 访问 http://localhost:3100 登录系统
- 如需停止所有服务，运行：pkill -f "dotnet run" && pkill -f "vite"
```
