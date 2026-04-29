using AuraCore.Engine.Models;
using Microsoft.Extensions.VectorData;

namespace AuraCore.Engine.Services;

public class TalentService(VectorStoreCollection<Guid, EmployeeVectorRecord> collection) : ITalentService
{
    public async Task<List<EmployeeVectorRecord>> GetEmployeesAsync()
    {
        var list = new List<EmployeeVectorRecord>();
        var employees = collection.GetAsync(item => true, top: 100);

        await foreach (var employee in employees)
        {
            list.Add(employee);
        }

        return list
            .OrderBy(employee => employee.FullName)
            .ToList();
    }

    public async Task<EmployeeVectorRecord> CreateEmployeeAsync(string fullName, string jobTitle, string summary)
    {
        var employee = new EmployeeVectorRecord
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            JobTitle = jobTitle,
            Summary = summary
        };

        await collection.UpsertAsync(employee);

        return employee;
    }

    public async Task<EmployeeVectorRecord?> DeleteEmployeeAsync(string fullName)
    {
        var employee = await FindEmployeeByFullNameAsync(fullName);
        if (employee is null)
        {
            return null;
        }

        await collection.DeleteAsync(employee.Id);
        return employee;
    }

    public async Task<List<EmployeeVectorRecord>> SearchEmployeesAsync(string query)
    {
        var list = new List<EmployeeVectorRecord>();
        
        await foreach (var result in collection.SearchAsync(query, 3))
        {
            list.Add(result.Record);
        }
        
        return list;
    }

    private async Task<EmployeeVectorRecord?> FindEmployeeByFullNameAsync(string fullName)
    {
        var employees = await GetEmployeesAsync();

        foreach (var employee in employees)
        {
            if (string.Equals(employee.FullName, fullName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return employee;
            }
        }

        return null;
    }

}
