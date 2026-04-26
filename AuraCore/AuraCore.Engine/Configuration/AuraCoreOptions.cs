namespace AuraCore.Engine.Configuration;

public class AuraCoreOptions
{
    public required string AzureOpenAiEndpoint { get; init; }

    public string ChatModel { get; init; } = "gpt-4.1-nano";

    public int ChatTimeoutSeconds { get; init; } = 30;

    public string EmbeddingModel { get; init; } = "text-embedding-3-small";

    public string DatabasePath { get; init; } = Path.Combine(Path.GetTempPath(), "aura-core-data.db");

    public string HrCollectionName { get; init; } = "hr_talents";

    public string CrmCollectionName { get; init; } = "crm_projects";
}
