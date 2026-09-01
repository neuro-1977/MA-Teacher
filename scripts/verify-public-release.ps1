param([string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$version = [string]((Get-Content (Join-Path $repo "ma-app.json") -Raw | ConvertFrom-Json).version)
$artifacts = Join-Path $repo "artifacts"
$installRoot = Join-Path ([IO.Path]::GetTempPath()) "MA-Teacher-CI-$([Guid]::NewGuid().ToString('N'))"
$installerSource = Get-Content (Join-Path $repo "Installer\MA-Teacher.iss") -Raw
$networkMarkers = @(
    'Name: "classroomnetwork"',
    'Check: IsAdminInstallMode',
    'http add urlacl url=http://+:5202/ sddl=D:(A;;GX;;;BU)',
    'profile=private,domain',
    'program="'' +',
    'ExpandConstant(''{app}\{#MyAppExeName}'')',
    'classroom-network.owner',
    'http delete urlacl url=http://+:5202/',
    'advfirewall firewall delete rule name="MA-Teacher Classroom Relay"'
)
foreach ($marker in $networkMarkers) {
    if ($installerSource.IndexOf($marker, [StringComparison]::Ordinal) -lt 0) {
        throw "Installer classroom-network contract is missing: $marker"
    }
}
if ($installerSource -match '(?i)profile\s*=\s*(?:any|public)') {
    throw "Installer must never expose the classroom relay on the Public firewall profile."
}
$principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
$exerciseNetworkInstall = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
& (Join-Path $PSScriptRoot "test-public-boundary.ps1")
[void][scriptblock]::Create((Get-Content (Join-Path $PSScriptRoot "sync-github-feedback.ps1") -Raw))
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
    $installArguments = @("/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART", "/SP-", "/DIR=$installRoot")
    if ($exerciseNetworkInstall) { $installArguments += @("/ALLUSERS", "/TASKS=classroomnetwork") }
    $process = Start-Process $installer -ArgumentList $installArguments -Wait -PassThru
    if ($process.ExitCode) { throw "Install failed: $($process.ExitCode)" }
    $installed = Join-Path $installRoot "MA-Teacher.exe"
    if (!(Test-Path $installed)) { throw "Installed executable is missing." }
    if ($exerciseNetworkInstall) {
        $marker = Join-Path $installRoot "data\classroom-network.owner"
        if (!(Test-Path $marker)) { throw "Elevated classroom-network install did not write its ownership marker." }
        $urlAcl = (& netsh.exe http show urlacl url=http://+:5202/ 2>&1 | Out-String)
        if ($LASTEXITCODE -ne 0 -or $urlAcl.IndexOf('http://+:5202/', [StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw "The exact classroom URL reservation was not installed."
        }
        $rules = @(Get-NetFirewallRule -DisplayName "MA-Teacher Classroom Relay" -ErrorAction SilentlyContinue)
        if ($rules.Count -ne 1) { throw "Expected exactly one owned classroom firewall rule; found $($rules.Count)." }
        $rule = $rules[0]
        $profiles = [string]$rule.Profile
        if ($rule.Direction -ne 'Inbound' -or $rule.Action -ne 'Allow' -or
            !$profiles.Contains('Private') -or !$profiles.Contains('Domain') -or $profiles.Contains('Public')) {
            throw "Classroom firewall scope is not exact Domain/Private inbound allow."
        }
        $portFilter = $rule | Get-NetFirewallPortFilter
        if ($portFilter.Protocol -ne 'TCP' -or [string]$portFilter.LocalPort -ne '5202') {
            throw "Classroom firewall port is not exact TCP 5202."
        }
        $applicationFilter = $rule | Get-NetFirewallApplicationFilter
        if (![IO.Path]::GetFullPath([string]$applicationFilter.Program).Equals([IO.Path]::GetFullPath($installed), [StringComparison]::OrdinalIgnoreCase)) {
            throw "Classroom firewall rule is not confined to the installed MA-Teacher executable."
        }
    }
    $process = Start-Process $installed -ArgumentList "--self-test" -Wait -PassThru
    if ($process.ExitCode) { throw "Installed self-test failed: $($process.ExitCode)" }
    $uninstaller = Join-Path $installRoot "unins000.exe"
    if (!(Test-Path $uninstaller)) { throw "Uninstaller is missing." }
    $process = Start-Process $uninstaller -ArgumentList @("/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART") -Wait -PassThru
    if ($process.ExitCode) { throw "Uninstall failed: $($process.ExitCode)" }
    if ($exerciseNetworkInstall) {
        if (Get-NetFirewallRule -DisplayName "MA-Teacher Classroom Relay" -ErrorAction SilentlyContinue) {
            throw "Uninstall left the owned classroom firewall rule behind."
        }
        $remainingUrlAcl = (& netsh.exe http show urlacl url=http://+:5202/ 2>&1 | Out-String)
        if ($remainingUrlAcl.IndexOf('http://+:5202/', [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "Uninstall left the owned classroom URL reservation behind."
        }
    }
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
[pscustomobject]@{ product = "MA-Teacher"; version = $version; installer = $versioned; bytes = (Get-Item $versioned).Length; sha256 = $hash; publishedSelfTest = "passed"; installedSelfTest = "passed"; silentUninstall = "passed"; classroomNetworkContract = "passed"; elevatedNetworkLifecycle = if ($exerciseNetworkInstall) { "passed" } else { "not-run-non-admin" } } | ConvertTo-Json
