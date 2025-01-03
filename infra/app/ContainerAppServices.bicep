metadata description = 'Creates a container app in an Azure Container App environment.'
param name string
param location string = resourceGroup().location
param tags object = {}
param storageAccountName string

@description('Indicates whether admin user is enabled')
param adminUserEnabled bool = false

@description('Indicates whether anonymous pull is enabled')
param anonymousPullEnabled bool = false

@description('Azure ad authentication as arm policy settings')
param azureADAuthenticationAsArmPolicy object = {
  status: 'enabled'
}

@description('Indicates whether data endpoint is enabled')
param dataEndpointEnabled bool = false

@description('Encryption settings')
param encryption object = {
  status: 'disabled'
}

@description('Export policy settings')
param exportPolicy object = {
  status: 'enabled'
}

@description('The log analytics workspace ID used for logging and monitoring')
param workspaceId string = ''

@description('Metadata search settings')
param metadataSearch string = 'Disabled'

@description('Options for bypassing network rules')
param networkRuleBypassOptions string = 'AzureServices'

@description('Public network access setting')
param publicNetworkAccess string = 'Enabled'

@description('Quarantine policy settings')
param quarantinePolicy object = {
  status: 'disabled'
}

@description('Retention policy settings')
param retentionPolicy object = {
  days: 7
  status: 'disabled'
}

@description('Scope maps setting')
param scopeMaps array = []

@description('SKU settings')
param sku object = {
  name: 'Basic'
}

@description('Soft delete policy settings')
param softDeletePolicy object = {
  retentionDays: 7
  status: 'disabled'
}

@description('Trust policy settings')
param trustPolicy object = {
  type: 'Notary'
  status: 'disabled'
}

@description('Zone redundancy setting')
param zoneRedundancy string = 'Disabled'

@description('Allowed origins')
param allowedOrigins array = []

@description('Name of the environment for container apps')
param containerAppsEnvironmentName string

@description('CPU cores allocated to a single container instance, e.g., 0.5')
param containerCpuCoreCount string = '0.5'

@description('The maximum number of replicas to run. Must be at least 1.')
@minValue(1)
param containerMaxReplicas int = 10

@description('Memory allocated to a single container instance, e.g., 1Gi')
param containerMemory string = '1.0Gi'

@description('The minimum number of replicas to run. Must be at least 1.')
param containerMinReplicas int = 0

@description('The name of the container')
param containerName string = 'main'

@description('The name of the container registry')
param containerRegistryName string = ''

@description('Hostname suffix for container registry. Set when deploying to sovereign clouds')
param containerRegistryHostSuffix string = 'azurecr.io'

@description('The protocol used by Dapr to connect to the app, e.g., http or grpc')
@allowed([ 'http', 'grpc' ])
param daprAppProtocol string = 'http'

@description('The Dapr app ID')
param daprAppId string = containerName

@description('Enable Dapr')
param daprEnabled bool = false

@description('The environment variables for the container')
param env array = []

@description('Specifies if the resource ingress is exposed externally')
param external bool = true

@description('The name of the user-assigned identity')
param identityName string = ''

@description('The type of identity for the resource')
@allowed([ 'None', 'SystemAssigned', 'UserAssigned' ])
param identityType string = 'None'

@description('The name of the container image')
param imageName string = ''

@description('Specifies if Ingress is enabled for the container app')
param ingressEnabled bool = true

param revisionMode string = 'Single'

@description('The secrets required for the container')
@secure()
param secrets object = {}

@description('The service binds associated with the container')
param serviceBinds array = []

@description('The name of the container apps add-on to use. e.g. redis')
param serviceType string = ''

@description('The target port for the container')
param targetPort int = 8080

@description('Log Analytics workspace')
param logAnalyticsName string

param services array = []

@description('The name of the Azure SQL Server.')
param sqlServerName string

@description('The admin username for the SQL Server.')
param sqlAdminUsername string

@secure()
@description('The admin password for the SQL Server.')
param sqlAdminPassword string

@description('The minimum TLS version for the SQL Server.')
param sqlMinTLSVersion string = '1.2'

param cosmos_name string

param applicationInsightsName string

param apimTier string = 'Consumption'
param apimCapacity int = 0
param apimName string

