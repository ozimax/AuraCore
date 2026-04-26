using AuraCore.Engine.Models;

namespace AuraCore.Engine.Services;

public interface ITalentService
{
    Task<List<EmployeeVectorRecord>> SearchEmployeesAsync(string query);
}
