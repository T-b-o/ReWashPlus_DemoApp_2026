using System;

namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// Represents a customer/client of the car wash business.
    /// Composed with AppUser via UserId — NOT inherited from AppUser.
    /// </summary>
    public class Customer : ITenantEntity, IAuditableEntity
    {
        /// <summary>Legacy integer key — used by LocalStorage layer.</summary>
        public int    Id         { get; set; }

        /// <summary>GUID primary key used by the server and sync layer.</summary>
        public Guid   CustomerId { get; set; } = Guid.NewGuid();

        // ── Multi-tenant Scoping ──────────────────────────────────────────────

        public Guid TenantId { get; set; }

        /// <summary>
        /// FK to AppUser.UserId — null for walk-in customers who have not registered.
        /// </summary>
        public Guid? UserId { get; set; }

        // ── Profile ───────────────────────────────────────────────────────────

        public string FirstName { get; set; } = string.Empty;
        public string LastName  { get; set; } = string.Empty;

        /// <summary>Unique phone number — primary identifier for walk-ins.</summary>
        public string PhoneNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        /// <summary>Preferred payment method (0: Cash, 1: Card, 2: EFT).</summary>
        public int PreferredPaymentMethod { get; set; } = 0;

        // ── Loyalty ───────────────────────────────────────────────────────────

        public decimal TotalSpent    { get; set; } = 0;
        public int     LoyaltyPoints { get; set; } = 0;

        // ── Offline Sync ──────────────────────────────────────────────────────

        public SyncStatus SyncState { get; set; } = SyncStatus.Pending;

        /// <summary>Optimistic-concurrency token; populated after first successful server sync.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // ── Audit ─────────────────────────────────────────────────────────────

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool     IsActive  { get; set; } = true;

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
