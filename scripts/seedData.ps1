param (
    [string]$cosmosDBAccountName,
    [string]$resourceGroupName, 
    [string]$databaseName = "ContosoTravelAgency"    
)

Push-Location "scripts"

$flagFilePath = ".\migrationCompleted.flag"

if (Test-Path -Path $flagFilePath) {
    Write-Output "Migration has already been completed. Skipping..."
    exit
}

Write-Output "Seeding data in Cosmos DB account '$cosmosDBAccountName' in resource group '$resourceGroupName'..."
Install-Module -Name Az.CosmosDB -Force -AllowClobber
Import-Module Az.CosmosDB

$cosmosDBConnectionStrings = Get-AzCosmosDBAccountKey -ResourceGroupName $resourceGroupName -Name $cosmosDBAccountName -Type "ConnectionStrings"
$cosmosDBPrimarySQLConnectionString = $cosmosDBConnectionStrings["Primary SQL Connection String"]
$connectionString = $cosmosDBPrimarySQLConnectionString + "Database='$databaseName';"

$jsonFilePath = ".\migrationsettings.template.json"
$destinationJsonFilePath = ".\migrationsettings.json"
$jsonTemplate = Get-Content -Path $jsonFilePath -Raw

$jsonConfig = $jsonTemplate -replace "{{cosmosAccountName}}", $cosmosDBAccountName `
                           -replace "{{cosmosConnectionString}}", $connectionString `
                           -replace "{{databaseName}}", $databaseName

$jsonConfig | Out-File -FilePath $destinationJsonFilePath -Encoding utf8

Write-Output "Checking container $containerName..."

try 
{
    Write-Output "Seed data into database $databaseName..."
    & "./dmt.exe"
    Write-Output "End seed data into database $databaseName..."
    New-Item -Path $flagFilePath -ItemType File -Force | Out-Null
    Remove-Item -Path $destinationJsonFilePath -Force -ErrorAction Stop
    Write-Output "Migration completed successfully. Flag file created."
} 
catch 
{
    Write-Error "An error occurred while seeding data. Exception details: $_"
}