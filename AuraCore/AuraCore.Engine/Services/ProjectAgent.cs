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
        When the user clearly asks to remove an employee from project assignments, use the remove_employee_from_projects tool.
        Do not update project assignments unless the user provides the employee's full name.
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

        var chatOptions = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(
                    RemoveEmployeeFromProjectsToolAsync,
                    "remove_employee_from_projects",
                    "Removes an employee from every project assignment in the CRM database.")
            ]
        };

        var token = AgentExecutionHelper.CreateTimeoutToken(options, cancellationToken, out var linkedCts);
        using (linkedCts)
        {
            var toolCallingClient = new FunctionInvokingChatClient(chatClient) {  MaximumIterationsPerRequest = 3};

            var response = await toolCallingClient.GetResponseAsync(messages, chatOptions, token);
            return response.Text ?? string.Empty;
        }
    }

    private async Task<string> RemoveEmployeeFromProjectsToolAsync(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Employee full name is required.", nameof(fullName));
        }

        var updatedProjects = await ventureService.RemoveEmployeeFromProjectsAsync(fullName.Trim());
        if (updatedProjects.Count == 0)
        {
            return $"{fullName.Trim()} was not assigned to any projects.";
        }

        var projectNames = string.Join(", ", updatedProjects.Select(project => project.ProjectName));
        return $"{fullName.Trim()} was removed from these projects: {projectNames}.";
    }
}





























