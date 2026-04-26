using AuraCore.Engine.Configuration;
using AuraCore.Engine.Extensions;
using AuraCore.Engine.Models;
using AuraCore.Engine.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? "https://ozan-onder-7267-resource.services.ai.azure.com";


var services = new ServiceCollection();
services.AddAuraCoreEngine(new AuraCoreOptions
{
    AzureOpenAiEndpoint = endpoint,
    ChatModel = "gpt-4.1-nano",
    DatabasePath = Path.Combine(AppContext.BaseDirectory, "aura-test.db")
});

await using var scope = services.BuildServiceProvider().CreateAsyncScope();

var hrCollection = scope.ServiceProvider.GetRequiredService<VectorStoreCollection<Guid, EmployeeVectorRecord>>();
var crmCollection = scope.ServiceProvider.GetRequiredService<VectorStoreCollection<Guid, ProjectVectorRecord>>();
var initializer = scope.ServiceProvider.GetRequiredService<DataInitializer>();
var orchestrator = scope.ServiceProvider.GetRequiredService<IOrchestratorAgent>();

await hrCollection.EnsureCollectionExistsAsync();
await crmCollection.EnsureCollectionExistsAsync();



Console.Write("Import Data? (Y/N): ");
string input = Console.ReadLine()?.ToUpperInvariant() ?? "N";

if (input == "Y")
{
    await hrCollection.EnsureCollectionDeletedAsync();
    await crmCollection.EnsureCollectionDeletedAsync();

    await hrCollection.EnsureCollectionExistsAsync();
    await crmCollection.EnsureCollectionExistsAsync();

    if (!Console.IsOutputRedirected)
    {
        Console.Clear();
    }

    await initializer.InitializeAsync();
    Console.WriteLine("\nEmbedding complete...");
}

Console.WriteLine("=== Agent Chat ===");
Console.WriteLine("Type 'exit' to quit.");



while (true)
{
    Console.Write("> ");
    var question = Console.ReadLine();

    if (string.Equals(question, "exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (string.IsNullOrWhiteSpace(question))
    {
        continue;
    }

    var answer = await orchestrator.RunAsync(question);
    Console.WriteLine(answer);
    Console.WriteLine();
}

if (!Console.IsInputRedirected)
{
    Console.WriteLine("Press any key to exit.");
    Console.ReadKey();
}
