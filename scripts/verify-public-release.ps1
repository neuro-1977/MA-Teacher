param([string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$version = [string]((Get-Content (Join-Path $repo "ma-app.json") -Raw | ConvertFrom-Json).version)
$artifacts = Join-Path $repo "artifacts"
$installRoot = Join-Path ([IO.Path]::GetTempPath()) "MA-Teacher-CI-$([Guid]::NewGuid().ToString('N'))"
& (Join-Path $PSScriptRoot "test-public-boundary.ps1")
Push-Location (Join-Path $repo "web")
try {
    & npm.cmd ci
    if ($LASTEXITCODE) { throw "npm ci failed." }
    & npm.cmd run typecheck
    if ($LASTEXITCODE) { throw "UI typecheck failed." }
    & npm.cmd audit --audit-level=high
    if ($LASTEXITCODE) { throw "npm high-severity audit failed." }
} finally { Pop-Location }
& dotnet build (Join-Path $repo "ModuleShell\ModuleShell.csproj") -c $Configuration --nologo
if ($LASTEXITCODE) { throw "Desktop build failed." }
& (Join-Path $repo "Installer\build-installer.ps1") -Configuration $Configuration -SkipNpmRestore
if ($LASTEXITCODE) { throw "Installer build failed." }
$installer = Join-Path $repo "Installer\bin\MA-Teacher-Setup.exe"
if (!(Test-Path $installer)) { throw "Installer output is missing." }
$published = Join-Path $repo "Installer\publish\MA-Teacher.exe"
$process = Start-Process $published -ArgumentList "--self-test" -Wait -PassThru
if ($process.ExitCode) { throw "Published self-test failed: $($process.ExitCode)" }
try {
    $process = Start-Process $installer -ArgumentList @("/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART", "/SP-", "/DIR=$installRoot") -Wait -PassThru
    if ($process.ExitCode) { throw "Install failed: $($process.ExitCode)" }
    $installed = Join-Path $installRoot "MA-Teacher.exe"
    if (!(Test-Path $installed)) { throw "Installed executable is missing." }
    $process = Start-Process $installed -ArgumentList "--self-test" -Wait -PassThru
    if ($process.ExitCode) { throw "Installed self-test failed: $($process.ExitCode)" }
    $uninstaller = Join-Path $installRoot "unins000.exe"
    if (!(Test-Path $uninstaller)) { throw "Uninstaller is missing." }
    $process = Start-Process $uninstaller -ArgumentList @("/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART") -Wait -PassThru
    if ($process.ExitCode) { throw "Uninstall failed: $($process.ExitCode)" }
} finally {
    if (Test-Path $installRoot) { Remove-Item $installRoot -Recurse -Force -ErrorAction SilentlyContinue }
}
New-Item -ItemType Directory -Force $artifacts | Out-Null
$versioned = Join-Path $artifacts "MA-Teacher-Setup-$version.exe"
$latest = Join-Path $artifacts "MA-Teacher-Setup-latest.exe"
Copy-Item $installer $versioned -Force
Copy-Item $installer $latest -Force
$hash = (Get-FileHash $versioned -Algorithm SHA256).Hash
Set-Content (Join-Path $artifacts "SHA256SUMS.txt") -Encoding ascii -Value @("$hash  MA-Teacher-Setup-$version.exe", "$hash  MA-Teacher-Setup-latest.exe")
[pscustomobject]@{ product = "MA-Teacher"; version = $version; installer = $versioned; bytes = (Get-Item $versioned).Length; sha256 = $hash; publishedSelfTest = "passed"; installedSelfTest = "passed"; silentUninstall = "passed" } | ConvertTo-Json
