# JNPF v5.2 客户演示操作手册

> **实测环境**：Windows 25H2 | SQL Server Express (local)\SQLEXPRESS | .NET 6.0 | Node.js + pnpm
> **实测日期**：2026-05-26 晚
> **后端版本标识**：JNPF: 3.4.7.0（HTTP Header 实测）
> **前端版本**：package.json version 3.6.0

---

## 第零部分：系统快速启动

### 0.1 开始前检查

#### 确认 SQL Server 运行

```powershell
Get-Service *SQL*
# 确认 SQL Server (SQLEXPRESS) 状态为 Running
```

如果未运行：
```powershell
Start-Service 'MSSQL$SQLEXPRESS'
```

#### 确认数据库可连接

```powershell
sqlcmd -S "(local)\SQLEXPRESS" -U sa -P "1qazxsw2" -Q "SELECT TOP 3 TABLE_NAME FROM ZXAF_V1_DevTest1.INFORMATION_SCHEMA.TABLES ORDER BY TABLE_NAME"
```

> **期望输出**：看到 `base_advanced_query_scheme`、`base_api_log`、`base_app_data` 等表名（`base_` 前缀 = v5.2 架构确认）

#### 确认源码目录完整

```powershell
Test-Path "d:\JNPF-v52\backend\application\JNPF.API.Entry\JNPF.API.Entry.csproj"
# 期望：True

Test-Path "d:\JNPF-v52\jnpf-web-vue3\node_modules"
# 期望：True（已有 1067 个包，无需 pnpm install）
```

---

### 0.1.1 项目实际配置（完整贴出）

#### ConnectionStrings.json
```json
{
  "ConnectionStrings": {
    "ConnectionConfigs": [
      {
        "Domain": "dev_v1.",
        "ConfigId": "default",
        "DBName": "ZXAF_V1_DevTest1",
        "DBType": "SqlServer",
        "Host": "(local)\\SQLEXPRESS",
        "Port": "1433",
        "UserName": "sa",
        "Password": "1qazxsw2",
        "DBSchema": "public"
      },
      {
        "ConfigId": "JNPF-Job",
        "DBName": "jnpf_sundial",
        "DBType": "SqlServer",
        "Host": "(local)\\SQLEXPRESS",
        "Port": "1433",
        "UserName": "sa",
        "Password": "1qazxsw2",
        "DBSchema": "public"
      }
    ]
  }
}
```

#### Cache.json
```json
{
  "Cache": {
    "CacheType": "MemoryCache",
    "ip": "127.0.0.1",
    "port": 6379,
    "RedisConnectionString": "{0}:{1}, poolsize=500,ssl=false,defaultDatabase=7"
  }
}
```
> ⚠️ **CacheType = MemoryCache**，不需要安装 Redis。

#### EventBus.json
```json
{
  "EventBus": {
    "EventBusType": "Memory",
    "HostName": "192.168.0.232",
    "UserName": "jnpf",
    "Password": "jnpf@2019"
  }
}
```
> ⚠️ **EventBusType = Memory**，不需要安装 RabbitMQ。

#### JWT.json
```json
{
  "JWTSettings": {
    "ValidateIssuerSigningKey": true,
    "IssuerSigningKey": "RkayGi4ltkMWrSQKsQTWic1VnakqsQfaJOmJIBUWE1gxGaS0IrJHxa9anjVAwuew",
    "ValidateIssuer": true,
    "ValidIssuer": "yinmaisoft",
    "ValidateAudience": true,
    "ValidAudience": "yinmaisoft",
    "ValidateLifetime": true,
    "ExpiredTime": 1440,
    "ClockSkew": 5
  }
}
```

---

### 0.1.2 端口总表

| 服务 | 端口 | 来源 | 状态 |
|------|------|------|------|
| **后端 API** | **:5000** | launchSettings.json `applicationUrl` | ✅ 实测确认 |
| **PC 前端** | **:3100** | jnpf-web-vue3/.env `VITE_PORT` | ✅ |
| **PC→API 代理** | → :5000 | jnpf-web-vue3/.env.development `VITE_PROXY` | ✅ 已匹配 |
| **大屏前端** | **:3102** | jnpf-web-datascreen/vite.config.js | ✅ |
| **大屏→API 代理** | → :30000 ❌ | jnpf-web-datascreen-vue3/.env.development | ⚠️ **需改为 5000** |
| **UniApp H5** | :3800（HBuilderX） | HBuilderX 内置 | 按需启动 |
| **UniApp→API** | → :30000 ❌ | jnpf-app-vue3/utils/define.js | ⚠️ **需改为 5000** |

