using AuraCore.Engine.Configuration;
using Microsoft.Extensions.AI;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AuraCore.Engine.Services;

public class OrchestratorAgent(IHrAgent hrAgent, IProjectAgent projectAgent, IChatClient chatClient, AuraCoreOptions options) : IOrchestratorAgent
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
        var workflow = await ChooseWorkflowAsync(input, cancellationToken);

        if (workflow == OrchestratorWorkflow.RemoveEmployee)
        {
            return await RunEmployeeRemovalWorkflowAsync(input, cancellationToken);
        }

        if (workflow == OrchestratorWorkflow.HrQuery)
        {
            return await hrAgent.RunAsync(input, cancellationToken);
        }

        if (workflow == OrchestratorWorkflow.ProjectQuery)
        {
            return await projectAgent.RunAsync(input, cancellationToken);
        }

        var projectResult = await projectAgent.RunAsync(input, cancellationToken);
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
        var hrResult = await hrAgent.RunAsync(input, cancellationToken);
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


    private sealed class WorkflowDecision
    {
        [JsonPropertyName("workflow")]
        public string? Workflow { get; set; }
    }

}
