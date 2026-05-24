namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// Marks an entity as belonging to a specific branch within a tenant.
    /// Inherits ITenantEntity so every branch-scoped entity also has TenantId.
    /// </summary>
    public interface IBranchEntity : ITenantEntity
    {
        Guid BranchId { get; set; }
    }
}
