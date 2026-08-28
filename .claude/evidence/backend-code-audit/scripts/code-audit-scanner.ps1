<#
.SYNOPSIS
    JNPF 后端类级代码审计扫描器

.DESCRIPTION
    按照《JNPF 后端类级代码审计扫描清单 v1.1》对后端全量业务代码进行系统性扫描

.PARAMETER Module
    指定要扫描的模块名称

.PARAMETER Batch
    指定要扫描的批次编号

.PARAMETER All
    扫描所有模块

.EXAMPLE
    .\code-audit-scanner.ps1 -Module system
    .\code-audit-scanner.ps1 -Batch 1
    .\code-audit-scanner.ps1 -All
#>

param(
    [string]$Module,
    [int]$Batch,
    [switch]$All
)

$ErrorActionPreference = "Stop"

# 配置路径
$ConfigPath = Join-Path $PSScriptRoot "..\scan-config.json"
$OutputRoot = Join-Path $PSScriptRoot ".."

# 加载配置
$config = Get-Content $ConfigPath -Raw | ConvertFrom-Json

# 检测规则定义
$DetectionRules = @{
    A = @{
        Name = "资源生命周期与内存泄漏"
        Priority = "P0"
        Rules = @(
            @{ Id = "A1"; Pattern = '\+=\s*(On|Handle|Handler)'; Severity = "P0"; Description = "事件订阅未退订"; Fix = "实现 IDisposable/IAsyncDisposable，退订事件" }
            @{ Id = "A2"; Pattern = 'static\s+(Concurrent|Dictionary|List|HashSet|Collection)'; Severity = "P1"; Description = "静态集合无限增长"; Fix = "添加淘汰机制或容量上限" }
            @{ Id = "A3"; Pattern = 'AddSingleton.*(DbContext|SqlSugarClient|ISqlSugarClient)'; Severity = "P0"; Description = "DbContext/SqlSugarClient 生命周期错误"; Fix = "改为 Scoped 注册" }
            @{ Id = "A4"; Pattern = 'new\s+(Stream|SqlConnection|HttpClient|Timer|SemaphoreSlim|CancellationTokenSource)'; Severity = "P1"; Description = "IDisposable 缺失"; Fix = "使用 using 语句" }
            @{ Id = "A5"; Pattern = 'new\s+(Timer|Thread)\s*\('; Severity = "P1"; Description = "Timer/后台线程未释放"; Fix = "添加取消令牌，应用关闭时终止" }
        )
    }
    B = @{
        Name = "CLR 内存模型与 GC 压力"
        Priority = "P1"
        Rules = @(
            @{ Id = "B1"; Pattern = '(ArrayList|Hashtable)\s*<'; Severity = "P2"; Description = "装箱/拆箱"; Fix = "使用泛型集合" }
            @{ Id = "B2"; Pattern = '(for|foreach|while)\s*\(.*\+\='; Severity = "P2"; Description = "循环中字符串拼接"; Fix = "使用 StringBuilder" }
            @{ Id = "B3"; Pattern = 'new\s+(byte|char)\[\d{5,}\]'; Severity = "P1"; Description = "大对象分配"; Fix = "使用 ArrayPool 或流式处理" }
            @{ Id = "B4"; Pattern = '\.ToList\(\)\.(Where|Select|Count|Any)'; Severity = "P2"; Description = "低效 LINQ"; Fix = "直接使用链式查询" }
        )
    }
    C = @{
        Name = "异步编程模型"
        Priority = "P1"
        Rules = @(
            @{ Id = "C1"; Pattern = '\.(Result|Wait\(\)|GetAwaiter\(\)\.GetResult\(\))'; Severity = "P0"; Description = "Sync-over-Async 死锁"; Fix = "使用 async/await" }
            @{ Id = "C2"; Pattern = 'async\s+void\s+'; Severity = "P0"; Description = "async void 滥用"; Fix = "改为 async Task" }
            @{ Id = "C3"; Pattern = 'await\s+[^;]+'; Severity = "P2"; Description = "缺少 ConfigureAwait(false)"; Fix = "类库代码添加 .ConfigureAwait(false)" }
            @{ Id = "C4"; Pattern = 'Task\.Run\s*\(\s*\(\)\s*=>'; Severity = "P2"; Description = "Task.Run 滥用"; Fix = "使用异步 API" }
            @{ Id = "C5"; Pattern = 'await.*\.Dispose\(\)'; Severity = "P1"; Description = "异步方法中调用同步 Dispose"; Fix = "使用 await DisposeAsync()" }
        )
    }
    D = @{
        Name = "线程安全与并发"
        Priority = "P1"
        Rules = @(
            @{ Id = "D1"; Pattern = 'static\s+(Dictionary|List|HashSet)\s*<'; Severity = "P1"; Description = "非线程安全集合并发访问"; Fix = "使用 Concurrent 集合或添加 lock" }
            @{ Id = "D2"; Pattern = 'lock\s*\(\s*(this|typeof|"|new)'; Severity = "P0"; Description = "危险锁对象"; Fix = "使用私有静态对象作为锁" }
            @{ Id = "D3"; Pattern = 'static\s+(int|long|bool)\s+\w+.*\+\+'; Severity = "P1"; Description = "静态变量非原子操作"; Fix = "使用 Interlocked" }
            @{ Id = "D4"; Pattern = 'if\s*\(\w+\s*==\s*null\)\s*\w+\s*=\s*new'; Severity = "P2"; Description = "非线程安全懒加载"; Fix = "使用 Lazy<T>" }
            @{ Id = "D5"; Pattern = 'async.*lock\s*\('; Severity = "P1"; Description = "异步方法中使用 lock"; Fix = "使用 SemaphoreSlim" }
        )
    }
    E = @{
        Name = "异常处理体系"
        Priority = "P1"
        Rules = @(
            @{ Id = "E1"; Pattern = 'catch\s*(\(\w*\))?\s*\{\s*\}'; Severity = "P0"; Description = "空 catch 块"; Fix = "至少记录日志" }
            @{ Id = "E2"; Pattern = 'catch.*\{[^}]*\breturn\b'; Severity = "P1"; Description = "catch 后未记录"; Fix = "添加日志记录" }
            @{ Id = "E3"; Pattern = 'try\s*\{[^}]*(Convert\.(ToInt32|ToDecimal)|Parse|\.First\(\))'; Severity = "P2"; Description = "异常控制流程"; Fix = "使用 TryParse/FirstOrDefault" }
            @{ Id = "E4"; Pattern = '(return|throw).*\bex\.(Message|StackTrace)'; Severity = "P0"; Description = "异常信息泄露"; Fix = "记录日志，返回通用错误" }
            @{ Id = "E5"; Pattern = 'throw\s+new\s+Exception\s*\('; Severity = "P1"; Description = "异常层次混乱"; Fix = "使用 Oops.Oh/Oops.Bah" }
            @{ Id = "E6"; Pattern = 'throw\s+new\s+(Exception|ApplicationException)'; Severity = "P1"; Description = "Oops 异常体系合规"; Fix = "使用 JNPF 异常体系" }
        )
    }
    F = @{
        Name = "高绩效热路径"
        Priority = "P2"
        Rules = @(
            @{ Id = "F1"; Pattern = '\.ToList\(\)\.(Count|FirstOrDefault|Any|Where)'; Severity = "P2"; Description = "冗余 ToList()"; Fix = "直接使用链式查询" }
            @{ Id = "F2"; Pattern = '\.Skip\(\s*\d{4,}\s*\)'; Severity = "P1"; Description = "深分页未优化"; Fix = "使用 Keyset Pagination" }
            @{ Id = "F3"; Pattern = 'foreach.*\.(Query|Queryable|Insertable|Updateable|Deleteable)'; Severity = "P1"; Description = "N+1 查询"; Fix = "使用批量操作" }
            @{ Id = "F4"; Pattern = '(GetProperty|GetMethod|Activator\.CreateInstance|\.Invoke\()'; Severity = "P1"; Description = "热路径反射"; Fix = "缓存反射结果或使用表达式树" }
            @{ Id = "F5"; Pattern = 'DateTime\.(Now|UtcNow|Today).*DateTime\.(Now|UtcNow|Today)'; Severity = "P2"; Description = "重复计算"; Fix = "缓存到局部变量" }
        )
    }
    G = @{
        Name = "C# 现代特性缺失"
        Priority = "P3"
        Rules = @(
            @{ Id = "G1"; Pattern = '<Nullable>(disable|none)</Nullable>'; Severity = "P3"; Description = "NRT 未启用"; Fix = "启用可空引用类型" }
            @{ Id = "G2"; Pattern = '(==\s*\d{2,}|>\s*\d{2,}|==\s*"[^"]{5,}")'; Severity = "P3"; Description = "魔法数字/字符串"; Fix = "提取为常量或配置" }
            @{ Id = "G3"; Pattern = 'if\s*\(\w+\s+is\s+\w+\)'; Severity = "P3"; Description = "旧式模式匹配"; Fix = "使用 switch 表达式" }
            @{ Id = "G4"; Pattern = 'public\s+string\s+(Status|State|Type|Kind|Category)\s*\{'; Severity = "P3"; Description = "字符串替代枚举"; Fix = "使用枚举类型" }
        )
    }
    H = @{
        Name = "开闭原则与扩展性"
        Priority = "P2"
        Rules = @(
            @{ Id = "H1"; Pattern = 'switch\s*\([^)]+\)\s*\{[^}]*case\s+'; Severity = "P2"; Description = "分支过多"; Fix = "使用策略模式" }
            @{ Id = "H2"; Pattern = '相似方法体'; Severity = "P2"; Description = "跨类重复代码"; Fix = "提取基类或接口" }
            @{ Id = "H3"; Pattern = 'private\s+readonly\s+(?!I\w+)\w+\s+_'; Severity = "P2"; Description = "直接依赖具体类"; Fix = "依赖接口" }
            @{ Id = "H4"; Pattern = '同一条件判断'; Severity = "P2"; Description = "条件逻辑散落"; Fix = "集中到领域服务" }
        )
    }
    I = @{
        Name = "整洁架构与依赖方向"
        Priority = "P0"
        Rules = @(
            @{ Id = "I1"; Pattern = 'using\s+JNPF\.\w+\.(Internal|Services|Impl)'; Severity = "P0"; Description = "模块边界违规"; Fix = "引用 .Interfaces" }
            @{ Id = "I2"; Pattern = '(_db|_context|_sqlSugar)\.(Query|Queryable)'; Severity = "P0"; Description = "Service 直接操作数据库"; Fix = "通过仓储接口" }
            @{ Id = "I3"; Pattern = 'using\s+JNPF\.\w+\.(Internal|Services|Impl)'; Severity = "P0"; Description = "跨模块直接引用实现"; Fix = "依赖接口" }
            @{ Id = "I4"; Pattern = 'IDynamicApiController.*\{[\s\S]{500,}'; Severity = "P1"; Description = "Controller 包含业务逻辑"; Fix = "移到 Application Service" }
            @{ Id = "I5"; Pattern = '模块内层违规'; Severity = "P1"; Description = ".Entitys 引用其他层"; Fix = "保持独立" }
        )
    }
    J = @{
        Name = "安全与健壮性"
        Priority = "P0"
        Rules = @(
            @{ Id = "J1"; Pattern = '(SELECT|INSERT|UPDATE|DELETE).*\+\s*'; Severity = "P0"; Description = "SQL 注入风险"; Fix = "参数化查询" }
            @{ Id = "J2"; Pattern = '(password|secret|apiKey|connectionString)\s*=\s*"[^"]+"'; Severity = "P0"; Description = "敏感信息硬编码"; Fix = "使用配置或密钥管理" }
            @{ Id = "J3"; Pattern = 'public\s+\w+\s+\w+\([^)]*\)\s*\{[^}]*if\s*\(\w+\s*==\s*null'; Severity = "P1"; Description = "未验证的用户输入"; Fix = "使用 Guard" }
            @{ Id = "J4"; Pattern = '(Path\.Combine|File\.(Read|Write|Delete)).*\+'; Severity = "P0"; Description = "路径遍历"; Fix = "验证路径" }
            @{ Id = "J5"; Pattern = '(DeserializeObject\s*<\s*object|BinaryFormatter)'; Severity = "P0"; Description = "不安全的反序列化"; Fix = "使用安全的序列化方式" }
            @{ Id = "J6"; Pattern = 'Queryable<\w+>\([^)]*\)\.Where\((?!.*TenantId)'; Severity = "P0"; Description = "多租户过滤缺失"; Fix = "添加 ITenantFilter" }
        )
    }
    K = @{
        Name = "可观测性与诊断"
        Priority = "P2"
        Rules = @(
            @{ Id = "K1"; Pattern = '_(db|sqlSugar)\.(Insert|Update|Delete)\([^)]*\)'; Severity = "P2"; Description = "缺少结构化日志"; Fix = "添加日志" }
            @{ Id = "K2"; Pattern = '缺少追踪'; Severity = "P2"; Description = "缺少追踪"; Fix = "集成 OpenTelemetry" }
            @{ Id = "K3"; Pattern = '缺少指标'; Severity = "P2"; Description = "缺少指标"; Fix = "添加 Prometheus 指标" }
            @{ Id = "K4"; Pattern = 'Log(Information|Warning|Error)\("[^"]*"'; Severity = "P2"; Description = "日志中缺少上下文"; Fix = "添加租户 ID/用户 ID" }
        )
    }
    L = @{
        Name = "设计模式与代码结构"
        Priority = "P2"
        Rules = @(
            @{ Id = "L1"; Pattern = 'class\s+\w+.*\{[\s\S]*(public|private|protected)\s+\w+\s+\w+\s*\('; Severity = "P2"; Description = "上帝类"; Fix = "拆分职责" }
            @{ Id = "L2"; Pattern = 'public\s+\w+\s*\([^)]*,[^)]*,[^)]*,[^)]*,[^)]*,'; Severity = "P2"; Description = "构造函数过多参数"; Fix = "使用 Facade" }
            @{ Id = "L3"; Pattern = 'class\s+\w+Entity.*\{[\s\S]*public\s+\w+\s+\w+\s*\{\s*get;\s*set;\s*\}'; Severity = "P2"; Description = "贫血模型"; Fix = "添加行为方法" }
            @{ Id = "L4"; Pattern = '幽灵类'; Severity = "P2"; Description = "幽灵类/死代码"; Fix = "删除或标记废弃" }
            @{ Id = "L5"; Pattern = 'static\s+class\s+\w+.*\{[\s\S]*static\s+(?!void|async)'; Severity = "P2"; Description = "静态类滥用"; Fix = "改为实例类" }
        )
    }
    M = @{
        Name = "日志与审计"
        Priority = "P2"
        Rules = @(
            @{ Id = "M1"; Pattern = 'Log(Information|Warning|Error)\("[^"]*(password|token|secret)'; Severity = "P0"; Description = "敏感数据入日志"; Fix = "脱敏处理" }
            @{ Id = "M2"; Pattern = 'LogInformation\("[^"]*debug'; Severity = "P2"; Description = "日志级别不当"; Fix = "调整日志级别" }
            @{ Id = "M3"; Pattern = '(Delete|Remove|Update.*Permission|Update.*Config)'; Severity = "P2"; Description = "审计缺失"; Fix = "添加审计日志" }
            @{ Id = "M4"; Pattern = 'Log(Information|Warning|Error)\("[^"]*"'; Severity = "P2"; Description = "多租户审计上下文"; Fix = "添加 TenantId" }
        )
    }
    N = @{
        Name = "JNPF 架构合规（铁律）"
        Priority = "P0"
        Rules = @(
            @{ Id = "N1"; Pattern = 'Queryable<\w+>\([^)]*\)\.Where\((?!.*TenantId)'; Severity = "P0"; Description = "多租户过滤缺失（R4）"; Fix = "添加 ITenantFilter" }
            @{ Id = "N2"; Pattern = '(\$"|string\.Format).*SELECT.*\{'; Severity = "P0"; Description = "SQL 注入风险（R7）"; Fix = "参数化查询" }
            @{ Id = "N3"; Pattern = 'public\s+.*\s+\w+\([^)]*\)\s*(\{|=>)(?!.*\[AllowAnonymous\]|.*\[SecurityDefine\])'; Severity = "P0"; Description = "API 权限声明缺失（R8）"; Fix = "添加权限声明" }
            @{ Id = "N4"; Pattern = 'StudioWorkspace.*\\[^\\]+\\[^\\]+\\'; Severity = "P0"; Description = "三元组完整性（R12）"; Fix = "确保三元组完整" }
            @{ Id = "N5"; Pattern = 'class\s+\w+.*:\s*IDynamicApiController(?!.*\[ApiDescriptionSettings\])'; Severity = "P1"; Description = "IDynamicApiController 路由合规"; Fix = "添加 ApiDescriptionSettings" }
            @{ Id = "N6"; Pattern = 'throw\s+new\s+(Exception|ApplicationException)'; Severity = "P1"; Description = "Oops 异常体系合规"; Fix = "使用 Oops.Oh/Oops.Bah" }
            @{ Id = "N7"; Pattern = 'using\s+JNPF\.\w+\.(Internal|Services|Impl)'; Severity = "P0"; Description = "模块边界违规"; Fix = "引用 .Interfaces" }
        )
    }
    O = @{
        Name = "SqlSugar 安全与性能"
        Priority = "P1"
        Rules = @(
            @{ Id = "O1"; Pattern = 'class\s+\w+.*\{[\s\S]*(?!.*TenantId).*public\s+\w+\s+Id'; Severity = "P0"; Description = "ITenantFilter 漏配"; Fix = "添加 [Tenant] 特性" }
            @{ Id = "O2"; Pattern = '\.ToSql\(\)'; Severity = "P1"; Description = "参数化查询缺失"; Fix = "验证 SQL 参数化" }
            @{ Id = "O3"; Pattern = 'foreach.*\.(Query|InSingle|GetById\()'; Severity = "P1"; Description = "SqlSugar N+1 查询"; Fix = "批量加载" }
            @{ Id = "O4"; Pattern = '\[SugarTable\("[^"]*"\)\]'; Severity = "P2"; Description = "SugarTable 特性误用"; Fix = "遵循命名规范" }
            @{ Id = "O5"; Pattern = 'BeginTransaction(?!.*Commit|.*Rollback)'; Severity = "P0"; Description = "事务边界不清"; Fix = "确保事务完整" }
        )
    }
}

