// ============================================================
// Smart Home Simulator — Azure Infrastructure (Bicep)
// ============================================================
// Resources created:
//   1. Log Analytics Workspace
//   2. Container Apps Environment
//   3. Azure SQL Server + Database
//   4. Container App: Mosquitto (internal TCP, port 1883)
//   5. Container App: API (external HTTP, port 8080)
//   6. Container App: Simulator (no ingress)
//   7. Static Web App (frontend)
// ============================================================

// --- Parameters ---

@description('Azure region for all resources')
param location string = 'centralus'

@description('Prefix used for naming all resources (must be globally unique for ACR/SQL)')
param environmentName string = 'smarthome'

@description('Name of the existing Azure Container Registry')
param acrName string

@description('SQL Server administrator login')
param sqlAdminLogin string

@description('SQL Server administrator password (min 8 chars, uppercase, lowercase, number, special char)')
@secure()
param sqlAdminPassword string

// --- Derived Names ---

var logAnalyticsName = '${environmentName}-logs'
var containerEnvName = '${environmentName}-env'
var sqlServerName = '${environmentName}-sql'
var sqlDbName = '${environmentName}-db'
var mqttAppName = '${environmentName}-mqtt'
var apiAppName = '${environmentName}-api'
var simulatorAppName = '${environmentName}-simulator'
var swaName = '${environmentName}-frontend'

// --- Reference to existing ACR (created by deploy script before Bicep) ---

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: acrName
}

// ============================================================
// 1. Log Analytics Workspace
// ============================================================

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// ============================================================
// 2. Container Apps Environment
// ============================================================

resource containerEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerEnvName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// ============================================================
// 3. Azure SQL Server + Database
// ============================================================

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDbName
  location: location
  sku: {
    name: 'S0'
    tier: 'Standard'
    capacity: 10
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
  }
}

// Allow Azure services (Container Apps) to access SQL Server
resource sqlFirewall 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ============================================================
// 4. Mosquitto Container App (Internal TCP on port 1883)
// ============================================================

resource mosquitto 'Microsoft.App/containerApps@2024-03-01' = {
  name: mqttAppName
  location: location
  properties: {
    managedEnvironmentId: containerEnv.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 1883
        transport: 'tcp'
        exposedPort: 1883
      }
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: acr.listCredentials().passwords[0].value
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'mosquitto'
          image: '${acr.properties.loginServer}/${mqttAppName}:latest'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

// ============================================================
// 5. Static Web App (Frontend) — created before API so
//    the API can reference its hostname for CORS
// ============================================================

resource swa 'Microsoft.Web/staticSites@2022-09-01' = {
  name: swaName
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {}
}

// ============================================================
// 6. API Container App (External HTTP on port 8080)
// ============================================================

var sqlConnectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabase.name};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

resource api 'Microsoft.App/containerApps@2024-03-01' = {
  name: apiAppName
  location: location
  properties: {
    managedEnvironmentId: containerEnv.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        allowInsecure: false
      }
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: acr.listCredentials().passwords[0].value
        }
        {
          name: 'sql-connection-string'
          value: sqlConnectionString
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: '${acr.properties.loginServer}/${apiAppName}:latest'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
              value: 'true'
            }
            {
              name: 'ConnectionStrings__DefaultConnection'
              secretRef: 'sql-connection-string'
            }
            {
              name: 'MqttSettings__Host'
              value: mqttAppName
            }
            {
              name: 'MqttSettings__Port'
              value: '1883'
            }
            {
              name: 'AllowedOrigins__0'
              value: 'https://${swa.properties.defaultHostname}'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

// ============================================================
// 7. Simulator Container App (No external ingress)
// ============================================================

resource simulator 'Microsoft.App/containerApps@2024-03-01' = {
  name: simulatorAppName
  location: location
  properties: {
    managedEnvironmentId: containerEnv.id
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: acr.listCredentials().passwords[0].value
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'simulator'
          image: '${acr.properties.loginServer}/${simulatorAppName}:latest'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'API_URL'
              value: 'https://${api.properties.configuration.ingress.fqdn}/api/devices/all-system'
            }
            {
              name: 'BROKER_HOST'
              value: mqttAppName
            }
            {
              name: 'BROKER_PORT'
              value: '1883'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

// ============================================================
// Outputs
// ============================================================

output acrLoginServer string = acr.properties.loginServer
output apiUrl string = 'https://${api.properties.configuration.ingress.fqdn}'
output apiInternalHost string = apiAppName
output mqttInternalHost string = mqttAppName
output staticWebAppHostname string = swa.properties.defaultHostname
output staticWebAppName string = swa.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
