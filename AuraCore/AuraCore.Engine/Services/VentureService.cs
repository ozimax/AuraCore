using AuraCore.Engine.Models;
using Microsoft.Extensions.VectorData;

namespace AuraCore.Engine.Services;

public class VentureService(VectorStoreCollection<Guid, ProjectVectorRecord> collection) : IVentureService
{
    public async Task<List<ProjectVectorRecord>> SearchProjectsAsync(string query)
    {
        var list = new List<ProjectVectorRecord>();
        
        await foreach (var result in collection.SearchAsync(query, 3))
        {
            list.Add(result.Record);
        }
        
        return list;
    }

}
