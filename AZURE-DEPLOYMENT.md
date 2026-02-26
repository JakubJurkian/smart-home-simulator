# Azure Deployment Guide

Complete guide for deploying the Smart Home Simulator to Microsoft Azure.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Azure Resource Group                        │
│                                                                     │
│  ┌──────────────────┐     ┌───────────────────────────────────────┐ │
│  │  Static Web App   │     │    Container Apps Environment         │ │
│  │  (Frontend)       │     │                                       │ │
│  │  React + Vite     │────▶│  ┌─────────────┐  ┌───────────────┐  │ │
│  │                   │     │  │  API         │  │  Mosquitto    │  │ │
│  └──────────────────┘     │  │  Container   │◀─│  MQTT Broker  │  │ │
│                            │  │  App (.NET)  │  │  (TCP:1883)   │  │ │
│  ┌──────────────────┐     │  │  (HTTP:8080) │  └───────┬───────┘  │ │
│  │  Azure SQL        │     │  └──────┬──────┘          │          │ │
│  │  Database         │◀────│─────────┘     ┌───────────┘          │ │
│  │  (Basic tier)     │     │               │                      │ │
│  └──────────────────┘     │  ┌────────────▼────────┐              │ │
│                            │  │  Simulator           │              │ │
│  ┌──────────────────┐     │  │  Container App       │              │ │
│  │  Container        │     │  │  (Console, no ingress)│             │ │
│  │  Registry (ACR)   │     │  └─────────────────────┘              │ │
│  └──────────────────┘     └───────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

| Azure Resource | Purpose | Maps to Docker Compose |
|---|---|---|
| **Azure Container App** | Backend .NET API (port 8080) | `api` service |
| **Azure Container App** | Mosquitto MQTT broker (internal TCP:1883) | `mqtt` service |
| **Azure Container App** | Temperature simulator (no ingress) | `simulator` service |
| **Azure Static Web App** | React frontend (SPA) | `frontend` service |
| **Azure SQL Database** | SQL Server (replaces SQLite) | SQLite file |
| **Azure Container Registry** | Stores Docker images | Local Docker images |
| **Log Analytics Workspace** | Centralized logging | Docker logs |

---

## Prerequisites

