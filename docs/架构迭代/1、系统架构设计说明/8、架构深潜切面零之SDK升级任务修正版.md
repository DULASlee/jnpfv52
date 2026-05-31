# JNPF V5.2 .NET 10 升级 — 修正版执行任务书

> **基于代码库实际审计结果修正，修正日期：2026-05-31**
> **原始方案中的错误已在本文档中逐一修正，工程师可直接按本文档执行。**

---

## 修正摘要

| 原始方案问题 | 修正内容 |
|-------------|---------|
| 解决方案文件名错误（`JNPF.sln`） | 改为 `zx_lowcode_netcore.sln`（主解决方案） |
| 未提及 framework 层 multi-targeting | 新增第四步-B：framework 层 TFM 单独处理 |
| 未提及备份文件和重复项目 | 新增第一步-B：清理 6 个冗余 csproj |
| `dotnet package search` 在 SDK 6.0 下不可用 | 调整执行顺序：先升级 global.json 再查包版本 |
| NuGet 检查遗漏 5 个高风险包 | 补充完整高风险包清单 |
| 未提及嵌入式 native DLL 兼容性 | 新增第六步-A：native 二进制验证 |
| 条件编译块处理缺失 | 新增详细的操作步骤和示例 |
| 时间预算偏乐观 | 修正为 8-12 小时（含缓冲） |

---

## 前置条件确认

在开始之前，工程师确认以下环境就绪：

```powershell
# 确认 .NET 10 SDK 已安装
dotnet --list-sdks
# 预期输出包含：10.0.202 [C:\Program Files\dotnet\sdk]

# 确认 .NET 10 Runtime 已安装
dotnet --list-runtimes
# 预期输出包含：Microsoft.AspNetCore.App 10.0.6

# 确认当前分支
git branch --show-current
# 预期：main
```

---

## 第一步-A：项目盘点（15分钟）

> **修正：命令改为 PowerShell，解决方案名改为 `zx_lowcode_netcore.sln`**

在 `D:\JNPF-v52\backend` 目录下执行：

```powershell
# 1.1 列出所有项目及当前 TargetFramework
Write-Host "=== 项目清单 ==="
Get-ChildItem -Recurse -Filter "*.csproj" | Where-Object { $_.Name -notlike "*Backup*" } | ForEach-Object {
    $tfm = Select-String -Path $_.FullName -Pattern '<TargetFramework>([^<]+)' -AllMatches | ForEach-Object { $_.Matches.Groups[1].Value }
    if (-not $tfm) { $tfm = "(继承自 Directory.Build.props)" }
    [PSCustomObject]@{ TFM = $tfm; Path = $_.FullName.Replace("D:\JNPF-v52\backend\", "") }
} | Sort-Object TFM, Path | Format-Table -AutoSize

Write-Host "`n=== 项目总数（不含备份） ==="
(Get-ChildItem -Recurse -Filter "*.csproj" | Where-Object { $_.Name -notlike "*Backup*" }).Count

Write-Host "`n=== TargetFramework 统计 ==="
Get-ChildItem -Recurse -Filter "*.csproj" | Where-Object { $_.Name -notlike "*Backup*" } | ForEach-Object {
    $tfm = Select-String -Path $_.FullName -Pattern '<TargetFramework>([^<]+)' -AllMatches | ForEach-Object { $_.Matches.Groups[1].Value }
    if ($tfm) { $tfm } else { "(继承自父级)" }
} | Group-Object | Sort-Object Count -Descending | Format-Table Name, Count
```

**预期结果：**

```
实际项目：约 52 个（不含 4 个 Backup 文件）
├── 继承自根 Directory.Build.props（net6.0）：约 43 个项目
├── 继承自 framework/Directory.Build.props（net5.0;net6.0;net7.0）：约 5 个项目
├── 在 csproj 中显式指定 net6.0：约 7 个项目（framework 层）
└── 备份文件（不参与编译）：4 个
```

**报告模板 1 — 项目清单：**

```
=== 项目清单 ===
[粘贴输出]

结论：共___个项目（不含备份），其中：
  - 继承根 TFM（net6.0）：___个
  - 继承 framework 多目标：___个
  - 显式指定 net6.0：___个
```

---

## 第一步-B：清理冗余文件（10分钟）

> **原始方案完全遗漏此步骤。这些文件会在升级时造成编译冲突。**

```powershell
# 确认要删除的文件（先 dry-run）
Write-Host "=== 待清理的冗余 csproj ==="

# 备份文件
$backups = @(
    "application\JNPF.API.Entry\JNPF - Backup.API.Entry.csproj"
    "application\JNPF.OA.API.Entry\JNPF - Backup.API.Entry.csproj"
    "framework\JNPF\JNPF - Backup.csproj"
)

# 重复文件（subdev 目录下的 ZxDev 副本）
$duplicates = @(
    "modularity\subdev\JNPF.SubDev\JNPF.ZxDev.csproj"
    "modularity\subdev\JNPF.SubDev.Entitys\JNPF.ZxDev.Entitys.csproj"
)

# 名称错误文件（zxdev 目录下用了 SubDev 的名字）
$misnamed = @(
    "modularity\zxdev\JNPF.ZxDev.Interfaces\JNPF.SubDev.Interfaces.csproj"
)

$all = $backups + $duplicates + $misnamed
foreach ($f in $all) {
    $fullPath = "D:\JNPF-v52\backend\$f"
    if (Test-Path $fullPath) {
        Write-Host "[存在] $f"
    } else {
        Write-Host "[缺失] $f"
    }
}

