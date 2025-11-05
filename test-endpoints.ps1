Write-Host "=== Testing SqlSyncService ===" -ForegroundColor Cyan

# Test service
Write-Host "`nChecking if service is running..." -ForegroundColor Yellow
$serviceRunning = $false
try {
    Invoke-WebRequest -Uri "http://localhost:8088/" -UseBasicParsing -ErrorAction Stop | Out-Null
} catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 401) {
        $serviceRunning = $true
    }
}

if ($serviceRunning) {
    Write-Host "✓ Service is RUNNING at http://localhost:8088" -ForegroundColor Green
} else {
    Write-Host "✗ Service is NOT running" -ForegroundColor Red
    exit
}

# Get API key
Write-Host "`nEnter API key from Admin app (Security tab):" -ForegroundColor Cyan
$apiKey = Read-Host "API Key"

if ([string]::IsNullOrWhiteSpace($apiKey)) {
    Write-Host "No API key provided. Exiting." -ForegroundColor Yellow
    exit
}

# Test each endpoint
Write-Host "`nTesting endpoints..." -ForegroundColor Cyan
$headers = @{ "X-API-Key" = $apiKey }
$endpoints = @("LogisticSites", "Depositors", "Customers", "Items", "Stock", "DetailedStock", "Orders", "Receipts")
$success = 0

foreach ($ep in $endpoints) {
    Write-Host "  $ep... " -NoNewline
    try {
        $r = Invoke-RestMethod -Uri "http://localhost:8088/$ep" -Headers $headers -TimeoutSec 10
        Write-Host "✓" -ForegroundColor Green
        $success++
    } catch {
        Write-Host "✗" -ForegroundColor Red
    }
}

Write-Host "`n$success / $($endpoints.Count) endpoints working!" -ForegroundColor $(if ($success -eq $endpoints.Count) { "Green" } else { "Yellow" })

