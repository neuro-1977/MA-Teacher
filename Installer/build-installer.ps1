param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$MaUpdatesDir = "D:\MA-Updates"
)

$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$web = Join-Path $repo "web"
$project = Join-Path $repo "ModuleShell\ModuleShell.csproj"
$publish = Join-Path $PSScriptRoot "publish"
$iss = Join-Path $PSScriptRoot "MA-Teacher.iss"
$compiled = Join-Path $PSScriptRoot "bin\MA-Teacher-Setup.exe"
$latest = Join-Path $MaUpdatesDir "MA-Teacher-Setup-latest.exe"
$version = [string]((Get-Content -LiteralPath (Join-Path $repo "ma-app.json") -Raw | ConvertFrom-Json).version)

$iscc = @("C:\Program Files (x86)\Inno Setup 6\ISCC.exe", "C:\Program Files\Inno Setup 6\ISCC.exe") |
    Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $iscc) { throw "Inno Setup 6 compiler was not found." }

foreach ($required in $project, $iss, (Join-Path $repo "assets\MA-Teacher.ico"), (Join-Path $repo "icon-large.png")) {
    if (-not (Test-Path -LiteralPath $required)) { throw "Required packaging input is missing: $required" }
}

Push-Location $web
try {
    & npm.cmd run build
    if ($LASTEXITCODE -ne 0) { throw "MA-Teacher web build failed." }
} finally { Pop-Location }

$dist = Join-Path $web "dist"
$distIndex = Join-Path $dist "index.html"
if (-not (Test-Path -LiteralPath $distIndex)) { throw "Vite did not produce dist\index.html." }
$index = Get-Content -LiteralPath $distIndex -Raw
$assetRefs = [regex]::Matches($index, '(?:src|href)="/?([^"#?]+)"') | ForEach-Object { $_.Groups[1].Value } | Where-Object { $_ -notmatch '^https?://' }
foreach ($assetRef in $assetRefs) {
    $asset = Join-Path $dist ($assetRef -replace '/', '\')
    if (-not (Test-Path -LiteralPath $asset)) { throw "Built UI references a missing asset: $assetRef" }
}

if (Test-Path -LiteralPath $publish) { Remove-Item -LiteralPath $publish -Recurse -Force }
& dotnet publish $project -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false -o $publish
if ($LASTEXITCODE -ne 0) { throw "MA-Teacher desktop publish failed." }

foreach ($required in (Join-Path $publish "MA-Teacher.exe"), (Join-Path $publish "ui\index.html")) {
    if (-not (Test-Path -LiteralPath $required)) { throw "Published payload is incomplete: $required" }
}

& $iscc "/DMyAppVersion=$version" $iss
if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed." }
if (-not (Test-Path -LiteralPath $compiled)) { throw "Expected installer was not produced: $compiled" }

New-Item -ItemType Directory -Path $MaUpdatesDir -Force | Out-Null
Copy-Item -LiteralPath $compiled -Destination $latest -Force
$sourceHash = (Get-FileHash -LiteralPath $compiled -Algorithm SHA256).Hash
$latestHash = (Get-FileHash -LiteralPath $latest -Algorithm SHA256).Hash
if ($sourceHash -ne $latestHash) { throw "MA-Updates copy hash does not match the compiled installer." }

[pscustomobject]@{
    product = "MA-Teacher"
    version = $version
    installerPath = $latest
    bytes = (Get-Item -LiteralPath $latest).Length
    sha256 = $latestHash
} | ConvertTo-Json