# 确认无误后执行删除
Write-Host "`n=== 执行删除 ==="
foreach ($f in $all) {
    $fullPath = "D:\JNPF-v52\backend\$f"
    if (Test-Path $fullPath) {
        Remove-Item $fullPath -Force
        Write-Host "[已删除] $f"
    }
}
```

**删除后验证：**

```powershell
# 确认没有残留的 Backup 文件
Get-ChildItem -Recurse -Filter "*Backup*.csproj" | Select-Object FullName
# 预期：无输出

# 确认 subdev 目录下没有 ZxDev 副本
Get-ChildItem "D:\JNPF-v52\backend\modularity\subdev" -Recurse -Filter "*.csproj" | Select-Object Name
# 预期：只有 JNPF.SubDev.csproj, JNPF.SubDev.Entitys.csproj, JNPF.SubDev.Interfaces.csproj
```

---

## 第二步：NuGet 核心依赖兼容性检查（1.5小时）

> **修正：先升级 global.json 再执行 `dotnet package search`；补充 5 个高风险包。**

### 2.1 暂时解锁 SDK 版本（仅用于查询）

```powershell
# 备份原始 global.json
Copy-Item "D:\JNPF-v52\backend\global.json" "D:\JNPF-v52\backend\global.json.bak"

# 临时改为 10.0.x 以便使用 dotnet package search
$globalJson = @{
    sdk = @{
        version = "10.0.202"
        rollForward = "latestFeature"
        allowPrerelease = $false
    }
} | ConvertTo-Json
$globalJson | Set-Content "D:\JNPF-v52\backend\global.json"

# 验证
dotnet --version
# 预期：10.0.202
```

### 2.2 列出当前解决方案中实际引用的所有 NuGet 包版本

```powershell
Write-Host "=== 当前引用的 NuGet 包版本 ==="
Get-ChildItem -Recurse -Filter "*.csproj" | Where-Object { $_.Name -notlike "*Backup*" } | ForEach-Object {
    Select-String -Path $_.FullName -Pattern 'PackageReference Include="([^"]+)" Version="([^"]+)"' -AllMatches
} | ForEach-Object {
    $_.Matches | ForEach-Object {
        [PSCustomObject]@{
            Package = $_.Groups[1].Value
            Version = $_.Groups[2].Value
        }
    }
} | Sort-Object Package, Version -Unique | Format-Table -AutoSize
```

### 2.3 检查核心包的 .NET 10 兼容性

```powershell
# 核心业务依赖
$corePackages = @(
    "SqlSugarCore"
    "Serilog.AspNetCore"
    "Serilog.Sinks.File"
    "Serilog.Sinks.Seq"
    "Serilog.Formatting.Compact"
    "RabbitMQ.Client"
    "Quartz"
    "Mapster"
    "Mapster.DependencyInjection"
    "CSRedisCore"
    "Newtonsoft.Json"
    "Swashbuckle.AspNetCore"
    "Microsoft.AspNetCore.Authentication.JwtBearer"
    "MiniProfiler.AspNetCore.Mvc"
    "Dapper.Contrib"
    "IGeekFan.AspNetCore.Knife4jUI"
)

# 高风险包（原始方案遗漏）
$highRiskPackages = @(
    "JavaScriptEngineSwitcher.ChakraCore"
    "JavaScriptEngineSwitcher.ChakraCore.Native.win-x64"
    "JavaScriptEngineSwitcher.V8"
    "JavaScriptEngineSwitcher.V8.Native.linux-x64"
    "JavaScriptEngineSwitcher.V8.Native.win-x64"
    "FreeSpire.Office"
    "DingDing.SDK.NetCore"
    "Aspose.Cells"
    "Aspose.Words"
    "OnceMi.AspNetCore.OSS"
    "Senparc.Weixin.MP"
    "Senparc.Weixin.Work"
    "Senparc.Weixin.WxOpen"
    "NPOI"
    "IPTools.China"
    "AlipaySDKNet.Standard"
    "AlibabaCloud.SDK.Dysmsapi20170525"
    "TencentCloudSDK.Sms"
    "MailKit"
    "SixLabors.ImageSharp"
    "SkiaSharp.NativeAssets.Linux.NoDependencies"
    "Yitter.IdGenerator"
    "UAParser"
    "Ben.Demystifier"
    "Microsoft.CodeAnalysis.CSharp"
    "xunit.extensibility.execution"
    "Roslynator.Analyzers"
    "StyleCop.Analyzers"
)

$allPackages = $corePackages + $highRiskPackages

Write-Host "=== 核心包 .NET 10 兼容性检查 ==="
foreach ($pkg in $allPackages) {
    Write-Host "`n--- $pkg ---"
    try {
        $result = dotnet package search $pkg --take 1 2>&1
        $result | Select-Object -First 8
    } catch {
        Write-Host "查询失败: $_"
    }
}
```

### 2.4 恢复 global.json（查询完毕后）

```powershell
# 恢复原始 global.json（升级时再正式修改）
Copy-Item "D:\JNPF-v52\backend\global.json.bak" "D:\JNPF-v52\backend\global.json" -Force
Remove-Item "D:\JNPF-v52\backend\global.json.bak"
dotnet --version
# 预期：6.0.428（恢复到 .NET 6 SDK）
```

**报告模板 2 — NuGet 兼容性：**

```
=== 当前引用的 NuGet 包版本 ===
[粘贴 2.2 的输出]