---

### 0.2 启动后端 API

**前置修复：启用大屏模块（VisualData）**

> 实测发现 `JNPF.API.Entry.csproj` 中缺少 `JNPF.VisualData` 引用。已添加以下行：
> ```xml
> <ProjectReference Include="..\..\modularity\visualdata\JNPF.VisualData\JNPF.VisualData.csproj" />
> ```
> 如果大屏演示不需要，可跳过此步骤。

**打开终端 1**（PowerShell）：

```powershell
cd d:\JNPF-v52\backend
dotnet run --project application/JNPF.API.Entry/JNPF.API.Entry.csproj
```

**等待输出**（首次编译约 2-3 分钟）：

```
EventBus hosted service is running.
【2026/5/26 20:43:03】服务当天日程推送加载
```

> ⚠️ 启动过程中会出现以下**非致命错误**，可忽略：
> - `System.NullReferenceException` in `MemoryCache.GetAllKeys()` — 定时任务缓存初始化问题，不影响核心功能
> - 大量 `warning SA1xxx` — StyleCop 代码风格警告，不影响运行

**验证后端**（另开终端）：

```powershell
curl.exe -s -o NUL -w "%{http_code}" http://localhost:5000/api/oauth/Login
# 期望：返回 405（Method Not Allowed，说明路由存在，只是不接受GET）
# 如果返回 000 或 Connection refused → 后端未启动完成，再等 30 秒
```

**API 启动报错处理**：

| 报错 | 原因 | 解决 |
|------|------|------|
| `Connection refused` | 后端未启动完 | 等待日志出现 `服务当天日程推送加载` |
| `Login failed for user 'sa'` | SQL Server 密码错误 | 确认密码是 `1qazxsw2`，检查 SQL Server 认证模式 |
| `Cannot open database "ZXAF_V1_DevTest1"` | 数据库不存在 | 需要先还原数据库 |
| `Could not find a part of the path` | `SystemPath` 路径不存在 | 创建 `C:\wwwroot\Resources` 目录 |

---

### 0.3 启动 PC 前端

**打开终端 2**（PowerShell）：

```powershell
cd d:\JNPF-v52\jnpf-web-vue3
pnpm run dev
```

**等待输出**：

```
VITE v4.x.x  ready in xxx ms
➜  Local:   http://localhost:3100/
```

> node_modules 已安装（1067 个包），无需执行 `pnpm install`。

**验证**：浏览器打开 `http://localhost:3100/`，应看到登录页面。

---

### 0.4 启动大屏前端

**前置修复：大屏 API 代理地址**

大屏 `.env.development` 中 `VITE_PROXY` 当前值为 `http://localhost:30000`，需改为 `http://localhost:5000`：

```powershell
# 修改文件：d:\JNPF-v52\jnpf-web-datascreen\.env.development
# 将 VITE_PROXY = "http://localhost:30000"
# 改为 VITE_PROXY = "http://localhost:5000"
```

**打开终端 3**（PowerShell）：

```powershell
cd d:\JNPF-v52\jnpf-web-datascreen
pnpm run dev
```

**等待输出**：

```
Local:   http://localhost:3102/DataV/
```

**验证**：浏览器打开 `http://localhost:3102/DataV/`，应看到大屏登录页面。

---

### 0.5 启动移动端（HBuilderX）

**前置修复：UniApp API 地址**

```powershell
# 修改文件：d:\JNPF-v52\jnpf-app-vue3\utils\define.js
# 将 const baseURL = "http://localhost:30000"
# 改为 const baseURL = "http://localhost:5000"
```

1. 打开 **HBuilderX**
2. 文件 → 导入 → 从本地目录导入 → 选择 `d:\JNPF-v52\jnpf-app-vue3`
3. 菜单 运行 → 运行到浏览器 → Chrome
4. F12 打开手机模拟模式（选 iPhone 12 Pro）

> ⚠️ UniApp 是 HBuilderX 工程，不是 npm CLI 项目，必须通过 HBuilderX 运行。

---

### 0.6 全套系统启动验证清单

