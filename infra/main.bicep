@description('Name of the Azure Container Registry')
param acrName string = 'hackathonwearables'

@description('Name of the Container Apps Environment')
param containerEnvName string = 'hackathon-wearables-env'

@description('Name of the Container App')
param containerAppName string = 'hackathon-wearables-api'

@description('Azure region')
param location string = 'uksouth'

@description('SQL Server name (without .database.windows.net)')
param sqlServerName string = 'hackathon-wearables'

@description('SQL Database name')
param sqlDatabaseName string = 'hackathon-wearables'

@description('ACR admin password')
@secure()
param acrAdminPassword string

var sqlEndpoint = az.environment().suffixes.sqlServerHostname
var connectionString = 'Server=tcp:${sqlServerName}${sqlEndpoint},1433;Initial Catalog=${sqlDatabaseName};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: acrName
}

resource containerEnv 'Microsoft.App/managedEnvironments@2023-05-01' existing = {
  name: containerEnvName
}

resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: containerAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    environmentId: containerEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      registries: [
        {
          server: 'hackathonwearables.azurecr.io'
          username: acrName
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: acrAdminPassword
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: 'hackathonwearables.azurecr.io/health-api:latest'
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ConnectionStrings__HealthApiDb'
              value: connectionString
            }
            {
              name: 'Auth__Authority'
              value: 'https://your-auth-provider/.well-known/openid-configuration'
            }
            {
              name: 'Auth__Audience'
              value: 'your-api-audience'
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 3
      }
    }
  }
}

output containerAppUrl string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output containerAppPrincipalId string = containerApp.identity.principalId
