param name string
param location string
param tags object
param applicationInsightsName string
param appServicePlanName string
param functionAppName string
param redisCacheName string

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

resource webApp 'Microsoft.Web/sites@2022-03-01' = {
  name: name
  location: location
  tags: union(tags, {'azd-service-name':  'TravelService.CustomerUI' })
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
      ] 
      runtimeName: 'dotnetcore'
      runtimeVersion: '8.0' 
    }
  }
}

output webAppUrl string = webApp.properties.defaultHostName