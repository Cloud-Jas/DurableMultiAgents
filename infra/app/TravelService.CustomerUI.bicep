param name string
param location string
param tags object
param applicationInsightsName string
param appServicePlanName string
param functionAppName string
param redisCacheName string
param identityName string
param realtimeOpenAILocation string
param realtimeDeploymentName string
param openAiSkuName string
param deployments array = []
param deploymentCapacity int = 120

var cognitiveServicesUserRoleDefinitionId = resourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
var cognitiveRoleAssignmentId = guid(cognitiveServicesUserRoleDefinitionId,identity.id)
var openaiName = 'realtimeopenai-${name}-${uniqueString(name)}'

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  kind: 'linux'
  properties: {
    reserved: false
  }
}
resource functionApp 'Microsoft.Web/sites@2022-03-01' existing = {
  name: functionAppName
}

resource redisCache 'Microsoft.Cache/redis@2023-08-01' existing = {
  name: redisCacheName
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsName
}
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
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
  location: realtimeOpenAILocation
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
    name: 'GlobalStandard'
    capacity: 1
  }
}]
resource webApp 'Microsoft.Web/sites@2022-03-01' = {
  name: name
  location: location
  tags: union(tags, {'azd-service-name':  'TravelService.CustomerUI' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~2'
        }
        {
            name: 'apiUrl'
            value: 'https://${functionApp.properties.defaultHostName}'
        }
        {
            name: 'UserId'
            value: 'P012'
        }
        {
          name: 'RedisConnectionString'
          value: '${redisCacheName}.redis.cache.windows.net,abortConnect=false,ssl=true,password=${redisCache.listKeys().primaryKey}'
        }  
        {
            name: 'AZURE_OPENAI_DEPLOYMENT'
            value: realtimeDeploymentName
        }
        {
            name:'AZURE_OPENAI_API_KEY'
            value: '${openAIService.listKeys().key1}'
        }
        {
            name:'AZURE_OPENAI_ENDPOINT'
            value:'https://${openAIService.name}.openai.azure.com/'
        }
      ] 
      runtimeName: 'dotnetcore'
      runtimeVersion: '8.0' 
    }
  }
}

output webAppUrl string = webApp.properties.defaultHostName