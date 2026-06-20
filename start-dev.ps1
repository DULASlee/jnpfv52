# start-dev.ps1 — 统一启动脚本
# 彻底清理所有 dotnet/node 相关进程后再启动

$ErrorActionPreference = 'SilentlyContinue'

Write-Host "=== 1. 清理旧进程 ===" -ForegroundColor Yellow

# 1a. 杀掉所有 dotnet.exe 进程（包括 watch、MSBuild 节点、JNPF.API.Entry）
$dotnetProcs = Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'"
if ($dotnetProcs) {
    $dotnetProcs | ForEach-Object {
        Stop-Process -Id $_.ProcessId -Force
        Write-Host "  Killed dotnet PID $($_.ProcessId)"
    }
}

# 1b. 杀掉所有 JNPF.API.Entry.exe 进程（独立宿主进程）
Get-Process -Name "JNPF.API.Entry" -ErrorAction SilentlyContinue | ForEach-Object {
    Stop-Process -Id $_.Id -Force
    Write-Host "  Killed JNPF.API.Entry PID $($_.Id)"
}

# 1c. 杀掉所有 node.exe 进程（Vite/esbuild）
$nodeProcs = Get-CimInstance Win32_Process -Filter "Name='node.exe'"
if ($nodeProcs) {
    $nodeProcs | ForEach-Object {
        Stop-Process -Id $_.ProcessId -Force
        Write-Host "  Killed node PID $($_.ProcessId)"
    }
}

# 1d. 等待进程退出
Start-Sleep -Seconds 2

# 1e. 端口占用兜底
foreach ($port in @(3100, 5000, 3001)) {
    $conn = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    if ($conn) {
        $conn | ForEach-Object {
            Stop-Process -Id $_.OwningProcess -Force
            Write-Host "  Killed process on port $port"
        }
    }
}

Write-Host "=== 清理完成 ===" -ForegroundColor Green

# 2. 启动前端（新窗口）
Write-Host "=== 2. 启动前端 (Vite) ===" -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd 'D:\JNPF-v52\jnpf-web-vue3'; `$env:NODE_OPTIONS='--max-old-space-size=4096'; npx vite --mode development"

# 3. 启动 SA Service（新窗口）
Write-Host "=== 3. 启动 SA 结构化分析服务 ===" -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd 'D:\JNPF-v52\sa-service'; `$env:SA_SERVICE_PORT='3001'; `$env:SA_DB_BACKEND='inmemory'; `$env:LLM_GATEWAY_URL='http://localhost:5000/api/LlmGateway/ChatAsync'; npx tsx src/server.ts"

# 4. 启动后端（新窗口）
Write-Host "=== 4. 启动后端 ===" -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd 'D:\JNPF-v52\backend'; dotnet watch run --project application/JNPF.API.Entry/JNPF.API.Entry.csproj --urls 'http://0.0.0.0:5000'"

Write-Host ""
Write-Host "=== 启动指令已发送 ===" -ForegroundColor Green
Write-Host "前端:    http://localhost:3100" -ForegroundColor Cyan
Write-Host "SA 服务: http://localhost:3001" -ForegroundColor Cyan
Write-Host "后端:    http://localhost:5000" -ForegroundColor Cyan