=== 核心包兼容性判断 ===
┌─────────────────────────────────┬──────────┬──────────┬───────────┬────────┐
│ 包名                             │ 当前版本  │ 最新版本  │ .NET 10兼容│ 风险    │
├─────────────────────────────────┼──────────┼──────────┼───────────┼────────┤
│ SqlSugarCore                    │          │          │ ✅/❌/⚠️   │ 低/中/高│
│ Serilog.AspNetCore              │          │          │           │        │
│ RabbitMQ.Client                 │          │          │           │        │
│ Mapster                         │          │          │           │        │
│ CSRedisCore                     │          │          │           │        │
│ Swashbuckle.AspNetCore          │          │          │           │        │
│ MiniProfiler.AspNetCore.Mvc     │          │          │           │        │
│ IGeekFan.AspNetCore.Knife4jUI   │          │          │           │        │
│ Dapper.Contrib                  │          │          │           │        │
├─────────────────────────────────┼──────────┼──────────┼───────────┼────────┤
│ 【高风险包】                     │          │          │           │        │
│ JavaScriptEngineSwitcher.Chakra │          │          │           │        │
│ JavaScriptEngineSwitcher.V8     │          │          │           │        │
│ FreeSpire.Office                │          │          │           │        │
│ DingDing.SDK.NetCore            │          │          │           │        │
│ Aspose.Cells / Words            │          │          │           │        │
│ NPOI                            │          │          │           │        │
└─────────────────────────────────┴──────────┴──────────┴───────────┴────────┘

是否有阻塞项（核心包不支持 .NET 10）：是/否
阻塞项描述：___
高风险包处理建议：___
```

---

## 第三步：基线性能记录（30分钟）

> **保持不变，但补充 PowerShell 命令**

确保在 main 分支、global.json 指向 SDK 6.0 时执行。

```powershell
# 3.1 后端编译时间
Write-Host "=== .NET 6 编译时间 ==="
dotnet clean "D:\JNPF-v52\backend\zx_lowcode_netcore.sln" --configuration Release -v q 2>$null
$times = @()
for ($i = 1; $i -le 3; $i++) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    dotnet build "D:\JNPF-v52\backend\zx_lowcode_netcore.sln" --configuration Release 2>$null | Out-Null
    $sw.Stop()
    $times += $sw.Elapsed.TotalSeconds
    Write-Host "  第${i}次: $([math]::Round($sw.Elapsed.TotalSeconds, 1))秒"
}
$avg = ($times | Measure-Object -Average).Average
Write-Host "  平均: $([math]::Round($avg, 1))秒"

# 3.2 启动时间（手动）
Write-Host "`n=== .NET 6 启动时间 ==="
Write-Host "请手动执行："
Write-Host "1. cd D:\JNPF-v52\backend\application\JNPF.API.Entry"
Write-Host "2. dotnet run --configuration Release --no-build"
Write-Host "3. 等到日志输出 'Application started' 或 'Listening on'"
Write-Host "4. Ctrl+C 停止，记录 wall clock time"
Write-Host "5. 重复 3 次取平均"

