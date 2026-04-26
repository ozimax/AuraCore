using Microsoft.Extensions.VectorData;

namespace AuraCore.Engine.Models;

public class EmployeeVectorRecord
{
    [VectorStoreKey] 
    public required Guid Id { get; init; } = Guid.NewGuid();

    [VectorStoreData] 
    public required string FullName { get; init; }

    [VectorStoreData] 
    public required string JobTitle { get; init; }

    [VectorStoreData] 
    public required string Summary { get; init; }

    [VectorStoreVector(1536)] 
    public string Vector => $"{FullName} | {JobTitle} | {Summary}";
}

 public  record EmployeeEntry(string FullName, string JobTitle, string Skills);
