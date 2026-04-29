using AuraCore.Engine.Services;

namespace AuraCore.Web.Services;

public sealed class AuraCoreInitializationService(IServiceScopeFactory scopeFactory)
{
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private Task? initializationTask;

    public bool IsInitialized { get; private set; }

    public async Task EnsureInitializedAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        await semaphore.WaitAsync();
        try
        {
            initializationTask ??= InitializeAsync();
        }
        finally
        {
            semaphore.Release();
        }

        await initializationTask;
    }

    private async Task InitializeAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DataInitializer>();
        await initializer.InitializeAsync();
        IsInitialized = true;
    }
}
