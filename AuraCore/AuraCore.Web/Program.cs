using AuraCore.Engine.Configuration;
using AuraCore.Engine.Extensions;
using AuraCore.Engine.Services;
using AuraCore.Web.Components;
using AuraCore.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuraCoreEngine(new AuraCoreOptions
{
    AzureOpenAiEndpoint = builder.Configuration["AzureOpenAI:Endpoint"]
        ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
        ?? "https://ozan-onder-7267-resource.services.ai.azure.com",
    ChatModel = builder.Configuration["AzureOpenAI:ChatModel"] ?? "gpt-4.1-nano",
    DatabasePath = Path.Combine(Path.GetTempPath(), "aura-core-data.db")
});

builder.Services.AddSingleton<AuraCoreInitializationService>();
builder.Services.AddScoped<IAgentActivitySink, AgentActivityFeed>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