# 3.3 前端产物大小（如已构建）
Write-Host "`n=== 前端构建产物大小 ==="
$distPath = "D:\JNPF-v52\jnpf-web-vue3\dist"
if (Test-Path $distPath) {
    $size = (Get-ChildItem $distPath -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
    Write-Host "  dist 总大小: $([math]::Round($size, 1)) MB"
} else {
    Write-Host "  dist 目录不存在，跳过"
}
```

**报告模板 3 — 基线数据：**

```
=== .NET 6 基线性能 ===
编译时间（Release）：___秒 / ___秒 / ___秒，平均___秒
冷启动时间：___秒 / ___秒 / ___秒，平均___秒
启动后内存占用(RSS)：___MB
前端构建产物总大小：___MB

注：如无法测量 .NET 6 基线（如环境已切换），标注"未测"，
    升级后只记录 .NET 10 的绝对值。
```

---

## 第四步-A：创建升级分支 + 修改 TFM（10分钟）

```powershell
# 4.1 创建升级分支
git checkout -b feature/dotnet10-upgrade

# 4.2 正式升级 global.json
$globalJson = @{
    sdk = @{
        version = "10.0.202"
        rollForward = "latestFeature"
        allowPrerelease = $false
    }
} | ConvertTo-Json
$globalJson | Set-Content "D:\JNPF-v52\backend\global.json"

# 验证
dotnet --version
# 预期：10.0.202

# 4.3 修改根 Directory.Build.props（影响约 43 个项目）
$rootProps = Get-Content "D:\JNPF-v52\backend\Directory.Build.props" -Raw
$rootProps = $rootProps -replace '<TargetFramework>net6\.0</TargetFramework>', '<TargetFramework>net10.0</TargetFramework>'
Set-Content "D:\JNPF-v52\backend\Directory.Build.props" $rootProps

# 验证
Select-String -Path "D:\JNPF-v52\backend\Directory.Build.props" -Pattern "TargetFramework"
# 预期：<TargetFramework>net10.0</TargetFramework>
```

---

## 第四步-B：Framework 层 Multi-Targeting 处理（30分钟）

> **原始方案完全遗漏此步骤。这是升级中最容易出错的部分。**

Framework 层有自己的 `Directory.Build.props`，设置了 `TargetFrameworks=net5.0;net6.0;net7.0`。
升级策略：**放弃 multi-targeting，只保留 net10.0**（因为 net5.0 和 net7.0 已 EOL）。

### 4B.1 修改 framework/Directory.Build.props

```powershell
$fwProps = Get-Content "D:\JNPF-v52\backend\framework\Directory.Build.props" -Raw

# 替换 multi-targeting 为单目标
$fwProps = $fwProps -replace '<TargetFrameworks>net5\.0;net6\.0;net7\.0</TargetFrameworks>', '<TargetFramework>net10.0</TargetFramework>'

Set-Content "D:\JNPF-v52\backend\framework\Directory.Build.props" $fwProps

# 验证
Select-String -Path "D:\JNPF-v52\backend\framework\Directory.Build.props" -Pattern "TargetFramework"
# 预期：<TargetFramework>net10.0</TargetFramework>
```

### 4B.2 清理 framework 项目中的条件编译块

以下 7 个 .csproj 有 `Condition="'$(TargetFramework)' == 'net6.0'"` 等条件块，需要逐个处理：

| # | 文件 | 需要处理的内容 |
|---|------|---------------|
| 1 | `framework/JNPF/JNPF.csproj` | MiniProfiler 条件版本、显式 `<TargetFramework>net6.0</TargetFramework>` |
| 2 | `framework/JNPF.Extras.Authentication.JwtBearer/*.csproj` | JwtBearer 条件版本、显式 TFM |
| 3 | `framework/JNPF.Extras.DatabaseAccessor.Dapper/*.csproj` | DI.Abstractions 条件版本 |
| 4 | `framework/JNPF.Extras.DatabaseAccessor.SqlSugar/*.csproj` | DI.Abstractions 条件版本、显式 TFM |
| 5 | `framework/JNPF.Extras.DependencyModel.CodeAnalysis/*.csproj` | 多个包的条件版本、显式 TFM |
| 6 | `framework/JNPF.Extras.Logging.Serilog/*.csproj` | Serilog 条件版本、DI.Abstractions 条件版本 |
| 7 | `framework/JNPF.Extras.ObjectMapper.Mapster/*.csproj` | Mapster 条件版本、显式 TFM |

**处理原则：**

```
1. 移除 csproj 中显式的 <TargetFramework>net6.0</TargetFramework>
   → 让它继承 framework/Directory.Build.props 的 net10.0

2. 将条件编译块简化为无条件引用
   例如：
   【改前】
   <PackageReference Include="MiniProfiler.AspNetCore.Mvc" Version="4.3.8"
       Condition="'$(TargetFramework)' == 'net6.0' Or '$(TargetFramework)' == 'net7.0'" />
   <PackageReference Include="MiniProfiler.AspNetCore.Mvc" Version="4.2.22"
       Condition="'$(TargetFramework)' == 'net5.0'" />

   【改后】
   <PackageReference Include="MiniProfiler.AspNetCore.Mvc" Version="4.3.8" />

3. 对于 Microsoft.Extensions.* 等框架包，统一升级到 10.0.x 版本
   例如：
   【改前】
   <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions"
       Version="5.0.0" Condition="'$(TargetFramework)' == 'net5.0'" />
   <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions"
       Version="6.0.0" Condition="'$(TargetFramework)' == 'net6.0'" />
   <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions"
       Version="7.0.0" Condition="'$(TargetFramework)' == 'net7.0'" />

   【改后】
   <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions"
       Version="10.0.0" />
```

**逐个处理命令（可用脚本批量执行，但建议逐个确认）：**

```powershell
# 示例：处理 JNPF.csproj
$csproj = Get-Content "D:\JNPF-v52\backend\framework\JNPF\JNPF.csproj" -Raw

# 移除显式 TargetFramework（如果有）
$csproj = $csproj -replace '<TargetFramework>net6\.0</TargetFramework>', ''

# 移除 net5.0 条件块
$csproj = $csproj -replace '(?s)<PackageReference[^>]*Condition="[^"]*net5\.0[^"]*"[^>]*/>\s*', ''

# 简化 net6.0/net7.0 条件为无条件
$csproj = $csproj -replace ' Condition="[^"]*(?:net6\.0|net7\.0)[^"]*"', ''

Set-Content "D:\JNPF-v52\backend\framework\JNPF\JNPF.csproj" $csproj

# 对其余 6 个文件重复类似操作
# 每处理一个就 dotnet restore 验证一次
```

### 4B.3 处理 API.Entry 项目中的框架包引用

API.Entry 项目（`JNPF.API.Entry.csproj` 和 `JNPF.OA.API.Entry.csproj`）继承根 Directory.Build.props 的 net10.0，但可能有显式的 `Microsoft.AspNetCore.*` 包引用需要清理：

```powershell
# 检查 API.Entry 是否有需要清理的包引用
Select-String -Path "D:\JNPF-v52\backend\application\JNPF.API.Entry\JNPF.API.Entry.csproj" -Pattern "PackageReference"
# 如果有 Microsoft.AspNetCore.* 6.0.x 的显式引用，需要移除或升级到 10.0.x
```

---

## 第四步-C：升级 NuGet 包（1-2小时）

> **修正：补充高风险包处理策略；按依赖关系分批升级。**

### 4C.1 第一批：Microsoft.Extensions.* 和 Microsoft.AspNetCore.* 框架包

这些包在 .NET 10 中已内置，显式引用的版本需要升级或移除：

```powershell
# 检查所有 Microsoft.Extensions.* 和 Microsoft.AspNetCore.* 的显式引用
Write-Host "=== 框架包引用检查 ==="
Get-ChildItem -Recurse -Filter "*.csproj" | Where-Object { $_.Name -notlike "*Backup*" } | ForEach-Object {
    Select-String -Path $_.FullName -Pattern 'PackageReference Include="(Microsoft\.(?:Extensions|AspNetCore)\.[^"]+)" Version="([^"]+)"' -AllMatches
} | ForEach-Object {
    $_.Matches | ForEach-Object {
        Write-Host "$($_.Groups[1].Value) = $($_.Groups[2].Value) in $($_.Path | Split-Path -Leaf)"
    }
}
```

处理策略：
- `Microsoft.AspNetCore.Authentication.JwtBearer` → 升级到 `10.0.0` 或移除（.NET 10 内置）
- `Microsoft.AspNetCore.Mvc.NewtonsoftJson` → 升级到 `10.0.0`
- `Microsoft.AspNetCore.Razor.Language` → 升级到 `10.0.0`
- `Microsoft.AspNetCore.WebSockets` 2.3.0 → **移除**（已内置到框架）
- `Microsoft.Extensions.Caching.Abstractions/Memory` 6.0.0 → 升级到 `10.0.0`
- `Microsoft.Extensions.DependencyInjection.Abstractions` → 升级到 `10.0.0`
- `Microsoft.Extensions.DependencyModel` → 升级到 `10.0.0`
- `System.Configuration.ConfigurationManager` 6.0.1 → 升级到 `10.0.0`
- `System.Diagnostics.PerformanceCounter` 6.0.1 → 升级到 `10.0.0`
- `System.Management` 6.0.2 → 升级到 `10.0.0`
- `System.Text.Json` → **移除**（.NET 10 内置）

### 4C.2 第二批：核心业务依赖

```powershell
# 尝试用 dotnet outdated 批量升级（排除 Major 版本跳跃）
dotnet tool install -g dotnet-outdated-tool 2>$null
dotnet outdated "D:\JNPF-v52\backend\zx_lowcode_netcore.sln" --upgrade --version-lock Major 2>&1 | Tee-Object -Variable upgradeLog

# 如果 dotnet outdated 失败或不支持 .NET 10，手动升级：
# 核心包升级清单
$upgrades = @{
    "SqlSugarCore" = "5.1.4.160"           # 检查最新兼容版本
    "Serilog.AspNetCore" = "8.0.3"          # 检查最新兼容版本
    "Swashbuckle.AspNetCore" = "6.5.0"      # 通常向后兼容
    "Mapster" = "7.4.0"                     # 通常向后兼容
    "CSRedisCore" = "3.8.670"              # 通常向后兼容
    "MiniProfiler.AspNetCore.Mvc" = "4.5.0" # 检查最新兼容版本
    "RabbitMQ.Client" = "6.8.1"            # 保持 6.x，不跳 7.x
    "Dapper.Contrib" = "2.0.78"            # 通常向后兼容
    "MailKit" = "4.3.0"                    # 通常向后兼容
    "IGeekFan.AspNetCore.Knife4jUI" = "0.0.13" # 检查最新版本
}

# 注意：以上版本号需要根据第二步的实际查询结果调整
# 工程师应根据 dotnet package search 的输出填入正确的最新版本
```

### 4C.3 第三批：高风险包处理

> **原始方案完全遗漏。这些包可能阻塞升级。**

| 包 | 当前版本 | 处理策略 |
|---|---------|---------|
| `JavaScriptEngineSwitcher.ChakraCore` | 3.18.2 | **检查是否支持 net10.0。如果不支持，暂时注释掉引用它的代码，升级后找替代方案** |
| `JavaScriptEngineSwitcher.V8` | 3.18.1 | 同上 |
| `FreeSpire.Office` | 8.2.0 | 检查官网是否有 .NET 10 版本。如果没有，考虑用 NPOI 替代或暂时保留 |
| `DingDing.SDK.NetCore` | 2021.1.7.1 | 检查是否有新版本。如果没有，测试当前版本在 .NET 10 下是否能加载 |
| `Aspose.Cells` / `Aspose.Words` | 23.11.0 | Aspose 通常支持较新 .NET，但需验证 |
| `NPOI` | 2.5.5 | 升级到最新版本（2.7.x+ 应支持 .NET 10） |

**如果某个高风险包确实不兼容 .NET 10：**

```powershell
# 方案 A：注释掉引用代码，编译通过后单独处理
# 在引用文件中用 #if 预处理指令包裹
# #if NET6_0_OR_GREATER && !NET10_0_OR_GREATER
#     // 旧代码
# #endif

# 方案 B：在 csproj 中条件排除
# <PackageReference Include="ProblematicPackage" Version="x.x.x"
#     Condition="'$(TargetFramework)' != 'net10.0'" />
```

### 4C.4 升级后验证

```powershell
# 检查升级后的包版本
Write-Host "=== 包升级后状态 ==="
Get-ChildItem -Recurse -Filter "*.csproj" | Where-Object { $_.Name -notlike "*Backup*" } | ForEach-Object {
    Select-String -Path $_.FullName -Pattern 'PackageReference Include="([^"]+)" Version="([^"]+)"' -AllMatches
} | ForEach-Object {
    $_.Matches | ForEach-Object {
        [PSCustomObject]@{
            Package = $_.Groups[1].Value
            Version = $_.Groups[2].Value
        }
    }
} | Sort-Object Package, Version -Unique | Format-Table -AutoSize
```

---

## 第五步：首次编译 + 修复编译错误（3-6小时）

> **修正：补充具体的错误类型和修复模板，基于实际代码库分析。**

### 5.1 首次编译

```powershell
Write-Host "=== 首次编译 ==="
dotnet restore "D:\JNPF-v52\backend\zx_lowcode_netcore.sln" 2>&1 | Tee-Object -Variable restoreLog
dotnet build "D:\JNPF-v52\backend\zx_lowcode_netcore.sln" --configuration Release --no-restore 2>&1 | Tee-Object -Variable buildLog1

# 统计错误
$errors = $buildLog1 | Select-String "error " | Measure-Object
Write-Host "`n总错误数: $($errors.Count)"

# 按错误码分类
Write-Host "`n=== 按错误码分类 ==="
$buildLog1 | Select-String -Pattern 'error ([A-Z]{2}\d+)' | ForEach-Object { $_.Matches.Groups[1].Value } | Group-Object | Sort-Object Count -Descending | Select-Object -First 20 | Format-Table Name, Count

# 按文件分类
Write-Host "`n=== 按文件分类 ==="
$buildLog1 | Select-String -Pattern '([^\s]+\.cs)\(\d+,\d+\)' | ForEach-Object { $_.Matches.Groups[1].Value } | Group-Object | Sort-Object Count -Descending | Select-Object -First 20 | Format-Table Name, Count
```

### 5.2 预期错误类型及修复模板

基于代码库分析，编译错误的主要来源：

**错误类型 1：CS0234 命名空间中不存在 XXX（预计 10-20 处）**

```
原因：API 在 .NET 10 中被移动到不同命名空间
修复：在文件顶部添加新的 using 语句
常见场景：
  - Microsoft.AspNetCore.Http.Features 可能需要显式引用
  - System.Text.Json 的某些类型可能需要调整命名空间
```

**错误类型 2：CS0618 XXX 已过时（预计 5-15 处）**

```
原因：废弃 API 在 .NET 10 中可能被移除
修复：用新 API 替代
常见场景：
  - WebHost.CreateDefaultBuilder → WebApplication.CreateBuilder
  - IHostingEnvironment → IWebHostEnvironment
  - IApplicationBuilder 某些扩展方法签名变化
```

**错误类型 3：NU1202 包与 net10.0 不兼容（预计 5-10 处）**

```
原因：某个包不支持 .NET 10
修复：升级该包到兼容版本，或找替代包
如果无法解决：标记为阻塞项，用 #if 条件编译暂时绕过
```

**错误类型 4：CS1729 XXX 不包含接受 XX 个参数的构造函数（预计 5-10 处）**

```
原因：包升级后 API 签名变化
修复：按新 API 签名调整代码
常见场景：
  - SqlSugar 的某些方法签名可能变化
  - Serilog 的配置 API 可能变化
```

**错误类型 5：CS0104 歧义引用（预计 5-10 处）**

```
原因：.NET 10 引入了更多隐式 using
修复：在冲突文件中显式指定完整命名空间
示例：
  使用 JsonSerializer 时可能与 System.Text.Json 和 Newtonsoft.Json 冲突
  → 改为 System.Text.Json.JsonSerializer 或 Newtonsoft.Json.JsonSerializer
```

**错误类型 6：CS0246 找不到类型或命名空间（预计 5-10 处）**

```
原因：framework 层条件编译块清理后，某些包引用丢失
修复：检查是否遗漏了某个 PackageReference，补回
```

### 5.3 修复策略

```
原则：先修高频错误类型，一次修一类，每修一类就编译验证一次。

推荐顺序：
  1. 先修 NU1202（包不兼容）→ 升级包版本或添加条件排除
  2. 再修 CS0234（命名空间）→ 添加 using
  3. 再修 CS0104（歧义引用）→ 显式指定命名空间
  4. 再修 CS0618（过时 API）→ 按新 API 替代
  5. 最后修 CS1729（构造函数）→ 调整调用代码
```

```powershell
# 每修复一类错误后，重新编译验证
dotnet build "D:\JNPF-v52\backend\zx_lowcode_netcore.sln" --configuration Release 2>&1 | Tee-Object -Variable buildLogN
$errorCount = ($buildLogN | Select-String "error " | Measure-Object).Count
Write-Host "剩余错误数: $errorCount"
```

### 5.4 编译通过标准

```
目标：0 error
警告可以忽略（TreatWarningsAsErrors 已在 Directory.Build.props 中设为 false）
```

---

## 第六步-A：Native 二进制兼容性验证（30分钟）

> **原始方案完全遗漏。嵌入式原生 DLL 在 .NET 10 runtime 下可能加载失败。**

```powershell
# 检查嵌入式 native 二进制
Write-Host "=== 嵌入式 Native 二进制清单 ==="

# API.Entry 中的 yitidgengo
$apiEntry = "D:\JNPF-v52\backend\application\JNPF.API.Entry"
Get-ChildItem $apiEntry -Filter "yitidgengo*" | ForEach-Object {
    Write-Host "  $($_.Name) ($([math]::Round($_.Length/1KB, 1)) KB)"
}

# Common 中的 ip2region.db
$commonDir = "D:\JNPF-v52\backend\modularity\common\JNPF.Common"
Get-ChildItem $commonDir -Filter "ip2region*" -Recurse | ForEach-Object {
    Write-Host "  $($_.Name) ($([math]::Round($_.Length/1KB, 1)) KB)"
}

# CollectiveOAuth 中的 TopSdk.dll
$oauthDir = "D:\JNPF-v52\backend\infrastructure\JNPF.Extras.CollectiveOAuth"
Get-ChildItem $oauthDir -Filter "TopSdk*" -Recurse | ForEach-Object {
    Write-Host "  $($_.Name) ($([math]::Round($_.Length/1KB, 1)) KB)"
}
```

**验证方式：** 在第六步-B 冒烟测试中，如果服务能正常启动并处理请求，说明 native 二进制兼容。如果启动时报 `DllNotFoundException` 或 `BadImageFormatException`，需要：
1. 检查是否有更新版本的 native 库
2. 重新编译 native 库为 .NET 10 兼容
3. 找替代方案

---

## 第六步-B：冒烟测试（30分钟）

> **保持不变**

```powershell
# 确认编译通过
Write-Host "=== 最终编译结果 ==="
dotnet build "D:\JNPF-v52\backend\zx_lowcode_netcore.sln" --configuration Release 2>&1 | Select-Object -Last 10

# 启动服务
Write-Host "`n=== 尝试启动服务 ==="
Write-Host "请手动执行："
Write-Host "cd D:\JNPF-v52\backend\application\JNPF.API.Entry"
Write-Host "dotnet run --configuration Release"
Write-Host ""
Write-Host "观察是否有启动异常，特别注意："
Write-Host "  - DllNotFoundException（native 库不兼容）"
Write-Host "  - BadImageFormatException（32/64位不匹配）"
Write-Host "  - TypeLoadException（包版本不兼容）"
Write-Host "  - InvalidOperationException（DI 注册失败）"
```

**冒烟测试清单：**

```
[ ] 1. 服务启动无异常退出         结果：通过/失败  备注：___
[ ] 2. Swagger/Knife4j 页面可访问  结果：通过/失败  备注：___
[ ] 3. 登录页可打开               结果：通过/失败  备注：___
[ ] 4. 登录成功（获取到 token）    结果：通过/失败  备注：___
[ ] 5. 登录后首页可显示           结果：通过/失败  备注：___
[ ] 6. 随便打开一个列表页面有数据  结果：通过/失败  备注：___
[ ] 7. 随便打开一个表单可以编辑    结果：通过/失败  备注：___
```

---

## 第七步：性能对比（15分钟）

```powershell
# .NET 10 编译时间
Write-Host "=== .NET 10 编译时间 ==="
$times = @()
for ($i = 1; $i -le 3; $i++) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    dotnet build "D:\JNPF-v52\backend\zx_lowcode_netcore.sln" --configuration Release 2>$null | Out-Null
    $sw.Stop()
    $times += $sw.Elapsed.TotalSeconds
    Write-Host "  第${i}次: $([math]::Round($sw.Elapsed.TotalSeconds, 1))秒"
}
$avg = ($times | Measure-Object -Average).Average
Write-Host "  平均: $([math]::Round($avg, 1))秒"
```

---

## 第八步：提交代码 + 整理报告（30分钟）

```powershell
# 提交代码
git add -A
git status
git commit -m "chore: upgrade from .NET 6 to .NET 10"

