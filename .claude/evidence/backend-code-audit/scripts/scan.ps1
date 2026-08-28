<#
.SYNOPSIS
    JNPF Backend Code Audit Scanner
#>

param(
    [string]$Module,
    [int]$Batch,
    [switch]$All
)

$ErrorActionPreference = "Stop"
$OutputRoot = Join-Path $PSScriptRoot ".."

# Detection rules
$Rules = @(
    # Dimension A: Resource Lifecycle
    @{ Dim = "A"; Id = "A1"; Pattern = '\+=\s*(On|Handle|Handler)'; Sev = "P0"; Desc = "Event subscription not unsubscribed"; Fix = "Implement IDisposable" }
    @{ Dim = "A"; Id = "A2"; Pattern = 'static\s+(Concurrent|Dictionary|List|HashSet)'; Sev = "P1"; Desc = "Static collection unlimited growth"; Fix = "Add eviction mechanism" }
    @{ Dim = "A"; Id = "A3"; Pattern = 'AddSingleton.*(DbContext|SqlSugarClient)'; Sev = "P0"; Desc = "DbContext lifecycle error"; Fix = "Change to Scoped" }
    @{ Dim = "A"; Id = "A4"; Pattern = 'new\s+(Stream|SqlConnection|HttpClient|Timer)'; Sev = "P1"; Desc = "IDisposable missing"; Fix = "Use using statement" }
    @{ Dim = "A"; Id = "A5"; Pattern = 'new\s+(Timer|Thread)\s*\('; Sev = "P1"; Desc = "Timer/Thread not released"; Fix = "Add cancellation token" }
    
    # Dimension B: CLR Memory
    @{ Dim = "B"; Id = "B1"; Pattern = '(ArrayList|Hashtable)'; Sev = "P2"; Desc = "Boxing/Unboxing"; Fix = "Use generic collections" }
    @{ Dim = "B"; Id = "B2"; Pattern = '(for|foreach|while).*\+\='; Sev = "P2"; Desc = "String concatenation in loop"; Fix = "Use StringBuilder" }
    @{ Dim = "B"; Id = "B3"; Pattern = 'new\s+(byte|char)\[\d{5,}\]'; Sev = "P1"; Desc = "Large object allocation"; Fix = "Use ArrayPool" }
    @{ Dim = "B"; Id = "B4"; Pattern = '\.ToList\(\)\.(Where|Select|Count|Any)'; Sev = "P2"; Desc = "Inefficient LINQ"; Fix = "Use chain query" }
    
    # Dimension C: Async
    @{ Dim = "C"; Id = "C1"; Pattern = '\.(Result|Wait\(\)|GetAwaiter)'; Sev = "P0"; Desc = "Sync-over-Async deadlock"; Fix = "Use async/await" }
    @{ Dim = "C"; Id = "C2"; Pattern = 'async\s+void\s+'; Sev = "P0"; Desc = "async void abuse"; Fix = "Change to async Task" }
    @{ Dim = "C"; Id = "C4"; Pattern = 'Task\.Run\s*\('; Sev = "P2"; Desc = "Task.Run abuse"; Fix = "Use async API" }
    @{ Dim = "C"; Id = "C5"; Pattern = 'await.*\.Dispose\(\)'; Sev = "P1"; Desc = "Sync Dispose in async"; Fix = "Use DisposeAsync" }
    
    # Dimension D: Thread Safety
    @{ Dim = "D"; Id = "D1"; Pattern = 'static\s+(Dictionary|List|HashSet)\s*<'; Sev = "P1"; Desc = "Non-thread-safe collection"; Fix = "Use Concurrent collection" }
    @{ Dim = "D"; Id = "D2"; Pattern = 'lock\s*\(\s*(this|typeof|"|new)'; Sev = "P0"; Desc = "Dangerous lock object"; Fix = "Use private static object" }
    @{ Dim = "D"; Id = "D3"; Pattern = 'static\s+(int|long|bool)\s+\w+.*\+\+'; Sev = "P1"; Desc = "Non-atomic static variable"; Fix = "Use Interlocked" }
    @{ Dim = "D"; Id = "D4"; Pattern = 'if\s*\(\w+\s*==\s*null\)\s*\w+\s*=\s*new'; Sev = "P2"; Desc = "Non-thread-safe lazy init"; Fix = "Use Lazy<T>" }
    @{ Dim = "D"; Id = "D5"; Pattern = 'async.*lock\s*\('; Sev = "P1"; Desc = "Lock in async method"; Fix = "Use SemaphoreSlim" }
    
    # Dimension E: Exception Handling
    @{ Dim = "E"; Id = "E1"; Pattern = 'catch\s*(\(\w*\))?\s*\{\s*\}'; Sev = "P0"; Desc = "Empty catch block"; Fix = "At least log" }
    @{ Dim = "E"; Id = "E2"; Pattern = 'catch.*\{[^}]*\breturn\b'; Sev = "P1"; Desc = "Catch without logging"; Fix = "Add logging" }
    @{ Dim = "E"; Id = "E4"; Pattern = '(return|throw).*\bex\.(Message|StackTrace)'; Sev = "P0"; Desc = "Exception info leak"; Fix = "Log, return generic error" }
    @{ Dim = "E"; Id = "E5"; Pattern = 'throw\s+new\s+Exception\s*\('; Sev = "P1"; Desc = "Exception hierarchy混乱"; Fix = "Use Oops.Oh/Oops.Bah" }
    @{ Dim = "E"; Id = "E6"; Pattern = 'throw\s+new\s+(Exception|ApplicationException)'; Sev = "P1"; Desc = "Oops compliance"; Fix = "Use JNPF exception" }
    
    # Dimension F: Hot Paths
    @{ Dim = "F"; Id = "F1"; Pattern = '\.ToList\(\)\.(Count|FirstOrDefault|Any)'; Sev = "P2"; Desc = "Redundant ToList()"; Fix = "Use chain query" }
    @{ Dim = "F"; Id = "F2"; Pattern = '\.Skip\(\s*\d{4,}\s*\)'; Sev = "P1"; Desc = "Deep pagination"; Fix = "Use Keyset Pagination" }
    @{ Dim = "F"; Id = "F3"; Pattern = 'foreach.*\.(Query|Queryable|Insertable)'; Sev = "P1"; Desc = "N+1 query"; Fix = "Use batch operations" }
    @{ Dim = "F"; Id = "F4"; Pattern = '(GetProperty|GetMethod|Activator\.CreateInstance)'; Sev = "P1"; Desc = "Hot path reflection"; Fix = "Cache reflection" }
    
    # Dimension G: Modern C#
    @{ Dim = "G"; Id = "G1"; Pattern = '<Nullable>(disable|none)</Nullable>'; Sev = "P3"; Desc = "NRT not enabled"; Fix = "Enable nullable" }
    @{ Dim = "G"; Id = "G2"; Pattern = '==\s*\d{2,}'; Sev = "P3"; Desc = "Magic number"; Fix = "Extract to constant" }
    @{ Dim = "G"; Id = "G4"; Pattern = 'public\s+string\s+(Status|State|Type)\s*\{'; Sev = "P3"; Desc = "String instead of enum"; Fix = "Use enum" }
    
    # Dimension H: Open-Closed
    @{ Dim = "H"; Id = "H1"; Pattern = 'switch\s*\([^)]+\)\s*\{[^}]*case\s+'; Sev = "P2"; Desc = "Too many branches"; Fix = "Use strategy pattern" }
    @{ Dim = "H"; Id = "H3"; Pattern = 'private\s+readonly\s+(?!I\w+)\w+\s+_'; Sev = "P2"; Desc = "Direct dependency on concrete"; Fix = "Depend on interface" }
    
    # Dimension I: Architecture
    @{ Dim = "I"; Id = "I1"; Pattern = 'using\s+JNPF\.\w+\.(Internal|Services|Impl)'; Sev = "P0"; Desc = "Module boundary violation"; Fix = "Reference .Interfaces" }
    @{ Dim = "I"; Id = "I2"; Pattern = '(_db|_context|_sqlSugar)\.(Query|Queryable)'; Sev = "P0"; Desc = "Service direct DB access"; Fix = "Use repository" }
    
    # Dimension J: Security
    @{ Dim = "J"; Id = "J1"; Pattern = '(SELECT|INSERT|UPDATE|DELETE).*\+\s*'; Sev = "P0"; Desc = "SQL injection risk"; Fix = "Parameterize" }
    @{ Dim = "J"; Id = "J2"; Pattern = '(password|secret|apiKey)\s*=\s*"[^"]+"'; Sev = "P0"; Desc = "Hardcoded secrets"; Fix = "Use config" }
    @{ Dim = "J"; Id = "J4"; Pattern = '(Path\.Combine|File\.(Read|Write|Delete)).*\+'; Sev = "P0"; Desc = "Path traversal"; Fix = "Validate path" }
    @{ Dim = "J"; Id = "J5"; Pattern = '(DeserializeObject|BinaryFormatter)'; Sev = "P0"; Desc = "Unsafe deserialization"; Fix = "Use safe serialization" }
    @{ Dim = "J"; Id = "J6"; Pattern = 'Queryable<\w+>.*\.Where\((?!.*TenantId)'; Sev = "P0"; Desc = "Multi-tenant filter missing"; Fix = "Add ITenantFilter" }
    
    # Dimension K: Observability
    @{ Dim = "K"; Id = "K1"; Pattern = '_(db|sqlSugar)\.(Insert|Update|Delete)'; Sev = "P2"; Desc = "Missing structured log"; Fix = "Add logging" }
    
    # Dimension L: Design Patterns
    @{ Dim = "L"; Id = "L1"; Pattern = 'class\s+\w+.*\{.*public\s+\w+\s+\w+\s*\('; Sev = "P2"; Desc = "God class"; Fix = "Split responsibilities" }
    @{ Dim = "L"; Id = "L2"; Pattern = 'public\s+\w+\s*\([^)]*,[^)]*,[^)]*,[^)]*,'; Sev = "P2"; Desc = "Too many constructor params"; Fix = "Use Facade" }
    
    # Dimension M: Logging
    @{ Dim = "M"; Id = "M1"; Pattern = 'Log(Information|Warning|Error)\("[^"]*(password|token|secret)'; Sev = "P0"; Desc = "Sensitive data in log"; Fix = "Mask data" }
    
    # Dimension N: JNPF Compliance
    @{ Dim = "N"; Id = "N1"; Pattern = 'Queryable<\w+>.*\.Where\((?!.*TenantId)'; Sev = "P0"; Desc = "Multi-tenant filter missing (R4)"; Fix = "Add ITenantFilter" }
    @{ Dim = "N"; Id = "N2"; Pattern = '(\$"|string\.Format).*SELECT.*\{'; Sev = "P0"; Desc = "SQL injection (R7)"; Fix = "Parameterize" }
    @{ Dim = "N"; Id = "N3"; Pattern = 'public\s+.*\s+\w+\([^)]*\)\s*\{(?!.*\[AllowAnonymous\]|.*\[SecurityDefine\])'; Sev = "P0"; Desc = "API permission missing (R8)"; Fix = "Add permission" }
    @{ Dim = "N"; Id = "N5"; Pattern = ':\s*IDynamicApiController(?!.*\[ApiDescriptionSettings\])'; Sev = "P1"; Desc = "Route compliance"; Fix = "Add ApiDescriptionSettings" }
    @{ Dim = "N"; Id = "N6"; Pattern = 'throw\s+new\s+(Exception|ApplicationException)'; Sev = "P1"; Desc = "Oops compliance"; Fix = "Use Oops.Oh/Oops.Bah" }
    @{ Dim = "N"; Id = "N7"; Pattern = 'using\s+JNPF\.\w+\.(Internal|Services|Impl)'; Sev = "P0"; Desc = "Module boundary violation"; Fix = "Reference .Interfaces" }
    
    # Dimension O: SqlSugar
    @{ Dim = "O"; Id = "O2"; Pattern = '\.ToSql\(\)'; Sev = "P1"; Desc = "Parameterized query missing"; Fix = "Verify SQL parameterization" }
    @{ Dim = "O"; Id = "O3"; Pattern = 'foreach.*\.(Query|InSingle|GetById)'; Sev = "P1"; Desc = "SqlSugar N+1 query"; Fix = "Batch load" }
    @{ Dim = "O"; Id = "O4"; Pattern = '\[SugarTable\("[^"]*"\)\]'; Sev = "P2"; Desc = "SugarTable naming"; Fix = "Follow naming convention" }
    @{ Dim = "O"; Id = "O5"; Pattern = 'BeginTransaction(?!.*Commit|.*Rollback)'; Sev = "P0"; Desc = "Transaction boundary unclear"; Fix = "Ensure complete" }
)

