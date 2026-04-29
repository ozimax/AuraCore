namespace AuraCore.Engine.Services;

public sealed record AgentActivity(string Source, string Message);

public interface IAgentActivitySink
{
    event Action<AgentActivity>? ActivityWritten;

    void Write(string source, string message);
}

internal sealed class NullAgentActivitySink : IAgentActivitySink
{
    public event Action<AgentActivity>? ActivityWritten
    {
        add { }
        remove { }
    }

    public void Write(string source, string message)
    {
    }
}
