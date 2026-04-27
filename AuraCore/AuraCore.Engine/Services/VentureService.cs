using AuraCore.Engine.Models;
using Microsoft.Extensions.VectorData;

namespace AuraCore.Engine.Services;

public class VentureService(VectorStoreCollection<Guid, ProjectVectorRecord> collection) : IVentureService
{
    private static readonly char[] EmployeeSeparators = [','];

    public async Task<List<ProjectVectorRecord>> SearchProjectsAsync(string query)
    {
        var list = new List<ProjectVectorRecord>();
        
        await foreach (var result in collection.SearchAsync(query, 3)) {
            list.Add(result.Record);
        }
        
        return list;
    }

    public async Task<List<ProjectVectorRecord>> RemoveEmployeeFromProjectsAsync(string fullName)
    {
        var updatedProjects = new List<ProjectVectorRecord>();
        var trimmedFullName = fullName.Trim();
        var projects = collection.GetAsync(item => true, top: 100);

        await foreach (var project in projects)
        {
            var assignedEmployees = project.AssignedEmployees
                .Split(EmployeeSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            var remainingEmployees = assignedEmployees
                .Where(employee => !string.Equals(employee, trimmedFullName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (remainingEmployees.Count == assignedEmployees.Count)
            {
                continue;
            }

            var updatedProject = new ProjectVectorRecord
            {
                Id = project.Id,
                ProjectName = project.ProjectName,
                ClientName = project.ClientName,
                Revenue = project.Revenue,
                AssignedEmployees = string.Join(", ", remainingEmployees),
                Summary = project.Summary
            };

            updatedProjects.Add(updatedProject);
        }

        foreach (var updatedProject in updatedProjects)
        {
            await collection.UpsertAsync(updatedProject);
        }

        return updatedProjects;
    }

}