function Test-N3IsApiMethod {
    param([string[]]$Lines, [int]$MethodLineIndex)
    
    # 1. Check if file contains IDynamicApiController - if not, not an API class
    $hasApiController = $false
    for ($i = 0; $i -lt [Math]::Min(50, $Lines.Count); $i++) {
        if ($Lines[$i] -match 'IDynamicApiController') {
            $hasApiController = $true
            break
        }
    }
    if (-not $hasApiController) { return $false }
    
    # 2. Check if class is static
    for ($i = 0; $i -lt [Math]::Min(30, $Lines.Count); $i++) {
        if ($Lines[$i] -match '^\s*(public\s+)?static\s+class\s+') { return $false }
    }
    
    # 3. Check if file is Helpers/Utils/Extensions/Formatter - not API classes
    $filePath = ""
    # We can't get file path here, but we can check class name patterns
    for ($i = 0; $i -lt [Math]::Min(30, $Lines.Count); $i++) {
        if ($Lines[$i] -match 'class\s+\w*(Helpers?|Utils?|Extensions?|Formatter|Mapper|Converter)\w*\s') { return $false }
    }
    
    # 4. Check if method is static (static methods in API class are usually helpers, not endpoints)
    if ($MethodLineIndex -gt 0) {
        $methodLine = $Lines[$MethodLineIndex]
        if ($methodLine -match 'public\s+static\s+') { return $false }
    }
    
    # 5. Check if method has route attributes (HttpGet, HttpPost, etc.) - strong indicator of API
    $hasRouteAttr = $false
    for ($i = [Math]::Max(0, $MethodLineIndex - 8); $i -lt $MethodLineIndex; $i++) {
        if ($Lines[$i] -match '\[(HttpGet|HttpPost|HttpPut|HttpDelete|HttpPatch|Route|MapToApiVersion|AllowAnonymous|SecurityDefine)') {
            $hasRouteAttr = $true
            break
        }
    }
    
    # 6. Check if method already has [SecurityDefine] or [AllowAnonymous]
    for ($i = [Math]::Max(0, $MethodLineIndex - 8); $i -lt $MethodLineIndex; $i++) {
        if ($Lines[$i] -match '^\s*\[(SecurityDefine|AllowAnonymous)') {
            return $false  # Already has permission
        }
    }
    
    # 7. Class has IDynamicApiController + method is instance (not static) + has route attribute = API method
    # Class has IDynamicApiController + method is instance (not static) but no route = likely API method (RESTful convention)
    return $true
}

