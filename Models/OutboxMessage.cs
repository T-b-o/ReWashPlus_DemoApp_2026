namespace ReWashPlus_DemoApp.Models
{
    public enum OutboxStatus
    {
        Pending   = 0,
        Processed = 1,
        Failed    = 2
    }

    /// <summary>
    /// Transactional outbox record.
    /// Domain events are written here atomically with the entity change,
    /// then an Azure Function / hosted service publishes them to the message bus.
    /// This guarantees at-least-once delivery without distributed transactions.
    /// </summary>
    public class OutboxMessage
    {
        public Guid   Id          { get; set; } = Guid.NewGuid();
        public Guid   TenantId   { get; set; }

        /// <summary>Fully-qualified event type name, e.g. "BookingCreatedEvent".</summary>
        public string EventType   { get; set; } = string.Empty;

        /// <summary>JSON-serialised event payload.</summary>
        public string Payload     { get; set; } = string.Empty;

        public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
        public int    RetryCount  { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
    }
}
