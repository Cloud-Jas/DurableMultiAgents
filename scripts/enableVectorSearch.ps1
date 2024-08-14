param (
    [string]$cosmosDBAccountName,
    [string]$resourceGroupName
)

Write-Output "Seeding data in Cosmos DB account '$cosmosDBAccountName' in resource group '$resourceGroupName'..."

# Check if the NoSQLVectorSearch capability is enabled
$cosmosDBAccount = az cosmosdb show --resource-group $resourceGroupName --name $cosmosDBAccountName --query "capabilities" -o json | ConvertFrom-Json
$capabilities = $cosmosDBAccount.capabilities -join " "

if ($capabilities -contains "EnableNoSQLVectorSearch") {
    Write-Output "NoSQLVectorSearch capability is already enabled. Skipping capability update."
} else {
    Write-Output "Enabling NoSQLVectorSearch capability..."
    az cosmosdb update --resource-group $resourceGroupName --name $cosmosDBAccountName --capabilities EnableNoSQLVectorSearch
}