namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// Marks an entity as belonging to a specific tenant.
    /// All tenant-scoped entities must implement this interface.
    /// Server-side EF Core global query filters key on TenantId.
    /// </summary>
    public interface ITenantEntity
    {
        Guid TenantId { get; set; }
    }
}