function Scan-File {
    param(
        [string]$FilePath,
        [string]$ModuleName
    )
    
    $findings = @()
    $content = Get-Content $FilePath -Raw -ErrorAction SilentlyContinue
    
    if (-not $content) {
        return $findings
    }
    
    $lines = Get-Content $FilePath -ErrorAction SilentlyContinue
    
    foreach ($dimension in $DetectionRules.Keys) {
        $rules = $DetectionRules[$dimension].Rules
        
        foreach ($rule in $rules) {
            try {
                $matches = [regex]::Matches($content, $rule.Pattern, 'Multiline')
                
                foreach ($match in $matches) {
                    $lineNumber = ($content.Substring(0, $match.Index) -split "`n").Count
                    $lineContent = if ($lineNumber -le $lines.Count) { $lines[$lineNumber - 1].Trim() } else { "" }
                    
                    $findings += [PSCustomObject]@{
                        Id = "$($rule.Id)-$(Get-Date -Format 'yyyyMMddHHmmss')-$(Get-Random)"
                        Dimension = $dimension
                        RuleId = $rule.Id
                        Severity = $rule.Severity
                        Module = $ModuleName
                        File = $FilePath
                        Line = $lineNumber
                        Code = $lineContent.Substring(0, [Math]::Min(100, $lineContent.Length))
                        Description = $rule.Description
                        Fix = $rule.Fix
                        Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
                    }
                }
            } catch {
                # 忽略正则表达式错误
            }
        }
    }
    
    return $findings
}