# 变更统计
Write-Host "=== 代码变更统计 ==="
git diff --stat main
```

---

## 报告模板 4 — 升级执行结果

```
=== .NET 10 升级执行报告 ===

1. TargetFramework 变更
   修改方式：Directory.Build.props 统一修改 + framework 层单独处理
   项目数：___个（不含备份）
   清理冗余文件：___个

2. NuGet 包升级记录
   ┌─────────────────────────────────┬──────────┬──────────┬────────────────┐
   │ 包名                             │ 升级前    │ 升级后    │ 是否有 API 变化  │
   ├─────────────────────────────────┼──────────┼──────────┼────────────────┤
   │                                 │          │          │                │
   └─────────────────────────────────┴──────────┴──────────┴────────────────┘

   高风险包处理：
   ├── JavaScriptEngineSwitcher.ChakraCore：___
   ├── JavaScriptEngineSwitcher.V8：___
   ├── FreeSpire.Office：___
   ├── DingDing.SDK.NetCore：___
   └── ...

3. 编译过程
   第一次编译错误数：___
   按类型分类：
   ├── CS0234（命名空间）：___处
   ├── CS0618（过时 API）：___处
   ├── NU1202（包不兼容）：___处
   ├── CS1729（构造函数变化）：___处
   ├── CS0104（歧义引用）：___处
   ├── CS0246（找不到类型）：___处
   └── 其他：___处

   最终编译结果：通过 / 未通过（剩余___个错误）

   错误修复过程中的关键决策：
   ├── [包名]：从___版本升级到___版本，因为___
   ├── [API 变化]：XXX 改为 YYY，因为___
   ├── [条件编译]：___用 #if 包裹，因为___
   ├── [无法修复]：___，原因___
   └── ...

