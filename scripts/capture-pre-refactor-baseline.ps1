# capture-pre-refactor-baseline.ps1
#
# CRITICAL: Run this FIRST, BEFORE any code modification.
# Freezes PRE_REFACTOR_COMMIT and captures immutable pre-refactor state.
#
# v5 HARDENING (Chief Architect v4 review):
#   - B1: provenance uses git BLOB SHA (git rev-parse), not materialised file hash
#   - P1-3: try/finally guarantees refactored file is restored on any failure
#
# This script MUST run before any modification of:
#   backend/modularity/workflow/JNPF.WorkFlow/Service/FlowCommentService.cs

param(
    [string]$ProjectPath   = "D:\JNPF-v52\backend\modularity\workflow\JNPF.WorkFlow\JNPF.WorkFlow.csproj",
    [string]$OutputPath    = "D:\JNPF-v52\backend\tests\JNPF.Tests.Runtime.Expert\build-baseline.json",
    [string]$RepoRoot      = "D:\JNPF-v52",
    [string]$TargetRelPath = "backend/modularity/workflow/JNPF.WorkFlow/Service/FlowCommentService.cs"
)

$ErrorActionPreference = "Stop"

# Canonical command string — same as CanonicalBuildRunner.ComposeCommandLine in tests
$command = "dotnet build `"$ProjectPath`" --no-restore --no-incremental"

Write-Host "=== capture-pre-refactor-baseline.ps1 (v5) ===" -ForegroundColor Cyan
Write-Host "Project: $ProjectPath"
Write-Host "Target:  $TargetRelPath"
Write-Host "Output:  $OutputPath"
Write-Host ""

# Step 0.1: Freeze commit SHA
$preRefactorCommit = (& git -C $RepoRoot rev-parse HEAD).Trim()
Write-Host "Step 0.1  PRE_REFACTOR_COMMIT = $preRefactorCommit" -ForegroundColor Yellow

$absoluteTarget = Join-Path $RepoRoot ($TargetRelPath.Replace('/', '\'))

# Step 0.2: Back up current (refactored) file — this is the FAIL-SAFE state to restore
$refactoredBackup = "$absoluteTarget.refactored.backup.v5"
$hadRefactoredBackup = $false
if (Test-Path $absoluteTarget) {
    Copy-Item $absoluteTarget $refactoredBackup -Force
    $hadRefactoredBackup = $true
    Write-Host "Step 0.2  Backed up refactored file -> $refactoredBackup"
}

# Step 0.3-0.10: Pre-refactor materialisation + capture (try/finally guarantees restore)
$referenceBlobSha = ""
$binaryHash = "NOT_BUILT"
$errorCount = 0
$warningCount = 0
$warningSamples = @()
$buildSucceeded = $false
$baseline = $null

try {
    # [B1] Step 0.3: BLOB identity (NOT materialised bytes)
    # git rev-parse returns the BLOB SHA from the object database — independent of
    # any text encoding, newline conversion, BOM, or PowerShell Out-File pipeline.
    $referenceBlobSha = (& git -C $RepoRoot rev-parse "${preRefactorCommit}:${TargetRelPath}").Trim()
    Write-Host "Step 0.3  referenceBlobSha = $referenceBlobSha" -ForegroundColor Yellow

    # Step 0.4: Materialise pre-refactor source for build only (never hashed)
    $preRefactorMaterial = & git -C $RepoRoot show "${preRefactorCommit}:${TargetRelPath}"
    [System.IO.File]::WriteAllText($absoluteTarget, $preRefactorMaterial, [System.Text.UTF8Encoding]::new($false))
    Write-Host "Step 0.4  Materialised pre-refactor source for build"

    # Step 0.5: Build with PRE-REFACTOR code, using canonical command (same as tests use)
    Write-Host "Step 0.5  Building: $command" -ForegroundColor Yellow
    $buildOutput = & dotnet build $ProjectPath --no-restore --no-incremental 2>&1 | Out-String
    $buildSucceeded = ($LASTEXITCODE -eq 0)
    Write-Host "         Build exit code: $LASTEXITCODE"

    # Step 0.6: Strict MSBuild parsing
    $errorMatches = [regex]::Matches($buildOutput, "error CS\d+:")
    $warningMatches = [regex]::Matches($buildOutput, "warning CS\d+:")
    $errorCount = $errorMatches.Count
    $warningCount = $warningMatches.Count
    Write-Host "Step 0.6  Errors: $errorCount, Warnings: $warningCount"

    # Step 0.7: Binary hash (post-build artifact)
    $binPath = Join-Path $RepoRoot "backend\modularity\workflow\JNPF.WorkFlow\bin\Debug\net8.0\JNPF.WorkFlow.dll"
    if (Test-Path $binPath) {
        $binaryHash = (Get-FileHash $binPath -Algorithm SHA256).Hash
        Write-Host "Step 0.7  Binary hash: $binaryHash"
    } else {
        Write-Host "Step 0.7  WARNING: $binPath not found" -ForegroundColor Red
    }

    # Step 0.8: SDK version
    $sdkVersion = (& dotnet --version).Trim()
    Write-Host "Step 0.8  SDK version: $sdkVersion"

    # Step 0.9: Warning samples (first 50)
    $warningSamples = @($warningMatches | Select-Object -First 50 | ForEach-Object { $_.Value })

    # Step 0.10: Compose baseline object
    $baseline = [ordered]@{
        preRefactorCommit = $preRefactorCommit
        timestamp         = (Get-Date).ToString("o")
        command           = $command
        sdkVersion        = $sdkVersion
        project           = $ProjectPath
        workingDirectory  = $RepoRoot
        targetRelPath     = $TargetRelPath
        errorCount        = $errorCount
        warningCount      = $warningCount
        binaryHash        = $binaryHash
        referenceBlobSha  = $referenceBlobSha   # [B1] replaces v4 referenceSourceHash
        buildSucceeded    = $buildSucceeded
        warningSamples    = $warningSamples
    }
}
finally {
    # [P1-3] ALWAYS restore refactored file, even on exception
    if ($hadRefactoredBackup -and (Test-Path $refactoredBackup)) {
        Copy-Item $refactoredBackup $absoluteTarget -Force
        Remove-Item $refactoredBackup -Force
        Write-Host "Step finally  RESTORED refactored file (try/finally safety)" -ForegroundColor Green
    }
}

if ($null -eq $baseline) {
    throw "Baseline capture aborted before baseline.json was produced."
}

# Step 0.11: Write baseline.json (immutable)
$baseline | ConvertTo-Json -Depth 5 | Out-File -FilePath $OutputPath -Encoding UTF8
Write-Host ""
Write-Host "=== Pre-Refactor Baseline Captured (v5) ===" -ForegroundColor Green
Write-Host "PRE_REFACTOR_COMMIT = $preRefactorCommit"
Write-Host "Reference BLOB SHA  = $referenceBlobSha"
Write-Host "Build success:      $buildSucceeded"
Write-Host "Errors: $errorCount, Warnings: $warningCount"
Write-Host "Binary: $binaryHash"
Write-Host "Written to:         $OutputPath"