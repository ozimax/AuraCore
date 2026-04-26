using AuraCore.Engine.Configuration;
using Microsoft.Extensions.AI;

namespace AuraCore.Engine.Services;

public class OrchestratorAgent(IHrAgent hrAgent, IProjectAgent projectAgent, IChatClient chatClient, AuraCoreOptions options) : IOrchestratorAgent
{
    private enum AgentRoute
    {
        Hr,
        Projects,
        Both
    }


    private const string RoutingInstructions =
        """
        Decide which AuraCore specialist should answer the user's question.

        Return exactly one word:
        HR - for employee, people, skills, staffing, job titles, roles, or talent questions.
        PROJECTS - for project, client, revenue, delivery, assignment, or CRM questions.
        BOTH - when the question needs both employee and project context, or when unsure.

        Important:
        If the user asks who has a role or job title, choose HR even when the title contains words like project or manager.
        If the user asks about job titles, roles, or skills of people assigned to projects, choose BOTH.
        Choose PROJECTS only when the user asks about project records, clients, revenue, delivery status, or project assignments.
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
        var route = await ChooseRouteAsync(input, cancellationToken);

        if (route == AgentRoute.Hr)
        {
            return await hrAgent.RunAsync(input, cancellationToken);
        }

        if (route == AgentRoute.Projects)
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




    private async Task<AgentRoute> ChooseRouteAsync(string input, CancellationToken cancellationToken)
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
            return ParseRoute(response.Text);
        }
    }




    private static AgentRoute ParseRoute(string? route)
    {
        return route?.Trim().ToUpperInvariant() switch
        {
            "HR" => AgentRoute.Hr,
            "PROJECTS" => AgentRoute.Projects,
            _ => AgentRoute.Both
        };
    }













}
