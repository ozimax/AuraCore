using Microsoft.Extensions.VectorData;

namespace AuraCore.Engine.Models;

public class ProjectVectorRecord
{
    [VectorStoreKey] 
    public required Guid Id { get; init; } = Guid.NewGuid();

    [VectorStoreData] 
    public required string ProjectName { get; init; }

    [VectorStoreData] 
    public required string ClientName { get; init; }

    [VectorStoreData] 
    public required double Revenue { get; init; }

    [VectorStoreData] 
    public required string AssignedEmployees { get; init; }
    
    [VectorStoreData] 
    public required string Summary { get; init; }

    [VectorStoreVector(1536)] 
    public string Vector => $"{ProjectName} | {ClientName} | {AssignedEmployees} | {Summary} | Revenue: {Revenue}";
}

public record ProjectEntry(
        string ProjectName,
        string ClientName,
        double Revenue,
        List<string> AssignedEmployees,
        string Summary);
