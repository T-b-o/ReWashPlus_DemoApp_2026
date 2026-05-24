namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// Immutable audit trail record.
    /// Populated server-side by AuditMiddleware or EF Core interceptors.
    /// Stored locally on the client for offline review.
    /// </summary>
    public class AuditLog
    {
        public long   AuditLogId   { get; set; }
        public Guid   TenantId     { get; set; }
        public Guid   UserId       { get; set; }
        public string EntityName   { get; set; } = string.Empty;
        public string EntityId     { get; set; } = string.Empty;

        /// <summary>"Create", "Update", or "Delete".</summary>
        public string Action       { get; set; } = string.Empty;

        /// <summary>JSON snapshot of values before the change. Null for Create.</summary>
        public string? OldValues   { get; set; }

        /// <summary>JSON snapshot of values after the change. Null for Delete.</summary>
        public string? NewValues   { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
