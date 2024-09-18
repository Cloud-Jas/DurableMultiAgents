param name string
param location string
param tags object
param identityName string
param storageAccountName string
param redisCacheName string
param applicationInsightsName string
@secure()
param appDefinition object
@allowed([
  'dotnet', 'dotnetcore', 'dotnet-isolated', 'node', 'python', 'java', 'powershell', 'custom'
])
param runtimeName string
param runtimeNameAndVersion string = '${runtimeName}|${runtimeVersion}'
param runtimeVersion string

// Microsoft.Web/sites Properties
param kind string = 'functionapp'

// Microsoft.Web/sites/config
param allowedOrigins array = []
param clientAffinityEnabled bool = false

param openAiLocation string
param openAiSkuName string
param chatGptDeploymentName string
param embeddingDeploymentName string
param cosmos_name string
@description('Friendly name for the SQL Role Definition')
param roleDefinitionName string = 'My Read Write Role'

@description('Data actions permitted by the Role Definition')
param dataActions array = [
  'Microsoft.DocumentDB/databaseAccounts/readMetadata'
  'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/*'
  'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/executeQuery'
  'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/readChangeFeed'
]

param deployments array = []
param deploymentCapacity int = 120

var indexingPolicy = {
  automatic: true
  indexingMode: 'consistent'
  includedPaths: [
    {
      path: '/vector/*'
    }
  ]
  excludedPaths: [
    {
      path: '/*'
    }
  ]
}

var vectorEmbeddingPolicy = {
  vectorEmbeddings: [
    {
      path: '/vector'
      dataType: 'float32'
      dimensions: 1536
      distanceFunction: 'cosine'
    }
  ]
}

var roleDefinitionId = guid('sql-role-definition-', identity.id, cosmosDBAccount.id)
var roleAssignmentId = guid(roleDefinitionId, identity.id, cosmosDBAccount.id)
var cognitiveServicesUserRoleDefinitionId = resourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
var cognitiveRoleAssignmentId = guid(cognitiveServicesUserRoleDefinitionId,identity.id)
var openaiName = 'openai-${name}-${uniqueString(name)}'

var appSettingsArray = filter(array(appDefinition.settings), i => i.name != '')
var secrets = map(filter(appSettingsArray, i => i.?secret != null), i => {
  name: i.name
  value: i.value
  secretRef: i.?secretRef ?? take(replace(replace(toLower(i.name), '_', '-'), '.', '-'), 32)
})
var env = map(filter(appSettingsArray, i => i.?secret == null), i => {
  name: i.name
  value: i.value
})

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-05-01' = {
  name: storageAccountName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: { name: 'Standard_LRS' }
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
  }
}
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsName
}

resource cosmosDBAccount 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: cosmos_name
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    capabilities:[      
      {name: 'EnableNoSQLVectorSearch'}
      {name: 'EnableServerless'}
    ]
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
    disableKeyBasedMetadataWriteAccess: true   
  }
}

resource cosmosDBDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2022-05-15' = {
  name: 'ContosoTravelAgency'
  parent: cosmosDBAccount
  properties: {
    resource: {
      id: 'ContosoTravelAgency'
    }
  }
}

