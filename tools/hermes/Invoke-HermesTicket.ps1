param(
    [Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Ticket,
    [Parameter(Mandatory)][ValidatePattern('^[a-z0-9._/-]+:free$')][string]$Model,
    [ValidateRange(30,600)][int]$RunBudgetSeconds = 180
)
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$promptPath = Join-Path $workspace "tools/hermes/prompts/$Ticket.txt"
if (-not (Test-Path -LiteralPath $promptPath -PathType Leaf)) { throw 'Unknown ticket prompt.' }
$catalog = Invoke-RestMethod -Uri 'https://openrouter.ai/api/v1/models' -TimeoutSec 30
$candidate = @($catalog.data | Where-Object { $_.id -ceq $Model })
if ($candidate.Count -ne 1) { throw 'Requested model is absent from the live catalog.' }
foreach ($field in @('prompt','completion','request')) {
    $price = $candidate[0].pricing.$field
    if ($null -eq $price -and $field -eq 'request') { continue }
    if ($null -eq $price -or [decimal]$price -ne 0) { throw "Model does not have zero $field pricing." }
}
$ticketRoot = Join-Path $workspace "artifacts/hermes-online/$Ticket"
[void][IO.Directory]::CreateDirectory($ticketRoot)
$lockPath = Join-Path $ticketRoot 'active.lock'
try { $ticketLock = [IO.File]::Open($lockPath, 'OpenOrCreate', 'ReadWrite', 'None') }
catch { throw 'This ticket already has an active runner; do not duplicate it.' }
try {
    $stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
    $runPath = Join-Path $ticketRoot $stamp
    [void][IO.Directory]::CreateDirectory($runPath)
    $outputPath = Join-Path $runPath 'response.txt'
    $manifestPath = Join-Path $runPath 'run.json'
    $manifest = [ordered]@{
        ticket=$Ticket; model=$Model; provider='openrouter'; startedUtc=[DateTime]::UtcNow.ToString('o')
        pricingCheckedUtc=[DateTime]::UtcNow.ToString('o'); promptPrice=$candidate[0].pricing.prompt
        completionPrice=$candidate[0].pricing.completion; sourceCommit=(& git -C $workspace rev-parse HEAD)
        promptSha256=(Get-FileHash -LiteralPath $promptPath -Algorithm SHA256).Hash
        budgetSeconds=$RunBudgetSeconds; state='running'; exitCode=$null; finishedUtc=$null; responseSha256=$null
    }
    $manifest | ConvertTo-Json | Set-Content -LiteralPath $manifestPath -Encoding utf8
    Push-Location $runPath
    try {
        # Self-contained online drafting: no terminal, filesystem, custom rules, plugins or MCP tools.
        & hermes chat --provider openrouter --model $Model --safe-mode --toolsets none --oneshot --quiet --max-turns 1 --run-budget $RunBudgetSeconds --query-file $promptPath *> $outputPath
        $manifest.exitCode = $LASTEXITCODE
        $manifest.state = if ($LASTEXITCODE -eq 0) { 'returned-needs-review' } else { 'failed' }
        if (Select-String -LiteralPath $outputPath -SimpleMatch 'No API key found' -Quiet) {
            $manifest.state = 'blocked-missing-credentials'
        }
    }
    finally {
        Pop-Location
        $manifest.finishedUtc = [DateTime]::UtcNow.ToString('o')
        if (Test-Path -LiteralPath $outputPath) { $manifest.responseSha256 = (Get-FileHash -LiteralPath $outputPath -Algorithm SHA256).Hash }
        $manifest | ConvertTo-Json | Set-Content -LiteralPath $manifestPath -Encoding utf8
    }
    Write-Output "Ticket $Ticket state=$($manifest.state); review $manifestPath and response.txt before integrating."
}
finally { $ticketLock.Dispose() }
