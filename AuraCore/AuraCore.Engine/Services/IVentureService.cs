using AuraCore.Engine.Models;

namespace AuraCore.Engine.Services;

public interface IVentureService
{
    Task<List<ProjectVectorRecord>> SearchProjectsAsync(string query);

    Task<List<ProjectVectorRecord>> RemoveEmployeeFromProjectsAsync(string fullName);
}
