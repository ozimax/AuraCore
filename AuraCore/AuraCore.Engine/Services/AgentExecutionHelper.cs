using AuraCore.Engine.Configuration;

namespace AuraCore.Engine.Services;

internal static class AgentExecutionHelper
{
    public static CancellationToken CreateTimeoutToken(AuraCoreOptions options, CancellationToken cancellationToken, out CancellationTokenSource? linkedCts)
    {
        linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(TimeSpan.FromSeconds(options.ChatTimeoutSeconds));
        return linkedCts.Token;
    }
}