function Scan-File {
    param([string]$FilePath, [string]$ModuleName)
    
    $findings = @()
    $content = Get-Content $FilePath -Raw -ErrorAction SilentlyContinue
    if (-not $content) { return $findings }
    
    $lines = Get-Content $FilePath -ErrorAction SilentlyContinue
    
    foreach ($rule in $Rules) {
        try {
            $matches = [regex]::Matches($content, $rule.Pattern, 'Multiline')
            foreach ($match in $matches) {
                $lineNum = ($content.Substring(0, $match.Index) -split "`n").Count
                $lineContent = if ($lineNum -le $lines.Count) { $lines[$lineNum - 1].Trim() } else { "" }
                
                # N3: Only flag genuine API methods in IDynamicApiController classes
                if ($rule.Id -eq "N3") {
                    $methodLineIdx = $lineNum - 1
                    if (-not (Test-N3IsApiMethod -Lines $lines -MethodLineIndex $methodLineIdx)) {
                        continue  # Skip false positives
                    }
                }
                
                $findings += [PSCustomObject]@{
                    Dimension = $rule.Dim
                    RuleId = $rule.Id
                    Severity = $rule.Sev
                    Module = $ModuleName
                    File = $FilePath
                    Line = $lineNum
                    Code = $lineContent.Substring(0, [Math]::Min(100, $lineContent.Length))
                    Description = $rule.Desc
                    Fix = $rule.Fix
                }
            }
        } catch { }
    }
    
    return $findings
}