| # | 服务 | 验证 URL | 期望结果 | 不通过处理 |
|---|------|----------|----------|-----------|
| 1 | 后端 API | `http://localhost:5000/api/oauth/Login` | 405/415（路由存在） | 重启终端 1 |
| 2 | PC 前端 | `http://localhost:3100/` | 登录页（标题"智轩云"） | 重启终端 2 |
| 3 | 大屏前端 | `http://localhost:3102/DataV/` | 大屏登录页 | 重启终端 3 |
| 4 | 移动端 | HBuilderX 内置浏览器 | UniApp 登录页 | 重新运行 |

---

## 第一部分：演示准备

### 1.1 确认 admin 登录

#### admin 账号信息（数据库实测）

| 字段 | 值 |
|------|-----|
| f_id | 349057407209541 |
| f_account | admin |
| f_real_name | 管理员 |
| f_enabled_mark | 1（启用） |
| f_is_administrator | 1（超级管理员） |
| f_password | 045cbd671a8d67d2110a0b6098025551（加密哈希） |
| f_secretkey | 26916bdf390242c9b0ac7ec1442a329e |

#### 登录方式

前端登录时密码经 AES 加密传输（密钥：`EY8WePvjM5GGwQzn`），**不要在命令行尝试明文密码登录 API**。

直接在浏览器 `http://localhost:3100/` 登录页面输入：
- 账号：`admin`
- 密码：尝试 `admin123`（JNPF 常用默认密码）

> **如果密码不对**，可通过前端登录页面的「验证码登录」或联系管理员重置。
>
> **密码重置 SQL**（慎用，先备份原密码哈希）：
> ```sql
> -- 备份
> SELECT f_password, f_secretkey FROM base_user WHERE f_account='admin'
> -- 重置需要在代码中走 AES 加密流程，不要直接写 MD5
> ```

#### 三系统登录验证

| 系统 | URL | 账号 | 登录结果 |
|------|-----|------|----------|
| PC 前端 | `http://localhost:3100/` | admin | ________ |
| 大屏前端 | `http://localhost:3102/DataV/` | admin | ________ |
| 移动端 | HBuilderX 浏览器 | admin | ________ |

---

### 1.2 准备演示基础数据

#### a) 确认在线开发菜单存在

```sql
SELECT f_full_name, f_url_address, f_type, f_category
FROM base_module
WHERE f_full_name LIKE '%在线%' OR f_url_address LIKE '%onlineDev%' OR f_url_address LIKE '%visual%'
ORDER BY f_sort_code
```

**实测结果**（6 条记录）：

| 菜单名 | URL | 类型 | 分类 |
|--------|-----|------|------|
| 在线开发 | (目录) | 1 | Web |
| 功能设计 | onlineDev/webDesign | 2 | Web |
| 报表设计 | onlineDev/dataReport | 2 | Web |
| 大屏设计 | ${dataV}?token=${jnpfToken} | 7 | Web |
| 门户设计 | onlineDev/visualPortal | 2 | Web |
| 集成助手 | onlineDev/integrate | 2 | Web |

> ✅ 登录后在左侧菜单找到 **「在线开发」** → **「功能设计」** 即可进入低代码设计器。

#### b) 确认大屏表存在

```sql
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE 'blade_visual%'
```

**实测结果**（8 张表）：
- blade_visual
- blade_visual_category
- blade_visual_component
- blade_visual_config
- blade_visual_db
- blade_visual_glob
- blade_visual_map
- blade_visual_record

> ✅ 大屏数据库表完整，大屏功能可用。

#### c) 确认移动端菜单

```sql
SELECT f_full_name, f_url_address, f_type, f_category
FROM base_module
WHERE f_category LIKE '%App%'
ORDER BY f_sort_code
```

**实测结果**（7 条记录，包含外勤打卡、系统应用、功能参考等 App 菜单）。

> ✅ 移动端有可用菜单。

---

### 1.3 演示前 30 分钟检查清单