resource userIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = if (!empty(identityName)) {
  name: identityName
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2021-12-01-preview' existing = {
  name: logAnalyticsName
}

var usePrivateRegistry = !empty(identityName) && !empty(containerRegistryName)

var normalizedIdentityType = !empty(identityName) ? 'UserAssigned' : identityType

var acrPullRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')

resource aksAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    scope: containerRegistry    
    name: guid(subscription().id, resourceGroup().id, userIdentity.name, acrPullRole)
    properties: {
      roleDefinitionId: acrPullRole
      principalType: 'ServicePrincipal'
      principalId: userIdentity.properties.principalId
    }
  }

  resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
    name: applicationInsightsName
  }

  resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
    name: containerRegistryName
    location: location
    tags: tags
    sku: sku
    properties: {
      adminUserEnabled: adminUserEnabled
      anonymousPullEnabled: anonymousPullEnabled
      dataEndpointEnabled: dataEndpointEnabled
      encryption: encryption
      metadataSearch: metadataSearch
      networkRuleBypassOptions: networkRuleBypassOptions
      policies:{
        quarantinePolicy: quarantinePolicy
        trustPolicy: trustPolicy
        retentionPolicy: retentionPolicy
        exportPolicy: exportPolicy
        azureADAuthenticationAsArmPolicy: azureADAuthenticationAsArmPolicy
        softDeletePolicy: softDeletePolicy
      }
      publicNetworkAccess: publicNetworkAccess
      zoneRedundancy: zoneRedundancy
    }
  }

resource app 'Microsoft.App/containerApps@2023-05-02-preview' = [for service in services: {
  name: '${service.name}'
  location: location
  tags: union(tags, {'azd-service-name':  '${service.deploymentName}' })
  dependsOn: [
    aksAcrPull
    sqlServer
    cosmosDBAccount
  ]
  identity: {
    type: normalizedIdentityType
    userAssignedIdentities: !empty(identityName) && normalizedIdentityType == 'UserAssigned' ? { '${userIdentity.id}': {} } : null
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: revisionMode
      ingress: ingressEnabled ? {
        external: external
        targetPort: targetPort
        transport: 'auto'
        corsPolicy: {
          allowedOrigins: union([ 'https://portal.azure.com', 'https://ms.portal.azure.com' ], allowedOrigins)
        }
      } : null
      dapr: daprEnabled ? {
        enabled: true
        appId: daprAppId
        appProtocol: daprAppProtocol
        appPort: ingressEnabled ? targetPort : 0
      } : { enabled: false }
      secrets: [for secret in items(secrets): {
        name: secret.key
        value: secret.value
      }]
      service: !empty(serviceType) ? { type: serviceType } : null
      registries: usePrivateRegistry ? [
        {
          server: '${containerRegistryName}.${containerRegistryHostSuffix}'
          identity: userIdentity.id
        }
      ] : []
    }
    template: {
      serviceBinds: !empty(serviceBinds) ? serviceBinds : null
      containers: [
        {
          image: !empty(imageName) ? imageName : 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          name: service.name
          env: union(
            env,
            contains(['FlightService', 'UserService'], service.deploymentName) ? [
              {
                name: 'DbConnString'
                value: 'Server=${sqlServerName}.database.windows.net;Database=${service.sqlDatabaseName};User ID=${sqlAdminUsername};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
              }
            ] : [],
            contains(['WeatherService', 'BookingService'], service.deploymentName) ? [
              {
                name: 'TenantId'
                value: subscription().tenantId
              }
              {
                name: 'DatabaseId'
                value: service.deploymentName
              }
              {
                name: 'CosmosDBAccountEndpoint'
                value: cosmosDBAccount.properties.documentEndpoint
              }
            ] : [],
            [
              {
                name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
                value: applicationInsights.properties.InstrumentationKey
              }
              {
                name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
                value: applicationInsights.properties.ConnectionString
              }
              {
                name:'ManagedIdentityClientId'
                value: userIdentity.properties.clientId
              }
            ]
          )
          resources: {
            cpu: json(containerCpuCoreCount)
            memory: containerMemory
          }
        }
      ]
      scale: {
        minReplicas: containerMinReplicas
        maxReplicas: containerMaxReplicas
      }
    }
}}]

