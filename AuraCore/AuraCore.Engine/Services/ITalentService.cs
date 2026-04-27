using AuraCore.Engine.Models;

namespace AuraCore.Engine.Services;

public interface ITalentService
{
    Task<List<EmployeeVectorRecord>> SearchEmployeesAsync(string query);

    Task<EmployeeVectorRecord> CreateEmployeeAsync(string fullName, string jobTitle, string summary);

    Task<EmployeeVectorRecord?> DeleteEmployeeAsync(string fullName);
}