- **Azure CLI** — [Install](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
- **Node.js 23+** and **npm** — [Install](https://nodejs.org/)
- **Azure subscription** — [Free tier](https://azure.microsoft.com/free/)

> **Docker is NOT required** — images are built in the cloud using ACR Tasks.

---

## Quick Deploy (PowerShell Script)

### 1. Login to Azure

```powershell
az login
```

### 2. Run the deployment script

```powershell
.\deploy.ps1 -SqlAdminLogin "youradmin" -SqlAdminPassword "YourP@ssw0rd!"
```

Optional parameters:

```powershell
.\deploy.ps1 `
    -SqlAdminLogin "myadmin" `
    -SqlAdminPassword "MyStr0ng!Pass" `
    -ResourceGroup "smarthome-rg" `
    -Location "westeurope" `
    -EnvironmentName "smarthome"
```

> **SQL Password Requirements**: Minimum 8 characters. Must include uppercase, lowercase, number, and special character.

The script will:
1. Create the Azure Resource Group
2. Create Azure Container Registry
3. Build & push all Docker images (Mosquitto, API, Simulator)
4. Deploy all Azure infrastructure via Bicep (SQL, Container Apps, Static Web App)
5. Build the frontend with the correct API URL
6. Deploy the frontend to Azure Static Web App

---

## Manual Deploy (Step by Step)

### 1. Set variables

```powershell
$ResourceGroup = "smarthome-rg"
$Location = "westeurope"
$EnvironmentName = "smarthome"
$AcrName = "smarthomeacr"
$SqlLogin = "adminsql"
$SqlPassword = "StrongP@ssw0rd!"
```

### 2. Create Resource Group & ACR

```powershell
az group create --name $ResourceGroup --location $Location
az acr create --resource-group $ResourceGroup --name $AcrName --sku Basic --admin-enabled true
```

### 3. Build & push images

```powershell
# Mosquitto (from project root)
az acr build --registry $AcrName --image "$EnvironmentName-mqtt:latest" --file Dockerfile.mosquitto .

# API (context = backend/src/)
az acr build --registry $AcrName --image "$EnvironmentName-api:latest" --file backend/src/SmartHome.Api/Dockerfile backend/src/

# Simulator (context = backend/src/)
az acr build --registry $AcrName --image "$EnvironmentName-simulator:latest" --file backend/src/SmartHome.Simulator/Dockerfile backend/src/
```

### 4. Deploy infrastructure

```powershell
az deployment group create `
    --resource-group $ResourceGroup `
    --template-file infra/main.bicep `
    --parameters environmentName=$EnvironmentName acrName=$AcrName sqlAdminLogin=$SqlLogin sqlAdminPassword=$SqlPassword
```

### 5. Build & deploy frontend

```powershell
# Get the API URL from the deployment
$ApiUrl = az containerapp show --name "$EnvironmentName-api" --resource-group $ResourceGroup --query "properties.configuration.ingress.fqdn" -o tsv

# Build frontend
cd frontend
$env:VITE_API_URL = "https://$ApiUrl/api"
npm ci
npm run build
cd ..

# Deploy to Static Web App
$SwaName = "$EnvironmentName-frontend"
$Token = az staticwebapp secrets list --name $SwaName --resource-group $ResourceGroup --query "properties.apiKey" -o tsv
npx --yes @azure/static-web-apps-cli@latest deploy ./frontend/dist --deployment-token $Token --env production
```

---

## CI/CD with GitHub Actions

### Setup

1. **Create a Service Principal:**

   ```bash
   az ad sp create-for-rbac --name "smarthome-cicd" --role contributor \
       --scopes /subscriptions/{subscription-id}/resourceGroups/smarthome-rg \
       --sdk-auth
   ```

2. **Add GitHub Secrets** (Settings → Secrets and variables → Actions):

   | Secret | Value |
   |---|---|
   | `AZURE_CREDENTIALS` | JSON output from the service principal command |
   | `SQL_ADMIN_LOGIN` | SQL admin username (e.g., `smarthomeadmin`) |
   | `SQL_ADMIN_PASSWORD` | SQL admin password |

3. **Add GitHub Variables** (optional — defaults shown):

   | Variable | Default |
   |---|---|
   | `RESOURCE_GROUP` | `smarthome-rg` |
   | `LOCATION` | `westeurope` |
   | `ENVIRONMENT_NAME` | `smarthome` |

4. **Push to `main`** — the workflow at `.github/workflows/azure-deploy.yml` runs automatically.

---

## How It Works in Azure

### Database

- **Production** uses **Azure SQL Server** (not SQLite)
- The API's `Program.cs` detects `ASPNETCORE_ENVIRONMENT=Production` and registers `UseSqlServer()`
- EF Core migrations run automatically on startup via `Database.Migrate()`

### MQTT Communication

- Mosquitto runs as an **internal** Container App (not exposed to the internet)
- The API and Simulator connect via the internal DNS name within the Container Apps Environment
- Frontend does **NOT** connect directly to MQTT — it receives real-time updates via **SignalR**

### SignalR

- The API hosts a SignalR hub at `/smarthomehub`
- The frontend connects to `https://<api-fqdn>/smarthomehub`
- Real-time events: `RefreshDevices`, `ReceiveTemperature`

### Authentication

- Cookie-based authentication with `SameSite=None` and `SecurePolicy=Always`
- Cookies work cross-origin between the Static Web App and the API Container App
- `AllowedOrigins` CORS is automatically configured with the Static Web App's hostname

---

## Estimated Azure Costs

| Resource | SKU | Estimated Cost/Month |
|---|---|---|
| Container App (API) | 0.5 vCPU, 1 GiB | ~$15–$35 |
| Container App (Mosquitto) | 0.25 vCPU, 0.5 GiB | ~$7–$15 |
| Container App (Simulator) | 0.25 vCPU, 0.5 GiB | ~$7–$15 |
| Azure SQL Database | Basic (5 DTU) | ~$5 |
| Static Web App | Free | $0 |
| Container Registry | Basic | ~$5 |
| Log Analytics | Per GB | ~$2–$5 |
| **Total** | | **~$40–$80/month** |

> Use the [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/) for accurate estimates.

---

## Teardown

Remove all Azure resources:

```powershell
az group delete --name smarthome-rg --yes --no-wait
```

---

## Troubleshooting

### View Container App logs

```powershell
az containerapp logs show --name smarthome-api --resource-group smarthome-rg --follow
az containerapp logs show --name smarthome-mqtt --resource-group smarthome-rg --follow
az containerapp logs show --name smarthome-simulator --resource-group smarthome-rg --follow
```

### Restart a Container App

```powershell
az containerapp revision restart --name smarthome-api --resource-group smarthome-rg --revision <revision-name>
```

### Check Container App status

```powershell
az containerapp show --name smarthome-api --resource-group smarthome-rg --query "properties.runningStatus"
```

### Static Web App location

Azure Static Web Apps are available in limited regions: `westus2`, `centralus`, `eastus2`, `westeurope`, `eastasia`. If deployment fails due to location, set `$Location` to one of these regions.

### CORS issues

If the frontend can't reach the API, verify the `AllowedOrigins` environment variable on the API Container App matches the Static Web App hostname:

```powershell
az containerapp show --name smarthome-api --resource-group smarthome-rg --query "properties.template.containers[0].env"
```

### Database connection issues

Test SQL connectivity from a local machine:

```powershell
# Add your IP to the SQL Server firewall
az sql server firewall-rule create --resource-group smarthome-rg --server smarthome-sql --name MyIP --start-ip-address <your-ip> --end-ip-address <your-ip>
```