resource bookingServiceDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2022-05-15' = {
  name: 'BookingService'
  parent: cosmosDBAccount
  properties: {
    resource: {
      id: 'BookingService'
    }
  }
}


resource weatherServiceDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2022-05-15' = {
  name: 'WeatherService'
  parent: cosmosDBAccount
  properties: {
    resource: {
      id: 'WeatherService'
    }
  }
}

resource bookingsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-05-15' = {
  name: 'Bookings'
  parent: bookingServiceDatabase
  properties: {
    resource: {
      id: 'Bookings'
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource leasesContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-05-15' = {
  name: 'leases'
  parent: bookingServiceDatabase
  properties: {
    resource: {
      id: 'leases'
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource weatherContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-05-15' = {
  name: 'Weather'
  parent: weatherServiceDatabase
  properties: {
    resource: {
      id: 'Weather'
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' = {
    name: containerAppsEnvironmentName
    location: location
    tags: tags
    properties: {
      appLogsConfiguration: {
        destination: 'log-analytics'
        logAnalyticsConfiguration: {
          customerId: logAnalyticsWorkspace.properties.customerId
          sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
        }
      }      
    }
  }


  resource storageAccount 'Microsoft.Storage/storageAccounts@2022-05-01' existing = {
    name: storageAccountName
  }

  resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2022-05-01' = {
    name: 'default'
    parent: storageAccount
  }

  resource container 'Microsoft.Storage/storageAccounts/blobServices/containers@2022-05-01' = {
    name: 'swagger'
    parent: blobService
    properties: {
      publicAccess: 'Blob'
    }
  }

  resource bookingServiceDeploymentScript 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
    name: 'swagger-upload-BookingService'
    location: location
    kind: 'AzureCLI'
  properties: {
    azCliVersion: '2.26.1'
    timeout: 'PT5M'
    retentionInterval: 'PT1H'
    environmentVariables: [
      {
        name: 'AZURE_STORAGE_ACCOUNT'
        value: storageAccount.name
      }
      {
        name: 'AZURE_STORAGE_KEY'
        secureValue: storageAccount.listKeys().keys[0].value
      }
      {
        name: 'CONTENT'
        value: loadTextContent('../../docs/openapi/BookingService.json')
      }
    ]
    scriptContent: 'echo "$CONTENT" > /tmp/swagger.json && az storage blob upload --account-name $AZURE_STORAGE_ACCOUNT --account-key $AZURE_STORAGE_KEY --container-name swagger --file /tmp/swagger.json --name BookingService.json'
  }
}

resource weatherServiceDeploymentScript 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: 'swagger-upload-WeatherService'
  location: location
  kind: 'AzureCLI'
properties: {
  azCliVersion: '2.26.1'
  timeout: 'PT5M'
  retentionInterval: 'PT1H'
  environmentVariables: [
    {
      name: 'AZURE_STORAGE_ACCOUNT'
      value: storageAccount.name
    }
    {
      name: 'AZURE_STORAGE_KEY'
      secureValue: storageAccount.listKeys().keys[0].value
    }
    {
      name: 'CONTENT'
      value: loadTextContent('../../docs/openapi/WeatherService.json')
    }
  ]
  scriptContent: 'echo "$CONTENT" > /tmp/swagger.json && az storage blob upload --account-name $AZURE_STORAGE_ACCOUNT --account-key $AZURE_STORAGE_KEY --container-name swagger --file /tmp/swagger.json --name WeatherService.json'
}
}

resource flightServiceDeploymentScript 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: 'swagger-upload-FlightService'
  location: location
  kind: 'AzureCLI'
properties: {
  azCliVersion: '2.26.1'
  timeout: 'PT5M'
  retentionInterval: 'PT1H'
  environmentVariables: [
    {
      name: 'AZURE_STORAGE_ACCOUNT'
      value: storageAccount.name
    }
    {
      name: 'AZURE_STORAGE_KEY'
      secureValue: storageAccount.listKeys().keys[0].value
    }
    {
      name: 'CONTENT'
      value: loadTextContent('../../docs/openapi/FlightService.json')
    }
  ]
  scriptContent: 'echo "$CONTENT" > /tmp/swagger.json && az storage blob upload --account-name $AZURE_STORAGE_ACCOUNT --account-key $AZURE_STORAGE_KEY --container-name swagger --file /tmp/swagger.json --name FlightService.json'
}
}


resource userServiceDeploymentScript 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: 'swagger-upload-UserService'
  location: location
  kind: 'AzureCLI'
properties: {
  azCliVersion: '2.26.1'
  timeout: 'PT5M'
  retentionInterval: 'PT1H'
  environmentVariables: [
    {
      name: 'AZURE_STORAGE_ACCOUNT'
      value: storageAccount.name
    }
    {
      name: 'AZURE_STORAGE_KEY'
      secureValue: storageAccount.listKeys().keys[0].value
    }
    {
      name: 'CONTENT'
      value: loadTextContent('../../docs/openapi/UserService.json')
    }
  ]
  scriptContent: 'echo "$CONTENT" > /tmp/swagger.json && az storage blob upload --account-name $AZURE_STORAGE_ACCOUNT --account-key $AZURE_STORAGE_KEY --container-name swagger --file /tmp/swagger.json --name UserService.json'
}
}


  resource apim 'Microsoft.ApiManagement/service@2023-09-01-preview' = {
    name: apimName
    location: location
    tags: tags
    sku: {
      name: apimTier
      capacity: apimCapacity
    }
    identity: {
      type: 'UserAssigned'
      userAssignedIdentities: {
        '${userIdentity.id}': {}
      }
    }
    properties: {
      publisherEmail: 'test@test.com'
      publisherName: 'test'
    }
  }


  resource products 'Microsoft.ApiManagement/service/products@2023-09-01-preview'= [for service in services: {
    name: '${service.deploymentName}'
    parent: apim
    properties: {
      displayName: '${service.deploymentName} Product'
      description: 'Product for ${service.deploymentName}'
      terms: 'Terms of service for ${service.deploymentName}'
      subscriptionRequired: false
      state: 'published'
    }
  }]


  resource apis 'Microsoft.ApiManagement/service/apis@2023-09-01-preview' = [for (service,i) in services: {
    name: '${service.deploymentName}-api'
    parent: apim
    dependsOn:[
      app
      userServiceDeploymentScript
      weatherServiceDeploymentScript
      flightServiceDeploymentScript
      bookingServiceDeploymentScript
    ]
    properties: {
      format: 'openapi-link'
      value: 'https://${storageAccount.name}.blob.core.windows.net/swagger/${service.deploymentName}.json'
      displayName: '${service.deploymentName} API'
      description: 'API for ${service.deploymentName}'
      serviceUrl: 'https://${app[i].properties.configuration.ingress.fqdn}'
      path: service.deploymentName
    }
  }]

  resource productLinks 'Microsoft.ApiManagement/service/products/apis@2023-09-01-preview' = [for (service,i) in services: {
    name: '${service.deploymentName}-api'
    parent: products[i]
    dependsOn: [
      apis[i]
    ]
  }]


  resource sqlServer 'Microsoft.Sql/servers@2022-11-01-preview' = {
    name: sqlServerName
    location: location
    tags: tags
    properties: {
      administratorLogin: sqlAdminUsername
      administratorLoginPassword: sqlAdminPassword
      minimalTlsVersion: sqlMinTLSVersion
      publicNetworkAccess: 'Enabled'
    }
  }

  module sqlDatabase '../shared/azureSqlDB.bicep' = [for service in services : if (contains(['FlightService', 'UserService'], service.deploymentName)) {
    name: '${service.deploymentName}-sql-infra'
    dependsOn:[sqlServer]
    params: {
      sqlServerName: sqlServerName
      sqlDatabaseName: '${service.sqlDatabaseName}'
      location: location
    }
  }]

  resource cosmosDBAccount 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' existing = {
    name: cosmos_name
  }

  output containerRegistryEndpoint string = containerRegistry.properties.loginServer
  output FlightServiceConnectionString string = 'Server=${sqlServerName}.database.windows.net;Database=FlightServiceDB;User ID=${sqlAdminUsername};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
  output UserServiceConnectionString string = 'Server=${sqlServerName}.database.windows.net;Database=UserServiceDB;User ID=${sqlAdminUsername};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
