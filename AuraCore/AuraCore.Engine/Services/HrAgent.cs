using System.Text;
using AuraCore.Engine.Configuration;
using AuraCore.Engine.Models;
using Microsoft.Extensions.AI;

namespace AuraCore.Engine.Services;

public class HrAgent(
    ITalentService talentService,
    IChatClient chatClient,
    AuraCoreOptions options) : IHrAgent
{
    private const string Instructions =
        """
        You are the HR agent for AuraCore.
        For employee questions, answer only from the provided employee context.
        If the context is insufficient for a question, say that the HR dataset does not contain enough information.
        When the user clearly asks to add, create, or register a new employee, use the create_employee tool.
        Do not create an employee unless the user provides a full name, job title, and skills or summary.
        When the user clearly asks to remove, delete, or offboard an employee, use the delete_employee tool.
        Do not delete an employee unless the user provides the employee's full name.
        Keep answers concise and factual.
        """;


    public async Task<string> RunAsync(string input, CancellationToken cancellationToken = default)
    {
        if (IsEmployeeListRequest(input))
        {
            var allEmployees = await talentService.GetEmployeesAsync();
            return FormatEmployeeList(allEmployees);
        }

        var employees = await talentService.SearchEmployeesAsync(input);

        var context = new StringBuilder();
        foreach (var employee in employees)
        {
            context.AppendLine($"Name: {employee.FullName}");
            context.AppendLine($"Title: {employee.JobTitle}");
            context.AppendLine($"Summary: {employee.Summary}");
            context.AppendLine();
        }

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, Instructions),
            new(ChatRole.User, $"Employee context:\n{context}"),
            new(ChatRole.User, input)
        };

        var chatOptions = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(
                    CreateEmployeeToolAsync,
                    "create_employee",
                    "Creates a new employee record in the HR database. Use only when the user clearly asks to add, create, or register a new employee."),
                AIFunctionFactory.Create(
                    DeleteEmployeeToolAsync,
                    "delete_employee",
                    "Deletes an employee record from the HR database. Use only when the user clearly asks to remove, delete, or offboard an employee.")
            ]
        };

        var token = AgentExecutionHelper.CreateTimeoutToken(options, cancellationToken, out var linkedCts);
        
        using (linkedCts)
        {
            var toolCallingClient = new FunctionInvokingChatClient(chatClient) { MaximumIterationsPerRequest = 3 };

            var response = await toolCallingClient.GetResponseAsync(messages, chatOptions, token);
            return response.Text ?? string.Empty;
        }
    }



    private async Task<EmployeeVectorRecord> CreateEmployeeToolAsync(string fullName, string jobTitle, string summary)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Employee full name is required.", nameof(fullName));
        }

        if (string.IsNullOrWhiteSpace(jobTitle))
        {
            throw new ArgumentException("Employee job title is required.", nameof(jobTitle));
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Employee skills or summary is required.", nameof(summary));
        }

        return await talentService.CreateEmployeeAsync(fullName.Trim(), jobTitle.Trim(), summary.Trim());
    }

    private async Task<string> DeleteEmployeeToolAsync(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Employee full name is required.", nameof(fullName));
        }

        var deletedEmployee = await talentService.DeleteEmployeeAsync(fullName.Trim());
        if (deletedEmployee is null)
        {
            return $"No HR employee record was found for {fullName.Trim()}.";
        }

        return $"{deletedEmployee.FullName} was deleted from the HR database.";
    }

    private static bool IsEmployeeListRequest(string input)
    {
        var normalizedInput = input.ToLowerInvariant();

        return ContainsAny(normalizedInput, "all", "list", "show", "everyone", "employees", "staff", "people")
            && ContainsAny(normalizedInput, "employee", "employees", "staff", "people", "everyone");
    }

    private static string FormatEmployeeList(IReadOnlyCollection<EmployeeVectorRecord> employees)
    {
        if (employees.Count == 0)
        {
            return "There are no employees in the HR dataset.";
        }

        var response = new StringBuilder();
        response.AppendLine($"Employees ({employees.Count}):");

        foreach (var employee in employees)
        {
            response.AppendLine($"- {employee.FullName} - {employee.JobTitle}: {employee.Summary}");
        }

        return response.ToString().TrimEnd();
    }

    private static bool ContainsAny(string input, params string[] terms) =>
        terms.Any(term => input.Contains(term, StringComparison.OrdinalIgnoreCase));
}

















