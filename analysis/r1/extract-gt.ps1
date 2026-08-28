$exe = 'd:\JNPF-v52\tools\JnpfAnalyzer\bin\Release\net8.0\JnpfAnalyzer.dll'
$cg  = 'd:\JNPF-v52\analysis\r1\run1\callgraph.json'
$samples = 'd:\JNPF-v52\analysis\r1\samples'
New-Item -ItemType Directory -Force $samples | Out-Null

& dotnet $exe --extract $cg --filter FileService --out (Join-Path $samples 'FileService.DownloadAll.json')
Write-Host "exit1=$LASTEXITCODE"
& dotnet $exe --extract $cg --filter ScheduleService --out (Join-Path $samples 'ScheduleService.Delete.json')
Write-Host "exit2=$LASTEXITCODE"
& dotnet $exe --extract $cg --filter OrderService --out (Join-Path $samples 'OrderService.Save-Delete.json')
Write-Host "exit3=$LASTEXITCODE"