function Scan-Module {
    param([string]$ModuleName)
    
    $modularityRoot = "D:\JNPF-v52\backend\modularity"
    $modulePath = Join-Path $modularityRoot $ModuleName
    
    Write-Host "Scanning module: $ModuleName" -ForegroundColor Cyan
    
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
    
    Write-Host "  Total: $($allFindings.Count) issues" -ForegroundColor $(if ($allFindings.Count -gt 0) { "Yellow" } else { "Green" })
    return $allFindings
}

# Main execution
Write-Host "=== JNPF Backend Code Audit Scanner ===" -ForegroundColor Green
Write-Host "Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

$allFindings = @()
$modules = @()

if ($All) {
    $modules = @("system", "oauth", "workflow", "visualdev", "codegen", "inteAssistant", "message", "engine", "app", "common", "extend", "report", "subdev", "taskscheduler", "visualdata", "zxdev")
} elseif ($Batch) {
    switch ($Batch) {
        1 { $modules = @("system", "oauth") }
        2 { $modules = @("workflow", "visualdev") }
        3 { $modules = @("codegen", "inteAssistant", "message") }
        4 { $modules = @("engine", "app", "common") }
        5 { $modules = @("extend", "report", "subdev", "taskscheduler", "visualdata", "zxdev") }
    }
} elseif ($Module) {
    $modules = @($Module)
} else {
    Write-Host "Usage: .\scan.ps1 -Module <name> | -Batch <1-5> | -All" -ForegroundColor Yellow
    exit 1
}

