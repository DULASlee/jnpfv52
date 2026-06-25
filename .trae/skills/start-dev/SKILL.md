---
name: "start-dev"
description: "Starts JNPF dev environment (backend + frontend + port verification). Invoke when user wants to start development, run the project, launch dev servers, or asks how to start the system."
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

### Step 3: 启动后端（后台运行）

```bash
cd backend && dotnet run --project application/JNPF.API.Entry/JNPF.API.Entry.csproj
```

- 预期端口：`:5000`（或 launchSettings.json 配置的端口）
- 等待输出 `Now listening on: http://localhost:5000`
- 如果 30 秒内未启动 → 报告错误日志

### Step 4: 启动前端主项目（后台运行）

```bash
cd jnpf-web-vue3 && pnpm run dev
```

- 预期端口：`:3100`
- 等待 Vite 输出 `Local: http://localhost:3100/`

### Step 5: 启动 DataV 大屏（可选，询问用户）

询问用户："是否同时启动 DataV 大屏（:8100）和 UniApp 移动端（:3800）？"

如果用户确认：

```bash
cd jnpf-web-datascreen && pnpm run dev    # :8100
cd jnpf-app-vue3 && pnpm run dev          # :3800
```

### Step 6: 输出启动状态表

```
## 开发环境启动状态

| 服务 | 端口 | 状态 | 访问地址 |
|------|------|------|---------|
| 后端 API | 5000 | ✅ Running | http://localhost:5000 |
| PC 前端 | 3100 | ✅ Running | http://localhost:3100 |
| DataV 大屏 | 8100 | ✅/⏸️ | http://localhost:8100/DataV/ |
| UniApp H5 | 3800 | ✅/⏸️ | http://localhost:3800 |

### 默认账号
- 管理员：admin / 123456

### 下一步
- 访问 http://localhost:3100 登录系统
- 如需停止所有服务，运行：pkill -f "dotnet run" && pkill -f "vite"
```
