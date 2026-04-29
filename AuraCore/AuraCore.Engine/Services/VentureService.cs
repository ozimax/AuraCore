using AuraCore.Engine.Models;
using Microsoft.Extensions.VectorData;

namespace AuraCore.Engine.Services;

public class VentureService(VectorStoreCollection<Guid, ProjectVectorRecord> collection) : IVentureService
{
    private static readonly char[] EmployeeSeparators = [','];

    public async Task<List<ProjectVectorRecord>> GetProjectsAsync()
    {
        var list = new List<ProjectVectorRecord>();
        var projects = collection.GetAsync(item => true, top: 100);

        await foreach (var project in projects)
        {
            list.Add(project);
        }

        return list
            .OrderBy(project => project.ProjectName)
            .ToList();
    }

    public async Task<List<ProjectVectorRecord>> SearchProjectsAsync(string query)
    {
        var list = new List<ProjectVectorRecord>();
        
        await foreach (var result in collection.SearchAsync(query, 3)) {
            list.Add(result.Record);
        }
        
        return list;
    }

    public async Task<ProjectVectorRecord> CreateProjectAsync(string projectName, string clientName, double revenue, string assignedEmployees, string summary)
    {
        var project = new ProjectVectorRecord
        {
            Id = Guid.NewGuid(),
            ProjectName = projectName,
            ClientName = clientName,
            Revenue = revenue,
            AssignedEmployees = NormalizeEmployeeList(assignedEmployees),
            Summary = summary
        };

        await collection.UpsertAsync(project);

        return project;
    }

    public async Task<ProjectVectorRecord?> DeleteProjectAsync(string projectName)
    {
        var project = await FindProjectByNameAsync(projectName);
        if (project is null)
        {
            return null;
        }

        await collection.DeleteAsync(project.Id);
        return project;
    }

    public async Task<ProjectVectorRecord?> AssignEmployeeToProjectAsync(string projectName, string fullName)
    {
        var project = await FindProjectByNameAsync(projectName);
        if (project is null)
        {
            return null;
        }

        var assignedEmployees = SplitEmployees(project.AssignedEmployees);
        if (!assignedEmployees.Any(employee => string.Equals(employee, fullName.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            assignedEmployees.Add(fullName.Trim());
        }

        var updatedProject = new ProjectVectorRecord
        {
            Id = project.Id,
            ProjectName = project.ProjectName,
            ClientName = project.ClientName,
            Revenue = project.Revenue,
            AssignedEmployees = string.Join(", ", assignedEmployees),
            Summary = project.Summary
        };

        await collection.UpsertAsync(updatedProject);
        return updatedProject;
    }

    public async Task<List<ProjectVectorRecord>> RemoveEmployeeFromProjectsAsync(string fullName)
    {
        var updatedProjects = new List<ProjectVectorRecord>();
        var trimmedFullName = fullName.Trim();
        var projects = await GetProjectsAsync();

        foreach (var project in projects)
        {
            var assignedEmployees = SplitEmployees(project.AssignedEmployees);

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

    private async Task<ProjectVectorRecord?> FindProjectByNameAsync(string projectName)
    {
        var projects = await GetProjectsAsync();

        return projects.FirstOrDefault(project =>
            string.Equals(project.ProjectName, projectName.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeEmployeeList(string assignedEmployees) =>
        string.Join(", ", SplitEmployees(assignedEmployees));

    private static List<string> SplitEmployees(string assignedEmployees) =>
        assignedEmployees
            .Split(EmployeeSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
