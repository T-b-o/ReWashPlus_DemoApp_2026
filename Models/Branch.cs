namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// A physical branch / location belonging to a tenant.
    /// Services, pricing, staff, and operating hours are scoped per branch.
    /// </summary>
    public class Branch
    {
        public Guid   BranchId  { get; set; } = Guid.NewGuid();
        public Guid   TenantId  { get; set; }
        public string Name      { get; set; } = string.Empty;
        public string Address   { get; set; } = string.Empty;
        public bool   IsActive  { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
