using System.Text;
using AuraCore.Engine.Configuration;
using Microsoft.Extensions.AI;

namespace AuraCore.Engine.Services;

public class ProjectAgent(
    IVentureService ventureService,
    IChatClient chatClient,
    AuraCoreOptions options) : IProjectAgent
{
    private const string Instructions =
        """
        You are the CRM projects agent for AuraCore.
        Answer only from the provided project context.
        If the context is insufficient, say that the project dataset does not contain enough information.
        When the user clearly asks to add, create, or register a new project, use the create_project tool.
        Do not create a project unless the user provides a project name, client name, revenue, and summary. Assigned employees may be empty.
        When the user clearly asks to delete or remove a project, use the delete_project tool.
        Do not delete a project unless the user provides the project name.
        When the user clearly asks to assign an employee to a project, use the assign_employee_to_project tool.
        Do not assign an employee unless the user provides the employee's full name and the project name.
        When the user clearly asks to remove an employee from project assignments, use the remove_employee_from_projects tool.
        Do not update project assignments unless the user provides the employee's full name.
        Keep answers concise and factual.
        """;

    public async Task<string> RunAsync(string input, CancellationToken cancellationToken = default)
    {
        if (IsProjectListRequest(input))
        {
            var allProjects = await ventureService.GetProjectsAsync();
            return FormatProjectList(allProjects);
        }

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
                    CreateProjectToolAsync,
                    "create_project",
                    "Creates a new project record in the CRM database. Use only when the user clearly asks to add, create, or register a project."),
                AIFunctionFactory.Create(
                    DeleteProjectToolAsync,
                    "delete_project",
                    "Deletes a project record from the CRM database by project name. Use only when the user clearly asks to delete or remove a project."),
                AIFunctionFactory.Create(
                    AssignEmployeeToProjectToolAsync,
                    "assign_employee_to_project",
                    "Assigns an employee to an existing project by project name and employee full name."),
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

    private async Task<string> CreateProjectToolAsync(string projectName, string clientName, double revenue, string summary, string assignedEmployees = "")
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new ArgumentException("Project name is required.", nameof(projectName));
        }

        if (string.IsNullOrWhiteSpace(clientName))
        {
            throw new ArgumentException("Client name is required.", nameof(clientName));
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Project summary is required.", nameof(summary));
        }

        var project = await ventureService.CreateProjectAsync(
            projectName.Trim(),
            clientName.Trim(),
            revenue,
            assignedEmployees.Trim(),
            summary.Trim());

        return $"{project.ProjectName} was created for {project.ClientName}.";
    }

    private async Task<string> DeleteProjectToolAsync(string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new ArgumentException("Project name is required.", nameof(projectName));
        }

        var deletedProject = await ventureService.DeleteProjectAsync(projectName.Trim());
        if (deletedProject is null)
        {
            return $"No project record was found for {projectName.Trim()}.";
        }

        return $"{deletedProject.ProjectName} was deleted from the project database.";
    }

    private async Task<string> AssignEmployeeToProjectToolAsync(string projectName, string fullName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new ArgumentException("Project name is required.", nameof(projectName));
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Employee full name is required.", nameof(fullName));
        }

        var updatedProject = await ventureService.AssignEmployeeToProjectAsync(projectName.Trim(), fullName.Trim());
        if (updatedProject is null)
        {
            return $"No project record was found for {projectName.Trim()}.";
        }

        return $"{fullName.Trim()} is assigned to {updatedProject.ProjectName}.";
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

    private static bool IsProjectListRequest(string input)
    {
        var normalizedInput = input.ToLowerInvariant();

        return ContainsAny(normalizedInput, "all", "list", "show", "projects", "portfolio")
            && ContainsAny(normalizedInput, "project", "projects", "portfolio");
    }

    private static string FormatProjectList(IReadOnlyCollection<Models.ProjectVectorRecord> projects)
    {
        if (projects.Count == 0)
        {
            return "There are no projects in the project dataset.";
        }

        var response = new StringBuilder();
        response.AppendLine($"Projects ({projects.Count}):");

        foreach (var project in projects)
        {
            response.AppendLine($"- {project.ProjectName} - {project.ClientName}, revenue {project.Revenue:C0}, assigned to {project.AssignedEmployees}: {project.Summary}");
        }

        return response.ToString().TrimEnd();
    }

    private static bool ContainsAny(string input, params string[] terms) =>
        terms.Any(term => input.Contains(term, StringComparison.OrdinalIgnoreCase));
}
























