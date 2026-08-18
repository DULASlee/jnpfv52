# 清理 Serena 遗留孤儿进程（OmniSharp / multiprocessing worker）
# 根因：Serena Python 重启时不杀旧 OmniSharp；Cursor 多路 SSE 连接触发 LSP 重载
# 用法: powershell -ExecutionPolicy Bypass -File scripts/serena-cleanup-orphans.ps1

$ErrorActionPreference = 'SilentlyContinue'

function Test-ProcessAlive([int]$ProcessId) {
    return $null -ne (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue)
}

$killedOmni = 0
Get-Process OmniSharp -ErrorAction SilentlyContinue | ForEach-Object {
    $cmd = (Get-CimInstance Win32_Process -Filter "ProcessId=$($_.Id)").CommandLine
    if ($cmd -match 'hostPID (\d+)') {
        $parentId = [int]$Matches[1]
        if (-not (Test-ProcessAlive $parentId)) {
            Stop-Process -Id $_.Id -Force
            $killedOmni++
            Write-Host "Killed orphan OmniSharp PID $($_.Id) (dead parent $parentId)"
        }
    }
}

$killedMp = 0
Get-CimInstance Win32_Process -Filter "Name='python.exe'" | ForEach-Object {
    if ($_.CommandLine -match 'multiprocessing-fork' -and $_.ParentProcessId) {
        if (-not (Test-ProcessAlive $_.ParentProcessId)) {
            Stop-Process -Id $_.ProcessId -Force
            $killedMp++
            Write-Host "Killed orphan python worker PID $($_.ProcessId)"
        }
    }
}

Write-Host ""
Write-Host "Summary: killed $killedOmni orphan OmniSharp, $killedMp orphan python workers"
Write-Host "Live serena.exe: $((Get-Process serena -ErrorAction SilentlyContinue).Count)"
Write-Host "Live OmniSharp:  $((Get-Process OmniSharp -ErrorAction SilentlyContinue).Count)"
Write-Host "SSE :9900 connections:"
netstat -ano | findstr ":9900" | findstr "ESTABLISHED"
