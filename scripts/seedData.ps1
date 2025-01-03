param (
    [string]$cosmosDBAccountName,
    [string]$resourceGroupName, 
    [string]$subscriptionId,
    [string]$tenantId,
    [string]$databaseName = "ContosoTravelAgency",
    [string]$weatherDatabaseName = "WeatherService",
    [string]$flightConnectionString,
    [string]$userConnectionString,
    [bool]$hardReload
)

Install-Module -Name Az.Sql -Force -AllowClobber
Install-Module -Name SqlServer -Force -AllowClobber
Import-Module SqlServer
Install-Module -Name Az.CosmosDB -Force -AllowClobber
Import-Module Az.CosmosDB


$services = @(
    @{ 
        Name = "FlightService";
        ConnectionString = $flightConnectionString;
        Database = "FlightServiceDB";
        RequiredTables = @("Airlines", "Airports","FlightListings")
    },
    @{ 
        Name = "UserService";
        ConnectionString = $userConnectionString;
        Database = "UserServiceDB";
        RequiredTables = @("Passengers")
    }
)

$cosmosServices = @(
    @{
        Name = "WeatherService";
        DatabaseId = "WeatherService";
        RequiredContainers = @("Weather")
        PartitionKey = "/id"
    }
)

function Fetch-JsonData {
    param (
        [string]$url
    )
    $json = Invoke-RestMethod -Uri $url -Method Get
    return $json
}

function Clear-SqlTable {
    param (
        [string]$connectionString,
        [string]$tableName
    )
    $deleteQuery = "DELETE FROM $tableName;"
    Invoke-Sqlcmd -ConnectionString $connectionString -Query $deleteQuery
    $dropQuery = "DROP TABLE IF EXISTS $tableName;"
    Invoke-Sqlcmd -ConnectionString $connectionString -Query $dropQuery
    Write-Output "All records deleted from table '$tableName'."
}