4. 冒烟测试结果
   [ ] 服务启动无异常退出     结果：通过/失败
   [ ] Swagger/Knife4j 可访问 结果：通过/失败
   [ ] 登录页可打开           结果：通过/失败
   [ ] 登录成功               结果：通过/失败
   [ ] 首页可显示             结果：通过/失败
   [ ] 列表页有数据           结果：通过/失败
   [ ] 表单可编辑             结果：通过/失败

5. 性能对比
   ┌────────────┬──────────┬──────────┬──────────┐
   │ 指标        │ .NET 6   │ .NET 10  │ 变化     │
   ├────────────┼──────────┼──────────┼──────────┤
   │ 编译时间    │ ___秒    │ ___秒    │ ___%     │
   │ 冷启动时间  │ ___秒    │ ___秒    │ ___%     │
   │ 内存占用    │ ___MB    │ ___MB    │ ___%     │
   └────────────┴──────────┴──────────┴──────────┘

6. Native 二进制验证
   [ ] yitidgengo.dll 加载正常    结果：通过/失败
   [ ] yitidgengo.so 加载正常     结果：通过/失败
   [ ] ip2region.db 读取正常      结果：通过/失败
   [ ] TopSdk.dll 加载正常        结果：通过/失败

7. 未解决的问题
   ├── [ ] 问题1：___
   ├── [ ] 问题2：___
   └── ...

