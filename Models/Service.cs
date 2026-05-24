using System;

namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// Represents a car wash service/offering with pricing.
    /// Services are scoped per branch — each branch can have different prices.
    /// </summary>
    public class Service : IBranchEntity, IAuditableEntity
    {
        /// <summary>Legacy integer key — used by LocalStorage layer.</summary>
        public int  Id        { get; set; }

        /// <summary>GUID primary key used by the server and sync layer.</summary>
        public Guid ServiceId { get; set; } = Guid.NewGuid();

        // ── Multi-tenant Scoping ──────────────────────────────────────────────

        public Guid TenantId { get; set; }
        public Guid BranchId { get; set; }
        
        /// <summary>
        /// Service name (e.g., "Basic Wash", "Deluxe", "Interior Clean", "Full Valet")
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Detailed description
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Price in ZAR (South African Rand)
        /// </summary>
        public decimal Price { get; set; } = 0;
        
        /// <summary>
        /// Estimated duration in minutes
        /// </summary>
        public int DurationMinutes { get; set; } = 30;
        
        public bool IsActive  { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Optimistic-concurrency token; populated after first successful server sync.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
