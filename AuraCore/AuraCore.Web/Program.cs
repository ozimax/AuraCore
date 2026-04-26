using AuraCore.Engine.Configuration;
using AuraCore.Engine.Extensions;
using AuraCore.Engine.Services;
using AuraCore.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuraCoreEngine(new AuraCoreOptions
{
    AzureOpenAiEndpoint = builder.Configuration["AzureOpenAI:Endpoint"] ?? "YOUR_ENDPOINT",
    DatabasePath = Path.Combine(Path.GetTempPath(), "aura-core-data.db")
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DataInitializer>();
    await initializer.InitializeAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
