# ============================================================
# Smart Home Simulator - Local CI/CD Deployment Script
# ============================================================

$ErrorActionPreference = "Stop"

# 1. Fetch configuration from GitHub Variables
Write-Host "Fetching configuration from GitHub Variables..." -ForegroundColor Cyan
$RG = gh variable get RESOURCE_GROUP
$LOC = gh variable get LOCATION
$ENV = gh variable get ENVIRONMENT_NAME
$ACR = gh variable get ACR_NAME

if ([string]::IsNullOrWhiteSpace($ACR)) {
    Write-Host "ERROR: ACR_NAME is empty. Please verify your GitHub variables." -ForegroundColor Red
    exit 1
}

# 2. Fetch secrets locally (GitHub prevents reading secrets via CLI)
Write-Host "Loading secrets from local .env file..." -ForegroundColor Cyan
if (-not (Test-Path ".env")) {
    Write-Host "File .env not found! Creating a template..." -ForegroundColor Yellow
    "SQL_ADMIN_LOGIN=adminuser`nSQL_ADMIN_PASSWORD=StrongPassword123!" | Out-File ".env" -Encoding utf8
    Write-Host "Created .env file. Please update it with your real passwords and run the script again." -ForegroundColor Red
    exit 1
}

# Load .env into PowerShell variables
Get-Content ".env" | Where-Object { $_ -match "^[^#\s]+=" } | ForEach-Object {
    $name, $value = $_.Split('=', 2)
    Set-Variable -Name $name.Trim() -Value $value.Trim() -Scope Script
}

$SQL_USER = $SQL_ADMIN_LOGIN
$SQL_PASS = $SQL_ADMIN_PASSWORD

if ([string]::IsNullOrWhiteSpace($SQL_PASS)) {
    Write-Host "ERROR: SQL_ADMIN_PASSWORD is empty in .env file." -ForegroundColor Red
    exit 1
}

# 3. Login to Azure Container Registry
Write-Host "Logging into Azure Container Registry ($ACR)..." -ForegroundColor Cyan
az acr login --name $ACR

# 4. Build and push Docker images
Write-Host "Building and pushing Docker images..." -ForegroundColor Yellow

# Mosquitto MQTT
Write-Host "Processing Mosquitto MQTT..." -ForegroundColor DarkGray
docker build -t "$ACR.azurecr.io/$ENV-mqtt:latest" -f Dockerfile.mosquitto .
docker push "$ACR.azurecr.io/$ENV-mqtt:latest"

# Smart Home API
Write-Host "Processing API..." -ForegroundColor DarkGray
docker build -t "$ACR.azurecr.io/$ENV-api:latest" -f backend/src/SmartHome.Api/Dockerfile backend/src/
docker push "$ACR.azurecr.io/$ENV-api:latest"

# Simulator
Write-Host "Processing Simulator..." -ForegroundColor DarkGray
docker build -t "$ACR.azurecr.io/$ENV-simulator:latest" -f backend/src/SmartHome.Simulator/Dockerfile backend/src/
docker push "$ACR.azurecr.io/$ENV-simulator:latest"

# 5. Deploy Azure Infrastructure (Bicep)
Write-Host "Deploying Azure Infrastructure (Bicep)..." -ForegroundColor Green
az deployment group create `
    --resource-group $RG `
    --template-file infra/main.bicep `
    --parameters environmentName=$ENV acrName=$ACR sqlAdminLogin=$SQL_USER sqlAdminPassword=$SQL_PASS location=$LOC

# 6. Fetch dynamic API URL for Frontend
Write-Host "Fetching API URL for frontend configuration..." -ForegroundColor Cyan
$ApiUrl = az containerapp show --name "$ENV-api" --resource-group $RG --query "properties.configuration.ingress.fqdn" -o tsv
$env:VITE_API_URL = "https://$ApiUrl/api"

# 7. Build Frontend (React)
Write-Host "Building frontend application..." -ForegroundColor Yellow
Push-Location frontend
npm ci
npm run build
Pop-Location

# 8. Fetch SWA Token
Write-Host "Fetching deployment token for Static Web App..." -ForegroundColor Cyan
$SwaName = "$ENV-frontend"
$SwaToken = az staticwebapp secrets list --name $SwaName --resource-group $RG --query "properties.apiKey" -o tsv

# 9. Deploy Frontend
Write-Host "Deploying frontend to Azure Static Web Apps..." -ForegroundColor Green
npx --yes @azure/static-web-apps-cli@latest deploy ./frontend/dist --deployment-token $SwaToken --env production

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Deployment completed successfully!" -ForegroundColor Green
Write-Host "Frontend URL: https://$(az staticwebapp show -n $SwaName -g $RG --query 'defaultHostname' -o tsv)" -ForegroundColor White
Write-Host "============================================================" -ForegroundColor Cyan