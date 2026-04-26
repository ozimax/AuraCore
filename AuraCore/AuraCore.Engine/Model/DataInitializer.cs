using AuraCore.Engine.Models;
using Microsoft.Extensions.VectorData;
using System.Text.Json;

namespace AuraCore.Engine.Services;

public class DataInitializer(VectorStoreCollection<Guid, EmployeeVectorRecord> hrCollection, VectorStoreCollection<Guid, ProjectVectorRecord> crmCollection)
{
    public async Task InitializeAsync()
    {
        await hrCollection.EnsureCollectionExistsAsync();
        await crmCollection.EnsureCollectionExistsAsync();

        // 1. Seed HR Data from JSON
        if (!await HasData(hrCollection))
        {
            string hrJsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "employees.json");
            if (File.Exists(hrJsonPath))
            {
                var json = await File.ReadAllTextAsync(hrJsonPath);
                var employees = JsonSerializer.Deserialize<List<EmployeeEntry>>(json);
                
                if (employees != null)
                {
                    foreach (var emp in employees)
                    {
                        await hrCollection.UpsertAsync(new EmployeeVectorRecord
                        {
                            Id = Guid.NewGuid(),
                            FullName = emp.FullName,
                            JobTitle = emp.JobTitle,
                            Summary = emp.Skills
                        });
                    }
                }
            }
        }

        // 2. Seed CRM Data from JSON
        if (!await HasData(crmCollection))
        {
            string crmJsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "projects.json");
            if (File.Exists(crmJsonPath))
            {
                var json = await File.ReadAllTextAsync(crmJsonPath);
                var projects = JsonSerializer.Deserialize<List<ProjectEntry>>(json);
                
                if (projects != null)
                {
                    foreach (var proj in projects)
                    {
                        await crmCollection.UpsertAsync(new ProjectVectorRecord
                        {
                            Id = Guid.NewGuid(),
                            ProjectName = proj.ProjectName,
                            ClientName = proj.ClientName,
                            Revenue = proj.Revenue,
                            AssignedEmployees = string.Join(", ", proj.AssignedEmployees),
                            Summary = proj.Summary
                        });
                    }
                }
            }
        }
    }

    private async Task<bool> HasData<T>(VectorStoreCollection<Guid, T> collection) where T : class
    {
        var matchingItems = collection.GetAsync(item => true, top: 1);

        await foreach (var item in matchingItems) {
            return true;
        }

        return false;
    }

   

    
}
