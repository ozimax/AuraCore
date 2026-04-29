using AuraCore.Engine.Configuration;
using AuraCore.Engine.Models;
using AuraCore.Engine.Services;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.SqliteVec;

namespace AuraCore.Engine.Extensions;

public static class AuraCoreServiceCollectionExtensions
{
    public static IServiceCollection AddAuraCoreEngine(this IServiceCollection services, AuraCoreOptions options)
    {
        services.AddSingleton(options);

        services.AddSingleton(sp =>
            new AzureOpenAIClient(new Uri(options.AzureOpenAiEndpoint), new DefaultAzureCredential()));

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
            sp.GetRequiredService<AzureOpenAIClient>()
                .GetEmbeddingClient(options.EmbeddingModel)
                .AsIEmbeddingGenerator());

        services.AddSingleton<IChatClient>(sp =>
            sp.GetRequiredService<AzureOpenAIClient>()
                .GetChatClient(options.ChatModel)
                .AsIChatClient());

        services.AddSingleton<VectorStore>(sp =>
            new SqliteVectorStore(
                $"Data Source={options.DatabasePath};",
                new SqliteVectorStoreOptions
                {
                    EmbeddingGenerator = sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>()
                }));

        services.AddSingleton(sp =>
            sp.GetRequiredService<VectorStore>()
                .GetCollection<Guid, EmployeeVectorRecord>(options.HrCollectionName));

        services.AddSingleton(sp =>
            sp.GetRequiredService<VectorStore>()
                .GetCollection<Guid, ProjectVectorRecord>(options.CrmCollectionName));

        services.AddScoped<ITalentService, TalentService>();
        services.AddScoped<IVentureService, VentureService>();
        services.TryAddScoped<IAgentActivitySink, NullAgentActivitySink>();
        services.AddScoped<IHrAgent, HrAgent>();
        services.AddScoped<IProjectAgent, ProjectAgent>();
        services.AddScoped<IOrchestratorAgent, OrchestratorAgent>();
        services.AddScoped<DataInitializer>();

        return services;
    }
}
