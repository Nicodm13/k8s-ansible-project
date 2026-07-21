param(
    [string]$BaseUrl = "https://taxsystem.kvikit.dk",
    [int]$Shards = 4,
    [switch]$SkipSeed
)

# Requestly / Postman-style collection runners execute requests and iterations
# sequentially on a single thread - there is no "parallel" toggle in the GUI runner.
# To generate real concurrent load against the system, this script shards the CSV
# data into $Shards files and launches that many Newman processes at the same time,
# each driving a slice of the rows through the stress collection concurrently.
#
# You can achieve the same effect in Requestly itself: run this script with
# -SkipSeed once seeding is done, note the shard files it writes under
# data\shards\, then import each shard CSV into its own Requestly runner tab and
# hit "Run" on all tabs at (roughly) the same time.

$ErrorActionPreference = "Stop"
$PackageRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$DataFile = Join-Path $PackageRoot "data\taxsystem-test-data.csv"
$SeedCollection = Join-Path $PackageRoot "collections\TaxSystem CSV Seed Test Data.postman_collection.json"
$StressCollection = Join-Path $PackageRoot "collections\TaxSystem CSV E2E Stress Test.postman_collection.json"
$ShardDir = Join-Path $PackageRoot "data\shards"

if (-not (Get-Command newman -ErrorAction SilentlyContinue)) {
    throw "Newman was not found. Install Node.js, then run: npm install -g newman"
}

if ($Shards -lt 1) {
    throw "-Shards must be at least 1"
}

$AllRows = Import-Csv -Path $DataFile
$Iterations = $AllRows.Count

if (-not $SkipSeed) {
    # Seeding must run once, sequentially, against the full data set so that
    # company/citizen creation is not raced across parallel workers.
    newman run $SeedCollection `
        --iteration-data $DataFile `
        --iteration-count $Iterations `
        --env-var "baseUrl=$BaseUrl"
}

# Split the rows round-robin across N shard CSVs, preserving column order.
if (Test-Path $ShardDir) {
    Remove-Item -Path $ShardDir -Recurse -Force
}
New-Item -ItemType Directory -Path $ShardDir | Out-Null

$Buckets = @()
for ($i = 0; $i -lt $Shards; $i++) {
    $Buckets += ,(New-Object System.Collections.Generic.List[object])
}
for ($i = 0; $i -lt $AllRows.Count; $i++) {
    $Buckets[$i % $Shards].Add($AllRows[$i])
}

$ShardFiles = @()
for ($i = 0; $i -lt $Shards; $i++) {
    if ($Buckets[$i].Count -eq 0) {
        continue
    }
    $ShardPath = Join-Path $ShardDir "shard-$i.csv"
    $Buckets[$i] | Export-Csv -Path $ShardPath -NoTypeInformation -Encoding UTF8
    $ShardFiles += $ShardPath
}

Write-Host "Launching $($ShardFiles.Count) parallel Newman workers against $BaseUrl ..."

$Jobs = foreach ($ShardPath in $ShardFiles) {
    $ShardCount = (Import-Csv -Path $ShardPath).Count
    Start-Job -ScriptBlock {
        param($Collection, $Data, $Count, $Url)
        & newman run $Collection --iteration-data $Data --iteration-count $Count --env-var "baseUrl=$Url" 2>&1
        [PSCustomObject]@{ ExitCode = $LASTEXITCODE }
    } -ArgumentList $StressCollection, $ShardPath, $ShardCount, $BaseUrl
}

$Jobs | Wait-Job | Out-Null

$FailedJobs = 0
foreach ($Job in $Jobs) {
    Write-Host "----- Worker job $($Job.Id) output -----"
    $Output = Receive-Job -Job $Job
    $Output | Where-Object { $_ -isnot [PSCustomObject] -or -not ($_.PSObject.Properties.Name -contains "ExitCode") } | Out-String | Write-Host
    $ExitInfo = $Output | Where-Object { $_ -is [PSCustomObject] -and $_.PSObject.Properties.Name -contains "ExitCode" } | Select-Object -Last 1
    if ($Job.State -ne "Completed" -or ($ExitInfo -and $ExitInfo.ExitCode -ne 0)) {
        $FailedJobs++
    }
}
$Jobs | Remove-Job

if ($FailedJobs -gt 0) {
    throw "$FailedJobs of $($Jobs.Count) parallel Newman workers did not complete successfully."
}

Write-Host "All parallel Newman workers finished."


