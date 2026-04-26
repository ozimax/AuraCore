namespace AuraCore.Engine.Services;

public interface IAgent
{
    Task<string> RunAsync(string input, CancellationToken cancellationToken = default);
}
