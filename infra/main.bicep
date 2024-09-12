targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@secure()
param travelServiceMultiAgentOrchestratorDefinition object

@description('Location for the OpenAI resource group')
@allowed(['australiaeast', 'canadaeast', 'francecentral', 'southindia', 'uksouth', 'swedencentral', 'westus', 'eastus'])
@metadata({
  azd: {
    type: 'location'
  }
})
param openAILocation string

param openAISku string = 'S0'

param chatGptDeploymentName string = 'gpt4'
param chatGptModelName string = 'gpt-4o'
param chatGptModelVersion string = '2024-05-13'
param embeddingDeploymentName string = 'embedding'
param embeddingDeploymentCapacity int = 120
param embeddingModelName string = 'text-embedding-ada-002'


var tags = {
  'azd-env-name': environmentName
}

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module monitoring './shared/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    tags: tags
    logAnalyticsName: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: '${abbrs.insightsComponents}${resourceToken}'
  }
  scope: rg
}

module dashboard './shared/dashboard-web.bicep' = {
  name: 'dashboard'
  params: {
    name: '${abbrs.portalDashboards}${resourceToken}'
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    location: location
    tags: tags
  }
  scope: rg
}

module travelServiceMultiAgentOrchestrator './app/TravelService.MultiAgent.Orchestrator.bicep' = {
  name: 'TravelService.MultiAgent.Orchestrator.FunctionApp'
  params: {
    name: '${abbrs.webSitesFunctions}travelservice-${resourceToken}'
    location: location
    tags: tags
    identityName: '${abbrs.managedIdentityUserAssignedIdentities}travelservice-${resourceToken}'
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    runtimeName: 'dotnet-isolated'
    runtimeVersion: '8.0'    
    redisCacheName: '${abbrs.cacheRedis}travelservice${resourceToken}'
    storageAccountName: '${abbrs.storageStorageAccounts}${resourceToken}'    
    appDefinition: travelServiceMultiAgentOrchestratorDefinition    
    openAiLocation: openAILocation
    openAiSkuName: openAISku
    deployments: [
      {
        name: chatGptDeploymentName
        model: {
          format: 'OpenAI'
          name: chatGptModelName
          version: chatGptModelVersion
        }
        scaleSettings: {
          scaleType: 'Standard'
        }
      }
      {
        name: embeddingDeploymentName
        model: {
          format: 'OpenAI'
          name: embeddingModelName          
        }
        sku: {
          name: 'Standard'
          capacity: embeddingDeploymentCapacity
        }
      }
    ]    
    chatGptDeploymentName: chatGptDeploymentName        
    embeddingDeploymentName: embeddingDeploymentName
    cosmos_name: '${abbrs.documentDBDatabaseAccounts}${resourceToken}'
  }
  scope: rg
}

module travelServiceCustomerUI './app/TravelService.CustomerUI.bicep' = {
  name: 'TravelService.CustomerUI.AppService'
  params: {
    name: '${abbrs.webSites}${resourceToken}'
    location: location
    tags: tags    
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    redisCacheName: travelServiceMultiAgentOrchestrator.outputs.redisCacheName
    functionAppName: travelServiceMultiAgentOrchestrator.outputs.functionAppName
    appServicePlanName: 'asp-${abbrs.webSites}${resourceToken}'    
  }
  scope: rg
}

output OPENAI_API_URL string = travelServiceMultiAgentOrchestrator.outputs.apiUrl
output OPENAI_DEPLOYMENT_NAME string = travelServiceMultiAgentOrchestrator.outputs.chatGptDeploymentName
output COSMOSDB_ACCOUNT_NAME string = travelServiceMultiAgentOrchestrator.outputs.cosmosDBAccountName
output AZURE_OPENAI_LOCATION string = openAILocation
output AZURE_TENANT_ID string = tenant().tenantId
output RESOURCE_GROUP_NAME string = rg.name
output AZURE_SUBSCRIPTION_ID string = subscription().subscriptionId
