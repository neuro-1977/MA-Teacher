param(
    [string]$Repository = "neuro-1977/MA-Teacher",
    [string]$BaseAddress = "http://127.0.0.1:5201",
    [ValidateRange(1, 200)][int]$MaxIssues = 200
)
$ErrorActionPreference = "Stop"
if ($Repository -ne "neuro-1977/MA-Teacher") { throw "Only the canonical MA-Teacher repository may be imported." }
& gh auth status | Out-Null
if ($LASTEXITCODE) { throw "GitHub CLI authentication is required." }
$json = & gh issue list --repo $Repository --state all --limit $MaxIssues --json number,id,state,title,body,url,author,createdAt,updatedAt,labels,comments
if ($LASTEXITCODE) { throw "GitHub issue retrieval failed." }
$issues = @($json | ConvertFrom-Json)
$payload = @{
    repository = $Repository
    issues = @($issues | ForEach-Object {
        @{
            number = [int]$_.number
            nodeId = [string]$_.id
            state = ([string]$_.state).ToLowerInvariant()
            title = [string]$_.title
            body = [string]$_.body
            url = [string]$_.url
            author = [string]$_.author.login
            createdAt = ([DateTimeOffset]$_.createdAt).ToUniversalTime().ToString("O")
            updatedAt = ([DateTimeOffset]$_.updatedAt).ToUniversalTime().ToString("O")
            labels = @($_.labels | ForEach-Object { [string]$_.name })
            comments = @($_.comments | ForEach-Object {
                @{
                    id = [string]$_.id
                    author = [string]$_.author.login
                    body = [string]$_.body
                    url = [string]$_.url
                    createdAt = ([DateTimeOffset]$_.createdAt).ToUniversalTime().ToString("O")
                    updatedAt = ([DateTimeOffset]$_.updatedAt).ToUniversalTime().ToString("O")
                }
            })
        }
    })
} | ConvertTo-Json -Depth 12 -Compress
$headers = @{
    Origin = $BaseAddress.TrimEnd('/')
    "X-MA-Teacher-Intent" = "import-github-feedback"
}
$result = Invoke-RestMethod -Method Post -Uri "$($BaseAddress.TrimEnd('/'))/api/development/feedback" -Headers $headers -ContentType "application/json" -Body $payload
if (!$result.ok) { throw "Feedback import was refused: $($result.error)" }
$result | ConvertTo-Json -Depth 6