8. 对专家 Review 的问题清单
   ├── Q1：___
   ├── Q2：___
   └── ...

9. 代码变更统计
   ├── 修改文件数：___
   ├── 新增文件数：___
   ├── 删除文件数：___
   └── git diff --stat 输出：[粘贴]
```

---

## 执行顺序检查表

```
[ ] 第一步-A：项目盘点（完成报告模板 1）
[ ] 第一步-B：清理 6 个冗余 csproj
[ ] 第二步：NuGet 兼容性检查（完成报告模板 2，含高风险包）
[ ] 第三步：基线性能记录（完成报告模板 3）
[ ] 第四步-A：创建分支 + 升级 global.json + 根 Directory.Build.props
[ ] 第四步-B：framework 层 multi-targeting 处理（7 个 csproj 条件编译清理）
[ ] 第四步-C：NuGet 包升级（分三批：框架包 → 业务包 → 高风险包）
[ ] 第五步：首次编译 + 修复编译错误（目标：0 error）
[ ] 第六步-A：Native 二进制兼容性验证
[ ] 第六步-B：冒烟测试
[ ] 第七步：性能对比
[ ] 第八步：提交代码 + 整理报告（完成报告模板 4）
```

---

## 时间预算（修正版）

```
第一步-A 项目盘点              15分钟
第一步-B 清理冗余文件          10分钟
第二步   NuGet 兼容性检查       1.5小时（含高风险包验证）
第三步   基线性能记录            30分钟
第四步-A 创建分支 + TFM 修改    10分钟
第四步-B framework 层处理       30分钟
第四步-C NuGet 包升级           1-2小时
第五步   编译错误修复            3-6小时（取决于错误数量）
第六步-A Native 二进制验证      30分钟
第六步-B 冒烟测试              30分钟
第七步   性能对比               15分钟
第八步   提交 + 报告            30分钟
─────────────────────────────────────
总计                           8-12小时（含缓冲）
```

**如果到某个步骤发现阻塞项（如 ChakraCore 不兼容），立即停止并记录，不要死磕。明早根据报告决定处理方案。**

---

## 附录：已安装的 SDK 和 Runtime

```
SDK:
  6.0.428 [C:\Program Files\dotnet\sdk]
  8.0.421 [C:\Program Files\dotnet\sdk]
  10.0.202 [C:\Program Files\dotnet\sdk]

