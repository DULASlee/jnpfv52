# ═══════════════════════════════════════════════════════════════
# arch-module-dependency-scan.ps1 — 模块依赖矩阵 + Common.Core 共享度统计
#
# 用途（战役0补充数据采集，2026-08-19）：
#   A. modularity/ 16 模块 ProjectReference 全量矩阵，
#      标注「绕过 .Interfaces 直接引用实现工程」的违规候选
#   B. JNPF.Common.Core 公共类型清单 + 跨模块引用广度统计，
#      识别仅被 1-2 个模块使用的「伪共享」可下沉类型
#
# 用法：powershell -File scripts\arch-module-dependency-scan.ps1
#       -Gate            CI 门禁模式：仅 Part A，对比基线，新增违规 exit 1
#       -UpdateBaseline  重生成 scripts/arch-dependency-baseline.json（需 CR 审批）
# 输出：.claude/evidence/arch-scan/*.json + 控制台 Markdown 摘要
# 说明：类型引用统计基于源码文本 \bType\b 词边界匹配（启发式，
#       同名类型可能误计；用于治理优先级排序，不作编译级结论）。
# ═══════════════════════════════════════════════════════════════
# ═══════════════════════════════════════════════════════════
param(
    [switch]$Gate,
    [switch]$UpdateBaseline
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$backend = Join-Path $repo 'backend'
$modularity = Join-Path $backend 'modularity'
$commonCoreDir = Join-Path $modularity 'common\JNPF.Common.Core'
$evidenceDir = Join-Path $repo '.claude\evidence\arch-scan'
New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null

function Get-Domain([string]$path) {
    $rel = [IO.Path]::GetRelativePath($modularity, $path)
    return ($rel -split '[\\/]')[0]
}

# ────────────────────────────────────────────────
# Part A：模块间 ProjectReference 矩阵
# ────────────────────────────────────────────────
$csprojs = Get-ChildItem $modularity -Recurse -Filter *.csproj | Where-Object { $_.FullName -notmatch '[\\/](obj|bin)[\\/]' }
$edges = @()
foreach ($cs in $csprojs) {
    $srcDomain = Get-Domain $cs.FullName
    $srcProject = [IO.Path]::GetFileNameWithoutExtension($cs.Name)
    [xml]$xml = Get-Content $cs.FullName -Raw
    foreach ($ref in $xml.SelectNodes('//ProjectReference')) {
        $targetPath = [IO.Path]::GetFullPath([IO.Path]::Combine($cs.DirectoryName, $ref.Include))
        $targetName = [IO.Path]::GetFileNameWithoutExtension($targetPath)
        $kind = 'other'
        $tgtDomain = $null; $viaInterfaces = $null
        if ($targetPath -like "*$([IO.Path]::DirectorySeparatorChar)modularity*") {
            $kind = 'module'
            $tgtDomain = Get-Domain $targetPath
            $viaInterfaces = $targetName.EndsWith('.Interfaces')
        } elseif ($targetPath -like "*$([IO.Path]::DirectorySeparatorChar)framework*") { $kind = 'framework' }
        elseif ($targetPath -like "*$([IO.Path]::DirectorySeparatorChar)infrastructure*") { $kind = 'infrastructure' }
        elseif ($targetPath -like "*$([IO.Path]::DirectorySeparatorChar)tests*") { $kind = 'tests' }
        $edges += [PSCustomObject]@{
            SrcDomain = $srcDomain; SrcProject = $srcProject
            Kind = $kind; TgtDomain = $tgtDomain; Target = $targetName; ViaInterfaces = $viaInterfaces
        }
    }
}

# 跨模块引用（排除模块内部工程互引）
$cross = $edges | Where-Object { $_.Kind -eq 'module' -and $_.SrcDomain -ne $_.TgtDomain }
$violations = $cross | Where-Object { -not $_.ViaInterfaces }
Write-Host "## 跨模块引用汇总: 总数=$($cross.Count) 其中绕过.Interfaces=$($violations.Count)"
$violations | Sort-Object SrcDomain, Target | ForEach-Object {
    Write-Host ("  [违规候选] {0} -> {1} (实现工程)" -f $_.SrcProject, $_.Target)
}

# 矩阵（域级聚合）
$domains = Get-ChildItem $modularity -Directory | Select-Object -ExpandProperty Name
Write-Host ""
Write-Host "## 模块间依赖矩阵（行=引用方，列=被引用方，I=仅经.Interfaces，X=存在实现引用）"
$matrix = @{}
foreach ($e in $cross) {
    $key = "$($e.SrcDomain)|$($e.TgtDomain)"
    if (-not $matrix.ContainsKey($key)) { $matrix[$key] = 'I' }
    if (-not $e.ViaInterfaces) { $matrix[$key] = 'X' }
}
$header = "| 引用方 \ 被引用方 | " + ($domains -join ' | ') + " |"
Write-Host $header
foreach ($d in $domains) {
    $row = "| **$d** |"
    foreach ($t in $domains) {
        if ($d -eq $t) { $row += ' - |' } else { $row += " $($matrix["$d|$t"]) |" }
    }
    Write-Host $row
}

$edges | ConvertTo-Json -Depth 3 | Set-Content (Join-Path $evidenceDir 'module-dependency-edges.json') -Encoding UTF8

# ────────────────────────────────────────────────
# Part A.2：基线门禁（任务 0-6：新增违规阻断合并）
# ────────────────────────────────────────────────
$violationKeys = @($violations | ForEach-Object { "$($_.SrcProject) -> $($_.Target)" } | Sort-Object)
$baselinePath = Join-Path $PSScriptRoot 'arch-dependency-baseline.json'

if ($UpdateBaseline) {
    @{ frozenAt = (Get-Date -Format 'yyyy-MM-dd'); crossModuleRefCount = $cross.Count; bypassInterfacesCount = $violations.Count; violations = $violationKeys } |
        ConvertTo-Json -Depth 3 | Set-Content $baselinePath -Encoding UTF8
    Write-Host "[baseline] 已写入 $baselinePath（违规 $($violations.Count) 条）"
}

if ($Gate) {
    if (-not (Test-Path $baselinePath)) {
        Write-Error '[gate] 基线文件不存在，先运行 -UpdateBaseline'
        exit 1
    }
    $bl = Get-Content $baselinePath -Raw | ConvertFrom-Json
    $newViolations = @($violationKeys | Where-Object { $bl.violations -notcontains $_ })
    $fixedCount = @($bl.violations | Where-Object { $violationKeys -notcontains $_ }).Count
    Write-Host "[gate] 跨模块引用=$($cross.Count) 绕过.Interfaces=$($violations.Count)（基线 $($bl.bypassInterfacesCount)）新增违规=$($newViolations.Count) 已修复=$fixedCount"
    foreach ($v in $newViolations) { Write-Host "[gate][NEW] $v" }
    if ($newViolations.Count -gt 0) {
        Write-Error '[gate] FAIL — 新增跨模块违规引用。模块间通信必须走对方 .Interfaces 工程（见 docs/architecture/JNPF-Backend-Architecture-Explained.md §3）。如属计划内治理，请走 CR 流程更新 scripts/arch-dependency-baseline.json。'
        exit 1
    }
    Write-Host '[gate] PASS'
    exit 0
}

# ────────────────────────────────────────────────
# Part B：JNPF.Common.Core 类型共享度（Gate 模式跳过，避免拖慢 CI）
# ────────────────────────────────────────────────
if (-not $Gate -and -not $UpdateBaseline) {
$ccFiles = Get-ChildItem $commonCoreDir -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '[\\/](obj|bin)[\\/]' }
$typeNames = @()
foreach ($f in $ccFiles) {
    $content = Get-Content $f.FullName -Raw
    $ms = [regex]::Matches($content, 'public\s+(?:sealed\s+|abstract\s+|static\s+|partial\s+)*(?:class|interface|enum|struct|record)\s+(\w+)')
    foreach ($m in $ms) { $typeNames += $m.Groups[1].Value }
}
$typeNames = $typeNames | Sort-Object -Unique

# 引用方工程数
$refCount = ($csprojs | Where-Object { (Get-Content $_.FullName -Raw) -match 'JNPF\.Common\.Core\.csproj' }).Count
$allCsprojRefCount = (Get-ChildItem $backend -Recurse -Filter *.csproj |
    Where-Object { $_.FullName -notmatch '[\\/](obj|bin)[\\/]' -and (Get-Content $_.FullName -Raw) -match 'JNPF\.Common\.Core\.csproj' }).Count

# 各域语料库（modularity 除 common 外 + infrastructure + application）
$corpus = @{}
foreach ($d in $domains) {
    if ($d -eq 'common') { continue }
    $dir = Join-Path $modularity $d
    $files = Get-ChildItem $dir -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '[\\/](obj|bin)[\\/]' }
    $corpus[$d] = ($files | ForEach-Object { Get-Content $_.FullName -Raw }) -join "`n"
}
foreach ($extra in @('infrastructure', 'application')) {
    $dir = Join-Path $backend $extra
    $files = Get-ChildItem $dir -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '[\\/](obj|bin)[\\/]' }
    $corpus[$extra] = ($files | ForEach-Object { Get-Content $_.FullName -Raw }) -join "`n"
}