function Scan-Module {
    param(
        [string]$ModuleName
    )
    
    $module = $config.modules | Where-Object { $_.name -eq $ModuleName }
    
    if (-not $module) {
        Write-Host "Module $ModuleName not found in config" -ForegroundColor Red
        return @()
    }
    
    Write-Host "Scanning module: $ModuleName" -ForegroundColor Cyan
    
    $modulePath = Join-Path $config.modularityRoot $module.path
    $files = Get-ChildItem -Path $modulePath -Recurse -Filter "*.cs" | 
        Where-Object { $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\.Designer\.cs$" }
    
    Write-Host "  Found $($files.Count) files" -ForegroundColor Gray
    
    $allFindings = @()
    
    foreach ($file in $files) {
        $findings = Scan-File -FilePath $file.FullName -ModuleName $ModuleName
        $allFindings += $findings
        
        if ($findings.Count -gt 0) {
            Write-Host "    $($file.Name): $($findings.Count) issues" -ForegroundColor Yellow
        }
    }
    
    Write-Host "  Total issues: $($allFindings.Count)" -ForegroundColor $(if ($allFindings.Count -gt 0) { "Yellow" } else { "Green" })
    
    return $allFindings
}

function Save-ModuleFindings {
    param(
        [string]$ModuleName,
        [array]$Findings
    )
    
    $moduleDir = Join-Path $OutputRoot "modules\$ModuleName"
    New-Item -ItemType Directory -Force -Path $moduleDir | Out-Null
    
    $outputFile = Join-Path $moduleDir "findings.json"
    $Findings | ConvertTo-Json -Depth 10 | Set-Content $outputFile
    
    Write-Host "  Saved findings to: $outputFile" -ForegroundColor Gray
}

# 主执行逻辑
Write-Host "=== JNPF 后端类级代码审计扫描器 ===" -ForegroundColor Green
Write-Host "Scan Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

$allFindings = @()

if ($All) {
    Write-Host "Scanning all modules..." -ForegroundColor Cyan
    foreach ($module in $config.modules) {
        $findings = Scan-Module -ModuleName $module.name
        $allFindings += $findings
        Save-ModuleFindings -ModuleName $module.name -Findings $findings
    }
} elseif ($Batch) {
    $batch = $config.batches | Where-Object { $_.id -eq $Batch }
    if ($batch) {
        Write-Host "Scanning batch $Batch: $($batch.modules -join ', ')" -ForegroundColor Cyan
        foreach ($moduleName in $batch.modules) {
            $findings = Scan-Module -ModuleName $moduleName
            $allFindings += $findings
            Save-ModuleFindings -ModuleName $moduleName -Findings $findings
        }
    } else {
        Write-Host "Batch $Batch not found" -ForegroundColor Red
    }
} elseif ($Module) {
    $findings = Scan-Module -ModuleName $Module
    $allFindings += $findings
    Save-ModuleFindings -ModuleName $Module -Findings $findings
} else {
    Write-Host "Please specify -Module, -Batch, or -All" -ForegroundColor Yellow
    exit 1
}

# 生成摘要
Write-Host ""
Write-Host "=== Scan Summary ===" -ForegroundColor Green

$summary = @{
    scanDate = Get-Date -Format "yyyy-MM-dd"
    totalFiles = ($allFindings | Select-Object -ExpandProperty File -Unique).Count
    totalIssues = $allFindings.Count
    byDimension = @{}
    bySeverity = @{
        P0 = ($allFindings | Where-Object { $_.Severity -eq "P0" }).Count
        P1 = ($allFindings | Where-Object { $_.Severity -eq "P1" }).Count
        P2 = ($allFindings | Where-Object { $_.Severity -eq "P2" }).Count
        P3 = ($allFindings | Where-Object { $_.Severity -eq "P3" }).Count
    }
    byModule = @{}
    topIssues = @()
}

foreach ($dim in $DetectionRules.Keys) {
    $summary.byDimension[$dim] = ($allFindings | Where-Object { $_.Dimension -eq $dim }).Count
}

foreach ($finding in $allFindings) {
    if (-not $summary.byModule.ContainsKey($finding.Module)) {
        $summary.byModule[$finding.Module] = 0
    }
    $summary.byModule[$finding.Module]++
}

$summary.topIssues = $allFindings | 
    Where-Object { $_.Severity -eq "P0" -or $_.Severity -eq "P1" } |
    Sort-Object @{Expression={if($_.Severity -eq "P0") {0} else {1}}}, @{Expression={$_.Dimension}} |
    Select-Object -First 20

Write-Host "Total files scanned: $($summary.totalFiles)" -ForegroundColor White
Write-Host "Total issues found: $($summary.totalIssues)" -ForegroundColor White
Write-Host ""
Write-Host "Issues by severity:" -ForegroundColor Cyan
Write-Host "  P0 (致命): $($summary.bySeverity.P0)" -ForegroundColor $(if ($summary.bySeverity.P0 -gt 0) { "Red" } else { "Green" })
Write-Host "  P1 (严重): $($summary.bySeverity.P1)" -ForegroundColor $(if ($summary.bySeverity.P1 -gt 0) { "Yellow" } else { "Green" })
Write-Host "  P2 (警告): $($summary.bySeverity.P2)" -ForegroundColor Gray
Write-Host "  P3 (建议): $($summary.bySeverity.P3)" -ForegroundColor Gray
Write-Host ""
Write-Host "Issues by dimension:" -ForegroundColor Cyan
foreach ($dim in ($summary.byDimension.Keys | Sort-Object)) {
    $count = $summary.byDimension[$dim]
    if ($count -gt 0) {
        Write-Host "  $dim ($($DetectionRules[$dim].Name)): $count" -ForegroundColor Yellow
    }
}

# 保存摘要
$summaryPath = Join-Path $OutputRoot "scan-summary.json"
$summary | ConvertTo-Json -Depth 10 | Set-Content $summaryPath
Write-Host ""
Write-Host "Summary saved to: $summaryPath" -ForegroundColor Gray

# 保存所有发现
$allFindingsPath = Join-Path $OutputRoot "all-findings.json"
$allFindings | ConvertTo-Json -Depth 10 | Set-Content $allFindingsPath
Write-Host "All findings saved to: $allFindingsPath" -ForegroundColor Gray

Write-Host ""
Write-Host "=== Scan Complete ===" -ForegroundColor Green