foreach ($mod in $modules) {
    $findings = Scan-Module -ModuleName $mod
    $allFindings += $findings
    
    # Save module findings
    $moduleDir = Join-Path $OutputRoot "modules\$mod"
    New-Item -ItemType Directory -Force -Path $moduleDir | Out-Null
    $allFindings | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $moduleDir "findings.json")
}

# Summary
Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Green
Write-Host "Total files: $(($allFindings | Select-Object -ExpandProperty File -Unique).Count)" -ForegroundColor White
Write-Host "Total issues: $($allFindings.Count)" -ForegroundColor White
Write-Host ""
Write-Host "By severity:" -ForegroundColor Cyan
Write-Host "  P0 (Critical): $(($allFindings | Where-Object { $_.Severity -eq 'P0' }).Count)" -ForegroundColor $(if (($allFindings | Where-Object { $_.Severity -eq 'P0' }).Count -gt 0) { "Red" } else { "Green" })
Write-Host "  P1 (High): $(($allFindings | Where-Object { $_.Severity -eq 'P1' }).Count)" -ForegroundColor $(if (($allFindings | Where-Object { $_.Severity -eq 'P1' }).Count -gt 0) { "Yellow" } else { "Green" })
Write-Host "  P2 (Medium): $(($allFindings | Where-Object { $_.Severity -eq 'P2' }).Count)" -ForegroundColor Gray
Write-Host "  P3 (Low): $(($allFindings | Where-Object { $_.Severity -eq 'P3' }).Count)" -ForegroundColor Gray

# Save summary
$summary = @{
    scanDate = Get-Date -Format "yyyy-MM-dd"
    totalFiles = ($allFindings | Select-Object -ExpandProperty File -Unique).Count
    totalIssues = $allFindings.Count
    bySeverity = @{
        P0 = ($allFindings | Where-Object { $_.Severity -eq "P0" }).Count
        P1 = ($allFindings | Where-Object { $_.Severity -eq "P1" }).Count
        P2 = ($allFindings | Where-Object { $_.Severity -eq "P2" }).Count
        P3 = ($allFindings | Where-Object { $_.Severity -eq "P3" }).Count
    }
    topIssues = $allFindings | Where-Object { $_.Severity -eq "P0" -or $_.Severity -eq "P1" } | Select-Object -First 20
}

$summary | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $OutputRoot "scan-summary.json")
$allFindings | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $OutputRoot "all-findings.json")

Write-Host ""
Write-Host "=== Scan Complete ===" -ForegroundColor Green