$typeStats = @()
foreach ($t in $typeNames) {
    $usedIn = @()
    foreach ($k in $corpus.Keys) {
        if ($corpus[$k] -match "\b$t\b") { $usedIn += $k }
    }
    $typeStats += [PSCustomObject]@{ Type = $t; DomainCount = $usedIn.Count; Domains = ($usedIn -join ',') }
}

Write-Host ""
Write-Host "## JNPF.Common.Core 统计"
Write-Host "公共类型总数: $($typeNames.Count) | 引用 JNPF.Common.Core 的 csproj: modularity内=$refCount 全后端=$allCsprojRefCount"
Write-Host ""
Write-Host "### 广度 Top20（被最多域使用的类型 = 真共享内核）"
$typeStats | Sort-Object DomainCount -Descending | Select-Object -First 20 |
    ForEach-Object { Write-Host ("  {0} -> {1} 域 ({2})" -f $_.Type, $_.DomainCount, $_.Domains) }

$sink = $typeStats | Where-Object { $_.DomainCount -ge 1 -and $_.DomainCount -le 2 }
$unused = $typeStats | Where-Object { $_.DomainCount -eq 0 }
Write-Host ""
Write-Host "### 可下沉候选（仅 1-2 域使用）: $($sink.Count) 个 | 疑似无引用: $($unused.Count) 个"
$sink | Group-Object Domains | Sort-Object Count -Descending | ForEach-Object {
    Write-Host ("  使用域[{0}]: {1} 个类型" -f $_.Name, $_.Count)
}

$typeStats | ConvertTo-Json -Depth 3 | Set-Content (Join-Path $evidenceDir 'common-core-type-usage.json') -Encoding UTF8
Write-Host ""
Write-Host "[done] 证据已写入 .claude/evidence/arch-scan/"
}
