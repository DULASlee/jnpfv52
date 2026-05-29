# JNPF v5.2 工作区

v5.2 单一仓库：后端 + PC / 大屏 / 移动端前端 + 文档与 SQL 初始化脚本。克隆后按下方「从零搭建」即可本地运行。

## 目录结构

```
JNPF-v52/
├── backend/                 # .NET 6 API（application / framework / modularity / infrastructure）
│   └── web/                 # SQL 初始化脚本（已纳入 Git）
├── jnpf-web-vue3/           # PC 前端 → http://localhost:3100
├── jnpf-web-datascreen/     # 大屏前端 → http://localhost:8100/DataV/
├── jnpf-app-vue3/           # UniApp 移动端（H5 发行 + proxy）
├── docs/                    # 演示手册、架构文档
├── scripts/                 # 工具链验证等脚本
└── openspec/                # 知识库（OpenSpec）
```

## 数据库脚本（`backend/web/`）

| 文件 | 说明 |
|------|------|
| `主库脚本.sql` | 主库 DDL + 种子数据（约 45 张 `base_*` 表） |
| `jnpf_sundial_init_sqlserver.sql` | 调度库 DDL（SQL Server） |
| `jnpf_sundial_init.sql` | 调度库 DDL（通用参考） |
| `jnpf事件库脚本.sql` | 事件库 DDL（若使用独立事件库） |
| `web.config` | IIS 部署参考配置 |

以上文件均在 Git 中跟踪，克隆仓库即可获取。

## 从零搭建环境

### 1. 安装依赖

| 依赖 | 版本 |
|------|------|
| .NET SDK | 6.0+（见 `backend/global.json`） |
| Node.js | 18+ |
| pnpm | 8.x（`npm install -g pnpm@8`） |
| SQL Server | Express 或更高版本 |
| Python 3 | 可选，移动端 H5 本地代理用 |

### 2. 创建数据库

在 SSMS 或 `sqlcmd` 中执行：

```sql
-- 1. 创建主库（库名可按需修改，须与 ConnectionStrings 一致）
CREATE DATABASE ZXAF_V1_DevTest1;
GO

-- 2. 创建调度库
CREATE DATABASE jnpf_sundial;
GO

-- 3. 初始化主库
USE ZXAF_V1_DevTest1;
GO
-- 在 SSMS 中打开并执行：backend/web/主库脚本.sql

-- 4. 初始化调度库
USE jnpf_sundial;
GO
-- 执行：backend/web/jnpf_sundial_init_sqlserver.sql

-- 5. （可选）若使用独立事件库，再执行 backend/web/jnpf事件库脚本.sql
```

PowerShell 示例（需已安装 `sqlcmd`，并按本机实例名调整 `-S`）：

```powershell
sqlcmd -S "(local)\SQLEXPRESS" -Q "CREATE DATABASE ZXAF_V1_DevTest1"
sqlcmd -S "(local)\SQLEXPRESS" -Q "CREATE DATABASE jnpf_sundial"
sqlcmd -S "(local)\SQLEXPRESS" -d ZXAF_V1_DevTest1 -i backend\web\主库脚本.sql
sqlcmd -S "(local)\SQLEXPRESS" -d jnpf_sundial -i backend\web\jnpf_sundial_init_sqlserver.sql
```

### 3. 修改后端配置

复制或新建（**勿提交 Git**）：

`backend/application/JNPF.API.Entry/Configurations/ConnectionStrings.json`

```json
{
  "ConnectionStrings": {
    "ConnectionConfigs": [
      {
        "ConfigId": "default",
        "DBName": "ZXAF_V1_DevTest1",
        "DBType": "SqlServer",
        "Host": "localhost",
        "Port": "1433",
        "UserName": "sa",
        "Password": "你的密码"
      },
      {
        "ConfigId": "JNPF-Job",
        "DBName": "jnpf_sundial",
        "DBType": "SqlServer",
        "Host": "localhost",
        "Port": "1433",
        "UserName": "sa",
        "Password": "你的密码"
      }
    ]
  }
}
```

OA 入口若启用，同样在 `JNPF.OA.API.Entry/Configurations/` 下配置（已 gitignore）。

### 4. 安装依赖并启动

```powershell
# 后端（仓库根目录下）
cd backend
dotnet build
dotnet run --project application/JNPF.API.Entry/JNPF.API.Entry.csproj

# PC 前端
cd ..\jnpf-web-vue3
pnpm install
pnpm run dev

# 大屏前端
cd ..\jnpf-web-datascreen
pnpm install
pnpm run dev

# 移动端 H5（需先用 HBuilderX 发行 web，再启动代理）
cd ..\jnpf-app-vue3
python scripts/proxy_server.py
```

### 5. 访问

| 系统 | 地址 | 说明 |
|------|------|------|
| PC 前端 | http://localhost:3100/ | 须为 Vite 占用 3100，勿与其他进程冲突 |
| 后端 API 文档 | http://localhost:5000/newapi | 冷启动约 60–90s |
| 大屏 | http://localhost:8100/DataV/ | 须先在 PC 端登录，从菜单带 token 进入 |
| 移动端 | http://localhost:3800/ | 经 `proxy_server.py` 转发 API |

默认账号：**admin / 123456**（以库中种子数据为准）。

## 快速启动（已配好库时）

```powershell
cd backend
dotnet run --project application/JNPF.API.Entry/JNPF.API.Entry.csproj

cd ..\jnpf-web-vue3
pnpm run dev

cd ..\jnpf-web-datascreen
pnpm run dev
```

API 基址：`http://localhost:5000`（大屏、移动端配置已对齐该地址）。

## 文档

| 文档 | 说明 |
|------|------|
| [docs/v52-demo-manual.md](docs/v52-demo-manual.md) | 客户演示执行脚本 |
| [docs/v52-baseline-snapshot.md](docs/v52-baseline-snapshot.md) | 环境基线快照 |
| [TOOLCHAIN.md](TOOLCHAIN.md) | Superpowers / OpenSpec / episodic 工具链 |

验证工具链：`node scripts/verify-toolchain.mjs`

## 说明

- 历史工作区 `d:\liu202505v2` 仅作存档，日常开发以本仓库为准。
- `ConnectionStrings.json`、`.env.toolchain`、`node_modules`、`bin/obj` 等已在 `.gitignore` 中排除。