| # | 检查什么 | 怎么检查 | 通过标准 | 不通过怎么办 |
|---|----------|----------|----------|-------------|
| 1 | SQL Server 运行 | `Get-Service *SQL*` | Running | `Start-Service 'MSSQL$SQLEXPRESS'` |
| 2 | 数据库可连 | `sqlcmd -S "(local)\SQLEXPRESS" -U sa -P "1qazxsw2" -Q "SELECT 1"` | 返回 1 | 检查 SQL Server 服务 |
| 3 | 后端 API | 浏览器 `http://localhost:5000/api/oauth/Login` | 405 错误页 | 重启终端 1 |
| 4 | PC 前端 | 浏览器 `http://localhost:3100/` | 登录页 | 重启终端 2 |
| 5 | admin 可登录 | 输入账号密码登录 | 进入首页 | 检查账号状态 |
| 6 | 在线开发菜单 | 左侧菜单 → 在线开发 | 看到功能设计 | 检查 base_module 表 |
| 7 | 大屏前端 | 浏览器 `http://localhost:3102/DataV/` | 大屏登录页 | 重启终端 3 |
| 8 | 大屏代理端口 | 检查 .env.development 的 VITE_PROXY | 指向 :5000 | 修改后重启 |
| 9 | 移动端（可选） | HBuilderX 运行 | UniApp 页面 | 检查 define.js baseURL |

---

## 第二部分：三个演示场景

### 2.1 场景一：Web 低代码功能设计全流程

**演示目标**：展示从新建功能 → 拖拽设计表单 → 发布 → 菜单配置 → 权限分配的完整低代码流程。

**预计耗时**：8 分钟

**前提条件**：
- PC 前端已启动并登录 admin
- 左侧菜单能看到「在线开发」→「功能设计」

#### 步骤 1：进入功能设计

1. 点击左侧菜单 **「在线开发」** 展开子菜单
2. 点击 **「功能设计」**
3. 进入功能设计列表页

> **话术**：「这是我们的低代码功能设计平台，无需写代码，通过拖拽即可生成完整的业务功能。」

#### 步骤 2：新建功能

1. 点击页面右上角 **「新建」** 按钮
2. 弹出新建对话框，填写：

| 字段 | 填写值 | 说明 |
|------|--------|------|
| 功能名称 | `客户信息管理` | 演示用功能 |
| 功能编码 | `customer` | 英文标识 |
| 页面类型 | 选择 **「表单+列表」** | 同时生成表单和列表 |
| 所属分组 | 选择任意可用分组 | |

3. 点击 **「确定」**

> **如果找不到新建按钮**：检查 admin 是否有功能设计权限（f_is_administrator=1 表示超管，应有全部权限）

#### 步骤 3：设计表单

进入设计器后，界面布局：
- **左侧**：组件面板（各种可拖拽的表单组件）
- **中间**：画布区域（表单设计区）
- **右侧**：属性配置面板（选中组件后显示配置项）
- **顶部**：工具栏（保存、预览、发布等按钮）

从左侧组件面板拖拽以下组件到画布：

| 顺序 | 拖什么组件 | 拖完后改什么配置 |
|------|-----------|----------------|
| 1 | 单行输入 | 标签改为"客户名称"，字段名改为"customer_name"，勾选"必填" |
| 2 | 单行输入 | 标签改为"联系人"，字段名改为"contact_person" |
| 3 | 手机号 | 标签改为"联系电话"，字段名改为"phone" |
| 4 | 下拉选择 | 标签改为"客户类型"，字段名改为"customer_type"，选项添加：企业、个人、政府 |
| 5 | 多行输入 | 标签改为"地址"，字段名改为"address" |

> **操作提示**：拖拽后点击组件，右侧属性面板会显示该组件的配置项。
>
> **如果找不到某个组件**：在组件面板顶部搜索框中输入组件名搜索。

完成后点击顶部工具栏 **「保存」** 按钮。

> **话术**：「通过拖拽组件、配置属性，几分钟就完成了一个业务表单的设计，传统开发可能需要半天。」

#### 步骤 4：发布功能

1. 点击顶部工具栏 **「发布」** 按钮
2. 弹出发布对话框
3. 选择发布位置（选择一个上级目录）
4. 点击 **「确认发布」**

> ⚠️ **发布前必须确保**：
> - 表单设计已保存（formData 不为空，否则报 COM1013）
> - 列表列配置已完成（columnData 不为空，否则报 COM1014）
> - 已选择上级目录（否则报 D4017）

> **话术**：「发布后系统自动创建数据库表、生成 API 接口、配置前端路由，全程零代码。」

#### 步骤 5：菜单配置

发布成功后，需要在菜单管理中让其他用户看到这个功能：

1. 点击左侧菜单 **「系统管理」** → **「菜单管理」**（或 **「权限管理」** → **「菜单管理」**）
2. 找到刚发布的「客户信息管理」菜单
3. 确认菜单状态为启用

#### 步骤 6：验证功能

