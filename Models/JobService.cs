using System;

namespace ReWashPlus_DemoApp.Models
{
    /// <summary>
    /// Represents the addition of a service to a job.
    /// This is a join table allowing multiple services per job with pricing history.
    /// </summary>
    public class JobService
    {
        public int Id { get; set; }
        
        public int JobId { get; set; }
        
        public int ServiceId { get; set; }
        
        /// <summary>
        /// Service name at the time of booking (for historical accuracy)
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;
        
        /// <summary>
        /// Quantity/count of this service
        /// </summary>
        public int Quantity { get; set; } = 1;
        
        /// <summary>
        /// Price paid at the time of service (for historical accuracy if prices change)
        /// </summary>
        public decimal PriceAtTime { get; set; } = 0;
        
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Calculate subtotal for this service
        /// </summary>
        public decimal Subtotal => PriceAtTime * Quantity;
    }
}
