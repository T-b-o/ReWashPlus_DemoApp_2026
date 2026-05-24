namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// A staff member's employment record at a specific branch.
    /// Uses composition with AppUser — NOT inheritance.
    /// One AppUser can have StaffProfiles at multiple branches.
    /// </summary>
    public class StaffProfile : IBranchEntity, IAuditableEntity
    {
        public Guid   StaffProfileId { get; set; } = Guid.NewGuid();

        /// <summary>FK to AppUser.UserId — the identity record for this person.</summary>
        public Guid   UserId         { get; set; }

        public Guid   TenantId       { get; set; }
        public Guid   BranchId       { get; set; }

        /// <summary>Role within the branch, e.g. "Washer", "Cashier", "Manager".</summary>
        public string Role           { get; set; } = string.Empty;

        public bool   IsActive       { get; set; } = true;
        public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt    { get; set; } = DateTime.UtcNow;
    }
}
