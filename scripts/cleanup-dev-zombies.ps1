# cleanup-dev-zombies.ps1 — 释放 JNPF 开发端口 + 清理 MSBuild/VBCSCompiler/Playwright 僵尸
# 用法：powershell -ExecutionPolicy Bypass -File scripts/cleanup-dev-zombies.ps1
$ErrorActionPreference = 'SilentlyContinue'

Write-Host "=== JNPF Dev Zombie Cleanup ===" -ForegroundColor Cyan

$ports = @(3100, 5000, 3001, 8100, 3800)
foreach ($port in $ports) {
    Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
        ForEach-Object {
            $pid = $_.OwningProcess
            $name = (Get-Process -Id $pid -ErrorAction SilentlyContinue).ProcessName
            Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
            Write-Host "  Port $port freed (PID $pid $name)"
        }
}

$zombies = 0
$freedMB = 0

Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" |
    Where-Object { $_.CommandLine -match 'MSBuild|VBCSCompiler|dotnet\.dll run' } |
    ForEach-Object {
        $p = Get-Process -Id $_.ProcessId -ErrorAction SilentlyContinue
        if ($p) {
            $freedMB += [math]::Round($p.WorkingSet64 / 1MB, 0)
            Stop-Process -Id $p.Id -Force
            $zombies++
        }
    }

Get-Process -Name 'node' -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -match 'jnpf|JNPF|phase4|PhaseB' } |
    ForEach-Object {
        $freedMB += [math]::Round($_.WorkingSet64 / 1MB, 0)
        Stop-Process -Id $_.Id -Force
        $zombies++
    }

Get-Process -Name 'chrome','msedge','chromium' -ErrorAction SilentlyContinue |
    Where-Object { $_.MainWindowTitle -eq '' -or $_.Path -match 'playwright|chrome-win' } |
    ForEach-Object {
        $freedMB += [math]::Round($_.WorkingSet64 / 1MB, 0)
        Stop-Process -Id $_.Id -Force
        $zombies++
    }

[System.GC]::Collect()
[System.GC]::WaitForPendingFinalizers()

Write-Host "  Cleaned $zombies process(es), ~$freedMB MB" -ForegroundColor Green
Write-Host "Done." -ForegroundColor Cyan
