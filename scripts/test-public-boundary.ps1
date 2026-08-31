$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$forbidden = @(
    "MostlyArmless", "Mostly Armless", "MA-Dev",
    "uk.mostlyarmless", "D:\_Code_", "D:\\_Code_", "C:\Users\Captain", "C:\\Users\\Captain", "D:\MA-Updates", "D:\\MA-Updates"
)
$extensions = @(".cs", ".xaml", ".csproj", ".ts", ".tsx", ".css", ".json", ".md", ".ps1", ".iss", ".yml", ".yaml")
$publishablePaths = & git -C $repo ls-files --cached --others --exclude-standard
if ($LASTEXITCODE) { throw "Could not enumerate the Git publish boundary." }
$files = $publishablePaths | Where-Object {
    $_ -ne "scripts/test-public-boundary.ps1" -and $extensions -contains [IO.Path]::GetExtension($_)
} | ForEach-Object { Get-Item -LiteralPath (Join-Path $repo $_) }
$issues = [Collections.Generic.List[string]]::new()
foreach ($file in $files | Sort-Object FullName -Unique) {
    $text = [IO.File]::ReadAllText($file.FullName)
    foreach ($term in $forbidden) {
        if ($text.Contains($term, [StringComparison]::OrdinalIgnoreCase)) {
            $issues.Add("$($file.FullName.Substring($repo.Length + 1)): forbidden public term '$term'")
        }
    }
    if ($text -match "gh[pousr]_[A-Za-z0-9_]{20,}" -or $text -match "github_pat_[A-Za-z0-9_]{20,}" -or $text -match "https://discord(?:app)?\.com/api/webhooks/\d+/[A-Za-z0-9._-]+") {
        $issues.Add("$($file.FullName.Substring($repo.Length + 1)): credential-shaped value")
    }
}
if ($issues.Count) { throw ("Public boundary failed:" + [Environment]::NewLine + ($issues -join [Environment]::NewLine)) }
Write-Host "Public boundary: PASS ($($files.Count) files scanned)."
