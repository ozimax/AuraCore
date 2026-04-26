using System.Text;
using AuraCore.Engine.Configuration;
using Microsoft.Extensions.AI;

namespace AuraCore.Engine.Services;

public class HrAgent(ITalentService talentService, IChatClient chatClient, AuraCoreOptions options) : IHrAgent
{
    private const string Instructions =
        """
        You are the HR agent for AuraCore.
        Answer only from the provided employee context.
        If the context is insufficient, say that the HR dataset does not contain enough information.
        Keep answers concise and factual.
        """;

    public async Task<string> RunAsync(string input, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("****HR AGENT CALLED****");
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

        var token = AgentExecutionHelper.CreateTimeoutToken(options, cancellationToken, out var linkedCts);
        using (linkedCts)
        {
            var response = await chatClient.GetResponseAsync(messages, cancellationToken: token);
            return response.Text ?? string.Empty;
        }
    }
}