Runtime:
  Microsoft.AspNetCore.App 6.0.36
  Microsoft.AspNetCore.App 8.0.17 / 8.0.26 / 8.0.27
  Microsoft.AspNetCore.App 10.0.6
  Microsoft.NETCore.App 6.0.36
  Microsoft.NETCore.App 8.0.17 / 8.0.26 / 8.0.27
  Microsoft.NETCore.App 10.0.6

当前 global.json: sdk.version = "6.0.0", rollForward = "latestFeature"
```

## 附录：项目结构概览

```
backend/
├── Directory.Build.props          ← 根级 TFM 定义（net6.0 → net10.0）
├── Directory.Build.targets
├── global.json                    ← SDK 版本锁定（6.0.0 → 10.0.202）
├── dotnet.ruleset
├── zx_lowcode_netcore.sln         ← 主解决方案（52+ 个项目）
├── framework/
│   ├── Directory.Build.props      ← framework 层 multi-targeting（需单独处理）
│   ├── JNPF.sln                   ← 框架子解决方案（仅 framework 层）
│   ├── JNPF/                      ← 核心框架
│   ├── JNPF.Extras.Authentication.JwtBearer/
│   ├── JNPF.Extras.DatabaseAccessor.Dapper/
│   ├── JNPF.Extras.DatabaseAccessor.SqlSugar/
│   ├── JNPF.Extras.DependencyModel.CodeAnalysis/
│   ├── JNPF.Extras.Logging.Serilog/
│   ├── JNPF.Extras.ObjectMapper.Mapster/
│   └── JNPF.Xunit/
├── infrastructure/
│   ├── JNPF.Extras.CollectiveOAuth/  ← 含 TopSdk.dll 本地引用
│   ├── JNPF.Extras.EventBus.RabbitMQ/
│   ├── JNPF.Extras.Thirdparty/      ← 含 ChakraCore/V8 高风险包
│   └── JNPF.Extras.WebSockets/
├── modularity/
│   ├── common/
│   ├── oauth/
│   ├── system/
│   ├── message/
│   ├── taskscheduler/
│   ├── workflow/
│   ├── visualdev/
│   ├── codegen/
│   ├── visualdata/
│   ├── extend/
│   ├── app/
│   ├── engine/
│   ├── inteAssistant/
│   ├── subdev/
│   └── zxdev/
└── application/
    ├── JNPF.API.Entry/            ← 主 API 入口
    └── JNPF.OA.API.Entry/         ← OA API 入口（未启用）
```
