namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// A vehicle registered to a customer.
    /// Replaces the old Car model.
    /// Bookings reference VehicleId — never a free-text description string.
    /// </summary>
    public class Vehicle : ITenantEntity, IAuditableEntity
    {
        /// <summary>GUID primary key used by the server and sync layer.</summary>
        public Guid   VehicleId          { get; set; } = Guid.NewGuid();

        /// <summary>Legacy integer key kept for LocalStorage backward-compatibility.</summary>
        public int    Id                 { get; set; }

        public Guid   TenantId           { get; set; }

        public int    CustomerId         { get; set; }

        /// <summary>License / registration plate number (was Plate in Car model).</summary>
        public string RegistrationNumber { get; set; } = string.Empty;

        public string Make               { get; set; } = string.Empty;
        public string Model              { get; set; } = string.Empty;
        public int?   Year               { get; set; }
        public string Color              { get; set; } = string.Empty;
        public string VinNumber          { get; set; } = string.Empty;
        public VehicleCategory Category  { get; set; } = VehicleCategory.Sedan;

        /// <summary>Optimistic-concurrency token; populated after first successful server sync.</summary>
        public byte[] RowVersion         { get; set; } = Array.Empty<byte>();

        public bool   IsActive           { get; set; } = true;
        public DateTime CreatedAt        { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt        { get; set; } = DateTime.UtcNow;

        public SyncStatus SyncState      { get; set; } = SyncStatus.Pending;

        /// <summary>Human-readable display string, e.g. "2020 Toyota Corolla - White".</summary>
        public string DisplayName => $"{Year} {Make} {Model} - {Color}".Trim();
    }
}
