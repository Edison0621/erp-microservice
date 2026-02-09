param (
    [Parameter(Mandatory=$true)]
    [string]$ServiceName
)

$services = @{
    "identity" = @{ "port" = 5001; "path" = "src/Services/Identity/ErpSystem.Identity" }
    "masterdata" = @{ "port" = 5002; "path" = "src/Services/MasterData/ErpSystem.MasterData" }
    "procurement" = @{ "port" = 5003; "path" = "src/Services/Procurement/ErpSystem.Procurement" }
    "finance" = @{ "port" = 5004; "path" = "src/Services/Finance/ErpSystem.Finance" }
    "inventory" = @{ "port" = 5005; "path" = "src/Services/Inventory/ErpSystem.Inventory" }
    "sales" = @{ "port" = 5006; "path" = "src/Services/Sales/ErpSystem.Sales" }
    "production" = @{ "port" = 5007; "path" = "src/Services/Production/ErpSystem.Production" }
    "hr" = @{ "port" = 5008; "path" = "src/Services/HR/ErpSystem.HR" }
    "crm" = @{ "port" = 5009; "path" = "src/Services/CRM/ErpSystem.CRM" }
    "analytics" = @{ "port" = 5010; "path" = "src/Services/Analytics/ErpSystem.Analytics" }
}

$service = $services[$ServiceName.ToLower()]

if ($null -eq $service) {
    Write-Error "Service '$ServiceName' not found. Available services: $($services.Keys -join ', ')"
    exit 1
}

$appId = "$($ServiceName.ToLower())-api"
$appPort = $service.port
$projectPath = $service.path
$componentsPath = "./components"

Write-Host "Starting Dapr for $appId on port $appPort..." -ForegroundColor Cyan

dapr run --app-id $appId `
         --app-port $appPort `
         --dapr-http-port (3500 + ($appPort % 100)) `
         --components-path $componentsPath `
         -- dotnet run --project $projectPath
