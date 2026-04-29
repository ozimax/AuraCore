using AuraCore.Engine.Configuration;
using Microsoft.Extensions.AI;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AuraCore.Engine.Services;

public class OrchestratorAgent(
    IHrAgent hrAgent,
    IProjectAgent projectAgent,
    IChatClient chatClient,
    AuraCoreOptions options,
    IAgentActivitySink activitySink) : IOrchestratorAgent
{
    private enum OrchestratorWorkflow
    {
        HrQuery,
        ProjectQuery,
        BothQuery,
        RemoveEmployee
    }


    private const string RoutingInstructions =
        """
        Classify the user's request into one AuraCore workflow.

        Return only valid JSON in this exact shape:
        {"workflow":"HR_QUERY"}

        Valid workflow values:
        HR_QUERY - for employee, people, skills, staffing, job titles, roles, or talent questions.
        PROJECT_QUERY - for project, client, revenue, delivery, assignment, or CRM questions.
        BOTH_QUERY - when the question needs both employee and project context, or when unsure.
        REMOVE_EMPLOYEE - when the user asks to remove, delete, or offboard an employee from the company, HR database, staff, or employee records.

        Important:
        If the user asks to create, add, register, delete, or remove a project, choose PROJECT_QUERY.
        If the user asks to assign an employee to a project, choose PROJECT_QUERY.
        If the user asks to remove an employee from project assignments only, choose PROJECT_QUERY.
        If the user asks who has a role or job title, choose HR_QUERY even when the title contains words like project.
        If the user asks about job titles, roles, or skills of people assigned to projects, choose BOTH_QUERY.
        Choose PROJECT_QUERY only when the user asks about project records, clients, revenue, delivery status, or project assignments.
        """;

    private const string SynthesisInstructions =
        """
        You are the AuraCore orchestrator.
        Combine the specialist agent outputs into one final answer.
        Keep the answer concise and based only on the supplied agent outputs.
        If there is not enough information, say so.
        """;


    public async Task<string> RunAsync(string input, CancellationToken cancellationToken = default)
    {
        if (IsCapabilityRequest(input))
        {
            return CapabilitySummary;
        }

        var workflow = await ChooseWorkflowAsync(input, cancellationToken);

        if (workflow == OrchestratorWorkflow.RemoveEmployee)
        {
            return await RunEmployeeRemovalWorkflowAsync(input, cancellationToken);
        }

        if (workflow == OrchestratorWorkflow.HrQuery)
        {
            activitySink.Write("orchestrator", "Calling HR agent");
            return await hrAgent.RunAsync(input, cancellationToken);
        }

        if (workflow == OrchestratorWorkflow.ProjectQuery)
        {
            activitySink.Write("orchestrator", "Calling project agent");
            return await projectAgent.RunAsync(input, cancellationToken);
        }

        activitySink.Write("orchestrator", "Calling project agent");
        var projectResult = await projectAgent.RunAsync(input, cancellationToken);
        activitySink.Write("orchestrator", "Calling HR agent");
        var hrResult = await hrAgent.RunAsync(
            $"User question: {input}\n\nRelevant project information:\n{projectResult}",
            cancellationToken);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SynthesisInstructions),
            new(ChatRole.User, $"User question: {input}"),
            new(ChatRole.User, $"HR agent output:\n{hrResult}"),
            new(ChatRole.User, $"Project agent output:\n{projectResult}")
        };

        var token = AgentExecutionHelper.CreateTimeoutToken(options, cancellationToken, out var linkedCts);
        using (linkedCts)
        {
            var response = await chatClient.GetResponseAsync(messages, cancellationToken: token);
            return response.Text ?? string.Empty;
        }
    }



    private async Task<string> RunEmployeeRemovalWorkflowAsync(string input, CancellationToken cancellationToken)
    {
        activitySink.Write("orchestrator", "Calling HR agent");
        var hrResult = await hrAgent.RunAsync(input, cancellationToken);
        activitySink.Write("orchestrator", "Calling project agent");
        var projectResult = await projectAgent.RunAsync(
            $"Remove the employee named in this request from all project assignments. User request: {input}",
            cancellationToken);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SynthesisInstructions),
            new(ChatRole.User, $"User question: {input}"),
            new(ChatRole.User, $"HR agent output:\n{hrResult}"),
            new(ChatRole.User, $"Project agent output:\n{projectResult}")
        };

        var token = AgentExecutionHelper.CreateTimeoutToken(options, cancellationToken, out var linkedCts);
        using (linkedCts)
        {
            var response = await chatClient.GetResponseAsync(messages, cancellationToken: token);
            return response.Text ?? string.Empty;
        }
    }



    private async Task<OrchestratorWorkflow> ChooseWorkflowAsync(string input, CancellationToken cancellationToken)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, RoutingInstructions),
            new(ChatRole.User, input)
        };

        var token = AgentExecutionHelper.CreateTimeoutToken(options, cancellationToken, out var linkedCts);
        using (linkedCts)
        {
            var response = await chatClient.GetResponseAsync(messages, cancellationToken: token);
            return ParseWorkflow(response.Text);
        }
    }


    private static OrchestratorWorkflow ParseWorkflow(string? responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return OrchestratorWorkflow.BothQuery;
        }

        try
        {
            var route = JsonSerializer.Deserialize<WorkflowDecision>(responseText);
            return route?.Workflow?.Trim().ToUpperInvariant() switch
            {
                "HR_QUERY" => OrchestratorWorkflow.HrQuery,
                "PROJECT_QUERY" => OrchestratorWorkflow.ProjectQuery,
                "REMOVE_EMPLOYEE" => OrchestratorWorkflow.RemoveEmployee,
                _ => OrchestratorWorkflow.BothQuery
            };
        }
        catch (JsonException)
        {
            return OrchestratorWorkflow.BothQuery;
        }
    }

    private const string CapabilitySummary =
        """
        I am the AuraCore orchestrator agent. I route your request to the right specialist agent and combine their answers when a question needs both HR and project context.

        I can help with:
        - Employee questions: list employees, search people by role or skills, and answer questions about employee records.
        - Employee changes: create a new employee when you provide a full name, job title, and skills or summary; delete or offboard an employee by full name.
        - Project questions: list projects, search projects by client, revenue, delivery details, or assigned employees.
        - Project changes: create a project when you provide a project name, client name, revenue, and summary; delete a project by project name.
        - Assignment updates: assign an employee to a project by project name and employee full name; remove an employee from project assignments when you provide the employee's full name.
        """;

    private static bool IsCapabilityRequest(string input)
    {
        var normalizedInput = input.ToLowerInvariant();

        return ContainsAny(normalizedInput, "what can you do", "your capabilities", "what are your capabilities", "help", "about yourself", "who are you", "what do you do")
            || (ContainsAny(normalizedInput, "agent", "orchestrator", "yourself")
                && ContainsAny(normalizedInput, "capable", "capabilities", "do", "help", "about"));
    }

    private static bool ContainsAny(string input, params string[] terms) =>
        terms.Any(term => input.Contains(term, StringComparison.OrdinalIgnoreCase));

    private sealed class WorkflowDecision
    {
        [JsonPropertyName("workflow")]
        public string? Workflow { get; set; }
    }

}