function Clear-CosmosContainer {
    param (
        [string]$cosmosDBAccountName,
        [string]$resourceGroupName,
        [string]$databaseName,
        [string]$containerName,
        [string]$partitionKeyPath
    )    


    Write-Host "Deleting container: $containerName..."
    az cosmosdb sql container delete `
        --account-name $cosmosDbAccountName `
        --resource-group $resourceGroupName `
        --database-name $databaseName `
        --name $containerName --yes

    Write-Host "Recreating container: $containerName..."
    az cosmosdb sql container create `
        --account-name $cosmosDbAccountName `
        --resource-group $resourceGroupName `
        --database-name $databaseName `
        --name $containerName `
        --partition-key-path $partitionKeyPath


    Write-Output "All documents deleted from Cosmos DB container '$containerName'."
}

function Check-TableExists {
    param (
        [string]$connectionString,
        [string]$tableName
    )

    $query = @"
    SELECT CASE 
        WHEN EXISTS (
            SELECT 1 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_NAME = '$tableName'
        ) THEN 1
        ELSE 0
    END AS TableExists
"@

    $result = Invoke-Sqlcmd -ConnectionString $connectionString -Query $query
    return $result.TableExists -eq 1
}


function Create-Table {
    param (
        [string]$connectionString,
        [string]$tableName
    )

    switch ($tableName) {
        "Airlines" {
            $createTableQuery = @"
            CREATE TABLE Airlines (
                AirlineId NVARCHAR(50) PRIMARY KEY,
                Name NVARCHAR(255) NOT NULL,
                Code NVARCHAR(10) NOT NULL,
                Country NVARCHAR(100) NOT NULL,
                City NVARCHAR(100) NOT NULL,
                LogoUrl NVARCHAR(255) NULL
            );
"@
        }
        "Airports" {
            $createTableQuery = @"
            CREATE TABLE Airports (
                AirportId NVARCHAR(50) PRIMARY KEY,
                Code NVARCHAR(10) NOT NULL,
                Name NVARCHAR(255) NOT NULL,
                City NVARCHAR(100) NOT NULL,
                Country NVARCHAR(100) NOT NULL
            );
"@
        }
        "FlightListings" {
            $createTableQuery = @"
            CREATE TABLE FlightListings (
                    FlightId NVARCHAR(50) PRIMARY KEY,
                    FlightNumber NVARCHAR(20) NOT NULL,
                    AirlineId NVARCHAR(50) NOT NULL,
                    AirlineCode NVARCHAR(10) NOT NULL,
                    DepartureAirportCode NVARCHAR(10) NOT NULL,
                    DestinationAirportCode NVARCHAR(10) NOT NULL,
                    DepartureTime DATETIME NOT NULL,
                    Price DECIMAL(10, 2) NOT NULL,
                    Description NVARCHAR(255) NULL,
                    AircraftType NVARCHAR(100) NULL,
                    AvailableSeats INT NOT NULL,
                    Duration NVARCHAR(50) NOT NULL,
                    FOREIGN KEY (AirlineId) REFERENCES Airlines(AirlineId) 
            );
"@
        }
        "Passengers" {
            $createTableQuery = @"
            CREATE TABLE Passengers (
                    Id NVARCHAR(50) PRIMARY KEY,
                    FirstName NVARCHAR(100) NOT NULL,
                    LastName NVARCHAR(100) NOT NULL,
                    Email NVARCHAR(255) NOT NULL,
                    Phone NVARCHAR(20) NULL,
                    PassportNumber NVARCHAR(20) NOT NULL,
                    Nationality NVARCHAR(50) NOT NULL,
                    DOB DATE NOT NULL,
                    FrequentFlyerNumber NVARCHAR(50) NULL,
            );
"@
        }
        default {
            throw "Unknown table name: $tableName"
        }
    }

    Invoke-Sqlcmd -ConnectionString $connectionString -Query $createTableQuery
    Write-Output "Table '$tableName' created in the database."

    if($tableName -eq "FlightListings")
    {
        Write-Output "create index for table $tableName..."
        $createIndexQuery = @"
        CREATE INDEX IX_FlightListings_DepartureDestinationDate 
        ON FlightListings (DepartureAirportCode, DestinationAirportCode, DepartureTime);
"@  
        Invoke-Sqlcmd -ConnectionString $connectionString -Query $createIndexQuery
    }
    if($tableName -eq "Airlines")
    {
        Write-Output "create index for table $tableName..."
        $createIndexQuery = @"
        CREATE INDEX IX_Airlines_City ON Airlines (City);
"@
        Invoke-Sqlcmd -ConnectionString $connectionString -Query $createIndexQuery
    }
}

Push-Location "scripts"

Write-Output "hardReload: $hardReload"

if($hardReload) {    
    
    Write-Output "Hard reload requested. Clearing all data from databases and Cosmos DB containers..."

    foreach ($service in $services) {

    if($service.Name -in @("FlightService"))
    {
    [Array]::Reverse($service.RequiredTables)
    }

        foreach ($table in $service.RequiredTables) {
             $exists = Check-TableExists -connectionString $service.ConnectionString -tableName $table
        if ($exists) {
            Write-Output "Table '$table' exists in the database."
            Clear-SqlTable -connectionString $service.ConnectionString -tableName $table
        } else {
            Write-Output "Table '$table' does not exist in the database. Skipping..."
        }
        }
    }

    foreach ($cosmosService in $cosmosServices) {
        foreach ($container in $cosmosService.RequiredContainers) {
            Clear-CosmosContainer -cosmosDBAccountName $cosmosDBAccountName `
                                  -resourceGroupName $resourceGroupName `
                                  -databaseName $cosmosService.DatabaseId `
                                  -containerName $container `
                                  -partitionKeyPath $cosmosService.PartitionKey
        }
    }

    Write-Output "Cosmos DB containers cleared."

    Remove-Item -Path ".\migrationCompleted.flag" -Force -ErrorAction SilentlyContinue
}

$flagFilePath = ".\migrationCompleted.flag"

if (Test-Path -Path $flagFilePath) {
    Write-Output "Migration has already been completed. Skipping..."
    exit
}

Connect-AzAccount -Tenant $tenantId -Subscription $subscriptionId

Write-Output "Seeding data in SQL databases..."

foreach ($service in $services) {
    Write-Output "Checking service: $($service.Name)"

    $connectionString = $service.ConnectionString

    if($service.Name -in @("FlightService"))
    {
    [Array]::Reverse($service.RequiredTables)
    }

    foreach ($table in $service.RequiredTables) {
        $exists = Check-TableExists -connectionString $connectionString -tableName $table
        if ($exists) {
            Write-Output "Table '$table' exists in the database."
        } else {
            Write-Output "Table '$table' does not exist in the database. Creating it..."
            Create-Table -connectionString $connectionString -tableName $table
        }
    }

    foreach ($table in $service.RequiredTables) {
        $jsonData = Fetch-JsonData -url "https://sadtdl.blob.core.windows.net/durablemultiagents/$table.json"
        Write-Output "Inserting data into '$table'"
        foreach ($record in $jsonData) {
            
            $escapeString = { param($input) $input -replace "'", "''" }
            
            switch ($table) {
                "Airlines" {
                    $insertQuery = @"
                    INSERT INTO Airlines (AirlineId, Name, Code, Country, City, LogoUrl)
                    VALUES ('$($record.Id)', '$($record.Name)', '$($record.Code)', '$($record.Country)', '$($record.City)', '$($record.LogoUrl)');
"@
                }
                "Airports" {
                    $insertQuery = @"
                    INSERT INTO Airports (AirportId, Code, Name, City, Country)
                    VALUES ('$($record.Id)', '$($record.Code)', '$($escapeString.Invoke($record.Name))', '$($record.City)', '$($record.Country)');
"@
                }
                "FlightListings" {
                    $insertQuery = @"
                    INSERT INTO FlightListings (FlightId, FlightNumber, AirlineId, AirlineCode, DepartureAirportCode, DestinationAirportCode, DepartureTime, Price, Description, AircraftType, AvailableSeats, Duration)
                    VALUES ('$($record.Id)', '$($record.FlightNumber)', '$($record.AirlineId)', '$($record.AirlineCode)', '$($record.Departure)', '$($record.Destination)', '$($record.DepartureTime)', $($record.Price), '$($record.Description)', '$($record.AircraftType)', $($record.AvailableSeats), '$($record.Duration)');
"@
                }
                "Passengers" {
                    $insertQuery = @"
                    INSERT INTO Passengers (Id, FirstName, LastName, Email, Phone, PassportNumber, Nationality, DOB, FrequentFlyerNumber)
                    VALUES ('$($record.Id)', '$($record.FirstName)', '$($record.LastName)', '$($record.Email)', '$($record.Phone)', '$($record.PassportNumber)', '$($record.Nationality)', '$($record.DOB)', '$($record.FrequentFlyerNumber)');
"@
                }
            }
            Invoke-Sqlcmd -ConnectionString $connectionString -Query $insertQuery
        }
    }
}

Write-Output "Seeding data in Cosmos DB account '$cosmosDBAccountName' in resource group '$resourceGroupName'..."

$cosmosDBConnectionStrings = Get-AzCosmosDBAccountKey -ResourceGroupName $resourceGroupName -Name $cosmosDBAccountName -Type "ConnectionStrings"
$cosmosDBPrimarySQLConnectionString = $cosmosDBConnectionStrings["Primary SQL Connection String"]
$connectionString = $cosmosDBPrimarySQLConnectionString + "Database='$weatherDatabaseName';"

$jsonFilePath = ".\migrationsettings.template.json"
$destinationJsonFilePath = ".\migrationsettings.json"
$jsonTemplate = Get-Content -Path $jsonFilePath -Raw

$jsonConfig = $jsonTemplate -replace "{{cosmosAccountName}}", $cosmosDBAccountName `
                           -replace "{{cosmosConnectionString}}", $connectionString `
                           -replace "{{databaseName}}", $weatherDatabaseName

$jsonConfig | Out-File -FilePath $destinationJsonFilePath -Encoding utf8

Write-Output "Checking container $containerName..."

try 
{
    Write-Output "Seed data into database $weatherDatabaseName..."
    & "./dmt.exe"
    Write-Output "End seed data into database $weatherDatabaseName..."
    New-Item -Path $flagFilePath -ItemType File -Force | Out-Null
    Remove-Item -Path $destinationJsonFilePath -Force -ErrorAction Stop
    Write-Output "Migration completed successfully. Flag file created."
} 
catch 
{
    Write-Error "An error occurred while seeding data. Exception details: $_"
}