1. 在左侧菜单找到「客户信息管理」
2. 点击进入列表页
3. 点击「新增」按钮，填写测试数据：
   - 客户名称：`测试公司A`
   - 联系人：`张三`
   - 联系电话：`13800138000`
   - 客户类型：选择 `企业`
   - 地址：`北京市朝阳区`
4. 保存后确认列表中出现该记录

> **卡壳处理**：

| 问题 | 原因 | 解决 |
|------|------|------|
| 发布报 COM1013 | 表单设计为空 | 先拖拽组件保存再发布 |
| 发布报 COM1014 | 列表列为空 | 在设计器中切换到列表视图，添加列 |
| 发布报 D4017 | 未选上级目录 | 在发布对话框中选择父级菜单 |
| 左侧菜单看不到 | 未分配权限 | 菜单管理中检查权限配置 |

---

### 2.2 场景二：数字大屏全流程

**演示目标**：展示从新建大屏 → 拖拽组件 → 配置数据 → 预览发布的完整大屏流程。

**预计耗时**：6 分钟

**前提条件**：
- 后端 API 运行中（:5000）
- 大屏前端已启动（:3102/DataV/）
- 大屏 API 代理已改为 :5000
- admin 已登录大屏系统

#### 步骤 1：进入大屏管理

1. 浏览器打开 `http://localhost:3102/DataV/`
2. 使用 admin 账号登录
3. 进入大屏管理列表页

> **话术**：「这是我们的数字大屏设计平台，可以快速搭建数据可视化大屏，用于数据展示和汇报。」

#### 步骤 2：新建大屏

1. 点击 **「新建大屏」** 按钮
2. 填写：
   - 名称：`销售数据看板`
   - 分类：选择任意分类
3. 点击确定

#### 步骤 3：设计大屏

进入大屏设计器：
- **画布区域**：深色背景，可拖拽缩放
- **左侧面板**：组件库（图表、文本、图片、边框装饰等）
- **右侧面板**：组件属性和数据配置

从左侧组件库拖拽以下组件：

| 顺序 | 组件 | 位置建议 | 数据配置 |
|------|------|----------|----------|
| 1 | 柱状图 | 左侧上方 | 粘贴下方静态数据 |
| 2 | 饼图 | 左侧下方 | 粘贴下方静态数据 |
| 3 | 数字翻牌器 | 顶部 | 粘贴下方静态数据 |
| 4 | 文本 | 顶部标题 | 输入"2026年度销售数据看板" |
| 5 | 边框/装饰 | 四周 | 选一个好看的边框 |

#### 柱状图静态数据

在右侧数据面板中选择「静态数据」，粘贴：

```json
[
  {"name": "1月", "value": 120},
  {"name": "2月", "value": 98},
  {"name": "3月", "value": 156},
  {"name": "4月", "value": 189},
  {"name": "5月", "value": 234},
  {"name": "6月", "value": 278}
]
```

#### 饼图静态数据

```json
[
  {"name": "华东区", "value": 35},
  {"name": "华南区", "value": 25},
  {"name": "华北区", "value": 20},
  {"name": "西南区", "value": 12},
  {"name": "其他", "value": 8}
]
```

#### 数字翻牌器数据

```json
{"value": 12580}
```

> **话术**：「组件支持静态数据、API 接口、SQL 查询等多种数据源接入方式，满足不同场景的数据展示需求。」

#### 步骤 4：保存与预览

1. 点击右上角 **「保存」** 按钮
2. 点击 **「预览」** 按钮在新窗口查看效果

> **预览 URL 格式**：`http://localhost:3102/DataV/view.html?id={大屏ID}&token={token}&isDev=1`
>
> ⚠️ 开发环境预览必须带 `isDev=1` 参数。

> **卡壳处理**：

| 问题 | 原因 | 解决 |
|------|------|------|
| 大屏页面空白 | API 代理未改 | 检查 .env.development 中 VITE_PROXY 是否为 :5000 |
| 登录失败 | 大屏后端不可达 | 确认后端 API 运行中 |
| 组件数据不显示 | 静态数据格式错误 | 确认 JSON 格式正确 |
| 预览 404 | 缺少 isDev 参数 | URL 加上 &isDev=1 |

---

### 2.3 场景三：UniApp 移动端全流程

**演示目标**：展示移动端 H5 应用，强调与 PC 端数据互通。

**预计耗时**：4 分钟

