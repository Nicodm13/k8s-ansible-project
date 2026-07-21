param(
    [string]$BaseUrl = "https://taxsystem.kvikit.dk",
    [switch]$SkipStress
)

$ErrorActionPreference = "Stop"
$PackageRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$DataFile = Join-Path $PackageRoot "data\taxsystem-test-data.csv"
$SeedCollection = Join-Path $PackageRoot "collections\TaxSystem CSV Seed Test Data.postman_collection.json"
$StressCollection = Join-Path $PackageRoot "collections\TaxSystem CSV E2E Stress Test.postman_collection.json"

if (-not (Get-Command newman -ErrorAction SilentlyContinue)) {
    throw "Newman was not found. Install Node.js, then run: npm install -g newman"
}

$Iterations = (Import-Csv -Path $DataFile).Count

newman run $SeedCollection `
    --iteration-data $DataFile `
    --iteration-count $Iterations `
    --env-var "baseUrl=$BaseUrl"

if (-not $SkipStress) {
    newman run $StressCollection `
        --iteration-data $DataFile `
        --iteration-count $Iterations `
        --env-var "baseUrl=$BaseUrl"
}