resource flightListingsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-05-15' = {
  name: 'FlightListings'
  parent: cosmosDBDatabase
  properties: {
    resource: {
      id: 'FlightListings'
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource bookingsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-05-15' = {
  name: 'Bookings'
  parent: cosmosDBDatabase
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

resource airlinesContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-05-15' = {
  name: 'Airlines'
  parent: cosmosDBDatabase
  properties: {
    resource: {
      id: 'Airlines'
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource passengersContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-05-15' = {
  name: 'Passengers'
  parent: cosmosDBDatabase
  properties: {
    resource: {
      id: 'Passengers'
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource airportsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-05-15' = {
  name: 'Airports'
  parent: cosmosDBDatabase
  properties: {
    resource: {
      id: 'Airports'
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource paymentsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-05-15' = {
  name: 'Payments'
  parent: cosmosDBDatabase
  properties: {
    resource: {
      id: 'Payments'
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
  parent: cosmosDBDatabase
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

resource chatHistoryContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-05-15' = {
  name: 'ChatHistory'
  parent: cosmosDBDatabase
  properties: {
    resource: {
      id: 'ChatHistory'
      partitionKey: {
        paths: [
          '/sessionId'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource semanticBookingLayerContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-05-15' = {
  name: 'SemanticBookingLayer'
  parent: cosmosDBDatabase
  properties: {
    resource: {
      id: 'SemanticBookingLayer'
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
  parent: cosmosDBDatabase
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

resource semanticBookingVectorLayerContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  name: 'SemanticBookingVectorLayer'
  parent: cosmosDBDatabase
  tags: tags
  properties: {    
    resource: union(
      {
        id: 'SemanticBookingVectorLayer'
        partitionKey: {
          paths:  [
            '/id'
          ]
          kind: 'Hash'
        }
      },
      !empty(indexingPolicy)
        ? {
            indexingPolicy: indexingPolicy
          }
        : {},
      !empty(vectorEmbeddingPolicy)
        ? {
            vectorEmbeddingPolicy: vectorEmbeddingPolicy
          }
        : {}
    )
  }
}

resource sqlRoleDefinition 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2021-04-15' = {
  parent: cosmosDBAccount
  name: roleDefinitionId
  properties: {
    roleName: roleDefinitionName
    type: 'CustomRole'
    assignableScopes: [
      cosmosDBAccount.id
    ]
    permissions: [
      {
        dataActions: dataActions
      }
    ]
  }
}

resource sqlRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2021-04-15' = {
  parent: cosmosDBAccount
  name: roleAssignmentId
  properties: {
    roleDefinitionId: sqlRoleDefinition.id
    principalId: identity.properties.principalId
    scope: cosmosDBAccount.id
  }
}
resource cognitiveServicesUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {  
  name: cognitiveRoleAssignmentId
  properties: {
    roleDefinitionId: cognitiveServicesUserRoleDefinitionId
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'    
  }
}

resource openAIService 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openaiName
  location: openAiLocation
  kind: 'OpenAI'
  sku: {
    name: openAiSkuName
  }
  properties: {    
    customSubDomainName: openaiName
    publicNetworkAccess: 'Enabled'
  }
  tags: tags  
}

@batchSize(1)
resource deployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = [for deployment in deployments: {
  parent: openAIService
  name: deployment.name
  properties: {
    model: deployment.model
    raiPolicyName: deployment.?raiPolicyName ?? null
  }
  sku: deployment.?sku ?? {
    name: 'Standard'
    capacity: deploymentCapacity
  }
}]

resource redisCache 'Microsoft.Cache/redis@2023-08-01' = {
  name: redisCacheName
  location: location
  properties: {
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    sku: {
      capacity: 0
      family: 'C'
      name: 'Basic'
    }   
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  kind: kind
  properties: {
    reserved: false
  }
}

resource functionApp 'Microsoft.Web/sites@2022-03-01' = {
  name: name
  location: location   
  kind: kind  
  tags: union(tags, {'azd-service-name':  'TravelService.MultiAgent.Orchestrator' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    clientAffinityEnabled: clientAffinityEnabled    
    siteConfig: {            
      netFrameworkVersion: 'v8.0'
      ftpsState: 'FtpsOnly'
      cors: {
        allowedOrigins: union([ 'https://portal.azure.com', 'https://ms.portal.azure.com' ], allowedOrigins)
      }
      appSettings: union([
        {
          name: 'AzureWebJobsStorage'          
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: storageAccount.name
        }
        {
          name: 'UserAssignedIdentity'
          value: identity.properties.clientId
        }
        {
          name: 'TenantId'
          value: subscription().tenantId
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: applicationInsights.properties.InstrumentationKey
        }      
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }  
        {
          name: 'CosmosDBAccountEndpoint'
          value: cosmosDBAccount.properties.documentEndpoint
        }
        {
            name: 'ResourceGroup'
            value: resourceGroup().name
        }
        {
            name: 'DatabaseAccount'
            value: cosmosDBAccount.name
        }
        {
          name: 'SubscriptionId'
          value: subscription().subscriptionId
        }
        {
          name: 'DatabaseId'
          value: cosmosDBDatabase.name
        }
        {
          name: 'OpenAIEndpoint'
          value: 'https://${openAIService.name}.openai.azure.com/'
        }
        {
          name: 'OpenAIChatCompletionDeploymentName'
          value: chatGptDeploymentName
        }
        {
          name: 'OpenAITextEmbeddingGenerationDeploymentName'
          value: embeddingDeploymentName
        }
        {
          name: 'cosmosDB__accountEndpoint'
          value: cosmosDBAccount.properties.documentEndpoint
        }
        {
          name: 'cosmosDB__credential'
          value: 'managedidentity'
        }
        {
          name: 'cosmosDB__clientId'
          value: identity.properties.clientId
        }       
        {
          name: 'RedisConnectionString'
          value: '${redisCacheName}.redis.cache.windows.net,abortConnect=false,ssl=true,password=${redisCache.listKeys().primaryKey}'
        }  
      ],env, map(secrets, secret => {
        name: secret.name
        value: secret.value
      }))
    }
  }
}

output apiUrl string = 'https://${openAIService.name}.openai.azure.com/'
output chatGptDeploymentName string = chatGptDeploymentName
output cosmosDBAccountName string = cosmosDBAccount.name
output functionAppName string = functionApp.name
output redisCacheName string = redisCache.name