**前提条件**：
- 后端 API 运行中
- UniApp define.js baseURL 已改为 `http://localhost:5000`
- HBuilderX 已导入工程并运行到浏览器
- 场景一中已发布的功能（如果有移动端选项）

#### 步骤 1：打开移动端

1. 在 HBuilderX 中运行到 Chrome 浏览器
2. F12 打开手机模拟模式
3. 选择机型：**iPhone 12 Pro**（390×844）
4. 地址栏输入 UniApp H5 地址

#### 步骤 2：登录

1. 输入账号：`admin`
2. 输入密码：与 PC 端相同
3. 点击登录

> **话术**：「移动端和 PC 端共用同一套账号体系，同一个账号可以在多端登录。」

#### 步骤 3：浏览应用

1. 登录后底部有 Tab 栏
2. 点击 **「应用」** Tab（从左数第 2-3 个）
3. 查看可用菜单列表

> **注意**：应用 Tab 只展示 `children.length > 0` 的菜单分组。如果某个分组下没有子菜单，不会显示。

#### 步骤 4：数据互通演示

1. 在移动端打开一个已有功能（如场景一发布的客户信息管理）
2. 查看数据列表
3. **强调**：这里看到的数据和 PC 端完全一致，因为共用同一张数据库表

> **话术**：「这就是低代码平台的强大之处——一次设计，多端运行。PC 端和移动端共享同一份数据，无需额外开发接口。」

> **卡壳处理**：

| 问题 | 原因 | 解决 |
|------|------|------|
| 页面空白/API 报错 | baseURL 未改 | 修改 define.js 中 baseURL 为 :5000 |
| 应用 Tab 无内容 | 无 App 类菜单 | 在菜单管理中给功能添加 App 分类 |
| HBuilderX 运行失败 | 缺少 uni_modules | 在 HBuilderX 中右键安装插件 |

---

## 第三部分：演示执行参考

### 3.1 推荐演示顺序与时间

| 顺序 | 内容 | 时间 | 累计 | 备注 |
|------|------|------|------|------|
| 1 | 开场 + 登录系统 | 2 min | 0:02 | 展示登录页、首页仪表盘 |
| 2 | 场景一：Web 低代码 | 8 min | 0:10 | **核心演示**，重点讲拖拽和发布 |
| 3 | 场景二：数字大屏 | 6 min | 0:16 | 视觉冲击，展示数据绑定 |
| 4 | 场景三：移动端 | 4 min | 0:20 | 多端能力，数据互通 |
| 5 | 总结 + 问答 | 3 min | 0:23 | 回顾核心优势 |

### 3.2 演示话术建议

#### 场景一话术

1. **开场**：「接下来演示我们平台的核心能力——低代码开发。传统开发一个新功能需要前后端配合、写数据库、写接口，至少 2-3 天。我们用拖拽的方式，10 分钟内完成。」
2. **拖拽时**：「平台内置了几十种常用组件，覆盖 90% 以上的业务场景。每个组件都有丰富的属性配置，必填项、默认值、联动规则都可以直接配置。」
3. **发布时**：「点击发布后，系统自动完成建表、生成接口、配置路由，传统开发需要 DBA 建表、后端写 CRUD、前端写页面的工作，全部自动化。」

#### 场景二话术

1. **开场**：「除了业务功能，数据可视化也是企业刚需。我们的大屏设计器可以快速搭建数据看板。」
2. **配置时**：「支持静态数据、API 接口、SQL 查询、WebSocket 等多种数据源，可以实时展示业务数据。」
3. **预览时**：「大屏可以直接投到会议室电视上，用于日常数据汇报。」

#### 场景三话术

1. **开场**：「移动办公是现在的趋势，我们的低代码平台天然支持多端运行。」
2. **展示时**：「刚才在 PC 端创建的功能，在移动端直接可用。数据是同一份，不需要额外开发移动端接口。」
3. **总结**：「一次设计、多端运行、数据互通，这就是低代码平台的核心价值。」

### 3.3 应急方案

