using AuraCore.Engine.Models;
using Microsoft.Extensions.VectorData;

namespace AuraCore.Engine.Services;

public class TalentService(VectorStoreCollection<Guid, EmployeeVectorRecord> collection) : ITalentService
{
    public async Task<List<EmployeeVectorRecord>> SearchEmployeesAsync(string query)
    {
        var list = new List<EmployeeVectorRecord>();
        
        await foreach (var result in collection.SearchAsync(query, 3))
        {
            list.Add(result.Record);
        }
        
        return list;
    }

}
