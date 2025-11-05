# Test Queries Script - Run as Administrator
Write-Host "=== SqlSyncService Query Testing ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check service status
Write-Host "[1/5] Checking service status..." -ForegroundColor Yellow
$service = Get-Service -Name "SqlSyncService" -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "  Service Status: $($service.Status)" -ForegroundColor White
    
    if ($service.Status -ne "Running") {
        Write-Host "  Starting service..." -ForegroundColor Yellow
        try {
            Start-Service -Name "SqlSyncService" -ErrorAction Stop
            Start-Sleep -Seconds 5
            Write-Host "  ✓ Service started successfully" -ForegroundColor Green
        }
        catch {
            Write-Host "  ✗ Failed to start service: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "  Please run this script as Administrator" -ForegroundColor Yellow
            exit 1
        }
    } else {
        Write-Host "  ✓ Service is already running" -ForegroundColor Green
    }
} else {
    Write-Host "  ✗ Service not installed" -ForegroundColor Red
    exit 1
}

# Step 2: Read configuration
Write-Host "`n[2/5] Reading configuration..." -ForegroundColor Yellow
$configDir = "C:\ProgramData\SqlSyncService"
$appSettings = Get-Content "$configDir\appsettings.json" | ConvertFrom-Json
$queries = Get-Content "$configDir\queries.json" | ConvertFrom-Json
$mapping = Get-Content "$configDir\mapping.json" | ConvertFrom-Json

$port = ([System.Uri]$appSettings.Service.ListenUrl).Port
Write-Host "  API Port: $port" -ForegroundColor White
Write-Host "  Queries configured: $($queries.Queries.Count)" -ForegroundColor White
Write-Host "  Endpoints configured: $($mapping.Routes.Count)" -ForegroundColor White

# Step 3: Wait for service to be ready
Write-Host "`n[3/5] Waiting for service to be ready..." -ForegroundColor Yellow
$maxAttempts = 10
$attempt = 0
$serviceReady = $false

while ($attempt -lt $maxAttempts -and -not $serviceReady) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$port/" -Method GET -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        $serviceReady = $true
        Write-Host "  ✓ Service is ready" -ForegroundColor Green
    }
    catch {
        $attempt++
        Write-Host "  Waiting... (attempt $attempt/$maxAttempts)" -ForegroundColor Gray
        Start-Sleep -Seconds 1
    }
}

if (-not $serviceReady) {
    Write-Host "  ✗ Service did not become ready" -ForegroundColor Red
    Write-Host "  Check logs at: $configDir\logs" -ForegroundColor Yellow
    exit 1
}

# Step 4: Test API endpoints
Write-Host "`n[4/5] Testing API endpoints..." -ForegroundColor Yellow

# Get API key (if required)
$headers = @{}
if ($appSettings.Security.RequireApiKey) {
    Write-Host "  Note: API key authentication is enabled" -ForegroundColor Yellow
    Write-Host "  You'll need to provide the API key to test endpoints" -ForegroundColor Yellow
    $apiKey = Read-Host "  Enter API key (or press Enter to skip endpoint tests)"
    
    if (-not [string]::IsNullOrWhiteSpace($apiKey)) {
        $headers = @{
            "X-API-Key" = $apiKey
        }
    }
}

# Test each endpoint
$testResults = @()

foreach ($route in $mapping.Routes) {
    $endpoint = $route.Endpoint
    $url = "http://localhost:$port/$endpoint"
    
    Write-Host "`n  Testing: $endpoint" -ForegroundColor Cyan
    Write-Host "  URL: $url" -ForegroundColor Gray
    
    try {
        if ($headers.Count -gt 0) {
            $response = Invoke-WebRequest -Uri $url -Method GET -Headers $headers -UseBasicParsing -TimeoutSec 30
        } else {
            $response = Invoke-WebRequest -Uri $url -Method GET -UseBasicParsing -TimeoutSec 30
        }
        
        $statusCode = $response.StatusCode
        $contentLength = $response.Content.Length
        
        Write-Host "  Status: $statusCode" -ForegroundColor Green
        Write-Host "  Content Length: $contentLength bytes" -ForegroundColor White
        
        # Parse JSON
        try {
            $json = $response.Content | ConvertFrom-Json
            Write-Host "  ✓ Valid JSON response" -ForegroundColor Green
            
            # Check for data arrays
            $queries = $route.Queries
            foreach ($query in $queries) {
                $arrayName = $query.TargetArray
                if ($json.PSObject.Properties.Name -contains $arrayName) {
                    $count = $json.$arrayName.Count
                    Write-Host "    • $arrayName: $count records" -ForegroundColor White
                } else {
                    Write-Host "    • $arrayName: not found in response" -ForegroundColor Yellow
                }
            }
            
            $testResults += @{
                Endpoint = $endpoint
                Status = "Success"
                StatusCode = $statusCode
                Size = $contentLength
            }
        }
        catch {
            Write-Host "  ✗ Invalid JSON response" -ForegroundColor Red
            $testResults += @{
                Endpoint = $endpoint
                Status = "Invalid JSON"
                StatusCode = $statusCode
                Size = $contentLength
            }
        }
    }
    catch {
        $errorMessage = $_.Exception.Message
        Write-Host "  ✗ Error: $errorMessage" -ForegroundColor Red
        
        $testResults += @{
            Endpoint = $endpoint
            Status = "Failed"
            Error = $errorMessage
        }
    }
}

# Step 5: Summary
Write-Host "`n[5/5] Test Summary" -ForegroundColor Yellow
Write-Host "==================" -ForegroundColor Yellow

$successCount = ($testResults | Where-Object { $_.Status -eq "Success" }).Count
$totalCount = $testResults.Count

Write-Host "Total Endpoints: $totalCount" -ForegroundColor White
Write-Host "Successful: $successCount" -ForegroundColor Green
Write-Host "Failed: $($totalCount - $successCount)" -ForegroundColor Red

Write-Host "`n✓ Testing complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Check logs at: $configDir\logs" -ForegroundColor White
Write-Host "2. Use Admin UI to view/edit queries" -ForegroundColor White
Write-Host "3. Test with your actual client application" -ForegroundColor White