| 突发情况 | 表现 | 处理方法 | 恢复时间 |
|----------|------|----------|----------|
| 后端 API 挂了 | 所有页面请求失败，显示网络错误 | 终端 1 Ctrl+C → `dotnet run --project application/JNPF.API.Entry/JNPF.API.Entry.csproj` | ~2 分钟 |
| 数据库断连 | 登录失败，提示数据库错误 | `Get-Service *SQL*` → `Start-Service 'MSSQL$SQLEXPRESS'` | 30 秒 |
| PC 前端白屏 | 页面加载不出 | 终端 2 Ctrl+C → `pnpm run dev` | 30 秒 |
| 大屏代理错误 | 大屏登录失败或数据加载失败 | 检查 .env.development 的 VITE_PROXY，改为 `http://localhost:5000`，重启终端 3 | 1 分钟 |
| 发布报错 | COM1013/COM1014/D4017 | 按错误码对照表处理（见场景一步骤 4） | 1 分钟 |
| 登录密码错误 | 提示密码不正确 | 尝试其他常见密码（admin、123456），或重置 | 2 分钟 |
| 菜单看不到 | 发布成功但左侧无菜单 | 检查菜单管理中是否已添加，或刷新页面 | 30 秒 |
| 移动端无数据 | 应用 Tab 空白 | 确认 define.js 的 baseURL 已改为 :5000 | 1 分钟 |

---

## 附录

### A. 关键路径速查

| 项目 | 路径 |
|------|------|
| 后端源码 | `d:\JNPF-v52\backend` |
| 后端入口项目 | `d:\JNPF-v52\backend\application\JNPF.API.Entry` |
| 后端配置文件 | `d:\JNPF-v52\backend\application\JNPF.API.Entry\Configurations\` |
| 后端模块目录 | `d:\JNPF-v52\backend\modularity\` |
| PC 前端 | `d:\JNPF-v52\jnpf-web-vue3` |
| 大屏前端 | `d:\JNPF-v52\jnpf-web-datascreen` |
| UniApp 移动端 | `d:\JNPF-v52\jnpf-app-vue3` |
| UniApp API 配置 | `d:\JNPF-v52\jnpf-app-vue3\utils\define.js` |
| 大屏代理配置 | `d:\JNPF-v52\jnpf-web-datascreen\.env.development` |

### B. 关键 URL 速查

| 系统 | URL |
|------|-----|
| PC 前端 | `http://localhost:3100/` |
| 后端 API | `http://localhost:5000` |
| 后端 Swagger（Knife4j） | `http://localhost:5000/newapi` |
| 大屏前端 | `http://localhost:3102/DataV/` |
| 大屏预览 | `http://localhost:3102/DataV/view.html?id={ID}&token={token}&isDev=1` |

### C. 数据库速查命令

```powershell
# 连接数据库
sqlcmd -S "(local)\SQLEXPRESS" -U sa -P "1qazxsw2" -d ZXAF_V1_DevTest1

# 查看 admin 账号
sqlcmd -S "(local)\SQLEXPRESS" -U sa -P "1qazxsw2" -d ZXAF_V1_DevTest1 -Q "SELECT f_id, f_account, f_real_name, f_enabled_mark FROM base_user WHERE f_account='admin'"

# 查看在线开发相关菜单
sqlcmd -S "(local)\SQLEXPRESS" -U sa -P "1qazxsw2" -d ZXAF_V1_DevTest1 -Q "SELECT f_full_name, f_url_address FROM base_module WHERE f_full_name LIKE '%在线%' OR f_url_address LIKE '%onlineDev%'"

# 查看大屏表
sqlcmd -S "(local)\SQLEXPRESS" -U sa -P "1qazxsw2" -d ZXAF_V1_DevTest1 -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE 'blade_visual%'"

# 查看低代码设计记录
sqlcmd -S "(local)\SQLEXPRESS" -U sa -P "1qazxsw2" -d ZXAF_V1_DevTest1 -Q "SELECT TOP 5 f_id, f_full_name FROM base_visual_dev"
```

### D. 关闭/重启服务命令

```powershell
# 关闭后端 API
# 在终端 1 按 Ctrl+C

# 重启后端 API
cd d:\JNPF-v52\backend
dotnet run --project application/JNPF.API.Entry/JNPF.API.Entry.csproj

# 关闭 PC 前端
# 在终端 2 按 Ctrl+C

# 重启 PC 前端
cd d:\JNPF-v52\jnpf-web-vue3
pnpm run dev

# 关闭大屏前端
# 在终端 3 按 Ctrl+C

# 重启大屏前端
cd d:\JNPF-v52\jnpf-web-datascreen
pnpm run dev

# 关闭移动端
# 在 HBuilderX 中停止运行，或关闭浏览器标签页
```

---

> **手册编写完成。请审核后使用。**
