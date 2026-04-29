# AuraCore

AuraCore is a .NET 10 agent demo that routes HR and project questions through an orchestrator. It uses Azure OpenAI for chat and embeddings, Semantic Kernel services, and a local SQLite vector store seeded from JSON data.

## Projects

- `AuraCore.Engine` contains the agent orchestration, HR/project services, Azure OpenAI client setup, and SQLite vector store integration.
- `AuraCore.Web` is the Blazor web app for chatting with the orchestrator.
- `AuraCore.Console` is a console runner for local testing.

## Requirements

- .NET SDK 10
- Azure CLI, if deploying to Azure App Service
- Access to an Azure AI/OpenAI resource with these deployments:
  - `gpt-4.1-nano`
  - `text-embedding-3-small`

The app uses `DefaultAzureCredential`, so local development uses your signed-in Azure identity and Azure App Service uses its managed identity.

## Configuration

Set the Azure OpenAI endpoint before running locally:

```powershell
$env:AZURE_OPENAI_ENDPOINT = "https://<your-resource>.cognitiveservices.azure.com/"
```

For App Service, use app settings:

```powershell
az webapp config appsettings set `
  --resource-group app-grp `
  --name Projectauracore `
  --settings AzureOpenAI__Endpoint="https://<your-resource>.cognitiveservices.azure.com/" AzureOpenAI__ChatModel="gpt-4.1-nano"
```

The App Service managed identity must have the `Cognitive Services OpenAI User` role on the Azure AI/OpenAI resource.

## Run Locally

Run the web app:

```powershell
dotnet run --project .\AuraCore.Web\AuraCore.Web.csproj
```

Run the console app:

```powershell
dotnet run --project .\AuraCore.Console\AuraCore.Console.csproj
```

The first initialization loads `AuraCore.Engine\Data\employees.json` and `AuraCore.Engine\Data\projects.json`, creates embeddings, and stores them in a SQLite database under the temp folder by default.

## Deploy to Azure App Service

Publish and package the web app:

```powershell
dotnet publish .\AuraCore.Web\AuraCore.Web.csproj -c Release -o .\publish
Compress-Archive -Path .\publish\* -DestinationPath .\auracore-web.zip -Force
```

Deploy to the existing App Service:

```powershell
az webapp deploy `
  --resource-group app-grp `
  --name Projectauracore `
  --src-path .\auracore-web.zip `
  --type zip
```

Restart if needed:

```powershell
az webapp restart --resource-group app-grp --name Projectauracore
```

## Generated Files

The repository ignores generated deployment and local emulator files:

- `publish/`
- `*.zip`
- `__azurite_db_*.json`
- `__queuestorage__/`

These files are local build or Azurite state and should not be committed.
