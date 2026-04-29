using AuraCore.Engine.Services;

namespace AuraCore.Web.Services;

public sealed class AgentActivityFeed : IAgentActivitySink
{
    public event Action<AgentActivity>? ActivityWritten;

    public void Write(string source, string message)
    {
        ActivityWritten?.Invoke(new AgentActivity(source, message));
    }
}
