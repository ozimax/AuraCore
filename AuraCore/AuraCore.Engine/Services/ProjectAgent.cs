using System.Text;
using AuraCore.Engine.Configuration;
using Microsoft.Extensions.AI;

namespace AuraCore.Engine.Services;

public class ProjectAgent(IVentureService ventureService, IChatClient chatClient, AuraCoreOptions options) : IProjectAgent
{
    private const string Instructions =
        """
        You are the CRM projects agent for AuraCore.
        Answer only from the provided project context.
        If the context is insufficient, say that the project dataset does not contain enough information.
        Keep answers concise and factual.
        """;

    public async Task<string> RunAsync(string input, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("****PROJECT AGENT CALLED****");
        var projects = await ventureService.SearchProjectsAsync(input);

        var context = new StringBuilder();
        foreach (var project in projects)
        {
            context.AppendLine($"Project: {project.ProjectName}");
            context.AppendLine($"Client: {project.ClientName}");
            context.AppendLine($"Revenue: {project.Revenue}");
            context.AppendLine($"Assigned Employees: {project.AssignedEmployees}");
            context.AppendLine($"Summary: {project.Summary}");
            context.AppendLine();
        }

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, Instructions),
            new(ChatRole.User, $"Project context:\n{context}"),
            new(ChatRole.User, input)
        };

        var token = AgentExecutionHelper.CreateTimeoutToken(options, cancellationToken, out var linkedCts);
        using (linkedCts)
        {
            var response = await chatClient.GetResponseAsync(messages, cancellationToken: token);
            return response.Text ?? string.Empty;
        }
    }
}
