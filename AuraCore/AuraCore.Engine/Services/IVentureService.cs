using AuraCore.Engine.Models;

namespace AuraCore.Engine.Services;

public interface IVentureService
{
    Task<List<ProjectVectorRecord>> GetProjectsAsync();

    Task<List<ProjectVectorRecord>> SearchProjectsAsync(string query);

    Task<ProjectVectorRecord> CreateProjectAsync(string projectName, string clientName, double revenue, string assignedEmployees, string summary);

    Task<ProjectVectorRecord?> DeleteProjectAsync(string projectName);

    Task<ProjectVectorRecord?> AssignEmployeeToProjectAsync(string projectName, string fullName);

    Task<List<ProjectVectorRecord>> RemoveEmployeeFromProjectsAsync(string fullName);
}
