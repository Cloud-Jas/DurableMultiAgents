@description('The name of the Azure SQL Server.')
param sqlServerName string

@description('The name of the Azure SQL Database.')
param sqlDatabaseName string

@description('Location for the SQL resources.')
param location string

@description('The edition of the Azure SQL Database.')
param sqlDatabaseEdition string = 'Basic'

@description('The compute size of the Azure SQL Database.')
param sqlComputeSize string = 'Basic'

@description('The collation of the Azure SQL Database.')
param sqlCollation string = 'SQL_Latin1_General_CP1_CI_AS'

@description('The IP range start for the SQL Server firewall.')
param sqlFirewallStartIP string = '0.0.0.0'

@description('The IP range end for the SQL Server firewall.')
param sqlFirewallEndIP string = '255.255.255.255'

resource sqlServer 'Microsoft.Sql/servers@2022-11-01-preview' existing= {
  name: sqlServerName
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-11-01-preview' = {
  parent: sqlServer
  dependsOn: [
    sqlServer
  ]
  name: sqlDatabaseName
  location: location
  properties: {
    collation: sqlCollation
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 268435456000
    zoneRedundant: false
  }
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
}

resource sqlFirewallRule 'Microsoft.Sql/servers/firewallRules@2022-11-01-preview' = {
  parent: sqlServer
  name: 'AllowAllAzureIPs'
  properties: {
    startIpAddress: sqlFirewallStartIP
    endIpAddress: sqlFirewallEndIP
  }
}

resource sqlFirewallAzureServices 'Microsoft.Sql/servers/firewallRules@2022-11-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

output sqlServerFQDN string = sqlServer.properties.fullyQualifiedDomainName
