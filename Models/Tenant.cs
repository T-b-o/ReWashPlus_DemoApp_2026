namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// A SaaS tenant — represents a car-wash company that subscribes to RewashPlus.
    /// One tenant can own many branches.
    /// </summary>
    public class Tenant
    {
        public Guid   TenantId    { get; set; } = Guid.NewGuid();

        /// <summary>Registered company name, e.g. "ABC Car Wash Group".</summary>
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>URL-friendly identifier, e.g. "abc-car-wash". Used for subdomain routing.</summary>
        public string Slug        { get; set; } = string.Empty;

        public bool   IsActive    { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
