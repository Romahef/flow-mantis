# Quick API Test Script
Write-Host "=== SqlSyncService API Test ===" -ForegroundColor Cyan
Write-Host ""

# Check if service is running
Write-Host "[1/3] Checking service status..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:8088/" -Method GET -UseBasicParsing -ErrorAction Stop
    Write-Host "  ✓ Service is running" -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 401) {
        Write-Host "  ✓ Service is running (API key required)" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Service is not running" -ForegroundColor Red
        Write-Host "  Please start the service first" -ForegroundColor Yellow
        exit 1
    }
}

# Get API key from user or use test key
Write-Host "`n[2/3] API Key" -ForegroundColor Yellow
Write-Host "  To get your API key:" -ForegroundColor Cyan
Write-Host "    1. Open: admin-fixed\SqlSyncService.Admin.exe" -ForegroundColor White
Write-Host "    2. Go to Security tab" -ForegroundColor White
Write-Host "    3. View or generate API key" -ForegroundColor White
Write-Host ""
$apiKey = Read-Host "  Enter API key (or press Enter to skip)"

if ([string]::IsNullOrWhiteSpace($apiKey)) {
    Write-Host "`n  ⚠ No API key provided. Cannot test endpoints." -ForegroundColor Yellow
    Write-Host "  Service is running correctly, but endpoint testing requires an API key." -ForegroundColor White
    exit 0
}

# Test endpoints
Write-Host "`n[3/3] Testing Endpoints..." -ForegroundColor Yellow
$headers = @{ "X-API-Key" = $apiKey }

$endpoints = @(
    "LogisticSites",
    "Depositors",
    "Customers",
    "Items",
    "Stock",
    "DetailedStock",
    "Orders",
    "Receipts"
)

$successCount = 0
$failCount = 0

foreach ($endpoint in $endpoints) {
    Write-Host "`n  Testing: $endpoint" -ForegroundColor Cyan
    try {
        $url = "http://localhost:8088/$endpoint"
        $response = Invoke-RestMethod -Uri $url -Headers $headers -TimeoutSec 30 -ErrorAction Stop
        
        Write-Host "    ✓ Status: 200 OK" -ForegroundColor Green
        
        # Show first array property and count
        $firstProp = $response.PSObject.Properties | Where-Object { $_.Name -notlike "_*" } | Select-Object -First 1
        if ($firstProp) {
            $count = $firstProp.Value.Count
            Write-Host "    • Array: $($firstProp.Name)" -ForegroundColor White
            Write-Host "    • Records: $count" -ForegroundColor White
            
            if ($count -gt 0) {
                Write-Host "    • Sample fields: $($firstProp.Value[0].PSObject.Properties.Name -join ', ')" -ForegroundColor Gray
            }
        }
        
        $successCount++
    } catch {
        $failCount++
        if ($_.Exception.Response.StatusCode.value__ -eq 401) {
            Write-Host "    ✗ Invalid API Key" -ForegroundColor Red
        } elseif ($_.Exception.Message -like "*database*" -or $_.Exception.Message -like "*SQL*") {
            Write-Host "    ⚠ Database connection issue" -ForegroundColor Yellow
            Write-Host "      $($_.Exception.Message)" -ForegroundColor Gray
        } else {
            Write-Host "    ✗ Error: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# Summary
Write-Host "`n" -NoNewline
Write-Host "==================" -ForegroundColor Cyan
Write-Host "SUMMARY" -ForegroundColor Cyan
Write-Host "==================" -ForegroundColor Cyan
Write-Host "Total endpoints: $($endpoints.Count)" -ForegroundColor White
Write-Host "Successful: $successCount" -ForegroundColor Green
Write-Host "Failed: $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($successCount -gt 0) {
    Write-Host "✓ Service is working! JSON responses are being generated." -ForegroundColor Green
}

Write-Host "`nService is running at: http://localhost:8088" -ForegroundColor Cyan
Write-Host "Press any key to exit (service will continue running)..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
