# Durable Multi Agents

![Image description](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/uuybgktnmsy3q3efegx0.gif)

Durable Multi Agents is an attempt to make use of Azure Durable Functions with Semantic Kernel to build Multi-Agents workflows.

# Highlights

- Proposed solution have implemented different patterns with Multi-Agents , some of them are 
    - Fan-Out/ Fan-in
    - Function Chaining
- NL2SQL is built on top of Semantic layer with the help of Azure Cosmos DB
- Vectorization is done on top of sematic layer to handle similarity based queries
- High scalability of each agents
- Managed Idenitty connectivity to Azure OpenAI and Azure Cosmos DB services
- Provided seed data project for you to get started

# Deploy to Azure

The easiest way to deploy this app is using the [Azure Developer CLI](https://aka.ms/azd).

To provision and deploy:
1) Open a new terminal and do the following from root folder:
```bash
azd up
```

# Steps to run the project locally

Update the `local.settings.json` with the below details

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "APPINSIGHTS_INSTRUMENTATIONKEY": "Your_InstrumentationKey_Here",
    "APPINSIGHTS_CONNECTION_STRING": "Your_ConnectionString_Here",
    "CosmosDBAccountEndpoint": "Your_CosmosDBAccountEndpoint_Here",
    "SubscriptionId": "Your_SubscriptionId_Here",
    "ResourceGroup": "Your_ResourceGroup_Here",
    "DatabaseAccount": "Your_DatabaseAccount_Here",
    "DatabaseId": "Your_DatabaseId_Here",
    "TenantId": "Your_TenantId_Here",
    "PostmarkServerToken": "Your_PostmarkServerToken_Here",
    "FromMailAddress": "Your_FromMailAddress_Here",    
    "cosmosDB__clientSecret": "Your_ClientSecret_Here",
    "cosmosDB__tenantId": "Your_TenantId_Here",
    "cosmosDB__clientId": "Your_ClientId_Here",
    "cosmosDB__accountEndpoint": "Your_CosmosDBAccountEndpoint_Here",
    "OpenAIEndpoint": "Your_OpenAI_Endpoint",
    "OpenAIChatCompletionDeploymentName": "Your_OpenAI_ChatCompletion_Deployment_Name",
    "OpenAITextEmbeddingGenerationDeploymentName": "Your_OpenAI_TextEmbedding_Deployment_Name"
  }
}
```

- To get the `APPINSIGHTS_INSTRUMENTATIONKEY` and `APPINSIGHTS_CONNECTION_STRING` you need to create an Application Insights in Azure and get the key from there
- To get the `CosmosDBAccountEndpoint` you need to create an Azure Cosmos DB account and get the endpoint from there
- To get the `SubscriptionId`, `ResourceGroup`, `DatabaseAccount`, `DatabaseId`, `TenantId`, you need to get the details from Azure Portal
- To get the `PostmarkServerToken` and `FromMailAddress` you need to create an account in Postmark and get the token from there. FromMailAddress should be based on the domain you have registered in Postmark
- To get the `cosmosDB__clientSecret`, `cosmosDB__tenantId`, `cosmosDB__clientId`, `cosmosDB__accountEndpoint` you need to create an Azure AD application and assign it with built-in Cosmos DB Data contributor role.
- To get the `OpenAIEndpoint`, `OpenAIChatCompletionDeploymentName`, `OpenAITextEmbeddingGenerationDeploymentName` you need to create an Azure OpenAI service and create deployments for Chat Completion and Text Embedding Generation
- Once you have all the details updated in the `local.settings.json` you can run the project

# Steps to assign managed identity to Azure OpenAI Services

- Go to Azure Portal
- Search for Azure OpenAI Services
- Select Access Control (IAM)
- Click on Add Role Assignment
- Select the role as Cognitive Services OpenAI Contributor
- Select the managed identity you have created for the Azure Functions, if running locally make sure the logged in user has the required permissions

# Steps to assign managed identity to Azure Cosmos DB

- Go to Azure Portal
- Open Azure Cloud Shell
- Run the below command to assign Cosmos DB data Contributor role
	```bash
	az cosmosdb sql role assignment create --resource-group $resourceGroupName --account-name $cosmosName --role-definition-id "00000000-0000-0000-0000-000000000002" --principal-id "<your-principal-id>" --scope "/"
	```
- Replace the `principal-id` with the managed identity id you have created for the Azure Functions or the Azure AD application's ObjectId if running locally.

# Data Flow

## Travel based query

### Flight Agent flow

![Image description](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/l923gxn4lhq5u3fsezg7.png)

1. The user initiates a request to the ManagerAgent, stating, "I'm planning for a trip from Chennai to Goa this month on the 10th for a 5-day stay."
2. The ManagerAgent receives the request and decides to delegate the task to the travel orchestrator.
3. The travel orchestrator activates and takes charge of the request.
4. The travel orchestrator instructs the FlightAgent to perform flight searches.
5. The FlightAgent sends a query to Azure Cosmos DB to retrieve flights based on the departure city (Chennai) and the departure date (10th of the month).
6. Azure Cosmos DB processes the query and returns the relevant flight information to the FlightAgent.
7. The FlightAgent sends another query to Azure Cosmos DB to retrieve flights based on the destination city (Goa) and the duration of the stay (5 days).
8. Azure Cosmos DB processes the query and returns the relevant flight information to the FlightAgent.
9. The FlightAgent consolidates the flight information and sends it back to the travel orchestrator.
10. The travel orchestrator returns the final flight search results to the ManagerAgent.
11. The ManagerAgent sends the final response back to the user.

![Image description](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/4x2k8j0d37a1laagkz46.png)

### Weather Agent flow

![Image description](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/1zcypkt5vv3zzp7ppbxm.png)

1. The user initiates a request to the ManagerAgent, asking, "Can you let me know how is the weather?"
2. The ManagerAgent receives the request and decides to delegate the task to the travel orchestrator.
3. The travel orchestrator activates and takes charge of the request.
4. The travel orchestrator instructs the WeatherAgent to perform weather searches.
5. The WeatherAgent sends a query to Azure Cosmos DB to retrieve weather conditions based on the departure city and the departure date.
6. Azure Cosmos DB processes the query and returns the relevant weather information to the WeatherAgent.
7. The WeatherAgent sends another query to Azure Cosmos DB to retrieve weather conditions based on the destination city and the return date.
8. Azure Cosmos DB processes the query and returns the relevant weather information to the WeatherAgent.
9. The WeatherAgent consolidates the weather information and sends it back to the travel orchestrator.
10. The travel orchestrator returns the final weather search results to the ManagerAgent.
11. The ManagerAgent sends the final response back to the user.


![Image description](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/u21unebmccg8ww8c0ktc.png)


### Booking flow

![Image description](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/q4n93u2v2zqcbs2a9niw.png)
![Image description](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/r2t3o53oc3greupyrv4b.png)





## Similarity based query 

![Image description](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/5rbdwe8cwlt5b26ibkvy.png)


User query : "Have I visited any beach destinations in the past?"

1. The user initiates a request to the ManagerAgent, asking, "Have I visited any beach destinations in the past?"
2. The ManagerAgent receives the user’s request and decides to delegate the task to the default orchestrator.
3. The ManagerAgent activates the Default Orchestrator to handle the request.
4. The Default Orchestrator instructs the SemanticLayerAgent to perform a Natural Language to SQL (NL2SQL) conversion and fetch the relevant data.
5. The SemanticLayerAgent processes the NL2SQL query conversion and sends a query to Azure Cosmos DB.
6. Azure Cosmos DB processes the SQL query and returns the requested data to the SemanticLayerAgent.
7. Simultaneously, the Default Orchestrator instructs the SemanticVectorAgent to perform a vector similarity search.
8. The SemanticVectorAgent creates a vectorized query and sends it to Azure Cosmos DB for searching against embeddings.
9. Azure Cosmos DB processes the vectorized query and returns the relevant data to the SemanticVectorAgent.
10. The SemanticLayerAgent and SemanticVectorAgent both send their responses to the ConsolidatorAgent.
11. The ConsolidatorAgent combines the responses from both agents to form a cohesive response.
12. The ConsolidatorAgent returns the consolidated response to the ManagerAgent.
13. The ManagerAgent sends the final response back to the user.

![Image description](https://dev-to-uploads.s3.amazonaws.com/uploads/articles/j2fpbhgwko9cn5vmz6o9.png)

# Sponsor

Leave a ⭐ if you like this project

<a href="https://www.buymeacoffee.com/divakarkumar" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" alt="Buy Me A Coffee" style="height: 40px !important;width: 145 !important;" ></a>


&copy; [Divakar Kumar](//github.com/Divakar-Kumar)

# Contact

[Website](//iamdivakarkumar.com) | [LinkedIn](https://www.linkedin.com/in/divakar-kumar/)
